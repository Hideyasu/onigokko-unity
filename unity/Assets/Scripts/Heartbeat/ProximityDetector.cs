using System.Collections.Generic;
using UnityEngine;
using Onigokko.BLE;

namespace Onigokko.Heartbeat
{
    /// <summary>
    /// 近接検出システム - BLEを使用してキラーとサバイバーの距離を検出
    /// </summary>
    public class ProximityDetector : MonoBehaviour
    {
        [Header("検出設定")]
        [SerializeField] private float updateInterval = 0.5f;       // 更新間隔（秒）
        [SerializeField] private float maxDetectionRange = 100f;    // 最大検出範囲（メートル）

        [Header("プレイヤー設定")]
        [SerializeField] private int myPlayerId = 1001;
        private const int KILLER_ID = 1000;                        // キラーのID固定値

        private iOSBLEBeacon bleBeacon;
        private float lastUpdateTime;
        private float distanceToKiller = float.MaxValue;
        private List<PlayerInfo> nearbyPlayers = new List<PlayerInfo>();

        [System.Serializable]
        public class PlayerInfo
        {
            public int playerId;
            public float distance;
            public float rssi;
            public bool isKiller;

            public PlayerInfo(int id, float dist, float rssiValue)
            {
                playerId = id;
                distance = dist;
                rssi = rssiValue;
                isKiller = (id == KILLER_ID);
            }
        }

        void Start()
        {
            InitializeDetector();
        }

        private void InitializeDetector()
        {
            // PlayerIdManagerからPlayer IDを取得（最優先）
            var playerIdManager = PlayerIdManager.Instance;
            if (playerIdManager != null)
            {
                myPlayerId = playerIdManager.GetPlayerId();
                Debug.Log($"[ProximityDetector] PlayerIdManagerからID取得: {myPlayerId}");
            }
            else
            {
                // フォールバック: PlayerPrefsから取得
                myPlayerId = PlayerPrefs.GetInt("PlayerID", 1001);
                Debug.Log($"[ProximityDetector] PlayerPrefsからID取得: {myPlayerId}");
            }

            // BLEビーコンの取得
            bleBeacon = iOSBLEBeacon.Instance;
            if (bleBeacon == null)
            {
                Debug.LogWarning("[ProximityDetector] iOSBLEBeacon が見つかりません - エディタではモック生成を使用");
                // エディタ環境ではBLEBeaconは作成しない（モック生成を使用）
                #if !UNITY_EDITOR
                // 実機でのみBLEBeaconコンポーネントを作成
                GameObject bleObject = new GameObject("BLEBeacon");
                bleBeacon = bleObject.AddComponent<iOSBLEBeacon>();
                DontDestroyOnLoad(bleObject);
                #endif
            }

            Debug.Log($"[ProximityDetector] 初期化完了 - PlayerID: {myPlayerId}, IsKiller: {IsKiller()}");
        }

        void Update()
        {
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                lastUpdateTime = Time.time;
                UpdateProximity();
            }
        }

        private void UpdateProximity()
        {
            if (bleBeacon == null)
            {
                #if UNITY_EDITOR
                // エディタ環境ではモック生成
                SimulateProximity();
                #endif
                return;
            }

            // 近くのビーコン（プレイヤー）を取得
            var beacons = bleBeacon.GetNearbyPlayers();
            nearbyPlayers.Clear();
            distanceToKiller = float.MaxValue;

            foreach (var beacon in beacons)
            {
                // minorがプレイヤーIDとして使用されている
                int playerId = beacon.minor;
                float distance = beacon.distance;

                // 最大検出範囲内のプレイヤーのみ
                if (distance <= maxDetectionRange)
                {
                    var playerInfo = new PlayerInfo(playerId, distance, beacon.rssi);
                    nearbyPlayers.Add(playerInfo);

                    // キラーとの距離を更新
                    if (playerId == KILLER_ID && myPlayerId != KILLER_ID)
                    {
                        distanceToKiller = distance;
                        OnKillerDetected(distance);
                    }
                }
            }

            // デバッグログ
            if (nearbyPlayers.Count > 0)
            {
                Debug.Log($"[ProximityDetector] 検出プレイヤー数: {nearbyPlayers.Count}, キラーとの距離: {distanceToKiller:F1}m");
            }
        }

        private void OnKillerDetected(float distance)
        {
            // キラー検出時の処理
            Debug.Log($"[ProximityDetector] キラー検出！ 距離: {distance:F1}m");
        }

        /// <summary>
        /// キラーとの距離を取得
        /// </summary>
        public float GetDistanceToKiller()
        {
            // 自分がキラーの場合は0を返す
            if (myPlayerId == KILLER_ID)
            {
                return 0f;
            }

            return distanceToKiller;
        }

        /// <summary>
        /// 最も近いプレイヤーとの距離を取得
        /// </summary>
        public float GetNearestPlayerDistance()
        {
            if (nearbyPlayers.Count == 0)
            {
                return float.MaxValue;
            }

            float minDistance = float.MaxValue;
            foreach (var player in nearbyPlayers)
            {
                if (player.playerId != myPlayerId && player.distance < minDistance)
                {
                    minDistance = player.distance;
                }
            }

            return minDistance;
        }

        /// <summary>
        /// 指定した距離内のプレイヤーリストを取得
        /// </summary>
        public List<PlayerInfo> GetPlayersInRange(float range)
        {
            var result = new List<PlayerInfo>();
            foreach (var player in nearbyPlayers)
            {
                if (player.distance <= range && player.playerId != myPlayerId)
                {
                    result.Add(player);
                }
            }
            return result;
        }

        /// <summary>
        /// 近くのサバイバーリストを取得（キラー用）
        /// </summary>
        public List<PlayerInfo> GetNearbySurvivors()
        {
            var result = new List<PlayerInfo>();
            if (myPlayerId != KILLER_ID)
            {
                return result; // キラーでない場合は空のリストを返す
            }

            foreach (var player in nearbyPlayers)
            {
                if (!player.isKiller)
                {
                    result.Add(player);
                }
            }

            // 距離でソート
            result.Sort((a, b) => a.distance.CompareTo(b.distance));
            return result;
        }

        /// <summary>
        /// 近くのプレイヤー全員を取得
        /// </summary>
        public List<PlayerInfo> GetAllNearbyPlayers()
        {
            return new List<PlayerInfo>(nearbyPlayers);
        }

        /// <summary>
        /// プレイヤーIDを設定
        /// </summary>
        public void SetPlayerId(int id)
        {
            myPlayerId = id;
            PlayerPrefs.SetInt("PlayerID", id);
            PlayerPrefs.Save();
            Debug.Log($"[ProximityDetector] PlayerID設定: {myPlayerId}");
        }

        /// <summary>
        /// 自分がキラーかどうか
        /// </summary>
        public bool IsKiller()
        {
            return myPlayerId == KILLER_ID;
        }

        #if UNITY_EDITOR
        // エディタ用のモックデータ生成
        void SimulateProximity()
        {
            if (bleBeacon == null)
            {
                // エディタでのテスト用にモックデータを生成
                nearbyPlayers.Clear();

                Debug.Log($"[ProximityDetector] モック生成 - myPlayerId: {myPlayerId}, IsKiller: {IsKiller()}");

                // サバイバーの場合：キラーを検出するモックデータ
                if (myPlayerId != KILLER_ID)
                {
                    float mockKillerDistance = Random.Range(5f, 60f);
                    nearbyPlayers.Add(new PlayerInfo(KILLER_ID, mockKillerDistance, -50f - mockKillerDistance));
                    distanceToKiller = mockKillerDistance;
                    Debug.Log($"[ProximityDetector] キラーをモック検出: 距離 {mockKillerDistance:F1}m");
                }

                // キラーの場合：サバイバーを検出するモックデータ（1001-1006の6人から）
                if (myPlayerId == KILLER_ID)
                {
                    int survivorCount = Random.Range(1, 7); // 1-6人のサバイバーを検出
                    for (int i = 0; i < survivorCount; i++)
                    {
                        int survivorId = 1001 + i;
                        float distance = Random.Range(10f, 80f);
                        nearbyPlayers.Add(new PlayerInfo(survivorId, distance, -50f - distance));
                        Debug.Log($"[ProximityDetector] サバイバー {survivorId} をモック検出: 距離 {distance:F1}m");
                    }
                }

                // サバイバー同士の検出（実際のゲームでも必要）
                if (myPlayerId != KILLER_ID)
                {
                    // 他のサバイバーのモックデータ（1001-1006から自分以外）
                    int otherSurvivorCount = Random.Range(0, 3); // 0-2人の他のサバイバーを検出
                    for (int i = 0; i < otherSurvivorCount; i++)
                    {
                        int otherSurvivorId = Random.Range(1001, 1007); // 1001-1006からランダム選択
                        if (otherSurvivorId != myPlayerId)
                        {
                            float distance = Random.Range(15f, 50f);
                            nearbyPlayers.Add(new PlayerInfo(otherSurvivorId, distance, -50f - distance));
                            Debug.Log($"[ProximityDetector] 他のサバイバー {otherSurvivorId} をモック検出: 距離 {distance:F1}m");
                        }
                    }
                }
            }
        }

        void OnGUI()
        {
            int y = 550;
            GUI.Label(new Rect(10, y, 400, 25), "=== 近接検出システム ===");
            y += 25;
            GUI.Label(new Rect(10, y, 400, 25), $"自分のID: {myPlayerId} ({(IsKiller() ? "キラー" : "サバイバー")})");
            y += 25;

            if (!IsKiller())
            {
                GUI.Label(new Rect(10, y, 400, 25), $"キラーとの距離: {distanceToKiller:F1}m");
                y += 25;
            }

            GUI.Label(new Rect(10, y, 400, 25), $"検出プレイヤー: {nearbyPlayers.Count}人");
            y += 25;

            foreach (var player in nearbyPlayers)
            {
                string role = player.isKiller ? "キラー" : "サバイバー";
                GUI.Label(new Rect(10, y, 400, 20), $"  ID:{player.playerId} ({role}) - {player.distance:F1}m");
                y += 20;
            }
        }
        #endif
    }
}