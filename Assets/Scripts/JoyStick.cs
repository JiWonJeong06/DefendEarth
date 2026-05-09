using UnityEngine;
using UnityEngine.EventSystems;

public class JoyStick : MonoBehaviour, IDragHandler, IEndDragHandler
{
    [Header("Joystick Settings")]
    [SerializeField] private RectTransform stick; 
    [SerializeField] private float maxDistance; 
    
    private RectTransform joystickRect;
    private Vector2 inputDirection = Vector2.zero;
    private Vector2 stickStartPos;

    private void Start()
    {
        joystickRect = GetComponent<RectTransform>();
        stickStartPos = stick.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (stick == null) return;
        
        Vector2 dragOffset = (Vector2)eventData.position - (Vector2)joystickRect.position;
        
        // 최대 거리로 제한
        if (dragOffset.magnitude > maxDistance)
        {
            dragOffset = dragOffset.normalized * maxDistance;
        }
        
        // 스틱을 좌우로만 움직임 (Y는 stickStartPos.y 유지)
        stick.anchoredPosition = new Vector2(dragOffset.x, stickStartPos.y);
        
        // 입력 방향은 X값만 사용
        inputDirection = new Vector2(dragOffset.x, 0f);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (stick == null) return;
        
        // 스틱을 중앙으로 돌려놓음
        stick.anchoredPosition = stickStartPos;
        
        // 입력 방향 초기화
        inputDirection = Vector2.zero;
    }

    public float GetHorizontalInput()
    {
        return inputDirection.x;
    }

    public float GetVerticalInput()
    {
        return inputDirection.y;
    }

    public Vector2 GetInputDirection()
    {
        return inputDirection;
    }
}