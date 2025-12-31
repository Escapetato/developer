using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using Firebase.Auth;
using Firebase.Extensions; // 이걸 써야 에러 안 남

public class AuthManager : MonoBehaviour
{
    [Header("UI 연결")]
    public TMP_InputField emailField;    // 이메일 입력창
    public TMP_InputField passwordField; // 비번 입력창
    public Button loginButton;           // 로그인 버튼
    public Button registerButton;        // 회원가입 버튼
    public TextMeshProUGUI statusText;   // 상태 메시지 (없으면 안 넣어도 됨)

    FirebaseAuth auth;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;

        // 버튼 눌렀을 때 실행될 함수 연결
        loginButton.onClick.AddListener(TryLogin);
        registerButton.onClick.AddListener(TryRegister);
    }

    // 회원가입
    void TryRegister()
    {
        string email = emailField.text;
        string pass = passwordField.text;

        auth.CreateUserWithEmailAndPasswordAsync(email, pass).ContinueWithOnMainThread(task => {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("가입 실패: " + task.Exception);
                if (statusText) statusText.text = "가입 실패";
                return;
            }
            Debug.Log("가입 성공! UID: " + task.Result.User.UserId);
            if (statusText) statusText.text = "가입 성공";
        });
    }

    // 로그인
    void TryLogin()
    {
        string email = emailField.text;
        string pass = passwordField.text;

        auth.SignInWithEmailAndPasswordAsync(email, pass).ContinueWithOnMainThread(task => {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("로그인 실패: " + task.Exception);
                if (statusText) statusText.text = "로그인 실패..";
                return;
            }
            Debug.Log("로그인 성공! 님 ID: " + task.Result.User.UserId);
            if (statusText) statusText.text = "로그인 성공!";

            // 여기서 게임 씬으로 넘어가면 됨!
            // UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
        });
    }
}