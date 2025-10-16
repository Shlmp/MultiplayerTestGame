using Fusion;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    public float speed = 2f;
    private Rigidbody2D rb;
    private BoxCollider2D col;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput<MyInput>(out var inputs) == false) { return; }

        Debug.Log("Is Moving");
        Movement();
    }

    private void Movement()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Debug.Log("MOVE, Speed is  -  " + speed);
        Vector2 moveVelocity = new Vector2(horizontalInput * speed * Runner.DeltaTime, verticalInput * speed * Runner.DeltaTime);  //  NEEDS TO BE FIXED
        rb.linearVelocity = moveVelocity;
    }
}