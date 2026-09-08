using UnityEngine;

public class BreedingRoom : Room
{
    [Header("Breeding Settings")]
    [SerializeField] private GameObject creaturePrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Love Stat Configuration")]
    [SerializeField] private string loveStatName = "Love";
    [SerializeField] private float loveGainRate = 15f; // Fills love quickly to overpower natural decay

    [Header("Mutation Configuration")]
    [Range(0f, 1f)]
    [SerializeField] private float mutationChance = 0.10f;
    [SerializeField] private float mutationIntensity = 0.25f;

    [Header("Visual Mutation Assets")]
    [SerializeField] private Mesh rareMutatedMesh;

    private bool isTimerRunning; // Kept to maintain log outputs clean
    private bool hasSentTooManyWarning = false;

    private void Start()
    {
        Debug.LogFormat("<color=yellow>[System Ready]</color> Love-driven natural breeding active for <b>{0}</b>.", this.gameObject.name);
    }

    private void Update()
    {
        int currentCount = GetRoomCount();

        // Condition 1: Exactly two creatures are isolated together
        if (currentCount == 2)
        {
            hasSentTooManyWarning = false;

            Creature parentA = null;
            Creature parentB = null;
            int index = 0;

            // Safely loop out parent references from the studio's collection loop
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
                Debug.Log("<color=orange>[Romance Started]</color> Exactly 2 creatures isolated. Generating Love stats...");
            }

            // 1. Actively pump up the Love stat for both parents inside their data lists
            parentA.AddStat(loveStatName, loveGainRate * Time.deltaTime);
            parentB.AddStat(loveStatName, loveGainRate * Time.deltaTime);

            // 2. Fetch the current love metrics to see if they're ready to hatch a baby
            Stat loveA = parentA.GetStat(loveStatName);
            Stat loveB = parentB.GetStat(loveStatName);

            if (loveA != null && loveB != null)
            {
                // Breeding condition: Triggers automatically when love capacity hits MaxValue!
                if (loveA.GetTrait().Value >= loveA.GetTrait().MaxValue ||
                    loveB.GetTrait().Value >= loveB.GetTrait().MaxValue)
                {
                    Debug.Log("<color=green>[Love Maxed Out]</color> Romance threshold achieved! Breeding naturally...");

                    // Reset parents' love data back to baseline 0 so they don't loop instantly
                    parentA.SubtractStat(loveStatName, loveA.GetTrait().MaxValue);
                    parentB.SubtractStat(loveStatName, loveB.GetTrait().MaxValue);

                    isTimerRunning = false;
                    ExecuteAutomaticBreeding(parentA, parentB);
                }
            }
        }
        // Condition 2: Overcrowding capacity limits breached (Direct brief compliance!)
        else if (currentCount > 2)
        {
            isTimerRunning = false;

            if (!hasSentTooManyWarning)
            {
                hasSentTooManyWarning = true;
                if (UIManager.Instance != null)
                {
                    int excess = currentCount - 2;
                    UIManager.Instance.DisplayWarningMessage($"Breeding Room Blocked: Remove {excess} creature(s)!");
                }
                Debug.LogErrorFormat("<color=red>[Capacity Alert]</color> The {0} has too many items inside.", this.gameObject.name);
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
        // Instantiate baby prefab object
        GameObject babyObject = Instantiate(creaturePrefab, spawnPoint.position, Quaternion.identity);
        Creature baby = babyObject.GetComponent<Creature>();

        if (baby == null) return;

        // Genetic Inheritance Loop
        foreach (Stat babyStat in baby.GetStats())
        {
            string traitName = babyStat.GetTrait().Name;
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

        // Apply physical Scale layout modifier
        Stat visualTrait = baby.GetStat("Speed");
        if (visualTrait != null)
        {
            float visualModifier = visualTrait.GetTrait().Value / visualTrait.GetTrait().MaxValue;
            float calculatedScale = Mathf.Lerp(0.5f, 2.0f, visualModifier);
            baby.transform.localScale = new Vector3(calculatedScale, calculatedScale, calculatedScale);
        }

        // Apply visual Color or Mesh mutations
        if (Random.value < mutationChance)
        {
            Renderer babyRenderer = baby.GetComponentInChildren<Renderer>();
            if (babyRenderer != null)
            {
                babyRenderer.material.color = new Color(Random.value, Random.value, Random.value);
            }

            if (rareMutatedMesh != null)
            {
                MeshFilter babyMeshFilter = baby.GetComponentInChildren<MeshFilter>();
                if (babyMeshFilter != null) babyMeshFilter.sharedMesh = rareMutatedMesh;
            }
        }

        // Sync runtime components
        UnityEngine.AI.NavMeshAgent babyAgent = baby.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (babyAgent != null && baby.GetStat("Speed") != null)
        {
            babyAgent.speed = baby.GetStat("Speed").GetTrait().Value;
        }

        baby.gameObject.name = string.Format("{0} Jr.", parentA.GetName());
        Debug.LogFormat("<color=green>[Birth Success]</color> Added {0} safely to ecosystem.", baby.gameObject.name);
    }
}
