using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PLG_CameraSequenceAdjustWindow : EditorWindow
{
    public SCR_CameraSequence targetCameraSequence;
    private Camera[] previewCameras;

    private float playbackLerpJumpDist = 1f;

    private bool isPlayingBack;
    private int currentPlaybackCamera;
    private float playbackOffset;
    private float convertedPlaybackJumpDist;

    public static void OpenWindow(SCR_CameraSequence inTargetCameraSequence)
    {
        var window = GetWindow(typeof(PLG_CameraSequenceAdjustWindow), false, "Camera Sequence Editor") as PLG_CameraSequenceAdjustWindow;
        window.targetCameraSequence = inTargetCameraSequence;
        Selection.activeObject = null;

        window.previewCameras = new Camera[window.targetCameraSequence.keys.Length];
        for (var c = 0; c < window.previewCameras.Length; ++c)
        {
            window.previewCameras[c] = EditorUtility.CreateGameObjectWithHideFlags("PreviewCamera", HideFlags.DontSave).AddComponent<Camera>();
            window.previewCameras[c].transform.SetPositionAndRotation(window.targetCameraSequence.keys[c].position, Quaternion.Euler(window.targetCameraSequence.keys[c].eulerAnglesRotation));
        }
    }

    public static void CloseWindow()
    {
        var openWindows = Resources.FindObjectsOfTypeAll<PLG_CameraSequenceAdjustWindow>();
        foreach(var window in openWindows)
        {
            window.Close();
        }
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;

        foreach(var cam in previewCameras)
        {
            DestroyImmediate(cam.gameObject);
        }
    }

    private void Update()
    {
        if(isPlayingBack)
        {
            playbackOffset += convertedPlaybackJumpDist;// * Time.deltaTime;
            if(playbackOffset >= 1)
            {
                playbackOffset = 0f;
                ++currentPlaybackCamera;
                targetCameraSequence.ConvertPlaybackLerpJumpDist(playbackLerpJumpDist, currentPlaybackCamera);
                if(currentPlaybackCamera >= previewCameras.Length)
                {
                    isPlayingBack = false;
                }
            }

            Repaint();
        }

        for(var c = 0; c < previewCameras.Length; ++c)
        {
            if(previewCameras[c].transform.position != targetCameraSequence.keys[c].position || previewCameras[c].transform.rotation.eulerAngles != targetCameraSequence.keys[c].eulerAnglesRotation)
            {
                targetCameraSequence.keys[c].position = previewCameras[c].transform.position;
                targetCameraSequence.keys[c].eulerAnglesRotation = previewCameras[c].transform.rotation.eulerAngles;
                Undo.RecordObject(targetCameraSequence, string.Concat("Adjusted preview camera ", c.ToString()));
            }
        }
    }

    private void OnGUI()
    {
        if(isPlayingBack)
        {
            var sceneCam = SceneView.lastActiveSceneView.camera;

            var oldPosition = sceneCam.transform.position;
            var oldRotation = sceneCam.transform.rotation;

            targetCameraSequence.GetLerpSpot(out var position, out var rotation, currentPlaybackCamera, playbackOffset);
            sceneCam.transform.SetPositionAndRotation(position, Quaternion.Euler(rotation));

            var tempRenderTexture = RenderTexture.GetTemporary(1280, 720, 16);
            var oldTargetTexture = sceneCam.targetTexture;
            sceneCam.targetTexture = tempRenderTexture;
            sceneCam.Render();
            sceneCam.targetTexture = oldTargetTexture;

            var rect = EditorGUILayout.GetControlRect(false, 200f);
            GUI.DrawTexture(rect, tempRenderTexture, ScaleMode.ScaleToFit, false);
            RenderTexture.ReleaseTemporary(tempRenderTexture);

            sceneCam.transform.SetPositionAndRotation(oldPosition, oldRotation);
        }
        else
        {
            playbackLerpJumpDist = EditorGUILayout.FloatField("Playback lerp jump dist", playbackLerpJumpDist);
            GUI.enabled = previewCameras.Length > 0;
            if (GUILayout.Button("Play preview"))
            {
                isPlayingBack = true;
                playbackOffset = 0f;
                currentPlaybackCamera = 0;
                convertedPlaybackJumpDist = targetCameraSequence.ConvertPlaybackLerpJumpDist(playbackLerpJumpDist, currentPlaybackCamera);
            }
            GUI.enabled = true;
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if(previewCameras.Length <= 0)
        {
            return;
        }

        var oldColor = Handles.color;
        Handles.color = Color.blue;

        for(var c = 0; c < previewCameras.Length - 1; ++c)
        {
            Handles.DrawWireCube(previewCameras[c].transform.position, Vector3.one);

            var prevPoint = previewCameras[c].transform.position;
            for (var t = 0.1f; t <= 1f; t += 0.1f)
            {
                targetCameraSequence.GetLerpSpot(out var nextPoint, out var rotation, c, t);
                Handles.DrawLine(prevPoint, nextPoint);
                prevPoint = nextPoint;
            }
        }
        Handles.DrawWireCube(previewCameras[previewCameras.Length - 1].transform.position, Vector3.one);

        Handles.color = oldColor;
    }
}
