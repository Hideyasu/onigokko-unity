using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Onigokko.BLE
{
    /// <summary>
    /// iOS専用 BLE iBeacon システム
    /// 鬼vs陰陽師ゲーム用の近接検出
    /// </summary>
    public class iOSBLEBeacon : MonoBehaviour
    {
        [Header("ゲーム設定")]
        [SerializeField] private string gameUUID = "550e8400-e29b-41d4-a716-446655440000";
        [SerializeField] private int sessionId = 1;
        [SerializeField] private int playerId = 1001;

        [Header("検証用設定")]
        [SerializeField] private bool autoStartAdvertising = true;
        [SerializeField] private bool autoStartScanning = true;
        [SerializeField] private bool showDebugUI = true;

        // 検出されたビーコン情報
        private Dictionary<string, BeaconInfo> detectedBeacons = new Dictionary<string, BeaconInfo>();
        private bool isAdvertising = false;
        private bool isScanning = false;

        [System.Serializable]
        public class BeaconInfo
        {
            public string uuid;
            public int major;
            public int minor;
            public float distance;
            public float rssi;
            public DateTime lastSeen;

            public override string ToString()
            {
                return $"Beacon({major}.{minor}): {distance:F1}m, RSSI:{rssi:F0}";
            }
        }

        #region Unity Lifecycle

        void Start()
        {
            // PlayerIdManagerからPlayer IDを取得
            var playerIdManager = Onigokko.Heartbeat.PlayerIdManager.Instance;
            if (playerIdManager != null)
            {
                playerId = playerIdManager.GetPlayerId();
                Debug.Log($"[BLE] PlayerIdManagerからID取得: {playerId}");
            }
            else
            {
                // フォールバック: PlayerPrefsから取得
                playerId = PlayerPrefs.GetInt("PlayerID", 1001);
                Debug.Log($"[BLE] PlayerPrefsからID取得: {playerId}");
            }

            #if UNITY_IOS && !UNITY_EDITOR
                InitializeNativePlugin();

                if (autoStartAdvertising)
                {
                    StartAdvertising();
                }

                if (autoStartScanning)
                {
                    StartScanning();
                }
            #else
                Debug.LogWarning($"[BLE] iOS BLE Beacon はiOSビルドでのみ動作します - PlayerID: {playerId}");
            #endif
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                Debug.Log("[BLE] アプリがバックグラウンドに移行");
                // iOS では バックグラウンドでもiBeaconは動作継続
            }
            else
            {
                Debug.Log("[BLE] アプリがフォアグラウンドに復帰");
            }
        }

        void OnDestroy()
        {
            #if UNITY_IOS && !UNITY_EDITOR
                StopAdvertising();
                StopScanning();
            #endif
        }

        #endregion

        #region Native Plugin Interface

        // デリゲート定義（プリプロセッサの外で定義）
        private delegate void BeaconDetectedCallback(string uuid, int major, int minor, float distance, float rssi);

        #if UNITY_IOS && !UNITY_EDITOR

        [DllImport("__Internal")]
        private static extern void _SetBeaconDetectedCallback(BeaconDetectedCallback callback);

        [DllImport("__Internal")]
        private static extern void _StartAdvertising(string uuid, int major, int minor);

        [DllImport("__Internal")]
        private static extern void _StopAdvertising();

        [DllImport("__Internal")]
        private static extern void _StartScanning(string uuid);

        [DllImport("__Internal")]
        private static extern void _StopScanning();

        [DllImport("__Internal")]
        private static extern float _GetDistanceToBeacon(int major, int minor);

        #endif

        private void InitializeNativePlugin()
        {
            #if UNITY_IOS && !UNITY_EDITOR
                _SetBeaconDetectedCallback(OnBeaconDetected);
                Debug.Log("[BLE] Native plugin initialized");
            #endif
        }

        #endregion

        #region Public API

        /// <summary>
        /// iBeacon アドバタイジング開始
        /// </summary>
        public void StartAdvertising()
        {
            #if UNITY_IOS && !UNITY_EDITOR
                _StartAdvertising(gameUUID, sessionId, playerId);
                isAdvertising = true;
                Debug.Log($"[BLE] アドバタイジング開始 - SessionID: {sessionId}, PlayerID: {playerId}");
            #else
                Debug.Log("[BLE] [SIMULATOR] アドバタイジング開始");
                isAdvertising = true;
            #endif
        }

        /// <summary>
        /// iBeacon アドバタイジング停止
        /// </summary>
        public void StopAdvertising()
        {
            #if UNITY_IOS && !UNITY_EDITOR
                _StopAdvertising();
            #endif
            isAdvertising = false;
            Debug.Log("[BLE] アドバタイジング停止");
        }

        /// <summary>
        /// iBeacon スキャン開始
        /// </summary>
        public void StartScanning()
        {
            #if UNITY_IOS && !UNITY_EDITOR
                _StartScanning(gameUUID);
                isScanning = true;
                Debug.Log("[BLE] スキャン開始");
            #else
                Debug.Log("[BLE] [SIMULATOR] スキャン開始");
                isScanning = true;

                // シミュレータ用のモックデータ
                SimulateBeaconDetection();
            #endif
        }

        /// <summary>
        /// iBeacon スキャン停止
        /// </summary>
        public void StopScanning()
        {
            #if UNITY_IOS && !UNITY_EDITOR
                _StopScanning();
            #endif
            isScanning = false;
            Debug.Log("[BLE] スキャン停止");
        }

        /// <summary>
        /// 特定プレイヤーとの距離を取得
        /// </summary>
        public float GetDistanceToPlayer(int targetPlayerId)
        {
            #if UNITY_IOS && !UNITY_EDITOR
                return _GetDistanceToBeacon(sessionId, targetPlayerId);
            #else
                // シミュレータ用
                return UnityEngine.Random.Range(1f, 50f);
            #endif
        }

        /// <summary>
        /// 近くのプレイヤーリストを取得
        /// </summary>
        public List<BeaconInfo> GetNearbyPlayers()
        {
            var result = new List<BeaconInfo>();

            foreach (var beacon in detectedBeacons.Values)
            {
                // 5分以内に検出されたビーコンのみ
                if ((DateTime.Now - beacon.lastSeen).TotalMinutes < 5)
                {
                    result.Add(beacon);
                }
            }

            // 距離でソート
            result.Sort((a, b) => a.distance.CompareTo(b.distance));
            return result;
        }

        /// <summary>
        /// 最も近いプレイヤーとの距離
        /// </summary>
        public float GetNearestPlayerDistance()
        {
            var nearbyPlayers = GetNearbyPlayers();
            return nearbyPlayers.Count > 0 ? nearbyPlayers[0].distance : float.MaxValue;
        }

        #endregion

        #region Callbacks

        /// <summary>
        /// ネイティブプラグインからのコールバック
        /// </summary>
        [AOT.MonoPInvokeCallback(typeof(BeaconDetectedCallback))]
        private static void OnBeaconDetected(string uuid, int major, int minor, float distance, float rssi)
        {
            // メインスレッドで実行するためにキューに追加
            if (Instance != null)
            {
                Instance.QueueBeaconUpdate(uuid, major, minor, distance, rssi);
            }
        }

        private void QueueBeaconUpdate(string uuid, int major, int minor, float distance, float rssi)
        {
            var beaconKey = $"{major}_{minor}";

            if (!detectedBeacons.ContainsKey(beaconKey))
            {
                detectedBeacons[beaconKey] = new BeaconInfo();
            }

            var beacon = detectedBeacons[beaconKey];
            beacon.uuid = uuid;
            beacon.major = major;
            beacon.minor = minor;
            beacon.distance = distance;
            beacon.rssi = rssi;
            beacon.lastSeen = DateTime.Now;

            Debug.Log($"[BLE] ビーコン検出: {beacon}");
        }

        #endregion

        #region Singleton

        private static iOSBLEBeacon _instance;
        public static iOSBLEBeacon Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<iOSBLEBeacon>();
                }
                return _instance;
            }
        }

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
            }
        }

        #endregion

        #region Debug & Testing

        private void SimulateBeaconDetection()
        {
            // エディタ・シミュレータ用のモックビーコン
            InvokeRepeating(nameof(GenerateMockBeacon), 2f, 3f);
        }

        private void GenerateMockBeacon()
        {
            if (!isScanning) return;

            // 自分のPlayer IDを取得
            var playerIdManager = Onigokko.Heartbeat.PlayerIdManager.Instance;
            int myPlayerId = playerIdManager != null ? playerIdManager.GetPlayerId() : playerId;

            // キラーかサバイバーかを判定
            bool isKiller = (myPlayerId == 1000);

            int mockPlayerId;
            if (isKiller)
            {
                // キラーの場合：サバイバーを検出（1001-1006の6人）
                mockPlayerId = UnityEngine.Random.Range(1001, 1007);
            }
            else
            {
                // サバイバーの場合：キラーを検出
                mockPlayerId = 1000;
            }

            float mockDistance = UnityEngine.Random.Range(0.5f, 30f);
            float mockRssi = -40f - (mockDistance * 2f); // 距離に応じたRSSI

            Debug.Log($"[BLE] モック生成 - 自分:{myPlayerId} → 検出:{mockPlayerId}");
            QueueBeaconUpdate(gameUUID, sessionId, mockPlayerId, mockDistance, mockRssi);
        }

        void OnGUI()
        {
            if (!showDebugUI) return;

            int y = 50;
            GUI.Label(new Rect(10, y, 300, 25), $"BLE Status - Adv: {isAdvertising}, Scan: {isScanning}");
            y += 30;

            GUI.Label(new Rect(10, y, 300, 25), $"Player ID: {playerId}, Session: {sessionId}");
            y += 30;

            GUI.Label(new Rect(10, y, 300, 25), "=== 検出されたビーコン ===");
            y += 25;

            var nearbyPlayers = GetNearbyPlayers();
            if (nearbyPlayers.Count == 0)
            {
                GUI.Label(new Rect(10, y, 300, 25), "ビーコンが検出されていません");
            }
            else
            {
                foreach (var beacon in nearbyPlayers)
                {
                    string info = $"Player {beacon.minor}: {beacon.distance:F1}m (RSSI: {beacon.rssi:F0})";
                    GUI.Label(new Rect(10, y, 350, 25), info);
                    y += 25;
                }
            }

            y += 10;
            if (GUI.Button(new Rect(10, y, 120, 30), isAdvertising ? "停止" : "アドバタイズ"))
            {
                if (isAdvertising) StopAdvertising();
                else StartAdvertising();
            }

            if (GUI.Button(new Rect(140, y, 120, 30), isScanning ? "停止" : "スキャン"))
            {
                if (isScanning) StopScanning();
                else StartScanning();
            }
        }

        #endregion
    }
}