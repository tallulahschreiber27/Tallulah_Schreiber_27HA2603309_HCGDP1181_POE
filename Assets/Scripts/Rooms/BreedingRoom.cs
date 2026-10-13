using UnityEngine;

public class BreedingRoom : Room
{
    [Header("Breeding Settings")]
    [SerializeField] private Transform spawnPoint;

    [Header("Love Stat Configuration")]
    [SerializeField] private string loveStatName = "Love";
    [SerializeField] private float loveGainRate = 15f;

    [Header("Pacing & Balance")]
    [SerializeField] private float birthCooldownDuration = 10f;
    private float cooldownTimer = 0f;

    [Header("Mutation Configuration")]
    [Range(0f, 1f)]
    [SerializeField] private float mutationChance = 0.10f;
    [SerializeField] private float mutationIntensity = 0.25f;

    [Header("Visual Mutation Assets")]
    [SerializeField] private Mesh rareMutatedMesh;

    private bool isTimerRunning;
    private bool hasSentTooManyWarning = false;

    private void Start()
    {
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

            Creature parentA = null;
            Creature parentB = null;
            int index = 0;

            foreach (Transform child in children)
            {
                Creature c = child.GetComponent<Creature>();
                if (c != null)
                {
                    if (index == 0) parentA = c;
                    if (index == 1) parentB = c;
                    index++;
                }
            }

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

            if (loveA != null && loveB != null)
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
                    int excess = currentCount - 2;
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
        // Cloning the parent game object
        GameObject babyObject = Instantiate(parentA.gameObject, spawnPoint.position, Quaternion.identity);
        Creature baby = babyObject.GetComponent<Creature>();

        if (baby == null) return;

        // Stat inheritance
        foreach (Stat babyStat in baby.GetStats())
        {
            string traitName = babyStat.GetTrait().Name;

            if (traitName == loveStatName)
            {
                babyStat.GetTrait().Value = 15f;
                continue;
            }

            Stat fatherStat = parentA.GetStat(traitName);
            Stat motherStat = parentB.GetStat(traitName);

            if (fatherStat != null && motherStat != null)
            {
                float fatherVal = fatherStat.GetTrait().Value;
                float motherVal = motherStat.GetTrait().Value;

                float inheritedValue = Random.Range(Mathf.Min(fatherVal, motherVal), Mathf.Max(fatherVal, motherVal));

                if (Random.value < mutationChance)
                {
                    float mutationOffset = Random.Range(-mutationIntensity, mutationIntensity) * inheritedValue;
                    inheritedValue += mutationOffset;
                }

                inheritedValue = Mathf.Clamp(inheritedValue, 0, babyStat.GetTrait().MaxValue);
                babyStat.GetTrait().Value = inheritedValue;
            }
        }

        // Apply scale modifier dynamically matching performance speed stat allocations
        Stat visualTrait = baby.GetStat("Speed");
        if (visualTrait != null)
        {
            float visualModifier = visualTrait.GetTrait().Value / visualTrait.GetTrait().MaxValue;
            float calculatedScale = Mathf.Lerp(0.5f, 2.0f, visualModifier);
            baby.transform.localScale = new Vector3(calculatedScale, calculatedScale, calculatedScale);
        }

        // Genetic identification for new mesh merge
        CreatureSpecies typeA = parentA.GetSpeciesType();
        CreatureSpecies typeB = parentB.GetSpeciesType();

        baby.gameObject.name = string.Format("{0} Jr.", parentA.GetName());

        // Physical mesh fusion
        Renderer babyRenderer = baby.GetComponentInChildren<Renderer>();
        MeshFilter babyMeshFilter = baby.GetComponentInChildren<MeshFilter>();

        MeshFilter fatherMeshFilter = parentA.GetComponentInChildren<MeshFilter>();
        MeshFilter motherMeshFilter = parentB.GetComponentInChildren<MeshFilter>();

        // Copy parent mesh structures cleanly using a 50/50 genetic coin flip layout
        if (babyMeshFilter != null && fatherMeshFilter != null && motherMeshFilter != null)
        {
            babyMeshFilter.sharedMesh = (Random.value > 0.5f) ? fatherMeshFilter.sharedMesh : motherMeshFilter.sharedMesh;
        }

        // Creates a random colour
        if (babyRenderer != null)
        {
            babyRenderer.material.color = new Color(Random.value, Random.value, Random.value);
        }

        // Absolute mutation fallback override for structural models
        if (Random.value < mutationChance && rareMutatedMesh != null && babyMeshFilter != null)
        {
            babyMeshFilter.sharedMesh = rareMutatedMesh;
        }

        // Sync local NavMesh Agent parameters
        UnityEngine.AI.NavMeshAgent babyAgent = baby.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (babyAgent != null && baby.GetStat("Speed") != null)
        {
            babyAgent.speed = baby.GetStat("Speed").GetTrait().Value;
        }

        Debug.LogFormat("[Lineage Verified] Parents: {0} ({1}) + {2} ({3}) -> Offspring: {4}",
            parentA.GetName(), typeA, parentB.GetName(), typeB, baby.gameObject.name);
    }
}
