using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    // (기존 UI 그리기 함수들은 생략 - 그대로 두세요)
    public void SelectSlot(ItemSlot slot) { if (selectedSlot != null) selectedSlot.SetSelected(false); if (slot.item != null) { selectedItem = slot.item; selectedSlot = slot; selectedSlot.SetSelected(true); } }
    public void ClearSelection() { if (selectedSlot != null) selectedSlot.SetSelected(false); selectedItem = null; selectedSlot = null; }
    public void SetCategory(string category) { currentCategory = category; ClearSelection(); RedrawInventory(); }
    private void RedrawInventory() { Dictionary<ItemData, int> allItems = InventoryManager.Instance.items; int i = 0; foreach (KeyValuePair<ItemData, int> itemPair in allItems) { if (currentCategory == "All" || itemPair.Key.itemCategory == currentCategory) { if (i < inventorySlots.Count) { inventorySlots[i].gameObject.SetActive(true); inventorySlots[i].SetSlot(itemPair.Key, itemPair.Value); if (selectedSlot == inventorySlots[i]) selectedSlot.SetSelected(true); i++; } } } for (int j = i; j < inventorySlots.Count; j++) { inventorySlots[j].ClearSlot(); inventorySlots[j].gameObject.SetActive(false); } }


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

            // 성공 팝업
            OpenSuccessPopup(newItem);
        }
        else
        {
            // [CASE B] 실패 (레시피가 없었음) -> 돈과 재료는 이미 날아감
            UIManager.Instance.ShowAlertPopup("아무런 반응이 없습니다...\n 재료가 모두 사라졌습니다.");
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