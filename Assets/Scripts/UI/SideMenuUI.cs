using UnityEngine;
using UnityEngine.UI;

public class SideMenuUI : MonoBehaviour
{
    public static SideMenuUI Instance { get; private set; }

    [Header("Settings")]
    public GameObject menuContainer; // 접었다 폈다 할 대상
    public Button toggleButton;      // ★ 회전시킬 버튼 자체

    private bool isExpanded = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveAllListeners();
            toggleButton.onClick.AddListener(OnClickToggle);
        }

        CloseMenu();
    }

    public void OnClickToggle()
    {
        isExpanded = !isExpanded;
        UpdateUI();
    }

    public void CloseMenu()
    {
        isExpanded = false;
        UpdateUI();
    }

    private void UpdateUI()
    {
        // 1. 목록 껐다 켜기
        if (menuContainer != null)
            menuContainer.SetActive(isExpanded);

        // 2. ★ 버튼 자체를 회전시키기 ★
        if (toggleButton != null)
        {
            // 펼쳐지면(isExpanded) 180도, 아니면 0도
            float zRotation = isExpanded ? 180f : 0f;

            // toggleButton의 트랜스폼(위치/회전 담당)을 직접 돌립니다.
            toggleButton.transform.localRotation = Quaternion.Euler(0, 0, zRotation);
        }
    }
}