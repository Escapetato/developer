using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; 

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    // 인벤토리 데이터: (어떤 아이템, 몇 개)
    // ScriptableObject인 ItemData를 Key로 사용
    public Dictionary<ItemData, int> items = new Dictionary<ItemData, int>();

    // 인벤토리가 변경될 때 UI에 알려주는 부분
    public event Action OnInventoryChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 'T' 키를 누르면 green_apple 1개 추가
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (testItem != null)
            {
                AddItem(testItem, 1);
            }
        }

        if (Input.GetKeyDown(KeyCode.Y)) 
        {
            if (testPotion != null)
            {
                AddItem(testPotion, 1);
            }
        }
    }


    public ItemData testItem; // Inspector에 테스트용 아이템 연결
    public ItemData testPotion;


    // 아이템 추가 함수
    public void AddItem(ItemData item, int amount)
    {
        // 이미 아이템을 가지고 있는가?
        if (items.ContainsKey(item))
        {
            items[item] += amount; // 수량만 더함
        }
        // 새로 얻은 아이템인가?
        else
        {
            items.Add(item, amount); // 딕셔너리에 새로 추가
        }

        // 인벤토리 바뀌었다고 알려 줌
        OnInventoryChanged?.Invoke();
        Debug.Log(item.itemName + " " + amount + "개 추가. 총: " + items[item] + "개");
    }

    // 아이템 제거 함수
    public void RemoveItem(ItemData item, int amount)
    {
        if (items.ContainsKey(item))
        {
            items[item] -= amount;

            // 만약 0개가 되면, 딕셔너리에서 아예 삭제
            if (items[item] <= 0)
            {
                items.Remove(item);
            }

            OnInventoryChanged?.Invoke();
            Debug.Log(item.itemName + " " + amount + "개 사용.");
        }
    }
}