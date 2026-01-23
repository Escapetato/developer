using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SeedPopupUI : MonoBehaviour 
{
    public Transform buttonContainer;
    public GameObject seedButtonPrefab;
    private Field currentField;
    private List<GameObject> spawnedButtons = new List<GameObject>();

    private void ClearButtons()
    {
        foreach (var b in spawnedButtons) Destroy(b);
        spawnedButtons.Clear();
    }

    public void RefreshButtons(Field field)
    {
        currentField = field;
        ClearButtons();

        var dict = InventoryManager.Instance.items;
        
        foreach (var kv in dict)
        {
            ItemData item = kv.Key; 
            int count = kv.Value;

            // 씨앗 카테고리가 아니면 생성하지 않음
            if (item.itemCategory != "Seed") continue;

            GameObject btn = Instantiate(seedButtonPrefab, buttonContainer);
            spawnedButtons.Add(btn);

            // UI 요소 연결
            Transform icon = btn.transform.Find("Icon");
            Transform nameText = btn.transform.Find("Name");
            Transform countText = btn.transform.Find("Count");

            if (icon != null) icon.GetComponent<Image>().sprite = item.itemIcon;
            if (nameText != null) nameText.GetComponent<TextMeshProUGUI>().text = item.itemName;
            if (countText != null) countText.GetComponent<TextMeshProUGUI>().text = count.ToString();

            Button btnComp = btn.GetComponent<Button>();
            if (btnComp != null)
            {
                // 클릭 리스너 설정
                btnComp.onClick.RemoveAllListeners();
                btnComp.onClick.AddListener(() => OnSeedClicked(item));
            }
        }
    }

    // 씨앗 클릭 시 실행되는 핵심 로직
    private void OnSeedClicked(ItemData seed)
    {
        if (currentField == null) return;

        // 1. 인벤토리 개수 확인 
        if (InventoryManager.Instance.items.ContainsKey(seed) && InventoryManager.Instance.items[seed] > 0)
        {
            // 인벤토리에서 일단 선택한 씨앗(또는 랜덤 씨앗) 1개 차감
            InventoryManager.Instance.RemoveItem(seed, 1);

            ItemData seedToPlant = seed; // 실제로 밭에 전달될 데이터

            bool israndom = false;
            // 2. 랜덤 씨앗 추첨 로직
            if (seed.itemName == "랜덤 씨앗")
            {
                israndom = true;
                List<ItemData> realSeeds = new List<ItemData>();

                // StoreDB를 뒤져서 랜덤 씨앗 후보군(실제 씨앗들)을 만듭니다.
                if (StoreUI.Instance != null && StoreUI.Instance.storeDB != null)
                {
                    foreach (var item in StoreUI.Instance.storeDB.itemsForSale)
                    {
                        // 조건: 카테고리가 씨앗이고, 이름이 "랜덤 씨앗"이 아니며, 잠금상태(?)가 아닌 것
                        if (item.itemCategory == "Seed" && item.itemName != "랜덤 씨앗" && !item.itemName.Contains("?"))
                        {
                            realSeeds.Add(item);
                        }
                    }
                }

                // 후보가 있다면 그 중 하나를 랜덤으로 선택
                if (realSeeds.Count > 0)
                {
                    int randIndex = Random.Range(0, realSeeds.Count);
                    seedToPlant = realSeeds[randIndex];

                    // 어떤 씨앗이 당첨되었는지 알림창 띄우기
                    Debug.Log($"{seedToPlant.itemName}이(가) 심겼습니다!");
                }
            }

            // 3. 밭에 최종 결정된 씨앗 데이터를 전달
            currentField.Plant(seedToPlant, israndom);

            // 팝업 닫기
            UIManager.Instance.CloseAllPopups();
        }
        else 
        {
            Debug.Log("씨앗이 부족합니다.");
        }
    }
}