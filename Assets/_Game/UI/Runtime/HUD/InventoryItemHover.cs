using System;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class InventoryItemHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    public Action<PointerEventData> Entered;
    public Action<PointerEventData> Exited;
    public Action<PointerEventData> Moved;

    public void OnPointerEnter(PointerEventData eventData)
    {
        Entered?.Invoke(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Exited?.Invoke(eventData);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        Moved?.Invoke(eventData);
    }
}
