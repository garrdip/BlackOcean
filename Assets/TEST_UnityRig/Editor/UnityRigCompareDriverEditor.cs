// 비교 드라이버 인스펙터: 플레이 모드에서 버튼으로 애니메이션 실행
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UnityRigCompareDriver))]
public class UnityRigCompareDriverEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var driver = (UnityRigCompareDriver)target;
        GUILayout.Space(8);
        GUILayout.Label("애니메이션 실행 (플레이 모드에서만)", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Idle")) driver.Play(0);
            if (GUILayout.Button("Attack0")) driver.Play(1);
            if (GUILayout.Button("Buff0")) driver.Play(2);
            if (GUILayout.Button("Defence0")) driver.Play(3);
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Walk (유니티 리그 전용)")) driver.PlayWalk();
        }
    }
}
