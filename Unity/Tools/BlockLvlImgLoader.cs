using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

public class BlockLvlImgLoader : EditorWindow {
    Vector2 scrollPos;

    int numPNGs = 1;
    List<Texture2D> layerPNGs = new List<Texture2D>();
    List<int> layerHeights = new List<int>();

    GameObject groundPrefab;
    int unitsPerPixel = 10;

    int numMappings = 1;
    class ColorToPrefab {
        public Color color;
        public GameObject gameObject;
        public ColorToPrefab() {
            color = Color.black;
            gameObject = null;
        }
    }
    List<ColorToPrefab> colorMappings = new List<ColorToPrefab>();
    List<bool> callFunction = new List<bool>();
    List<int> functionIndices = new List<int>();
    static MethodInfo[] methods;
    static string[] methodSigs;
    List<List<object>> functionArguments = new List<List<object>>();

    string errorMsg;
    float errorDispTime = 2.0f;
    float errorTimer = 0.0f;

    bool generatingLvl = false;
    int currentLvl = 0;
    int xPixelCoord = 0;
    int zPixelCoord = 0;
    GameObject lastInstantiatedObj = null;

    string undoCommandName = "Undo generate level blocking";

    [MenuItem("Level Generation/Load Blocking from PNG")]
    public static void ShowWindow() {
        EditorWindow.GetWindow(typeof(BlockLvlImgLoader));
        RefreshLvlGenFunctions();
    }
	
	void OnGUI() {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        numPNGs = EditorGUILayout.DelayedIntField("PNG Layers Count", numPNGs);
        ResizePNGLists();
        for(int i = 0; i < numPNGs; ++i) {
            EditorGUILayout.BeginHorizontal();
            layerPNGs[i] = (Texture2D)EditorGUILayout.ObjectField("Layer " + (i + 1).ToString(), layerPNGs[i], typeof(Texture2D), false, GUILayout.Height(100));
            layerHeights[i] = EditorGUILayout.DelayedIntField("Y Height", layerHeights[i]);
            EditorGUILayout.EndHorizontal();
        }

        GUILayout.FlexibleSpace();

        EditorGUILayout.BeginHorizontal();
        groundPrefab = (GameObject)EditorGUILayout.ObjectField("Ground Prefab", groundPrefab, typeof(GameObject), false);
        unitsPerPixel = EditorGUILayout.DelayedIntField("Units Per Pixel", unitsPerPixel);
        EditorGUILayout.EndHorizontal();

        GUILayout.FlexibleSpace();

        numMappings = EditorGUILayout.DelayedIntField("Color Mappings Count", numMappings);
        ResizeMappingsLists();        
        for (int i = 0; i < colorMappings.Count; ++i) {
            EditorGUILayout.BeginHorizontal();
            colorMappings[i].color = EditorGUILayout.ColorField("PNG Pixel Color", colorMappings[i].color);
            colorMappings[i].gameObject = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Gameobject","Objects will be scaled in X and Z to Units Per Pixel"), colorMappings[i].gameObject, typeof(GameObject), false);
            callFunction[i] = EditorGUILayout.Toggle("Call Function", callFunction[i]);
            if (callFunction[i]) {
                GenLvlGenFunctionsUI(i);
            }
            EditorGUILayout.EndHorizontal();
        }

        GUILayout.FlexibleSpace();

        if(GUILayout.Button("Generate Level")) {
            errorMsg = GenerateLevel();
            if(errorMsg != null) {
                errorTimer = errorDispTime;
            }
        }
        if(errorTimer > 0.0f) {
            EditorGUILayout.HelpBox(errorMsg, MessageType.Error);
        }

        EditorGUILayout.EndScrollView();
    }

    void Update() {
        if(errorTimer > 0.0f) {
            errorTimer -= Time.deltaTime;
        }
    }

    void ResizePNGLists() {
        ResizeList(layerPNGs, numPNGs);
        ResizeList(layerHeights, numPNGs);
    }

    void ResizeMappingsLists() {
        ResizeList(colorMappings, numMappings);
        ResizeList(callFunction, numMappings);
        ResizeList(functionIndices, numMappings);
        ResizeList(functionArguments, numMappings);
    }

    void ResizeList<T>(List<T> list, int newSize) {
        if(list == null) {
            list = new List<T>();
        }

        if(list.Count > newSize) {
            int diff = list.Count - newSize;
            list.RemoveRange(list.Count - diff, diff);
        }
        else if(list.Count < newSize) {
            var constructor = typeof(T).GetConstructor(System.Type.EmptyTypes);
            while(list.Count < newSize) {
                if(constructor != null) {
                    list.Add((T)constructor.Invoke(null));
                }
                else {
                    list.Add(default(T));
                }
            }
        }
    }

    [UnityEditor.Callbacks.DidReloadScripts]
    static void RefreshLvlGenFunctions() {
        methods = typeof(LvlGenFunctions).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        methodSigs = new string[methods.Length];
        for (int i = 0; i < methods.Length; ++i) {
            methodSigs[i] = methods[i].Name + "(";
            ParameterInfo[] parameterInfos = methods[i].GetParameters();
            for(int j = 0; j < parameterInfos.Length; ++j)
            {
                methodSigs[i] += parameterInfos[j].ToString();
                if(j < parameterInfos.Length - 1)
                {
                    methodSigs[i] += ",";
                }
            }
            methodSigs[i] += ")";
        }
    }

    void GenLvlGenFunctionsUI(int mappingIndex) {
        EditorGUILayout.BeginVertical();
        functionIndices[mappingIndex] = EditorGUILayout.Popup("Function to Call", functionIndices[mappingIndex], methodSigs);
        ParameterInfo[] parameters = methods[functionIndices[mappingIndex]].GetParameters();
        ResizeList(functionArguments[mappingIndex], parameters.Length);
        EditorGUILayout.BeginHorizontal();
        for(int i = 0; i < parameters.Length; ++i) {
            System.Type type = parameters[i].ParameterType;
            if (type.IsSubclassOf(typeof(Object))) {
                if (functionArguments[mappingIndex][i] != null && !functionArguments[mappingIndex][i].GetType().IsSubclassOf(typeof(Object))) {
                    functionArguments[mappingIndex][i] = null;
                }
                functionArguments[mappingIndex][i] = EditorGUILayout.ObjectField((Object)functionArguments[mappingIndex][i], type, false);
            }
            else if (type.IsEnum) {
                if(functionArguments[mappingIndex][i] != null && !functionArguments[mappingIndex][i].GetType().IsEnum) {
                    functionArguments[mappingIndex][i] = null;
                }
                if(functionArguments[mappingIndex][i] == null) {
                    functionArguments[mappingIndex][i] = (System.Enum)System.Activator.CreateInstance(type);
                }
                functionArguments[mappingIndex][i] = EditorGUILayout.EnumPopup((System.Enum)functionArguments[mappingIndex][i]);
            }
            else if (type.Equals(typeof(int)))
            {
                if(functionArguments[mappingIndex][i] != null && !functionArguments[mappingIndex][i].GetType().Equals(typeof(int)))
                {
                    functionArguments[mappingIndex][i] = null;
                }
                if (functionArguments[mappingIndex][i] == null)
                {
                    functionArguments[mappingIndex][i] = 0;
                }
                functionArguments[mappingIndex][i] = EditorGUILayout.DelayedIntField((int)functionArguments[mappingIndex][i]);
            }
            else if (type.Equals(typeof(float)))
            {
                if(functionArguments[mappingIndex][i] != null && ! functionArguments[mappingIndex][i].GetType().Equals(typeof(float)))
                {
                    functionArguments[mappingIndex][i] = null;
                }
                if(functionArguments[mappingIndex][i] == null)
                {
                    functionArguments[mappingIndex][i] = 0.0f;
                }
                functionArguments[mappingIndex][i] = EditorGUILayout.DelayedFloatField((float)functionArguments[mappingIndex][i]);
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    string GenerateLevel() {
        if(groundPrefab == null || layerPNGs[0] == null) {
            return "A ground prefab and one PNG for the level must be provided";
        }

        GameObject[] allObj = FindObjectsOfType<GameObject>();
        for(int i = allObj.Length - 1; i >= 0; --i) {
            if (allObj[i] != null && !allObj[i].CompareTag("MainCamera") && !allObj[i].CompareTag("DontDestroyOnLvlGen")) {
                Undo.DestroyObjectImmediate(allObj[i]);
            }
        }
        if(AssetDatabase.IsValidFolder("Assets/LevelGeneratedMeshes"))
        {
            AssetDatabase.DeleteAsset("Assets/LevelGeneratedMeshes");
        }

        GameObject ground = Instantiate(groundPrefab);
        ground.transform.position = new Vector3(layerPNGs[0].width * unitsPerPixel / 2.0f, ground.transform.position.y, layerPNGs[0].height * unitsPerPixel / 2.0f);
        ground.transform.localScale = new Vector3(layerPNGs[0].width * unitsPerPixel, ground.transform.localScale.y, layerPNGs[0].height * unitsPerPixel);
        Undo.RegisterCreatedObjectUndo(ground, undoCommandName);

        generatingLvl = true;
        // For each PNG
        for(currentLvl = 0; currentLvl < layerPNGs.Count; ++currentLvl) {
            GameObject layerHolder = new GameObject("Layer " + (currentLvl + 1).ToString());
            Undo.RegisterCreatedObjectUndo(layerHolder, undoCommandName);
            // For each pixel in the PNG
            for(xPixelCoord = 0; xPixelCoord < layerPNGs[currentLvl].width; ++xPixelCoord) {
                for(zPixelCoord = 0; zPixelCoord < layerPNGs[currentLvl].height; ++zPixelCoord) {
                    // If the pixel is not fully transparent
                    if(layerPNGs[currentLvl].GetPixel(xPixelCoord, zPixelCoord).a != 0) {
                        // Find the colorMapping whose color matches the pixel color
                        for(int j = 0; j < colorMappings.Count; ++j) {
                            if (colorMappings[j].color == layerPNGs[currentLvl].GetPixel(xPixelCoord, zPixelCoord)) {
                                if(colorMappings[j].gameObject != null) {
                                    lastInstantiatedObj = Instantiate(colorMappings[j].gameObject, new Vector3(unitsPerPixel * xPixelCoord + unitsPerPixel / 2.0f, layerHeights[currentLvl], unitsPerPixel * zPixelCoord + unitsPerPixel / 2.0f), Quaternion.identity);
                                    lastInstantiatedObj.transform.localScale = new Vector3(unitsPerPixel, lastInstantiatedObj.transform.localScale.y, unitsPerPixel);
                                    lastInstantiatedObj.transform.SetParent(layerHolder.transform, true);
                                    Undo.RegisterCreatedObjectUndo(lastInstantiatedObj, undoCommandName);
                                }
                                if (callFunction[j]) {
                                    object[] arguments = new object[functionArguments[j].Count];
                                    for(int k = 0; k < functionArguments[j].Count; ++k) {
                                        arguments[k] = functionArguments[j][k];
                                    }
                                    methods[functionIndices[j]].Invoke(null, arguments);
                                }
                            }
                        }
                    }                    
                }
            }
        }
        generatingLvl = false;
        currentLvl = xPixelCoord = zPixelCoord = 0;

        return null;
    }

    public int GetCurrentLvl() {
        if (!generatingLvl) {
            return -1;
        }
        else {
            return currentLvl;
        }
    }

    public Vector2 GetCurrentPixelCoords() {
        if (!generatingLvl) {
            return -Vector2.one;
        }
        else {
            return new Vector2(xPixelCoord, zPixelCoord);
        }
    }

    public GameObject GetLastInstantiatedObj() {
        return lastInstantiatedObj;
    }
}
