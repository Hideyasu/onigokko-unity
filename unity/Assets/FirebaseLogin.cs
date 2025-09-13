using Firebase;
using Firebase.Database;
using UnityEngine;

public class FirebaseLogin : MonoBehaviour
{
    DatabaseReference reference;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    async void Start()
    {
        Debug.Log("Firebase init start");
        var status = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (status == DependencyStatus.Available)
        {
            reference = FirebaseDatabase.DefaultInstance.RootReference;

            // データ書き込み例
            reference.Child("users").Child("test_user").Child("score").SetValueAsync(100);
            Debug.Log("Write Success");
        }
        else
        {
            Debug.LogError($"Firebase init failed: {status}");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
