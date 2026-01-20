using UnityEngine;
using UnityEngine.EventSystems;

public class WorldObjectButton : MonoBehaviour
{
    public enum WorldObjectType { Store, Lab }
    public WorldObjectType objectType;

    // 마우스가 클릭되었을 때 호출
    private void OnMouseDown()
    {
        // 1. 만약 다른 UI(인벤토리 등)가 이미 열려있다면 클릭 무시
        if (EventSystem.current.IsPointerOverGameObject()) return;

        // 2. 타입에 따라 UIManager 호출
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

}