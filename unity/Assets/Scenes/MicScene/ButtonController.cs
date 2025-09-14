using UnityEngine;
using UnityEngine.UI;

public class ButtonController : MonoBehaviour
{
    private TBSKDemo tbskDemo;
    private UnifiedTBSKReceiver receiver;
    private InputField messageInput;
    private bool regenerateBeforePlay = false;
    
    [SerializeField] private Text outputText;

    void Start()
    {
        receiver = FindObjectOfType<UnifiedTBSKReceiver>();
        if (receiver == null)
        {
            var go = new GameObject("TBSKReceiver");
            receiver = go.AddComponent<UnifiedTBSKReceiver>();
            receiver.DisableAutoStart();
            receiver.SetEnableKeyToggle(false);
            receiver.SetStopOnPause(false);
            receiver.SetStopOnFocusLoss(false);
        }
        else
        {
            receiver.DisableAutoStart();
            receiver.SetEnableKeyToggle(false);
            receiver.SetStopOnPause(false);
            receiver.SetStopOnFocusLoss(false);
            if (receiver.IsRecording)
            {
                receiver.StopRecording();
            }
        }

        if (receiver != null)
        {
            receiver.MessageDecoded += OnMessageDecoded;
        }
    }

    public void OnPlayPressed()
    {
        Debug.Log("Button Clicked");
        
        // TBSKDemoを再検索
        if (tbskDemo == null)
        {
            tbskDemo = FindObjectOfType<TBSKDemo>();
        }
        
        // 見つからない場合は新しく作成
        if (tbskDemo == null)
        {
            Debug.Log("TBSKDemo not found. Creating new TBSKDemo object.");
            GameObject tbskObj = new GameObject("TBSKDemoObject");
            tbskDemo = tbskObj.AddComponent<TBSKDemo>();
        }
        
        if (tbskDemo == null)
        {
            Debug.LogError("Failed to create or find TBSKDemo!");
            return;
        }

        // 入力ボックスがあれば、そのテキストを送信メッセージに反映
        if (messageInput != null)
        {
            var msg = messageInput.text ?? string.Empty;
            tbskDemo.SetMessageAndGenerate(msg);
        }
        else if (regenerateBeforePlay)
        {
            // 明示的な入力が無い場合でも最新メッセージで再生成（任意）
            tbskDemo.SetMessageAndGenerate(tbskDemo.GetCurrentMessage());
        }

        tbskDemo.PlayAudio();
    }

    public void OnRecordPressed()
    {
        if (receiver == null) return;

        if (receiver.IsRecording)
        {
            receiver.StopRecording();
            Debug.Log("Recording stopped");
        }
        else
        {
            receiver.StartRecording();
            Debug.Log("Recording started");
        }
    }

    private void OnMessageDecoded(string msg)
    {
        if (outputText != null)
        {
            if (!string.IsNullOrEmpty(outputText.text))
            {
                outputText.text += "\n";
            }
            outputText.text += msg;
        }
        else
        {
            Debug.Log($"[ButtonController] Decoded: {msg}");
        }
    }

    void OnDestroy()
    {
        if (receiver != null)
        {
            receiver.MessageDecoded -= OnMessageDecoded;
        }
    }
}
