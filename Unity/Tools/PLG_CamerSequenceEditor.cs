using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(SCR_CameraSequence))]
public class PLG_CamerSequenceEditor : Editor
{
    [MenuItem("Tools/Level/Grab scene camera transform %g")]
    public static void CaptureSceneCamera()
    {
        CaptureCameraHelper(SceneView.lastActiveSceneView.camera);
    }

    [MenuItem("Tools/Level/Grab selected camera transform %&g")]
    public static void CaptureSelectedCamera()
    {
        var selectedCamGO = Selection.activeGameObject;
        if(selectedCamGO == null)
        {
            Debug.LogError("No selected game object - a camera must be selected to grab its transform");
            return;
        }

        var selectedCam = selectedCamGO.GetComponent<Camera>();
        if(selectedCam == null)
        {
            Debug.LogError("Selected game object is not a camera - a camera must be selected to grab its transform");
        }

        CaptureCameraHelper(selectedCam);
    }

    private static void CaptureCameraHelper(Camera cam)
    {
        var selectedCameraSequence = Selection.activeObject as SCR_CameraSequence;
        if (selectedCameraSequence != null)
        {
            selectedCameraSequence.AddCameraKey(cam.transform.position, cam.transform.rotation.eulerAngles);
        }
        else
        {
            var openEditorWindows = Resources.FindObjectsOfTypeAll<PLG_CameraSequenceAdjustWindow>();
            if (openEditorWindows.Length > 0)
            {
                var editedSequences = new List<SCR_CameraSequence>();
                foreach (var window in openEditorWindows)
                {
                    editedSequences.Add(window.targetCameraSequence);
                    window.targetCameraSequence.AddCameraKey(cam.transform.position, cam.transform.rotation.eulerAngles);
                }
                PLG_CameraSequenceAdjustWindow.CloseWindow();
                foreach (var sequence in editedSequences)
                {
                    PLG_CameraSequenceAdjustWindow.OpenWindow(sequence);
                }
            }
            else
            {
                var newSeqenceAsset = CreateInstance<SCR_CameraSequence>();
                newSeqenceAsset.AddCameraKey(cam.transform.position, cam.transform.rotation.eulerAngles);
                var path = AssetDatabase.GenerateUniqueAssetPath("Assets/CameraSequence.asset");
                AssetDatabase.CreateAsset(newSeqenceAsset, path);
                AssetDatabase.Refresh();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = newSeqenceAsset;
            }
        }
    }

    private const float SUBSPACING = 5f;
    private const float SPACING = 10f;

    private ReorderableList reorderableList;

    private void OnEnable()
    {
        PLG_CameraSequenceAdjustWindow.CloseWindow();

        reorderableList = new ReorderableList(serializedObject, serializedObject.FindProperty("keys"), true, true, true, true);
        reorderableList.drawHeaderCallback = DrawHeaderCallback;
        reorderableList.drawElementCallback = DrawElementCallback;
        reorderableList.elementHeightCallback += ElementHeightCallback;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        reorderableList.DoLayoutList();

        if(GUILayout.Button("Launch Editor Window"))
        {
            PLG_CameraSequenceAdjustWindow.OpenWindow((SCR_CameraSequence)target);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawHeaderCallback(Rect rect)
    {
        EditorGUI.LabelField(rect, "Camera Transform Keys");
    }

    private void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
    {
        var element = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
        var positionProp = element.FindPropertyRelative("position");
        var eulerAnglesProp = element.FindPropertyRelative("eulerAnglesRotation");
        EditorGUI.PropertyField(new Rect(rect.x, rect.y, Screen.width * 0.8f, EditorGUIUtility.singleLineHeight), positionProp);
        EditorGUI.PropertyField(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight + SUBSPACING, Screen.width * 0.8f, EditorGUIUtility.singleLineHeight), eulerAnglesProp);
        EditorGUI.PropertyField(new Rect(rect.x, rect.y + 2f * EditorGUIUtility.singleLineHeight + 2f * SUBSPACING, Screen.width * 0.8f, EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("isCurvedConnector"));

        var sceneCam = SceneView.lastActiveSceneView.camera;
        var oldPosition = sceneCam.transform.position;
        var oldRotation = sceneCam.transform.rotation;
        sceneCam.transform.SetPositionAndRotation(positionProp.vector3Value, Quaternion.Euler(eulerAnglesProp.vector3Value));

        var tempRenderTexture = RenderTexture.GetTemporary(1280, 720, 16);
        var oldTargetTexture = sceneCam.targetTexture;
        sceneCam.targetTexture = tempRenderTexture;
        sceneCam.Render();
        sceneCam.targetTexture = oldTargetTexture;

        GUI.DrawTexture(new Rect(rect.x, rect.y + 3f * EditorGUIUtility.singleLineHeight + 3f * SUBSPACING, 160f, 90f), tempRenderTexture, ScaleMode.ScaleToFit, false);
        RenderTexture.ReleaseTemporary(tempRenderTexture);

        sceneCam.transform.SetPositionAndRotation(oldPosition, oldRotation);
    }

    private float ElementHeightCallback(int index)
    {
        var element = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
        var propertyHeight = EditorGUI.GetPropertyHeight(element.FindPropertyRelative("position"));
        propertyHeight += EditorGUI.GetPropertyHeight(element.FindPropertyRelative("eulerAnglesRotation"));
        propertyHeight += 90f;
        propertyHeight += EditorGUI.GetPropertyHeight(element.FindPropertyRelative("isCurvedConnector"));

        var spacing = 3f * SUBSPACING + SPACING;
        return propertyHeight + spacing;
    }
}
