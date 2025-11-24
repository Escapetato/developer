using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ItemData의 SeedType Enum이 이 파일이나 접근 가능한 곳에 선언되어 있어야 합니다.
// public enum SeedType { None, Fruit, Grain, Vegetable } 
// 이 코드가 ItemData.cs 파일 외부에 있다면, 이곳에 다시 선언해 줍니다.

public class SeedPopupUI : MonoBehaviour
{
    private Field currentField;

    // [삭제 대상] buttonContainer는 아래 세부 컨테이너로 대체되므로, 제거하거나 사용하지 않아야 합니다.
    // public GameObject buttonContainer; 

    // 카테고리별 UI 요소
    [Header("Category UI Management")]
    public Button fruitTabButton;         // 과일 탭 버튼
    public Button grainTabButton;         // 곡식 탭 버튼
    public Button vegetableTabButton;     // 야채 탭 버튼

    public GameObject fruitScrollView;    // 과일 스크롤 뷰 (전체 GameObject)
    public GameObject grainScrollView;    // 곡식 스크롤 뷰
    public GameObject vegetableScrollView;// 야채 스크롤 뷰

    // 각 스크롤뷰 내부에 있는 버튼 컨테이너 (버튼이 생성될 부모)
    public Transform fruitButtonContainer;
    public Transform grainButtonContainer;
    public Transform vegetableButtonContainer;

    public GameObject seedButtonPrefab;

    // 전체 생성된 버튼을 관리할 딕셔너리 또는 리스트
    private List<GameObject> spawnedButtons = new List<GameObject>();

    // 현재 활성화된 카테고리 추적
    private string currentCategory = "Fruit"; // 기본값 설정
    
    // --- 추가 및 수정된 함수 시작 ---

    private void Awake()
    {
        // 탭 버튼 리스너 연결
        fruitTabButton.onClick.AddListener(() => SetActiveCategory("Fruit"));
        grainTabButton.onClick.AddListener(() => SetActiveCategory("Grain"));
        vegetableTabButton.onClick.AddListener(() => SetActiveCategory("Vegetable"));
        
        // 팝업이 처음 열릴 때 기본 카테고리 설정 (ClearButtons와 통합됨)
    }
    
    private void ClearButtons()
    {
        // 버튼이 여러 컨테이너에 나뉘어 생성되므로, 모든 spawnedButtons 리스트의 요소를 제거합니다.
        foreach (var b in spawnedButtons) Destroy(b);
        spawnedButtons.Clear();
    }

    private void SetActiveCategory(string category)
    {
        currentCategory = category;
        
        // 모든 스크롤 뷰를 비활성화하고 현재 카테고리 스크롤 뷰만 활성화
        fruitScrollView.SetActive(category == "Fruit");
        grainScrollView.SetActive(category == "Grain");
        vegetableScrollView.SetActive(category == "Vegetable");
    }

    private Transform GetButtonContainer(string category)
    {
        // ItemData의 seedCategory (Enum)를 ToString()으로 변환한 문자열을 받습니다.
        return category switch
        {
            "Fruit" => fruitButtonContainer,
            "Grain" => grainButtonContainer,
            "Vegetable" => vegetableButtonContainer,
            _ => null
        };
    }

    // [삭제] GetSeedType 함수는 ItemData.seedCategory 필드를 사용하도록 대체됩니다.
    // private string GetSeedType(ItemData item) { ... } 


    // UIManager가 호출할 수 있도록 public으로 변경
    public void RefreshButtons(Field field)
    {
        currentField = field;
        ClearButtons();
        
        // [추가] Refresh 시 기본 탭 활성화 (Awake가 아닌 팝업 활성화 시점에서 호출하는 것이 좋습니다)
        SetActiveCategory(currentCategory); 

        var dict = InventoryManager.Instance.items;
        Debug.Log("총 아이템 종류: " + dict.Count);

        foreach (var kv in dict)
        {
            ItemData item = kv.Key; 
            int count = kv.Value;

            // 아이템 카테고리가 "Seed"가 아니면 건너뛰기
            if (item.itemCategory != "Seed")
            {
                continue;
            }
            
            // 1. ItemData에서 SeedType을 직접 가져옵니다.
            // (ItemData에 seedCategory 필드가 있다는 가정 하에)
            string seedType = item.seedCategory.ToString(); 

            // 2. 이 seedType을 기반으로 적절한 컨테이너를 찾습니다.
            Transform container = GetButtonContainer(seedType);
            
            // [수정] container가 유효한지 확인하고, 해당 container에 버튼을 생성합니다.
            if (container == null) 
            {
                 Debug.LogWarning($"아이템 '{item.itemName}'의 SeedType '{seedType}'에 맞는 컨테이너를 찾을 수 없습니다.");
                 continue;
            }

            // [오류 수정: buttonContainer 대신 container 변수 사용]
            GameObject btn = Instantiate(seedButtonPrefab, container);
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