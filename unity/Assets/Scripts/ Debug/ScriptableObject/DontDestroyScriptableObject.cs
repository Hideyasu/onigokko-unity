using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// シーン変更に関する設定を管理するScriptableObject
/// </summary>
namespace UICreate
{
    [CreateAssetMenu(fileName = "DontDestroyScriptableObject", menuName = "Scriptable Objects/DontDestroyScriptableObject")]
    public class DontDestroyScriptableObject : ScriptableObject
    {
        [Header("シーンリスト")]
        public SceneAsset[] SceneLists;
    }
}