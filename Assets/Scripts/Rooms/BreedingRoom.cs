using UnityEngine;

public class BreedingRoom : Room
{
    [Header("Breeding Settings")]
    [SerializeField] private GameObject creaturePrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Mutation Configuration")]
    [Range(0f, 1f)]
    [SerializeField] private float mutationChance = 0.10f;
    [SerializeField] private float mutationIntensity = 0.25f;

    [Header("Testing Tool")]
    [SerializeField] private KeyCode testBreedKey = KeyCode.Space;

    private void Start()
    {
        // Debug to confirm the script is active in the scene on startup
        Debug.LogFormat("<color=yellow>[BreedingRoom System Setup]</color> Active on GameObject: <b>{0}</b>. Waiting for key input: <b>{1}</b>.", this.gameObject.name, testBreedKey);
    }

    private void Update()
    {
        if (Input.GetKeyDown(testBreedKey))
        {
            int detectedCount = GetRoomCount();
            Debug.LogFormat("<color=orange>[Input Detected]</color> Key <b>{0}</b> pressed! Studio engine currently reports <b>{1}</b> creature(s) in this room.", testBreedKey, detectedCount);

            TryBreedCreatures();
        }
    }

    /// <summary>
    /// Executes the room tracking parameters and breeds exactly 2 creatures.
    /// </summary>
    public void TryBreedCreatures()
    {
        int currentCount = GetRoomCount();

        // Check for missing setup configuration values in Inspector
        if (creaturePrefab == null)
        {
            Debug.LogError("<color=red>[Setup Error]</color> You forgot to drag your Creature Prefab into the script slot in the Inspector!", this);
            return;
        }
        if (spawnPoint == null)
        {
            Debug.LogError("<color=red>[Setup Error]</color> You forgot to assign a Spawn Point Transform reference in the Inspector!", this);
            return;
        }

        // 1. Enforce strict capacity limits (Section B Brief Criteria)
        if (currentCount < 2)
        {
            Debug.LogWarningFormat("<color=orange>[Validation Failed]</color> The {0} does not have enough creatures to breed. (Current: {1}/2)", this.gameObject.name, currentCount);
            return;
        }

        if (currentCount > 2)
        {
            int excess = currentCount - 2;
            Debug.LogErrorFormat("<color=red>[Validation Blocked]</color> The {0} has too many creatures! Remove {1} creature(s) to isolate exactly 2 parents.", this.gameObject.name, excess);
            return;
        }

        // 2. Safely extract parent components out of the studio's collection
        if (children == null || children.Count < 2)
        {
            Debug.LogError("<color=red>[System Mismatch]</color> Room tracker count is 2, but the underlying children array references are missing or broken!");
            return;
        }

        Creature parentA = children[0].GetComponent<Creature>();
        Creature parentB = children[1].GetComponent<Creature>();

        if (parentA == null || parentB == null)
        {
            Debug.LogError("<color=red>[Component Error]</color> Found game objects in the room, but they are missing the required 'Creature' script component!");
            return;
        }

        Debug.LogFormat("<color=green>[Validation Passed]</color> Successfully pairing parent profiles: <b>{0}</b> and <b>{1}</b>. Initializing genetic calculations...", parentA.GetName(), parentB.GetName());
        ExecuteBirth(parentA, parentB);
    }

    private void ExecuteBirth(Creature father, Creature mother)
    {
        // 3. Spawn the baby creature prefab into the scene
        GameObject babyObject = Instantiate(creaturePrefab, spawnPoint.position, Quaternion.identity);
        Creature baby = babyObject.GetComponent<Creature>();

        if (baby == null)
        {
            Debug.LogError("<color=red>[Prefab Error]</color> The baby spawned, but the Prefab item is missing the 'Creature' data class script!");
            return;
        }

        Debug.Log("<color=cyan>[Hatching Engine]</color> New baby object instantiated successfully. Calculating DNA inheritance array strings...");

        // 4. Genetic Inheritance Loop
        foreach (Stat babyStat in baby.GetStats())
        {
            string traitName = babyStat.GetTrait().Name;

            Stat fatherStat = father.GetStat(traitName);
            Stat motherStat = mother.GetStat(traitName);

            if (fatherStat != null && motherStat != null)
            {
                float fatherVal = fatherStat.GetTrait().Value;
                float motherVal = motherStat.GetTrait().Value;

                // Pick a midpoint between parent values
                float inheritedValue = Random.Range(Mathf.Min(fatherVal, motherVal), Mathf.Max(fatherVal, motherVal));
                Debug.LogFormat("[Genetics] Trait <b>{0}</b> -> Parent Mix Baseline Value computed as: {1:F2}", traitName, inheritedValue);

                // 5. Mutation Algorithm (Section B Brief Criteria)
                if (Random.value < mutationChance)
                {
                    float mutationOffset = Random.Range(-mutationIntensity, mutationIntensity) * inheritedValue;
                    inheritedValue += mutationOffset;

                    Debug.LogFormat("<color=purple>[Mutation Factor Alpha]</color> Random cell mutation triggered! Trait '{0}' shifted by {1:F2}", traitName, mutationOffset);
                }

                // Apply values within safe max limits
                inheritedValue = Mathf.Clamp(inheritedValue, 0, babyStat.GetTrait().MaxValue);
                babyStat.GetTrait().Value = inheritedValue;
            }
            else
            {
                Debug.LogWarningFormat("[Genetics Warning] Parent structures do not match! Trait mapping for '{0}' bypassed.", traitName);
            }
        }

        // 6. Visual Representation (Section B Brief Criteria)
        Stat visualTrait = baby.GetStat("Speed");
        if (visualTrait != null)
        {
            float visualModifier = visualTrait.GetTrait().Value / visualTrait.GetTrait().MaxValue;
            float calculatedScale = Mathf.Lerp(0.5f, 2.0f, visualModifier);
            baby.transform.localScale = new Vector3(calculatedScale, calculatedScale, calculatedScale);
            Debug.LogFormat("[Visual Engine] Morphing complete. Scaling local bounds array uniformly to factor: <b>{0:F2}x</b> based on 'Speed' profile value.", calculatedScale);
        }
        else
        {
            Debug.LogWarning("[Visual Engine] Could not locate a 'Speed' trait stat profile on the baby prefab template object to update layout scale constraints.");
        }

        // Sync NavMesh runtime variables to match genetic data
        UnityEngine.AI.NavMeshAgent babyAgent = baby.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (babyAgent != null && baby.GetStat("Speed") != null)
        {
            babyAgent.speed = baby.GetStat("Speed").GetTrait().Value;
        }

        // 7. Dynamic Naming Polish (Professional Touch)
        string uniqueName = string.Format("{0} Jr.", father.GetName());
        baby.gameObject.name = uniqueName;

        Debug.LogFormat("<color=green>[System Complete]</color> Birth sequence finalized! <b>{0}</b> successfully introduced to ecosystem room.", uniqueName);
    }
}
