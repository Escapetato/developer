using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Google;
using System.Threading.Tasks;

public class AuthManager : MonoBehaviour
{
    [Header("UI 연결")]
    public TMP_InputField emailField;
    public TMP_InputField passwordField;
    public Button loginButton;
    public Button registerButton;
    public TextMeshProUGUI statusText;

    [Header("Google Login")]
    public string webClientId; // Firebase 콘솔 Web Client ID

    private FirebaseAuth auth;
    private GoogleSignInConfiguration googleConfig;
    private bool initialized = false;

    /* =========================
     * Lifecycle
     * ========================= */
    void Awake()
    {
        EnsureInitialized();
    }

    void Start()
    {
        if (loginButton) loginButton.onClick.AddListener(TryLogin);
        if (registerButton) registerButton.onClick.AddListener(TryRegister);

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            Debug.Log("[Firebase] Dependencies: " + task.Result);
        });
    }

    /* =========================
     * Initialization (SAFE)
     * ========================= */
    void EnsureInitialized()
    {
        if (initialized) return;

        auth = FirebaseAuth.DefaultInstance;

        if (string.IsNullOrEmpty(webClientId))
        {
            Debug.LogError("[AuthManager] Web Client ID가 비어있습니다.");
        }
        else
        {
            googleConfig = new GoogleSignInConfiguration
            {
                WebClientId = webClientId,
                RequestIdToken = true,
                RequestEmail = true,
                UseGameSignIn = false
            };
        }

        initialized = true;
    }

    /* =========================
     * Email / Password Register
     * ========================= */
    void TryRegister()
    {
        EnsureInitialized();

        string email = emailField.text.Trim();
        string pass  = passwordField.text.Trim();

        if (statusText) statusText.text = "회원가입 시도 중...";

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
        {
            if (statusText) statusText.text = "이메일/비밀번호를 입력하세요.";
            return;
        }

        auth.CreateUserWithEmailAndPasswordAsync(email, pass)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("가입 실패: " + task.Exception);
                    if (statusText) statusText.text = "가입 실패";
                    return;
                }

                Debug.Log("가입 성공 UID: " + task.Result.User.UserId);
                if (statusText) statusText.text = "가입 성공";
            });
    }

    /* =========================
     * Email / Password Login
     * ========================= */
    void TryLogin()
    {
        EnsureInitialized();

        string email = emailField.text.Trim();
        string pass  = passwordField.text.Trim();

        if (statusText) statusText.text = "로그인 시도 중...";
        Debug.Log("[Auth] TryLogin called: " + email);

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
        {
            if (statusText) statusText.text = "이메일/비밀번호를 입력하세요.";
            return;
        }

        auth.SignInWithEmailAndPasswordAsync(email, pass)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("로그인 실패: " + task.Exception);
                    if (statusText) statusText.text = "로그인 실패";
                    return;
                }

                Debug.Log("로그인 성공 UID: " + task.Result.User.UserId);
                if (statusText) statusText.text = "로그인 성공";

                var db = FindObjectOfType<DBManager>();
                if (db != null)
                    db.LoadGameData(task.Result.User.UserId);
                else
                    Debug.LogWarning("[AuthManager] DBManager 없음 (Auth 씬일 수 있음)");

                UnityEngine.SceneManagement.SceneManager.LoadScene("Lab");
            });
    }

    /* =========================
     * Google Login
     * ========================= */
    public void TryGoogleLogin()
    {
        EnsureInitialized();
        _ = GoogleLoginAsync();
    }

    async Task GoogleLoginAsync()
    {
        try
        {
            if (googleConfig == null)
                throw new System.NullReferenceException("googleConfig is null");

            GoogleSignIn.Configuration = googleConfig;

            var googleUser = await GoogleSignIn.DefaultInstance.SignIn();
            Debug.Log("Google 로그인 성공: " + googleUser.Email);

            var credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);
            var firebaseUser = await auth.SignInWithCredentialAsync(credential);

            Debug.Log("Firebase Google 로그인 UID: " + firebaseUser.UserId);
            if (statusText) statusText.text = "Google 로그인 성공";

            var db = FindObjectOfType<DBManager>();
            if (db != null)
                db.LoadGameData(firebaseUser.UserId);
            else
                Debug.LogWarning("[AuthManager] DBManager 없음 (Auth 씬)");

            UnityEngine.SceneManagement.SceneManager.LoadScene("Lab");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Google 로그인 실패: " + e);
            if (statusText) statusText.text = "Google 로그인 실패";
        }
    }
}