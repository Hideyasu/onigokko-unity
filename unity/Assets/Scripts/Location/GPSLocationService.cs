using System;
using System.Collections;
using UnityEngine;

namespace Onigokko.Location
{
    /// <summary>
    /// GPS位置情報サービス
    /// Unity Location Servicesを使用したGPS取得システム
    /// </summary>
    public class GPSLocationService : MonoBehaviour
    {
        [Header("GPS設定")]
        [SerializeField] private bool autoStartGPS = true;
        [SerializeField] private float desiredAccuracyInMeters = 5f;
        [SerializeField] private float updateDistanceInMeters = 1f;
        [SerializeField] private int maxWaitTime = 20;

        [Header("デバッグ設定")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool useSimulatedLocation = false;
        [SerializeField] private double simulatedLatitude = 35.6762;
        [SerializeField] private double simulatedLongitude = 139.6503;

        // GPS状態
        public bool IsGPSEnabled => Input.location.isEnabledByUser;
        public LocationServiceStatus GPS_Status => Input.location.status;
        public bool IsLocationReady => GPS_Status == LocationServiceStatus.Running;

        // 現在位置
        public LocationInfo CurrentLocation { get; private set; }
        public Vector2d CurrentPosition => GetCurrentPosition();
        public float CurrentAccuracy => IsLocationReady ? Input.location.lastData.horizontalAccuracy : -1f;

        // イベント
        public System.Action<LocationInfo> OnLocationUpdated;
        public System.Action<string> OnLocationError;

        [System.Serializable]
        public struct Vector2d
        {
            public double latitude;
            public double longitude;

            public Vector2d(double lat, double lon)
            {
                latitude = lat;
                longitude = lon;
            }

            public override string ToString()
            {
                return $"({latitude:F6}, {longitude:F6})";
            }
        }

        void Start()
        {
            if (autoStartGPS)
            {
                StartGPS();
            }
        }

        /// <summary>
        /// GPS開始
        /// </summary>
        public void StartGPS()
        {
            if (useSimulatedLocation)
            {
                Debug.Log("[GPS] シミュレーションモード使用");
                StartCoroutine(SimulateGPS());
                return;
            }

            if (!IsGPSEnabled)
            {
                string error = "GPS が無効です。設定でLocation Servicesを有効にしてください";
                Debug.LogError($"[GPS] {error}");
                OnLocationError?.Invoke(error);
                return;
            }

            Debug.Log("[GPS] GPS開始中...");
            StartCoroutine(StartGPSCoroutine());
        }

        /// <summary>
        /// GPS停止
        /// </summary>
        public void StopGPS()
        {
            Input.location.Stop();
            Debug.Log("[GPS] GPS停止");
        }

        /// <summary>
        /// GPS開始コルーチン
        /// </summary>
        private IEnumerator StartGPSCoroutine()
        {
            // GPS開始
            Input.location.Start(desiredAccuracyInMeters, updateDistanceInMeters);

            int waitTime = 0;
            while (GPS_Status == LocationServiceStatus.Initializing && waitTime < maxWaitTime)
            {
                yield return new WaitForSeconds(1);
                waitTime++;
            }

            if (waitTime >= maxWaitTime)
            {
                string error = "GPS初期化がタイムアウトしました";
                Debug.LogError($"[GPS] {error}");
                OnLocationError?.Invoke(error);
                yield break;
            }

            if (GPS_Status == LocationServiceStatus.Failed)
            {
                string error = "GPS初期化に失敗しました。権限を確認してください";
                Debug.LogError($"[GPS] {error}");
                OnLocationError?.Invoke(error);
                yield break;
            }

            Debug.Log("[GPS] GPS開始完了");
            StartCoroutine(UpdateLocationCoroutine());
        }

        /// <summary>
        /// 位置情報更新コルーチン
        /// </summary>
        private IEnumerator UpdateLocationCoroutine()
        {
            while (IsLocationReady)
            {
                CurrentLocation = Input.location.lastData;
                OnLocationUpdated?.Invoke(CurrentLocation);

                if (showDebugInfo)
                {
                    Debug.Log($"[GPS] 位置更新: {CurrentPosition} (精度: {CurrentAccuracy:F1}m)");
                }

                yield return new WaitForSeconds(1f);
            }
        }

        /// <summary>
        /// シミュレーションGPS
        /// </summary>
        private IEnumerator SimulateGPS()
        {
            yield return new WaitForSeconds(2f); // 初期化をシミュレート

            while (true)
            {
                // 少しずつ位置を変化させる（歩行をシミュレート）
                simulatedLatitude += UnityEngine.Random.Range(-0.00001f, 0.00001f);
                simulatedLongitude += UnityEngine.Random.Range(-0.00001f, 0.00001f);

                var simulatedLocationInfo = new LocationInfo
                {
                    latitude = (float)simulatedLatitude,
                    longitude = (float)simulatedLongitude,
                    altitude = 10f,
                    horizontalAccuracy = 3f,
                    verticalAccuracy = 10f,
                    timestamp = DateTimeOffset.Now.ToUnixTimeSeconds()
                };

                CurrentLocation = simulatedLocationInfo;
                OnLocationUpdated?.Invoke(CurrentLocation);

                if (showDebugInfo)
                {
                    Debug.Log($"[GPS] [SIM] 位置更新: {CurrentPosition} (精度: 3.0m)");
                }

                yield return new WaitForSeconds(1f);
            }
        }

        /// <summary>
        /// 現在位置を取得
        /// </summary>
        public Vector2d GetCurrentPosition()
        {
            if (useSimulatedLocation)
            {
                return new Vector2d(simulatedLatitude, simulatedLongitude);
            }

            if (IsLocationReady)
            {
                return new Vector2d(CurrentLocation.latitude, CurrentLocation.longitude);
            }

            return new Vector2d(0, 0);
        }

        /// <summary>
        /// 2点間の距離を計算（メートル）
        /// </summary>
        public static double CalculateDistance(Vector2d pos1, Vector2d pos2)
        {
            const double earthRadius = 6371000; // 地球の半径（メートル）

            double lat1Rad = pos1.latitude * Math.PI / 180;
            double lat2Rad = pos2.latitude * Math.PI / 180;
            double deltaLatRad = (pos2.latitude - pos1.latitude) * Math.PI / 180;
            double deltaLonRad = (pos2.longitude - pos1.longitude) * Math.PI / 180;

            double a = Math.Sin(deltaLatRad / 2) * Math.Sin(deltaLatRad / 2) +
                      Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                      Math.Sin(deltaLonRad / 2) * Math.Sin(deltaLonRad / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadius * c;
        }

        /// <summary>
        /// ゲームエリア内かどうかをチェック
        /// </summary>
        public bool IsWithinGameArea(Vector2d center, double radiusMeters)
        {
            double distance = CalculateDistance(CurrentPosition, center);
            return distance <= radiusMeters;
        }

        void OnGUI()
        {
            if (!showDebugInfo) return;

            int y = 50;
            GUI.Label(new Rect(10, y, 350, 25), "=== GPS Location Service ===");
            y += 25;

            GUI.Label(new Rect(10, y, 350, 25), $"GPS有効: {IsGPSEnabled}");
            y += 25;

            GUI.Label(new Rect(10, y, 350, 25), $"GPS状態: {GPS_Status}");
            y += 25;

            if (useSimulatedLocation)
            {
                GUI.Label(new Rect(10, y, 350, 25), "[シミュレーションモード]");
                y += 25;
            }

            GUI.Label(new Rect(10, y, 350, 25), $"現在位置: {CurrentPosition}");
            y += 25;

            GUI.Label(new Rect(10, y, 350, 25), $"精度: {CurrentAccuracy:F1}m");
            y += 25;

            y += 10;
            if (GUI.Button(new Rect(10, y, 100, 30), "GPS開始"))
            {
                StartGPS();
            }

            if (GUI.Button(new Rect(120, y, 100, 30), "GPS停止"))
            {
                StopGPS();
            }

            if (GUI.Button(new Rect(230, y, 120, 30), useSimulatedLocation ? "実GPS" : "シミュレート"))
            {
                useSimulatedLocation = !useSimulatedLocation;
                StopGPS();
                StartGPS();
            }
        }

        void OnDestroy()
        {
            StopGPS();
        }
    }
}