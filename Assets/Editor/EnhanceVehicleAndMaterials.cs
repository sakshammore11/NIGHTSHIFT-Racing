using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class EnhanceVehicleAndMaterials
{
    [MenuItem("NIGHTSHIFT/Enhance Vehicle & Materials")]
    public static void EnhanceAll()
    {
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");

        // 1. Upgrade Car Body Material (Deep Metallic Obsidian / Midnight Blue)
        Material carBodyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Mat_CarBody.mat");
        if (carBodyMat != null)
        {
            carBodyMat.shader = litShader;
            carBodyMat.color = new Color(0.05f, 0.08f, 0.18f, 1f); // Deep Midnight Blue
            if (carBodyMat.HasProperty("_Metallic")) carBodyMat.SetFloat("_Metallic", 0.88f);
            if (carBodyMat.HasProperty("_Smoothness")) carBodyMat.SetFloat("_Smoothness", 0.92f);
            EditorUtility.SetDirty(carBodyMat);
        }

        // 2. Upgrade Car Glass Material (Tinted Crystal Clear)
        Material glassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Mat_CarGlass.mat");
        if (glassMat != null)
        {
            glassMat.shader = litShader;
            glassMat.color = new Color(0.1f, 0.15f, 0.25f, 0.4f);
            if (glassMat.HasProperty("_Surface")) glassMat.SetFloat("_Surface", 1f); // Transparent
            if (glassMat.HasProperty("_Blend")) glassMat.SetFloat("_Blend", 0f); // Alpha blend
            if (glassMat.HasProperty("_Metallic")) glassMat.SetFloat("_Metallic", 0.3f);
            if (glassMat.HasProperty("_Smoothness")) glassMat.SetFloat("_Smoothness", 0.98f);
            EditorUtility.SetDirty(glassMat);
        }

        // 3. Upgrade Headlight Material (Glowing Xenon Cyan/White)
        Material headlightMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Mat_Headlight.mat");
        if (headlightMat != null)
        {
            headlightMat.shader = unlitShader;
            headlightMat.color = new Color(0.4f, 0.9f, 1f, 1f); // Bright Xenon Cyan
            EditorUtility.SetDirty(headlightMat);
        }

        // 4. Upgrade Taillight Material (Glowing Crimson Red)
        Material taillightMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Mat_Taillight.mat");
        if (taillightMat != null)
        {
            taillightMat.shader = unlitShader;
            taillightMat.color = new Color(1f, 0.05f, 0.1f, 1f); // Glowing Crimson Red
            EditorUtility.SetDirty(taillightMat);
        }

        // 5. Upgrade Rim Material (Polished Chrome Alloy)
        Material rimMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Mat_Rim.mat");
        if (rimMat != null)
        {
            rimMat.shader = litShader;
            rimMat.color = new Color(0.85f, 0.88f, 0.92f, 1f); // Polished Chrome
            if (rimMat.HasProperty("_Metallic")) rimMat.SetFloat("_Metallic", 0.95f);
            if (rimMat.HasProperty("_Smoothness")) rimMat.SetFloat("_Smoothness", 0.90f);
            EditorUtility.SetDirty(rimMat);
        }

        // 6. Upgrade Tire Material (Matte Rubber)
        Material tireMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Mat_Tire.mat");
        if (tireMat != null)
        {
            tireMat.shader = litShader;
            tireMat.color = new Color(0.1f, 0.1f, 0.12f, 1f); // Matte Black Rubber
            if (tireMat.HasProperty("_Metallic")) tireMat.SetFloat("_Metallic", 0.05f);
            if (tireMat.HasProperty("_Smoothness")) tireMat.SetFloat("_Smoothness", 0.25f);
            EditorUtility.SetDirty(tireMat);
        }

        // 7. Process Scene Vehicles: Add Spotlights & Taillights
        string[] scenes = new string[]
        {
            "Assets/Scenes/MainMenuScene.unity",
            "Assets/Scenes/RaceScene.unity",
            "Assets/Scenes/GarageScene.unity"
        };

        foreach (string scenePath in scenes)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject car = GameObject.Find("PlayerCar") ?? GameObject.Find("ShowcaseCar") ?? GameObject.FindWithTag("Player");

            if (car != null)
            {
                // Headlight Spotlights
                Transform hl = car.transform.Find("VisualModel/Headlight_L") ?? car.transform.Find("Headlight_L");
                Transform hr = car.transform.Find("VisualModel/Headlight_R") ?? car.transform.Find("Headlight_R");

                if (hl != null && hl.GetComponentInChildren<Light>() == null)
                {
                    GameObject spotL = new GameObject("SpotLight_L");
                    spotL.transform.SetParent(hl);
                    spotL.transform.localPosition = Vector3.zero;
                    spotL.transform.localRotation = Quaternion.identity;
                    Light l = spotL.AddComponent<Light>();
                    l.type = LightType.Spot;
                    l.color = new Color(0.6f, 0.9f, 1f); // Xenon Cyan
                    l.intensity = 5f;
                    l.range = 45f;
                    l.spotAngle = 60f;
                }

                if (hr != null && hr.GetComponentInChildren<Light>() == null)
                {
                    GameObject spotR = new GameObject("SpotLight_R");
                    spotR.transform.SetParent(hr);
                    spotR.transform.localPosition = Vector3.zero;
                    spotR.transform.localRotation = Quaternion.identity;
                    Light l = spotR.AddComponent<Light>();
                    l.type = LightType.Spot;
                    l.color = new Color(0.6f, 0.9f, 1f);
                    l.intensity = 5f;
                    l.range = 45f;
                    l.spotAngle = 60f;
                }

                // Taillight Point Lights
                Transform tl = car.transform.Find("VisualModel/Taillight_L") ?? car.transform.Find("Taillight_L");
                Transform tr = car.transform.Find("VisualModel/Taillight_R") ?? car.transform.Find("Taillight_R");

                if (tl != null && tl.GetComponentInChildren<Light>() == null)
                {
                    GameObject pointL = new GameObject("TailLight_L");
                    pointL.transform.SetParent(tl);
                    pointL.transform.localPosition = Vector3.zero;
                    Light l = pointL.AddComponent<Light>();
                    l.type = LightType.Point;
                    l.color = new Color(1f, 0.1f, 0.15f);
                    l.intensity = 2.5f;
                    l.range = 8f;
                }

                if (tr != null && tr.GetComponentInChildren<Light>() == null)
                {
                    GameObject pointR = new GameObject("TailLight_R");
                    pointR.transform.SetParent(tr);
                    pointR.transform.localPosition = Vector3.zero;
                    Light l = pointR.AddComponent<Light>();
                    l.type = LightType.Point;
                    l.color = new Color(1f, 0.1f, 0.15f);
                    l.intensity = 2.5f;
                    l.range = 8f;
                }

                EditorUtility.SetDirty(car);
            }

            EditorSceneManager.SaveScene(scene);
        }

        // Save MainMenu as active scene
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenuScene.unity", OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
        Debug.Log("[NIGHTSHIFT Upgrade] Vehicle materials & headlight spotlights enhanced successfully!");
    }
}
