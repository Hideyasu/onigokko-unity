using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// シーン変更に関する設定を管理するScriptableObject
/// </summary>
namespace UICreate
{
    [System.Serializable]
    public class SceneReference
    {
        public string sceneName;
#if UNITY_EDITOR
        public SceneAsset sceneAsset;
#endif
    }

    [CreateAssetMenu(fileName = "SceneChangeScriptableObject", menuName = "Scriptable Objects/SceneChangeScriptableObject")]
    public class SceneChangeScriptableObject : ScriptableObject
    {
        [Header("シーンリスト")]
        [SerializeField]
        private SceneReference[] sceneReferences = new SceneReference[0];

        // 互換性のためのプロパティ
        public SceneReference[] SceneLists
        {
            get { return sceneReferences; }
            set { sceneReferences = value; }
        }

#if UNITY_EDITOR
        // Editor専用: SceneAssetの配列として扱うためのヘルパー
        public void UpdateSceneNames()
        {
            foreach (var sceneRef in sceneReferences)
            {
                if (sceneRef != null && sceneRef.sceneAsset != null)
                {
                    sceneRef.sceneName = sceneRef.sceneAsset.name;
                }
            }
        }

        private void OnValidate()
        {
            UpdateSceneNames();
        }
#endif

        /// <summary>
        /// シーン名を取得
        /// </summary>
        public string GetSceneName(int index)
        {
            if (index < 0 || index >= sceneReferences.Length) return "";
            if (sceneReferences[index] == null) return "";

#if UNITY_EDITOR
            // Editorではシーンアセットから名前を取得
            if (sceneReferences[index].sceneAsset != null)
            {
                return sceneReferences[index].sceneAsset.name;
            }
#endif
            // ビルド時はsceneNameを使用
            return sceneReferences[index].sceneName;
        }

        /// <summary>
        /// シーンを読み込み
        /// </summary>
        public void LoadScene(int index)
        {
            string sceneName = GetSceneName(index);
            if (!string.IsNullOrEmpty(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
        }
    }
}