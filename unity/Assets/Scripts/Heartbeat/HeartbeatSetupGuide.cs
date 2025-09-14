using UnityEngine;

namespace Onigokko.Heartbeat
{
    /// <summary>
    /// 心音システム設定ガイド
    /// </summary>
    [System.Serializable]
    public class HeartbeatSetupGuide : MonoBehaviour
    {
        [Header("📋 設定ガイド")]
        [TextArea(10, 20)]
        [SerializeField] private string setupInstructions =
@"🎮 心音システム設定ガイド

## 1. 基本設定
### Player ID設定:
• キラー: 1000
• サバイバー: 1001, 1002, 1003...

### Inspector設定:
• Auto Initialize: ☑️ (推奨)
• Player ID: プレイヤーに応じて設定

## 2. オーディオ設定
### 必要なオーディオファイル:
• heartbeat_slow.wav (遠距離用)
• heartbeat_medium.wav (中距離用)
• heartbeat_fast.wav (近距離用)
• heartbeat_critical.wav (極近距離用)
• ambient_tension.wav (環境音)
• alert_sound.wav (警告音)

### 配置場所:
Assets/Resources/Sounds/Heartbeat/

## 3. UI設定
### 必要なUI要素:
• Canvas (自動作成)
• ビネットオーバーレイ (自動作成)
• パルスエフェクト (自動作成)
• 危険オーバーレイ (自動作成)

## 4. BLE設定
### 必要なコンポーネント:
• iOSBLEBeacon (自動追加)
• UUID: 550e8400-e29b-41d4-a716-446655440000

## 5. 使用方法
### 初期化:
HeartbeatManager.Instance.Initialize();

### システム開始:
HeartbeatManager.Instance.StartHeartbeatSystem();

### Player ID変更:
HeartbeatManager.Instance.SetPlayerId(1001);";

        [Header("🎯 距離設定")]
        [Tooltip("遠距離心音の開始距離 (メートル)")]
        public float heartbeatRangeFar = 50f;

        [Tooltip("中距離心音の開始距離 (メートル)")]
        public float heartbeatRangeMid = 30f;

        [Tooltip("近距離心音の開始距離 (メートル)")]
        public float heartbeatRangeNear = 10f;

        [Header("🎨 エフェクト設定")]
        [Tooltip("最大音量 (0.0 - 1.0)")]
        [Range(0f, 1f)]
        public float maxVolume = 1f;

        [Tooltip("最大ビネット透明度 (0.0 - 1.0)")]
        [Range(0f, 1f)]
        public float maxVignetteAlpha = 0.6f;

        [Tooltip("振動を有効にする")]
        public bool enableVibration = true;

        [Tooltip("画面揺れを有効にする")]
        public bool enableScreenShake = true;

        void Start()
        {
            // 設定値をHeartbeatSystemに適用
            ApplySettings();
        }

        private void ApplySettings()
        {
            var heartbeatSystem = HeartbeatSystem.Instance;
            if (heartbeatSystem != null)
            {
                // リフレクションを使用して設定値を適用
                var type = heartbeatSystem.GetType();

                var farRangeField = type.GetField("heartbeatRangeFar", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (farRangeField != null) farRangeField.SetValue(heartbeatSystem, heartbeatRangeFar);

                var midRangeField = type.GetField("heartbeatRangeMid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (midRangeField != null) midRangeField.SetValue(heartbeatSystem, heartbeatRangeMid);

                var nearRangeField = type.GetField("heartbeatRangeNear", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (nearRangeField != null) nearRangeField.SetValue(heartbeatSystem, heartbeatRangeNear);

                Debug.Log($"[HeartbeatSetupGuide] 設定を適用: Far={heartbeatRangeFar}m, Mid={heartbeatRangeMid}m, Near={heartbeatRangeNear}m");
            }
        }

        [ContextMenu("Create Audio Folder Structure")]
        public void CreateAudioFolderStructure()
        {
            #if UNITY_EDITOR
            string basePath = "Assets/Resources/Sounds/Heartbeat";

            if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
            }

            if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources/Sounds"))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets/Resources", "Sounds");
            }

            if (!UnityEditor.AssetDatabase.IsValidFolder(basePath))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets/Resources/Sounds", "Heartbeat");
            }

            // ReadMeファイルの作成
            string readmePath = basePath + "/README.txt";
            string readmeContent = @"心音システム用オーディオファイル配置場所

必要なファイル:
- heartbeat_slow.wav (遠距離用 - ゆっくりとした心音)
- heartbeat_medium.wav (中距離用 - 普通の心音)
- heartbeat_fast.wav (近距離用 - 速い心音)
- heartbeat_critical.wav (極近距離用 - 非常に速い心音)
- ambient_tension.wav (環境音 - 緊張感のある背景音)
- alert_sound.wav (警告音 - キラー発見時の警告)

推奨設定:
- サンプルレート: 44100Hz
- ビット深度: 16bit
- フォーマット: WAV
- 長さ: 1-3秒程度 (ループ再生)
";

            System.IO.File.WriteAllText(readmePath, readmeContent);
            UnityEditor.AssetDatabase.Refresh();

            Debug.Log($"オーディオフォルダ構造を作成しました: {basePath}");
            #endif
        }

        [ContextMenu("Setup Example Scene")]
        public void SetupExampleScene()
        {
            #if UNITY_EDITOR
            // HeartbeatManagerの追加
            GameObject managerObj = GameObject.Find("HeartbeatManager");
            if (managerObj == null)
            {
                managerObj = new GameObject("HeartbeatManager");
                managerObj.AddComponent<HeartbeatManager>();
            }

            // カメラの確認
            if (Camera.main == null)
            {
                GameObject cameraObj = new GameObject("Main Camera");
                cameraObj.AddComponent<Camera>();
                cameraObj.tag = "MainCamera";
            }

            // EventSystemの確認
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            Debug.Log("サンプルシーンのセットアップが完了しました");
            #endif
        }

        void OnGUI()
        {
            #if UNITY_EDITOR
            int y = 10;

            GUI.Box(new Rect(Screen.width - 320, y, 300, 200), "");

            GUI.Label(new Rect(Screen.width - 310, y + 10, 280, 25), "=== 心音システム設定 ===");
            y += 35;

            GUI.Label(new Rect(Screen.width - 310, y, 280, 20), $"遠距離: {heartbeatRangeFar}m");
            y += 20;
            GUI.Label(new Rect(Screen.width - 310, y, 280, 20), $"中距離: {heartbeatRangeMid}m");
            y += 20;
            GUI.Label(new Rect(Screen.width - 310, y, 280, 20), $"近距離: {heartbeatRangeNear}m");
            y += 25;

            GUI.Label(new Rect(Screen.width - 310, y, 280, 20), $"音量: {maxVolume:F1}");
            y += 20;
            GUI.Label(new Rect(Screen.width - 310, y, 280, 20), $"エフェクト: {maxVignetteAlpha:F1}");
            y += 25;

            if (GUI.Button(new Rect(Screen.width - 310, y, 130, 25), "設定を適用"))
            {
                ApplySettings();
            }

            if (GUI.Button(new Rect(Screen.width - 170, y, 130, 25), "フォルダ作成"))
            {
                CreateAudioFolderStructure();
            }
            #endif
        }
    }
}