using UnityEngine;
using Firebase;
using Firebase.Database;
using System.Threading.Tasks;

public class FirebaseCreateRoom : MonoBehaviour
{
    public async Task CreateRoom()
    {
        string userId = PlayerPrefs.GetString("PlayerID", "");
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("PlayerID not found in PlayerPrefs");
            return;
        }

        var reference = FirebaseDatabase.DefaultInstance.RootReference;
        var newRoomRef = reference.Child("rooms").Push();
        string roomId = newRoomRef.Key;

        await newRoomRef.Child("users").Child(userId).SetValueAsync(true);
        
        PlayerPrefs.SetString("RoomID", roomId);
        PlayerPrefs.Save();
        
        Debug.Log($"Room created - RoomID: {roomId}, UserID: {userId}");
    }

    public async void Start()
    {
      Debug.Log("FirebaseCreateRoom Start() called on: " + gameObject.name);
      await CreateRoom();
    }
}
