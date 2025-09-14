using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Firebase.Database;
using Onigokko.Location;

namespace Onigokko.Map
{
    /// <summary>
    /// Map検証システム
    /// GPS位置情報とマップ表示の検証
    /// OpenStreetMapタイルを使用し、現在地を追従するように変更
    /// </summary>
    public class MapVerificationSystem : MonoBehaviour
    {
        [Header("マップ設定")]
        [SerializeField] private bool showDebugUI = true;
        [SerializeField] private float updateInterval = 1f;
        [SerializeField] private float gameAreaRadius = 500f; // ゲームエリア半径（メートル）

        [Header("ゲームエリア中心（固定）")]
        [SerializeField] private double centerLatitude = 35.6762;  // 東京駅
        [SerializeField] private double centerLongitude = 139.6503;

        [Header("OpenStreetMap 設定")]
        [Tooltip("マップタイルを表示するUI RawImage。インスペクターで設定してください。")]
        [SerializeField] private RawImage mapDisplay;
        [Tooltip("マップのズームレベル")]
        [SerializeField] private int mapZoom = 16;
        [Tooltip("表示するタイルグリッドのサイズ (N x N)。奇数を推奨。")]
        [SerializeField] private int mapSize = 3;

        [Header("テスト用プレイヤー位置")]
        [SerializeField] private List<TestPlayer> testPlayers = new List<TestPlayer>();

        // コンポーネント
        private GPSLocationService gpsService;

        // マップデータ
        private GPSLocationService.Vector2d gameAreaCenter; // ゲームエリアの中心（固定）
        private GPSLocationService.Vector2d mapViewCenter;  // マップ表示の中心（現在地を追従）
        private List<MapMarker> mapMarkers = new List<MapMarker>();
        
        // Firebase関連
        private DatabaseReference databaseRef;
        private string roomId;
        private string userId;
        private float lastFirebaseUpdateTime;
        
        // UI関連
        private GUIStyle markerLabelStyle;
        private Coroutine updateMapTilesCoroutine;

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

            // ゲームエリア中心（固定）とマップ表示中心（可変）を初期化
            gameAreaCenter = new GPSLocationService.Vector2d(centerLatitude, centerLongitude);
            mapViewCenter = gameAreaCenter;

            // GPS イベントを購読
            gpsService.OnLocationUpdated += OnLocationUpdated;
            gpsService.OnLocationError += OnLocationError;

            // マップマーカーを初期化
            InitializeMapMarkers();
            
            // Firebase初期化
            InitializeFirebase();

            // OpenStreetMap タイルをダウンロード
            if (mapDisplay != null)
            {
                updateMapTilesCoroutine = StartCoroutine(UpdateMapTiles());
            }
            else
            {
                Debug.LogWarning("[Map] mapDisplayが設定されていません。OpenStreetMapは表示されません。");
            }

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
        /// Firebase初期化
        /// </summary>
        private void InitializeFirebase()
        {
            roomId = PlayerPrefs.GetString("RoomID", "");
            userId = PlayerPrefs.GetString("PlayerID", "");
            
            if (string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(userId))
            {
                Debug.LogError("[Map] RoomID or PlayerID not found in PlayerPrefs");
                return;
            }
            
            databaseRef = FirebaseDatabase.DefaultInstance.RootReference;
            Debug.Log($"[Map] Firebase初期化完了 - RoomID: {roomId}, UserID: {userId}");
        }

        /// <summary>
        /// 位置情報更新時のコールバック
        /// </summary>
        private void OnLocationUpdated(LocationInfo locationInfo)
        {
            var currentPos = gpsService.CurrentPosition;

            // --- マップの再センタリング判定 ---
            var oldTile = LatLonToTile(mapViewCenter, mapZoom);
            var newTile = LatLonToTile(currentPos, mapZoom);

            mapViewCenter = currentPos;

            if (mapDisplay != null && (oldTile.x != newTile.x || oldTile.y != newTile.y))
            {
                if (updateMapTilesCoroutine != null)
                {
                    StopCoroutine(updateMapTilesCoroutine);
                }
                updateMapTilesCoroutine = StartCoroutine(UpdateMapTiles());
            }
            // --- ここまで ---

            // ゲームエリア関連のロジック（中心は固定の gameAreaCenter を使用）
            bool isInGameArea = gpsService.IsWithinGameArea(gameAreaCenter, gameAreaRadius);
            double distanceFromCenter = GPSLocationService.CalculateDistance(currentPos, gameAreaCenter);

            UpdatePlayerDistances();
            UpdateMapMarkers();
            
            if (Time.time - lastFirebaseUpdateTime >= 3f)
            {
                SendLocationToFirebase(currentPos);
                lastFirebaseUpdateTime = Time.time;
            }

            if (showDebugUI)
            {
                Debug.Log($"[Map] 位置更新 - エリア内: {isInGameArea}, 中心距離: {distanceFromCenter:F1}m");
            }
        }

        /// <summary>
        /// Firebase に位置情報を送信
        /// </summary>
        private async void SendLocationToFirebase(GPSLocationService.Vector2d position)
        {
            if (databaseRef == null || string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(userId)) return;
            
            try
            {
                var userLocationRef = databaseRef.Child("rooms").Child(roomId).Child("users").Child(userId);
                await userLocationRef.Child("lat").SetValueAsync(position.latitude);
                await userLocationRef.Child("lng").SetValueAsync(position.longitude);
                Debug.Log($"[Map] 位置情報送信完了 - Lat: {position.latitude:F6}, Lng: {position.longitude:F6}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Map] Firebase位置情報送信エラー: {e.Message}");
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

        #region OpenStreetMap Integration

        private IEnumerator UpdateMapTiles()
        {
            if (mapDisplay == null) yield break;

            var centerTile = LatLonToTile(mapViewCenter, mapZoom);
            int halfSize = mapSize / 2;
            int tileSize = 256;
            
            Texture2D mapTexture = new Texture2D(mapSize * tileSize, mapSize * tileSize);
            mapDisplay.texture = mapTexture;

            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    int tileX = centerTile.x - halfSize + x;
                    int tileY = centerTile.y - halfSize + y;

                    string url = $"https://tile.openstreetmap.org/{mapZoom}/{tileX}/{tileY}.png";
                    using (var www = UnityWebRequestTexture.GetTexture(url))
                    {
                        www.SetRequestHeader("User-Agent", "Onigokko-Unity-Client/1.0");
                        yield return www.SendWebRequest();

                        if (www.result == UnityWebRequest.Result.Success)
                        {
                            Texture2D tileTexture = DownloadHandlerTexture.GetContent(www);
                            mapTexture.SetPixels(x * tileSize, (mapSize - 1 - y) * tileSize, tileSize, tileSize, tileTexture.GetPixels());
                            mapTexture.Apply();
                        }
                        else
                        {
                            Debug.LogError($"[Map] タイルのダウンロードに失敗: {url} - {www.error}");
                        }
                    }
                }
            }
            Debug.Log("[Map] OpenStreetMapタイルの更新完了。");
        }

        public Vector2 WorldToScreenPosition(GPSLocationService.Vector2d worldPos, Rect mapRect)
        {
            var centerTile = LatLonToTile(mapViewCenter, mapZoom);
            int halfSize = mapSize / 2;
            
            var topLeftTile = new Vector2Int(centerTile.x - halfSize, centerTile.y - halfSize);
            var bottomRightTile = new Vector2Int(topLeftTile.x + mapSize, topLeftTile.y + mapSize);

            var topLeftLon = TileToLon(topLeftTile.x, mapZoom);
            var topLeftLat = TileToLat(topLeftTile.y, mapZoom);
            var bottomRightLon = TileToLon(bottomRightTile.x, mapZoom);
            var bottomRightLat = TileToLat(bottomRightTile.y, mapZoom);

            float normalizedX = (float)((worldPos.longitude - topLeftLon) / (bottomRightLon - topLeftLon));

            double worldPosMercator = LatToMercator(worldPos.latitude);
            double topLeftMercator = LatToMercator(topLeftLat);
            double bottomRightMercator = LatToMercator(bottomRightLat);
            
            float normalizedY = (float)((worldPosMercator - topLeftMercator) / (bottomRightMercator - topLeftMercator));

            return new Vector2(
                mapRect.x + normalizedX * mapRect.width,
                mapRect.y + (1 - normalizedY) * mapRect.height
            );
        }

        private Vector2Int LatLonToTile(GPSLocationService.Vector2d pos, int zoom)
        {
            int x = (int)((pos.longitude + 180.0) / 360.0 * (1 << zoom));
            int y = (int)((1.0 - System.Math.Log(System.Math.Tan(pos.latitude * System.Math.PI / 180.0) + 1.0 / System.Math.Cos(pos.latitude * System.Math.PI / 180.0)) / System.Math.PI) / 2.0 * (1 << zoom));
            return new Vector2Int(x, y);
        }

        private double TileToLon(int x, int z) => x / System.Math.Pow(2, z) * 360.0 - 180.0;

        private double TileToLat(int y, int z) {
            double n = System.Math.PI - 2.0 * System.Math.PI * y / System.Math.Pow(2, z);
            return 180.0 / System.Math.PI * System.Math.Atan(0.5 * (System.Math.Exp(n) - System.Math.Exp(-n)));
        }

        private double LatToMercator(double lat) => System.Math.Log(System.Math.Tan((90 + lat) * System.Math.PI / 360.0));

        #endregion

        void OnGUI()
        {
            if (!showDebugUI) return;

            DrawLocationInfo();
            DrawMapOverlay();
            DrawPlayerList();
        }

        private void DrawLocationInfo()
        {
            int y = 300;
            GUI.Label(new Rect(10, y, 400, 25), "=== Map Verification (OSM) ===");
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

        private void DrawMapOverlay()
        {
            if (mapDisplay == null || mapDisplay.texture == null) return;

            // RawImageの実際のスクリーン座標を取得
            Vector3[] corners = new Vector3[4];
            mapDisplay.rectTransform.GetWorldCorners(corners);
            // corners[0] = bottom-left, [1] = top-left, [2] = top-right, [3] = bottom-right
            // OnGUIの座標系に変換 (Y軸が逆)
            Rect mapRect = new Rect(
                corners[1].x,
                Screen.height - corners[1].y,
                corners[2].x - corners[1].x,
                corners[1].y - corners[0].y
            );

            if (markerLabelStyle == null)
            {
                markerLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 10
                };
            }

            foreach (var marker in mapMarkers)
            {
                Vector2 screenPos = WorldToScreenPosition(marker.position, mapRect);

                if (!mapRect.Contains(screenPos)) continue;

                bool isCurrentUser = (marker.label == "現在位置");
                
                // --- 現在位置マーカーに脈動効果を追加 ---
                float pulse = isCurrentUser ? (Mathf.Sin(Time.time * 5f) + 1f) * 2f : 0f; // 0pxから4pxへ脈動
                float size = (isCurrentUser ? 16f : 12f) + pulse; // ベースサイズに脈動を加える
                
                Rect borderRect = new Rect(screenPos.x - size / 2, screenPos.y - size / 2, size, size);
                GUI.color = isCurrentUser ? Color.white : Color.black;
                GUI.DrawTexture(borderRect, Texture2D.whiteTexture);

                Rect markerRect = new Rect(borderRect.x + 2, borderRect.y + 2, borderRect.width - 4, borderRect.height - 4);
                GUI.color = marker.color;
                GUI.DrawTexture(markerRect, Texture2D.whiteTexture);
                
                GUIContent labelContent = new GUIContent(marker.label);
                Vector2 labelSize = markerLabelStyle.CalcSize(labelContent);
                Rect labelRect = new Rect(screenPos.x - labelSize.x / 2 - 2, screenPos.y + size / 2 + 2, labelSize.x + 4, labelSize.y + 2);

                GUI.color = new Color(0, 0, 0, 0.6f);
                GUI.DrawTexture(labelRect, Texture2D.whiteTexture);

                markerLabelStyle.normal.textColor = Color.white;
                GUI.Label(labelRect, labelContent, markerLabelStyle);
            }

            GUI.color = Color.white;
            GUI.Label(new Rect(mapRect.x, mapRect.y - 20, 200, 20), "OpenStreetMap");
        }

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