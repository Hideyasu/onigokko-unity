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

        string playerId = GetOrCreatePlayerId();
        Debug.Log($"Player ID: {playerId}, Name: {playerName}");

        var reference = FirebaseDatabase.DefaultInstance.RootReference;
        await reference.Child("users").Child(playerId).Child("name").SetValueAsync(playerName);
        Debug.Log($"Player data saved - ID: {playerId}, Name: {playerName}");
    }

    private string GetOrCreatePlayerId()
    {
        const string PLAYER_ID_KEY = "PlayerID";
        
        if (PlayerPrefs.HasKey(PLAYER_ID_KEY))
        {
            return PlayerPrefs.GetString(PLAYER_ID_KEY);
        }

        string newId = System.Guid.NewGuid().ToString();
        PlayerPrefs.SetString(PLAYER_ID_KEY, newId);
        PlayerPrefs.Save();
        return newId;
    }
}