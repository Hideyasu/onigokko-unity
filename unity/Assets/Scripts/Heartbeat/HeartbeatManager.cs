using UnityEngine;
using UnityEngine.UI;
using Onigokko.BLE;

namespace Onigokko.Heartbeat
{
    /// <summary>
    /// 心音マネージャー - MainSceneで心音システム全体を管理
    /// </summary>
    public class HeartbeatManager : MonoBehaviour
    {
        [Header("心音システムコンポーネント")]
        [SerializeField] private HeartbeatSystem heartbeatSystem;
        [SerializeField] private ProximityDetector proximityDetector;
        [SerializeField] private HeartbeatUIController uiController;
        [SerializeField] private HeartbeatAudioManager audioManager;

        [Header("UI要素")]
        [SerializeField] private Canvas heartbeatCanvas;
        [SerializeField] private Image vignetteOverlay;
        [SerializeField] private Image pulseOverlay;
        [SerializeField] private CanvasGroup dangerOverlay;

        [Header("プレイヤー設定")]
        [SerializeField] private int playerId = 1001;
        [SerializeField] private bool autoInitialize = true;

        private iOSBLEBeacon bleBeacon;
        private bool isInitialized = false;

        private static HeartbeatManager _instance;
        public static HeartbeatManager Instance => _instance;

        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (autoInitialize)
            {
                Initialize();
            }
        }

        public void Initialize()
        {
            if (isInitialized) return;

            Debug.Log("[HeartbeatManager] 心音システムの初期化を開始");

            // Player IDの取得（PlayerIdManagerを優先）
            var playerIdManager = PlayerIdManager.Instance;
            if (playerIdManager != null)
            {
                playerId = playerIdManager.GetPlayerId();
                Debug.Log($"[HeartbeatManager] PlayerIdManagerからID取得: {playerId}");
            }
            else
            {
                playerId = PlayerPrefs.GetInt("PlayerID", 1001);
                Debug.Log($"[HeartbeatManager] PlayerPrefsからID取得: {playerId}");
            }

            // BLEビーコンの初期化
            InitializeBLE();

            // 心音システムコンポーネントの初期化
            InitializeComponents();

            // UIの初期化
            InitializeUI();

            isInitialized = true;
            Debug.Log($"[HeartbeatManager] 初期化完了 - PlayerID: {playerId}");
        }

        private void InitializeBLE()
        {
            // BLEビーコンの取得または作成
            bleBeacon = iOSBLEBeacon.Instance;
            if (bleBeacon == null)
            {
                GameObject bleObject = new GameObject("BLEBeacon");
                bleBeacon = bleObject.AddComponent<iOSBLEBeacon>();
                DontDestroyOnLoad(bleObject);
            }

            // BLEビーコンの設定
            var bleBeaconComponent = bleBeacon.GetComponent<iOSBLEBeacon>();
            if (bleBeaconComponent != null)
            {
                // Player IDとSession IDの設定
                int sessionId = PlayerPrefs.GetInt("SessionID", 1);

                // リフレクションまたはpublicメソッドでプロパティを設定
                // （iOSBLEBeaconクラスにSetterメソッドが必要）
            }
        }

        private void InitializeComponents()
        {
            // HeartbeatSystemの初期化
            if (heartbeatSystem == null)
            {
                heartbeatSystem = GetComponent<HeartbeatSystem>();
                if (heartbeatSystem == null)
                {
                    heartbeatSystem = gameObject.AddComponent<HeartbeatSystem>();
                }
            }
            heartbeatSystem.SetPlayerId(playerId);

            // ProximityDetectorの初期化
            if (proximityDetector == null)
            {
                proximityDetector = GetComponent<ProximityDetector>();
                if (proximityDetector == null)
                {
                    proximityDetector = gameObject.AddComponent<ProximityDetector>();
                }
            }
            proximityDetector.SetPlayerId(playerId);

            // UIControllerの初期化
            if (uiController == null)
            {
                uiController = GetComponent<HeartbeatUIController>();
                if (uiController == null)
                {
                    uiController = gameObject.AddComponent<HeartbeatUIController>();
                }
            }

            // AudioManagerの初期化
            if (audioManager == null)
            {
                audioManager = GetComponent<HeartbeatAudioManager>();
                if (audioManager == null)
                {
                    audioManager = gameObject.AddComponent<HeartbeatAudioManager>();
                }
            }
        }

        private void InitializeUI()
        {
            Debug.Log("[HeartbeatManager] UI初期化開始");

            // Canvasの作成または取得
            if (heartbeatCanvas == null)
            {
                GameObject canvasObject = GameObject.Find("HeartbeatCanvas");
                if (canvasObject == null)
                {
                    canvasObject = new GameObject("HeartbeatCanvas");
                    heartbeatCanvas = canvasObject.AddComponent<Canvas>();
                    heartbeatCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    heartbeatCanvas.sortingOrder = 100; // 最前面に表示

                    // CanvasScalerの追加
                    CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);

                    // GraphicRaycasterの追加
                    canvasObject.AddComponent<GraphicRaycaster>();

                    Debug.Log("[HeartbeatManager] HeartbeatCanvasを新規作成");
                }
                else
                {
                    heartbeatCanvas = canvasObject.GetComponent<Canvas>();
                    Debug.Log("[HeartbeatManager] 既存のHeartbeatCanvasを使用");
                }
            }

            // ビネットオーバーレイの作成
            if (vignetteOverlay == null)
            {
                GameObject vignetteObject = new GameObject("VignetteOverlay");
                vignetteObject.transform.SetParent(heartbeatCanvas.transform, false);

                vignetteOverlay = vignetteObject.AddComponent<Image>();
                vignetteOverlay.color = new Color(0, 0, 0, 0);
                vignetteOverlay.raycastTarget = false;

                // 全画面に広げる
                RectTransform rect = vignetteObject.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;

                // ビネット用のグラデーションテクスチャを作成
                CreateVignetteTexture(vignetteOverlay);
            }

            // パルスオーバーレイの作成
            if (pulseOverlay == null)
            {
                GameObject pulseObject = new GameObject("PulseOverlay");
                pulseObject.transform.SetParent(heartbeatCanvas.transform, false);

                pulseOverlay = pulseObject.AddComponent<Image>();
                pulseOverlay.color = new Color(0.8f, 0, 0, 0);
                pulseOverlay.raycastTarget = false;

                // 全画面に広げる
                RectTransform rect = pulseObject.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;
            }

            // 危険オーバーレイの作成
            if (dangerOverlay == null)
            {
                GameObject dangerObject = new GameObject("DangerOverlay");
                dangerObject.transform.SetParent(heartbeatCanvas.transform, false);

                dangerOverlay = dangerObject.AddComponent<CanvasGroup>();
                dangerOverlay.alpha = 0;
                dangerOverlay.interactable = false;
                dangerOverlay.blocksRaycasts = false;

                Image dangerImage = dangerObject.AddComponent<Image>();
                dangerImage.color = new Color(1, 0, 0, 0.2f);

                // 全画面に広げる
                RectTransform rect = dangerObject.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;
            }

            // UIControllerにUI要素を設定
            if (uiController != null)
            {
                // リフレクションまたはpublicメソッドでUI要素を設定
                System.Type type = uiController.GetType();

                var vignetteField = type.GetField("vignetteImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (vignetteField != null) vignetteField.SetValue(uiController, vignetteOverlay);

                var pulseField = type.GetField("pulseEffectImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (pulseField != null) pulseField.SetValue(uiController, pulseOverlay);

                var dangerField = type.GetField("dangerOverlay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (dangerField != null) dangerField.SetValue(uiController, dangerOverlay);

                Debug.Log("[HeartbeatManager] UIController にエフェクト要素を設定完了");
            }

            // デバッガーの追加
            var debugger = GetComponent<HeartbeatDebugger>();
            if (debugger == null)
            {
                gameObject.AddComponent<HeartbeatDebugger>();
                Debug.Log("[HeartbeatManager] HeartbeatDebugger を追加");
            }

            Debug.Log("[HeartbeatManager] UI初期化完了");
        }

        private void CreateVignetteTexture(Image targetImage)
        {
            // ビネット効果用のグラデーションテクスチャを作成
            int size = 256;
            Texture2D vignetteTexture = new Texture2D(size, size);
            Color[] colors = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distX = Mathf.Abs(x - size * 0.5f) / (size * 0.5f);
                    float distY = Mathf.Abs(y - size * 0.5f) / (size * 0.5f);
                    float dist = Mathf.Sqrt(distX * distX + distY * distY);
                    dist = Mathf.Clamp01(dist);

                    float alpha = Mathf.Pow(dist, 2f); // 二乗でグラデーション
                    colors[y * size + x] = new Color(0, 0, 0, alpha);
                }
            }

            vignetteTexture.SetPixels(colors);
            vignetteTexture.Apply();

            targetImage.sprite = Sprite.Create(vignetteTexture, new Rect(0, 0, size, size), Vector2.one * 0.5f);
        }

        public void SetPlayerId(int id)
        {
            playerId = id;
            PlayerPrefs.SetInt("PlayerID", id);
            PlayerPrefs.Save();

            // 各コンポーネントに通知
            if (heartbeatSystem != null) heartbeatSystem.SetPlayerId(id);
            if (proximityDetector != null) proximityDetector.SetPlayerId(id);

            Debug.Log($"[HeartbeatManager] PlayerID変更: {id}");
        }

        public void StartHeartbeatSystem()
        {
            if (!isInitialized)
            {
                Initialize();
            }

            // BLEスキャンを開始
            if (bleBeacon != null)
            {
                bleBeacon.StartScanning();
                bleBeacon.StartAdvertising();
            }

            Debug.Log("[HeartbeatManager] 心音システムを開始");
        }

        public void StopHeartbeatSystem()
        {
            // BLEスキャンを停止
            if (bleBeacon != null)
            {
                bleBeacon.StopScanning();
                bleBeacon.StopAdvertising();
            }

            // オーディオを停止
            if (audioManager != null)
            {
                audioManager.StopAllAudio();
            }

            Debug.Log("[HeartbeatManager] 心音システムを停止");
        }

        void OnDestroy()
        {
            StopHeartbeatSystem();

            if (_instance == this)
            {
                _instance = null;
            }
        }

        #if UNITY_EDITOR
        void OnGUI()
        {
            int y = 10;

            GUI.Label(new Rect(10, y, 300, 25), "=== 心音マネージャー ===");
            y += 25;

            GUI.Label(new Rect(10, y, 300, 25), $"PlayerID: {playerId} ({(playerId == 1000 ? "キラー" : "サバイバー")})");
            y += 25;

            GUI.Label(new Rect(10, y, 300, 25), $"システム状態: {(isInitialized ? "初期化済み" : "未初期化")}");
            y += 30;

            if (GUI.Button(new Rect(10, y, 100, 30), "開始"))
            {
                StartHeartbeatSystem();
            }

            if (GUI.Button(new Rect(120, y, 100, 30), "停止"))
            {
                StopHeartbeatSystem();
            }
        }
        #endif
    }
}