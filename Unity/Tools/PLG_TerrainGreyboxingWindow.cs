using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

// Custom editor window for the terrain greyboxing tool that generates a Unity terrain by raycasting against selected meshes to union or subtract them
public class PLG_TerrainGreyboxingWindow : EditorWindow
{
    Terrain combinedTerrainObj;
    List<MeshFilter> selectedMeshFilters = new List<MeshFilter>();
    int heightMapResolution = 50;
    Texture2D selectedHeightMap;
    Texture2D smoothedHeightMap;

    bool terrainIsSelected = false;
    bool smoothedHeightMapIsBehind = true;

    [MenuItem("Tools/Level/Greyboxing")]
    public static void ShowWindow()
    {
        // Show existing window instance. If one doesn't exist, make one
        PLG_TerrainGreyboxingWindow window = (PLG_TerrainGreyboxingWindow)EditorWindow.GetWindow(typeof(PLG_TerrainGreyboxingWindow));
    }

    void OnEnable()
    {
        // Check what is selected when the window opens
        OnSelectionChange();
        // Attempt to fill default references
        combinedTerrainObj = FindObjectOfType<Terrain>();
        if(combinedTerrainObj != null)
        {
            selectedHeightMap = new Texture2D(combinedTerrainObj.terrainData.heightmapHeight, combinedTerrainObj.terrainData.heightmapWidth, TextureFormat.RFloat, false, false);
            smoothedHeightMap = new Texture2D(combinedTerrainObj.terrainData.heightmapHeight, combinedTerrainObj.terrainData.heightmapWidth, TextureFormat.RFloat, false, false);
        }
    }

    void OnGUI()
    {
        // Terrains don't support a smaller resolution than 32
        heightMapResolution = Mathf.Max(32, EditorGUILayout.IntField("Height map resolution", heightMapResolution));

        EditorGUI.BeginChangeCheck();
        Terrain oldCombinedTerrainObj = combinedTerrainObj;
        combinedTerrainObj = (Terrain)EditorGUILayout.ObjectField("Combined terrain object", combinedTerrainObj, typeof(Terrain), true);
        if(EditorGUI.EndChangeCheck())
        {
            if(combinedTerrainObj != null && oldCombinedTerrainObj != null)
            {
                // Make new, appropriately-sized textures if the heightmapResolution changes
                if(oldCombinedTerrainObj.terrainData.heightmapResolution != combinedTerrainObj.terrainData.heightmapResolution)
                {
                    selectedHeightMap = new Texture2D(combinedTerrainObj.terrainData.heightmapHeight, combinedTerrainObj.terrainData.heightmapWidth, TextureFormat.RFloat, false, false);
                    smoothedHeightMap = new Texture2D(combinedTerrainObj.terrainData.heightmapHeight, combinedTerrainObj.terrainData.heightmapWidth, TextureFormat.RFloat, false, false);
                }
            }
        }

        GUILayout.BeginHorizontal();
        // Disable combine meshes button if no meshes are selected
        if (selectedMeshFilters.Count <= 0)
        {
            GUI.enabled = false;
        }
        if(GUILayout.Button("Combine meshes into terrain"))
        {
            Bounds containingBounds = GetBoundingBoxContainingSelectedMeshes();
            GenerateHeightmapInBounds(containingBounds);
            CreateTerrainFromHeightMap(selectedHeightMap, containingBounds);
            // Reset the smoothedHeightMap texture after regenerating terrain
            BlackOutHeightMap(smoothedHeightMap);
            smoothedHeightMapIsBehind = true;
        }
        // Disable carve terrain button if there is no terrain or no meshes are selected
        if(selectedMeshFilters.Count <= 0 || combinedTerrainObj == null)
        {
            GUI.enabled = false;
        }
        if(GUILayout.Button("Carve meshes from terrain"))
        {
            GenerateCarvedMeshHeightmap();
            CreateTerrainFromHeightMap(selectedHeightMap, GetTransformedTerrainBounds());
            // Reset the smoothedHeightMap texture after regenerating terrain
            BlackOutHeightMap(smoothedHeightMap);
            smoothedHeightMapIsBehind = true;
        }
        GUILayout.EndHorizontal();

        GUI.enabled = false;
        selectedHeightMap = (Texture2D)EditorGUILayout.ObjectField("Active height map", selectedHeightMap, typeof(Texture2D), false);
        if(selectedHeightMap != null)
        {
            GUI.enabled = true;
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("Smooth height map"))
            {
                if(!smoothedHeightMapIsBehind)
                {
                    SmoothHeightMap(smoothedHeightMap);
                }
                else
                {
                    SmoothHeightMap(selectedHeightMap);
                    smoothedHeightMapIsBehind = false;
                }
            }
            GUI.enabled = false;
            EditorGUILayout.ObjectField("Smoothed height map", smoothedHeightMap, typeof(Texture2D), false);
            GUILayout.EndHorizontal();

            if (smoothedHeightMap != null)
            {
                GUI.enabled = true;
            }
            if(GUILayout.Button("Make terrain from smoothed height map"))
            {
                CreateTerrainFromHeightMap(smoothedHeightMap, GetTransformedTerrainBounds());
            }
        }
        GUI.enabled = true;
    }

    void OnSelectionChange()
    {
        // Update the list of selected MeshFilters
        selectedMeshFilters.Clear();
        foreach (Transform trans in Selection.transforms)
        {
            MeshFilter meshFilter = trans.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                selectedMeshFilters.Add(meshFilter);
            }
        }
        // Check if the combinedTerrainObj exists and is selected
        terrainIsSelected = false;
        if(combinedTerrainObj != null && Selection.Contains(combinedTerrainObj.gameObject))
        {
            terrainIsSelected = true;
        }
    }

    // Function to return a bounds encompassing all selected meshes and the combinedTerrainObj
    Bounds GetBoundingBoxContainingSelectedMeshes()
    {
        Vector3 min = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
        Vector3 max = new Vector3(Mathf.NegativeInfinity, Mathf.NegativeInfinity, Mathf.NegativeInfinity);

        foreach(MeshFilter meshFilter in selectedMeshFilters)
        {
            foreach(Vector3 vertex in meshFilter.sharedMesh.vertices)
            {
                Vector3 vertexInWorldSpace = meshFilter.transform.TransformPoint(vertex);
                if(vertexInWorldSpace.x < min.x)
                {
                    min.x = vertexInWorldSpace.x;
                }
                if(vertexInWorldSpace.y < min.y)
                {
                    min.y = vertexInWorldSpace.y;
                }
                if(vertexInWorldSpace.z < min.z)
                {
                    min.z = vertexInWorldSpace.z;
                }
                if(vertexInWorldSpace.x > max.x)
                {
                    max.x = vertexInWorldSpace.x;
                }
                if(vertexInWorldSpace.y > max.y)
                {
                    max.y = vertexInWorldSpace.y;
                }
                if(vertexInWorldSpace.z > max.z)
                {
                    max.z = vertexInWorldSpace.z;
                }
            }
        }

        if(terrainIsSelected)
        {
            Bounds terrainBounds = GetTransformedTerrainBounds();
            if(terrainBounds.min.x < min.x)
            {
                min.x = terrainBounds.min.x;
            }
            if(terrainBounds.min.y < min.y)
            {
                min.y = terrainBounds.min.y;
            }
            if(terrainBounds.min.z < min.z)
            {
                min.z = terrainBounds.min.z;
            }
            if(terrainBounds.max.x > max.x)
            {
                max.x = terrainBounds.max.x;
            }
            if(terrainBounds.max.y > max.y)
            {
                max.y = terrainBounds.max.y;
            }
            if(terrainBounds.max.z > max.z)
            {
                max.z = terrainBounds.max.z;
            }
        }

        Vector3 center = (min + max) / 2f;
        Vector3 size = max - min;
        return new Bounds(center, size);
    }

    // Function that populates selectedHeightMap by raycasting downward along the length of a bounds
    // Heightmap height is the max of hit selected meshes and selected terrain
    void GenerateHeightmapInBounds(Bounds bounds)
    {
        TerrainData terrainData = MakeTerrainObjectIfNeeded();
        Bounds transformedTerrainBounds = GetTransformedTerrainBounds();

        float[,] heightMapVals = new float[terrainData.heightmapHeight, terrainData.heightmapWidth];
        for (int row = 0; row < terrainData.heightmapHeight; ++row)
        {
            for (int col = 0; col < terrainData.heightmapWidth; ++col)
            {
                // Set the border edges to height 0 so there aren't invisible sides
                if (row == 0 || row == terrainData.heightmapHeight - 1
                    || col == 0 || col == terrainData.heightmapWidth - 1)
                {
                    heightMapVals[row, col] = 0;
                }
                else
                {
                    List<float> hitPoints = new List<float>();
                    Vector3 raycastPos = new Vector3(bounds.min.x + col * bounds.size.x / terrainData.heightmapWidth,
                                                    bounds.max.y + 1,
                                                    bounds.min.z + row * bounds.size.z / terrainData.heightmapHeight);
                    RaycastHit[] hits = Physics.RaycastAll(raycastPos, Vector3.down, 1 + bounds.size.y);
                    foreach(RaycastHit hit in hits)
                    {
                        if(System.Array.Find(Selection.transforms, x => x.gameObject == hit.collider.gameObject))
                        {
                            hitPoints.Add((hit.point.y - bounds.min.y) / bounds.size.y);
                        }
                    }
                    raycastPos.y = transformedTerrainBounds.center.y;
                    if(terrainIsSelected && transformedTerrainBounds.Contains(raycastPos))
                    {
                        hitPoints.Add((combinedTerrainObj.SampleHeight(raycastPos) +combinedTerrainObj.GetPosition().y - bounds.min.y) / bounds.size.y);
                    }

                    if(hitPoints.Count > 0)
                    {
                        heightMapVals[row, col] = hitPoints.Max();
                    }
                    else
                    {
                        heightMapVals[row, col] = 0;
                    }
                }
            }
        }

        byte[] byteArray = new byte[heightMapVals.Length * 4];
        System.Buffer.BlockCopy(heightMapVals, 0, byteArray, 0, byteArray.Length);
        Undo.RecordObject(selectedHeightMap, "Combine heightmap");
        selectedHeightMap.LoadRawTextureData(byteArray);
        selectedHeightMap.Apply();
    }

    // Function that sets the heightmap of the Unity terrain and scales it to a bounds
    // Parameter heightMap must be a Texture2D in RFloat format the same size as the terrain's heightmap
    void CreateTerrainFromHeightMap(Texture2D heightMap, Bounds bounds)
    {
        TerrainData terrainData = MakeTerrainObjectIfNeeded();
        Undo.RecordObject(terrainData, "Set terrain heightmap");
        terrainData.size = bounds.size;

        byte[] imageBytes = heightMap.GetRawTextureData();
        float[,] heightMapVals = new float[terrainData.heightmapHeight, terrainData.heightmapWidth];
        System.Buffer.BlockCopy(imageBytes, 0, heightMapVals, 0, imageBytes.Length);
        terrainData.SetHeights(0, 0, heightMapVals);

        Undo.RecordObject(combinedTerrainObj.transform, "Set terrain heightmap");
        combinedTerrainObj.transform.position = bounds.min;
    }

    // Function that populates selectedHeightMap by raycasting upward along terrain's bounding box
    // Heightmap height is the min of hit selected meshes and selected terrain
    void GenerateCarvedMeshHeightmap()
    {
        TerrainData terrainData = MakeTerrainObjectIfNeeded();
        Bounds transformedTerrainBounds = GetTransformedTerrainBounds();

        float[,] heightMapVals = new float[terrainData.heightmapHeight, terrainData.heightmapWidth];
        for (int row = 0; row < terrainData.heightmapHeight; ++row)
        {
            for (int col = 0; col < terrainData.heightmapWidth; ++col)
            {
                // Set the border edges to height 0 so there aren't invisible sides
                if (row == 0 || row == terrainData.heightmapHeight - 1
                    || col == 0 || col == terrainData.heightmapWidth - 1)
                {
                    heightMapVals[row, col] = 0;
                }
                else
                {
                    List<float> hitPoints = new List<float>();
                    Vector3 raycastPos = new Vector3(transformedTerrainBounds.min.x + col * transformedTerrainBounds.size.x / terrainData.heightmapWidth,
                                                        transformedTerrainBounds.min.y - 1,
                                                        transformedTerrainBounds.min.z + row * transformedTerrainBounds.size.z / terrainData.heightmapHeight);
                    RaycastHit[] hits = Physics.RaycastAll(raycastPos, Vector3.up, 1 + transformedTerrainBounds.size.y);
                    foreach (RaycastHit hit in hits)
                    {
                        if (System.Array.Find(Selection.transforms, x => x.gameObject == hit.collider.gameObject))
                        {
                            hitPoints.Add((hit.point.y - transformedTerrainBounds.min.y) / transformedTerrainBounds.size.y);
                        }
                    }
                    hitPoints.Add((combinedTerrainObj.SampleHeight(raycastPos) + combinedTerrainObj.GetPosition().y - transformedTerrainBounds.min.y) / transformedTerrainBounds.size.y);

                    if (hitPoints.Count > 0)
                    {
                        heightMapVals[row, col] = hitPoints.Min();
                    }
                    else
                    {
                        heightMapVals[row, col] = 0f;
                    }
                }
            }
        }

        byte[] byteArray = new byte[heightMapVals.Length * 4];
        System.Buffer.BlockCopy(heightMapVals, 0, byteArray, 0, byteArray.Length);
        Undo.RecordObject(selectedHeightMap, "Carve heightmap");
        selectedHeightMap.LoadRawTextureData(byteArray);
        selectedHeightMap.Apply();
    }

    // Function to return the TerrainData of the combinedTerrainObj and make one if it doesn't exist
    TerrainData MakeTerrainObjectIfNeeded()
    {
        TerrainData terrainData = null;
        if (combinedTerrainObj == null)
        {
            terrainData = new TerrainData();
            GameObject terrainObj = Terrain.CreateTerrainGameObject(terrainData);
            combinedTerrainObj = terrainObj.GetComponent<Terrain>();
            // Only set heightmapResolution when creating a new terrain because it resets the terrain's heightmap data
            terrainData.heightmapResolution = heightMapResolution + 1;

            Undo.RegisterCreatedObjectUndo(terrainObj, "Create new terrain");

            if (selectedHeightMap == null || selectedHeightMap.width != terrainData.heightmapHeight)
            {
                selectedHeightMap = new Texture2D(terrainData.heightmapHeight, terrainData.heightmapWidth, TextureFormat.RFloat, false, false);
                smoothedHeightMap = new Texture2D(terrainData.heightmapHeight, terrainData.heightmapWidth, TextureFormat.RFloat, false, false);
            }
        }
        else
        {
            terrainData = combinedTerrainObj.terrainData;
        }
        return terrainData;
    }

    // Function to convolve a gaussian blur kernel across a heightmap Texture2D
    // Parameter heightMap must be a Texture2D in RFloat format
    void SmoothHeightMap(Texture2D heightMap)
    {
        byte[] rawImageBytes = heightMap.GetRawTextureData();
        float[] rawImageFloats = new float[rawImageBytes.Length / 4];
        System.Buffer.BlockCopy(rawImageBytes, 0, rawImageFloats, 0, rawImageBytes.Length);

        float[] processedImageFloats = new float[rawImageFloats.Length];
        System.Buffer.BlockCopy(rawImageFloats, 0, processedImageFloats, 0, rawImageFloats.Length);

        float[] gaussianBlurKernel3 = new float[] { 0.077847f, 0.123317f, 0.077847f, 0.123317f, 0.195346f, 0.123317f, 0.077847f, 0.123317f, 0.077847f };
        for (int row = 1; row < heightMap.height - 1; ++row)
        {
            for (int col = 1; col < heightMap.width - 1; ++col)
            {
                float topLeft = rawImageFloats[(row - 1) * heightMap.width + col - 1] * gaussianBlurKernel3[0];
                float topMiddle = rawImageFloats[(row - 1) * heightMap.width + col] * gaussianBlurKernel3[1];
                float topRight = rawImageFloats[(row - 1) * heightMap.width + col + 1] * gaussianBlurKernel3[2];
                float left = rawImageFloats[row * heightMap.width + col - 1] * gaussianBlurKernel3[3];
                float middle = rawImageFloats[row * heightMap.width + col] * gaussianBlurKernel3[4];
                float right = rawImageFloats[row * heightMap.width + col + 1] * gaussianBlurKernel3[5];
                float bottomLeft = rawImageFloats[(row + 1) * heightMap.width + col - 1] * gaussianBlurKernel3[6];
                float bottomMiddle = rawImageFloats[(row + 1) * heightMap.width + col] * gaussianBlurKernel3[7];
                float bottomRight = rawImageFloats[(row + 1) * heightMap.width + col + 1] * gaussianBlurKernel3[8];
                processedImageFloats[row * heightMap.width + col] = topLeft + topMiddle + topRight + left + middle + right + bottomLeft + bottomMiddle + bottomRight;
            }
        }

        byte[] processedImageBytes = new byte[processedImageFloats.Length * 4];
        System.Buffer.BlockCopy(processedImageFloats, 0, processedImageBytes, 0, processedImageBytes.Length);
        Undo.RecordObject(smoothedHeightMap, "Smooth heightmap");
        smoothedHeightMap.LoadRawTextureData(processedImageBytes);
        smoothedHeightMap.Apply();
    }

    // Function to return the bounds of the combinedTerrainObj transformed into world space
    Bounds GetTransformedTerrainBounds()
    {
        TerrainData terrainData = MakeTerrainObjectIfNeeded();
        Vector3 min = terrainData.bounds.min;
        Vector3 max = terrainData.bounds.max;
        min = combinedTerrainObj.transform.TransformPoint(min);
        max = combinedTerrainObj.transform.TransformPoint(max);

        Vector3 center = (min + max) / 2f;
        Vector3 size = max - min;
        return new Bounds(center, size);
    }

    // Function to set all the pixels of a Texture2D to black
    void BlackOutHeightMap(Texture2D heightmap)
    {
        byte[] imageBytes = heightmap.GetRawTextureData();
        System.Array.Clear(imageBytes, 0, imageBytes.Length);
        heightmap.LoadRawTextureData(imageBytes);
    }
}
