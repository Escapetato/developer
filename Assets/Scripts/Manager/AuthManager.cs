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

    // 로그 중복 방지용
    private bool loggedAuthNotReady = false;
    private bool loggedWebClientEmpty = false;

    void Awake()
    {
        // 초기화는 Start에서 진행
    }

    System.Collections.IEnumerator Start()
    {
        if (loginButton) loginButton.onClick.AddListener(TryLogin);
        if (registerButton) registerButton.onClick.AddListener(TryRegister);
        if (googleLoginButton) googleLoginButton.onClick.AddListener(TryGoogleLogin);

        // FirebaseBootstrap 대기
        while (FirebaseBootstrap.Auth == null)
            yield return null;

        EnsureInitialized();
    }

    void EnsureInitialized()
    {
        if (initialized) return;

        auth = FirebaseBootstrap.Auth;
        if (auth == null)
        {
            if (!loggedAuthNotReady)
            {
                Debug.LogWarning("[AuthManager] FirebaseAuth not ready");
                loggedAuthNotReady = true;
            }
            return;
        }

        if (string.IsNullOrEmpty(webClientId))
        {
            if (!loggedWebClientEmpty)
            {
                Debug.LogWarning("[AuthManager] Web Client ID 비어있음 (Google 로그인 불가)");
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
        string pass = passwordField.text.Trim();

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

                // [수정] 타입 명시적 변환
                FirebaseUser newUser = ((Task<FirebaseUser>)task).Result;
                Debug.Log("가입 성공 UID: " + newUser.UserId);
                
                if (statusText) statusText.text = "가입 성공";
            });
    }

    void TryLogin()
    {
        EnsureInitialized();

        string email = emailField.text.Trim();
        string pass = passwordField.text.Trim();

        if (statusText) statusText.text = "로그인 시도 중...";

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
        {
            if (statusText) statusText.text = "이메일/비밀번호를 입력하세요.";
            return;
        }

        // ▼▼▼ [수정] 파라미터를 그냥 'task'로 두고, 안에서 변환합니다 ▼▼▼
        auth.SignInWithEmailAndPasswordAsync(email, pass).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("로그인 취소");
                if (statusText) statusText.text = "로그인 취소";
                return;
            }
            if (task.IsFaulted)
            {
                LogAuthException(task.Exception, "로그인 실패");
                if (statusText) statusText.text = "로그인 실패";
                return;
            }

            FirebaseUser user = null;

            // 1. 최신 버전 SDK 방식 (Task<FirebaseUser>)
            if (task is Task<FirebaseUser> userTask)
            {
                user = userTask.Result;
            }
            // 2. 구버전 SDK 방식 (Task<AuthResult>) - 혹시 몰라 대비용
            else if (task is Task<AuthResult> authTask)
            {
                user = authTask.Result.User;
            }

            // 유저 정보가 없으면 에러 처리
            if (user == null)
            {
                Debug.LogError("로그인 결과에서 유저 정보를 찾을 수 없습니다.");
                return;
            }

            Debug.Log("로그인 성공 UID: " + user.UserId);
            if (statusText) statusText.text = "로그인 성공";

            var db = FindObjectOfType<DBManager>();
            if (db != null)
            {
                db.LoadAllData(user.UserId); // DBManager 호출
            }
            else
            {
                Debug.LogWarning("[AuthManager] DBManager 없음");
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
        });
    }

    public void TryGoogleLogin()
    {
        EnsureInitialized();

        if (!initialized || auth == null)
        {
            Debug.LogWarning("[AuthManager] Firebase not initialized");
            if (statusText) statusText.text = "초기화 중...";
            return;
        }

        if (string.IsNullOrEmpty(webClientId) || googleConfig == null)
        {
            Debug.LogError("Web Client ID 미설정");
            if (statusText) statusText.text = "Web Client ID 미설정";
            return;
        }

        if (statusText) statusText.text = "Google 로그인 시도 중...";

        GoogleSignIn.Configuration = googleConfig;
        GoogleSignIn.DefaultInstance.SignOut();

        // ▼▼▼ 여기가 에러 나던 부분 (수정됨) ▼▼▼
        GoogleSignIn.DefaultInstance.SignIn().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("Google 로그인 실패");
                if (statusText) statusText.text = "Google 로그인 실패";
                return;
            }

            // 1. 구글 로그인 결과 가져오기 (타입 명시)
            Task<GoogleSignInUser> signInTask = (Task<GoogleSignInUser>)task;
            GoogleSignInUser googleUser = signInTask.Result; // 여기에 .User 붙이면 안됨!

            // 2. 파이어베이스에 자격 증명 넘기기
            Credential credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);

            auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(fbTask =>
            {
                if (fbTask.IsCanceled || fbTask.IsFaulted)
                {
                    LogAuthException(fbTask.Exception, "Firebase 로그인 실패");
                    if (statusText) statusText.text = "Firebase 로그인 실패";
                    return;
                }

                // 3. 파이어베이스 유저 결과 가져오기 (타입 명시)
                FirebaseUser firebaseUser = ((Task<FirebaseUser>)fbTask).Result;

                Debug.Log("Google 로그인 성공 UID: " + firebaseUser.UserId);
                if (statusText) statusText.text = "Google 로그인 성공";

                var db = FindObjectOfType<DBManager>();
                if (db != null)
                {
                    // LoadAllData 사용
                    db.LoadAllData(firebaseUser.UserId);
                }
                else
                    Debug.LogWarning("[AuthManager] DBManager 없음");

                UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
            });
        });
    }

    void LogAuthException(System.Exception ex, string prefix)
    {
        Debug.LogError($"{prefix}: {ex}");
    }
}