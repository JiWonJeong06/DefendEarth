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
        if (stick != null)
        {
            stickStartPos = stick.anchoredPosition;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (stick == null) return;
        
        // 터치 위치를 조이스틱 배경의 로컬 좌표로 정확하게 변환
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(joystickRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            // X축 이동 거리를 -maxDistance 와 maxDistance 사이로 제한
            float clampedX = Mathf.Clamp(localPoint.x, -maxDistance, maxDistance);
            
            // 스틱 이동 적용 (Y축은 고정)
            stick.anchoredPosition = new Vector2(clampedX, stickStartPos.y);
            
            // 입력 방향을 -1.0 ~ 1.0 사이로 정규화
            inputDirection = new Vector2(clampedX / maxDistance, 0f);
        }
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