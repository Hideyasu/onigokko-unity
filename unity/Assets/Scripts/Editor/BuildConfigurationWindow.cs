using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using Onigokko.Heartbeat;

namespace Onigokko.Editor
{
    /// <summary>
    /// ビルド設定ウィンドウ - キラー/サバイバー用ビルド設定を管理
    /// </summary>
    public class BuildConfigurationWindow : EditorWindow
    {
        private int selectedPlayerId = 1001;
        private bool overridePlayerPrefs = true;
        private string buildName = "";

        [MenuItem("Onigokko/Build Configuration")]
        public static void ShowWindow()
        {
            GetWindow<BuildConfigurationWindow>("ビルド設定");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("🎮 鬼vs陰陽師 ビルド設定", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // Player ID選択
            EditorGUILayout.LabelField("プレイヤー設定", EditorStyles.boldLabel);

            // キラー行
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(selectedPlayerId == 1000, "キラー (ID: 1000)", EditorStyles.miniButton))
            {
                selectedPlayerId = 1000;
                buildName = "Killer";
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("サバイバー", EditorStyles.miniBoldLabel);

            // サバイバー 1-3行
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(selectedPlayerId == 1001, "サバイバー1 (ID: 1001)", EditorStyles.miniButton))
            {
                selectedPlayerId = 1001;
                buildName = "Survivor1";
            }

            if (GUILayout.Toggle(selectedPlayerId == 1002, "サバイバー2 (ID: 1002)", EditorStyles.miniButton))
            {
                selectedPlayerId = 1002;
                buildName = "Survivor2";
            }

            if (GUILayout.Toggle(selectedPlayerId == 1003, "サバイバー3 (ID: 1003)", EditorStyles.miniButton))
            {
                selectedPlayerId = 1003;
                buildName = "Survivor3";
            }
            EditorGUILayout.EndHorizontal();

            // サバイバー 4-6行
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(selectedPlayerId == 1004, "サバイバー4 (ID: 1004)", EditorStyles.miniButton))
            {
                selectedPlayerId = 1004;
                buildName = "Survivor4";
            }

            if (GUILayout.Toggle(selectedPlayerId == 1005, "サバイバー5 (ID: 1005)", EditorStyles.miniButton))
            {
                selectedPlayerId = 1005;
                buildName = "Survivor5";
            }

            if (GUILayout.Toggle(selectedPlayerId == 1006, "サバイバー6 (ID: 1006)", EditorStyles.miniButton))
            {
                selectedPlayerId = 1006;
                buildName = "Survivor6";
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 設定オプション
            overridePlayerPrefs = EditorGUILayout.Toggle("PlayerPrefs を上書き", overridePlayerPrefs);

            EditorGUILayout.Space(10);

            // 現在の設定表示
            EditorGUILayout.HelpBox($"ビルド設定: Player ID {selectedPlayerId} ({(selectedPlayerId == 1000 ? "キラー" : "サバイバー")})", MessageType.Info);

            EditorGUILayout.Space(10);

            // シーン内のPlayerIdManagerを設定
            EditorGUILayout.LabelField("シーン設定", EditorStyles.boldLabel);

            if (GUILayout.Button("現在のシーンに設定を適用"))
            {
                ApplyToCurrentScene();
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("全シーンに設定を適用"))
            {
                ApplyToAllScenes();
            }

            EditorGUILayout.Space(10);

            // ビルド実行
            EditorGUILayout.LabelField("ビルド実行", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button($"iOS {buildName} ビルド"))
            {
                BuildForPlatform(BuildTarget.iOS);
            }

            if (GUILayout.Button($"Android {buildName} ビルド"))
            {
                BuildForPlatform(BuildTarget.Android);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // デバッグ情報
            EditorGUILayout.LabelField("デバッグ情報", EditorStyles.boldLabel);

            if (GUILayout.Button("Player ID設定状況を確認"))
            {
                CheckPlayerIdConfiguration();
            }

            // PlayerPrefsの現在値表示
            int currentPlayerPrefsId = PlayerPrefs.GetInt("PlayerID", -1);
            if (currentPlayerPrefsId != -1)
            {
                EditorGUILayout.HelpBox($"現在のPlayerPrefs: {currentPlayerPrefsId} ({(currentPlayerPrefsId == 1000 ? "キラー" : "サバイバー")})", MessageType.None);
            }
        }

        private void ApplyToCurrentScene()
        {
            // 現在のシーンのPlayerIdManagerを検索
            PlayerIdManager playerIdManager = FindObjectOfType<PlayerIdManager>();

            if (playerIdManager == null)
            {
                // PlayerIdManagerがない場合は作成
                GameObject managerObj = new GameObject("PlayerIdManager");
                playerIdManager = managerObj.AddComponent<PlayerIdManager>();
                Debug.Log("PlayerIdManager を作成しました");
            }

            // リフレクションで設定値を適用
            SetPlayerIdManagerValues(playerIdManager);

            EditorUtility.SetDirty(playerIdManager);
            Debug.Log($"現在のシーンにPlayer ID {selectedPlayerId} を設定しました");
        }

        private void ApplyToAllScenes()
        {
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
            int processedScenes = 0;

            foreach (string guid in sceneGuids)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);

                // MainSceneのみ処理
                if (scenePath.Contains("MainScene"))
                {
                    var currentScene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);

                    PlayerIdManager playerIdManager = FindObjectOfType<PlayerIdManager>();
                    if (playerIdManager == null)
                    {
                        GameObject managerObj = new GameObject("PlayerIdManager");
                        playerIdManager = managerObj.AddComponent<PlayerIdManager>();
                    }

                    SetPlayerIdManagerValues(playerIdManager);
                    EditorUtility.SetDirty(playerIdManager);

                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(currentScene);
                    processedScenes++;
                }
            }

            Debug.Log($"{processedScenes} 個のMainSceneにPlayer ID {selectedPlayerId} を設定しました");
        }

        private void SetPlayerIdManagerValues(PlayerIdManager manager)
        {
            var type = typeof(PlayerIdManager);

            var buildPlayerIdField = type.GetField("buildPlayerId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (buildPlayerIdField != null)
                buildPlayerIdField.SetValue(manager, selectedPlayerId);

            var overrideField = type.GetField("overridePlayerPrefs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (overrideField != null)
                overrideField.SetValue(manager, overridePlayerPrefs);
        }

        private void BuildForPlatform(BuildTarget buildTarget)
        {
            // ビルド前に設定を適用
            ApplyToCurrentScene();

            // PlayerPrefsにも設定
            PlayerPrefs.SetInt("PlayerID", selectedPlayerId);
            PlayerPrefs.Save();

            // ビルド設定
            BuildPlayerOptions buildOptions = new BuildPlayerOptions();

            // MainSceneのパスを明示的に指定
            string[] scenePaths;

            // Build Settings にシーンが設定されている場合はそれを使用
            if (EditorBuildSettings.scenes.Length > 0)
            {
                scenePaths = System.Array.ConvertAll(
                    System.Array.FindAll(EditorBuildSettings.scenes, scene => scene.enabled),
                    scene => scene.path
                );
            }
            else
            {
                // MainSceneのパスを直接指定
                string mainScenePath = "Assets/Scenes/MainGame/MainScene.unity";
                if (System.IO.File.Exists(mainScenePath))
                {
                    scenePaths = new string[] { mainScenePath };
                }
                else
                {
                    // 現在のアクティブシーンを使用（フォールバック）
                    var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
                    if (!string.IsNullOrEmpty(activeScene.path))
                    {
                        scenePaths = new string[] { activeScene.path };
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("エラー", "有効なシーンが見つかりません。Build Settingsでシーンを設定してください。", "OK");
                        return;
                    }
                }
            }

            buildOptions.scenes = scenePaths;
            buildOptions.target = buildTarget;

            // デバッグ情報
            Debug.Log($"ビルド対象シーン: {string.Join(", ", scenePaths)}");

            string platformName = buildTarget == BuildTarget.iOS ? "iOS" : "Android";
            string buildDirectory = "Builds";

            // Buildsディレクトリが存在しない場合は作成
            if (!System.IO.Directory.Exists(buildDirectory))
            {
                System.IO.Directory.CreateDirectory(buildDirectory);
            }

            buildOptions.locationPathName = $"{buildDirectory}/{platformName}_{buildName}";

            if (buildTarget == BuildTarget.Android)
            {
                buildOptions.locationPathName += ".apk";
            }

            // プロダクト名にPlayer IDを含める
            PlayerSettings.productName = $"鬼vs陰陽師_{buildName}";

            Debug.Log($"{platformName} {buildName} ビルドを開始します...");

            var report = BuildPipeline.BuildPlayer(buildOptions);

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"ビルド成功: {buildOptions.locationPathName}");
                EditorUtility.DisplayDialog("ビルド完了", $"{platformName} {buildName} のビルドが完了しました！", "OK");
            }
            else
            {
                Debug.LogError($"ビルド失敗: {report.summary.result}");
                EditorUtility.DisplayDialog("ビルドエラー", $"ビルドに失敗しました: {report.summary.result}", "OK");
            }
        }

        private void CheckPlayerIdConfiguration()
        {
            Debug.Log("=== Player ID 設定確認 ===");

            // PlayerIdManager の確認
            PlayerIdManager playerIdManager = FindObjectOfType<PlayerIdManager>();
            if (playerIdManager != null)
            {
                var type = typeof(PlayerIdManager);
                var buildIdField = type.GetField("buildPlayerId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (buildIdField != null)
                {
                    int buildId = (int)buildIdField.GetValue(playerIdManager);
                    Debug.Log($"PlayerIdManager Build ID: {buildId}");
                }
            }
            else
            {
                Debug.LogWarning("PlayerIdManager が見つかりません");
            }

            // HeartbeatManager の確認
            HeartbeatManager heartbeatManager = FindObjectOfType<HeartbeatManager>();
            if (heartbeatManager != null)
            {
                Debug.Log("HeartbeatManager が見つかりました");
            }
            else
            {
                Debug.LogWarning("HeartbeatManager が見つかりません");
            }

            // PlayerPrefs の確認
            int playerPrefsId = PlayerPrefs.GetInt("PlayerID", -1);
            Debug.Log($"PlayerPrefs ID: {playerPrefsId}");

            Debug.Log("=======================");
        }
    }
}