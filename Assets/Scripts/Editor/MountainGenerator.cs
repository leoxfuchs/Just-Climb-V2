using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MountainGenerator
{
    [MenuItem("Tools/Mountain Generator")]
    public static void GenerateMountainNow()
    {
        ClearMountain();
        
        // Create the terrain
        TerrainData terrainData = GenerateTerrainData();
        GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
        terrainObject.name = "Mountain";
        
        // Add climbing elements
        GenerateClimbingFeatures(terrainData, terrainObject.transform);
        PaintGravelPath(terrainData);
        
        Selection.activeGameObject = terrainObject;
        
        EditorUtility.DisplayDialog("Mountain Generated", 
            "Realistic mountain terrain with forests and climbing ledges created!", "OK");
    }
    
    static TerrainData GenerateTerrainData()
    {
        TerrainData terrainData = new TerrainData();
        
        // Set terrain dimensions
        int heightmapResolution = 513; // Must be power of 2 + 1
        int alphamapResolution = 512;
        terrainData.heightmapResolution = heightmapResolution;
        terrainData.alphamapResolution = alphamapResolution;
        terrainData.size = new Vector3(800, 400, 800); // Tall climbable mountain
        
        // Generate heightmap
        float[,] heights = GenerateHeightmap(heightmapResolution);
        terrainData.SetHeights(0, 0, heights);
        
        // Create and assign terrain textures
        SetupTerrainTextures(terrainData);
        
        // Paint terrain with different textures based on height and slope
        PaintTerrain(terrainData, heights);
        
        // Add trees for forests
        PlaceTrees(terrainData, heights);
        
        return terrainData;
    }
    
    static float[,] GenerateHeightmap(int resolution)
    {
        float[,] heights = new float[resolution, resolution];
        
        float centerX = resolution * 0.5f;
        float centerZ = resolution * 0.5f;
        float maxDistance = resolution * 0.45f;
        
        // Generate base mountain shape first
        for (int x = 0; x < resolution; x++)
        {
            for (int z = 0; z < resolution; z++)
            {
                // Distance from center for mountain shape
                float distanceFromCenter = Vector2.Distance(new Vector2(x, z), new Vector2(centerX, centerZ));
                float mountainFalloff = Mathf.Clamp01(1f - (distanceFromCenter / maxDistance));
                
                // Create STEEP CLIMBABLE WALLS like PEAK
                float height = 0f;
                float amplitude = 1f;
                float frequency = 0.008f; // Larger features
                
                // Large scale mountain structure
                height += Mathf.PerlinNoise(x * frequency, z * frequency) * amplitude;
                
                // Create cliff faces and vertical walls
                frequency = 0.02f;
                amplitude = 0.6f;
                float cliffNoise = Mathf.PerlinNoise(x * frequency + 5000, z * frequency + 5000);
                if (cliffNoise > 0.6f) // Create steep walls
                {
                    height += (cliffNoise - 0.6f) * amplitude * 3f; // Sharp vertical increase
                }
                
                // Add rock bulges and ledges directly into terrain
                frequency = 0.05f;
                amplitude = 0.3f;
                float bulgeNoise = Mathf.PerlinNoise(x * frequency + 10000, z * frequency + 10000);
                if (bulgeNoise > 0.7f)
                {
                    // Create protruding rock formations
                    height += (bulgeNoise - 0.7f) * amplitude * 4f;
                }
                
                // Create horizontal ledges for resting
                frequency = 0.03f;
                float ledgeNoise = Mathf.PerlinNoise(x * frequency + 15000, z * frequency + 15000);
                if (ledgeNoise > 0.65f && ledgeNoise < 0.75f)
                {
                    // Flatten areas to create ledges
                    float ledgeHeight = Mathf.Floor(height * 20f) / 20f; // Quantize height
                    height = Mathf.Lerp(height, ledgeHeight, 0.8f);
                }
                
                // Medium scale rock details
                frequency = 0.08f;
                amplitude = 0.15f;
                height += Mathf.PerlinNoise(x * frequency + 2000, z * frequency + 2000) * amplitude;
                
                // Apply mountain falloff but keep it steep
                height *= mountainFalloff;
                height = Mathf.Pow(height, 0.8f); // Less aggressive falloff, steeper walls
                
                heights[x, z] = Mathf.Clamp01(height);
            }
        }
        
        // Add climbing routes - carved paths up the mountain
        AddClimbingRoutes(heights, resolution, centerX, centerZ);
        
        return heights;
    }
    
    static void AddClimbingRoutes(float[,] heights, int resolution, float centerX, float centerZ)
    {
        // Create 4 major climbing routes up different sides of the mountain
        Vector2[] routeStarts = {
            new Vector2(centerX - resolution * 0.4f, centerZ), // West route
            new Vector2(centerX + resolution * 0.4f, centerZ), // East route
            new Vector2(centerX, centerZ - resolution * 0.4f), // South route
            new Vector2(centerX, centerZ + resolution * 0.4f)  // North route
        };
        
        Vector2 peak = new Vector2(centerX, centerZ);
        
        foreach (Vector2 start in routeStarts)
        {
            // Create spiral route to the top
            int steps = 200;
            for (int step = 0; step < steps; step++)
            {
                float progress = (float)step / (steps - 1);
                
                // Spiral path to peak
                Vector2 currentPos = Vector2.Lerp(start, peak, progress);
                
                // Add spiral motion
                float angle = progress * Mathf.PI * 4f; // 4 spirals to top
                float spiralRadius = (1f - progress) * resolution * 0.1f;
                currentPos.x += Mathf.Cos(angle) * spiralRadius;
                currentPos.y += Mathf.Sin(angle) * spiralRadius;
                
                // Add handholds and ledges along the route
                for (int radius = 0; radius < 8; radius++)
                {
                    for (int angle_step = 0; angle_step < 8; angle_step++)
                    {
                        float a = angle_step * 45f * Mathf.Deg2Rad;
                        int x = Mathf.RoundToInt(currentPos.x + Mathf.Cos(a) * radius);
                        int z = Mathf.RoundToInt(currentPos.y + Mathf.Sin(a) * radius);
                        
                        if (x >= 0 && x < resolution && z >= 0 && z < resolution)
                        {
                            if (radius < 3)
                            {
                                // Create small ledges for handholds
                                heights[x, z] += Random.Range(0.02f, 0.05f);
                            }
                            else if (radius < 6)
                            {
                                // Create medium bulges
                                heights[x, z] += Random.Range(0.01f, 0.03f);
                            }
                        }
                    }
                }
            }
        }
    }
    
    static void SetupTerrainTextures(TerrainData terrainData)
    {
        // Create terrain layers (Unity's new system)
        TerrainLayer[] terrainLayers = new TerrainLayer[5];
        
        // Grass layer
        terrainLayers[0] = new TerrainLayer();
        terrainLayers[0].diffuseTexture = CreateSolidColorTexture(new Color(0.3f, 0.6f, 0.2f, 1f), "Grass");
        terrainLayers[0].tileSize = new Vector2(15, 15);
        
        // Dirt layer  
        terrainLayers[1] = new TerrainLayer();
        terrainLayers[1].diffuseTexture = CreateSolidColorTexture(new Color(0.55f, 0.4f, 0.25f, 1f), "Dirt");
        terrainLayers[1].tileSize = new Vector2(15, 15);
        
        // Rock layer
        terrainLayers[2] = new TerrainLayer();
        terrainLayers[2].diffuseTexture = CreateSolidColorTexture(new Color(0.45f, 0.45f, 0.5f, 1f), "Rock");
        terrainLayers[2].tileSize = new Vector2(10, 10);
        
        // Snow layer
        terrainLayers[3] = new TerrainLayer();
        terrainLayers[3].diffuseTexture = CreateSolidColorTexture(new Color(0.95f, 0.95f, 1f, 1f), "Snow");
        terrainLayers[3].tileSize = new Vector2(8, 8);
        
        // Gravel layer
        terrainLayers[4] = new TerrainLayer();
        terrainLayers[4].diffuseTexture = CreateSolidColorTexture(new Color(0.6f, 0.55f, 0.5f, 1f), "Gravel");
        terrainLayers[4].tileSize = new Vector2(12, 12);
        
        terrainData.terrainLayers = terrainLayers;
    }
    
    static Texture2D CreateSolidColorTexture(Color color, string name)
    {
        Texture2D texture = new Texture2D(256, 256, TextureFormat.RGB24, false);
        Color[] pixels = new Color[256 * 256];
        
        for (int i = 0; i < pixels.Length; i++)
        {
            int x = i % 256;
            int y = i / 256;
            
            // Create rough rock texture with multiple noise layers
            float roughness1 = Mathf.PerlinNoise(x * 0.02f, y * 0.02f) * 0.4f;
            float roughness2 = Mathf.PerlinNoise(x * 0.08f, y * 0.08f) * 0.25f;
            float roughness3 = Mathf.PerlinNoise(x * 0.2f, y * 0.2f) * 0.15f;
            float roughness4 = Mathf.PerlinNoise(x * 0.5f, y * 0.5f) * 0.1f;
            
            float totalRoughness = (roughness1 + roughness2 + roughness3 + roughness4 - 0.45f);
            
            // Add cracks and darker spots for boulder-like texture
            float crackNoise = Mathf.PerlinNoise(x * 0.1f + 1000, y * 0.1f + 1000);
            if (crackNoise < 0.15f)
            {
                totalRoughness -= 0.3f; // Dark cracks
            }
            
            // Vary each color channel differently for more realistic look
            Color finalColor = color;
            finalColor.r = Mathf.Clamp01(color.r + totalRoughness * 0.8f);
            finalColor.g = Mathf.Clamp01(color.g + totalRoughness * 0.9f);
            finalColor.b = Mathf.Clamp01(color.b + totalRoughness * 1.1f);
            
            pixels[i] = finalColor;
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        texture.name = name;
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
        
        return texture;
    }
    
    static void PaintTerrain(TerrainData terrainData, float[,] heights)
    {
        int alphamapWidth = terrainData.alphamapWidth;
        int alphamapHeight = terrainData.alphamapHeight;
        int numTextures = terrainData.alphamapLayers;
        
        float[,,] splatmapData = new float[alphamapWidth, alphamapHeight, numTextures];
        
        for (int y = 0; y < alphamapHeight; y++)
        {
            for (int x = 0; x < alphamapWidth; x++)
            {
                // Sample height at this position
                float normalizedX = (float)x / (alphamapWidth - 1);
                float normalizedY = (float)y / (alphamapHeight - 1);
                
                int heightX = Mathf.RoundToInt(normalizedX * (heights.GetLength(0) - 1));
                int heightY = Mathf.RoundToInt(normalizedY * (heights.GetLength(1) - 1));
                
                float height = heights[heightX, heightY];
                
                // Calculate slope
                float slope = CalculateSlope(heights, heightX, heightY);
                
                // Texture weights based on height and slope
                float[] weights = new float[numTextures];
                
                // Grass (low areas, gentle slopes)
                if (height < 0.3f && slope < 0.3f)
                    weights[0] = 1f - slope;
                
                // Dirt (mid areas, moderate slopes)
                if (height >= 0.2f && height < 0.6f)
                    weights[1] = Mathf.Clamp01(1f - Mathf.Abs(height - 0.4f) * 2f);
                
                // Rock (steep areas, high areas) - more rock on steep slopes
                if (slope > 0.15f || (height > 0.4f && height < 0.8f))
                    weights[2] = Mathf.Clamp01(slope * 1.5f + (height > 0.5f ? height - 0.5f : 0));
                
                // Snow (very high areas)
                if (height > 0.7f)
                    weights[3] = (height - 0.7f) / 0.3f;
                
                // Gravel (transition areas)
                if (height < 0.4f && slope > 0.1f && slope < 0.4f)
                    weights[4] = slope * 0.5f;
                
                // Normalize weights
                float totalWeight = 0f;
                for (int i = 0; i < numTextures; i++)
                    totalWeight += weights[i];
                
                if (totalWeight > 0)
                {
                    for (int i = 0; i < numTextures; i++)
                        weights[i] /= totalWeight;
                }
                else
                {
                    weights[0] = 1f; // Default to grass if no other texture applies
                }
                
                // Apply weights to splatmap
                for (int i = 0; i < numTextures; i++)
                {
                    splatmapData[x, y, i] = weights[i];
                }
            }
        }
        
        terrainData.SetAlphamaps(0, 0, splatmapData);
    }
    
    static float CalculateSlope(float[,] heights, int x, int y)
    {
        int width = heights.GetLength(0);
        int height = heights.GetLength(1);
        
        if (x <= 0 || x >= width - 1 || y <= 0 || y >= height - 1)
            return 0f;
        
        float heightL = heights[x - 1, y];
        float heightR = heights[x + 1, y];
        float heightD = heights[x, y - 1];
        float heightU = heights[x, y + 1];
        
        float dx = heightR - heightL;
        float dy = heightU - heightD;
        
        return Mathf.Sqrt(dx * dx + dy * dy);
    }
    
    static void PlaceTrees(TerrainData terrainData, float[,] heights)
    {
        // Skip trees for now - terrain generation works without them
        terrainData.treePrototypes = new TreePrototype[0];
        terrainData.treeInstances = new TreeInstance[0];
    }
    
    static GameObject CreateTreePrefab()
    {
        GameObject tree = new GameObject("Tree");
        
        // Trunk
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.parent = tree.transform;
        trunk.transform.localPosition = new Vector3(0, 1, 0);
        trunk.transform.localScale = new Vector3(0.3f, 2, 0.3f);
        
        Renderer trunkRenderer = trunk.GetComponent<Renderer>();
        Material trunkMat = new Material(Shader.Find("Standard"));
        trunkMat.color = new Color(0.4f, 0.25f, 0.15f);
        trunkRenderer.material = trunkMat;
        
        // Foliage
        GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        foliage.name = "Foliage";
        foliage.transform.parent = tree.transform;
        foliage.transform.localPosition = new Vector3(0, 3, 0);
        foliage.transform.localScale = new Vector3(2, 2, 2);
        
        Renderer foliageRenderer = foliage.GetComponent<Renderer>();
        Material foliageMat = new Material(Shader.Find("Standard"));
        foliageMat.color = new Color(0.2f, 0.6f, 0.2f);
        foliageRenderer.material = foliageMat;
        
        return tree;
    }
    
    static void GenerateClimbingFeatures(TerrainData terrainData, Transform parent)
    {
        GameObject climbingParent = new GameObject("Climbing Features");
        climbingParent.transform.parent = parent;
        
        float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        Vector3 terrainSize = terrainData.size;
        Vector3 center = new Vector3(terrainSize.x * 0.5f, 0, terrainSize.z * 0.5f);
        
        // Generate LARGE CLIMBABLE ROCK WALLS like PEAK
        GenerateRockWalls(heights, terrainSize, center, climbingParent.transform);
        
        // Generate protruding handholds on walls
        GenerateWallHandholds(heights, terrainSize, center, climbingParent.transform);
        
        // Generate resting ledges
        GenerateRestingLedges(heights, terrainSize, center, climbingParent.transform);
    }
    
    static void GenerateRockWalls(float[,] heights, Vector3 terrainSize, Vector3 center, Transform parent)
    {
        GameObject wallsParent = new GameObject("Climbable Rock Walls");
        wallsParent.transform.parent = parent;
        
        // Create 4 major climbing walls on each side of the mountain
        Vector3[] wallCenters = {
            center + Vector3.forward * terrainSize.z * 0.3f,  // North wall
            center + Vector3.back * terrainSize.z * 0.3f,     // South wall  
            center + Vector3.left * terrainSize.x * 0.3f,     // West wall
            center + Vector3.right * terrainSize.x * 0.3f     // East wall
        };
        
        for (int wallIndex = 0; wallIndex < wallCenters.Length; wallIndex++)
        {
            Vector3 wallCenter = wallCenters[wallIndex];
            
            // Create large rock wall sections
            for (int section = 0; section < 8; section++)
            {
                Vector3 sectionPos = wallCenter + new Vector3(
                    Random.Range(-terrainSize.x * 0.15f, terrainSize.x * 0.15f),
                    Random.Range(terrainSize.y * 0.2f, terrainSize.y * 0.8f),
                    Random.Range(-terrainSize.z * 0.15f, terrainSize.z * 0.15f)
                );
                
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = $"ClimbWall_{wallIndex}_{section}";
                wall.tag = "Ledge";
                wall.transform.parent = wallsParent.transform;
                wall.transform.position = sectionPos;
                
                // Large wall sections
                wall.transform.localScale = new Vector3(
                    Random.Range(15f, 25f),    // Wide
                    Random.Range(20f, 40f),    // Tall
                    Random.Range(5f, 12f)      // Thick
                );
                
                // Random orientation for natural look
                wall.transform.rotation = Quaternion.Euler(
                    Random.Range(-10f, 10f),
                    Random.Range(0f, 360f),
                    Random.Range(-5f, 5f)
                );
                
                Renderer renderer = wall.GetComponent<Renderer>();
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.45f, 0.4f, 0.35f) * Random.Range(0.9f, 1.1f);
                renderer.material = mat;
            }
        }
    }
    
    static void GenerateWallHandholds(float[,] heights, Vector3 terrainSize, Vector3 center, Transform parent)
    {
        GameObject handholdParent = new GameObject("Wall Handholds");
        handholdParent.transform.parent = parent;
        
        // Create protruding handholds on the steep walls
        for (int i = 0; i < 400; i++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(terrainSize.x * 0.2f, terrainSize.x * 0.45f);
            
            float x = center.x + Mathf.Cos(angle) * distance;
            float z = center.z + Mathf.Sin(angle) * distance;
            
            float normalizedX = x / terrainSize.x;
            float normalizedZ = z / terrainSize.z;
            
            if (normalizedX >= 0.1f && normalizedX <= 0.9f && normalizedZ >= 0.1f && normalizedZ <= 0.9f)
            {
                int heightX = Mathf.RoundToInt(normalizedX * (heights.GetLength(0) - 1));
                int heightZ = Mathf.RoundToInt(normalizedZ * (heights.GetLength(1) - 1));
                
                float heightValue = heights[heightX, heightZ];
                float slope = CalculateSlope(heights, heightX, heightZ);
                
                // Only place on steep walls that need climbing
                if (slope > 0.3f && heightValue > 0.2f)
                {
                    Vector3 terrainPos = new Vector3(x, heightValue * terrainSize.y, z);
                    
                    GameObject handhold = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    handhold.name = "Handhold_" + i;
                    handhold.tag = "Ledge";
                    handhold.transform.parent = handholdParent.transform;
                    
                    // Position protruding from wall
                    Vector3 slopeNormal = GetSlopeNormal(heights, heightX, heightZ, terrainSize);
                    handhold.transform.position = terrainPos + slopeNormal * Random.Range(0.8f, 2f);
                    
                    // Small handhold size
                    handhold.transform.localScale = Vector3.one * Random.Range(1.2f, 2.5f);
                    
                    Renderer renderer = handhold.GetComponent<Renderer>();
                    Material mat = new Material(Shader.Find("Standard"));
                    mat.color = new Color(0.7f, 0.6f, 0.5f) * Random.Range(0.8f, 1.2f);
                    renderer.material = mat;
                }
            }
        }
    }
    
    static void GenerateRestingLedges(float[,] heights, Vector3 terrainSize, Vector3 center, Transform parent)
    {
        GameObject ledgesParent = new GameObject("Resting Ledges");
        ledgesParent.transform.parent = parent;
        
        // Create horizontal resting platforms like PEAK
        int numLevels = 6; // Different height levels
        
        for (int level = 0; level < numLevels; level++)
        {
            float heightLevel = (float)(level + 1) / numLevels; // 0.16, 0.33, 0.5, etc.
            
            // Create 3-5 ledges at each height level
            int ledgesAtLevel = Random.Range(3, 6);
            
            for (int i = 0; i < ledgesAtLevel; i++)
            {
                float angle = (360f / ledgesAtLevel) * i + Random.Range(-30f, 30f);
                angle *= Mathf.Deg2Rad;
                float distance = Random.Range(terrainSize.x * 0.25f, terrainSize.x * 0.42f);
                
                Vector3 ledgePos = center + new Vector3(
                    Mathf.Cos(angle) * distance,
                    heightLevel * terrainSize.y + Random.Range(-10f, 10f),
                    Mathf.Sin(angle) * distance
                );
                
                GameObject ledge = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ledge.name = $"RestLedge_L{level}_{i}";
                ledge.tag = "Ledge";
                ledge.transform.parent = ledgesParent.transform;
                ledge.transform.position = ledgePos;
                
                // Wide, flat resting platforms
                ledge.transform.localScale = new Vector3(
                    Random.Range(6f, 12f),    // Wide enough to stand on
                    Random.Range(0.5f, 1.5f), // Thick platform
                    Random.Range(4f, 8f)      // Deep enough to rest
                );
                
                // Keep mostly horizontal for resting
                ledge.transform.rotation = Quaternion.Euler(
                    Random.Range(-5f, 5f),
                    Random.Range(0f, 360f),
                    Random.Range(-3f, 3f)
                );
                
                Renderer renderer = ledge.GetComponent<Renderer>();
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.5f, 0.45f, 0.4f) * Random.Range(0.9f, 1.1f);
                renderer.material = mat;
            }
        }
    }
    
    static Vector3 GetSlopeNormal(float[,] heights, int x, int y, Vector3 terrainSize)
    {
        int width = heights.GetLength(0);
        int height = heights.GetLength(1);
        
        if (x <= 0 || x >= width - 1 || y <= 0 || y >= height - 1)
            return Vector3.up;
        
        float heightL = heights[x - 1, y] * terrainSize.y;
        float heightR = heights[x + 1, y] * terrainSize.y;
        float heightD = heights[x, y - 1] * terrainSize.y;
        float heightU = heights[x, y + 1] * terrainSize.y;
        
        Vector3 normal = new Vector3(heightL - heightR, 2f, heightD - heightU);
        return normal.normalized;
    }
    
    static void GenerateRockOutcrops(TerrainData terrainData, Transform parent)
    {
        GameObject rocksParent = new GameObject("Rock Outcrops");
        rocksParent.transform.parent = parent;
        
        float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        Vector3 terrainSize = terrainData.size;
        
        // Large boulders and rock formations
        int numBoulders = 60;
        
        for (int i = 0; i < numBoulders; i++)
        {
            float normalizedX = Random.Range(0.1f, 0.9f);
            float normalizedZ = Random.Range(0.1f, 0.9f);
            
            int heightX = Mathf.RoundToInt(normalizedX * (heights.GetLength(0) - 1));
            int heightZ = Mathf.RoundToInt(normalizedZ * (heights.GetLength(1) - 1));
            
            float heightValue = heights[heightX, heightZ];
            
            Vector3 worldPos = new Vector3(
                normalizedX * terrainSize.x,
                heightValue * terrainSize.y,
                normalizedZ * terrainSize.z
            );
            
            // Create different types of rocks
            PrimitiveType rockType = Random.Range(0f, 1f) > 0.3f ? PrimitiveType.Cube : PrimitiveType.Sphere;
            GameObject rock = GameObject.CreatePrimitive(rockType);
            rock.name = "Boulder_" + i;
            rock.transform.parent = rocksParent.transform;
            rock.transform.position = worldPos;
            
            // Random rotation for natural look
            rock.transform.rotation = Quaternion.Euler(
                Random.Range(0f, 360f),
                Random.Range(0f, 360f),
                Random.Range(0f, 360f)
            );
            
            // Various sizes - some very large
            float size = Random.Range(3f, 12f);
            rock.transform.localScale = new Vector3(
                size * Random.Range(0.7f, 1.3f),
                size * Random.Range(0.5f, 1.2f),
                size * Random.Range(0.7f, 1.3f)
            );
            
            Renderer renderer = rock.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = new Color(0.5f, 0.5f, 0.55f) * Random.Range(0.6f, 1.1f);
            renderer.material = mat;
        }
        
        // Smaller scattered rocks
        for (int i = 0; i < 100; i++)
        {
            float normalizedX = Random.Range(0f, 1f);
            float normalizedZ = Random.Range(0f, 1f);
            
            int heightX = Mathf.RoundToInt(normalizedX * (heights.GetLength(0) - 1));
            int heightZ = Mathf.RoundToInt(normalizedZ * (heights.GetLength(1) - 1));
            
            float heightValue = heights[heightX, heightZ];
            
            Vector3 worldPos = new Vector3(
                normalizedX * terrainSize.x,
                heightValue * terrainSize.y,
                normalizedZ * terrainSize.z
            );
            
            GameObject smallRock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            smallRock.name = "Rock_" + i;
            smallRock.transform.parent = rocksParent.transform;
            smallRock.transform.position = worldPos;
            
            smallRock.transform.rotation = Quaternion.Euler(
                Random.Range(0f, 360f),
                Random.Range(0f, 360f),
                Random.Range(0f, 360f)
            );
            
            float size = Random.Range(0.5f, 2.5f);
            smallRock.transform.localScale = Vector3.one * size;
            
            Renderer renderer = smallRock.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = new Color(0.6f, 0.55f, 0.5f) * Random.Range(0.8f, 1.2f);
            renderer.material = mat;
        }
    }
    
    static void PaintGravelPath(TerrainData terrainData)
    {
        Vector3 terrainSize = terrainData.size;
        float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        float[,,] splatmapData = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);
        
        // Create path from edge to base
        Vector3 startPoint = new Vector3(terrainSize.x * 0.1f, 0, terrainSize.z * 0.1f);
        Vector3 endPoint = new Vector3(terrainSize.x * 0.5f, 0, terrainSize.z * 0.5f);
        
        // Paint gravel texture along path
        for (int step = 0; step < 100; step++)
        {
            float progress = (float)step / 99f;
            Vector3 pathPos = Vector3.Lerp(startPoint, endPoint, progress);
            
            // Add winding
            pathPos.x += Mathf.Sin(progress * 8f) * 8f;
            pathPos.z += Mathf.Cos(progress * 6f) * 6f;
            
            float normalizedX = pathPos.x / terrainSize.x;
            float normalizedZ = pathPos.z / terrainSize.z;
            
            if (normalizedX >= 0 && normalizedX <= 1 && normalizedZ >= 0 && normalizedZ <= 1)
            {
                int alphaX = Mathf.RoundToInt(normalizedX * (terrainData.alphamapWidth - 1));
                int alphaZ = Mathf.RoundToInt(normalizedZ * (terrainData.alphamapHeight - 1));
                
                // Paint gravel in a radius around path point
                for (int x = -8; x <= 8; x++)
                {
                    for (int z = -8; z <= 8; z++)
                    {
                        int targetX = alphaX + x;
                        int targetZ = alphaZ + z;
                        
                        if (targetX >= 0 && targetX < terrainData.alphamapWidth && 
                            targetZ >= 0 && targetZ < terrainData.alphamapHeight)
                        {
                            float distance = Mathf.Sqrt(x * x + z * z);
                            if (distance <= 8f)
                            {
                                float strength = 1f - (distance / 8f);
                                
                                // Set to gravel texture (index 4)
                                splatmapData[targetX, targetZ, 0] = 0f; // grass
                                splatmapData[targetX, targetZ, 1] = 0f; // dirt  
                                splatmapData[targetX, targetZ, 2] = 0f; // rock
                                splatmapData[targetX, targetZ, 3] = 0f; // snow
                                splatmapData[targetX, targetZ, 4] = strength; // gravel
                            }
                        }
                    }
                }
            }
        }
        
        terrainData.SetAlphamaps(0, 0, splatmapData);
    }
    
    static void ClearMountain()
    {
        GameObject existingMountain = GameObject.Find("Mountain");
        if (existingMountain != null)
        {
            UnityEngine.Object.DestroyImmediate(existingMountain);
        }
    }
}