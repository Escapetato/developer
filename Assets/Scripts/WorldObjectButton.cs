using UnityEngine;
using UnityEngine.EventSystems;

public class WorldObjectButton : MonoBehaviour
{
    public enum WorldObjectType { Store, Lab }
    public WorldObjectType objectType;

    private void OnMouseDown()
    {
        // ▼▼▼ [추가할 코드] 설정 버튼 같은 UI를 누르고 있다면 건물 클릭 무시! ▼▼▼
        if (UIManager.Instance.IsPointerOverUI())
        {
            return;
        }
        // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

        // (기존 코드: 이미 팝업이 떠 있으면 클릭 무시)
        if (IsAnyPopupActive())
        {
            return;
        }

        // 타입에 따라 팝업 열기
        switch (objectType)
        {
            case WorldObjectType.Store:
                UIManager.Instance.OpenStorePopup();
                break;
            case WorldObjectType.Lab:
                UIManager.Instance.OpenResearchLabPopup();
                break;
        }
    }

    private bool IsAnyPopupActive()
    {
        var ui = UIManager.Instance;
        // 안전장치: ui가 null일 수도 있으니 체크
        if (ui == null) return false;

        return ui.inventoryPopup.activeSelf ||
               ui.researchLabPopup.activeSelf ||
               ui.storePopup.activeSelf ||
               ui.collectionPopup.activeSelf ||
               ui.seedPopup.activeSelf ||
               ui.fertilizerPopup.activeSelf ||
               ui.questPopup.activeSelf;
    }
}