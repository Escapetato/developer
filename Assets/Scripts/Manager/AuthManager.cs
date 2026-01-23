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
    [Header("=== Panels (패널 연결) ===")]
    public GameObject loginPanel;      // 로그인 화면 패널
    public GameObject registerPanel;   // 회원가입 화면 패널

    [Header("=== Login UI (로그인 화면) ===")]
    public TMP_InputField emailField;
    public TMP_InputField passwordField;
    public Button loginButton;
    public Button goToRegisterButton;
    public Toggle rememberIdToggle;    // 아이디 저장 토글

    [Header("=== Register UI (회원가입 화면) ===")]
    public TMP_InputField regEmailField;
    public TMP_InputField regPasswordField;
    public TMP_InputField regConfirmPasswordField; // [추가] 비밀번호 재확인 필드
    public Button registerButton;
    public Button goToLoginButton;

    [Header("=== Common UI (공통) ===")]
    public Button googleLoginButton;
    public TextMeshProUGUI statusText; // [부활] 상태 메시지 (에러 표시용)

    [Header("Google Login")]
    public string webClientId;

    private FirebaseAuth auth;
    private GoogleSignInConfiguration googleConfig;
    private bool initialized = false;

    private bool loggedAuthNotReady = false;
    private bool loggedWebClientEmpty = false;

    void Awake() { }

    System.Collections.IEnumerator Start()
    {
        // 버튼 리스너 연결
        if (loginButton) loginButton.onClick.AddListener(TryLogin);
        if (registerButton) registerButton.onClick.AddListener(TryRegister);
        if (googleLoginButton) googleLoginButton.onClick.AddListener(TryGoogleLogin);

        if (goToRegisterButton) goToRegisterButton.onClick.AddListener(ShowRegisterPanel);
        if (goToLoginButton) goToLoginButton.onClick.AddListener(ShowLoginPanel);

        // 초기 상태
        ShowLoginPanel();

        // 저장된 아이디 불러오기
        if (PlayerPrefs.HasKey("SavedEmail"))
        {
            emailField.text = PlayerPrefs.GetString("SavedEmail");
            if (rememberIdToggle != null) rememberIdToggle.isOn = true;
        }

        while (FirebaseBootstrap.Auth == null)
            yield return null;

        EnsureInitialized();
    }

    void ShowLoginPanel()
    {
        if (loginPanel) loginPanel.SetActive(true);
        if (registerPanel) registerPanel.SetActive(false);
        if (statusText) statusText.text = ""; // 패널 바꿀 때 에러 메시지 초기화
        if (passwordField) passwordField.text = "";
    }

    void ShowRegisterPanel()
    {
        if (loginPanel) loginPanel.SetActive(false);
        if (registerPanel) registerPanel.SetActive(true);
        if (statusText) statusText.text = ""; // 패널 바꿀 때 에러 메시지 초기화

        // 가입창 초기화
        if (regEmailField) regEmailField.text = "";
        if (regPasswordField) regPasswordField.text = "";
        if (regConfirmPasswordField) regConfirmPasswordField.text = "";
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
                Debug.LogWarning("[AuthManager] Web Client ID 비어 있음");
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

    // [수정된 회원가입 시도 함수]
    void TryRegister()
    {
        EnsureInitialized();

        string email = regEmailField.text.Trim();
        string pass = regPasswordField.text.Trim();
        string confirmPass = regConfirmPasswordField != null ? regConfirmPasswordField.text.Trim() : "";

        // 1. 유효성 검사
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
        {
            SetStatusMessage("이메일과 비밀번호를 모두 입력해 주세요.", true);
            return;
        }
        if (pass != confirmPass)
        {
            SetStatusMessage("비밀번호가 일치하지 않습니다.", true);
            return;
        }
        if (pass.Length < 6)
        {
            SetStatusMessage("비밀번호는 최소 6자리 이상이어야 합니다.", true);
            return;
        }

        SetStatusMessage("회원가입 중...", false);

        // 2. Firebase 가입 요청
        auth.CreateUserWithEmailAndPasswordAsync(email, pass).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                SetStatusMessage("회원가입이 취소되었습니다.", true);
                return;
            }
            if (task.IsFaulted)
            {
                string errorMsg = "회원가입 실패!";
                if (task.Exception != null)
                {
                    Debug.LogError(task.Exception);
                    errorMsg = "이미 가입된 이메일이거나 형식이 잘못되었습니다.";
                }
                SetStatusMessage(errorMsg, true);
                return;
            }

            // 3. 성공 시 처리 (안전하게 코루틴으로 넘김)
            FirebaseUser newUser = ((Task<FirebaseUser>)task).Result;
            Debug.Log("가입 성공 UID: " + newUser.UserId);

            // 여기서 바로 화면 전환 로직을 코루틴으로 실행
            StartCoroutine(RegisterSuccessRoutine(email));
        });
    }

    // [추가] 가입 성공 후 대기 및 화면 전환을 담당하는 코루틴
    System.Collections.IEnumerator RegisterSuccessRoutine(string email)
    {
        // 성공 메시지 출력
        SetStatusMessage("가입 성공! 잠시 후 로그인 화면으로 이동합니다.", false);

        // 0.5초 대기
        yield return new WaitForSeconds(0.5f);

        // 로그인 화면으로 전환
        ShowLoginPanel();

        // 입력창 비우기
        regEmailField.text = "";
        regPasswordField.text = "";
        if (regConfirmPasswordField) regConfirmPasswordField.text = "";

        // 로그인 편의를 위해 이메일 채워주기
        emailField.text = email;

        // 상태 메시지 초기화 (선택사항)
        SetStatusMessage("", false);
    }
    // [핵심] 로그인 시도
    void TryLogin()
    {
        EnsureInitialized();

        string email = emailField.text.Trim();
        string pass = passwordField.text.Trim();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
        {
            SetStatusMessage("이메일과 비밀번호를 입력해 주세요.", true);
            return;
        }

        SetStatusMessage("로그인 중...", false);

        auth.SignInWithEmailAndPasswordAsync(email, pass).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                SetStatusMessage("로그인이 취소되었습니다.", true);
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("로그인 실패: " + task.Exception);
                // 포괄적인 에러 메시지
                SetStatusMessage("이메일이나 비밀번호를 확인해 주세요!", true);
                return;
            }

            FirebaseUser user = null;
            if (task is Task<FirebaseUser> userTask) user = userTask.Result;
            else if (task is Task<AuthResult> authTask) user = authTask.Result.User;

            if (user == null) return;

            Debug.Log("로그인 성공 UID: " + user.UserId);
            SetStatusMessage("로그인 성공!", false);

            // 아이디 저장 로직
            if (rememberIdToggle != null && rememberIdToggle.isOn)
            {
                PlayerPrefs.SetString("SavedEmail", email);
                PlayerPrefs.Save();
            }
            else
            {
                PlayerPrefs.DeleteKey("SavedEmail");
            }

            // 씬 이동
            var db = FindObjectOfType<DBManager>();
            if (db != null)
            {
                db.LoadAllData(user.UserId, () =>
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
                });
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
            }
        });
    }

    public void TryGoogleLogin()
    {
        EnsureInitialized();
        if (!initialized || auth == null) return;

        SetStatusMessage("Google 로그인 시도 중...", false);

        GoogleSignIn.Configuration = googleConfig;
        GoogleSignIn.DefaultInstance.SignOut();

        GoogleSignIn.DefaultInstance.SignIn().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                SetStatusMessage("Google 로그인 실패", true);
                return;
            }

            Task<GoogleSignInUser> signInTask = (Task<GoogleSignInUser>)task;
            GoogleSignInUser googleUser = signInTask.Result;
            Credential credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);

            auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(fbTask =>
            {
                if (fbTask.IsCanceled || fbTask.IsFaulted)
                {
                    SetStatusMessage("Firebase 연결 실패", true);
                    return;
                }

                FirebaseUser firebaseUser = ((Task<FirebaseUser>)fbTask).Result;
                Debug.Log("Google 로그인 성공 UID: " + firebaseUser.UserId);
                SetStatusMessage("Google 로그인 성공!", false);

                var db = FindObjectOfType<DBManager>();
                if (db != null)
                {
                    db.LoadAllData(firebaseUser.UserId, () =>
                    {
                        UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
                    });
                }
                else
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
                }
            });
        });
    }

    // 헬퍼 함수: 상태 메시지 색상 및 텍스트 설정
    void SetStatusMessage(string msg, bool isError)
    {
        if (statusText == null) return;
        statusText.text = msg;

        string errorColorHex = "#6B3F00";   // 에러일 때 색상 (아까 설정한 갈색)
        string successColorHex = "#6B3F00"; // 성공일 때 색상 (지금은 똑같이 갈색으로 해둠, 원하면 #228B22(초록) 등으로 변경 가능)

        // 상황에 맞는 색상 코드 선택
        string targetHex = isError ? errorColorHex : successColorHex;

        Color customColor;
        // Hex 코드를 컬러로 변환 시도
        if (ColorUtility.TryParseHtmlString(targetHex, out customColor))
        {
            statusText.color = customColor;
        }
        else
        {
            // 혹시라도 코드가 오타나서 변환 실패하면 유니티 기본색 사용
            statusText.color = isError ? Color.red : Color.green;
        }
    }

    void LogAuthException(System.Exception ex, string prefix)
    {
        Debug.LogError($"{prefix}: {ex}");
    }
}