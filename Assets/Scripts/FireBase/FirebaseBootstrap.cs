using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;

public class FirebaseBootstrap : MonoBehaviour
{
    public static FirebaseAuth Auth;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        FirebaseApp.CheckAndFixDependenciesAsync()
            .ContinueWithOnMainThread(task =>
            {
                Debug.Log("[FirebaseBootstrap] DependencyStatus = " + task.Result);

                if (task.Result == DependencyStatus.Available)
                {
                    Auth = FirebaseAuth.DefaultInstance;
                    Debug.Log("[FirebaseBootstrap] FirebaseAuth ready");
                }
                else
                {
                    Debug.LogError("[FirebaseBootstrap] Firebase init failed");
                }
            });
    }
}
