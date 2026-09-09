using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class FixEventSystemsAndSetup
{
    [MenuItem("NIGHTSHIFT/Stop Play Mode")]
    public static void StopPlay()
    {
        EditorApplication.isPlaying = false;
    }

    [MenuItem("NIGHTSHIFT/Fix Scenes and Event Systems")]
    public static void FixAllScenes()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }

        string[] scenes = new string[]
        {
            "Assets/Scenes/MainMenuScene.unity",
            "Assets/Scenes/GarageScene.unity",
            "Assets/Scenes/RaceScene.unity"
        };

        foreach (string scenePath in scenes)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Debug.Log($"[NIGHTSHIFT Fixer] Processing scene: {scenePath}");

            // Find or create EventSystem
            EventSystem es = Object.FindAnyObjectByType<EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                es = esObj.AddComponent<EventSystem>();
                esObj.AddComponent<StandaloneInputModule>();
                Debug.Log($"[NIGHTSHIFT Fixer] Created EventSystem in {scenePath}");
            }

            // Fix MainMenuScene specific links
            if (scenePath.Contains("MainMenuScene"))
            {
                MainMenuController mmc = Object.FindAnyObjectByType<MainMenuController>();
                if (mmc != null)
                {
                    if (mmc.startRaceButton == null)
                    {
                        var btnObj = GameObject.Find("Btn_STARTRACE") ?? GameObject.Find("StartButton") ?? GameObject.Find("StartRaceButton");
                        if (btnObj != null) mmc.startRaceButton = btnObj.GetComponent<Button>();
                    }
                    if (mmc.garageButton == null)
                    {
                        var btnObj = GameObject.Find("Btn_GARAGE") ?? GameObject.Find("GarageButton");
                        if (btnObj != null) mmc.garageButton = btnObj.GetComponent<Button>();
                    }
                    if (mmc.quitButton == null)
                    {
                        var btnObj = GameObject.Find("Btn_QUIT") ?? GameObject.Find("QuitButton");
                        if (btnObj != null) mmc.quitButton = btnObj.GetComponent<Button>();
                    }
                    EditorUtility.SetDirty(mmc);
                }
            }

            EditorSceneManager.SaveScene(scene);
        }

        // Open MainMenuScene as active scene
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenuScene.unity", OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
        Debug.Log("[NIGHTSHIFT Fixer] All scenes checked, repaired, and saved successfully!");
    }

    [MenuItem("NIGHTSHIFT/Rebuild Race Track & Coordinates")]
    public static void RebuildTrack()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }

        string scenePath = "Assets/Scenes/RaceScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        Debug.Log("[NIGHTSHIFT Track Rebuilder] Rebuilding RaceScene coordinates...");

        // 1. Setup Environment Ground Plane (2000m x 2000m)
        GameObject ground = GameObject.Find("GroundTerrain");
        if (ground == null)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "GroundTerrain";
        }
        ground.transform.position = new Vector3(0f, -0.1f, 250f);
        ground.transform.localScale = new Vector3(200f, 1f, 200f);
        MeshRenderer groundRend = ground.GetComponent<MeshRenderer>();
        if (groundRend != null)
        {
            Material groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            groundMat.color = new Color(0.08f, 0.09f, 0.11f);
            groundRend.sharedMaterial = groundMat;
        }
        if (ground.GetComponent<Collider>() == null)
        {
            ground.AddComponent<BoxCollider>();
        }

        // 2. Setup Road Waypoints & Mesh Segments
        Vector3[] waypoints = new Vector3[]
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 60f),
            new Vector3(15f, 0f, 120f),
            new Vector3(25f, 0f, 180f),
            new Vector3(-10f, 0f, 260f),
            new Vector3(-20f, 0f, 330f),
            new Vector3(0f, 0f, 410f),
            new Vector3(0f, 0f, 500f)
        };

        GameObject trackParent = GameObject.Find("TrackParent");
        if (trackParent != null)
        {
            Object.DestroyImmediate(trackParent);
        }
        trackParent = new GameObject("TrackParent");

        Material roadMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        roadMat.color = new Color(0.12f, 0.12f, 0.15f);

        Material borderMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        borderMat.color = new Color(0.8f, 0.2f, 0.1f);

        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            Vector3 start = waypoints[i];
            Vector3 end = waypoints[i + 1];
            Vector3 dir = (end - start).normalized;
            float segLength = Vector3.Distance(start, end);
            Vector3 mid = (start + end) * 0.5f;
            Quaternion rot = Quaternion.LookRotation(dir);

            // Road Segment
            GameObject roadSeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roadSeg.name = $"RoadSegment_{i}";
            roadSeg.transform.SetParent(trackParent.transform);
            roadSeg.transform.position = mid + Vector3.down * 0.1f;
            roadSeg.transform.rotation = rot;
            roadSeg.transform.localScale = new Vector3(14f, 0.2f, segLength + 1f);
            roadSeg.GetComponent<MeshRenderer>().sharedMaterial = roadMat;

            // Left Guardrail
            GameObject railL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            railL.name = $"Guardrail_L_{i}";
            railL.transform.SetParent(trackParent.transform);
            railL.transform.position = mid + rot * new Vector3(-7.2f, 0.5f, 0f);
            railL.transform.rotation = rot;
            railL.transform.localScale = new Vector3(0.4f, 1f, segLength + 1f);
            railL.GetComponent<MeshRenderer>().sharedMaterial = borderMat;

            // Right Guardrail
            GameObject railR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            railR.name = $"Guardrail_R_{i}";
            railR.transform.SetParent(trackParent.transform);
            railR.transform.position = mid + rot * new Vector3(7.2f, 0.5f, 0f);
            railR.transform.rotation = rot;
            railR.transform.localScale = new Vector3(0.4f, 1f, segLength + 1f);
            railR.GetComponent<MeshRenderer>().sharedMaterial = borderMat;
        }

        // 3. Fix Car Position & Physics Spawn
        GameObject carObj = GameObject.Find("PlayerCar") ?? GameObject.Find("ShowcaseCar") ?? GameObject.FindWithTag("Player");
        if (carObj != null)
        {
            carObj.name = "PlayerCar";
            carObj.transform.position = new Vector3(0f, 0.6f, 0f);
            carObj.transform.rotation = Quaternion.identity;
            carObj.transform.localScale = Vector3.one;

            Rigidbody rb = carObj.GetComponent<Rigidbody>();
            if (rb == null) rb = carObj.AddComponent<Rigidbody>();
            rb.mass = 1400f;
            rb.centerOfMass = new Vector3(0f, -0.4f, 0f);
            rb.linearDamping = 0.05f;
            rb.angularDamping = 0.5f;

            CarController carCtrl = carObj.GetComponent<CarController>();
            if (carCtrl != null)
            {
                carCtrl.spawnPosition = new Vector3(0f, 0.6f, 0f);
                carCtrl.spawnRotation = Quaternion.identity;
            }
        }

        // 4. Fix Main Camera & CameraFollow
        GameObject camObj = GameObject.Find("Main Camera") ?? GameObject.FindWithTag("MainCamera");
        if (camObj != null)
        {
            camObj.transform.position = new Vector3(0f, 3f, -7f);
            camObj.transform.rotation = Quaternion.Euler(12f, 0f, 0f);

            CameraFollow camFollow = camObj.GetComponent<CameraFollow>();
            if (camFollow == null) camFollow = camObj.AddComponent<CameraFollow>();

            if (carObj != null)
            {
                camFollow.target = carObj.transform;
            }
            camFollow.distance = 6.5f;
            camFollow.height = 2.2f;
            camFollow.followSmoothing = 8f;
            camFollow.rotationSmoothing = 6f;
        }

        // 5. Fix Checkpoints System
        GameObject checkpointParent = GameObject.Find("CheckpointsParent");
        if (checkpointParent != null)
        {
            Object.DestroyImmediate(checkpointParent);
        }
        checkpointParent = new GameObject("CheckpointsParent");

        CheckpointSystem cpSys = checkpointParent.AddComponent<CheckpointSystem>();

        int[] checkpointIndices = new int[] { 1, 2, 3, 4, 5, 6 };
        System.Collections.Generic.List<CheckpointSystem.Checkpoint> cpList = new System.Collections.Generic.List<CheckpointSystem.Checkpoint>();

        for (int i = 0; i < checkpointIndices.Length; i++)
        {
            int idx = checkpointIndices[i];
            Vector3 pos = waypoints[idx];
            Vector3 prev = waypoints[idx - 1];
            Vector3 dir = (pos - prev).normalized;
            Quaternion rot = Quaternion.LookRotation(dir);

            GameObject cpObj = new GameObject($"Checkpoint_{i}");
            cpObj.transform.SetParent(checkpointParent.transform);
            cpObj.transform.position = pos + Vector3.up * 2f;
            cpObj.transform.rotation = rot;

            BoxCollider col = cpObj.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(16f, 6f, 3f);

            CheckpointTrigger trigger = cpObj.AddComponent<CheckpointTrigger>();
            trigger.checkpointIndex = i;

            bool isFinish = (i == checkpointIndices.Length - 1);
            cpList.Add(new CheckpointSystem.Checkpoint { transform = cpObj.transform, isPassed = false, isFinishLine = isFinish });

            // Visual Arch Posts
            GameObject archL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            archL.transform.SetParent(cpObj.transform);
            archL.transform.localPosition = new Vector3(-8f, 0f, 0f);
            archL.transform.localScale = new Vector3(0.5f, 3f, 0.5f);

            GameObject archR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            archR.transform.SetParent(cpObj.transform);
            archR.transform.localPosition = new Vector3(8f, 0f, 0f);
            archR.transform.localScale = new Vector3(0.5f, 3f, 0.5f);

            GameObject archTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            archTop.transform.SetParent(cpObj.transform);
            archTop.transform.localPosition = new Vector3(0f, 3f, 0f);
            archTop.transform.localScale = new Vector3(16.5f, 0.5f, 0.5f);

            Material archMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            archMat.color = isFinish ? new Color(0f, 0.9f, 0.4f) : new Color(0f, 0.7f, 1f);
            archL.GetComponent<MeshRenderer>().sharedMaterial = archMat;
            archR.GetComponent<MeshRenderer>().sharedMaterial = archMat;
            archTop.GetComponent<MeshRenderer>().sharedMaterial = archMat;
        }

        cpSys.checkpoints = cpList.ToArray();

        // 6. Fix RaceManager links
        RaceManager rm = Object.FindAnyObjectByType<RaceManager>();
        if (rm != null)
        {
            if (carObj != null) rm.playerCar = carObj.GetComponent<CarController>();
            rm.checkpointSystem = cpSys;
            HUDController hud = Object.FindAnyObjectByType<HUDController>();
            if (hud != null) rm.hud = hud;
            EditorUtility.SetDirty(rm);
        }

        // 7. Fix HUD Layout (Prevent Overlapping Text & Numbers)
        HUDController hudCtrl = Object.FindAnyObjectByType<HUDController>();
        if (hudCtrl != null)
        {
            if (hudCtrl.speedText != null)
            {
                RectTransform rt = hudCtrl.speedText.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector3(30f, 30f, 0f);
                    rt.sizeDelta = new Vector2(160f, 60f);
                }
                hudCtrl.speedText.alignment = TextAlignmentOptions.BottomLeft;
                hudCtrl.speedText.fontSize = 42;
            }

            if (hudCtrl.timerText != null)
            {
                RectTransform rt = hudCtrl.timerText.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector3(0f, -30f, 0f);
                    rt.sizeDelta = new Vector2(240f, 40f);
                }
                hudCtrl.timerText.alignment = TextAlignmentOptions.Center;
                hudCtrl.timerText.fontSize = 32;
            }

            if (hudCtrl.checkpointText != null)
            {
                RectTransform rt = hudCtrl.checkpointText.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector3(-30f, -30f, 0f);
                    rt.sizeDelta = new Vector2(180f, 40f);
                }
                hudCtrl.checkpointText.alignment = TextAlignmentOptions.Right;
                hudCtrl.checkpointText.fontSize = 28;
            }
        }

        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[NIGHTSHIFT Track Rebuilder] RaceScene rebuilt successfully with perfect 3D coordinates!");
    }
}
