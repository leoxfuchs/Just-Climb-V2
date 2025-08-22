using UnityEngine;
using UnityEditor;

public class SceneBuilder : EditorWindow
{
    [MenuItem("Tools/Build Scene")]
    public static void BuildDemoScene()
    {
        // Clear existing objects (optional)
        if (EditorUtility.DisplayDialog("Build Demo Scene", 
            "This will create a demo climbing scene. Continue?", "Yes", "No"))
        {
            CreateDemoScene();
        }
    }
    
    static void CreateDemoScene()
    {
        // Generate Mountain
        MountainGenerator.GenerateMountainNow();
        
        // Create Player at base of mountain
        GameObject player = CreatePlayer();
        player.transform.position = new Vector3(20f, 5f, 20f); // Position at mountain base
        
        // Create Camera
        SetupCamera(player);
        
        // Add one test ledge
        CreateTestLedge();
        
        // Create Lighting
        SetupLighting();
        
        Debug.Log("Mountain scene created successfully!");
        EditorUtility.DisplayDialog("Scene Built", 
            "Mountain climbing scene has been created!\n\nControls:\n- WASD: Move player\n- Mouse: Look around\n- Left Click + Drag: Move left hand\n- Right Click + Drag: Move right hand\n- Grab ledges to climb!", 
            "OK");
    }
    
    static void CreateTestLedge()
    {
        GameObject ledge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ledge.name = "Test_Ledge";
        ledge.tag = "Ledge";
        ledge.transform.position = new Vector3(25f, 8f, 25f);
        ledge.transform.localScale = new Vector3(3f, 0.5f, 1.5f);
        
        Renderer renderer = ledge.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = new Color(1f, 0f, 0f); // Bright red
        renderer.material = mat;
    }
    
    static GameObject CreatePlayer()
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0, 1, 0);
        
        // Add required components
        player.AddComponent<PlayerController>();
        player.AddComponent<HandController>();
        player.AddComponent<ClimbingSystem>();
        player.AddComponent<PlayerTransparency>();
        player.AddComponent<FirstPersonCamera>();
        
        // Setup Rigidbody
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = player.AddComponent<Rigidbody>();
        }
        rb.mass = 1f;
        rb.linearDamping = 1f;
        rb.angularDamping = 5f;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        
        // Set player color
        Renderer renderer = player.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(0.2f, 0.3f, 0.8f);
        }
        
        return player;
    }
    
    static void SetupCamera(GameObject player)
    {
        // Find or create main camera
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            mainCamera = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }
        
        // Position camera - will be controlled by PlayerController
        mainCamera.transform.position = player.transform.position + Vector3.up * 1.6f;
        mainCamera.transform.rotation = Quaternion.identity;
        
        // DO NOT add CameraFollow - it conflicts with mouse look!
    }
    
    static void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0, -0.5f, 0);
        ground.transform.localScale = new Vector3(30, 1, 30);
        
        Renderer renderer = ground.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(0.4f, 0.6f, 0.3f);
        }
    }
    
    static void CreateClimbingWall()
    {
        // Create main wall
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "ClimbingWall";
        wall.transform.position = new Vector3(0, 5, 5);
        wall.transform.localScale = new Vector3(10, 10, 0.5f);
        
        Renderer wallRenderer = wall.GetComponent<Renderer>();
        if (wallRenderer != null)
        {
            wallRenderer.material.color = new Color(0.6f, 0.5f, 0.4f);
        }
        
        // Create ledges
        CreateLedgesOnWall();
    }
    
    static void CreateLedgesOnWall()
    {
        // Create a parent object for ledges
        GameObject ledgeParent = new GameObject("Ledges");
        
        // Define ledge positions for a climbing path
        Vector3[,] ledgePositions = new Vector3[,]
        {
            // Lower ledges
            { new Vector3(-2, 1.5f, 4.5f), new Vector3(0.8f, 0.3f, 0.8f) },
            { new Vector3(1.5f, 1.8f, 4.5f), new Vector3(0.8f, 0.3f, 0.8f) },
            { new Vector3(-1, 2.5f, 4.5f), new Vector3(0.8f, 0.3f, 0.8f) },
            { new Vector3(2, 3f, 4.5f), new Vector3(0.8f, 0.3f, 0.8f) },
            
            // Mid ledges
            { new Vector3(-2.5f, 4f, 4.5f), new Vector3(1f, 0.3f, 0.8f) },
            { new Vector3(0, 4.5f, 4.5f), new Vector3(0.8f, 0.3f, 0.8f) },
            { new Vector3(2.5f, 5f, 4.5f), new Vector3(0.8f, 0.3f, 0.8f) },
            { new Vector3(-1.5f, 5.5f, 4.5f), new Vector3(0.8f, 0.3f, 0.8f) },
            
            // Upper ledges
            { new Vector3(1, 6.5f, 4.5f), new Vector3(0.8f, 0.3f, 0.8f) },
            { new Vector3(-2, 7f, 4.5f), new Vector3(1.2f, 0.3f, 0.8f) },
            { new Vector3(2, 7.5f, 4.5f), new Vector3(0.8f, 0.3f, 0.8f) },
            { new Vector3(0, 8.5f, 4.5f), new Vector3(1.5f, 0.3f, 0.8f) },
        };
        
        for (int i = 0; i < ledgePositions.GetLength(0); i++)
        {
            GameObject ledge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ledge.name = "Ledge_" + (i + 1);
            ledge.tag = "Ledge";
            ledge.transform.position = ledgePositions[i, 0];
            ledge.transform.localScale = ledgePositions[i, 1];
            ledge.transform.parent = ledgeParent.transform;
            
            // Color the ledges
            Renderer renderer = ledge.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.9f, 0.3f, 0.1f);
            }
        }
        
        // Create some side ledges for variety
        for (int i = 0; i < 3; i++)
        {
            GameObject sideLedge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sideLedge.name = "SideLedge_" + (i + 1);
            sideLedge.tag = "Ledge";
            sideLedge.transform.position = new Vector3(-4f + i * 1.5f, 3f + i * 1.2f, 4.5f);
            sideLedge.transform.localScale = new Vector3(0.6f, 0.3f, 0.8f);
            sideLedge.transform.parent = ledgeParent.transform;
            
            Renderer renderer = sideLedge.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.9f, 0.3f, 0.1f);
            }
        }
    }
    
    static void SetupLighting()
    {
        // Find or create directional light
        Light[] lights = GameObject.FindObjectsByType<Light>(FindObjectsSortMode.None);
        Light directionalLight = null;
        
        foreach (Light light in lights)
        {
            if (light.type == LightType.Directional)
            {
                directionalLight = light;
                break;
            }
        }
        
        if (directionalLight == null)
        {
            GameObject lightObj = new GameObject("Directional Light");
            directionalLight = lightObj.AddComponent<Light>();
            directionalLight.type = LightType.Directional;
        }
        
        directionalLight.transform.rotation = Quaternion.Euler(45f, -30f, 0);
        directionalLight.intensity = 1.2f;
        directionalLight.color = new Color(1f, 0.95f, 0.8f);
        
        // Set ambient lighting
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.4f, 0.45f, 0.5f);
    }
}