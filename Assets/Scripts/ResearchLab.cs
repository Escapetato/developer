using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 임시
[System.Serializable]
public class CategoryTab
{
    public string categoryName;       // 예: "Crop", "Potion" (코드랑 똑같이 적어야 함)
    public Button buttonObj;          // 버튼 컴포넌트
    public Image buttonBackground;    // 배경 이미지를 바꿀 타겟
    public TextMeshProUGUI buttonText;// 텍스트 색을 바꿀 타겟
}

public class ResearchLab : MonoBehaviour
{
    public static ResearchLab Instance { get; private set; }

    [Header("1. Evolution Data")]
    public List<EvolutionRecipe> allRecipes;

    // ★ [추가] 레시피가 없는 조합일 때 들어가는 기본 비용
    public int defaultFailureCost = 100;

    [Header("2. Mixer Slots")]
    public ItemSlot materialSlot;
    public ItemSlot potionSlot;

    [Header("3. Inventory Grid")]
    public Transform slotParent;
    private List<ItemSlot> inventorySlots;

    [Header("4. Confirm Popup")]
    public GameObject confirmPopupObject;
    public TextMeshProUGUI confirmPopupText;

    [Header("5. Success Popup")]
    public GameObject successPopupObject;
    public Image successItemIcon;
    public TextMeshProUGUI successNameText;
    public TextMeshProUGUI successMessageText;

    [Header("6. Category Tabs UI")] // 새로 추가할 부분
    public List<CategoryTab> categoryTabs; // 인스펙터에서 버튼들 등록
    public Sprite tabSelectedSprite;  // 선택됐을 때 배경 그림
    public Sprite tabNormalSprite;    // 평소 배경 그림

    public Color tabSelectedColor = Color.white; // 선택됐을 때 글자 색
    public Color tabNormalColor = Color.gray;    // 평소 글자 색

    private EvolutionRecipe pendingRecipe; // 찾은 레시피 (없으면 null)
    private int currentEvolutionCost = 0;  // ★ [추가] 이번 진화에 들어갈 비용

    private string currentCategory = "Crop";
    public ItemData selectedItem { get; private set; }
    public ItemSlot selectedSlot { get; private set; }


    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        inventorySlots = new List<ItemSlot>();
        if (slotParent != null)
            slotParent.GetComponentsInChildren<ItemSlot>(inventorySlots);

        if (confirmPopupObject != null) confirmPopupObject.SetActive(false);
        if (successPopupObject != null) successPopupObject.SetActive(false);
    }

    void OnEnable() { InventoryManager.Instance.OnInventoryChanged += RedrawInventory; SetCategory("Crop"); }
    void OnDisable() { InventoryManager.Instance.OnInventoryChanged -= RedrawInventory; }

   
    public void SelectSlot(ItemSlot slot) { if (selectedSlot != null) selectedSlot.SetSelected(false); if (slot.item != null) { selectedItem = slot.item; selectedSlot = slot; selectedSlot.SetSelected(true); } }
    public void ClearSelection() { if (selectedSlot != null) selectedSlot.SetSelected(false); selectedItem = null; selectedSlot = null; }
    // [2] 카테고리 변경 함수 수정
    public void SetCategory(string category)
    {
        currentCategory = category;
        ClearSelection();
        RedrawInventory();

        // ★ 버튼 UI 업데이트 로직 추가
        UpdateTabUI();
    }
    private void RedrawInventory() { Dictionary<ItemData, int> allItems = InventoryManager.Instance.items; int i = 0; foreach (KeyValuePair<ItemData, int> itemPair in allItems) { if (currentCategory == "All" || itemPair.Key.itemCategory == currentCategory) { if (i < inventorySlots.Count) { inventorySlots[i].gameObject.SetActive(true); inventorySlots[i].SetSlot(itemPair.Key, itemPair.Value); if (selectedSlot == inventorySlots[i]) selectedSlot.SetSelected(true); i++; } } } for (int j = i; j < inventorySlots.Count; j++) { inventorySlots[j].ClearSlot(); inventorySlots[j].gameObject.SetActive(false); } }

    // ★ [3] 버튼 모양을 바꿔주는 함수 추가
    private void UpdateTabUI()
    {
        foreach (var tab in categoryTabs)
        {
            // 이 버튼이 현재 선택된 카테고리인지 확인
            bool isSelected = (tab.categoryName == currentCategory);

            // 1. 배경 이미지 변경
            if (tab.buttonBackground != null)
            {
                tab.buttonBackground.sprite = isSelected ? tabSelectedSprite : tabNormalSprite;
            }

            // 2. 텍스트 색상 변경 (혹은 텍스트 내용 변경)
            if (tab.buttonText != null)
            {
                tab.buttonText.color = isSelected ? tabSelectedColor : tabNormalColor;

                // 만약 텍스트 내용도 바꾸고 싶다면? (예: "작물" -> "작물(선택됨)")
                // tab.buttonText.text = isSelected ? $"[{tab.categoryName}]" : tab.categoryName;
            }

            // 3. 버튼 인터랙션 (선택된 건 클릭 안 되게 하려면)
            if (tab.buttonObj != null)
            {
                tab.buttonObj.interactable = !isSelected;
            }
        }
    }


    // [1] 진화 버튼 클릭
    public void OnEvolutionButtonClick()
    {
        if (materialSlot.item == null || potionSlot.item == null)
        {
            UIManager.Instance.ShowAlertPopup("재료가 부족합니다!");
            return;
        }

        ItemData inputMaterial = materialSlot.item;
        ItemData inputPotion = potionSlot.item;
        EvolutionRecipe foundRecipe = null;

        // 레시피 검색
        foreach (var recipe in allRecipes)
        {
            if (recipe.material == inputMaterial && recipe.potion == inputPotion)
            {
                foundRecipe = recipe;
                break;
            }
        }

        // ★ [변경] 레시피를 못 찾아도 팝업을 띄워야 함!
        pendingRecipe = foundRecipe; // null일 수도 있음

        // 비용 결정 (레시피 있으면 그 비용, 없으면 기본 실패 비용)
        if (foundRecipe != null)
            currentEvolutionCost = foundRecipe.evolutionCost;
        else
            currentEvolutionCost = defaultFailureCost;

        // 팝업 텍스트 설정
        if (confirmPopupText != null)
            confirmPopupText.text = $"{currentEvolutionCost} 포잉으로 진화하시겠습니까?";

        // 팝업 띄우기
        if (confirmPopupObject != null) confirmPopupObject.SetActive(true);
        else OnConfirmEvolution();
    }

    // [2] 결제 팝업에서 '네' 클릭
    public void OnConfirmEvolution()
    {
        if (confirmPopupObject != null) confirmPopupObject.SetActive(false);

        // 1. 돈 검사 (pendingRecipe가 null이어도 currentEvolutionCost로 검사)
        if (!PoingManager.Instance.HasEnoughPoing(currentEvolutionCost))
        {
            UIManager.Instance.ShowAlertPopup("포잉이 부족합니다!");
            return;
        }

        // === 시도 시작 (성공이든 실패든 공통 수행) ===

        // 2. 돈 차감
        PoingManager.Instance.DecreasePoing(currentEvolutionCost);

        // 3. 재료 삭제
        if (materialSlot.item != null) InventoryManager.Instance.RemoveItem(materialSlot.item, 1);
        if (potionSlot.item != null) InventoryManager.Instance.RemoveItem(potionSlot.item, 1);

        materialSlot.ClearSlot();
        potionSlot.ClearSlot();


        // === 결과 판정 ===

        if (pendingRecipe != null)
        {
            // [CASE A] 성공 (레시피가 있었음)
            ItemData newItem = pendingRecipe.resultItem;
            GameProgressionManager.Instance.UnlockItem(newItem);
            InventoryManager.Instance.AddItem(newItem, 1);

            SoundManager.Instance.PlaySFX("ev_success"); // 소리 추가

            // 성공 팝업
            OpenSuccessPopup(newItem);
        }
        else
        {
            // [CASE B] 실패 (레시피가 없었음) -> 돈과 재료는 이미 날아감
            UIManager.Instance.ShowAlertPopup("아무런 반응이 없습니다...\n 재료가 모두 사라졌습니다.");

            SoundManager.Instance.PlaySFX("ev_fail");
        }

        // 초기화
        pendingRecipe = null;
        currentEvolutionCost = 0;
    }

    public void OnCancelEvolution()
    {
        if (confirmPopupObject != null) confirmPopupObject.SetActive(false);
        pendingRecipe = null;
        currentEvolutionCost = 0;
    }

    private void OpenSuccessPopup(ItemData item)
    {
        if (successPopupObject != null)
        {
            successPopupObject.SetActive(true);
            if (successItemIcon != null) successItemIcon.sprite = item.itemIcon;
            if (successNameText != null) successNameText.text = item.itemName;
            if (successMessageText != null) successMessageText.text = "진화에 성공했습니다!";
        }
    }

    public void OnCloseSuccessPopup()
    {
        if (successPopupObject != null) successPopupObject.SetActive(false);
    }
}