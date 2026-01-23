using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

// 게임 내 모든 UI 팝업과 알림창을 중앙에서 관리하는 매니저 클래스
public class UIManager : MonoBehaviour
{
    // 싱글톤 패턴: 어디서든 UIManager.Instance로 접근 가능
    public static UIManager Instance { get; private set; }

    [Header("UI Popups (메인 팝업창들)")]
    // Inspector에서 각 팝업창(Panel) 오브젝트를 연결해야 함
    public GameObject inventoryPopup;   // 인벤토리 창
    public GameObject researchLabPopup; // 연구실 창
    public GameObject storePopup;       // 상점 창
    public GameObject collectionPopup; // 도감 창
    public GameObject questPopup;       // 퀘스트 창

    [Header("Alert Popup (단순 알림창)")]
    public GameObject alertPopup;       // 알림창 패널
    public TextMeshProUGUI alertMessageText; // [TMP] 알림 메시지 (예: "돈이 부족합니다")
    public Button alertCloseButton;     // 알림창 닫기(확인) 버튼

    [Header("Confirm Popup (질문 팝업)")]
    public GameObject confirmPopup;         // 팝업 패널
    public TextMeshProUGUI confirmText;     // 질문 텍스트
    public Button confirmYesButton;         // '네' 버튼
    public Button confirmNoButton;          // '아니오' 버튼

    [Header("Item Acquired Popup (아이템 획득 팝업)")]
    public GameObject itemAcquiredPopup;       // 획득 팝업 패널
    public Image itemAcquiredIcon;             // 획득한 아이템 아이콘
    public TextMeshProUGUI itemAcquiredNameText; // [TMP] 획득한 아이템 이름
    public Button itemAcquiredConfirmButton;   // 확인 버튼

    [Header("Main UI Elements (메인 화면 UI)")]
    public RectTransform poingBarRect;      // 포잉 바
    public Transform poingBarOriginalParent;

    // ▼▼▼ [추가] 툴 UI (부채꼴 메뉴) 연결용 변수 ▼▼▼
    public GameObject mainToolUI;

    [Header("Farm Popups")]
    public GameObject seedPopup;
    public GameObject fertilizerPopup;

    private Field currentField; // [추가] 씨앗을 심을 밭
    // private Field lastField = null;   // 마지막으로 클릭한 밭 (경고 때문에 잠깐 주석 처리!)
    private bool isSeedPopupOpen = false; // 시드 팝업 열림 여부

    void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 게임 시작 시 포잉 바의 원래 부모(위치)를 저장해둠
        // (나중에 팝업창 안으로 이동했다가 다시 돌아올 때 사용)
        if (poingBarRect != null)
        {
            poingBarOriginalParent = poingBarRect.parent;
        }
    }

    void Start()
    {
        CloseAllPopups();

        // 안전장치: 알림창들이 켜져 있다면 강제로 끔
        if (alertPopup != null) alertPopup.SetActive(false);
        if (itemAcquiredPopup != null) itemAcquiredPopup.SetActive(false);
        if (seedPopup != null) seedPopup.SetActive(false);
        if (questPopup != null) questPopup.SetActive(false);

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayBGM("mainfarm");
        else
            Debug.LogWarning("SoundManager.Instance is null (BGM skipped).");
        if (confirmPopup != null) confirmPopup.SetActive(false);

        SoundManager.Instance.PlayBGM("mainfarm");

        // ▼▼▼ [여기 추가!] 씬이 켜지자마자 저장된 밭 데이터를 복구 ▼▼▼
        if (DBManager.Instance != null)
        {
            DBManager.Instance.ApplyFieldDataToScene();
        }
    }

    public void ShowConfirmPopup(string message, Action onConfirm)
    {
        if (confirmPopup == null) return;

        confirmPopup.SetActive(true);
        if (confirmText != null) confirmText.text = message;

        // '네' 버튼 설정
        if (confirmYesButton != null)
        {
            confirmYesButton.onClick.RemoveAllListeners();
            confirmYesButton.onClick.AddListener(() =>
            {
                onConfirm(); // 진짜 기능 실행 (판매 등)
                confirmPopup.SetActive(false);
                SoundManager.Instance.PlaySFX("button");
            });
        }

        // '아니오' 버튼 설정
        if (confirmNoButton != null)
        {
            confirmNoButton.onClick.RemoveAllListeners();
            confirmNoButton.onClick.AddListener(() =>
            {
                confirmPopup.SetActive(false); // 그냥 닫기
                SoundManager.Instance.PlaySFX("button");
            });
        }

        SoundManager.Instance.PlaySFX("PopupOpen");
    }

    // 화면에 떠 있는 모든 메인 팝업을 닫는 함수
    public void CloseAllPopups(bool goToMain = true)
    {
        // 1. [소리 체크] 팝업이 하나라도 열려 있었는지 확인 (questPopup 추가)
        bool wasAnyPopupOpen = (inventoryPopup != null && inventoryPopup.activeSelf) ||
                               (researchLabPopup != null && researchLabPopup.activeSelf) ||
                               (storePopup != null && storePopup.activeSelf) ||
                               (collectionPopup != null && collectionPopup.activeSelf) ||
                               (fertilizerPopup != null && fertilizerPopup.activeSelf) ||
                               (questPopup != null && questPopup.activeSelf); // 퀘스트 창도 체크

        // 2. [소리 재생] 팝업이 열려있었다면 농장 BGM으로 복귀
        if (wasAnyPopupOpen && goToMain)
        {
            SoundManager.Instance.PlayBGM("mainfarm");
            SoundManager.Instance.PlaySFX("back"); 
        }

        isSeedPopupOpen = false;

        // 3. [기능] 실제로 팝업들 끄기
        if (fertilizerPopup != null) fertilizerPopup.SetActive(false);
        if (inventoryPopup != null) inventoryPopup.SetActive(false);
        if (researchLabPopup != null) researchLabPopup.SetActive(false);
        if (seedPopup != null) seedPopup.SetActive(false);
        if (storePopup != null) storePopup.SetActive(false);
        if (collectionPopup != null) collectionPopup.SetActive(false);
        if (confirmPopup != null) confirmPopup.SetActive(false);

        // ※ 추가 : 퀘스트 창이 열려있었다면 카메라 상태 복원
        if (questPopup != null && questPopup.activeSelf)
        {
            CameraController cameraController = FindObjectOfType<CameraController>();
            if (cameraController != null)
            {
                cameraController.RestoreCameraState(); 
            }
        }

        // ★ [추가] 퀘스트 창 끄기
        if (questPopup != null) questPopup.SetActive(false);

        ResetPoingUIPosition();

        // ★ [추가] 모든 팝업이 닫히고 메인 화면이면 -> 툴 UI 다시 켜기!
        if (goToMain && mainToolUI != null)
        {
            mainToolUI.SetActive(true);
        }
    }

    // [기능] 단순 메시지 알림창 띄우기
    public void ShowAlertPopup(string message)
    {
        // 텍스트 내용 변경
        if (alertMessageText != null) alertMessageText.text = message;

        // 기존에 연결된 버튼 이벤트 제거 (중복 실행 방지)
        alertCloseButton.onClick.RemoveAllListeners();

        // '확인' 버튼 누르면 팝업 꺼지도록 설정
        alertCloseButton.onClick.AddListener(() =>
        {
            alertPopup.SetActive(false);
        });

        alertPopup.SetActive(true); // 팝업 켜기
    }

    // [기능] 아이템 획득 팝업 띄우기
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
        itemAcquiredConfirmButton.onClick.AddListener(() =>
        {


            // 그냥 창만 닫으면 됨 (이미 ResearchLab이 아이템 줬음)
            itemAcquiredPopup.SetActive(false);
        });

        itemAcquiredPopup.SetActive(true);
    }

    // --- 팝업 열기 함수들 ---

    public void OpenQuestPopup()
    {
        CloseAllPopups(false); // 다른 창 닫기
        if (SideMenuUI.Instance != null) SideMenuUI.Instance.CloseMenu();

        if (questPopup != null)
        {
            // ※ 추가 : 퀘스트 화면 진입 전 현재 카메라 상태 저장
            CameraController cameraController = FindObjectOfType<CameraController>();
            if (cameraController != null)
            {
                cameraController.SaveCameraState();  // 현재 상태 저장
                cameraController.ResetCameraForQuest();  // 기본값으로 리셋
            }

            questPopup.SetActive(true);

            // ★ [추가] 퀘스트 창 열릴 때 포잉 바 숨기기
            if (poingBarRect != null)
                poingBarRect.gameObject.SetActive(false);

            SoundManager.Instance.PlaySFX("enter");
        }
    }



    public void OpenInventoryPopup()
    {
        CloseAllPopups(); // 다른 창 닫고

        if (SideMenuUI.Instance != null) SideMenuUI.Instance.CloseMenu();

        if (inventoryPopup != null)
        {
            inventoryPopup.SetActive(true); // 인벤토리 열기
            MovePoingUIToPopup(inventoryPopup.transform); // 포잉 바를 인벤토리 창 안으로 이동
            SoundManager.Instance.PlaySFX("enter");
        }
    }

    public void OpenResearchLabPopup()
    {
        CloseAllPopups();

        if (SideMenuUI.Instance != null) SideMenuUI.Instance.CloseMenu();

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
        CloseAllPopups();

        if (SideMenuUI.Instance != null) SideMenuUI.Instance.CloseMenu();

        if (storePopup != null)
        {
            storePopup.SetActive(true);
            MovePoingUIToPopup(storePopup.transform); // 포잉 바를 상점 창 안으로 이동
            SoundManager.Instance.PlayBGM("store");
            SoundManager.Instance.PlaySFX("enter");
        }
    }

    public void OpenCollectionPopup()
    {
        CloseAllPopups(); // 다른 창 다 닫고

        if (SideMenuUI.Instance != null) SideMenuUI.Instance.CloseMenu();

        if (collectionPopup != null)
        {
            collectionPopup.SetActive(true); // 도감 열기
            // (도감은 화면을 꽉 채우니까 포잉 바 이동은 선택사항. 필요하면 아래 줄 주석 해제)
            // MovePoingUIToPopup(collectionPopup.transform); 
            SoundManager.Instance.PlaySFX("open");
        }
    }

    // 밭을 클릭했을 때 씨앗 메뉴 열기
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
        CloseAllPopups(); // 다른 모든 팝업 닫기
        currentField = field;

        if (fertilizerPopup != null)
        {
            fertilizerPopup.SetActive(true);
            // FertilizerPopupUI 컴포넌트의 Show 함수 호출 (비료 정보 로드)
            // UIManager는 GameObject만 참조하고, 실제 UI 로직은 해당 스크립트가 처리합니다.
            FertilizerPopupUI fertilizerUI = fertilizerPopup.GetComponent<FertilizerPopupUI>();
            if (fertilizerUI != null)
            {
                fertilizerUI.Show(field);
            }
            else
            {
                Debug.LogError("FertilizerPopup에 FertilizerPopupUI 스크립트가 없습니다.");
            }
            SoundManager.Instance.PlaySFX("open");
        }
    }

    // 현재 열려있는 밭 정보를 반환 (다른 스크립트에서 사용)
    public Field GetCurrentField()
    {
        return currentField;
    }

    // [유틸리티] 포잉 UI(재화 바)를 특정 팝업창 안으로 이동시키는 함수
    // 이유: 팝업창 위에 돈이 보여야 하므로, 계층 구조상 부모를 바꿔줌
    private void MovePoingUIToPopup(Transform targetParent)
    {
        if (poingBarRect == null || targetParent == null) return;

        poingBarRect.SetParent(targetParent); // 부모 변경

        // 위치 및 앵커(Anchor) 재설정 (우측 상단 고정 등)
        poingBarRect.anchorMin = new Vector2(1, 1);
        poingBarRect.anchorMax = new Vector2(1, 1);
        poingBarRect.pivot = new Vector2(1, 1);
        poingBarRect.anchoredPosition = new Vector2(-200, -50); // 여백 조정
        poingBarRect.localScale = Vector3.one; // 크기 초기화
    }

    // [유틸리티] 포잉 UI를 원래 위치(메인 화면)로 복구하는 함수
    private void ResetPoingUIPosition()
    {
        if (poingBarRect == null || poingBarOriginalParent == null) return;

        poingBarRect.SetParent(poingBarOriginalParent); // 원래 부모로 복귀

        // 원래 위치 좌표로 재설정 (좌측 상단 등)
        poingBarRect.anchorMin = new Vector2(0, 1);
        poingBarRect.anchorMax = new Vector2(0, 1);
        poingBarRect.pivot = new Vector2(0, 1);
        poingBarRect.anchoredPosition = new Vector2(50, -50);
        poingBarRect.localScale = Vector3.one;

        // 다시 켜기
        poingBarRect.gameObject.SetActive(true);
    }

    // ▼▼▼ [추가] 연구실에서 도감으로 바로 이동할 때 사용하는 함수 ▼▼▼
    public void OpenCollectionPanel(ItemData targetItem = null)
    {
        // 1. 모든 팝업 닫기
        CloseAllPopups();

        // 2. 연구실 창 확실히 끄기 (변수명 수정됨: researchLabPanel -> researchLabPopup)
        if (researchLabPopup != null)
        {
            researchLabPopup.SetActive(false);
        }

        // 3. 도감 창 켜기
        if (collectionPopup != null)
        {
            collectionPopup.SetActive(true);
        }


        if (targetItem != null)
        {
            CollectionUI.Instance.ShowItem(targetItem);
        }
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
                (seedPopup != null && seedPopup.activeSelf);

            if (mainToolUI.activeSelf == isAnyPopupOpen)
            {
                mainToolUI.SetActive(!isAnyPopupOpen);
            }
        }

        // 2. [씨앗 팝업 닫기 로직] - 2D 전용
        if (isSeedPopupOpen)
        {
            // [추가] 마우스 휠(줌) 조작 시 즉시 닫기
            if (Input.GetAxis("Mouse ScrollWheel") != 0)
            {
                CloseAllPopups();
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                // UI(버튼 등)를 눌렀다면 팝업을 닫지 않음
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                // [2D 핵심] 마우스 클릭 지점의 월드 좌표를 가져옴
                Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                // 해당 지점에 있는 2D 콜라이더 검사
                Collider2D hitCollider = Physics2D.OverlapPoint(mousePos);

                if (hitCollider != null)
                {
                    Field clickedField = hitCollider.GetComponent<Field>();

                    // 클릭한 대상이 밭(Field)이 아니면 팝업 닫기
                    if (clickedField == null)
                    {
                        CloseAllPopups();
                    }
                }
                else
                {
                    // 아무것도 없는 허공을 클릭했을 때도 팝업 닫기
                    CloseAllPopups();
                }
            }
        }
    }

    // ▼▼▼ [추가] 마우스가 UI 위에 있는지 확인하는 함수 ▼▼▼
    public bool IsPointerOverUI()
    {
        // PC (마우스)
        if (EventSystem.current.IsPointerOverGameObject())
            return true;

        // 모바일 (터치)
        if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            return true;

        return false;
    }
}