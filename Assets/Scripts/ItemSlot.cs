using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public enum SlotType { Inventory, Material, Result, Lab_Inventory, Store }

public class ItemSlot : MonoBehaviour, IPointerClickHandler
{
    // ★ [수정] 이 변수는 이제 안 씁니다 (삭제함)
    // public Image icon; 

    [Header("Core Info")]
    public ItemData item;
    public SlotType slotType;

    [Header("UI Components")]
    public Image itemIconDisplay;
    public Image slotBackground;
    public TextMeshProUGUI detailText;   // 가격 or 수량

    // ★ 아이템 이름을 표시할 텍스트
    public TextMeshProUGUI itemNameText;

    public Image selectionBorder;

    [Header("Selection Sprites")]
    public Sprite selectedSprite;

    // [수정 포인트 1] 배경 이미지가 자꾸 사라져서, 아예 직접 넣을 수 있게 public으로 만들었습니다.
    // 기존: private Sprite defaultSprite;
    public Sprite defaultBackground;

    [Header("Settings")]
    // (연구실) 비어 있을 때 보여줄 기본 이미지 (+ 모양)
    public Sprite defaultIcon;

    // 테스트용
    void Start()
    {
        // 만약 인스펙터에 아이템을 넣어 놨다면?
        if (item != null)
        {
            int realCount = 1; // 기본은 1개지만

            // 창고(InventoryManager)가 있으면 진짜 개수 물어보기
            if (InventoryManager.Instance != null && InventoryManager.Instance.items.ContainsKey(item))
            {
                realCount = InventoryManager.Instance.items[item];
            }

            // 진짜 개수로 설정
            SetSlot(item, realCount);
        }
    }
    void Awake()
    {
        if (selectionBorder == null && transform.Find("SelectionBorder") != null)
            selectionBorder = transform.Find("SelectionBorder").GetComponent<Image>();

        if (slotBackground == null)
            slotBackground = GetComponent<Image>();

        // [수정 포인트 3] 실수로 인스펙터에 배경을 안 넣었을 때를 대비한 안전장치
        if (defaultBackground == null && slotBackground != null)
            defaultBackground = slotBackground.sprite;

        if (selectionBorder != null)
            selectionBorder.gameObject.SetActive(false);
    }

    public void SetSlot(ItemData newItem, int quantity = 1)
    {
        item = newItem;

        // 1. 아이콘 설정 (수정됨)
        if (itemIconDisplay != null)
        {
            itemIconDisplay.sprite = newItem.itemIcon;
            itemIconDisplay.color = Color.white;
            itemIconDisplay.gameObject.SetActive(true); // 아이템이 들어왔으니 켜기
        }

        // 2. ★ [추가] 이름 표시 (이름 텍스트가 연결되어 있다면)
        if (itemNameText != null)
        {
            itemNameText.text = newItem.itemName;
            itemNameText.gameObject.SetActive(true);
        }

        // 3. 하단 텍스트 (가격 or 수량)
        if (detailText != null)
        {
            switch (slotType)
            {
                case SlotType.Inventory:
                case SlotType.Lab_Inventory:
                    detailText.text = "" + quantity.ToString();
                    detailText.gameObject.SetActive(true);
                    break;

                case SlotType.Store:
                    detailText.text = newItem.price.ToString();
                    detailText.gameObject.SetActive(true);
                    break;

                default:
                    detailText.text = "";
                    detailText.gameObject.SetActive(false);
                    break;
            }
        }
    }

    public void ClearSlot()
    {
        item = null;

        // 아이콘 처리 로직 변경
        if (itemIconDisplay != null)
        {
            // 만약 기본 아이콘(+모양)이 설정되어 있다면?
            if (defaultIcon != null)
            {
                itemIconDisplay.sprite = defaultIcon; // + 그림으로 교체
                itemIconDisplay.color = Color.white;  // 보이게 설정
                itemIconDisplay.gameObject.SetActive(true); // 오브젝트를 켜야 보임
            }
            else
            {
                // 설정된 게 없다면 (인벤토리 등) -> 그냥 숨김
                itemIconDisplay.sprite = null;
                itemIconDisplay.gameObject.SetActive(false);
            }
        }

        if (detailText != null) detailText.gameObject.SetActive(false);
        if (itemNameText != null) itemNameText.gameObject.SetActive(false);
        if (selectionBorder != null) selectionBorder.gameObject.SetActive(false);

        // [수정 포인트 4] 배경 복구 시 defaultBackground 사용
        if (slotBackground != null) slotBackground.sprite = defaultBackground;
    }

    public void SetSelected(bool isSelected)
    {
        if (selectionBorder != null) selectionBorder.gameObject.SetActive(isSelected);

        // [수정 포인트 5] 선택 해제 시에도 defaultBackground 사용
        if (slotBackground != null && selectedSprite != null)
            slotBackground.sprite = isSelected ? selectedSprite : defaultBackground;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (slotType == SlotType.Inventory && eventData.clickCount == 1) InventoryUI.Instance.SelectSlot(this);
        else if (slotType == SlotType.Store && eventData.clickCount == 1) StoreUI.Instance.SelectSlot(this);
        else if (slotType == SlotType.Lab_Inventory)
        {
            if (eventData.clickCount == 1) ResearchLab.Instance.SelectSlot(this);
            else if (eventData.clickCount == 2 && this.item != null)
            {
                ResearchLab lab = ResearchLab.Instance;
                if (this.item.itemCategory == "Crop" && lab.materialSlot.item == null) { lab.materialSlot.SetSlot(this.item, 1); InventoryManager.Instance.RemoveItem(this.item, 1); lab.ClearSelection(); }
                else if (this.item.itemCategory == "Potion" && lab.potionSlot.item == null) { lab.potionSlot.SetSlot(this.item, 1); InventoryManager.Instance.RemoveItem(this.item, 1); lab.ClearSelection(); }
            }
        }
        else if (slotType == SlotType.Material && this.item != null) { InventoryManager.Instance.AddItem(this.item, 1); this.ClearSlot(); ResearchLab.Instance.ClearSelection(); }
        else if (slotType == SlotType.Result && this.item != null) { InventoryManager.Instance.AddItem(this.item, 1); this.ClearSlot(); }
    }
}