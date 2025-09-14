using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Onigokko.Heartbeat
{
    /// <summary>
    /// 心音UIコントローラー - 画面エフェクトとビジュアルフィードバックの管理
    /// </summary>
    public class HeartbeatUIController : MonoBehaviour
    {
        [Header("画面エフェクト")]
        [SerializeField] private Image vignetteImage;              // ビネット効果用イメージ
        [SerializeField] private Image pulseEffectImage;           // パルスエフェクト用イメージ
        [SerializeField] private CanvasGroup dangerOverlay;        // 危険時のオーバーレイ

        [Header("エフェクト設定")]
        [SerializeField] private Color farColor = new Color(0.3f, 0f, 0f, 0.2f);
        [SerializeField] private Color midColor = new Color(0.5f, 0f, 0f, 0.4f);
        [SerializeField] private Color nearColor = new Color(0.7f, 0f, 0f, 0.6f);
        [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("アニメーション設定")]
        [SerializeField] private float pulseSpeed = 1f;
        [SerializeField] private float fadeSpeed = 2f;
        [SerializeField] private bool enableScreenShake = true;
        [SerializeField] private float shakeIntensity = 0.05f;

        private HeartbeatSystem heartbeatSystem;
        private Camera mainCamera;
        private Vector3 originalCameraPosition;
        private Coroutine currentPulseCoroutine;
        private Coroutine currentShakeCoroutine;

        void Start()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            // HeartbeatSystemの取得
            heartbeatSystem = HeartbeatSystem.Instance;
            if (heartbeatSystem == null)
            {
                Debug.LogError("[HeartbeatUIController] HeartbeatSystemが見つかりません");
                return;
            }

            // メインカメラの取得
            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                originalCameraPosition = mainCamera.transform.localPosition;
            }

            // UIコンポーネントの初期化
            if (vignetteImage != null)
            {
                vignetteImage.color = new Color(0, 0, 0, 0);
                vignetteImage.raycastTarget = false;
            }

            if (pulseEffectImage != null)
            {
                pulseEffectImage.color = new Color(1, 1, 1, 0);
                pulseEffectImage.raycastTarget = false;
            }

            if (dangerOverlay != null)
            {
                dangerOverlay.alpha = 0;
                dangerOverlay.interactable = false;
                dangerOverlay.blocksRaycasts = false;
            }

            Debug.Log("[HeartbeatUIController] UI初期化完了");
        }

        void Update()
        {
            if (heartbeatSystem == null) return;

            UpdateVignetteEffect();
            UpdateDangerOverlay();
        }

        private void UpdateVignetteEffect()
        {
            if (vignetteImage == null) return;

            var heartbeatLevel = heartbeatSystem.GetCurrentHeartbeatLevel();
            Color targetColor = new Color(0, 0, 0, 0);

            switch (heartbeatLevel)
            {
                case HeartbeatSystem.HeartbeatLevel.Far:
                    targetColor = farColor;
                    StartPulseEffect(2f);
                    break;
                case HeartbeatSystem.HeartbeatLevel.Mid:
                    targetColor = midColor;
                    StartPulseEffect(1.5f);
                    break;
                case HeartbeatSystem.HeartbeatLevel.Near:
                    targetColor = nearColor;
                    StartPulseEffect(1f);
                    StartScreenShake();
                    break;
                default:
                    StopPulseEffect();
                    StopScreenShake();
                    break;
            }

            // スムーズな色の遷移
            vignetteImage.color = Color.Lerp(vignetteImage.color, targetColor, Time.deltaTime * fadeSpeed);
        }

        private void UpdateDangerOverlay()
        {
            if (dangerOverlay == null) return;

            var heartbeatLevel = heartbeatSystem.GetCurrentHeartbeatLevel();
            float targetAlpha = 0f;

            if (heartbeatLevel == HeartbeatSystem.HeartbeatLevel.Near)
            {
                float distance = heartbeatSystem.GetCurrentDistance();
                // 10m以下で徐々に強くなる
                targetAlpha = Mathf.InverseLerp(10f, 2f, distance) * 0.3f;
            }

            dangerOverlay.alpha = Mathf.Lerp(dangerOverlay.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
        }

        private void StartPulseEffect(float interval)
        {
            if (currentPulseCoroutine != null)
            {
                StopCoroutine(currentPulseCoroutine);
            }
            currentPulseCoroutine = StartCoroutine(PulseEffectCoroutine(interval));
        }

        private void StopPulseEffect()
        {
            if (currentPulseCoroutine != null)
            {
                StopCoroutine(currentPulseCoroutine);
                currentPulseCoroutine = null;
            }

            if (pulseEffectImage != null)
            {
                pulseEffectImage.color = new Color(1, 1, 1, 0);
            }
        }

        private IEnumerator PulseEffectCoroutine(float interval)
        {
            if (pulseEffectImage == null) yield break;

            while (true)
            {
                // パルスアニメーション
                float timer = 0f;
                while (timer < interval)
                {
                    timer += Time.deltaTime * pulseSpeed;
                    float normalizedTime = timer / interval;
                    float alpha = pulseCurve.Evaluate(normalizedTime) * 0.3f;

                    Color color = pulseEffectImage.color;
                    color.a = alpha;
                    pulseEffectImage.color = color;

                    yield return null;
                }

                // リセット
                Color resetColor = pulseEffectImage.color;
                resetColor.a = 0;
                pulseEffectImage.color = resetColor;

                yield return new WaitForSeconds(0.1f);
            }
        }

        private void StartScreenShake()
        {
            if (!enableScreenShake || mainCamera == null) return;

            if (currentShakeCoroutine != null)
            {
                StopCoroutine(currentShakeCoroutine);
            }
            currentShakeCoroutine = StartCoroutine(ScreenShakeCoroutine());
        }

        private void StopScreenShake()
        {
            if (currentShakeCoroutine != null)
            {
                StopCoroutine(currentShakeCoroutine);
                currentShakeCoroutine = null;
            }

            if (mainCamera != null)
            {
                mainCamera.transform.localPosition = originalCameraPosition;
            }
        }

        private IEnumerator ScreenShakeCoroutine()
        {
            while (true)
            {
                float x = Random.Range(-shakeIntensity, shakeIntensity);
                float y = Random.Range(-shakeIntensity, shakeIntensity);

                mainCamera.transform.localPosition = originalCameraPosition + new Vector3(x, y, 0);

                yield return new WaitForSeconds(0.05f);
            }
        }

        public void TriggerDamageEffect()
        {
            StartCoroutine(DamageEffectCoroutine());
        }

        private IEnumerator DamageEffectCoroutine()
        {
            if (vignetteImage == null) yield break;

            // 赤い画面フラッシュ
            Color originalColor = vignetteImage.color;
            vignetteImage.color = new Color(1f, 0f, 0f, 0.8f);

            yield return new WaitForSeconds(0.1f);

            // 元に戻す
            float timer = 0f;
            while (timer < 0.5f)
            {
                timer += Time.deltaTime;
                vignetteImage.color = Color.Lerp(new Color(1f, 0f, 0f, 0.8f), originalColor, timer / 0.5f);
                yield return null;
            }

            vignetteImage.color = originalColor;
        }

        void OnDestroy()
        {
            StopPulseEffect();
            StopScreenShake();
        }
    }
}