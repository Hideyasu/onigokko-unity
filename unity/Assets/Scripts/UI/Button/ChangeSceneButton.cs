using UnityEngine;
using UnityEngine.SceneManagement;

namespace UICreate
{
    /// <summary>
    /// シーン変更ボタンの挙動を制御するクラス
    /// </summary>
    public class ChangeSceneButton : MonoBehaviour
    {
        [SerializeField] private SceneChangeScriptableObject _sceneChangeScriptableObject;
        [SerializeField] private int _sceneIndex = -1;
        public static int CurrentSceneIndex { get; private set; } = 0;

        public void OnClick()
        {
            LoadScene(_sceneIndex);
        }

        /// <summary>
        /// 引数で与えられたインデックスのシーンへ遷移する
        /// </summary>
        /// <param name="index"></param>
        private void LoadScene(int index)
        {

            if (index == -1)
            {
                LoadNextScene();
                return;
            }

            if (index < 0 || index >= _sceneChangeScriptableObject.SceneLists.Length)
            {
                Debug.LogError("シーンのインデックスが不正です");
                return;
            }
            CurrentSceneIndex = index;
            LoadSpecificScene(_sceneChangeScriptableObject.SceneLists[index].name);
        }

        private void LoadNextScene()
        {
            CurrentSceneIndex++;
            if (CurrentSceneIndex >= _sceneChangeScriptableObject.SceneLists.Length)
            {
                Debug.LogError("次のシーンが存在しません");
                return;
            }
            SceneManager.LoadScene(_sceneChangeScriptableObject.SceneLists[CurrentSceneIndex].name);
        }

        private void LoadSpecificScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}