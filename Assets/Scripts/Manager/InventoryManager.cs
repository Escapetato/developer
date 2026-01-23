using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Firebase.Auth; // ★ 중요: 저장하려면 내 ID를 알아야 해서 추가됨

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public Dictionary<ItemData, int> items = new Dictionary<ItemData, int>();
    public event Action OnInventoryChanged;

    [Header("테스트 및 특수 아이템 연결")]
    public ItemData testItem;
    public ItemData testPotion;
    public ItemData SeedItem;
    public ItemData FertilizerItem;
    public ItemData Tools;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
/*        if (testItem != null) AddItem(testItem, 10);
        if (testPotion != null) AddItem(testPotion, 10);
        if (Tools != null) AddItem(Tools, 1);*/
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T) && testItem != null) AddItem(testItem, 1);
        if (Input.GetKeyDown(KeyCode.Y) && testPotion != null) AddItem(testPotion, 1);
        if (Input.GetKeyDown(KeyCode.J) && SeedItem != null) AddItem(SeedItem, 5);
        if (Input.GetKeyDown(KeyCode.U) && FertilizerItem != null) AddItem(FertilizerItem, 5);
        if (Input.GetKeyDown(KeyCode.Z) && Tools != null) AddItem(Tools, 1);
    }

    void SaveToDB()
    {
        if (FirebaseAuth.DefaultInstance.CurrentUser != null && DBManager.Instance != null)
        {
            string myId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
            DBManager.Instance.SaveAllData(myId);
        }
    }

    public void AddItem(ItemData item, int amount)
    {
        if (items.ContainsKey(item)) items[item] += amount;
        else items.Add(item, amount);

        OnInventoryChanged?.Invoke();
        Debug.Log(item.itemName + " " + amount + "개 추가됨.");

        // ▼▼▼ [추가] 아이템을 먹었으면 도감 매니저에게 알려줘야 함
        if (GameProgressionManager.Instance != null)
        {
            GameProgressionManager.Instance.UnlockItem(item);
        }
        // ▲▲▲ 추가 끝 ▲▲▲

        SaveToDB();
    }
    public void RemoveItem(ItemData item, int amount)
    {
        if (items.ContainsKey(item))
        {
            items[item] -= amount;
            if (items[item] <= 0) items.Remove(item);

            OnInventoryChanged?.Invoke();
            Debug.Log(item.itemName + " " + amount + "개 사용.");

            // ▼▼▼ [추가] 아이템 썼으니 저장! ▼▼▼
            SaveToDB();
        }
    }

    public int GetItemCount(ItemData item)
    {
        if (items.ContainsKey(item)) return items[item];
        return 0;
    }

    // DBManager가 불러올 때 쓰는 함수
    public void LoadInventory(List<InventorySaveData> savedList)
    {
        items.Clear();
        foreach (var savedData in savedList)
        {
            // DBManager에 있는 아이템 명부에서 이름으로 찾기
            if (DBManager.Instance == null) return;

            ItemData item = DBManager.Instance.FindItemByName(savedData.itemName);
            if (item != null)
            {
                items.Add(item, savedData.amount);
            }
            else
            {
                Debug.LogWarning($"[인벤 로드 실패] '{savedData.itemName}'을 DBManager 리스트에서 찾을 수 없습니다.");
            }
        }
        OnInventoryChanged?.Invoke();
        Debug.Log("인벤토리 복구 완료");
    }
}