using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SeedPopupUI : MonoBehaviour // [수정] 이름
{
    public Transform buttonContainer;
    public GameObject seedButtonPrefab;
    private Field currentField;
    private List<GameObject> spawnedButtons = new List<GameObject>();

    // [삭제] Start(), Show(), Hide() 함수 삭제 (UIManager가 대신함)

    private void ClearButtons()
    {
        foreach (var b in spawnedButtons) Destroy(b);
        spawnedButtons.Clear();
    }

    // UIManager가 호출할 수 있도록 public으로 변경
    public void RefreshButtons(Field field)
    {
        currentField = field;
        ClearButtons();

// 'Seed_InventoryManager' 대신 'InventoryManager' 사용
var dict = InventoryManager.Instance.items;
Debug.Log("총 아이템 종류: " + dict.Count);

foreach (var kv in dict)
{
ItemData item = kv.Key; // [수정] SeedData -> ItemData
int count = kv.Value;

// 아이템 카테고리가 "Seed"가 아니면 건너뛰기
if (item.itemCategory != "Seed")
{
continue;
}

GameObject btn = Instantiate(seedButtonPrefab, buttonContainer);
spawnedButtons.Add(btn);

// [수정] ItemData의 변수명 사용
Transform icon = btn.transform.Find("Icon");
Transform name = btn.transform.Find("Name");
Transform cnt = btn.transform.Find("Count");

if (icon != null) icon.GetComponent<Image>().sprite = item.itemIcon; // icon
if (name != null) name.GetComponent<TextMeshProUGUI>().text = item.itemName; // itemName
if (cnt != null) cnt.GetComponent<TextMeshProUGUI>().text = count.ToString();

Button btnComp = btn.GetComponent<Button>();
if (btnComp != null)
btnComp.onClick.AddListener(() => OnSeedClicked(item)); // ItemData
else
Debug.LogError("Button 컴포넌트가 btn에 없음!");
}
}

// [수정] SeedData -> ItemData
private void OnSeedClicked(ItemData seed)
{
if (currentField == null) return;

// 'Seed_InventoryManager' 대신 'InventoryManager' 사용
if (InventoryManager.Instance.items.ContainsKey(seed) && InventoryManager.Instance.items[seed] > 0)
{
InventoryManager.Instance.RemoveItem(seed, 1); // UseSeed 대신 RemoveItem
currentField.Plant(seed); // Plant 함수는 ItemData를 받도록 수정 필요
UIManager.Instance.CloseAllPopups(); // [수정] UIManager가 닫도록 함
}
else Debug.Log("씨앗이 없습니다.");
}
}