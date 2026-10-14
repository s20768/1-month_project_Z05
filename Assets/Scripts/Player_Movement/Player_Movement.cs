using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Player_Movement : MonoBehaviour
{
    [Header("Ruch")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Skok")]
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private bool jumpUnlocked = true;
    [SerializeField] private bool doubleJumpUnlocked = true;

    [Header("Ground check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.05f;

    private Rigidbody2D rb;
    private Collider2D col;
    private float moveInput;

    private bool isGrounded;
    private float coyoteCounter;
    private bool canDoubleJump;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    private void Update()
    {
        moveInput = ReadMoveInput();
        Flip();
        CheckGround();
        HandleJump();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    private float ReadMoveInput()
    {
        float input = 0f;

        // Klawiatura: strzalki
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.leftArrowKey.isPressed) input -= 1f;
            if (keyboard.rightArrowKey.isPressed) input += 1f;
        }
        return Mathf.Clamp(input, -1f, 1f);
    }

    private void CheckGround()
    {
        Bounds b = col.bounds;
        Vector2 center = new Vector2(b.center.x, b.min.y - groundCheckDistance / 2f);
        Vector2 size = new Vector2(b.size.x * 0.9f, groundCheckDistance);

        bool touchingGround = Physics2D.OverlapBox(center, size, 0f, groundLayer) != null;

       
        isGrounded = touchingGround && rb.linearVelocity.y <= 0.01f;

        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
            canDoubleJump = doubleJumpUnlocked;
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }
    }

    private void HandleJump()
    {
        if (!jumpUnlocked) return;

        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.spaceKey.wasPressedThisFrame) return;

        if (coyoteCounter > 0f)
        {
            
            Jump();
            coyoteCounter = 0f;
        }
        else if (canDoubleJump)
        {
            
            Jump();
            canDoubleJump = false;
        }
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    private void Flip()
    {
        if (moveInput == 0f) return;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(moveInput);
        transform.localScale = scale;
    }
}