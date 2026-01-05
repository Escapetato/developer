using UnityEngine;

public class HarvestToolManager : MonoBehaviour
{
    public static HarvestToolManager Instance { get; private set; }

    [Header("현재 상태")]
    public int currentToolTier = 1;

    [Header("오브젝트 연결")]
    public GameObject bigQuadrantPanel; // 큰 사분원 (버튼들이 들어있는 부모 오브젝트)
    public SpriteRenderer currentToolSpriteRenderer; // 작은 사분원 (스프라이트)
    public Sprite[] toolIcons;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 처음 시작할 때 큰 사분원은 꺼둡니다.
        if (bigQuadrantPanel != null) bigQuadrantPanel.SetActive(false);
        
        SelectTool(1); // 기본 도구 설정
    }
    public void ToggleBigQuadrant()
    {
        if (bigQuadrantPanel != null)
        {
            // 현재 상태의 반대로 설정 (켜져 있으면 끄고, 꺼져 있으면 켬)
            bool isActive = bigQuadrantPanel.activeSelf;
            bigQuadrantPanel.SetActive(!isActive);
        }
    }

    public void SelectTool(int tier)
    {
        currentToolTier = tier;

        if (currentToolSpriteRenderer != null && toolIcons.Length >= tier)
        {
            currentToolSpriteRenderer.sprite = toolIcons[tier - 1];
        }

        // 도구를 선택하면 큰 사분원은 자동으로 닫히게 설정
       // if (bigQuadrantPanel != null) bigQuadrantPanel.SetActive(false);

        Debug.Log($"도구 {tier}번 선택 및 메뉴 닫힘");
    }
}