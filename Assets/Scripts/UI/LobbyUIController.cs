using UnityEngine;
using UnityEngine.UI;

public class LobbyUIController : MonoBehaviour
{
    [Header("Top Menu Buttons")]
    public Button researchLabButton; // 연구실 버튼
    public Button storeButton;       // 상점 버튼

    void Start()
    {
        // 씬 시작 시(Start), 살아있는 UIManager를 찾아서 기능 연결!

        // 1. 연구실 버튼 연결
        if (researchLabButton != null)
        {
            researchLabButton.onClick.RemoveAllListeners();
            researchLabButton.onClick.AddListener(() => {
                UIManager.Instance.OpenResearchLabPopup(); // 싱글톤 호출
                SoundManager.Instance.PlaySFX("enter");
            });
        }

        // 2. 상점 버튼 연결
        if (storeButton != null)
        {
            storeButton.onClick.RemoveAllListeners();
            storeButton.onClick.AddListener(() => {
                UIManager.Instance.OpenStorePopup(); // 싱글톤 호출
                SoundManager.Instance.PlaySFX("enter");
            });
        }
    }
}