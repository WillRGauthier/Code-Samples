using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEditor;

public class LvlGenFunctions {
    public enum LvlTraversalAction {
        ENDPOINT,
        REUSE
    }

    class LvlTraversalData {
        public Vector2 pixelCoords;
        public LvlTraversalAction action;
        public GameObject traversalObj;
        public int additionalInt;
        public LvlTraversalData(Vector2 inCoords, LvlTraversalAction inMarker, GameObject inObj, int inInt) {
            pixelCoords = inCoords;
            action = inMarker;
            traversalObj = inObj;
            additionalInt = inInt;
        }
    }

    static void AddLvlTraversalMarkerToList(Dictionary<int, List<LvlTraversalData>> markerList, LvlTraversalAction action, int optionalInt = 0)
    {
        BlockLvlImgLoader loaderWindow = (BlockLvlImgLoader)EditorWindow.GetWindow(typeof(BlockLvlImgLoader));
        int level = loaderWindow.GetCurrentLvl();
        Vector2 pixelCoord = loaderWindow.GetCurrentPixelCoords();
        GameObject traversalObj = loaderWindow.GetLastInstantiatedObj();
        if (!markerList.ContainsKey(level))
        {
            markerList.Add(level, new List<LvlTraversalData>());
        }
        markerList[level].Add(new LvlTraversalData(pixelCoord, action, traversalObj, optionalInt));
    }

    static LvlTraversalData FindClosestTraversable(LvlTraversalData currTraversable, List<LvlTraversalData> nextLvlTraversables)
    {
        if (nextLvlTraversables == null || nextLvlTraversables.Count <= 0)
        {
            return null;
        }
        else
        {
            LvlTraversalData closestTraversable = nextLvlTraversables[0];
            float closestManhattanDist = Mathf.Abs(nextLvlTraversables[0].pixelCoords.x - currTraversable.pixelCoords.x) + Mathf.Abs(nextLvlTraversables[0].pixelCoords.y - currTraversable.pixelCoords.y);
            for (int i = 1; i < nextLvlTraversables.Count; ++i)
            {
                float manhattanDist = Mathf.Abs(nextLvlTraversables[i].pixelCoords.x - currTraversable.pixelCoords.x) + Mathf.Abs(nextLvlTraversables[i].pixelCoords.y - currTraversable.pixelCoords.y);
                if (manhattanDist < closestManhattanDist)
                {
                    closestManhattanDist = manhattanDist;
                    closestTraversable = nextLvlTraversables[i];
                }
            }
            if (closestTraversable.action == LvlTraversalAction.ENDPOINT)
            {
                nextLvlTraversables.Remove(closestTraversable);
            }
            return closestTraversable;
        }
    }

    static void ConnectTraversables(Dictionary<int, List<LvlTraversalData>> traversableMarkers, Action<LvlTraversalData, LvlTraversalData> function)
    {
        List<int> levels = new List<int>(traversableMarkers.Keys);
        for (int i = 0; i < levels.Count; ++i)
        {
            List<LvlTraversalData> removalList = new List<LvlTraversalData>();
            foreach (LvlTraversalData traversableMarker in traversableMarkers[levels[i]])
            {
                LvlTraversalData closestTraversable = null;
                int levelsToCheck = Math.Max(levels.Count - 1 - i, i);
                for(int levelDiff = 1; levelsToCheck > 0; --levelsToCheck, ++levelDiff)
                {
                    if(i + levelDiff < levels.Count)
                    {
                        closestTraversable = FindClosestTraversable(traversableMarker, traversableMarkers[levels[i + levelDiff]]);
                    }
                    if(closestTraversable == null && i - levelDiff >= 0)
                    {
                        closestTraversable = FindClosestTraversable(traversableMarker, traversableMarkers[levels[i - levelDiff]]);
                    }
                    if(closestTraversable != null)
                    {
                        break;
                    }
                }

                if (closestTraversable == null)
                {
                    Debug.LogError("Traversable at layer " + (i + 1).ToString() + " at coordinates " + traversableMarker.pixelCoords.ToString() + " could not find endpoint; Aborting");
                    traversableMarkers.Clear();
                    return;
                }
                else
                {
                    function(traversableMarker, closestTraversable);
                    if(traversableMarker.action == LvlTraversalAction.ENDPOINT)
                    {
                        removalList.Add(traversableMarker);
                    }
                }
            }
            foreach(LvlTraversalData traversableMarker in removalList)
            {
                traversableMarkers[levels[i]].Remove(traversableMarker);
            }
        }
        traversableMarkers.Clear();
    }

    static Dictionary<int, List<LvlTraversalData>> rampMarkers = new Dictionary<int, List<LvlTraversalData>>();

    static public void GenerateRamp(LvlTraversalAction action) {
        AddLvlTraversalMarkerToList(rampMarkers, action);
    }

    public static void ConnectRamps() {
        ConnectTraversables(rampMarkers, ShearRamp);
    }

    static void ShearRamp(LvlTraversalData rampStart, LvlTraversalData rampEnd) {
        // Clone the start of reusable ramps so it can be reused
        GameObject originalStart = rampStart.traversalObj;
        if(rampStart.action == LvlTraversalAction.REUSE)
        {
            originalStart = GameObject.Instantiate(rampStart.traversalObj);
        }

        Vector4 diff = (rampEnd.traversalObj.transform.position - rampStart.traversalObj.transform.position);
        diff.x /= rampStart.traversalObj.transform.localScale.x;
        diff.y /= rampStart.traversalObj.transform.localScale.y;
        diff.z /= rampStart.traversalObj.transform.localScale.z;
        Matrix4x4 shearMat = Matrix4x4.identity;
        shearMat.SetColumn(1, diff);

        Mesh mesh = Mesh.Instantiate(rampStart.traversalObj.GetComponent<MeshFilter>().sharedMesh) as Mesh;
        Vector3[] vertices = mesh.vertices;
        for (int v = 0; v < vertices.Length; ++v) {
            vertices[v] = shearMat.MultiplyPoint(vertices[v]);
        }
        mesh.vertices = vertices;
        if(!AssetDatabase.IsValidFolder("Assets/LevelGeneratedMeshes"))
        {
            AssetDatabase.CreateFolder("Assets", "LevelGeneratedMeshes");
        }
        AssetDatabase.CreateAsset(mesh, "Assets/LevelGeneratedMeshes/rampID" + rampStart.traversalObj.GetInstanceID().ToString() + ".asset");
        rampStart.traversalObj.GetComponent<MeshFilter>().mesh = mesh;
        mesh.RecalculateBounds();

        float raisedY = rampStart.traversalObj.transform.position.y + (diff.y / 2.0f) * rampStart.traversalObj.transform.localScale.y;
        float adjustedX = rampStart.traversalObj.transform.position.x + (diff.x / 2.0f) * rampStart.traversalObj.transform.localScale.x;
        float adjustedZ = rampStart.traversalObj.transform.position.z + (diff.z / 2.0f) * rampStart.traversalObj.transform.localScale.x;
        rampStart.traversalObj.transform.position = new Vector3(adjustedX, raisedY, adjustedZ);

        rampStart.traversalObj = originalStart;
        if(rampEnd.action == LvlTraversalAction.ENDPOINT) {
            UnityEngine.Object.DestroyImmediate(rampEnd.traversalObj);
        }
    }

    public static void RaiseInvisibleWall(float height)
    {
        GameObject invisibleWallHolder = ((BlockLvlImgLoader)EditorWindow.GetWindow(typeof(BlockLvlImgLoader))).GetLastInstantiatedObj();
        float alterHeight = invisibleWallHolder.GetComponentInChildren<MeshFilter>().sharedMesh.bounds.size.y * invisibleWallHolder.transform.GetChild(0).localScale.y;
        alterHeight = height / alterHeight;
        Vector3 scale = invisibleWallHolder.transform.localScale;
        scale.y = alterHeight;
        invisibleWallHolder.transform.localScale = scale;
    }

    static Dictionary<int, List<LvlTraversalData>> stairMarkers = new Dictionary<int, List<LvlTraversalData>>();

    public static void GenerateStairs(int numSteps, LvlTraversalAction action)
    {
        AddLvlTraversalMarkerToList(stairMarkers, action, numSteps);
    }

    public static void ConnectStairs()
    {
        ConnectTraversables(stairMarkers, BuildStairs);
    }

    static void BuildStairs(LvlTraversalData stairStart, LvlTraversalData stairEnd)
    {
        // If the stairs are upside down, switch the start and end
        if (stairEnd.traversalObj.transform.position.y < stairStart.traversalObj.transform.position.y)
        {
            LvlTraversalData temp = stairStart;
            stairStart = stairEnd;
            stairEnd = temp;
        }

        GameObject stairsHolder = new GameObject("Stairs");
        stairsHolder.transform.position = stairStart.traversalObj.transform.position;
        GameObject firstStep = stairStart.traversalObj;
        // Clone the start of reusable stairs so reusing stairs get the same start data
        if (stairStart.action == LvlTraversalAction.REUSE)
        {
            firstStep = GameObject.Instantiate(firstStep);
        }   

        Vector3 stairModelsize = firstStep.GetComponent<MeshFilter>().sharedMesh.bounds.size;
        Vector3 diff = stairEnd.traversalObj.transform.position - firstStep.transform.position;
        float stairHeightMod = (diff.y / stairStart.additionalInt) / stairModelsize.y;
        diff.y = 0.0f;
        float stairDepth = (diff.magnitude / stairStart.additionalInt) / stairModelsize.x;

        Vector3 scale = new Vector3(stairDepth, stairHeightMod, firstStep.transform.localScale.z);
        firstStep.transform.localScale = scale;
        Vector3 pos = firstStep.transform.position;
        pos.x = (int)pos.x + stairDepth / 2.0f;
        pos.y += stairHeightMod / 2.0f;
        firstStep.transform.position = pos;
        firstStep.transform.SetParent(stairsHolder.transform, true);
        for(int i = 1; i < stairStart.additionalInt; ++i)
        {
            GameObject stair = GameObject.Instantiate(firstStep);
            scale.y = (i + 1) * stairHeightMod;
            stair.transform.localScale = scale;
            pos.x += stairDepth;
            pos.y += stairHeightMod / 2.0f;
            stair.transform.position = pos;
            stair.transform.SetParent(stairsHolder.transform, true);
        }

        Vector3 stairEndDir = new Vector3(diff.x, 0.0f, diff.z).normalized;
        Vector3 stairNowDir = Vector3.right;
        float theta = Mathf.Acos(Vector3.Dot(stairNowDir, stairEndDir)) * Mathf.Rad2Deg;
        if(Vector3.Cross(stairNowDir, stairEndDir).y < 0.0f)
        {
            theta = -theta;
        }  
        stairsHolder.transform.Rotate(new Vector3(0.0f, theta));
    }
}
