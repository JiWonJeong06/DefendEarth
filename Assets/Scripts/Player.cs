using UnityEngine;

public class Player : MonoBehaviour
{
    public float speed = 5f;

    private float moveInput = 0;

    public void SetMove(float value)
    {
        moveInput = value;
    }

    void Update()
    {
        Vector3 move = new Vector3(moveInput, 0, 0);
        transform.position += move * speed * Time.deltaTime;
    }
}