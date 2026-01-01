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
    public Button googleLoginButton;
    public TextMeshProUGUI statusText;

    [Header("Google Login")]
    public string webClientId; // Firebase 콘솔 Web Client ID

    private FirebaseAuth auth;
    private GoogleSignInConfiguration googleConfig;
    private bool initialized = false;

    private bool loggedAuthNotReady = false;
    private bool loggedWebClientEmpty = false;

    /* =========================
     * Lifecycle
     * ========================= */
    // FirebaseBootstrap 초기화는 비동기라서, Awake에서 바로 Auth를 잡으면 null일 수 있음
    void Awake()
    {
        // 의도적으로 여기서는 초기화하지 않음 (Start에서 대기 후 초기화)
    }

    System.Collections.IEnumerator Start()
    {
        if (loginButton) loginButton.onClick.AddListener(TryLogin);
        if (registerButton) registerButton.onClick.AddListener(TryRegister);
        if (googleLoginButton) googleLoginButton.onClick.AddListener(TryGoogleLogin);

        // FirebaseBootstrap가 Auth를 준비할 때까지 잠깐 대기 (첫 프레임에서 null 방지)
        while (FirebaseBootstrap.Auth == null)
            yield return null;

        EnsureInitialized();
    }

    /* =========================
     * Initialization (SAFE)
     * ========================= */
    void EnsureInitialized()
    {
        if (initialized) return;

        auth = FirebaseBootstrap.Auth;
        if (auth == null)
        {
            if (!loggedAuthNotReady)
            {
                Debug.LogWarning("[AuthManager] FirebaseAuth not ready (FirebaseBootstrap 초기화 대기 중)");
                loggedAuthNotReady = true;
            }
            return;
        }

        if (string.IsNullOrEmpty(webClientId))
        {
            if (!loggedWebClientEmpty)
            {
                Debug.LogWarning("[AuthManager] Web Client ID가 비어있습니다. (Google 로그인만 영향)");
                loggedWebClientEmpty = true;
            }
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
                    LogAuthException(task.Exception, "가입 실패");
                    if (statusText) statusText.text = "가입 실패";
                    return;
                }

                Debug.Log("가입 성공 UID: " + task.Result.User.UserId);
                if (statusText) statusText.text = "가입 성공";
            });
    }

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
                    LogAuthException(task.Exception, "로그인 실패");
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

    void LogAuthException(System.Exception ex, string prefix)
    {
        Debug.LogError($"{prefix}: {ex}");

        // AggregateException 풀기
        if (ex is System.AggregateException ag)
            ex = ag.Flatten().InnerExceptions[0];

        if (ex is FirebaseException fex)
        {
            Debug.LogError($"{prefix}: FirebaseException ErrorCode(int) = {fex.ErrorCode}");

            try
            {
                var authError = (AuthError)fex.ErrorCode;
                Debug.LogError($"{prefix}: AuthError = {authError}");
            }
            catch
            {
                Debug.LogError($"{prefix}: AuthError cast 실패");
            }
        }
    }

    public void TryGoogleLogin()
    {
        EnsureInitialized();

        if (!initialized || auth == null)
        {
            Debug.LogWarning("[AuthManager] Firebase not initialized yet");
            if (statusText) statusText.text = "Firebase 초기화 중...";
            return;
        }

        if (string.IsNullOrEmpty(webClientId) || googleConfig == null)
        {
            Debug.LogError("[AuthManager] Web Client ID가 설정되지 않았습니다. (Google 로그인 불가)");
            if (statusText) statusText.text = "Web Client ID 미설정";
            return;
        }

        if (statusText) statusText.text = "Google 로그인 시도 중...";

        // 항상 계정 선택 화면이 뜨게 하고 싶으면 SignOut 후 SignIn
        GoogleSignIn.Configuration = googleConfig;
        GoogleSignIn.DefaultInstance.SignOut();

        GoogleSignIn.DefaultInstance.SignIn().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogWarning("Google 로그인 취소");
                if (statusText) statusText.text = "Google 로그인 취소";
                return;
            }

            if (task.IsFaulted)
            {
                Debug.LogError("Google 로그인 실패: " + task.Exception);
                if (statusText) statusText.text = "Google 로그인 실패";
                return;
            }

            // ContinueWithOnMainThread 콜백 파라미터 타입이 Task로 잡히는 경우가 있어 안전 캐스팅
            var signInTask = task as Task<GoogleSignInUser>;
            if (signInTask == null)
            {
                Debug.LogError("[AuthManager] Google SignIn Task 타입 캐스팅 실패");
                if (statusText) statusText.text = "Google 로그인 실패";
                return;
            }

            var googleUser = signInTask.Result;
            Debug.Log("Google 로그인 성공: " + googleUser.Email);

            if (string.IsNullOrEmpty(googleUser.IdToken))
            {
                Debug.LogError("[AuthManager] Google IdToken이 비어있습니다. WebClientId/RequestIdToken 설정 확인 필요");
                if (statusText) statusText.text = "IdToken 없음";
                return;
            }

            var credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);

            auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(fbTask =>
            {
                if (fbTask.IsCanceled || fbTask.IsFaulted)
                {
                    LogAuthException(fbTask.Exception, "Firebase Google 로그인 실패");
                    if (statusText) statusText.text = "Firebase 로그인 실패";
                    return;
                }

                // Firebase SDK 버전에 따라 반환 타입이 AuthResult 또는 FirebaseUser일 수 있음
                FirebaseUser firebaseUser = null;

                var authResultTask = fbTask as Task<AuthResult>;
                if (authResultTask != null)
                    firebaseUser = authResultTask.Result.User;
                else
                {
                    var userTask = fbTask as Task<FirebaseUser>;
                    if (userTask != null)
                        firebaseUser = userTask.Result;
                }

                if (firebaseUser == null)
                {
                    Debug.LogError("[AuthManager] Firebase SignInWithCredentialAsync 결과 타입을 해석할 수 없습니다.");
                    if (statusText) statusText.text = "Firebase 로그인 실패";
                    return;
                }

                Debug.Log("Firebase Google 로그인 UID: " + firebaseUser.UserId);
                if (statusText) statusText.text = "Google 로그인 성공";

                var db = FindObjectOfType<DBManager>();
                if (db != null)
                    db.LoadGameData(firebaseUser.UserId);
                else
                    Debug.LogWarning("[AuthManager] DBManager 없음 (Auth 씬)");

                UnityEngine.SceneManagement.SceneManager.LoadScene("Lab");
            });
        });
    }
}