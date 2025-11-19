using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // [필수]

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Popups")]
    public GameObject inventoryPopup;
    public GameObject researchLabPopup;
    public GameObject storePopup;

    [Header("Alert Popup")]
    public GameObject alertPopup;
    // [변경] Text -> TextMeshProUGUI
    public TextMeshProUGUI alertMessageText;
    public Button alertCloseButton;

    [Header("Item Acquired Popup")]
    public GameObject itemAcquiredPopup;
    public Image itemAcquiredIcon;
    // [변경] Text -> TextMeshProUGUI
    public TextMeshProUGUI itemAcquiredNameText;
    public Button itemAcquiredConfirmButton;

    [Header("Farm Popups")]
    public GameObject seedPopup;
    private Field currentField;

    [Header("Main UI Elements")]
    public RectTransform poingBarRect;
    public Transform poingBarOriginalParent;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (poingBarRect != null)
        {
            poingBarOriginalParent = poingBarRect.parent;
        }
    }

    void Start()
    {
        CloseAllPopups();
        if (alertPopup != null) alertPopup.SetActive(false);
        if (itemAcquiredPopup != null) itemAcquiredPopup.SetActive(false);
        if (seedPopup != null) seedPopup.SetActive(false);
    }

    public void CloseAllPopups()
    {
        if (inventoryPopup != null) inventoryPopup.SetActive(false);
        if (researchLabPopup != null) researchLabPopup.SetActive(false);
        if (seedPopup != null) seedPopup.SetActive(false);
        if (storePopup != null) storePopup.SetActive(false);

        ResetPoingUIPosition();
    }

    public void ShowAlertPopup(string message)
    {
        if (alertMessageText != null) alertMessageText.text = message;

        alertCloseButton.onClick.RemoveAllListeners();
        alertCloseButton.onClick.AddListener(() => {
            alertPopup.SetActive(false);
        });

        alertPopup.SetActive(true);
    }

    public void ShowItemAcquiredPopup(ItemData item)
    {
        if (itemAcquiredIcon != null)
        {
            itemAcquiredIcon.sprite = item.itemIcon;
            itemAcquiredIcon.color = Color.white;
        }

        if (itemAcquiredNameText != null)
        {
            itemAcquiredNameText.text = item.itemName + " (획득)";
        }

        itemAcquiredConfirmButton.onClick.RemoveAllListeners();
        itemAcquiredConfirmButton.onClick.AddListener(() => {
            InventoryManager.Instance.AddItem(item, 1);
            itemAcquiredPopup.SetActive(false);
        });

        itemAcquiredPopup.SetActive(true);
    }

    public void OpenInventoryPopup()
    {
        CloseAllPopups();
        if (inventoryPopup != null)
        {
            inventoryPopup.SetActive(true);
            MovePoingUIToPopup(inventoryPopup.transform);
        }
    }

    public void OpenResearchLabPopup()
    {
        CloseAllPopups();
        if (researchLabPopup != null) researchLabPopup.SetActive(true);
    }

    public void OpenStorePopup()
    {
        CloseAllPopups();
        if (storePopup != null)
        {
            storePopup.SetActive(true);
            MovePoingUIToPopup(storePopup.transform);
        }
    }

    public void OpenSeedPopup(Field field)
    {
        CloseAllPopups();
        currentField = field;
        if (seedPopup != null)
        {
            seedPopup.SetActive(true);
            // SeedPopupUI가 없는 경우를 대비한 안전장치
            var popupUI = seedPopup.GetComponent<SeedPopupUI>();
            if (popupUI != null) popupUI.RefreshButtons(field);
        }
    }

    public Field GetCurrentField()
    {
        return currentField;
    }

    private void MovePoingUIToPopup(Transform targetParent)
    {
        if (poingBarRect == null || targetParent == null) return;

        poingBarRect.SetParent(targetParent);
        poingBarRect.anchorMin = new Vector2(1, 1);
        poingBarRect.anchorMax = new Vector2(1, 1);
        poingBarRect.pivot = new Vector2(1, 1);
        poingBarRect.anchoredPosition = new Vector2(-50, -50);
        poingBarRect.localScale = Vector3.one;
    }

    private void ResetPoingUIPosition()
    {
        if (poingBarRect == null || poingBarOriginalParent == null) return;

        poingBarRect.SetParent(poingBarOriginalParent);
        poingBarRect.anchorMin = new Vector2(0, 1);
        poingBarRect.anchorMax = new Vector2(0, 1);
        poingBarRect.pivot = new Vector2(0, 1);
        poingBarRect.anchoredPosition = new Vector2(50, -50);
        poingBarRect.localScale = Vector3.one;
    }
}