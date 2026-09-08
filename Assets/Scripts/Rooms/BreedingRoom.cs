using UnityEngine;

public class BreedingRoom : Room
{
    [Header("Breeding Settings")]
    [SerializeField] private GameObject creaturePrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Love Stat Configuration")]
    [SerializeField] private string loveStatName = "Love";
    [SerializeField] private float loveGainRate = 15f; // Fills love smoothly over time

    [Header("Pacing & Balance")]
    [SerializeField] private float birthCooldownDuration = 10f; // Seconds parents must wait before breeding again
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
        Debug.LogFormat("<color=yellow>[System Ready]</color> Love-driven natural breeding active for <b>{0}</b>.", this.gameObject.name);
    }

    private void Update()
    {
        // 1. Handle our recovery cooldown ticking mechanism if it is active [10]
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            return; // Completely freezes love generation while the cooldown is active!
        }

        int currentCount = GetRoomCount();

        // Condition 1: Exactly two creatures are isolated together
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
                Debug.Log("<color=orange>[Romance Started]</color> Exactly 2 creatures isolated. Generating Love stats...");
            }

            // Progress Love values over time for both parents
            parentA.AddStat(loveStatName, loveGainRate * Time.deltaTime);
            parentB.AddStat(loveStatName, loveGainRate * Time.deltaTime);

            // Fetch current live runtime stats
            Stat loveA = parentA.GetStat(loveStatName);
            Stat loveB = parentB.GetStat(loveStatName);

            if (loveA != null && loveB != null)
            {
                float currentLoveA = loveA.GetTrait().Value;
                float maxLoveLimit = loveA.GetTrait().MaxValue;

                if (currentLoveA >= maxLoveLimit && maxLoveLimit > 0)
                {
                    Debug.Log("<color=green>[Love Maxed Out]</color> Romance threshold achieved! Breeding naturally...");

                    // Reset love metrics fully back to 0
                    loveA.GetTrait().Value = 0f;
                    loveB.GetTrait().Value = 0f;

                    isTimerRunning = false;

                    //  THE LOOP FIX: Activate the cooldown timer immediately [10]
                    cooldownTimer = birthCooldownDuration;
                    Debug.LogFormat("<color=yellow>[Cooldown Active]</color> Parents are resting. Love generation paused for {0} seconds.", birthCooldownDuration);

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
                    UIManager.Instance.DisplayWarningMessage($"Breeding Room Blocked: Remove {excess} creature(s)!");
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
