using UnityEngine;

namespace Onigokko.Heartbeat
{
    /// <summary>
    /// Player ID管理システム - ビルド設定とランタイム設定を統一管理
    /// </summary>
    public class PlayerIdManager : MonoBehaviour
    {
        [Header("ビルド時Player ID設定")]
        [SerializeField] private int buildPlayerId = 1001;
        [SerializeField] private bool overridePlayerPrefs = true;  // PlayerPrefsを上書きするか

        [Header("デバッグ設定")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool allowRuntimeChange = false;  // ランタイムでの変更を許可

        private static PlayerIdManager _instance;
        public static PlayerIdManager Instance => _instance;

        private int currentPlayerId;

        void Awake()
        {
            // シングルトンパターン
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePlayerId();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializePlayerId()
        {
            Debug.Log($"[PlayerIdManager] 初期化開始 - buildPlayerId: {buildPlayerId}, overridePlayerPrefs: {overridePlayerPrefs}");

            // ビルド設定を最優先にする
            if (overridePlayerPrefs || !HasPlayerIdInPrefs())
            {
                currentPlayerId = buildPlayerId;
                PlayerPrefs.SetInt("PlayerID", buildPlayerId);
                PlayerPrefs.Save();

                Debug.Log($"[PlayerIdManager] ビルド設定でPlayer IDを設定: {buildPlayerId} ({GetRoleString(buildPlayerId)})");
            }
            else
            {
                currentPlayerId = PlayerPrefs.GetInt("PlayerID", buildPlayerId);
                Debug.Log($"[PlayerIdManager] PlayerPrefsからPlayer IDを取得: {currentPlayerId} ({GetRoleString(currentPlayerId)})");
            }

            // 強制的に設定を確認・更新
            if (currentPlayerId != buildPlayerId && overridePlayerPrefs)
            {
                Debug.LogWarning($"[PlayerIdManager] Player ID不整合を修正: {currentPlayerId} → {buildPlayerId}");
                currentPlayerId = buildPlayerId;
                PlayerPrefs.SetInt("PlayerID", buildPlayerId);
                PlayerPrefs.Save();
            }

            Debug.Log($"[PlayerIdManager] 最終Player ID: {currentPlayerId} ({GetRoleString(currentPlayerId)})");

            // HeartbeatManagerに通知
            NotifyHeartbeatManager();
        }

        private bool HasPlayerIdInPrefs()
        {
            return PlayerPrefs.HasKey("PlayerID");
        }

        private void NotifyHeartbeatManager()
        {
            // HeartbeatManagerが存在する場合は通知
            var heartbeatManager = HeartbeatManager.Instance;
            if (heartbeatManager != null)
            {
                heartbeatManager.SetPlayerId(currentPlayerId);
            }
            else
            {
                // HeartbeatManagerがまだ存在しない場合は、後で通知するためにInvokeを使用
                Invoke(nameof(DelayedNotification), 0.1f);
            }
        }

        private void DelayedNotification()
        {
            var heartbeatManager = HeartbeatManager.Instance;
            if (heartbeatManager != null)
            {
                heartbeatManager.SetPlayerId(currentPlayerId);
            }
        }

        public int GetPlayerId()
        {
            return currentPlayerId;
        }

        public bool IsKiller()
        {
            return currentPlayerId == 1000;
        }

        public string GetRoleString(int playerId = -1)
        {
            if (playerId == -1) playerId = currentPlayerId;
            return playerId == 1000 ? "キラー" : "サバイバー";
        }

        public void SetPlayerId(int newPlayerId)
        {
            if (!allowRuntimeChange && Application.isPlaying)
            {
                Debug.LogWarning("[PlayerIdManager] ランタイムでのPlayer ID変更は無効化されています");
                return;
            }

            currentPlayerId = newPlayerId;
            PlayerPrefs.SetInt("PlayerID", newPlayerId);
            PlayerPrefs.Save();

            Debug.Log($"[PlayerIdManager] Player IDを変更: {newPlayerId} ({GetRoleString(newPlayerId)})");

            // HeartbeatManagerに通知
            NotifyHeartbeatManager();
        }

        // ビルド前にこのメソッドを呼んでPlayer IDを設定
        [ContextMenu("Set as Killer Build")]
        public void SetAsKillerBuild()
        {
            buildPlayerId = 1000;
            overridePlayerPrefs = true;
            Debug.Log("[PlayerIdManager] キラービルド用に設定しました");

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }

        [ContextMenu("Set as Survivor Build")]
        public void SetAsSurvivorBuild()
        {
            buildPlayerId = 1001;
            overridePlayerPrefs = true;
            Debug.Log("[PlayerIdManager] サバイバービルド用に設定しました");

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }

        void Start()
        {
            // 起動時にPlayer IDを確定
            Debug.Log($"[PlayerIdManager] 起動時Player ID確定: {currentPlayerId} ({GetRoleString()})");

            // 他のシステムにも確実に通知
            Invoke(nameof(ForceNotifyAllSystems), 0.5f);

            if (showDebugInfo)
            {
                StartCoroutine(ShowPlayerIdInfo());
            }
        }

        /// <summary>
        /// すべてのシステムに強制的にPlayer IDを通知
        /// </summary>
        private void ForceNotifyAllSystems()
        {
            Debug.Log($"[PlayerIdManager] 全システムに強制通知: Player ID {currentPlayerId}");

            // HeartbeatSystemに通知
            var heartbeatSystem = HeartbeatSystem.Instance;
            if (heartbeatSystem != null)
            {
                heartbeatSystem.SetPlayerId(currentPlayerId);
            }

            // ProximityDetectorに通知
            var proximityDetector = FindObjectOfType<ProximityDetector>();
            if (proximityDetector != null)
            {
                proximityDetector.SetPlayerId(currentPlayerId);
            }

            // HeartbeatManagerに通知
            var heartbeatManager = HeartbeatManager.Instance;
            if (heartbeatManager != null)
            {
                heartbeatManager.SetPlayerId(currentPlayerId);
            }
        }

        private System.Collections.IEnumerator ShowPlayerIdInfo()
        {
            yield return new WaitForSeconds(1f);

            // 他のシステムの状態も確認
            var heartbeatSystem = HeartbeatSystem.Instance;
            var proximityDetector = FindObjectOfType<ProximityDetector>();

            Debug.Log("=== Player ID 設定状況 ===");
            Debug.Log($"PlayerIdManager: {currentPlayerId} ({GetRoleString()})");

            if (heartbeatSystem != null)
            {
                // HeartbeatSystemの内部状態を確認（リフレクション使用）
                var field = typeof(HeartbeatSystem).GetField("myPlayerId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    var heartbeatId = (int)field.GetValue(heartbeatSystem);
                    Debug.Log($"HeartbeatSystem: {heartbeatId} ({GetRoleString(heartbeatId)})");
                }
            }

            if (proximityDetector != null)
            {
                var isKiller = proximityDetector.IsKiller();
                Debug.Log($"ProximityDetector: IsKiller = {isKiller}");
            }

            Debug.Log("========================");
        }

        void OnGUI()
        {
            if (!showDebugInfo) return;

            int y = Screen.height - 150;

            GUI.Box(new Rect(10, y, 300, 140), "Player ID Manager");

            y += 25;
            GUI.Label(new Rect(20, y, 280, 20), $"Current ID: {currentPlayerId} ({GetRoleString()})");

            y += 25;
            GUI.Label(new Rect(20, y, 280, 20), $"Build ID: {buildPlayerId} ({GetRoleString(buildPlayerId)})");

            y += 25;
            GUI.Label(new Rect(20, y, 280, 20), $"Override PlayerPrefs: {overridePlayerPrefs}");

            y += 30;
            if (allowRuntimeChange)
            {
                if (GUI.Button(new Rect(20, y, 120, 25), "Set Killer"))
                {
                    SetPlayerId(1000);
                }

                if (GUI.Button(new Rect(150, y, 120, 25), "Set Survivor"))
                {
                    SetPlayerId(1001);
                }
            }
            else
            {
                GUI.Label(new Rect(20, y, 280, 20), "Runtime change disabled");
            }
        }
    }
}