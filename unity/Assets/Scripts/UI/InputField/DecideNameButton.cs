using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Database;

public class DecideNameButton : MonoBehaviour
{
    [SerializeField] private Text nameText;

    public async void OnClick()
    {
        string playerName = nameText.text;
        if (string.IsNullOrEmpty(playerName)) return;

        Debug.Log($"DecideNameButton clicked with name: {playerName}");

        var reference = FirebaseDatabase.DefaultInstance.RootReference;
        await reference.Child("users").Child("player").Child("name").SetValueAsync(playerName);
        Debug.Log($"Player name saved: {playerName}");
    }
}