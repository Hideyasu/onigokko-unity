using UnityEngine;
using UnityEngine.UI;

namespace Onigokko.Heartbeat
{
    /// <summary>
    /// 心音システムデバッグ用 - 演出の動作確認とテスト
    /// </summary>
    public class HeartbeatDebugger : MonoBehaviour
    {
        [Header("デバッグ設定")]
        [SerializeField] private bool enableDebugMode = true;
        [SerializeField] private bool forceTestMode = false;
        [SerializeField] private float testDistance = 15f;

        [Header("テスト用UI")]
        [SerializeField] private Slider distanceSlider;
        [SerializeField] private Text debugText;
        [SerializeField] private Button testHeartbeatButton;

        private HeartbeatSystem heartbeatSystem;
        private ProximityDetector proximityDetector;
        private HeartbeatManager heartbeatManager;

        void Start()
        {
            if (!enableDebugMode) return;

            InitializeDebugger();
            CreateDebugUI();
        }

        private void InitializeDebugger()
        {
            // コンポーネントの取得
            heartbeatSystem = HeartbeatSystem.Instance;
            proximityDetector = FindObjectOfType<ProximityDetector>();
            heartbeatManager = HeartbeatManager.Instance;

            // 初期化状況をログ出力
            Debug.Log("=== 心音システム デバッグ情報 ===");
            Debug.Log($"HeartbeatSystem: {(heartbeatSystem != null ? "存在" : "不存在")}");
            Debug.Log($"ProximityDetector: {(proximityDetector != null ? "存在" : "不存在")}");
            Debug.Log($"HeartbeatManager: {(heartbeatManager != null ? "存在" : "不存在")}");

            // Player ID確認
            int playerId = PlayerPrefs.GetInt("PlayerID", -1);
            string role = playerId == 1000 ? "キラー" : playerId > 1000 ? "サバイバー" : "未設定";
            Debug.Log($"Player ID: {playerId} ({role})");

            // PlayerIdManagerの確認
            var playerIdManager = PlayerIdManager.Instance;
            if (playerIdManager != null)
            {
                Debug.Log($"PlayerIdManager: 存在 (ID: {playerIdManager.GetPlayerId()})");
            }
            else
            {
                Debug.LogWarning("PlayerIdManager が見つかりません");
            }

            Debug.Log("===========================");
        }

        private void CreateDebugUI()
        {
            // デバッグ用UIを動的作成
            GameObject canvasObj = GameObject.Find("DebugCanvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("DebugCanvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1000; // 最前面

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // デバッグテキスト
            CreateDebugText(canvasObj.transform);
        }

        private void CreateDistanceSlider(Transform parent)
        {
            GameObject sliderObj = new GameObject("DistanceSlider");
            sliderObj.transform.SetParent(parent, false);

            RectTransform rect = sliderObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.9f);
            rect.anchorMax = new Vector2(0.6f, 0.95f);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            distanceSlider = sliderObj.AddComponent<Slider>();
            distanceSlider.minValue = 0f;
            distanceSlider.maxValue = 100f;
            distanceSlider.value = testDistance;

            // スライダーの背景
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(sliderObj.transform, false);
            RectTransform bgRect = bg.AddComponent<RectTransform>();
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            distanceSlider.targetGraphic = bgImage;

            // スライダーのハンドル
            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(sliderObj.transform, false);
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(30, 30);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;
            distanceSlider.handleRect = handleRect;

            // スライダーの値変更イベント
            distanceSlider.onValueChanged.AddListener(OnDistanceChanged);
        }

        private void CreateTestButton(Transform parent)
        {
            GameObject buttonObj = new GameObject("TestButton");
            buttonObj.transform.SetParent(parent, false);

            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.7f, 0.9f);
            rect.anchorMax = new Vector2(0.9f, 0.95f);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            testHeartbeatButton = buttonObj.AddComponent<Button>();

            // ボタンの背景
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.7f, 0.2f, 0.8f);
            testHeartbeatButton.targetGraphic = buttonImage;

            // ボタンのテキスト
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.sizeDelta = Vector2.zero;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            Text buttonText = textObj.AddComponent<Text>();
            buttonText.text = "心音テスト";
            buttonText.color = Color.white;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            buttonText.fontSize = 24;

            // ボタンクリックイベント
            testHeartbeatButton.onClick.AddListener(TestHeartbeat);
        }

        private void CreateDebugText(Transform parent)
        {
            GameObject textObj = new GameObject("DebugText");
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.02f, 0.02f);
            rect.anchorMax = new Vector2(0.5f, 0.4f);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            debugText = textObj.AddComponent<Text>();
            debugText.color = Color.white;
            debugText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            debugText.fontSize = 18;
            debugText.alignment = TextAnchor.UpperLeft;
        }

        private void OnDistanceChanged(float distance)
        {
            testDistance = distance;
        }

        private void TestHeartbeat()
        {
            Debug.Log($"心音テスト実行 - 距離: {testDistance}m");

            // 強制的に心音を発生させる
            if (heartbeatSystem != null)
            {
                // リフレクションでプライベートフィールドを設定
                var type = typeof(HeartbeatSystem);
                var distanceField = type.GetField("currentDistance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (distanceField != null)
                {
                    distanceField.SetValue(heartbeatSystem, testDistance);
                    Debug.Log($"HeartbeatSystem に距離 {testDistance}m を設定");
                }

                // 心音レベル更新メソッドを呼び出し
                var updateMethod = type.GetMethod("UpdateHeartbeatLevel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (updateMethod != null)
                {
                    updateMethod.Invoke(heartbeatSystem, null);
                    Debug.Log("心音レベルを更新");
                }
            }

            // ProximityDetectorにもテスト距離を設定
            if (proximityDetector != null)
            {
                var type = typeof(ProximityDetector);
                var distanceField = type.GetField("distanceToKiller", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (distanceField != null)
                {
                    distanceField.SetValue(proximityDetector, testDistance);
                    Debug.Log($"ProximityDetector に距離 {testDistance}m を設定");
                }
            }
        }

        void Update()
        {
            if (!enableDebugMode) return;

            UpdateDebugInfo();

            // フォーステストモードの場合は継続的に距離を更新
            if (forceTestMode)
            {
                TestHeartbeat();
            }
        }

        private void UpdateDebugInfo()
        {
            if (debugText == null) return;

            string info = "=== 心音システム デバッグ ===\n";

            // Player ID情報
            int playerId = PlayerPrefs.GetInt("PlayerID", -1);
            info += $"Player ID: {playerId}\n";
            info += $"Role: {(playerId == 1000 ? "キラー" : "サバイバー")}\n\n";

            // HeartbeatSystem情報
            if (heartbeatSystem != null)
            {
                var level = heartbeatSystem.GetCurrentHeartbeatLevel();
                var distance = heartbeatSystem.GetCurrentDistance();
                info += $"心音レベル: {level}\n";
                info += $"キラーとの距離: {distance:F2}m\n";

                // 極近距離の特別表示
                if (distance <= 0.5f)
                {
                    info += "🚨 極近距離！最大演出\n";
                }
                else if (distance <= 1f)
                {
                    info += "⚠️ 非常に接近中\n";
                }

                info += "\n";
            }
            else
            {
                info += "HeartbeatSystem: 未初期化\n\n";
            }

            // ProximityDetector情報
            if (proximityDetector != null)
            {
                var nearbyPlayers = proximityDetector.GetAllNearbyPlayers();
                info += $"検出プレイヤー: {nearbyPlayers.Count}人\n";

                foreach (var player in nearbyPlayers)
                {
                    string role = player.isKiller ? "キラー" : "サバイバー";
                    info += $"  ID:{player.playerId}({role}) {player.distance:F1}m\n";
                }

                // キラーとの距離を明示的に表示
                float killerDistance = proximityDetector.GetDistanceToKiller();
                if (killerDistance != float.MaxValue)
                {
                    info += $"キラーとの距離: {killerDistance:F1}m\n";
                }
                else
                {
                    info += "キラー未検出\n";
                }
            }
            else
            {
                info += "ProximityDetector: 未初期化\n";
            }

            // テスト設定
            info += $"\nテスト距離: {testDistance:F1}m";

            debugText.text = info;
        }

        void OnGUI()
        {
            if (!enableDebugMode) return;

            int y = 10;

            GUI.Label(new Rect(Screen.width - 320, y, 300, 25), "=== 心音デバッガー ===");
            y += 30;

            // 極近距離テスト
            if (GUI.Button(new Rect(Screen.width - 320, y, 70, 30), "0.1m"))
            {
                testDistance = 0.1f;
                TestHeartbeat();
            }

            if (GUI.Button(new Rect(Screen.width - 245, y, 70, 30), "0.2m"))
            {
                testDistance = 0.2f;
                TestHeartbeat();
            }

            if (GUI.Button(new Rect(Screen.width - 170, y, 70, 30), "1m"))
            {
                testDistance = 1f;
                TestHeartbeat();
            }

            if (GUI.Button(new Rect(Screen.width - 95, y, 70, 30), "5m"))
            {
                testDistance = 5f;
                TestHeartbeat();
            }

            y += 35;

            if (GUI.Button(new Rect(Screen.width - 320, y, 70, 30), "10m"))
            {
                testDistance = 10f;
                TestHeartbeat();
            }

            if (GUI.Button(new Rect(Screen.width - 245, y, 70, 30), "20m"))
            {
                testDistance = 20f;
                TestHeartbeat();
            }

            if (GUI.Button(new Rect(Screen.width - 170, y, 70, 30), "30m"))
            {
                testDistance = 30f;
                TestHeartbeat();
            }

            if (GUI.Button(new Rect(Screen.width - 95, y, 70, 30), "50m"))
            {
                testDistance = 50f;
                TestHeartbeat();
            }

            y += 40;

            forceTestMode = GUI.Toggle(new Rect(Screen.width - 320, y, 200, 25), forceTestMode, "継続テストモード");

            y += 25;
            GUI.Label(new Rect(Screen.width - 320, y, 300, 20), $"現在のテスト距離: {testDistance:F1}m");
        }
    }
}