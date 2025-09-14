using UnityEngine;
using System.Collections.Generic;

namespace Onigokko.Heartbeat
{
    /// <summary>
    /// 心音オーディオ管理システム
    /// </summary>
    public class HeartbeatAudioManager : MonoBehaviour
    {
        [Header("オーディオソース")]
        [SerializeField] private AudioSource heartbeatSource;
        [SerializeField] private AudioSource ambientSource;
        [SerializeField] private AudioSource alertSource;

        [Header("心音クリップ")]
        [SerializeField] private AudioClip heartbeatSlow;      // 遠距離用（ゆっくり）
        [SerializeField] private AudioClip heartbeatMedium;    // 中距離用（普通）
        [SerializeField] private AudioClip heartbeatFast;      // 近距離用（速い）
        [SerializeField] private AudioClip heartbeatCritical;  // 極近距離用（非常に速い）

        [Header("環境音")]
        [SerializeField] private AudioClip ambientTension;     // 緊張感のある環境音
        [SerializeField] private AudioClip alertSound;         // 警告音

        [Header("音量設定")]
        [SerializeField] private float masterVolume = 1f;
        [SerializeField] private float heartbeatMaxVolume = 0.8f;
        [SerializeField] private float ambientMaxVolume = 0.3f;
        [SerializeField] private AnimationCurve volumeFalloffCurve;

        private HeartbeatSystem heartbeatSystem;
        private HeartbeatSystem.HeartbeatLevel currentLevel = HeartbeatSystem.HeartbeatLevel.None;
        private float targetHeartbeatVolume = 0f;
        private float targetAmbientVolume = 0f;
        private float heartbeatInterval = 1f;
        private float lastHeartbeatTime = 0f;

        void Start()
        {
            InitializeAudio();
        }

        private void InitializeAudio()
        {
            heartbeatSystem = HeartbeatSystem.Instance;

            // オーディオソースの設定
            if (heartbeatSource == null)
            {
                heartbeatSource = gameObject.AddComponent<AudioSource>();
            }
            ConfigureAudioSource(heartbeatSource, false, 1f);

            if (ambientSource == null)
            {
                ambientSource = gameObject.AddComponent<AudioSource>();
            }
            ConfigureAudioSource(ambientSource, true, 0.3f);

            if (alertSource == null)
            {
                alertSource = gameObject.AddComponent<AudioSource>();
            }
            ConfigureAudioSource(alertSource, false, 0.5f);

            // デフォルトのボリュームカーブ設定
            if (volumeFalloffCurve == null || volumeFalloffCurve.length == 0)
            {
                volumeFalloffCurve = AnimationCurve.EaseInOut(0, 0.1f, 1, 1f);
            }

            Debug.Log("[HeartbeatAudioManager] オーディオシステム初期化完了");
        }

        private void ConfigureAudioSource(AudioSource source, bool loop, float volume)
        {
            source.playOnAwake = false;
            source.loop = loop;
            source.volume = volume * masterVolume;
            source.spatialBlend = 0f; // 2D音源
            source.priority = 128;
        }

        void Update()
        {
            if (heartbeatSystem == null) return;

            UpdateHeartbeatAudio();
            UpdateAmbientAudio();
        }

        private void UpdateHeartbeatAudio()
        {
            var newLevel = heartbeatSystem.GetCurrentHeartbeatLevel();
            float distance = heartbeatSystem.GetCurrentDistance();

            // レベルが変わった場合
            if (newLevel != currentLevel)
            {
                currentLevel = newLevel;
                OnHeartbeatLevelChanged(newLevel);
            }

            // 心音の再生タイミング
            if (currentLevel != HeartbeatSystem.HeartbeatLevel.None)
            {
                if (Time.time - lastHeartbeatTime >= heartbeatInterval)
                {
                    PlayHeartbeat();
                    lastHeartbeatTime = Time.time;
                }

                // 音量の更新
                float normalizedDistance = GetNormalizedDistance(distance);
                targetHeartbeatVolume = volumeFalloffCurve.Evaluate(1f - normalizedDistance) * heartbeatMaxVolume;
            }
            else
            {
                targetHeartbeatVolume = 0f;
            }

            // スムーズな音量変更
            if (heartbeatSource != null)
            {
                heartbeatSource.volume = Mathf.Lerp(heartbeatSource.volume, targetHeartbeatVolume * masterVolume, Time.deltaTime * 3f);
            }
        }

        private void UpdateAmbientAudio()
        {
            if (ambientSource == null) return;

            // 距離に応じた環境音の調整
            if (currentLevel != HeartbeatSystem.HeartbeatLevel.None)
            {
                targetAmbientVolume = ambientMaxVolume;

                if (!ambientSource.isPlaying && ambientTension != null)
                {
                    ambientSource.clip = ambientTension;
                    ambientSource.Play();
                }
            }
            else
            {
                targetAmbientVolume = 0f;
            }

            ambientSource.volume = Mathf.Lerp(ambientSource.volume, targetAmbientVolume * masterVolume, Time.deltaTime * 2f);

            // 音量が0になったら停止
            if (ambientSource.volume < 0.01f && ambientSource.isPlaying)
            {
                ambientSource.Stop();
            }
        }

        private void OnHeartbeatLevelChanged(HeartbeatSystem.HeartbeatLevel level)
        {
            // レベルに応じた心音間隔の設定
            switch (level)
            {
                case HeartbeatSystem.HeartbeatLevel.Near:
                    heartbeatInterval = 0.4f;
                    break;
                case HeartbeatSystem.HeartbeatLevel.Mid:
                    heartbeatInterval = 0.7f;
                    break;
                case HeartbeatSystem.HeartbeatLevel.Far:
                    heartbeatInterval = 1.2f;
                    break;
                default:
                    heartbeatInterval = 0f;
                    break;
            }

            Debug.Log($"[HeartbeatAudioManager] 心音レベル変更: {level}, 間隔: {heartbeatInterval}秒");
        }

        private void PlayHeartbeat()
        {
            if (heartbeatSource == null) return;

            AudioClip clipToPlay = null;
            float distance = heartbeatSystem.GetCurrentDistance();

            // 距離に応じたクリップの選択
            if (distance <= 5f && heartbeatCritical != null)
            {
                clipToPlay = heartbeatCritical;
            }
            else if (distance <= 10f && heartbeatFast != null)
            {
                clipToPlay = heartbeatFast;
            }
            else if (distance <= 30f && heartbeatMedium != null)
            {
                clipToPlay = heartbeatMedium;
            }
            else if (distance <= 50f && heartbeatSlow != null)
            {
                clipToPlay = heartbeatSlow;
            }

            if (clipToPlay != null)
            {
                heartbeatSource.PlayOneShot(clipToPlay);
            }
        }

        public void PlayAlertSound()
        {
            if (alertSource != null && alertSound != null)
            {
                alertSource.PlayOneShot(alertSound);
            }
        }

        private float GetNormalizedDistance(float distance)
        {
            if (distance <= 10f)
            {
                return distance / 10f;
            }
            else if (distance <= 30f)
            {
                return 0.3f + (distance - 10f) / 20f * 0.4f;
            }
            else if (distance <= 50f)
            {
                return 0.7f + (distance - 30f) / 20f * 0.3f;
            }
            else
            {
                return 1f;
            }
        }

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
        }

        private void UpdateAllVolumes()
        {
            if (heartbeatSource != null)
            {
                heartbeatSource.volume = heartbeatSource.volume * masterVolume;
            }
            if (ambientSource != null)
            {
                ambientSource.volume = ambientSource.volume * masterVolume;
            }
            if (alertSource != null)
            {
                alertSource.volume = alertSource.volume * masterVolume;
            }
        }

        public void StopAllAudio()
        {
            if (heartbeatSource != null) heartbeatSource.Stop();
            if (ambientSource != null) ambientSource.Stop();
            if (alertSource != null) alertSource.Stop();
        }

        void OnDestroy()
        {
            StopAllAudio();
        }
    }
}