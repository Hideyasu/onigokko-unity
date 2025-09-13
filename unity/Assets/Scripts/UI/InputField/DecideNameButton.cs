using UnityEngine;
using UnityEngine.UI;

public class DecideNameButton : MonoBehaviour
{
    [SerializeField] private Text nameText;

    public void OnClick()
    {
        SoundManager.Instance.PlaySE();
        string playerName = nameText.text;
        Debug.Log("Player Name: " + playerName);
    }
}