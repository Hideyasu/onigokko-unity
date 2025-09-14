using System;
using UnityEngine;
using UnityEngine.UI;
using Onigokko.BLE;

namespace Onigokko.Heartbeat
{
    /// <summary>
    /// 心音システム - キラーとサバイバーの距離に応じた心音エフェクト制御
    /// </summary>
    public class HeartbeatSystem : MonoBehaviour
    {
        [Header("プレイヤー設定")]
        [SerializeField] private int myPlayerId = 1001;
        [SerializeField] private bool isKiller = false;

        [Header("心音距離設定")]
        [SerializeField] private float heartbeatRangeFar = 50f;     // 遠距離心音開始距離
        [SerializeField] private float heartbeatRangeMid = 30f;     // 中距離心音開始距離
        [SerializeField] private float heartbeatRangeNear = 10f;    // 近距離心音開始距離

        [Header("心音エフェクト設定")]
        [SerializeField] private AudioSource heartbeatAudioSource;
        [SerializeField] private AudioClip[] heartbeatClips;        // 距離別の心音クリップ（0:遠, 1:中, 2:近）
        [SerializeField] private Image screenVignetteEffect;        // 画面エフェクト用のイメージ
        [SerializeField] private AnimationCurve volumeCurve = AnimationCurve.EaseInOut(0, 0.2f, 1, 1f);
        [SerializeField] private AnimationCurve vibrationCurve = AnimationCurve.EaseInOut(0, 0f, 1, 1f);

        [Header("エフェクト強度設定")]
        [SerializeField] private float maxVolume = 1f;
        [SerializeField] private float maxVignetteAlpha = 0.6f;
        [SerializeField] private Color vignetteColor = new Color(0f, 0f, 0f, 0.5f);  // 黒色に変更

        [Header("振動設定")]
        [SerializeField] private bool enableVibration = true;
        [SerializeField] private float vibrationInterval = 0.5f;    // 振動間隔（秒）

        private ProximityDetector proximityDetector;
        private float currentDistance = float.MaxValue;
        private HeartbeatLevel currentLevel = HeartbeatLevel.None;
        private float lastVibrationTime = 0f;
        private float heartbeatTimer = 0f;
        private float heartbeatInterval = 1f;

        public enum HeartbeatLevel
        {
            None,
            Far,
            Mid,
            Near
        }

        private static HeartbeatSystem _instance;
        public static HeartbeatSystem Instance => _instance;

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

            // PlayerIdManagerからPlayer IDを取得（最優先）
            var playerIdManager = FindObjectOfType<PlayerIdManager>();
            if (playerIdManager != null)
            {
                myPlayerId = playerIdManager.GetPlayerId();
                Debug.Log($"[HeartbeatSystem] PlayerIdManagerからID取得: {myPlayerId}");
            }
            else
            {
                // フォールバック: PlayerPrefsから取得
                myPlayerId = PlayerPrefs.GetInt("PlayerID", 1001);
                Debug.Log($"[HeartbeatSystem] PlayerPrefsからID取得: {myPlayerId}");
            }

            isKiller = (myPlayerId == 1000);
            Debug.Log($"[HeartbeatSystem] Player ID: {myPlayerId}, IsKiller: {isKiller}");
        }

        void Start()
        {
            InitializeSystem();
        }

        private void InitializeSystem()
        {
            // ProximityDetectorの取得または作成
            proximityDetector = GetComponent<ProximityDetector>();
            if (proximityDetector == null)
            {
                proximityDetector = gameObject.AddComponent<ProximityDetector>();
            }

            // AudioSourceの設定
            if (heartbeatAudioSource == null)
            {
                heartbeatAudioSource = gameObject.AddComponent<AudioSource>();
                heartbeatAudioSource.loop = false;
                heartbeatAudioSource.playOnAwake = false;
                heartbeatAudioSource.volume = maxVolume;
                heartbeatAudioSource.spatialBlend = 0f; // 2D音源
            }

            // 画面エフェクトの初期設定
            if (screenVignetteEffect != null)
            {
                screenVignetteEffect.color = new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, 0);
                screenVignetteEffect.raycastTarget = false;
            }

            // デフォルトの心音クリップを作成（オーディオファイルがない場合）
            if (heartbeatClips == null || heartbeatClips.Length == 0)
            {
                CreateDefaultHeartbeatClips();
            }

            Debug.Log($"[HeartbeatSystem] 初期化完了 - PlayerID: {myPlayerId}, IsKiller: {isKiller}");
            Debug.Log($"[HeartbeatSystem] オーディオクリップ数: {(heartbeatClips?.Length ?? 0)}");

            // キラーでない場合はテスト用の距離を設定
            if (!isKiller)
            {
                // テスト用：5秒後にテスト距離を設定
                Invoke(nameof(StartTestMode), 2f);
            }
        }

        private void CreateDefaultHeartbeatClips()
        {
            // リソースからオーディオクリップを読み込み
            heartbeatClips = new AudioClip[3];
            heartbeatClips[0] = Resources.Load<AudioClip>("Sounds/Heartbeat/heartbeat_slow");
            heartbeatClips[1] = Resources.Load<AudioClip>("Sounds/Heartbeat/heartbeat_medium");
            heartbeatClips[2] = Resources.Load<AudioClip>("Sounds/Heartbeat/heartbeat_fast");

            // オーディオファイルが見つからない場合は、簡単なテスト音を生成
            for (int i = 0; i < heartbeatClips.Length; i++)
            {
                if (heartbeatClips[i] == null)
                {
                    heartbeatClips[i] = CreateTestAudioClip($"Heartbeat_{i}", 0.5f);
                    Debug.LogWarning($"[HeartbeatSystem] オーディオファイルが見つかりません。テスト音を生成: {i}");
                }
            }
        }

        private AudioClip CreateTestAudioClip(string name, float duration)
        {
            int sampleRate = 44100;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);

            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                // 簡単なビープ音を生成
                float time = (float)i / sampleRate;
                data[i] = Mathf.Sin(2f * Mathf.PI * 800f * time) * 0.5f * Mathf.Exp(-time * 3f);
            }

            clip.SetData(data, 0);
            return clip;
        }

        private void StartTestMode()
        {
            if (isKiller) return;

            Debug.Log("[HeartbeatSystem] テストモード開始 - 15m距離でテスト");

            // テスト用の距離を設定
            currentDistance = 15f;
            UpdateHeartbeatLevel();
        }

        void Update()
        {
            // キラーは心音を聞かない
            if (isKiller)
            {
                return;
            }

            // 最寄りのキラーとの距離を取得
            UpdateKillerDistance();

            // 心音レベルの更新
            UpdateHeartbeatLevel();

            // エフェクトの更新
            UpdateEffects();

            // 心音の再生
            UpdateHeartbeatSound();

            // 振動の更新
            UpdateVibration();
        }

        private void UpdateKillerDistance()
        {
            if (proximityDetector != null)
            {
                float newDistance = proximityDetector.GetDistanceToKiller();

                if (newDistance != currentDistance)
                {
                    currentDistance = newDistance;
                    Debug.Log($"[HeartbeatSystem] 距離更新: {currentDistance:F1}m");
                }
            }
            else
            {
                Debug.LogWarning("[HeartbeatSystem] ProximityDetector が null です");
            }
        }

        private void UpdateHeartbeatLevel()
        {
            HeartbeatLevel newLevel = HeartbeatLevel.None;

            if (currentDistance <= 0.5f)
            {
                // 極近距離: 0.5m以内は最大強度
                newLevel = HeartbeatLevel.Near;
            }
            else if (currentDistance <= heartbeatRangeNear)
            {
                newLevel = HeartbeatLevel.Near;
            }
            else if (currentDistance <= heartbeatRangeMid)
            {
                newLevel = HeartbeatLevel.Mid;
            }
            else if (currentDistance <= heartbeatRangeFar)
            {
                newLevel = HeartbeatLevel.Far;
            }

            if (newLevel != currentLevel)
            {
                currentLevel = newLevel;
                OnHeartbeatLevelChanged();
            }
        }

        private void OnHeartbeatLevelChanged()
        {
            Debug.Log($"[HeartbeatSystem] 心音レベル変更: {currentLevel} (距離: {currentDistance:F1}m)");

            // レベルに応じた心音間隔の設定
            switch (currentLevel)
            {
                case HeartbeatLevel.Near:
                    // 極近距離での特別な設定
                    if (currentDistance <= 0.5f)
                    {
                        heartbeatInterval = 0.2f; // 極速心音
                        Debug.Log($"[HeartbeatSystem] 極近距離モード: {currentDistance:F2}m - 超高速心音");
                    }
                    else
                    {
                        heartbeatInterval = 0.4f; // 高速心音
                    }
                    break;
                case HeartbeatLevel.Mid:
                    heartbeatInterval = 0.8f;
                    break;
                case HeartbeatLevel.Far:
                    heartbeatInterval = 1.2f;
                    break;
                default:
                    heartbeatInterval = 0f;
                    break;
            }
        }

        private void UpdateEffects()
        {
            if (screenVignetteEffect == null) return;

            // 距離に応じたエフェクト強度の計算
            float effectIntensity = 0f;

            if (currentLevel != HeartbeatLevel.None)
            {
                float normalizedDistance = 0f;

                switch (currentLevel)
                {
                    case HeartbeatLevel.Near:
                        // 極近距離では最大エフェクト
                        if (currentDistance <= 0.5f)
                        {
                            normalizedDistance = 1f; // 最大強度
                        }
                        else
                        {
                            normalizedDistance = 1f - (currentDistance / heartbeatRangeNear);
                        }
                        break;
                    case HeartbeatLevel.Mid:
                        normalizedDistance = 1f - ((currentDistance - heartbeatRangeNear) / (heartbeatRangeMid - heartbeatRangeNear));
                        break;
                    case HeartbeatLevel.Far:
                        normalizedDistance = 1f - ((currentDistance - heartbeatRangeMid) / (heartbeatRangeFar - heartbeatRangeMid));
                        break;
                }

                effectIntensity = Mathf.Clamp01(normalizedDistance);

                // 極近距離では追加のエフェクト強度
                if (currentDistance <= 0.5f)
                {
                    effectIntensity = Mathf.Max(effectIntensity, 0.9f);
                }
            }

            // ビネットエフェクトの更新
            float targetAlpha = effectIntensity * maxVignetteAlpha;

            // 極近距離では赤みを強調
            if (currentDistance <= 0.5f)
            {
                targetAlpha = Mathf.Max(targetAlpha, 0.8f);
            }

            Color currentColor = screenVignetteEffect.color;
            currentColor.a = Mathf.Lerp(currentColor.a, targetAlpha, Time.deltaTime * 5f);
            screenVignetteEffect.color = currentColor;
        }

        private void UpdateHeartbeatSound()
        {
            if (currentLevel == HeartbeatLevel.None || heartbeatAudioSource == null)
            {
                return;
            }

            heartbeatTimer += Time.deltaTime;

            if (heartbeatTimer >= heartbeatInterval)
            {
                heartbeatTimer = 0f;
                PlayHeartbeatSound();
            }
        }

        private void PlayHeartbeatSound()
        {
            if (heartbeatClips == null || heartbeatClips.Length == 0)
            {
                return;
            }

            // レベルに応じた音声クリップの選択
            int clipIndex = Mathf.Min((int)currentLevel - 1, heartbeatClips.Length - 1);
            if (clipIndex >= 0 && heartbeatClips[clipIndex] != null)
            {
                // 距離に応じた音量の計算
                float normalizedDistance = Mathf.InverseLerp(heartbeatRangeFar, 0, currentDistance);
                float volume = volumeCurve.Evaluate(normalizedDistance) * maxVolume;

                heartbeatAudioSource.clip = heartbeatClips[clipIndex];
                heartbeatAudioSource.volume = volume;
                heartbeatAudioSource.Play();
            }
        }

        private void UpdateVibration()
        {
            if (!enableVibration || currentLevel == HeartbeatLevel.None)
            {
                return;
            }

            if (Time.time - lastVibrationTime >= vibrationInterval)
            {
                lastVibrationTime = Time.time;

                // 距離に応じた振動の強さ
                float normalizedDistance = Mathf.InverseLerp(heartbeatRangeFar, 0, currentDistance);
                float vibrationStrength = vibrationCurve.Evaluate(normalizedDistance);

                TriggerVibration(vibrationStrength);
            }
        }

        private void TriggerVibration(float strength)
        {
            #if UNITY_IOS || UNITY_ANDROID
            if (strength > 0.7f)
            {
                Handheld.Vibrate(); // 強い振動
            }
            else if (strength > 0.3f)
            {
                // 中程度の振動（プラットフォーム固有の実装が必要）
                #if UNITY_IOS
                // iOS Haptic Feedback API を使用
                #elif UNITY_ANDROID
                // Android Vibration API を使用
                #endif
            }
            // 弱い振動は省略（バッテリー節約）
            #endif
        }

        public void SetPlayerId(int id)
        {
            myPlayerId = id;
            isKiller = (id == 1000);
            PlayerPrefs.SetInt("PlayerID", id);
            PlayerPrefs.Save();

            Debug.Log($"[HeartbeatSystem] PlayerID設定: {myPlayerId}, IsKiller: {isKiller}");
        }

        public HeartbeatLevel GetCurrentHeartbeatLevel()
        {
            return currentLevel;
        }

        public float GetCurrentDistance()
        {
            return currentDistance;
        }

        void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        #if UNITY_EDITOR
        void OnGUI()
        {
            if (isKiller) return;

            int y = 400;
            GUI.Label(new Rect(10, y, 400, 25), "=== 心音システム ===");
            y += 25;
            GUI.Label(new Rect(10, y, 400, 25), $"キラーとの距離: {currentDistance:F1}m");
            y += 25;
            GUI.Label(new Rect(10, y, 400, 25), $"心音レベル: {currentLevel}");
            y += 25;
            GUI.Label(new Rect(10, y, 400, 25), $"心音間隔: {heartbeatInterval:F1}秒");
        }
        #endif
    }
}