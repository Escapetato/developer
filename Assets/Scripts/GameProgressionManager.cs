using System.Collections.Generic;
using UnityEngine;

public class GameProgressionManager : MonoBehaviour
{
    public static GameProgressionManager Instance { get; private set; }

    [Header("--- Shop Status ---")]
    // 상점이 해금되었는지 여부 (기본값 false = 잠김)
    public bool isShopUnlocked = false;

    [Header("--- Item Unlocks ---")]
    // 'unlockedItems'로 일반화 (씨앗, 수확도구 등 도감용)
    public HashSet<ItemData> unlockedItems = new HashSet<ItemData>();

    // [테스트용] Inspector에서 미리 해금할 아이템
    public List<ItemData> startingItems;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 테스트용 아이템 해금
        foreach (ItemData item in startingItems)
        {
            UnlockItem(item);
        }
    }

    // 상점 해금 함수
    public void UnlockShop()
    {
        if (!isShopUnlocked)
        {
            isShopUnlocked = true;
            Debug.Log("🔓 상점 해금 완료! 이제 상점 이용 가능.");

            // (선택사항) 여기서 상점 해금 알림 UI를 띄워도 됨
            // UIManager.Instance.ShowToastMessage("상점이 오픈되었습니다!");
        }
    }

    // 아이템 해금 (도감용)
    public void UnlockItem(ItemData item)
    {
        if (item != null && !unlockedItems.Contains(item))
        {
            unlockedItems.Add(item);
            Debug.Log(item.itemName + " 해금!");
        }
    }

    public bool IsItemUnlocked(ItemData item)
    {
        return unlockedItems.Contains(item);
    }
}