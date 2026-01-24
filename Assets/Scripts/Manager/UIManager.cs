using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Popups (메인 팝업창들)")]
    public GameObject inventoryPopup;
    public GameObject researchLabPopup;
    public GameObject storePopup;
    public GameObject decoPopup;
    public GameObject collectionPopup;
    public GameObject questPopup;

    [Header("Alert Popup")]
    public GameObject alertPopup;
    public TextMeshProUGUI alertMessageText;
    public Button alertCloseButton;

    [Header("Confirm Popup")]
    public GameObject confirmPopup;
    public TextMeshProUGUI confirmText;
    public Button confirmYesButton;
    public Button confirmNoButton;

    [Header("Item Acquired Popup")]
    public GameObject itemAcquiredPopup;
    public Image itemAcquiredIcon;
    public TextMeshProUGUI itemAcquiredNameText;
    public Button itemAcquiredConfirmButton;

    [Header("Main UI Elements")]
    public RectTransform poingBarRect;
    public Transform poingBarOriginalParent;

    public GameObject mainToolUI;

    [Header("Farm Popups")]
    public GameObject seedPopup;
    public GameObject fertilizerPopup;
    public Button decoCloseButton;

    private Field currentField;
    private bool isSeedPopupOpen = false;

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
        if (questPopup != null) questPopup.SetActive(false);
        if (confirmPopup != null) confirmPopup.SetActive(false);

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayBGM("mainfarm");

        if (DBManager.Instance != null)
        {
            DBManager.Instance.ApplyFieldDataToScene();
        }
    }

    // ... (ShowConfirmPopup 등은 기존과 동일) ...
    public void ShowConfirmPopup(string message, Action onConfirm)
    {
        if (confirmPopup == null) return;
        confirmPopup.SetActive(true);
        if (confirmText != null) confirmText.text = message;

        if (confirmYesButton != null)
        {
            confirmYesButton.onClick.RemoveAllListeners();
            confirmYesButton.onClick.AddListener(() =>
            {
                onConfirm();
                confirmPopup.SetActive(false);
                SoundManager.Instance.PlaySFX("button");
            });
        }
        if (confirmNoButton != null)
        {
            confirmNoButton.onClick.RemoveAllListeners();
            confirmNoButton.onClick.AddListener(() =>
            {
                confirmPopup.SetActive(false);
                SoundManager.Instance.PlaySFX("button");
            });
        }
        SoundManager.Instance.PlaySFX("PopupOpen");
    }

    // ★★★ [핵심 수정] 팝업 닫기 함수
    public void CloseAllPopups(bool goToMain = true)
    {
        bool wasAnyPopupOpen = (inventoryPopup != null && inventoryPopup.activeSelf) ||
                               (researchLabPopup != null && researchLabPopup.activeSelf) ||
                               (storePopup != null && storePopup.activeSelf) ||
                               (decoPopup != null && decoPopup.activeSelf) ||
                               (collectionPopup != null && collectionPopup.activeSelf) ||
                               (fertilizerPopup != null && fertilizerPopup.activeSelf) ||
                               (questPopup != null && questPopup.activeSelf);

        // [수정] 메인 화면으로 갈 때만 사이드 메뉴를 '다시 켭니다' (SetVisible true)
        if (goToMain && SideMenuUI.Instance != null)
        {
            SideMenuUI.Instance.SetVisible(true);
        }

        if (wasAnyPopupOpen && goToMain)
        {
            SoundManager.Instance.PlayBGM("mainfarm");
            SoundManager.Instance.PlaySFX("back");
        }

        isSeedPopupOpen = false;

        if (fertilizerPopup != null) fertilizerPopup.SetActive(false);
        if (inventoryPopup != null) inventoryPopup.SetActive(false);
        if (researchLabPopup != null) researchLabPopup.SetActive(false);
        if (decoPopup != null) decoPopup.SetActive(false);
        if (seedPopup != null) seedPopup.SetActive(false);
        if (storePopup != null) storePopup.SetActive(false);
        if (collectionPopup != null) collectionPopup.SetActive(false);
        if (confirmPopup != null) confirmPopup.SetActive(false);
        if (questPopup != null) questPopup.SetActive(false); // 퀘스트 창 닫기 추가

        if (questPopup != null && questPopup.activeSelf)
        {
            CameraController cameraController = FindObjectOfType<CameraController>();
            if (cameraController != null) cameraController.RestoreCameraState();
        }

        ResetPoingUIPosition();

        if (goToMain && mainToolUI != null)
        {
            mainToolUI.SetActive(true);
        }
    }

    // ... (ShowAlertPopup, ShowItemAcquiredPopup 등은 기존과 동일) ...
    public void ShowAlertPopup(string message)
    {
        if (alertMessageText != null) alertMessageText.text = message;
        alertCloseButton.onClick.RemoveAllListeners();
        alertCloseButton.onClick.AddListener(() => { alertPopup.SetActive(false); });
        alertPopup.SetActive(true);
    }

    public void ShowItemAcquiredPopup(ItemData item)
    {
        if (itemAcquiredIcon != null)
        {
            itemAcquiredIcon.sprite = item.itemIcon;
            itemAcquiredIcon.color = Color.white;
        }
        if (itemAcquiredNameText != null) itemAcquiredNameText.text = item.itemName + " (획득)";
        itemAcquiredConfirmButton.onClick.RemoveAllListeners();
        itemAcquiredConfirmButton.onClick.AddListener(() => { itemAcquiredPopup.SetActive(false); });
        itemAcquiredPopup.SetActive(true);
    }

    // --- 팝업 열기 함수들 ---

    public void OpenQuestPopup()
    {
        CloseAllPopups(false);
        // [수정] CloseMenu() 대신 SetVisible(false) 사용
        if (SideMenuUI.Instance != null) SideMenuUI.Instance.SetVisible(false);

        if (questPopup != null)
        {
            CameraController cameraController = FindObjectOfType<CameraController>();
            if (cameraController != null)
            {
                cameraController.SaveCameraState();
                cameraController.ResetCameraForQuest();
            }
            questPopup.SetActive(true);
            if (poingBarRect != null) poingBarRect.gameObject.SetActive(false);
            SoundManager.Instance.PlaySFX("enter");
        }
    }

    public void OpenInventoryPopup()
    {
        CloseAllPopups(false);
        // [수정] CloseMenu() 대신 SetVisible(false) 사용
        if (SideMenuUI.Instance != null) SideMenuUI.Instance.SetVisible(false);

        if (inventoryPopup != null)
        {
            inventoryPopup.SetActive(true);
            MovePoingUIToPopup(inventoryPopup.transform);
            SoundManager.Instance.PlaySFX("enter");
        }
    }

    public void OpenResearchLabPopup()
    {
        CloseAllPopups(false);
        // [수정] CloseMenu() 대신 SetVisible(false) 사용
        if (SideMenuUI.Instance != null) SideMenuUI.Instance.SetVisible(false);

        if (researchLabPopup != null)
        {
            researchLabPopup.SetActive(true);
            MovePoingUIToPopup(researchLabPopup.transform);
            SoundManager.Instance.PlayBGM("lab");
            SoundManager.Instance.PlaySFX("enter");
        }
    }

    public void OpenStorePopup()
    {
        CloseAllPopups(false);
        // [수정] CloseMenu() 대신 SetVisible(false) 사용
        if (SideMenuUI.Instance != null) SideMenuUI.Instance.SetVisible(false);

        if (storePopup != null)
        {
            storePopup.SetActive(true);
            MovePoingUIToPopup(storePopup.transform);
            SoundManager.Instance.PlayBGM("store");
            SoundManager.Instance.PlaySFX("enter");
        }
    }

    public void OpendecoPopup()

    {

        CloseAllPopups();

        if (SideMenuUI.Instance != null) SideMenuUI.Instance.CloseMenu();


        if (decoPopup != null)
        {
            decoPopup.SetActive(true);
            MovePoingUIToPopup(storePopup.transform);
            SoundManager.Instance.PlaySFX("enter");
        }
        decoCloseButton.onClick.RemoveAllListeners();

        // '확인' 버튼 누르면 팝업 꺼지도록 설정

        decoCloseButton.onClick.AddListener(() =>
        {
            SoundManager.Instance.PlaySFX("close");

            // 단순히 SetActive(false)만 하지 말고, 

            // CloseAllPopups()를 호출해야 포잉 바가 원래 자리로 돌아옵니다.

            CloseAllPopups();

        });

    }

    public void OpenCollectionPopup()
    {
        CloseAllPopups(false);

        if (SideMenuUI.Instance != null)
        {
            // SideMenuUI.Instance.SetVisible(false);  <-- 이거 주석 처리 하거나 삭제

            // 대신 메뉴 목록만 깔끔하게 접어줍니다. (화살표는 남음)
            SideMenuUI.Instance.CloseMenu();
        }
        // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

        if (collectionPopup != null)
        {
            collectionPopup.SetActive(true);
            SoundManager.Instance.PlaySFX("open");
        }
    }

    public void OpenSeedPopup(Field field)
    {
        CloseAllPopups();
        currentField = field;
        if (seedPopup != null)
        {
            seedPopup.SetActive(true);
            seedPopup.GetComponent<SeedPopupUI>()?.RefreshButtons(field);
            SoundManager.Instance.PlaySFX("open");
        }
        isSeedPopupOpen = true;
    }

    public void OpenFertilizerPopup(Field field)
    {
        CloseAllPopups();
        currentField = field;

        if (fertilizerPopup != null)
        {
            fertilizerPopup.SetActive(true);
            FertilizerPopupUI fertilizerUI = fertilizerPopup.GetComponent<FertilizerPopupUI>();
            if (fertilizerUI != null) fertilizerUI.Show(field);
            SoundManager.Instance.PlaySFX("open");
        }
    }

    public Field GetCurrentField() { return currentField; }

    private void MovePoingUIToPopup(Transform targetParent)
    {
        if (poingBarRect == null || targetParent == null) return;
        poingBarRect.SetParent(targetParent);
        poingBarRect.anchorMin = new Vector2(1, 1);
        poingBarRect.anchorMax = new Vector2(1, 1);
        poingBarRect.pivot = new Vector2(1, 1);
        poingBarRect.anchoredPosition = new Vector2(-200, -50);
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
        poingBarRect.gameObject.SetActive(true);
    }

    public void OpenCollectionPanel(ItemData targetItem = null)
    {
        CloseAllPopups();
        if (researchLabPopup != null) researchLabPopup.SetActive(false);
        if (collectionPopup != null) collectionPopup.SetActive(true);
        if (targetItem != null) CollectionUI.Instance.ShowItem(targetItem);
    }

    void Update()
    {
        // 1. [툴 UI 자동 감지 로직]
        if (mainToolUI != null)
        {
            bool isAnyPopupOpen =
                (inventoryPopup != null && inventoryPopup.activeSelf) ||
                (researchLabPopup != null && researchLabPopup.activeSelf) ||
                (storePopup != null && storePopup.activeSelf) ||
                (collectionPopup != null && collectionPopup.activeSelf) ||
                (questPopup != null && questPopup.activeSelf) ||
                (seedPopup != null && seedPopup.activeSelf) ||
                (decoPopup != null && decoPopup.activeSelf);

            if (mainToolUI.activeSelf == isAnyPopupOpen)
            {
                mainToolUI.SetActive(!isAnyPopupOpen);
            }
        }

        // 2. [씨앗 팝업 닫기 로직]
        if (isSeedPopupOpen)
        {
            if (Input.GetAxis("Mouse ScrollWheel") != 0)
            {
                CloseAllPopups();
                return;
            }
            if (Input.GetMouseButtonDown(0))
            {
                if (IsPointerOverUI()) return; // 함수 분리 사용

                Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Collider2D hitCollider = Physics2D.OverlapPoint(mousePos);

                if (hitCollider != null)
                {
                    Field clickedField = hitCollider.GetComponent<Field>();
                    if (clickedField == null)
                    {
                        SoundManager.Instance.PlaySFX("close");
                        CloseAllPopups();
                    }
                }
                else
                {
                    SoundManager.Instance.PlaySFX("close");
                    CloseAllPopups();
                }
            }
        }
    }

    public bool IsPointerOverUI()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return true;
        if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return true;
        return false;
    }
}