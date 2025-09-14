using UnityEngine;
using UnityEditor;
using Onigokko.Heartbeat;

namespace Onigokko.Editor
{
    /// <summary>
    /// HeartbeatManager用のカスタムエディタ
    /// </summary>
    [CustomEditor(typeof(HeartbeatManager))]
    public class HeartbeatManagerEditor : UnityEditor.Editor
    {
        private HeartbeatManager heartbeatManager;

        private void OnEnable()
        {
            heartbeatManager = (HeartbeatManager)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("心音システム設定", EditorStyles.boldLabel);

            // プレイヤーID設定
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Player ID:", GUILayout.Width(70));

            if (GUILayout.Button("キラー (1000)", GUILayout.Width(100)))
            {
                SetPlayerId(1000);
            }
            if (GUILayout.Button("サバイバー (1001)", GUILayout.Width(120)))
            {
                SetPlayerId(1001);
            }
            if (GUILayout.Button("サバイバー (1002)", GUILayout.Width(120)))
            {
                SetPlayerId(1002);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 現在の設定表示
            int currentPlayerId = PlayerPrefs.GetInt("PlayerID", 1001);
            string role = (currentPlayerId == 1000) ? "キラー" : "サバイバー";
            EditorGUILayout.HelpBox($"現在の設定: Player ID {currentPlayerId} ({role})", MessageType.Info);

            EditorGUILayout.Space(10);

            // システム制御ボタン
            EditorGUILayout.LabelField("システム制御", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("システム開始"))
            {
                if (Application.isPlaying)
                {
                    heartbeatManager.StartHeartbeatSystem();
                }
                else
                {
                    Debug.LogWarning("プレイモードでのみ実行できます");
                }
            }

            if (GUILayout.Button("システム停止"))
            {
                if (Application.isPlaying)
                {
                    heartbeatManager.StopHeartbeatSystem();
                }
                else
                {
                    Debug.LogWarning("プレイモードでのみ実行できます");
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 設定ガイド
            EditorGUILayout.LabelField("設定ガイド", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("1. Player IDを設定\n2. 必要に応じてオーディオクリップを設定\n3. プレイモードでシステムを開始", MessageType.None);

            // オーディオ設定の状態確認
            EditorGUILayout.Space(5);
            CheckAudioSetup();

            // BLE設定の状態確認
            CheckBLESetup();
        }

        private void SetPlayerId(int id)
        {
            PlayerPrefs.SetInt("PlayerID", id);
            PlayerPrefs.Save();

            if (Application.isPlaying && heartbeatManager != null)
            {
                heartbeatManager.SetPlayerId(id);
            }

            Debug.Log($"Player ID を {id} に設定しました");
        }

        private void CheckAudioSetup()
        {
            HeartbeatAudioManager audioManager = heartbeatManager.GetComponent<HeartbeatAudioManager>();
            if (audioManager == null)
            {
                EditorGUILayout.HelpBox("HeartbeatAudioManager が見つかりません。自動で追加されます。", MessageType.Warning);
            }
            else
            {
                // オーディオクリップの確認
                var fields = typeof(HeartbeatAudioManager).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                bool hasAudioClips = false;

                foreach (var field in fields)
                {
                    if (field.FieldType == typeof(AudioClip) && field.GetValue(audioManager) != null)
                    {
                        hasAudioClips = true;
                        break;
                    }
                }

                if (!hasAudioClips)
                {
                    EditorGUILayout.HelpBox("心音用のオーディオクリップが設定されていません。\nAssets/Resources/Sounds/Heartbeat/ フォルダにオーディオファイルを配置してください。", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox("オーディオ設定: OK", MessageType.Info);
                }
            }
        }

        private void CheckBLESetup()
        {
            var bleBeacon = FindObjectOfType<Onigokko.BLE.iOSBLEBeacon>();
            if (bleBeacon == null)
            {
                EditorGUILayout.HelpBox("iOSBLEBeacon が見つかりません。自動で作成されます。", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("BLE設定: OK", MessageType.Info);
            }
        }
    }
}