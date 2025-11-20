using System.Collections.Generic;
using UnityEngine;

public class GameProgressionManager : MonoBehaviour
{
    public static GameProgressionManager Instance { get; private set; }

    // 'unlockedItems'로 일반화 (씨앗, 수확도구)
    public HashSet<ItemData> unlockedItems = new HashSet<ItemData>();

    // [테스트용] Inspector에서 미리 해금할 아이템 (호미 등)
    public List<ItemData> startingItems;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        foreach (ItemData item in startingItems)
        {
            UnlockItem(item);
        }
    }

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