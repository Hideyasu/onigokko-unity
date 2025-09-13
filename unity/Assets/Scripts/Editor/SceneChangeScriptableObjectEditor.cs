using UnityEngine;
using UnityEditor;
using UICreate;

[CustomEditor(typeof(SceneChangeScriptableObject))]
public class SceneChangeScriptableObjectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SceneChangeScriptableObject script = (SceneChangeScriptableObject)target;

        EditorGUI.BeginChangeCheck();

        // シーンリストの表示と編集
        EditorGUILayout.LabelField("シーンリスト", EditorStyles.boldLabel);

        if (script.SceneLists == null)
        {
            script.SceneLists = new SceneReference[0];
        }

        int newSize = EditorGUILayout.IntField("Size", script.SceneLists.Length);
        if (newSize != script.SceneLists.Length)
        {
            var newArray = new SceneReference[newSize];
            for (int i = 0; i < Mathf.Min(newSize, script.SceneLists.Length); i++)
            {
                newArray[i] = script.SceneLists[i];
            }
            script.SceneLists = newArray;
        }

        for (int i = 0; i < script.SceneLists.Length; i++)
        {
            if (script.SceneLists[i] == null)
            {
                script.SceneLists[i] = new SceneReference();
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Element {i}", GUILayout.Width(80));

            // SceneAssetフィールド
            SceneAsset newSceneAsset = EditorGUILayout.ObjectField(
                script.SceneLists[i].sceneAsset,
                typeof(SceneAsset),
                false
            ) as SceneAsset;

            if (newSceneAsset != script.SceneLists[i].sceneAsset)
            {
                script.SceneLists[i].sceneAsset = newSceneAsset;
                if (newSceneAsset != null)
                {
                    script.SceneLists[i].sceneName = newSceneAsset.name;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(script);
            script.UpdateSceneNames();
        }

        // シーン名の更新ボタン
        if (GUILayout.Button("シーン名を更新"))
        {
            script.UpdateSceneNames();
            EditorUtility.SetDirty(script);
        }
    }
}