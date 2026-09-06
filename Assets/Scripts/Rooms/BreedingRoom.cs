using UnityEngine;

public class BreedingRoom : Room
{
    [Header("Breeding Settings")]
    [SerializeField] private GameObject creaturePrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Mutation Configuration")]
    [Range(0f, 1f)]
    [SerializeField] private float mutationChance = 0.10f; // 10% chance to mutate
    [SerializeField] private float mutationIntensity = 0.25f; // Max variation up/down

    [Header("Visual Mutation Assets (Section B Criteria)")]
    [SerializeField] private Mesh rareMutatedMesh; // Drag your custom rare model variant here in Inspector

    private void Start()
    {
        Debug.LogFormat("<color=yellow>[BreedingRoom System Setup]</color> Active on <b>{0}</b>. " +
            "Right-Click this component heading in the Inspector and select 'FORCE TEST BREED' to test!", this.gameObject.name);
    }

    /// <summary>
    /// ContextMenu allows you to trigger this function directly from the Unity Editor Inspector window!
    /// </summary>
    [ContextMenu("FORCE TEST BREED")]
    public void TryBreedCreatures()
    {
        int currentCount = GetRoomCount();
        Debug.LogFormat("<color=orange>[Context Button Clicked]</color> Force-firing breeding routine. Studio engine reports <b>{0}</b> creature(s) in this room.", currentCount);

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

        // 2. Safely extract parent components out of the studio's collection loop
        Creature parentA = null;
        Creature parentB = null;
        int index = 0;

        foreach (Transform child in children)
        {
            if (index == 0) parentA = child.GetComponent<Creature>();
            if (index == 1) parentB = child.GetComponent<Creature>();
            index++;
        }

        if (parentA == null || parentB == null)
        {
            Debug.LogError("<color=red>[Component Error]</color> Found game objects in the room, but failed to retrieve the required 'Creature' script components!");
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

                // Pick a random midpoint between parent values
                float inheritedValue = Random.Range(Mathf.Min(fatherVal, motherVal), Mathf.Max(fatherVal, motherVal));
                Debug.LogFormat("[Genetics] Trait <b>{0}</b> -> Parent Mix Baseline Value computed as: {1:F2}", traitName, inheritedValue);

                // Mutation Algorithm (Section B Brief Criteria)
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
        }

        // 5. Visual Representation: Scale (Section B Brief Criteria)
        Stat visualTrait = baby.GetStat("Speed");
        if (visualTrait != null)
        {
            float visualModifier = visualTrait.GetTrait().Value / visualTrait.GetTrait().MaxValue;
            float calculatedScale = Mathf.Lerp(0.5f, 2.0f, visualModifier);
            baby.transform.localScale = new Vector3(calculatedScale, calculatedScale, calculatedScale);
            Debug.LogFormat("[Visual Engine] Morphing complete. Scaling local bounds array uniformly to factor: <b>{0:F2}x</b> based on 'Speed' profile value.", calculatedScale);
        }

        // 6. Visual Representation: Color & Mesh Mutation (Section B Brief Criteria)
        if (Random.value < mutationChance)
        {
            // A. COLOUR MUTATION: Apply a random, unique RGB value
            Renderer babyRenderer = baby.GetComponentInChildren<Renderer>();
            if (babyRenderer != null)
            {
                Color randomMutatedColor = new Color(Random.value, Random.value, Random.value);
                babyRenderer.material.color = randomMutatedColor;
                Debug.Log("<color=purple>[Visual Mutation - COLOUR]</color> Baby skin chemistry mutated to a totally unique rogue colour spectrum!");
            }

            // B. MESH MUTATION: Swap out the structural geometry template mesh data reference
            if (rareMutatedMesh != null)
            {
                MeshFilter babyMeshFilter = baby.GetComponentInChildren<MeshFilter>();
                if (babyMeshFilter != null)
                {
                    babyMeshFilter.sharedMesh = rareMutatedMesh;
                    Debug.Log("<color=purple>[Visual Mutation - MESH]</color> Rare anatomical skeletal variation triggered! Mesh geometric template altered.");
                }
            }
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
