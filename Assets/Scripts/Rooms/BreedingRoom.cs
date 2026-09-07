using UnityEngine;

public class BreedingRoom : Room
{
    [Header("Breeding Settings")]
    [SerializeField] private GameObject creaturePrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Natural Automation")]
    [SerializeField] private float breedingDuration = 5.0f; // Seconds parents must spend together
    private float breedingTimer;
    private bool isTimerRunning;

    [Header("Mutation Configuration")]
    [Range(0f, 1f)]
    [SerializeField] private float mutationChance = 0.10f;
    [SerializeField] private float mutationIntensity = 0.25f;

    [Header("Visual Mutation Assets")]
    [SerializeField] private Mesh rareMutatedMesh;

    private void Start()
    {
        ResetBreedingTimer();
        Debug.LogFormat("<color=yellow>[System Ready]</color> Natural breeding automated for <b>{0}</b>.", this.gameObject.name);
    }

    private void Update()
    {
        int currentCount = GetRoomCount();

        // Organic Automation Logic
        if (currentCount == 2)
        {
            if (!isTimerRunning)
            {
                isTimerRunning = true;
                Debug.Log("<color=orange>[Timer Started]</color> Exactly 2 creatures isolated. Incubation active...");
            }

            // Tick down the automated timer
            breedingTimer -= Time.deltaTime;

            if (breedingTimer <= 0f)
            {
                Debug.Log("<color=green>[Timer Complete]</color> Incubation successful!");
                ExecuteAutomaticBreeding();
            }
        }
        else
        {
            // Instantly aborts and resets if a 3rd creature enters or one leaves
            if (isTimerRunning)
            {
                ResetBreedingTimer();
                Debug.LogWarning("<color=red>[Breeding Aborted]</color> Room occupancy disrupted! Resetting genetic countdown.");
            }
        }
    }

    private void ResetBreedingTimer()
    {
        breedingTimer = breedingDuration;
        isTimerRunning = false;
    }

    private void ExecuteAutomaticBreeding()
    {
        ResetBreedingTimer();

        Creature parentA = null;
        Creature parentB = null;
        int index = 0;

        foreach (Transform child in children)
        {
            if (index == 0) parentA = child.GetComponent<Creature>();
            if (index == 1) parentB = child.GetComponent<Creature>();
            index++;
        }

        if (parentA == null || parentB == null) return;

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
    }
}
