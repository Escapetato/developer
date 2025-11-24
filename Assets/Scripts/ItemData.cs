using UnityEngine;

  public enum SeedType
    {
        None,       // 미지정
        Fruit,      // 과일
        Grain,      // 곡식
        Vegetable   // 야채
    }

[CreateAssetMenu(fileName = "NewItem", menuName = "Data/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("기본 정보")]
    public string itemName;
    public string description;
    public Sprite itemIcon;
    public string itemCategory; // "Seed", "Tool", "Potion", "Fertilizer", "Theme"
    public int price = 50;

    [Header("씨앗 전용 정보 (Seed)")]
    public SeedType seedCategory = SeedType.None;
    public GameObject plantPrefab;
    public float growTime;       // (초 단위, 예: 10분 = 600)
    public ItemData harvestItem; // 수확물

    // (씨앗에만 필요) 이 씨앗을 수확할 때 필요한 도구 레벨
    public int requiredToolTier = 1; // 1=호미, 2=낫, 3=모종삽, 4=가위

    [Header("수확 도구 전용 정보 (Tool)")]
    // (도구 아이템 자체에 필요) 이 도구의 레벨
    public int toolTier = 0;

    [Header("비료 전용 정보 (Fertilizer)")]
    // (비료 아이템 자체에 필요)
    public float growthReductionPercent = 0.25f; // 25%

    // 꾸미기 테마는 특별한 변수 없이 "Theme" 카테고리와 이름만
}