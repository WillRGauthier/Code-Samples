using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class XYGrid : Editor
{
    public float addLineButtonRecomputeTime = 2f;
    public float linePickSnapDist = 0.05f;
    public bool drawGrid = true;

    // -1 because it takes two added lines to add the first cell
    public int numRowsOfCells = -1;
    public int numColsOfCells = -1;

    SortedDictionary<float, InteractiveLine> horizontalLinesByY = new SortedDictionary<float, InteractiveLine>();
    SortedDictionary<float, InteractiveLine> verticalLinesByX = new SortedDictionary<float, InteractiveLine>();

    List<Vector4> isolatedCellMinMaxs = new List<Vector4>();

    float minX = Mathf.Infinity;
    float maxX = Mathf.NegativeInfinity;
    float minY = Mathf.Infinity;
    float maxY = Mathf.NegativeInfinity;

    float timeSinceButtonRecompute = 0.0f;
    Vector3 addLeftVerticalLineButtonPos, addRightVerticalLineButtonPos, addBottomHorizontalLineButtonPos, addTopHorizontalLineButtonPos;

    InteractiveLine selectedLine = null;
    InteractiveLine lastHoverLine = null;

    // Don't leak memory or suffer errors hopefully
    private void OnDestroy()
    {
        foreach (InteractiveLine line in horizontalLinesByY.Values)
        {
            DestroyImmediate(line);
        }
        horizontalLinesByY.Clear();
        foreach (InteractiveLine line in verticalLinesByX.Values)
        {
            DestroyImmediate(line);
        }
        verticalLinesByX.Clear();

        minX = Mathf.Infinity;
        maxX = Mathf.NegativeInfinity;
        minY = Mathf.Infinity;
        maxY = Mathf.NegativeInfinity;
        numRowsOfCells = -1;
        numColsOfCells = -1;
}

    // Don't leak memory or suffer errors hopefully
    void OnDisable()
    {
        foreach (InteractiveLine line in horizontalLinesByY.Values)
        {
            DestroyImmediate(line);
        }
        horizontalLinesByY.Clear();
        foreach (InteractiveLine line in verticalLinesByX.Values)
        {
            DestroyImmediate(line);
        }
        verticalLinesByX.Clear();

        minX = Mathf.Infinity;
        maxX = Mathf.NegativeInfinity;
        minY = Mathf.Infinity;
        maxY = Mathf.NegativeInfinity;
        numRowsOfCells = -1;
        numColsOfCells = -1;
    }

    public void OnSceneGUI()
    {
        if(drawGrid)
        {
            int controlID = EditorGUIUtility.GetControlID(FocusType.Passive);
            switch (Event.current.GetTypeForControl(controlID))
            {
                // On Layout event (the first event for initialization) record grid as being 0 units away if mouse within grid bounds
                //  else declare infinity away to steal clicks and avoid deselection
                case EventType.Layout:
                    if (selectedLine == null)
                    {
                        Vector3 worldMousePos = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).origin;
                        if (worldMousePos.x >= minX && worldMousePos.x <= maxX && worldMousePos.y >= minY && worldMousePos.y <= maxY)
                        {
                            HandleUtility.AddControl(controlID, 0f);
                        }
                        else
                        {
                            HandleUtility.AddControl(controlID, Mathf.Infinity);
                        }
                    }
                    else
                    {
                        HandleUtility.AddDefaultControl(controlID);
                    }
                    break;
                // All other events perform logic and draw like normal
                default:
                    HighlightHoveringLine();
                    if (Event.current.type == EventType.MouseUp)
                    {
                        if (selectedLine != null)
                        {
                            selectedLine.SetSelectState(InteractiveLine.SelectState.NONE);
                        }
                        selectedLine = lastHoverLine;
                        if (selectedLine != null)
                        {
                            selectedLine.SetSelectState(InteractiveLine.SelectState.SELECTED);
                            Event.current.Use();
                        }
                    }
                    if(selectedLine == null)
                    {
                        ShowAddLineButtons();
                    }
                    break;
            }
        }   
    }

    // Adds a line to the grid, computing if it's horizontal or vertical, and scales all lines to the same length
    InteractiveLine AddLine(Vector3 point1, Vector3 point2)
    {
        InteractiveLine line = ScriptableObject.CreateInstance<InteractiveLine>();
        InteractiveLine.Orientation orientation;
        if(point1.x == point2.x)
        {
            orientation = InteractiveLine.Orientation.VERTICAL;
            ++numColsOfCells;
        }
        else if(point1.y == point2.y)
        {
            orientation = InteractiveLine.Orientation.HORIZONTAL;
            ++numRowsOfCells;
        }
        else
        {
            Debug.LogError("XYGrid AddLine: Line isn't horizontal or vertical");
            DestroyImmediate(line);
            return null;
        }

        line.Init(point1, point2, this, orientation);
        if(orientation == InteractiveLine.Orientation.HORIZONTAL)
        {
            if(horizontalLinesByY.ContainsKey(point1.y))
            {
                DestroyImmediate(horizontalLinesByY[point1.y]);
                horizontalLinesByY.Remove(point1.y);
            }
            horizontalLinesByY.Add(point1.y, line);
        }
        else
        {
            if(verticalLinesByX.ContainsKey(point1.x))
            {
                DestroyImmediate(verticalLinesByX[point1.x]);
                verticalLinesByX.Remove(point1.x);
            }
            verticalLinesByX.Add(point1.x, line);
        }
        UpdateMinMaxs(line);
        RescaleLines();
        return line;
    }

    // Intended for loading grid from text file, doesn't normalize lines or check if dictionary already contains
    public void AddExistingLine(InteractiveLine line)
    {
        line.SetContainingGrid(this);
        if(line.orientation == InteractiveLine.Orientation.HORIZONTAL)
        {
            horizontalLinesByY.Add(line.point1.y, line);
            ++numRowsOfCells;
        }
        else
        {
            verticalLinesByX.Add(line.point1.x, line);
            ++numColsOfCells;
        }
        UpdateMinMaxs(line);
    }

    // Updates variables tracking min and max in X and Y and rescales lines to be same length
    void UpdateMinMaxs(InteractiveLine line)
    {
        float minLineX = Mathf.Min(line.point1.x, line.point2.x);
        float minLineY = Mathf.Min(line.point1.y, line.point2.y);
        float maxLineX = Mathf.Max(line.point1.x, line.point2.x);
        float maxLineY = Mathf.Max(line.point1.y, line.point2.y);

        if(minLineX < minX)
        {
            minX = minLineX;
        }
        if(maxLineX > maxX)
        {
            maxX = maxLineX;
        }
        if(minLineY < minY)
        {
            minY = minLineY;
        }
        if(maxLineY > maxY)
        {
            maxY = maxLineY;
        }
    }

    // Rescales lines to be uniform length based on tracked min and max in X and Y
    void RescaleLines()
    {
        foreach(InteractiveLine line in horizontalLinesByY.Values)
        {
            line.point1.x = minX;
            line.point2.x = maxX;
        }
        foreach(InteractiveLine line in verticalLinesByX.Values)
        {
            line.point1.y = minY;
            line.point2.y = maxY;
        }
    }

    void HighlightHoveringLine()
    {
        if(lastHoverLine != null && lastHoverLine != selectedLine)
        {
            lastHoverLine.SetSelectState(InteractiveLine.SelectState.NONE);
        }

        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        Vector3 worldMousePos = ray.origin;
        InteractiveLine closestLine = ApproximateBinarySearch(horizontalLinesByY, worldMousePos.y);
        if (closestLine == null)
        {
            closestLine = ApproximateBinarySearch(verticalLinesByX, worldMousePos.x);
        }
        if(closestLine != null && closestLine.selectState == InteractiveLine.SelectState.IMMOBILE)
        {
            closestLine = null;
        }
        if (closestLine != null && closestLine != selectedLine)
        {
            closestLine.SetSelectState(InteractiveLine.SelectState.HOVER);
        }
        lastHoverLine = closestLine;
    }

    InteractiveLine ApproximateBinarySearch(SortedDictionary<float, InteractiveLine> lineDict, float coordinate)
    {
        if(lineDict.Count > 0)
        {
            float[] lineCoordinates = lineDict.Keys.ToArray();
            bool exactMatchFound;
            int index = InterpretArrayBinarySearchIndex(Array.BinarySearch(lineCoordinates, coordinate), out exactMatchFound);
            if(exactMatchFound)
            {
                return lineDict[lineCoordinates[index]];
            }
            else
            {
                if (index == 0)
                {
                    if (Math.Abs(lineCoordinates[index] - coordinate) <= linePickSnapDist)
                    {
                        return lineDict[lineCoordinates[index]];
                    }
                    else
                    {
                        return null;
                    }
                }
                else if (index == lineCoordinates.Length)
                {
                    if (Mathf.Abs(lineCoordinates[index - 1] - coordinate) <= linePickSnapDist)
                    {
                        return lineDict[lineCoordinates[index - 1]];
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    if (Mathf.Abs(lineCoordinates[index - 1] - coordinate) <= linePickSnapDist)
                    {
                        return lineDict[lineCoordinates[index - 1]];
                    }
                    else if (Mathf.Abs(lineCoordinates[index] - coordinate) <= linePickSnapDist)
                    {
                        return lineDict[lineCoordinates[index]];
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }
        else
        {
            return null;
        }
    }

    int InterpretArrayBinarySearchIndex(int index, out bool isExactMatch)
    {
        if(index < 0)
        {
            isExactMatch = false;
            index = ~index;
        }
        else
        {
            isExactMatch = true;
        }
        return index;
    }

    void ShowAddLineButtons()
    {
        float[] verticalLineXs = verticalLinesByX.Keys.ToArray();
        float[] horizontalLineYs = horizontalLinesByY.Keys.ToArray();
        float minVerticalX = verticalLineXs.Length > 0 ? verticalLineXs[0] : Mathf.NegativeInfinity;
        float maxVerticalX = verticalLineXs.Length > 0 ? verticalLineXs[verticalLineXs.Length - 1] : Mathf.Infinity;
        float minHorizontalY = horizontalLineYs.Length > 0 ? horizontalLineYs[0] : Mathf.NegativeInfinity;
        float maxHorizontalY = horizontalLineYs.Length > 0 ? horizontalLineYs[horizontalLineYs.Length - 1] : Mathf.Infinity;

        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        Vector3 worldMousePos = ray.origin;
        timeSinceButtonRecompute += Time.deltaTime;
        if(timeSinceButtonRecompute >= addLineButtonRecomputeTime)
        {
            addLeftVerticalLineButtonPos = HandleUtility.WorldToGUIPoint(new Vector3(minVerticalX, worldMousePos.y, 0f));
            addRightVerticalLineButtonPos = HandleUtility.WorldToGUIPoint(new Vector3(maxVerticalX, worldMousePos.y, 0f));
            addBottomHorizontalLineButtonPos = HandleUtility.WorldToGUIPoint(new Vector3(worldMousePos.x, minHorizontalY, 0f));
            addTopHorizontalLineButtonPos = HandleUtility.WorldToGUIPoint(new Vector3(worldMousePos.x, maxHorizontalY, 0f));
            timeSinceButtonRecompute = 0.0f;
        }
        
        // If mouse is left of leftmost vertical line
        if (worldMousePos.x < minVerticalX)
        {
            Handles.BeginGUI();
            if(GUI.Button(new Rect(addLeftVerticalLineButtonPos.x - 50f, addLeftVerticalLineButtonPos.y, 40f, 20f), "+V"))
            {
                // Add vertical line on the left
                AddLine(new Vector3(worldMousePos.x, minY, 0f), new Vector3(worldMousePos.x, maxY, 0f));
            }
            Handles.EndGUI();
        }
        // If mouse is right of rightmost vertical line
        else if(worldMousePos.x > maxVerticalX)
        {
            Handles.BeginGUI();
            if (GUI.Button(new Rect(addRightVerticalLineButtonPos.x + 10f, addRightVerticalLineButtonPos.y, 40f, 20f), "+V"))
            {
                // Add vertical line on right
                AddLine(new Vector3(worldMousePos.x, minY, 0f), new Vector3(worldMousePos.x, maxY, 0f));
            }
            Handles.EndGUI();
        }
        // If mouse is below bottom horizontal line
        if(worldMousePos.y < minHorizontalY)
        {
            Handles.BeginGUI();
            if(GUI.Button(new Rect(addBottomHorizontalLineButtonPos.x, addBottomHorizontalLineButtonPos.y + 20f, 40f, 20f), "+H"))
            {
                // Add horizontal line on bottom
                AddLine(new Vector3(minX, worldMousePos.y, 0f), new Vector3(maxX, worldMousePos.y, 0f));
            }
            Handles.EndGUI();
        }
        // If mouse is above top horizontal line
        else if(worldMousePos.y > maxHorizontalY)
        {
            Handles.BeginGUI();
            if (GUI.Button(new Rect(addTopHorizontalLineButtonPos.x, addTopHorizontalLineButtonPos.y - 40f, 40f, 20f), "+H"))
            {
                // Add horizontal line on top
                AddLine(new Vector3(minX, worldMousePos.y, 0f), new Vector3(maxX, worldMousePos.y, 0f));
            }
            Handles.EndGUI();
        }
    }

    public void SaveGrid(string fireManagerID)
    {
        // Manually convert every line to JSON because I can't get collections to work
        string saveData = "";
        foreach(InteractiveLine line in verticalLinesByX.Values)
        {
            saveData += JsonUtility.ToJson(line) + "|";
        }
        foreach(InteractiveLine line in horizontalLinesByY.Values)
        {
            saveData += JsonUtility.ToJson(line) + "|";
        }
        saveData = saveData.TrimEnd('|');
        if (!AssetDatabase.IsValidFolder("Assets/Editor/FireGrids"))
        {
            AssetDatabase.CreateFolder("Assets/Editor", "FireGrids");
        }
		System.IO.StreamWriter sw = new System.IO.StreamWriter ("Assets/Editor/FireGrids/" + fireManagerID + ".txt", false);
		sw.Write(saveData);
		sw.Close();
		AssetDatabase.Refresh();
    }

    public bool LoadGrid(string fireManagerID)
    {
        TextAsset gridText = AssetDatabase.LoadAssetAtPath("Assets/Editor/FireGrids/" + fireManagerID + ".txt", typeof(TextAsset)) as TextAsset;
        if (gridText)
        {
            string allLines = gridText.text;
            string[] eachLine = allLines.Split('|');
            foreach(string line in eachLine)
            {
                InteractiveLine interactiveLine = CreateInstance<InteractiveLine>();
                JsonUtility.FromJsonOverwrite(line, interactiveLine);
                AddExistingLine(interactiveLine);
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    // Creates a 3x3 grid of unit squares centered around the origin
    public void CreateDefault()
    {
        Vector3[] points = { new Vector3(-1.0f, -1.0f, 0.0f), new Vector3(-1.0f, 1.0f, 0.0f), new Vector3(1.0f, 1.0f, 0.0f), new Vector3(1.0f, -1.0f, 0.0f) };
        AddLine(points[0] - (points[1] - points[0]) / 2.0f, points[1] + (points[1] - points[0]) / 2.0f);
        AddLine(points[2] - (points[3] - points[2]) / 2.0f, points[3] + (points[3] - points[2]) / 2.0f);
        AddLine(points[0] - (points[3] - points[0]) / 2.0f, points[3] + (points[3] - points[0]) / 2.0f);
        AddLine(points[1] - (points[2] - points[1]) / 2.0f, points[2] + (points[2] - points[1]) / 2.0f);
    }

    // Adds a cell to the grid where other lines cannot pass inside of
    // Paramter cellCorners is array of 4 Vector3 representing corners of cell in order bottom left, top left, top right, bottom right
    public void AddIsolatedCell(Vector3[] cellCorners)
    {
        isolatedCellMinMaxs.Add(new Vector4(cellCorners[0].x, cellCorners[2].x, cellCorners[0].y, cellCorners[2].y));

        // Remove any non-immobile lines inside the new isolated cell area
        float[] xCoordinates = verticalLinesByX.Keys.ToArray();
        float[] yCoordinates = horizontalLinesByY.Keys.ToArray();
        bool exactMatchFound;
        int lowerXBoundIndex = InterpretArrayBinarySearchIndex(Array.BinarySearch(xCoordinates, cellCorners[0].x), out exactMatchFound);
        int upperXBoundIndex = InterpretArrayBinarySearchIndex(Array.BinarySearch(xCoordinates, cellCorners[2].x), out exactMatchFound);
        int lowerYBoundIndex = InterpretArrayBinarySearchIndex(Array.BinarySearch(yCoordinates, cellCorners[0].y), out exactMatchFound);
        int upperYBoundIndex = InterpretArrayBinarySearchIndex(Array.BinarySearch(yCoordinates, cellCorners[2].y), out exactMatchFound);
        for (; lowerXBoundIndex < upperXBoundIndex; ++lowerXBoundIndex)
        {
            if (lowerXBoundIndex < xCoordinates.Length && verticalLinesByX[xCoordinates[lowerXBoundIndex]].selectState != InteractiveLine.SelectState.IMMOBILE)
            {
                DestroyImmediate(verticalLinesByX[xCoordinates[lowerXBoundIndex]]);
                verticalLinesByX.Remove(xCoordinates[lowerXBoundIndex]);
            }
        }
        for (; lowerYBoundIndex < upperYBoundIndex; ++lowerYBoundIndex)
        {
            if(lowerYBoundIndex < yCoordinates.Length && horizontalLinesByY[yCoordinates[lowerYBoundIndex]].selectState != InteractiveLine.SelectState.IMMOBILE)
            {
                DestroyImmediate(horizontalLinesByY[yCoordinates[lowerYBoundIndex]]);
                horizontalLinesByY.Remove(yCoordinates[lowerYBoundIndex]);
            }
        }

        InteractiveLine line = AddLine(cellCorners[0], cellCorners[1]);
        line.SetSelectState(InteractiveLine.SelectState.IMMOBILE);
        line = AddLine(cellCorners[1], cellCorners[2]);
        line.SetSelectState(InteractiveLine.SelectState.IMMOBILE);
        line = AddLine(cellCorners[3], cellCorners[2]);
        line.SetSelectState(InteractiveLine.SelectState.IMMOBILE);
        line = AddLine(cellCorners[0], cellCorners[3]);
        line.SetSelectState(InteractiveLine.SelectState.IMMOBILE);
    }

    // Removes an isolated cell from the grid
    public void RemoveIsolatedCell(Vector3[] cellCorners)
    {
        foreach(Vector4 minMaxs in isolatedCellMinMaxs)
        {
            if(Mathf.Approximately(cellCorners[0].x, minMaxs.x) && Mathf.Approximately(cellCorners[2].x, minMaxs.y) && Mathf.Approximately(cellCorners[0].y, minMaxs.z) && Mathf.Approximately(cellCorners[2].y, minMaxs.w))
            {
                isolatedCellMinMaxs.Remove(minMaxs);
                verticalLinesByX[minMaxs.x].selectState = InteractiveLine.SelectState.NONE;
                verticalLinesByX[minMaxs.y].selectState = InteractiveLine.SelectState.NONE;
                horizontalLinesByY[minMaxs.z].selectState = InteractiveLine.SelectState.NONE;
                horizontalLinesByY[minMaxs.w].selectState = InteractiveLine.SelectState.NONE;
                return;
            }
        }
        Debug.LogError("Trying to remove non-existant isolated cell from grid");
    }

    public void NotifyLineMoved(InteractiveLine line, float oldCoordinate)
    {
        if (line.orientation == InteractiveLine.Orientation.HORIZONTAL)
        {
            horizontalLinesByY.Remove(oldCoordinate);
            --numRowsOfCells;
        }
        else
        {
            verticalLinesByX.Remove(oldCoordinate);
            --numColsOfCells;
        }

        // Destroy lines that move into isolated cells
        foreach(Vector4 minMaxs in isolatedCellMinMaxs)
        {
            if(line.orientation == InteractiveLine.Orientation.HORIZONTAL)
            {
                if(line.point1.y >= minMaxs.z && line.point1.y <= minMaxs.w)
                {
                    DestroyImmediate(line);
                    return;
                }
            }
            else
            {
                if(line.point1.x >= minMaxs.x && line.point1.x <= minMaxs.y)
                {
                    DestroyImmediate(line);
                    return;
                }
            }
        }

        AddExistingLine(line);
        RescaleLines();
    }

    public void NotifyLineDeleting(InteractiveLine line)
    {
        if(line.orientation == InteractiveLine.Orientation.HORIZONTAL)
        {
            horizontalLinesByY.Remove(line.point1.y);
        }
        else
        {
            verticalLinesByX.Remove(line.point1.x);
        }
    }

    public void NotifyToDrawChanged()
    {
        foreach(InteractiveLine line in horizontalLinesByY.Values)
        {
            line.RegisterForDisplay(drawGrid);
        }
        foreach(InteractiveLine line in verticalLinesByX.Values)
        {
            line.RegisterForDisplay(drawGrid);
        }
    }

    public List<Bounds> BakeGrid()
    {
        List<Bounds> retBakedBounds = new List<Bounds>();
        Vector3 cellCenter = Vector3.zero;
        Vector3 cellSize = Vector3.zero;

        // Create Bounds for all isolated cells first
        List<Bounds> isolatedCellBounds = new List<Bounds>();
        foreach(Vector4 minMaxs in isolatedCellMinMaxs)
        {
            cellCenter.x = (minMaxs.x + minMaxs.y) / 2f;
            cellCenter.y = (minMaxs.z + minMaxs.w) / 2f;
            cellSize.x = minMaxs.y - minMaxs.x;
            cellSize.y = minMaxs.w - minMaxs.z;
            isolatedCellBounds.Add(new Bounds(cellCenter, cellSize));
        }

        // Get all lines
        float[] verticalItersectors = verticalLinesByX.Keys.ToArray();
        int numCols = verticalItersectors.Length >= 2 ? verticalItersectors.Length - 1 : 0;
        float[] horizontalIntersectors = horizontalLinesByY.Keys.ToArray();
        int numRows = horizontalIntersectors.Length >= 2 ? horizontalIntersectors.Length - 1 : 0;
        // Traverse them left to right, bottom to top creating bounds for each cell
        InteractiveLine leftVertBoundingLine, rightVertBoundingLine, bottomHorizBoundingLine, topHorizBoundingLine;
        for(int row = 0; row < numRows; ++row)
        {
            for(int col = 0; col < numCols; ++col)
            {
                leftVertBoundingLine = verticalLinesByX[verticalItersectors[col]];
                rightVertBoundingLine = verticalLinesByX[verticalItersectors[col + 1]];
                bottomHorizBoundingLine = horizontalLinesByY[horizontalIntersectors[row]];
                topHorizBoundingLine = horizontalLinesByY[horizontalIntersectors[row + 1]];

                bool cellCenterIsInIsolatedCell = false;
                cellCenter.x = (leftVertBoundingLine.point1.x + rightVertBoundingLine.point1.x) / 2f;
                cellCenter.y = (bottomHorizBoundingLine.point1.y + topHorizBoundingLine.point1.y) / 2f;
                foreach(Bounds isolatedCell in isolatedCellBounds)
                {
                    if(isolatedCell.Contains(cellCenter))
                    {
                        cellCenterIsInIsolatedCell = true;
                        if(!retBakedBounds.Contains(isolatedCell))
                        {
                            retBakedBounds.Add(isolatedCell);
                        }
                        break;
                    }
                }
                if(!cellCenterIsInIsolatedCell)
                {
                    cellSize.x = rightVertBoundingLine.point1.x - leftVertBoundingLine.point1.x;
                    cellSize.y = topHorizBoundingLine.point1.y - bottomHorizBoundingLine.point1.y;
                    retBakedBounds.Add(new Bounds(cellCenter, cellSize));
                }
            }
        }

        return retBakedBounds;
    }

    public void ResetGrid()
    {
        OnDestroy();
        CreateDefault();
    }
}
