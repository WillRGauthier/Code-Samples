using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(PathingAgent))]
[CanEditMultipleObjects]
public class PathingAgentEditor : Editor
{
    PathingAgent agent;
    ReorderableList waypointsList;
    Dictionary<string, ReorderableList> connectedWaypointsListDict = new Dictionary<string, ReorderableList>();
    List<bool> showWaypointSubInfo = new List<bool>();
    List<bool> showRandomWaypointConns = new List<bool>();
    bool showNonNavMeshVariables = false;

    public void OnEnable()
    {
        agent = (PathingAgent)target;
        PathingAgent.lastSelectedPathingAgent = null;

        // Cache common variables
        SerializedProperty connectedWaypointsProperty = serializedObject.FindProperty("connectedWaypoints");
        SerializedProperty waypointsProperty = serializedObject.FindProperty("waypoints");

        // Make reordable waypoints list
        waypointsList = new ReorderableList(serializedObject, waypointsProperty);
        waypointsList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Waypoints");
        };
        // Expand relevant paired lists when adding a new waypoint
        waypointsList.onAddCallback = (ReorderableList list) =>
        {
            showWaypointSubInfo.Add(false);
            showRandomWaypointConns.Add(false);
            connectedWaypointsProperty.arraySize += 1;

            waypointsProperty.arraySize += 1;
            waypointsList.index = waypointsProperty.arraySize - 1;
        };
        // Shrink relevant paired lists when deleting a waypoint
        waypointsList.onRemoveCallback = (ReorderableList list) =>
        {
            Object waypoint = waypointsProperty.GetArrayElementAtIndex(list.index).objectReferenceValue;
            // If there is a waypoint, set it to null
            if(waypoint != null)
            {
                waypointsProperty.DeleteArrayElementAtIndex(list.index);
            }
            // If it's null, delete the item and its place in the relevant paired lists
            else
            {
                waypointsProperty.DeleteArrayElementAtIndex(list.index);
                connectedWaypointsProperty.DeleteArrayElementAtIndex(list.index);
                showWaypointSubInfo.RemoveAt(list.index);
                showRandomWaypointConns.RemoveAt(list.index);
            }
        };
        // Handle reordering relevant paired lists when reordering waypoints
        waypointsList.onReorderCallbackWithDetails = (ReorderableList list, int oldIndex, int newIndex) =>
        {
            connectedWaypointsProperty.MoveArrayElement(oldIndex, newIndex);

            bool temp = showWaypointSubInfo[oldIndex];
            showWaypointSubInfo[oldIndex] = showWaypointSubInfo[newIndex];
            showWaypointSubInfo[newIndex] = temp;

            temp = showRandomWaypointConns[oldIndex];
            showRandomWaypointConns[oldIndex] = showRandomWaypointConns[newIndex];
            showRandomWaypointConns[newIndex] = temp;
        };
        // Visualize waypoints list
        waypointsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            float singleItemHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            float cumulativeHeight = 0;

            var element = waypointsProperty.GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), element, new GUIContent("Waypoint " + index.ToString()));
            cumulativeHeight += singleItemHeight;

            // Make non-reordable connectedWaypoints list per waypoint
            SerializedProperty connectionsProperty = connectedWaypointsProperty.GetArrayElementAtIndex(index).FindPropertyRelative("connections");
            string listKey = connectionsProperty.propertyPath;
            ReorderableList connectedWaypointsList;
            if(connectedWaypointsListDict.ContainsKey(listKey))
            {
                connectedWaypointsList = connectedWaypointsListDict[listKey];
            }
            else
            {
                connectedWaypointsList = new ReorderableList(connectionsProperty.serializedObject, connectionsProperty, false, true, true, true);
                connectedWaypointsList.drawHeaderCallback = (Rect innerRect) =>
                {
                    EditorGUI.LabelField(innerRect, "Connected Waypoints");
                };
                connectedWaypointsList.drawElementCallback = (Rect innerRect, int innerIndex, bool innerIsActive, bool innerIsFocused) =>
                {
                    var innerElement = connectionsProperty.GetArrayElementAtIndex(innerIndex);
                    EditorGUI.PropertyField(new Rect(innerRect.x, innerRect.y, innerRect.width, connectionsProperty.arraySize * EditorGUIUtility.singleLineHeight), innerElement, GUIContent.none, true);
                };
                connectedWaypointsListDict[listKey] = connectedWaypointsList;
            }

            showWaypointSubInfo[index] = EditorGUI.Foldout(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight, rect.width, EditorGUIUtility.singleLineHeight), showWaypointSubInfo[index], "Per Waypoint Settings");
            if(showWaypointSubInfo[index])
            {
                cumulativeHeight += 2 * singleItemHeight;
                showRandomWaypointConns[index] = GUI.Toggle(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * 2, rect.width, EditorGUIUtility.singleLineHeight), showRandomWaypointConns[index], "Draw Random Waypoint Connections");

                connectedWaypointsList.DoList(new Rect(rect.x, rect.y + singleItemHeight * 3, rect.width, EditorGUIUtility.singleLineHeight));
                cumulativeHeight += EditorGUI.GetPropertyHeight(connectionsProperty);
                cumulativeHeight += (connectionsProperty.arraySize + 1) * singleItemHeight;
                cumulativeHeight += waypointsList.headerHeight + waypointsList.footerHeight;

                // If a waypoint is assigned, show the onEnter/onExit events
                if (element.objectReferenceValue)
                {
                    SerializedObject waypointSerializedObject = new SerializedObject(element.objectReferenceValue);
                    waypointSerializedObject.Update();

                    SerializedProperty waypointEnterProperty = waypointSerializedObject.FindProperty("onWaypointEnter");
                    EditorGUI.PropertyField(new Rect(rect.x, rect.y + cumulativeHeight, rect.width, EditorGUIUtility.singleLineHeight),
                                            waypointEnterProperty,
                                            new GUIContent("On Waypoint Enter"));
                    cumulativeHeight += EditorGUI.GetPropertyHeight(waypointEnterProperty);
                    EditorGUI.PropertyField(new Rect(rect.x, rect.y + cumulativeHeight, rect.width, EditorGUIUtility.singleLineHeight),
                                            waypointSerializedObject.FindProperty("onWaypointExit"),
                                            new GUIContent("On Waypoint Exit"));

                    waypointSerializedObject.ApplyModifiedProperties();
                }
            }
        };
        waypointsList.elementHeightCallback = (int index) =>
        {
            var waypoint = waypointsProperty.GetArrayElementAtIndex(index);
            float waypointHeight = EditorGUI.GetPropertyHeight(waypoint);
            // Spacing for the foldout
            waypointHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            if(showWaypointSubInfo[index])
            {
                // Spacing for the random connection drawing toggle
                waypointHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                SerializedProperty connectionsProperty = connectedWaypointsProperty.GetArrayElementAtIndex(index).FindPropertyRelative("connections");
                waypointHeight += EditorGUI.GetPropertyHeight(connectionsProperty);
                waypointHeight += (connectionsProperty.arraySize + 1) * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
                waypointHeight += waypointsList.footerHeight + waypointsList.headerHeight;

                if (waypoint.objectReferenceValue)
                {
                    SerializedObject waypointSerializedObject = new SerializedObject(waypoint.objectReferenceValue);
                    waypointHeight += EditorGUI.GetPropertyHeight(waypointSerializedObject.FindProperty("onWaypointEnter"));
                    waypointHeight += EditorGUI.GetPropertyHeight(waypointSerializedObject.FindProperty("onWaypointExit"));
                }
            }

            return waypointHeight;
        };
        
        while(showWaypointSubInfo.Count < waypointsProperty.arraySize)
        {
            showWaypointSubInfo.Add(false);
        }
        while(showRandomWaypointConns.Count < waypointsProperty.arraySize)
        {
            showRandomWaypointConns.Add(false);
        }
    }

    public void OnDisable()
    {
        PathingAgent.lastSelectedPathingAgent = agent;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        GUI.enabled = false;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"), true, new GUILayoutOption[0]);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("currentWaypointTarget"));
        GUI.enabled = true;

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("tetherObjectTransform"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("tetherRadius"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("tetherDirectionStartAngle"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("tetherDirectionEndAngle"));

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("orientParallelToSlopes"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("minSlopeReorientAngle"));

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("pathingMode"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("goRndWaypointChance"));
        waypointsList.DoLayoutList();
        if(GUILayout.Button("Generate Waypoint Collection Asset"))
        {
            SCR_WaypointCollection waypointCollection = ScriptableObject.CreateInstance<SCR_WaypointCollection>();
            waypointCollection.Initialize(agent.waypoints, agent.connectedWaypoints);
            if(!AssetDatabase.IsValidFolder("Assets/Mara_Data"))
            {
                AssetDatabase.CreateFolder("Assets", "Mara_Data");
            }
            string path = EditorUtility.SaveFilePanel("Save waypoint collection asset", "Assets/Mara_Data", agent.gameObject.name + "_waypointCollection", "asset");
            if(path.Length > 0)
            {
                int assetsFolderPathIndex = path.IndexOf("Assets");
                path = path.Substring(assetsFolderPathIndex);
                AssetDatabase.CreateAsset(waypointCollection, path);
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = waypointCollection;
            }
        }
        if(GUILayout.Button("Load Waypoint Collection Asset"))
        {
            if(!AssetDatabase.IsValidFolder("Assets/Mara_Data"))
            {
                AssetDatabase.CreateFolder("Assets", "Mara_Data");
            }
            string path = EditorUtility.OpenFilePanelWithFilters("Load waypoint collection asset", "Assets/Mara_Data", new string[] { "Asset", "asset" });
            if(path.Length > 0)
            {
                int assetsFolderPathIndex = path.IndexOf("Assets");
                path = path.Substring(assetsFolderPathIndex);
                SCR_WaypointCollection waypointCollection = (SCR_WaypointCollection)AssetDatabase.LoadAssetAtPath(path, typeof(SCR_WaypointCollection));
                if(waypointCollection)
                {
                    agent.LoadWaypointCollection(waypointCollection);
                    Selection.activeObject = agent;
                }
            }
        }
        EditorGUILayout.PropertyField(serializedObject.FindProperty("allWaypointsOnEnter"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("allWaypointsOnExit"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("nextWaypointIndex"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rotateAtWaypoints"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("turnDegreesPerSec"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("beginPathingOnStart"));

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("navFailureResponse"));
        showNonNavMeshVariables = EditorGUILayout.Foldout(showNonNavMeshVariables, "Non-NavMesh Settings");
        PathingAgent.NavFailureResponse failureResponse = (PathingAgent.NavFailureResponse)serializedObject.FindProperty("navFailureResponse").enumValueIndex;
        if(showNonNavMeshVariables || failureResponse == PathingAgent.NavFailureResponse.LERP)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("inheritSpeedFromNavAgent"));;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("moveSpeed"));
        }

        // Only allow waypoints to be listed as a connectedWaypoint once
        HashSet<WaypointObject> uniqueConnectedWaypoints = new HashSet<WaypointObject>();
        for (int i = 0; i < agent.connectedWaypoints.Count; ++i)
        {
            for (int w = 0; w < agent.connectedWaypoints[i].connections.Count; ++w)
            {
                if (uniqueConnectedWaypoints.Contains(agent.connectedWaypoints[i].connections[w]))
                {
                    agent.connectedWaypoints[i].connections[w] = null;
                }
                else
                {
                    uniqueConnectedWaypoints.Add(agent.connectedWaypoints[i].connections[w]);
                }
            }
            uniqueConnectedWaypoints.Clear();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void OnSceneGUI()
    {
        Color oldColor = Handles.color;
        GUIStyle style = new GUIStyle();

        for (int w = agent.waypoints.Count - 1; w >= 0; --w)
        {
            if(agent.waypoints[w])
            {
                Handles.color = Color.cyan;
                style.normal.textColor = Handles.color;
                Handles.Label(agent.waypoints[w].transform.position + Vector3.up, (w + 1).ToString(), style);
                int prevNonNullWaypointIndex = w - 1;
                while(prevNonNullWaypointIndex >= 0 && agent.waypoints[prevNonNullWaypointIndex] == null)
                {
                    --prevNonNullWaypointIndex;
                }
                if(prevNonNullWaypointIndex >= 0)
                {
                    Handles.DrawAAPolyLine(7.5f, new Vector3[] { agent.waypoints[w].transform.position, agent.waypoints[prevNonNullWaypointIndex].transform.position });
                    //Handles.DrawLine(agent.waypoints[w].transform.position, agent.waypoints[prevNonNullWaypointIndex].transform.position);
                }

                if(showRandomWaypointConns[w])
                {
                    Handles.color = new Color((float)w / agent.waypoints.Count, 0, 0);
                    style.normal.textColor = Handles.color;
                    if (agent.connectedWaypoints.Count > w && agent.connectedWaypoints[w] != null)
                    {
                        for (int cw = 0; cw < agent.connectedWaypoints[w].connections.Count; ++cw)
                        {
                            if (agent.connectedWaypoints[w].connections[cw] != null)
                            {
                                Handles.DrawAAPolyLine(3.75f, new Vector3[] { agent.waypoints[w].transform.position, agent.connectedWaypoints[w].connections[cw].transform.position });
                                Vector3 dist = agent.connectedWaypoints[w].connections[cw].transform.position - agent.waypoints[w].transform.position;
                                Handles.Label(agent.waypoints[w].transform.position + dist.normalized * (dist.magnitude / 2f),
                                    agent.waypoints[w].name + "->" + agent.connectedWaypoints[w].connections[cw].name,
                                    style);
                                //Handles.DrawLine(agent.waypoints[w].transform.position, agent.connectedWaypoints[w].connections[cw].transform.position);
                            }
                        }
                    }
                }
            }
        }
        Handles.color = oldColor;
    }
}
