using Firebase;
using UnityEngine;

public class FirebaseLogin : MonoBehaviour
{
    async void Start()
    {
        Debug.Log("Firebase initializing...");
        var status = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (status == DependencyStatus.Available)
        {
            Debug.Log("Firebase initialized successfully");
        }
        else
        {
            Debug.LogError($"Firebase init failed: {status}");
        }
    }
}
