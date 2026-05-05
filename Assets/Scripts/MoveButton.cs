using UnityEngine;
using UnityEngine.EventSystems;

public class MoveButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum Direction { Left, Right }
    public Direction direction;

    private Player player;

    void Awake()
    {
        player = FindFirstObjectByType<Player>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (direction == Direction.Left)
            player.SetMove(-1);
        else
            player.SetMove(1);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        player.SetMove(0);
    }
}