using UnityEngine;
using UnityEditor;

namespace Onigokko.Editor
{
    /// <summary>
    /// シーン設定ヘルパー - Build SettingsにMainSceneを自動追加
    /// </summary>
    public static class SceneSetupHelper
    {
        [MenuItem("Onigokko/Setup Build Scenes")]
        public static void SetupBuildScenes()
        {
            string mainScenePath = "Assets/Scenes/MainGame/MainScene.unity";

            // シーンファイルの存在確認
            if (!System.IO.File.Exists(mainScenePath))
            {
                Debug.LogError($"MainSceneが見つかりません: {mainScenePath}");
                EditorUtility.DisplayDialog("エラー", $"MainSceneが見つかりません:\n{mainScenePath}", "OK");
                return;
            }

            // Build Settingsの現在のシーン一覧を取得
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            // MainSceneが既に追加されているか確認
            bool mainSceneExists = false;
            foreach (var scene in scenes)
            {
                if (scene.path == mainScenePath)
                {
                    mainSceneExists = true;
                    if (!scene.enabled)
                    {
                        scene.enabled = true;
                        Debug.Log($"MainSceneを有効化しました: {mainScenePath}");
                    }
                    break;
                }
            }

            // MainSceneが存在しない場合は追加
            if (!mainSceneExists)
            {
                scenes.Add(new EditorBuildSettingsScene(mainScenePath, true));
                Debug.Log($"MainSceneをBuild Settingsに追加しました: {mainScenePath}");
            }

            // Build Settingsを更新
            EditorBuildSettings.scenes = scenes.ToArray();

            Debug.Log("Build Settingsの設定が完了しました");
            EditorUtility.DisplayDialog("完了", "Build SettingsにMainSceneが設定されました", "OK");
        }

        [MenuItem("Onigokko/Validate Build Configuration")]
        public static void ValidateBuildConfiguration()
        {
            Debug.Log("=== ビルド設定検証 ===");

            // シーン設定の確認
            var buildScenes = EditorBuildSettings.scenes;
            Debug.Log($"Build Settings シーン数: {buildScenes.Length}");

            foreach (var scene in buildScenes)
            {
                string status = scene.enabled ? "有効" : "無効";
                string exists = System.IO.File.Exists(scene.path) ? "存在" : "不存在";
                Debug.Log($"  - {scene.path} [{status}] [{exists}]");
            }

            // PlayerIdManager の確認
            var playerIdManager = Object.FindObjectOfType<Onigokko.Heartbeat.PlayerIdManager>();
            if (playerIdManager != null)
            {
                var type = typeof(Onigokko.Heartbeat.PlayerIdManager);
                var buildIdField = type.GetField("buildPlayerId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (buildIdField != null)
                {
                    int buildId = (int)buildIdField.GetValue(playerIdManager);
                    string role = buildId == 1000 ? "キラー" : "サバイバー";
                    Debug.Log($"PlayerIdManager Build ID: {buildId} ({role})");
                }
            }
            else
            {
                Debug.LogWarning("PlayerIdManager が見つかりません");
            }

            // 現在のPlayerPrefs確認
            int playerPrefsId = PlayerPrefs.GetInt("PlayerID", -1);
            if (playerPrefsId != -1)
            {
                string role = playerPrefsId == 1000 ? "キラー" : "サバイバー";
                Debug.Log($"PlayerPrefs ID: {playerPrefsId} ({role})");
            }

            Debug.Log("==================");
        }

        /// <summary>
        /// プロジェクト読み込み時にシーン設定を自動実行
        /// </summary>
        [InitializeOnLoadMethod]
        static void AutoSetupOnLoad()
        {
            // 初回のみ実行（EditorPrefsで管理）
            string key = "Onigokko_AutoSceneSetup";
            if (!EditorPrefs.GetBool(key, false))
            {
                EditorApplication.delayCall += () =>
                {
                    SetupBuildScenes();
                    EditorPrefs.SetBool(key, true);
                };
            }
        }
    }
}