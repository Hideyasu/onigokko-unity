using System;
using System.Collections.Generic;
using UnityEngine;

namespace Onigokko.BLE
{
    /// <summary>
    /// BLE Beacon距離精度検証システム
    /// 実際の距離とBLE距離の比較・記録
    /// </summary>
    public class BLEDistanceValidator : MonoBehaviour
    {
        [Header("検証設定")]
        [SerializeField] private bool enableValidation = true;
        [SerializeField] private bool autoLogResults = true;
        [SerializeField] private float validationInterval = 2f;

        [Header("精度基準")]
        [SerializeField] private float nearRangeThreshold = 2f;        // 近距離: 2m以内
        [SerializeField] private float midRangeThreshold = 10f;        // 中距離: 10m以内
        [SerializeField] private float nearRangeAccuracy = 0.5f;       // 近距離精度: ±0.5m
        [SerializeField] private float midRangeAccuracy = 2f;          // 中距離精度: ±2m
        [SerializeField] private float farRangeAccuracy = 5f;          // 遠距離精度: ±5m

        // 検証データ
        private List<ValidationRecord> validationRecords = new List<ValidationRecord>();
        private iOSBLEBeacon bleBeacon;
        private float lastValidationTime;

        [System.Serializable]
        public class ValidationRecord
        {
            public DateTime timestamp;
            public float actualDistance;      // 実際の距離（手動入力）
            public float bleDistance;         // BLE測定距離
            public float rssi;               // RSSI値
            public float errorDistance;       // 誤差
            public float errorPercentage;     // 誤差率
            public AccuracyLevel accuracy;    // 精度レベル

            public override string ToString()
            {
                return $"実測:{actualDistance:F1}m BLE:{bleDistance:F1}m 誤差:{errorDistance:F1}m({errorPercentage:F0}%) RSSI:{rssi:F0} [{accuracy}]";
            }
        }

        public enum AccuracyLevel
        {
            Excellent,  // 基準内
            Good,       // 基準の1.5倍以内
            Poor,       // 基準の2倍以内
            Failed      // 基準の2倍超過
        }

        void Start()
        {
            bleBeacon = iOSBLEBeacon.Instance;
            if (bleBeacon == null)
            {
                Debug.LogWarning("[Validator] iOSBLEBeacon が見つかりません（同じGameObjectに追加してください）");
                // エラーで無効化せず、後で再試行可能にする
                return;
            }

            Debug.Log("[Validator] BLE距離精度検証システム開始");

            if (enableValidation)
            {
                InvokeRepeating(nameof(ValidateDistance), validationInterval, validationInterval);
            }
        }

        /// <summary>
        /// 手動で距離検証を実行
        /// </summary>
        public void ValidateDistanceManual(float actualDistance)
        {
            if (bleBeacon == null) return;

            var nearbyPlayers = bleBeacon.GetNearbyPlayers();
            if (nearbyPlayers.Count == 0)
            {
                Debug.LogWarning("[Validator] 検出されたビーコンがありません");
                return;
            }

            var closestPlayer = nearbyPlayers[0];
            RecordValidation(actualDistance, closestPlayer.distance, closestPlayer.rssi);
        }

        /// <summary>
        /// 自動距離検証（開発時のみ）
        /// </summary>
        private void ValidateDistance()
        {
            if (!enableValidation || bleBeacon == null) return;

            var nearbyPlayers = bleBeacon.GetNearbyPlayers();
            if (nearbyPlayers.Count == 0) return;

            var closestPlayer = nearbyPlayers[0];

            // 開発時は実際の距離をランダムで生成（実際の運用では削除）
            float mockActualDistance = GenerateMockActualDistance(closestPlayer.distance);
            RecordValidation(mockActualDistance, closestPlayer.distance, closestPlayer.rssi);
        }

        /// <summary>
        /// 検証結果を記録
        /// </summary>
        private void RecordValidation(float actualDistance, float bleDistance, float rssi)
        {
            var record = new ValidationRecord
            {
                timestamp = DateTime.Now,
                actualDistance = actualDistance,
                bleDistance = bleDistance,
                rssi = rssi,
                errorDistance = Mathf.Abs(bleDistance - actualDistance),
                errorPercentage = actualDistance > 0 ? (Mathf.Abs(bleDistance - actualDistance) / actualDistance) * 100 : 0,
                accuracy = CalculateAccuracyLevel(actualDistance, bleDistance)
            };

            validationRecords.Add(record);

            if (autoLogResults)
            {
                Debug.Log($"[Validator] {record}");
            }

            // 記録が多くなりすぎた場合は古いものを削除
            if (validationRecords.Count > 100)
            {
                validationRecords.RemoveAt(0);
            }
        }

        /// <summary>
        /// 精度レベルを計算
        /// </summary>
        private AccuracyLevel CalculateAccuracyLevel(float actualDistance, float bleDistance)
        {
            float errorDistance = Mathf.Abs(bleDistance - actualDistance);
            float threshold;

            // 距離に応じた精度基準を選択
            if (actualDistance <= nearRangeThreshold)
            {
                threshold = nearRangeAccuracy;
            }
            else if (actualDistance <= midRangeThreshold)
            {
                threshold = midRangeAccuracy;
            }
            else
            {
                threshold = farRangeAccuracy;
            }

            if (errorDistance <= threshold)
                return AccuracyLevel.Excellent;
            else if (errorDistance <= threshold * 1.5f)
                return AccuracyLevel.Good;
            else if (errorDistance <= threshold * 2f)
                return AccuracyLevel.Poor;
            else
                return AccuracyLevel.Failed;
        }

        /// <summary>
        /// 開発用のモック実際距離生成（本番では削除）
        /// </summary>
        private float GenerateMockActualDistance(float bleDistance)
        {
            // BLE距離を基準に±20%のランダム誤差を付与
            float variation = bleDistance * 0.2f;
            return bleDistance + UnityEngine.Random.Range(-variation, variation);
        }

        /// <summary>
        /// 統計情報を取得
        /// </summary>
        public ValidationStats GetValidationStats()
        {
            if (validationRecords.Count == 0)
                return new ValidationStats();

            var stats = new ValidationStats();
            stats.totalRecords = validationRecords.Count;

            float totalError = 0f;
            float totalErrorPercentage = 0f;

            foreach (var record in validationRecords)
            {
                totalError += record.errorDistance;
                totalErrorPercentage += record.errorPercentage;

                switch (record.accuracy)
                {
                    case AccuracyLevel.Excellent: stats.excellentCount++; break;
                    case AccuracyLevel.Good: stats.goodCount++; break;
                    case AccuracyLevel.Poor: stats.poorCount++; break;
                    case AccuracyLevel.Failed: stats.failedCount++; break;
                }
            }

            stats.averageError = totalError / validationRecords.Count;
            stats.averageErrorPercentage = totalErrorPercentage / validationRecords.Count;
            stats.successRate = (float)(stats.excellentCount + stats.goodCount) / validationRecords.Count * 100f;

            return stats;
        }

        [System.Serializable]
        public class ValidationStats
        {
            public int totalRecords;
            public float averageError;
            public float averageErrorPercentage;
            public float successRate;
            public int excellentCount;
            public int goodCount;
            public int poorCount;
            public int failedCount;

            public override string ToString()
            {
                return $"検証回数:{totalRecords} 平均誤差:{averageError:F1}m({averageErrorPercentage:F1}%) 成功率:{successRate:F1}% [優:{excellentCount} 良:{goodCount} 可:{poorCount} 失:{failedCount}]";
            }
        }

        /// <summary>
        /// 検証結果をクリア
        /// </summary>
        public void ClearValidationRecords()
        {
            validationRecords.Clear();
            Debug.Log("[Validator] 検証記録をクリアしました");
        }

        /// <summary>
        /// 検証結果をCSVで出力
        /// </summary>
        public void ExportValidationRecords()
        {
            if (validationRecords.Count == 0)
            {
                Debug.LogWarning("[Validator] 出力する検証記録がありません");
                return;
            }

            var csv = "Timestamp,ActualDistance,BLEDistance,RSSI,ErrorDistance,ErrorPercentage,Accuracy\n";

            foreach (var record in validationRecords)
            {
                csv += $"{record.timestamp:yyyy-MM-dd HH:mm:ss},{record.actualDistance:F2},{record.bleDistance:F2},{record.rssi:F0},{record.errorDistance:F2},{record.errorPercentage:F1},{record.accuracy}\n";
            }

            string fileName = $"BLE_Validation_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);

            try
            {
                System.IO.File.WriteAllText(filePath, csv);
                Debug.Log($"[Validator] 検証結果をエクスポートしました: {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Validator] エクスポートエラー: {e.Message}");
            }
        }

        void OnGUI()
        {
            if (!enableValidation) return;

            int y = 300;
            var stats = GetValidationStats();

            GUI.Label(new Rect(10, y, 400, 25), "=== 距離精度検証 ===");
            y += 25;

            if (stats.totalRecords > 0)
            {
                GUI.Label(new Rect(10, y, 400, 25), stats.ToString());
                y += 25;
            }
            else
            {
                GUI.Label(new Rect(10, y, 400, 25), "検証データなし");
                y += 25;
            }

            y += 10;
            if (GUI.Button(new Rect(10, y, 100, 30), "手動検証"))
            {
                // テスト用: 5mとして検証
                ValidateDistanceManual(5f);
            }

            if (GUI.Button(new Rect(120, y, 100, 30), "記録クリア"))
            {
                ClearValidationRecords();
            }

            if (GUI.Button(new Rect(230, y, 100, 30), "CSV出力"))
            {
                ExportValidationRecords();
            }
        }
    }
}