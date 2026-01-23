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

    private void OnSeedClicked(ItemData seed)
    {
        if (currentField == null) return;

        // 인벤토리에 있는지 확인
        if (InventoryManager.Instance.items.ContainsKey(seed) && InventoryManager.Instance.items[seed] > 0)
        {
            // 1. 인벤토리에서 클릭한 씨앗 1개 사용 (차감)
            InventoryManager.Instance.RemoveItem(seed, 1);

            ItemData seedToPlant = seed; // 실제로 심게 될 씨앗 (기본값: 클릭한 씨앗)
            bool isRandomCheck = false;  // Field에 전달할 랜덤 여부 플래그

            // 2. 만약 이름이 "랜덤 씨앗"이라면 -> 진짜 씨앗 중 하나로 변신시킴!
            if (seed.itemName == "랜덤 씨앗")
            {
                isRandomCheck = true; // 랜덤 씨앗임을 표시
                List<ItemData> realSeeds = new List<ItemData>();

                if (StoreUI.Instance != null && StoreUI.Instance.storeDB != null)
                {
                    foreach (var item in StoreUI.Instance.storeDB.itemsForSale)
                    {
                        if (item.itemCategory == "Seed" && item.itemName != "랜덤 씨앗" && item.itemName != "???")
                        {
                            realSeeds.Add(item);
                        }
                    }
                }

                // 후보 중에서 하나 랜덤 뽑기
                if (realSeeds.Count > 0)
                {
                    int randIndex = Random.Range(0, realSeeds.Count);
                    seedToPlant = realSeeds[randIndex]; // 당첨된 씨앗으로 교체!
                }
            }

            // 3. 결정된 씨앗(seedToPlant)과 랜덤 여부(isRandomCheck)를 밭에 심음
            currentField.Plant(seedToPlant, isRandomCheck);

            UIManager.Instance.CloseAllPopups();
        }
        else
        {
            Debug.Log("씨앗이 없습니다.");
        }
    }
}