using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// InputFieldの挙動を制御するクラス
/// InputFieldに入力された名前を取得し、他のクラスから参照できるようにする
///　入力された文字列は常にこオブジェクトのTextに表示される
/// </summary>
public class InputName : MonoBehaviour
{
    private InputField inputField;
    private string playerName = null;
    public string PlayerName => playerName;
    [SerializeField] private Text placeholderText;
    [SerializeField] private Text inputText;

    void Awake()
    {
        inputField = GetComponent<InputField>();
        inputField.onValueChanged.AddListener(OnInputFieldValueChanged);
        if (inputText != null)
        {
            inputField.textComponent = inputText;
        }
        if (placeholderText != null)
        {
            inputField.placeholder = placeholderText;
        }
    }

    void Start()
    {
        if (placeholderText != null)
        {
            placeholderText.text = "名前を入力してください"; // プレースホルダーテキストの設定
        }
    }

    /// <summary>
    /// placeholderTextに入力された名前をinputTextに表示する
    /// </summary>
    void Update()
    {
        if (inputText != null)
        {
            inputText.text = playerName; // 入力された名前をTextに反映
        }
    }

    public void OnInputFieldValueChanged(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            Debug.Log("InputField is empty");
            playerName = null;
            return;
        }
        // Debug.Log("InputField Value Changed: " + value);
        playerName = value;
    }
}