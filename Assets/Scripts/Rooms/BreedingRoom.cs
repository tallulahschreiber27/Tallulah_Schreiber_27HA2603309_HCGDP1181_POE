using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BreedingRoom : Room
{
    [Header("Breeding & Placement Settings")]
    [SerializeField] private Transform spawnPoint;
    [Tooltip("Assign another room (e.g., Main Room / Nursery) so the baby spawns inside a valid room on the NavMesh without overcrowding the Breeding Room.")]
    [SerializeField] private Room nurseryRoom;

    [Header("Love Stat Configuration")]
    [SerializeField] private string loveStatName = "Love";
    [SerializeField] private float loveGainRate = 15f;

    [Header("Pacing & Balance")]
    [SerializeField] private float birthCooldownDuration = 10f;
    private float cooldownTimer = 0f;

    [Header("Inheritance & Mutation Configuration")]
    [Range(0f, 1f)]
    [SerializeField] private float uniqueTraitInheritChance = 0.50f;
    [Range(0f, 1f)]
    [SerializeField] private float mutationChance = 0.15f;
    [SerializeField] private float mutationIntensity = 0.15f;

    [Header("Visual Mutation Assets")]
    [SerializeField] private Mesh rareMutatedMesh;

    private bool isTimerRunning;
    private bool hasSentTooManyWarning = false;

    public override void Start()
    {
        base.Start();
        Debug.Log("[System Ready] Love-driven natural breeding active.");
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            return;
        }

        int currentCount = GetRoomCount();

        if (currentCount == 2)
        {
            hasSentTooManyWarning = false;

            Creature parentA = children[0].GetComponent<Creature>();
            Creature parentB = children[1].GetComponent<Creature>();

            if (parentA == null || parentB == null) return;


            if (!isTimerRunning)
            {
                isTimerRunning = true;
                Debug.Log("[Romance Started] Exactly 2 creatures isolated. Generating Love stats...");
            }

            parentA.AddStat(loveStatName, loveGainRate * Time.deltaTime);
            parentB.AddStat(loveStatName, loveGainRate * Time.deltaTime);

            Stat loveA = parentA.GetStat(loveStatName);
            Stat loveB = parentB.GetStat(loveStatName);

            if (loveA != null && loveB != null && loveA.GetTrait() != null && loveB.GetTrait() != null)
            {
                float currentLoveA = loveA.GetTrait().Value;
                float maxLoveLimit = loveA.GetTrait().MaxValue;

                if (currentLoveA >= maxLoveLimit && maxLoveLimit > 0)
                {
                    Debug.Log("[Love Maxed Out] Romance threshold achieved! Breeding naturally...");

                    loveA.GetTrait().Value = 0f;
                    loveB.GetTrait().Value = 0f;

                    isTimerRunning = false;
                    cooldownTimer = birthCooldownDuration;
                    Debug.Log("[Cooldown Active] Parents are resting.");

                    ExecuteAutomaticBreeding(parentA, parentB);
                }
            }
        }
        else if (currentCount > 2)
        {
            isTimerRunning = false;

            if (!hasSentTooManyWarning)
            {
                hasSentTooManyWarning = true;
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.DisplayWarningMessage("Breeding Room Blocked: Remove excess creature(s)!");
                }
            }
        }
        else
        {
            isTimerRunning = false;
            hasSentTooManyWarning = false;
        }
    }

    private void ExecuteAutomaticBreeding(Creature parentA, Creature parentB)
    {
        Vector3 targetSpawnPos = GetValidNavMeshPosition();

        GameObject babyObject = Instantiate(parentA.gameObject, targetSpawnPos, Quaternion.identity);
        Creature baby = babyObject.GetComponent<Creature>();

        if (baby == null) return;

        string babyName = string.Format("{0}-{1} Jr.", parentA.GetName(), parentB.GetName());
        baby.gameObject.name = babyName;
        baby.SetName(babyName);

        // 1. Dynamically inherit and mutate Trait/Stat components from BOTH parents
        InheritTraitsFromBothParents(parentA, parentB, baby);

        // 2. Apply visual inheritance & subtle generational mutation
        ApplyVisualInheritanceAndMutation(parentA, parentB, baby);

        // 3. Place baby inside a valid Room on the NavMesh to prevent NullReferenceExceptions
        PlaceBabyInValidRoom(baby, targetSpawnPos);

        Debug.LogFormat("[Lineage Verified] Parents: {0} ({1}) + {2} ({3}) -> Offspring: {4} | Total Traits: {5}",
            parentA.GetName(), parentA.GetSpeciesType(),
            parentB.GetName(), parentB.GetSpeciesType(),
            baby.gameObject.name, baby.GetStats().Count);
    }

    private void InheritTraitsFromBothParents(Creature parentA, Creature parentB, Creature baby)
    {
        List<Stat> statsA = parentA.GetStats();
        List<Stat> statsB = parentB.GetStats();

        // Collect all unique trait names from BOTH parents
        HashSet<string> allTraitNames = new HashSet<string>();
        foreach (Stat s in statsA)
        {
            if (s != null && s.GetTrait() != null)
            {
                allTraitNames.Add(s.GetTrait().Name);
            }
        }
        foreach (Stat s in statsB)
        {
            if (s != null && s.GetTrait() != null)
            {
                allTraitNames.Add(s.GetTrait().Name);
            }
        }

        foreach (string traitName in allTraitNames)
        {
            Stat statA = parentA.GetStat(traitName);
            Stat statB = parentB.GetStat(traitName);
            Stat babyStat = baby.GetStat(traitName);

            // Reset Love to a baseline starting value on newborns
            if (traitName == loveStatName)
            {
                if (babyStat != null && babyStat.GetTrait() != null)
                {
                    babyStat.GetTrait().Value = 15f;
                }
                continue;
            }

            bool hasParentA = (statA != null && statA.GetTrait() != null);
            bool hasParentB = (statB != null && statB.GetTrait() != null);

            if (hasParentA && hasParentB)
            {
                // SHARED TRAIT: Both parents have this trait -> always inherited and blended
                Trait blendedTrait = CreateInheritedTrait(statA.GetTrait(), statB.GetTrait());
                if (babyStat != null)
                {
                    babyStat.InitializeTrait(blendedTrait);
                }
            }
            else if (hasParentA && !hasParentB)
            {
                // UNIQUE TO PARENT A: Discerning selection decides if baby keeps it
                if (Random.value <= uniqueTraitInheritChance)
                {
                    Trait inheritedTrait = CreateSingleParentTrait(statA.GetTrait());
                    if (babyStat != null)
                    {
                        babyStat.InitializeTrait(inheritedTrait);
                    }
                }
                else if (babyStat != null)
                {
                    baby.RemoveStat(babyStat);
                }
            }
            else if (!hasParentA && hasParentB)
            {
                // UNIQUE TO PARENT B: Discerning selection decides if baby gains Parent B's Stat component
                if (Random.value <= uniqueTraitInheritChance)
                {
                    Stat addedStat = (Stat)baby.gameObject.AddComponent(statB.GetType());
                    Trait inheritedTrait = CreateSingleParentTrait(statB.GetTrait());
                    addedStat.InitializeTrait(inheritedTrait);
                    baby.RegisterStat(addedStat);
                }
            }
        }

        baby.RefreshMovementSpeed();
    }

    private Trait CreateInheritedTrait(Trait traitA, Trait traitB)
    {
        float minVal = Mathf.Min(traitA.Value, traitB.Value);
        float maxVal = Mathf.Max(traitA.Value, traitB.Value);
        float inheritedValue = Random.Range(minVal, maxVal);

        float blendedMax = (traitA.MaxValue + traitB.MaxValue) * 0.5f;

        // Subtle generational mutation
        if (Random.value < mutationChance)
        {
            float mutationFactor = 1f + Random.Range(-mutationIntensity, mutationIntensity);
            blendedMax = Mathf.Max(1f, blendedMax * mutationFactor);
            inheritedValue *= mutationFactor;
            Debug.LogFormat("[Mutation Occurred] Shared trait '{0}' subtly mutated across generation!", traitA.Name);
        }

        inheritedValue = Mathf.Clamp(inheritedValue, 0f, blendedMax);

        Trait result = new Trait(traitA.Name, blendedMax, traitA.Description);
        result.Value = inheritedValue;
        result.MaxValue = blendedMax;
        return result;
    }

    private Trait CreateSingleParentTrait(Trait sourceTrait)
    {
        float inheritedMax = sourceTrait.MaxValue;
        float inheritedValue = sourceTrait.Value;

        // Subtle generational mutation
        if (Random.value < mutationChance)
        {
            float mutationFactor = 1f + Random.Range(-mutationIntensity, mutationIntensity);
            inheritedMax = Mathf.Max(1f, inheritedMax * mutationFactor);
            inheritedValue *= mutationFactor;
            Debug.LogFormat("[Mutation Occurred] Unique inherited trait '{0}' subtly mutated!", sourceTrait.Name);
        }

        inheritedValue = Mathf.Clamp(inheritedValue, 0f, inheritedMax);

        Trait result = new Trait(sourceTrait.Name, inheritedMax, sourceTrait.Description);
        result.Value = inheritedValue;
        result.MaxValue = inheritedMax;
        return result;
    }

    private void ApplyVisualInheritanceAndMutation(Creature parentA, Creature parentB, Creature baby)
    {
        // Scale inheritance with subtle mutation
        float avgScale = (parentA.transform.localScale.x + parentB.transform.localScale.x) * 0.5f;
        if (Random.value < mutationChance)
        {
            avgScale *= (1f + Random.Range(-mutationIntensity, mutationIntensity));
        }
        avgScale = Mathf.Clamp(avgScale, 0.5f, 2.0f);
        baby.transform.localScale = new Vector3(avgScale, avgScale, avgScale);

        // Mesh 50/50 inheritance from either parent + rare mutation
        MeshFilter babyMeshFilter = baby.GetComponentInChildren<MeshFilter>();
        MeshFilter fatherMeshFilter = parentA.GetComponentInChildren<MeshFilter>();
        MeshFilter motherMeshFilter = parentB.GetComponentInChildren<MeshFilter>();

        if (babyMeshFilter != null && fatherMeshFilter != null && motherMeshFilter != null)
        {
            babyMeshFilter.sharedMesh = (Random.value > 0.5f) ? fatherMeshFilter.sharedMesh : motherMeshFilter.sharedMesh;
        }

        if (Random.value < mutationChance && rareMutatedMesh != null && babyMeshFilter != null)
        {
            babyMeshFilter.sharedMesh = rareMutatedMesh;
        }
        // Color inheritance + visible generational color shift
        Renderer babyRenderer = baby.GetComponentInChildren<Renderer>();
        Renderer rendererA = parentA.GetComponentInChildren<Renderer>();
        Renderer rendererB = parentB.GetComponentInChildren<Renderer>();

        if (babyRenderer != null && rendererA != null && rendererB != null)
        {
            Color colorA = rendererA.material.color;
            Color colorB = rendererB.material.color;

            // If both parents are still default white, generate a distinct inherited hue;
            // otherwise blend the parents' colors and apply a subtle generational tint shift
            if (colorA == Color.white && colorB == Color.white)
            {
                babyRenderer.material.color = Color.HSVToRGB(Random.value, Random.Range(0.45f, 0.85f), Random.Range(0.75f, 1.0f));
            }
            else
            {
                Color blendedColor = Color.Lerp(colorA, colorB, Random.Range(0.25f, 0.75f));
                float shift = (Random.value < mutationChance) ? 0.35f : 0.15f;
                blendedColor.r = Mathf.Clamp01(blendedColor.r + Random.Range(-shift, shift));
                blendedColor.g = Mathf.Clamp01(blendedColor.g + Random.Range(-shift, shift));
                blendedColor.b = Mathf.Clamp01(blendedColor.b + Random.Range(-shift, shift));
                babyRenderer.material.color = blendedColor;
            }
        }
    }

    private Color GetRendererColor(Renderer targetRenderer)
    {
        if (targetRenderer.material.HasProperty("_BaseColor"))
        {
            return targetRenderer.material.GetColor("_BaseColor");
        }
        return targetRenderer.material.color;
    }

    private void SetRendererColor(Renderer targetRenderer, Color newColor)
    {
        if (targetRenderer.material.HasProperty("_BaseColor"))
        {
            targetRenderer.material.SetColor("_BaseColor", newColor);
        }
        targetRenderer.material.color = newColor;
    }


    private Vector3 GetValidNavMeshPosition()
    {
        Vector3 desiredPos = transform.position;
        if (nurseryRoom != null)
        {
            desiredPos = nurseryRoom.transform.position;
        }
        else if (spawnPoint != null)
        {
            desiredPos = spawnPoint.position;
        }
        // Snap to the nearest valid point on the baked NavMesh within 5 units
        NavMeshHit hit;
        if (NavMesh.SamplePosition(desiredPos, out hit, 5.0f, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return desiredPos;
    }
    private void PlaceBabyInValidRoom(Creature baby, Vector3 validNavPos)
    {
        Room destinationRoom = (nurseryRoom != null) ? nurseryRoom : this;
        baby.transform.SetParent(destinationRoom.transform);
        NavMeshAgent babyAgent = baby.GetComponent<NavMeshAgent>();

        if (babyAgent != null)
        {
            babyAgent.Warp(validNavPos);
            babyAgent.ResetPath();
        }
    }
}