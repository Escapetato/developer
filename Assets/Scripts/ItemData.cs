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
    public Sprite itemIconOffVer;
    public string itemCategory; // "Seed", "Tool", "Potion", "Fertilizer", "Theme"

    // [도감용] 세부 분류 (예: "Vegetable", "Fruit", "Grain")
    public string collectionCategory;

    public int price = 50;

    [Header("씨앗 전용 정보 (Seed)")]
    public SeedType seedCategory = SeedType.None;
    public GameObject plantPrefab;
    public float growTime;         // 실제 로직용 시간 (초)
    public ItemData harvestItem;   // 수확물
    public int requiredToolTier = 1;

    [Header("도감 표시용 정보 (UI)")]
    public string growTimeDisplay = "1분";    // 예: "10분", "30초"
    public string harvestToolName = "호미";   // 예: "호미", "낫"

    [Header("수확 도구 전용 정보 (Tool)")]
    public int toolTier = 0;

    [Header("비료 전용 정보 (Fertilizer)")]
    public float growthReductionPercent = 0.25f;

    [Header("해금 및 설명")]
    public bool isDefaultUnlocked = false;

    [TextArea]
    public string itemDescription;
}