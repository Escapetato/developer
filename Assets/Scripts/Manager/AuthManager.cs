using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Google;
using System.Threading.Tasks;
using System.Collections;

public class AuthManager : MonoBehaviour
{
    [Header("=== Panels (패널 연결) ===")]
    public GameObject loginPanel;
    public GameObject registerPanel;

    [Header("=== Login UI (로그인 화면) ===")]
    public TMP_InputField emailField;
    public TMP_InputField passwordField;
    public Button loginButton;
    public Button goToRegisterButton;
    public Toggle rememberIdToggle;

    [Header("=== Register UI (회원가입 화면) ===")]
    public TMP_InputField regEmailField;
    public TMP_InputField regPasswordField;
    public TMP_InputField regConfirmPasswordField;
    public Button registerButton;
    public Button goToLoginButton;

    [Header("=== Common UI (공통) ===")]
    public Button googleLoginButton;
    public TextMeshProUGUI statusText;

    [Header("Google Login")]
    public string webClientId;

    private FirebaseAuth auth;
    private GoogleSignInConfiguration googleConfig;
    private bool initialized = false;

    private bool loggedAuthNotReady = false;
    private bool loggedWebClientEmpty = false;

    // ▼▼▼ [핵심 수정] 스레드 문제를 피하기 위한 깃발 변수들 ▼▼▼
    private bool isRegisterSuccess = false; // 회원가입 성공했나?
    private string registeredEmailTemp = ""; // 가입한 이메일 임시 저장

    private bool isLoginSuccess = false;    // 로그인 성공했나?
    private string loginUserIdTemp = "";    // 로그인한 UID 임시 저장
    // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

    void Awake() { }

    IEnumerator Start()
    {
        if (loginButton) loginButton.onClick.AddListener(TryLogin);
        if (registerButton) registerButton.onClick.AddListener(TryRegister);
        if (googleLoginButton) googleLoginButton.onClick.AddListener(TryGoogleLogin);

        if (goToRegisterButton) goToRegisterButton.onClick.AddListener(ShowRegisterPanel);
        if (goToLoginButton) goToLoginButton.onClick.AddListener(ShowLoginPanel);

        ShowLoginPanel();

        if (PlayerPrefs.HasKey("SavedEmail"))
        {
            emailField.text = PlayerPrefs.GetString("SavedEmail");
            if (rememberIdToggle != null) rememberIdToggle.isOn = true;
        }

        // FirebaseBootstrap 기다리기
        var dependencyTask = FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => dependencyTask.IsCompleted);

        if (dependencyTask.Result == DependencyStatus.Available)
        {
            auth = FirebaseAuth.DefaultInstance;
            initialized = true;
        }
        else
        {
            Debug.LogError("Firebase Init Failed: " + dependencyTask.Result);
        }

        EnsureInitialized();
    }

    // ▼▼▼ [핵심 수정] Update에서 깃발을 감시하다가 실행 (이건 무조건 메인스레드임) ▼▼▼
    void Update()
    {
        // 1. 회원가입 성공 감지
        if (isRegisterSuccess)
        {
            isRegisterSuccess = false; // 깃발 내리기
            StartCoroutine(RegisterSuccessRoutine(registeredEmailTemp));
        }

        // 2. 로그인 성공 감지
        if (isLoginSuccess)
        {
            isLoginSuccess = false; // 깃발 내리기
            ProceedLogin(loginUserIdTemp);
        }
    }
    // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

    void ShowLoginPanel()
    {
        if (loginPanel) loginPanel.SetActive(true);
        if (registerPanel) registerPanel.SetActive(false);
        if (statusText) statusText.text = "";
        if (passwordField) passwordField.text = "";
    }

    void ShowRegisterPanel()
    {
        if (loginPanel) loginPanel.SetActive(false);
        if (registerPanel) registerPanel.SetActive(true);
        if (statusText) statusText.text = "";

        if (regEmailField) regEmailField.text = "";
        if (regPasswordField) regPasswordField.text = "";
        if (regConfirmPasswordField) regConfirmPasswordField.text = "";
    }

    void EnsureInitialized()
    {
        if (initialized) return;
        if (auth == null) auth = FirebaseAuth.DefaultInstance;

        if (!string.IsNullOrEmpty(webClientId))
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

        string email = regEmailField.text.Trim();
        string pass = regPasswordField.text.Trim();
        string confirmPass = regConfirmPasswordField != null ? regConfirmPasswordField.text.Trim() : "";

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
                if (task.Exception != null) errorMsg = "이미 가입된 이메일이거나 형식이 잘못되었습니다.";
                SetStatusMessage(errorMsg, true);
                return;
            }

            // ▼▼▼ [수정된 부분] AuthResult로 받아서 User 꺼내기 ▼▼▼
            AuthResult result = task.Result;
            FirebaseUser newUser = result.User; // 여기서 꺼냄
            // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

            Debug.Log("가입 성공 UID: " + newUser.UserId);

            // 성공 시 -> 깃발 들기
            registeredEmailTemp = email;
            isRegisterSuccess = true;
        });
    }

    IEnumerator RegisterSuccessRoutine(string email)
    {
        SetStatusMessage("가입 성공! 로그인 화면으로 이동합니다.", false);
        yield return new WaitForSeconds(0.5f);

        ShowLoginPanel();

        regEmailField.text = "";
        regPasswordField.text = "";
        if (regConfirmPasswordField) regConfirmPasswordField.text = "";

        emailField.text = email;
        SetStatusMessage("", false);
    }

    void TryLogin()
    {
        EnsureInitialized();

        string email = emailField.text.Trim();
        string pass = passwordField.text.Trim();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
        {
            SetStatusMessage("이메일과 비밀번호를 모두 입력해 주세요.", true);
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
                SetStatusMessage("이메일이나 비밀번호를 확인해 주세요!", true);
                return;
            }

            // ▼▼▼ [수정된 부분] AuthResult로 받아서 User 꺼내기 ▼▼▼
            AuthResult result = task.Result;
            FirebaseUser user = result.User;
            // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

            Debug.Log("로그인 성공 UID: " + user.UserId);

            // 아이디 저장
            if (rememberIdToggle != null && rememberIdToggle.isOn)
            {
                PlayerPrefs.SetString("SavedEmail", email);
                PlayerPrefs.Save();
            }
            else
            {
                PlayerPrefs.DeleteKey("SavedEmail");
            }

            // 성공 시 -> 깃발 들기
            loginUserIdTemp = user.UserId;
            isLoginSuccess = true;
        });
    }

    // 로그인 후처리 함수 (Update에서 호출됨)
    void ProceedLogin(string userId)
    {
        SetStatusMessage("로그인 성공!", false);

        var db = FindObjectOfType<DBManager>();
        if (db != null)
        {
            db.LoadAllData(userId, () =>
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
            });
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
        }
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

            GoogleSignInUser googleUser = task.Result;
            Credential credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);

            auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(fbTask =>
            {
                if (fbTask.IsCanceled || fbTask.IsFaulted)
                {
                    SetStatusMessage("Firebase 연결 실패", true);
                    return;
                }

                FirebaseUser firebaseUser = fbTask.Result;

                // 성공 시 -> 깃발 들기
                loginUserIdTemp = firebaseUser.UserId;
                isLoginSuccess = true;
            });
        });
    }

    void SetStatusMessage(string msg, bool isError)
    {
        if (statusText == null) return;
        statusText.text = msg;

        string errorColorHex = "#6B3F00";
        string successColorHex = "#6B3F00";
        string targetHex = isError ? errorColorHex : successColorHex;

        Color customColor;
        if (ColorUtility.TryParseHtmlString(targetHex, out customColor))
            statusText.color = customColor;
        else
            statusText.color = isError ? Color.red : Color.green;
    }
}