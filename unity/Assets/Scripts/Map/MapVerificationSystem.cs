using System.Collections.Generic;
using UnityEngine;
using Onigokko.Location;

namespace Onigokko.Map
{
    /// <summary>
    /// Map検証システム
    /// GPS位置情報とマップ表示の検証
    /// </summary>
    public class MapVerificationSystem : MonoBehaviour
    {
        [Header("マップ設定")]
        [SerializeField] private bool showDebugUI = true;
        [SerializeField] private float updateInterval = 1f;
        [SerializeField] private float gameAreaRadius = 500f; // ゲームエリア半径（メートル）

        [Header("ゲームエリア中心")]
        [SerializeField] private double centerLatitude = 35.6762;  // 東京駅
        [SerializeField] private double centerLongitude = 139.6503;

        [Header("テスト用プレイヤー位置")]
        [SerializeField] private List<TestPlayer> testPlayers = new List<TestPlayer>();

        // コンポーネント
        private GPSLocationService gpsService;

        // マップデータ
        private GPSLocationService.Vector2d gameAreaCenter;
        private List<MapMarker> mapMarkers = new List<MapMarker>();

        [System.Serializable]
        public class TestPlayer
        {
            public string playerName;
            public double latitude;
            public double longitude;
            public Color markerColor = Color.red;
            public bool isVisible = true;

            public GPSLocationService.Vector2d Position => new GPSLocationService.Vector2d(latitude, longitude);
        }

        [System.Serializable]
        public class MapMarker
        {
            public string label;
            public GPSLocationService.Vector2d position;
            public Color color;
            public MarkerType type;

            public enum MarkerType
            {
                Player,
                GameAreaCenter,
                RitualPoint,
                Trap
            }
        }

        void Start()
        {
            // GPS サービスを取得または追加
            gpsService = GetComponent<GPSLocationService>();
            if (gpsService == null)
            {
                gpsService = gameObject.AddComponent<GPSLocationService>();
            }

            // ゲームエリア中心を設定
            gameAreaCenter = new GPSLocationService.Vector2d(centerLatitude, centerLongitude);

            // GPS イベントを購読
            gpsService.OnLocationUpdated += OnLocationUpdated;
            gpsService.OnLocationError += OnLocationError;

            // マップマーカーを初期化
            InitializeMapMarkers();

            Debug.Log("[Map] Map検証システム開始");
        }

        /// <summary>
        /// マップマーカーを初期化
        /// </summary>
        private void InitializeMapMarkers()
        {
            mapMarkers.Clear();

            // ゲームエリア中心マーカー
            mapMarkers.Add(new MapMarker
            {
                label = "ゲームエリア中心",
                position = gameAreaCenter,
                color = Color.blue,
                type = MapMarker.MarkerType.GameAreaCenter
            });

            // テストプレイヤーマーカー
            for (int i = 0; i < testPlayers.Count; i++)
            {
                var player = testPlayers[i];
                if (player.isVisible)
                {
                    mapMarkers.Add(new MapMarker
                    {
                        label = player.playerName,
                        position = player.Position,
                        color = player.markerColor,
                        type = MapMarker.MarkerType.Player
                    });
                }
            }
        }

        /// <summary>
        /// 位置情報更新時のコールバック
        /// </summary>
        private void OnLocationUpdated(LocationInfo locationInfo)
        {
            var currentPos = gpsService.CurrentPosition;

            // ゲームエリア内チェック
            bool isInGameArea = gpsService.IsWithinGameArea(gameAreaCenter, gameAreaRadius);
            double distanceFromCenter = GPSLocationService.CalculateDistance(currentPos, gameAreaCenter);

            // テストプレイヤーとの距離計算
            UpdatePlayerDistances();

            // マーカー更新
            UpdateMapMarkers();

            if (showDebugUI)
            {
                Debug.Log($"[Map] 位置更新 - エリア内: {isInGameArea}, 中心距離: {distanceFromCenter:F1}m");
            }
        }

        /// <summary>
        /// プレイヤー間距離を更新
        /// </summary>
        private void UpdatePlayerDistances()
        {
            var currentPos = gpsService.CurrentPosition;

            foreach (var player in testPlayers)
            {
                double distance = GPSLocationService.CalculateDistance(currentPos, player.Position);
                Debug.Log($"[Map] {player.playerName}との距離: {distance:F1}m");
            }
        }

        /// <summary>
        /// マップマーカーを更新
        /// </summary>
        private void UpdateMapMarkers()
        {
            // 現在位置マーカーを更新
            var currentPosMarker = mapMarkers.Find(m => m.label == "現在位置");
            if (currentPosMarker != null)
            {
                currentPosMarker.position = gpsService.CurrentPosition;
            }
            else
            {
                mapMarkers.Add(new MapMarker
                {
                    label = "現在位置",
                    position = gpsService.CurrentPosition,
                    color = Color.green,
                    type = MapMarker.MarkerType.Player
                });
            }
        }

        /// <summary>
        /// 位置情報エラー時のコールバック
        /// </summary>
        private void OnLocationError(string error)
        {
            Debug.LogError($"[Map] GPS エラー: {error}");
        }

        /// <summary>
        /// テストプレイヤーを追加
        /// </summary>
        public void AddTestPlayer(string name, double lat, double lon, Color color)
        {
            testPlayers.Add(new TestPlayer
            {
                playerName = name,
                latitude = lat,
                longitude = lon,
                markerColor = color,
                isVisible = true
            });

            InitializeMapMarkers();
        }

        /// <summary>
        /// ゲームエリアの境界チェック
        /// </summary>
        public bool IsPositionInGameArea(GPSLocationService.Vector2d position)
        {
            double distance = GPSLocationService.CalculateDistance(position, gameAreaCenter);
            return distance <= gameAreaRadius;
        }

        /// <summary>
        /// マップ座標を画面座標に変換（簡易版）
        /// </summary>
        public Vector2 WorldToScreenPosition(GPSLocationService.Vector2d worldPos, Rect mapRect)
        {
            // 簡易的な座標変換（実際のマップでは投影法が必要）
            double mapRange = 0.01; // 緯度経度の表示範囲

            float normalizedX = (float)((worldPos.longitude - gameAreaCenter.longitude) / mapRange + 0.5);
            float normalizedY = (float)((worldPos.latitude - gameAreaCenter.latitude) / mapRange + 0.5);

            return new Vector2(
                mapRect.x + normalizedX * mapRect.width,
                mapRect.y + (1 - normalizedY) * mapRect.height // Y軸反転
            );
        }

        void OnGUI()
        {
            if (!showDebugUI) return;

            DrawLocationInfo();
            DrawSimpleMap();
            DrawPlayerList();
        }

        /// <summary>
        /// 位置情報を描画
        /// </summary>
        private void DrawLocationInfo()
        {
            int y = 300;
            GUI.Label(new Rect(10, y, 400, 25), "=== Map Verification ===");
            y += 25;

            var currentPos = gpsService.CurrentPosition;
            GUI.Label(new Rect(10, y, 400, 25), $"現在位置: {currentPos}");
            y += 25;

            bool isInGameArea = gpsService.IsWithinGameArea(gameAreaCenter, gameAreaRadius);
            double distanceFromCenter = GPSLocationService.CalculateDistance(currentPos, gameAreaCenter);

            GUI.Label(new Rect(10, y, 400, 25), $"ゲームエリア内: {isInGameArea}");
            y += 25;

            GUI.Label(new Rect(10, y, 400, 25), $"中心からの距離: {distanceFromCenter:F1}m");
            y += 25;

            GUI.Label(new Rect(10, y, 400, 25), $"エリア半径: {gameAreaRadius}m");
        }

        /// <summary>
        /// 簡易マップを描画
        /// </summary>
        private void DrawSimpleMap()
        {
            Rect mapRect = new Rect(Screen.width - 250, 50, 200, 200);

            // マップ背景
            GUI.color = Color.white;
            GUI.DrawTexture(mapRect, Texture2D.whiteTexture);

            // ゲームエリア円
            GUI.color = new Color(0, 0, 1, 0.3f);
            Rect circleRect = new Rect(mapRect.center.x - 80, mapRect.center.y - 80, 160, 160);
            GUI.DrawTexture(circleRect, Texture2D.whiteTexture);

            // マーカーを描画
            foreach (var marker in mapMarkers)
            {
                Vector2 screenPos = WorldToScreenPosition(marker.position, mapRect);
                Rect markerRect = new Rect(screenPos.x - 3, screenPos.y - 3, 6, 6);

                GUI.color = marker.color;
                GUI.DrawTexture(markerRect, Texture2D.whiteTexture);

                // ラベル
                GUI.color = Color.black;
                GUI.Label(new Rect(screenPos.x - 30, screenPos.y + 5, 60, 20), marker.label);
            }

            GUI.color = Color.white;
            GUI.Label(new Rect(mapRect.x, mapRect.y - 20, 200, 20), "簡易マップ");
        }

        /// <summary>
        /// プレイヤーリストを描画
        /// </summary>
        private void DrawPlayerList()
        {
            int y = 450;
            GUI.Label(new Rect(10, y, 400, 25), "=== Test Players ===");
            y += 25;

            var currentPos = gpsService.CurrentPosition;

            foreach (var player in testPlayers)
            {
                if (!player.isVisible) continue;

                double distance = GPSLocationService.CalculateDistance(currentPos, player.Position);
                GUI.color = player.markerColor;
                GUI.Label(new Rect(10, y, 400, 20), $"● {player.playerName}: {distance:F1}m");
                y += 20;
            }

            GUI.color = Color.white;
        }

        void OnDestroy()
        {
            if (gpsService != null)
            {
                gpsService.OnLocationUpdated -= OnLocationUpdated;
                gpsService.OnLocationError -= OnLocationError;
            }
        }
    }
}