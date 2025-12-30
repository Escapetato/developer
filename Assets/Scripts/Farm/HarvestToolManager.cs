using UnityEngine;
using UnityEngine.UI;

public class HarvestToolManager : MonoBehaviour
{
    public static HarvestToolManager Instance { get; private set; }

    [Header("현재 상태 (데이터)")]
    public int currentToolTier = 1; // 1:호미, 2:낫, 3:모종삽, 4:전지가위

    [Header("작은 사분원 (시각적 표시)")]
    public Image currentToolDisplayImage;
    public Sprite[] toolIcons; // 인스펙터에서 4개의 도구 이미지를 순서대로 등록

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 게임 시작 시 기본 도구(1번: 호미) 상태로 이미지와 데이터 초기화
        SelectTool(1);
    }

    public void SelectTool(int tier)
    {
        // 1. 현재 선택된 도구 티어 데이터 변경
        currentToolTier = tier;

        // 2. 작은 사분원 SpriteRenderer 이미지 변경 (티어는 1부터이므로 인덱스는 -1)
        if (currentToolDisplayImage != null && toolIcons.Length >= tier)
        {
            currentToolDisplayImage.sprite = toolIcons[tier - 1];
            currentToolDisplayImage.enabled = true;
        }

        Debug.Log($"도구 변경 완료: {tier}번 도구 (현재 선택된 티어: {currentToolTier})");
    }
}