using UnityEngine;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// NIGHTSHIFT — Endless Highway & AI Traffic Generator
/// Procedurally spawns infinite road tiles, mountain scenery, streetlights, billboards, and AI traffic.
/// </summary>
public class EndlessHighwayManager : MonoBehaviour
{
    public static EndlessHighwayManager Instance { get; private set; }

    [Header("Player Target")]
    public Transform playerCar;

    [Header("Chunk Settings")]
    public float chunkSize = 60f;
    public int initialChunkCount = 10;
    public float laneWidth = 4.5f;

    [Header("Materials")]
    public Material roadMaterial;
    public Material borderMaterial;
    public Material mountainMaterial;
    public Material streetlightMaterial;
    public Material trafficCarMaterial;

    [Header("UI References")]
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI bestDistanceText;

    // Internal State
    private List<GameObject> activeChunks = new List<GameObject>();
    private List<GameObject> activeTraffic = new List<GameObject>();
    private float spawnZ = 0f;
    private float distanceDriven = 0f;
    private float bestDistance = 0f;
    private int score = 0;
    private Vector3 initialPlayerPos;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        bestDistance = PlayerPrefs.GetFloat("NIGHTSHIFT_BestDistance", 0f);
    }

    void Start()
    {
        if (playerCar == null)
        {
            GameObject p = GameObject.Find("PlayerCar") ?? GameObject.FindWithTag("Player");
            if (p != null) playerCar = p.transform;
        }

        if (playerCar != null)
        {
            initialPlayerPos = playerCar.position;
        }

        CreateMaterials();
        InitializeRoad();
    }

    void CreateMaterials()
    {
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        // Disable screen-obscuring Unity fog & set clear crisp night ambient light
        RenderSettings.fog = false;
        RenderSettings.ambientLight = new Color(0.22f, 0.25f, 0.35f); // Crisp Night Ambient Light

        if (roadMaterial == null)
        {
            roadMaterial = new Material(litShader);
            roadMaterial.color = new Color(0.08f, 0.09f, 0.12f); // Dark wet asphalt
            roadMaterial.SetFloat("_Smoothness", 0.88f); // High reflection wet look
            roadMaterial.SetFloat("_Metallic", 0.35f);
        }

        if (borderMaterial == null)
        {
            borderMaterial = new Material(litShader);
            borderMaterial.color = new Color(0.95f, 0.2f, 0.1f); // Metallic Crimson guardrail
            borderMaterial.SetFloat("_Smoothness", 0.75f);
            borderMaterial.SetFloat("_Metallic", 0.8f);
        }

        if (mountainMaterial == null)
        {
            mountainMaterial = new Material(litShader);
            mountainMaterial.color = new Color(0.03f, 0.04f, 0.07f); // Dark synthwave mountain silhouette
            mountainMaterial.SetFloat("_Smoothness", 0.1f);
        }

        if (streetlightMaterial == null)
        {
            streetlightMaterial = new Material(litShader);
            streetlightMaterial.color = new Color(1f, 0.85f, 0.4f); // Warm gold glow
            streetlightMaterial.SetFloat("_Smoothness", 0.9f);
            streetlightMaterial.SetFloat("_Metallic", 0.9f);
        }

        if (trafficCarMaterial == null)
        {
            trafficCarMaterial = new Material(litShader);
            trafficCarMaterial.color = new Color(0.85f, 0.15f, 0.15f); // Red AI car
            trafficCarMaterial.SetFloat("_Smoothness", 0.85f);
            trafficCarMaterial.SetFloat("_Metallic", 0.5f);
        }
    }

    void InitializeRoad()
    {
        spawnZ = -60f; // Start behind player slightly
        for (int i = 0; i < initialChunkCount; i++)
        {
            SpawnChunk(i < 2); // First 2 chunks have no traffic
        }
    }

    void Update()
    {
        if (playerCar == null)
        {
            GameObject p = GameObject.Find("PlayerCar") ?? GameObject.FindWithTag("Player");
            if (p != null) playerCar = p.transform;
            return;
        }

        float playerZ = playerCar.position.z;

        // 1. Calculate distance driven & score
        if (playerZ > distanceDriven)
        {
            distanceDriven = playerZ;
            score = Mathf.FloorToInt(distanceDriven * 10f);

            if (distanceDriven > bestDistance)
            {
                bestDistance = distanceDriven;
                PlayerPrefs.SetFloat("NIGHTSHIFT_BestDistance", bestDistance);
            }
        }

        UpdateUI();

        // 2. Continuously spawn chunks up to 800 meters ahead of player
        while (spawnZ < playerZ + 800f)
        {
            SpawnChunk(false);
        }

        // 3. Clean up chunks that are safely > 150 meters BEHIND player
        while (activeChunks.Count > 0 && (activeChunks[0] == null || activeChunks[0].transform.position.z + chunkSize < playerZ - 150f))
        {
            if (activeChunks[0] != null)
            {
                Destroy(activeChunks[0]);
            }
            activeChunks.RemoveAt(0);
        }

        // 4. Update traffic positions
        UpdateTraffic();
    }

    void UpdateUI()
    {
        if (distanceText != null)
        {
            distanceText.text = $"{Mathf.FloorToInt(distanceDriven)} M";
        }

        if (scoreText != null)
        {
            scoreText.text = $"{score} PTS";
        }

        if (bestDistanceText != null)
        {
            bestDistanceText.text = $"BEST: {Mathf.FloorToInt(bestDistance)} M";
        }
    }

    void SpawnChunk(bool isSafeZone)
    {
        GameObject chunk = new GameObject($"HighwayChunk_{spawnZ}");
        chunk.transform.SetParent(transform);
        chunk.transform.position = new Vector3(0f, 0f, spawnZ);

        // 1. Off-Road Solid Ground Floor (120m wide - guarantees car NEVER falls into empty space)
        GameObject offRoadGround = GameObject.CreatePrimitive(PrimitiveType.Cube);
        offRoadGround.name = "OffRoadTerrainFloor";
        offRoadGround.transform.SetParent(chunk.transform);
        offRoadGround.transform.localPosition = new Vector3(0f, -0.3f, chunkSize * 0.5f);
        offRoadGround.transform.localScale = new Vector3(120f, 0.4f, chunkSize);
        offRoadGround.GetComponent<MeshRenderer>().sharedMaterial = mountainMaterial;

        // 2. Asphalt Road Segment (15m wide x 60m long)
        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "Road";
        road.transform.SetParent(chunk.transform);
        road.transform.localPosition = new Vector3(0f, -0.05f, chunkSize * 0.5f);
        road.transform.localScale = new Vector3(15f, 0.2f, chunkSize);
        road.GetComponent<MeshRenderer>().sharedMaterial = roadMaterial;

        // 3. Thick Left Guardrail Barrier (Bounces car back onto track)
        GameObject railL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        railL.name = "Guardrail_L";
        railL.transform.SetParent(chunk.transform);
        railL.transform.localPosition = new Vector3(-7.6f, 1.2f, chunkSize * 0.5f);
        railL.transform.localScale = new Vector3(1.0f, 2.4f, chunkSize);
        railL.GetComponent<MeshRenderer>().sharedMaterial = borderMaterial;

        // 4. Thick Right Guardrail Barrier (Bounces car back onto track)
        GameObject railR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        railR.name = "Guardrail_R";
        railR.transform.SetParent(chunk.transform);
        railR.transform.localPosition = new Vector3(7.6f, 1.2f, chunkSize * 0.5f);
        railR.transform.localScale = new Vector3(1.0f, 2.4f, chunkSize);
        railR.GetComponent<MeshRenderer>().sharedMaterial = borderMaterial;

        // 4. Streetlights & Cat-Eye Reflectors (Every 15-30 meters)
        Material catEyeMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
        catEyeMat.color = new Color(1f, 0.7f, 0f); // Glowing Amber Reflector

        for (float z = 10f; z < chunkSize; z += 15f)
        {
            // Cat-Eye Reflectors
            GameObject catL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            catL.transform.SetParent(chunk.transform);
            catL.transform.localPosition = new Vector3(-7.2f, 0.1f, z);
            catL.transform.localScale = new Vector3(0.2f, 0.15f, 0.2f);
            catL.GetComponent<MeshRenderer>().sharedMaterial = catEyeMat;

            GameObject catR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            catR.transform.SetParent(chunk.transform);
            catR.transform.localPosition = new Vector3(7.2f, 0.1f, z);
            catR.transform.localScale = new Vector3(0.2f, 0.15f, 0.2f);
            catR.GetComponent<MeshRenderer>().sharedMaterial = catEyeMat;

            if (z % 30f == 0)
            {
                SpawnStreetlight(chunk.transform, new Vector3(-8.5f, 0f, z), true);
                SpawnStreetlight(chunk.transform, new Vector3(8.5f, 0f, z + 15f), false);
            }
        }

        // Billboard
        if (Random.value < 0.2f)
        {
            GameObject bill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bill.transform.SetParent(chunk.transform);
            bill.transform.localPosition = new Vector3(10f, 4f, 30f);
            bill.transform.localScale = new Vector3(0.5f, 6f, 10f);
        }

        // 5. Scenery: Mountains & Trees
        SpawnMountainScenery(chunk.transform);

        // 6. AI Traffic Cars
        if (!isSafeZone && Random.value < 0.75f)
        {
            SpawnTrafficCar(spawnZ);
        }

        activeChunks.Add(chunk);
        spawnZ += chunkSize;
    }

    void SpawnStreetlight(Transform parent, Vector3 localPos, bool isLeft)
    {
        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "Streetlight";
        pole.transform.SetParent(parent);
        pole.transform.localPosition = localPos + Vector3.up * 3f;
        pole.transform.localScale = new Vector3(0.3f, 3f, 0.3f);
        pole.GetComponent<MeshRenderer>().sharedMaterial = streetlightMaterial;

        // Point Light
        GameObject lightObj = new GameObject("LightPool");
        lightObj.transform.SetParent(pole.transform);
        lightObj.transform.localPosition = new Vector3(isLeft ? 1.5f : -1.5f, 1f, 0f);

        Light l = lightObj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.85f, 0.5f);
        l.intensity = 3.5f;
        l.range = 18f;
    }

    void SpawnMountainScenery(Transform chunkTransform)
    {
        // Left Mountain Silhouette
        GameObject mtnL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        mtnL.name = "Mountain_L";
        mtnL.transform.SetParent(chunkTransform);
        float scaleL = Random.Range(30f, 60f);
        mtnL.transform.localPosition = new Vector3(-45f, scaleL * 0.5f - 5f, chunkSize * 0.5f);
        mtnL.transform.localScale = new Vector3(scaleL, scaleL, scaleL);
        mtnL.GetComponent<MeshRenderer>().sharedMaterial = mountainMaterial;

        // Right Mountain Silhouette
        GameObject mtnR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        mtnR.name = "Mountain_R";
        mtnR.transform.SetParent(chunkTransform);
        float scaleR = Random.Range(35f, 65f);
        mtnR.transform.localPosition = new Vector3(45f, scaleR * 0.5f - 5f, chunkSize * 0.5f);
        mtnR.transform.localScale = new Vector3(scaleR, scaleR, scaleR);
        mtnR.GetComponent<MeshRenderer>().sharedMaterial = mountainMaterial;
    }

    void SpawnTrafficCar(float chunkZ)
    {
        int lane = Random.Range(-1, 2); // -1 (Left), 0 (Center), 1 (Right)
        float posX = lane * laneWidth;
        float posZ = chunkZ + Random.Range(10f, 50f);

        GameObject traffic = GameObject.CreatePrimitive(PrimitiveType.Cube);
        traffic.name = "AITrafficCar";
        traffic.transform.position = new Vector3(posX, 0.7f, posZ);
        traffic.transform.localScale = new Vector3(2.1f, 1.2f, 4.4f);

        // Random body color
        Color[] carColors = new Color[]
        {
            new Color(0.8f, 0.1f, 0.1f), // Crimson
            new Color(0.1f, 0.4f, 0.8f), // Blue
            new Color(0.9f, 0.7f, 0.1f), // Yellow
            new Color(0.2f, 0.2f, 0.2f), // Black
            new Color(0.8f, 0.8f, 0.8f)  // Silver
        };
        Material carMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        carMat.color = carColors[Random.Range(0, carColors.Length)];
        traffic.GetComponent<MeshRenderer>().sharedMaterial = carMat;

        // Add BoxCollider & Rigidbody
        Rigidbody rb = traffic.AddComponent<Rigidbody>();
        rb.isKinematic = true; // AI moves programmatically

        // Tail Lights
        GameObject tailLight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tailLight.transform.SetParent(traffic.transform);
        tailLight.transform.localPosition = new Vector3(0f, 0.1f, -0.51f);
        tailLight.transform.localScale = new Vector3(0.8f, 0.2f, 0.1f);
        Material redGlow = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
        redGlow.color = Color.red;
        tailLight.GetComponent<MeshRenderer>().sharedMaterial = redGlow;

        activeTraffic.Add(traffic);
    }

    void UpdateTraffic()
    {
        for (int i = activeTraffic.Count - 1; i >= 0; i--)
        {
            GameObject car = activeTraffic[i];
            if (car == null)
            {
                activeTraffic.RemoveAt(i);
                continue;
            }

            // Move traffic forward slowly (or oncoming)
            car.transform.Translate(Vector3.forward * 12f * Time.deltaTime, Space.World);

            // Destroy traffic far behind player
            if (playerCar != null && car.transform.position.z < playerCar.position.z - 40f)
            {
                Destroy(car);
                activeTraffic.RemoveAt(i);
            }
        }
    }

    void RemoveOldChunk()
    {
        if (activeChunks.Count > 0)
        {
            GameObject old = activeChunks[0];
            activeChunks.RemoveAt(0);
            Destroy(old);
        }
    }
}
