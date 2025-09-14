using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Database;

public class DecideNameButton : MonoBehaviour
{
    [SerializeField] private Text nameText;

    public async void OnClick()
    {
        var status = await FirebaseApp.CheckAndFixDependenciesAsync();
        SoundManager.Instance.PlaySE();
        string playerName = nameText.text;
        if (string.IsNullOrEmpty(playerName)) return;

        var reference = FirebaseDatabase.DefaultInstance.RootReference;
        var newPlayerRef = reference.Child("users").Push();
        
        await newPlayerRef.Child("name").SetValueAsync(playerName);
        
        string playerId = newPlayerRef.Key;
        PlayerPrefs.SetString("PlayerID", playerId);
        PlayerPrefs.Save();
        
        Debug.Log($"Player created - ID: {playerId}, Name: {playerName}");
    }
}