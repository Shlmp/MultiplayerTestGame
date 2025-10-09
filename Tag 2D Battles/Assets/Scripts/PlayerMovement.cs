using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private CharacterController characterController;
    public float playerSpeed = 2f;
    private PlayerInputs playerInputs;

    private void Awake()
    {
        playerInputs = new PlayerInputs();
        playerInputs.Player.Enable();
        gameObject.TryGetComponent(out characterController);
    }

    void Update()
    {
        Vector2 moveInput = playerInputs.Player.Movement.ReadValue<Vector2>();
        Vector2 move = new Vector2(moveInput.x, moveInput.y);

        characterController.Move(move * playerSpeed * Time.deltaTime);

        if (move != Vector2.zero)
        {
            gameObject.transform.forward = move;
        }
    }
}
