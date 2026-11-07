using System;
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

    [Header("Atak")]
    [SerializeField] private float attackCooldown = 0.4f;
    [SerializeField] private bool attackUnlocked = true;

    [Header("Animacje")]
    [SerializeField] private Animator _animator;

    // Zdarzenia dla tutorialu
    public event Action Jumped;
    public event Action DoubleJumped;
    public event Action Attacked;

    public float MoveInput => moveInput;
    public bool IsGrounded => isGrounded;

    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int YVelocityHash = Animator.StringToHash("YVelocity");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int AirAttackHash = Animator.StringToHash("AirAttack");

    private Rigidbody2D rb;
    private Collider2D col;
    private float moveInput;
    private bool inputLocked;

    private bool isGrounded;
    private float coyoteCounter;
    private bool canDoubleJump;

    private float attackTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        // Jesli nie przypisano w Inspectorze, szukaj Animatora na graczu lub jego dzieciach
        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
        }
    }

    private void Update()
    {
        moveInput = inputLocked ? 0f : ReadMoveInput();
        Flip();
        CheckGround();

        if (!inputLocked)
        {
            HandleJump();
            HandleAttack();
        }

        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    private float ReadMoveInput()
    {
        float input = 0f;

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input += 1f;
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
            Jumped?.Invoke();
        }
        else if (canDoubleJump)
        {
            Jump();
            canDoubleJump = false;
            DoubleJumped?.Invoke();
        }
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    private void HandleAttack()
    {
        attackTimer -= Time.deltaTime;

        if (!attackUnlocked) return;

        // Lewy przycisk myszy lub Q
        bool attackPressed =
            (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
            (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame);

        if (!attackPressed) return;
        if (attackTimer > 0f) return;

        attackTimer = attackCooldown;

        if (_animator != null)
        {
            _animator.SetTrigger(isGrounded ? AttackHash : AirAttackHash);
        }

        Attacked?.Invoke();
    }

    private void Flip()
    {
        if (moveInput == 0f) return;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(moveInput);
        transform.localScale = scale;
    }

    private void UpdateAnimator()
    {
        if (_animator == null) return;

        _animator.SetBool(IsRunningHash, moveInput != 0f);
        _animator.SetBool(IsGroundedHash, isGrounded);
        _animator.SetFloat(YVelocityHash, rb.linearVelocity.y);
    }

    // Blokada sterowania (rozmowa z Krabem, cutscenki itp.)
    public void SetInputLocked(bool value)
    {
        inputLocked = value;
        if (value) moveInput = 0f;
    }

    // Odblokowywanie mechanik (tutorial, Krab)
    public void SetJumpUnlocked(bool value) => jumpUnlocked = value;
    public void SetDoubleJumpUnlocked(bool value) => doubleJumpUnlocked = value;
    public void SetAttackUnlocked(bool value) => attackUnlocked = value;
}