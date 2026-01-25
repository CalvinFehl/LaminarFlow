using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CheckPointTrack))]
public class CheckPointTrackEditor : Editor
{
    private CheckPointTrack checkPointTrack;

    private void OnEnable()
    {
        checkPointTrack = (CheckPointTrack)target;
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();

        if (EditorGUI.EndChangeCheck())
        {
            checkPointTrack.RefreshPath();
        }

        if (GUILayout.Button("Rebuild Path"))
        {
            checkPointTrack.RefreshPath();
        }
    }

    private void OnSceneGUI()
    {
        for (int i = 0; i < checkPointTrack.CheckPoints.Count; i++)
        {
            CheckPoint checkPoint = checkPointTrack.CheckPoints[i];
            Handles.color = Color.gray;

            Handles.Label(checkPoint.transform.position, (i + 1).ToString(), new GUIStyle() { fontSize = 10 });
            EditorGUI.BeginChangeCheck();
            checkPoint.transform.position = Handles.PositionHandle(checkPoint.transform.position, checkPoint.transform.rotation);
            if (EditorGUI.EndChangeCheck())
            {
                checkPointTrack.RefreshPath();
            }
        }
    }
}