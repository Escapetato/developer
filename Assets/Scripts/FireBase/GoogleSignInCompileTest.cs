using UnityEngine;
using Google;

public class GoogleSignInCompileTest : MonoBehaviour
{
    void Start()
    {
        GoogleSignInConfiguration config =
            new GoogleSignInConfiguration();

        Debug.Log("Google Sign-In compile OK");
    }
}
