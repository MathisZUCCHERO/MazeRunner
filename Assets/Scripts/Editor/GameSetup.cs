using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using UnityEngine.UI;
using System.IO;

public class GameSetup : EditorWindow
{
    [MenuItem("Maze Game/Setup Scene")]
    public static void Setup()
    {
        SetupStats("Beginning Scene Setup...");
        
        // 1. Create Prefabs Directory
        if (!Directory.Exists("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
            
        // 2. Create/Load Prefabs
        
        // Find User Materials
        Material brickMat = FindMaterial("Brick");
        Material dirtMat = FindMaterial("Dirt");

        GameObject wallPrefab = CreatePrefab("Wall", PrimitiveType.Cube, (go) => {
            go.transform.localScale = new Vector3(1, 1, 1);
            var obs = go.AddComponent<NavMeshObstacle>();
            obs.carving = true;
            if (brickMat) go.GetComponent<Renderer>().sharedMaterial = brickMat;
        });

        GameObject floorPrefab = CreatePrefab("FloorTile", PrimitiveType.Plane, (go) => {
            go.transform.localScale = new Vector3(0.4f, 1, 0.4f); 
            if (dirtMat) go.GetComponent<Renderer>().sharedMaterial = dirtMat;
        });
        
        GameObject playerPrefab = CreatePrefab("Player", PrimitiveType.Capsule, (go) => {
            go.tag = "Player";
            go.AddComponent<CharacterController>();
            go.AddComponent<PlayerController>();
            // Add Camera
            GameObject cam = new GameObject("Main Camera");
            cam.transform.parent = go.transform;
            cam.transform.localPosition = new Vector3(0, 0.6f, 0);
            cam.AddComponent<Camera>();
            cam.AddComponent<AudioListener>();
            cam.tag = "MainCamera";
            // Assign camera to script
            go.GetComponent<PlayerController>().playerCamera = cam.transform;

            // Add Minimap Camera (Disabled by default, enabled by pickup)
            GameObject mapCam = new GameObject("MinimapCamera");
            mapCam.transform.parent = go.transform;
            mapCam.transform.localPosition = new Vector3(0, 20f, 0);
            mapCam.transform.localEulerAngles = new Vector3(90f, 0, 0);
            Camera mc = mapCam.AddComponent<Camera>();
            mc.clearFlags = CameraClearFlags.Depth;
            mc.orthographic = true;
            mc.orthographicSize = 10f;
            mc.depth = 1; // Render on top of main camera
            // Top Right Corner
            mc.rect = new Rect(0.7f, 0.7f, 0.25f, 0.25f);
            mapCam.SetActive(false); // Start disabled
        });

        // Create Materials First
        Material redMat = GetOrCreateMaterial("MinotaurRed", Color.red);
        Material greenMat = GetOrCreateMaterial("EndGreen", Color.green);
        Material blueGlow = GetOrCreateMaterial("SpeedBlue", Color.blue, true);
        Material brownGlow = GetOrCreateMaterial("MapBrown", new Color(0.6f, 0.4f, 0.2f), true);
        Material minotaurMat = GetOrCreateMaterial("Minotaur", Color.red);

        AudioClip minotaurGrowlClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Minotaur_Growl.mp3");

        GameObject minotaurPrefab = CreatePrefab("Minotaur", PrimitiveType.Capsule, (go) => {
            // 1. Setup Root (Agent + AI)
            var agent = go.AddComponent<NavMeshAgent>();
            agent.speed = 20f; // ULTRA Speed
            agent.radius = 0.35f; 
            agent.height = 2.0f;
            agent.acceleration = 420f; // Instant response
            agent.angularSpeed = 400f; // Instant turning
            agent.autoBraking = false; 
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            
            go.AddComponent<MinotaurAI>();
            
            // Remove default visual (Cylinder/Capsule mesh) from root, keep Collider
            Object.DestroyImmediate(go.GetComponent<MeshFilter>());
            Object.DestroyImmediate(go.GetComponent<MeshRenderer>());
            
            // Adjust Root Collider
            var collider = go.GetComponent<CapsuleCollider>();
            if (collider)
            {
                collider.radius = 0.3f; // Slightly smaller than Agent Radius
                collider.height = 2.0f;
                collider.center = new Vector3(0, 1f, 0);
                collider.isTrigger = true; // Make it a trigger so it doesn't physically collide with walls (Agent handles that)
            }

            // 2. Load & Setup Visual Model (Use Running.fbx)
            string fbxPath = "Assets/Animations/Running.fbx";
            
            // STEP A: Ensure Import Settings (Loop Time) BEFORE Instantiation
            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer)
            {
                var clips = importer.defaultClipAnimations;
                bool changed = false;
                foreach (var clip in clips)
                {
                    if (!clip.loopTime)
                    {
                        clip.loopTime = true;
                        changed = true;
                    }
                }
                if (changed)
                {
                    importer.clipAnimations = clips;
                    importer.SaveAndReimport();
                    Debug.Log("Fixed Loop Time on Running.fbx");
                }
            }

            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (modelPrefab)
            {
                GameObject visual = Object.Instantiate(modelPrefab, go.transform);
                visual.name = "Visual";
                
                // Standard transforms
                visual.transform.localPosition = new Vector3(0, -0.55f, 0);
                visual.transform.localEulerAngles = Vector3.zero;
                visual.transform.localScale = new Vector3(2, 2, 2);

                // Apply Material (search for renderers)
                var renderers = visual.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers) r.sharedMaterial = minotaurMat;
                
                // SAFETY: Destroy any colliders on the visual model to prevent Agent interference
                var colliders = visual.GetComponentsInChildren<Collider>();
                foreach (var c in colliders) Object.DestroyImmediate(c);
                
                // 3. Setup Animation
                string controllerPath = "Assets/Animations/Minotaur.controller";
                RuntimeAnimatorController runtimeController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
                UnityEditor.Animations.AnimatorController ac = null;
                if (runtimeController) ac = runtimeController as UnityEditor.Animations.AnimatorController;
                
                if (!ac)
                {
                    if (!Directory.Exists("Assets/Animations")) AssetDatabase.CreateFolder("Assets", "Animations");
                    ac = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                }

                Animator anim = visual.GetComponent<Animator>();
                if (!anim) anim = visual.AddComponent<Animator>();
                anim.applyRootMotion = false; 
                anim.runtimeAnimatorController = ac;

                if (ac)
                {
                    AnimationClip runningClip = null;
                    Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
                    foreach (Object obj in assets)
                    {
                        if (obj is AnimationClip clip && !obj.name.Contains("__preview__")) 
                        {
                            runningClip = clip;
                            break;
                        }
                    }

                    if (runningClip)
                    {
                        // Clean Layers
                        while (ac.layers.Length > 0) ac.RemoveLayer(0);
                        ac.AddLayer("Base Layer");
                        
                        var layer = ac.layers[0];
                        var sm = layer.stateMachine;
                        var defaultState = sm.AddState("Running");
                        defaultState.motion = runningClip;
                        sm.defaultState = defaultState;
                        
                        EditorUtility.SetDirty(ac);
                    }
                }
            }
            else
            {
                Debug.LogError($"Minotaur FBX not found at {fbxPath}");
                GameObject debugVis = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                debugVis.transform.parent = go.transform;
                debugVis.transform.localPosition = new Vector3(0, 1, 0);
            }
            
            // 4. Audio
            var audioSource = go.AddComponent<AudioSource>();
            audioSource.clip = minotaurGrowlClip;
            audioSource.spatialBlend = 1f;
            audioSource.playOnAwake = true;
            audioSource.loop = true;
            audioSource.volume = 0.7f;
        });
        
        GameObject endPrefab = CreatePrefab("EndTrigger", PrimitiveType.Cube, (go) => {
            go.GetComponent<BoxCollider>().isTrigger = true;
            go.AddComponent<EndTrigger>();
            go.GetComponent<Renderer>().sharedMaterial = greenMat;
        });
        
        GameObject speedPrefab = CreatePrefab("SpeedBoost", PrimitiveType.Sphere, (go) => {
            go.GetComponent<SphereCollider>().isTrigger = true;
            go.AddComponent<SpeedBoost>();
            go.GetComponent<Renderer>().sharedMaterial = blueGlow;
        });

        GameObject minimapPrefab = CreatePrefab("MinimapPickup", PrimitiveType.Cube, (go) => {
            go.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            go.GetComponent<BoxCollider>().isTrigger = true;
            go.AddComponent<MinimapPickup>();
            go.GetComponent<Renderer>().sharedMaterial = brownGlow;
        });

        // 3. Setup Scene Objects
        // GameManager
        GameObject gm = GameObject.Find("GameManager");
        if (!gm) gm = new GameObject("GameManager");
        if (!gm.GetComponent<GameManager>()) gm.AddComponent<GameManager>();

        // MazeGenerator
        GameObject mg = GameObject.Find("MazeGenerator");
        if (!mg) mg = new GameObject("MazeGenerator");
        MazeGenerator mgScript = mg.GetComponent<MazeGenerator>();
        if (!mgScript) mgScript = mg.AddComponent<MazeGenerator>();
        
        // Add NavMeshSurface for runtime baking
        var surface = mg.GetComponent<Unity.AI.Navigation.NavMeshSurface>();
        if (!surface) surface = mg.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
        surface.collectObjects = Unity.AI.Navigation.CollectObjects.Children;
        
        // Assign Prefabs
        mgScript.wallPrefab = wallPrefab;
        mgScript.floorPrefab = floorPrefab;
        mgScript.playerPrefab = playerPrefab;
        mgScript.minotaurPrefab = minotaurPrefab;
        mgScript.endTriggerPrefab = endPrefab;
        mgScript.speedBoostPrefab = speedPrefab;
        mgScript.minimapPrefab = minimapPrefab;
        mgScript.minimapPrefab = minimapPrefab;
        mgScript.cellSize = 4f;
        mgScript.width = 40;
        mgScript.height = 40;
        mgScript.wallHeight = 10.0f;
        mgScript.speedBoostChance = 0.01f;
        mgScript.minimapCount = 1;

        // UI
        GameObject canvasGO = GameObject.Find("Canvas");
        if (!canvasGO)
        {
            canvasGO = new GameObject("Canvas");
            canvasGO.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        
        // HUD - Timer
        GameObject timerTxt = GameObject.Find("TimerText");
        if (!timerTxt)
        {
            timerTxt = new GameObject("TimerText");
            timerTxt.transform.SetParent(canvasGO.transform, false);
            Text t = timerTxt.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.alignment = TextAnchor.UpperCenter;
            t.color = Color.white;
            t.fontSize = 24;
            t.rectTransform.anchorMin = new Vector2(0.5f, 1);
            t.rectTransform.anchorMax = new Vector2(0.5f, 1);
            t.rectTransform.anchoredPosition = new Vector2(0, -30);
            t.rectTransform.sizeDelta = new Vector2(300, 50);
            t.text = "TIME: 0.00";
        }

        // Leaderboard / End Screen
        GameObject lbGO = GameObject.Find("LeaderboardUI");
        if (lbGO) DestroyImmediate(lbGO); // FORCE REPLAcEMENT to fix stale UI state
        
        lbGO = new GameObject("LeaderboardUI");
        lbGO.transform.SetParent(canvasGO.transform, false);
        LeaderboardUI lbScript = lbGO.AddComponent<LeaderboardUI>();
            
        // 1. Create Panel (Blur background mock)
            GameObject panel = new GameObject("EndScreenPanel");
            panel.transform.SetParent(lbGO.transform, false);
            Image img = panel.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.85f); // Dark semi-transparent
            panel.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            panel.GetComponent<RectTransform>().anchorMax = Vector2.one;
            panel.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            panel.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            
            // 2. Text
            GameObject txtObj = new GameObject("ResultText");
            txtObj.transform.SetParent(panel.transform, false);
            Text lbText = txtObj.AddComponent<Text>();
            lbText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lbText.alignment = TextAnchor.MiddleCenter;
            lbText.color = Color.yellow;
            lbText.fontSize = 20;
            lbText.rectTransform.anchoredPosition = new Vector2(0, 50);
            lbText.rectTransform.sizeDelta = new Vector2(400, 300);
            lbText.text = "LEADERBOARD";
            
            // 3. Replay Button
            GameObject btnObj = new GameObject("ReplayButton");
            btnObj.transform.SetParent(panel.transform, false);
            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = Color.white;
            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btnObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -150);
            btnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 50);
            
            GameObject btnTxtObj = new GameObject("Text");
            btnTxtObj.transform.SetParent(btnObj.transform, false);
            Text btnTxt = btnTxtObj.AddComponent<Text>();
            btnTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnTxt.text = "REPLAY";
            btnTxt.alignment = TextAnchor.MiddleCenter;
            btnTxt.color = Color.black;
            btnTxtObj.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            btnTxtObj.GetComponent<RectTransform>().anchorMax = Vector2.one;
            btnTxtObj.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            btnTxtObj.GetComponent<RectTransform>().offsetMax = Vector2.zero;

            // Assign references
            lbScript.endScreenPanel = panel;
            lbScript.leaderboardText = lbText;
            lbScript.replayButton = btn;
            
            // Default hidden is handled by Awake, but let's hide here too to be clear in editor
            // panel.SetActive(false); // Can't disable here easily because script Awake handles it better
        
        
        // EventSystem
        if (!GameObject.Find("EventSystem"))
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        SetupStats("Scene Setup Complete! Press Play.");
    }

    private static GameObject CreatePrefab(string name, PrimitiveType type, System.Action<GameObject> setup)
    {
        string path = $"Assets/Prefabs/{name}.prefab";
        
        GameObject go = GameObject.CreatePrimitive(type);
        setup(go);
        
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go);
        return prefab;
    }

    private static void SetupStats(string msg)
    {
        Debug.Log($"[MazeSetup] {msg}");
    }

    private static Material FindMaterial(string name)
    {
        string[] guids = AssetDatabase.FindAssets($"{name} t:Material");
        if (guids.Length > 0)
        {
            return AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        return null; // Should fall back or error, or user can assign manually
    }
    
    // ... existing CreatePrefab method ...
    
    private static Material GetOrCreateMaterial(string name, Color color, bool emissive = false)
    {
        string matPath = $"Assets/Materials/{name}.mat";
        if (!Directory.Exists("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (!mat)
        {
            mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            if (emissive)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 2f);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            AssetDatabase.CreateAsset(mat, matPath);
        }
        return mat;
    }

    [MenuItem("Maze Game/Clear Leaderboard Data")]
    public static void ClearData()
    {
        PlayerPrefs.DeleteKey("MazeLeaderboard");
        PlayerPrefs.Save();
        Debug.Log("Leaderboard Data Cleared!");
    }
}
