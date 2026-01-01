using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; 

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    // 인벤토리 데이터: (어떤 아이템, 몇 개)
    public Dictionary<ItemData, int> items = new Dictionary<ItemData, int>();

    // 인벤토리가 변경될 때 UI에 알려주는 부분
    public event Action OnInventoryChanged;

    [Header("테스트 및 특수 아이템 연결")]
    public ItemData testItem;        // Inspector에 테스트용 아이템 연결
    public ItemData testPotion;
    public ItemData SeedItem;        // 씨앗
    public ItemData FertilizerItem;  // [추가] 비료 아이템 (Field.cs에서 참조할 예정)

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // 필요하다면 주석 해제
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // start랑 update는 테스트 
    void Start()
    {
        // 게임 시작하자마자 테스트 아이템을 10개씩 자동으로 넣기
        if (testItem != null)
        {
            AddItem(testItem, 10);
        }

        if (testPotion != null)
        {
            AddItem(testPotion, 10);
        }
    }

    // 'T' 키를 누르면 green_apple 1개 추가
    void Update()
    {
        // 'T' 키를 누르면 green_apple 1개 추가
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (testItem != null) AddItem(testItem, 1);
        }

        if (Input.GetKeyDown(KeyCode.Y)) 
        {
            if (testPotion != null) AddItem(testPotion, 1);
        }

        // 'J' 키로 star 씨앗 5개 추가
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (SeedItem != null) AddItem(SeedItem, 5);
        }

        // [추가] 'U' 키로 비료 5개 추가 테스트
        if (Input.GetKeyDown(KeyCode.U))
        {
            if (FertilizerItem != null) 
            {
                AddItem(FertilizerItem, 5);
                Debug.Log($"비료 {FertilizerItem.itemName} {5}개 추가!");
            }
            else 
            {
                Debug.LogWarning("FertilizerItem이 Inspector에 연결되지 않았습니다.");
            }
        }
    }

    // 아이템 추가 함수 (기존과 동일)
    public void AddItem(ItemData item, int amount)
    {
        if (items.ContainsKey(item))
        {
            items[item] += amount;
        }
        else
        {
            items.Add(item, amount);
        }

        OnInventoryChanged?.Invoke();
        Debug.Log(item.itemName + " " + amount + "개 추가. 총: " + items[item] + "개");
    }

    // 아이템 제거 함수 (기존과 동일)
    public void RemoveItem(ItemData item, int amount)
    {
        if (items.ContainsKey(item))
        {
            items[item] -= amount;

            if (items[item] <= 0)
            {
                items.Remove(item);
            }

            OnInventoryChanged?.Invoke();
            Debug.Log(item.itemName + " " + amount + "개 사용.");
        }
    }

    // [추가] 특정 아이템의 보유 개수를 반환하는 함수 (FertilizerPopupUI에서 사용)
    public int GetItemCount(ItemData item)
    {
        if (items.ContainsKey(item))
        {
            return items[item];
        }
        return 0;
    }

    // [추가] 1. 저장할 때: 현재 인벤토리를 리스트로 포장해서 DBManager에게 줌
    public List<InvenData> GetInventorySaveData()
    {
        List<InvenData> saveDataList = new List<InvenData>();

        foreach (KeyValuePair<ItemData, int> pair in items)
        {
            // pair.Key.name은 파일 이름(예: "PotatoSeed"), pair.Value는 개수
            saveDataList.Add(new InvenData(pair.Key.name, pair.Value));
        }

        return saveDataList;
    }

    // [추가] 2. 불러올 때: DB에서 받은 리스트로 인벤토리 복구
    public void LoadInventoryData(List<InvenData> loadedList)
    {
        items.Clear(); // 싹 비우고

        foreach (var data in loadedList)
        {
            // 이름으로 아이템 원본 데이터 찾기
            ItemData foundItem = FindItemDataByName(data.itemId);

            if (foundItem != null)
            {
                items.Add(foundItem, data.count);
            }
        }

        OnInventoryChanged?.Invoke(); // UI 갱신!
        Debug.Log("인벤토리 복구 완료!");
    }

    [Header("★ 게임의 모든 아이템을 여기에 등록하세요!")]
    public List<ItemData> allGameItems = new List<ItemData>(); // 전체 아이템 도감

    private ItemData FindItemDataByName(string name)
    {
        // 아까 만든 리스트(allGameItems)를 뒤져서 이름 같은 애를 찾아냄
        return allGameItems.Find(x => x.name == name);
    }
}