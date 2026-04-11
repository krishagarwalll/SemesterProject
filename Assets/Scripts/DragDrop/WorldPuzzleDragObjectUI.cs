using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class WorldPuzzleDragObjectUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private bool canBeDragged = true;
    [SerializeField] private bool keepPointerOffset = true;

    [SerializeField] private bool useUnstableOffset;
    [SerializeField] private float offsetStrength = 20f;
    [SerializeField] private float offsetSpeed = 5f;

    private Vector2 unstableOffset;
    
    private RectTransform rectTransform;
    private Vector2 dragOffset;
    

    public bool CanBeDragged => canBeDragged;
    private Vector2 lastPointerPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public bool CanStartDrag()
    {
        return canBeDragged && enabled && gameObject.activeInHierarchy;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanStartDrag()) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );
        dragOffset = keepPointerOffset
            ? rectTransform.anchoredPosition - localPoint
            : Vector2.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );

        //float movement = Vector2.Distance(localPoint, lastPointerPosition);
        //bool isPointerMoving = movement > 0.01f;

        if (useUnstableOffset)
        {
            float x = Mathf.PerlinNoise(Time.time * offsetSpeed, 0f) - 0.5f;
            float y = Mathf.PerlinNoise(0f, Time.time * offsetSpeed) - 0.5f;

            unstableOffset = new Vector2(x, y) * offsetStrength;
        }
        else
        {
            unstableOffset = Vector2.zero;
        }

        //rectTransform.anchoredPosition = localPoint + dragOffset + unstableOffset;
        rectTransform.anchoredPosition = Vector2.Lerp(
            rectTransform.anchoredPosition,
            localPoint + dragOffset + unstableOffset,
            10f * Time.deltaTime
        );

        //lastPointerPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        
    }
}
