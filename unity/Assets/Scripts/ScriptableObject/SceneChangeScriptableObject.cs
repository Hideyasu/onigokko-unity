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
    [CreateAssetMenu(fileName = "SceneChangeScriptableObject", menuName = "Scriptable Objects/SceneChangeScriptableObject")]
    public class SceneChangeScriptableObject : ScriptableObject
    {
        [Header("シーンリスト")]
        public SceneAsset[] SceneLists;
    }
}