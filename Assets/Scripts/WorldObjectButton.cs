using UnityEngine;
using UnityEngine.EventSystems;

public class WorldObjectButton : MonoBehaviour
{
    public enum WorldObjectType { Store, Lab }
    public WorldObjectType objectType;

    private void OnMouseDown()
    {
        if (IsAnyPopupActive()) 
        {
            return;
        }

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
    private bool IsAnyPopupActive()
    {

        var ui = UIManager.Instance;
        
        return ui.inventoryPopup.activeSelf || 
               ui.researchLabPopup.activeSelf || 
               ui.storePopup.activeSelf || 
               ui.collectionPopup.activeSelf || 
               ui.seedPopup.activeSelf || 
               ui.fertilizerPopup.activeSelf ||
               ui.questPopup.activeSelf;
    }
}