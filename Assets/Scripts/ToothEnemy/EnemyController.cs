using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Health))]
public class EnemyController : MonoBehaviour
{
    private enum State { Patrol, Chase, Attack, Hurt, Dead }

    [Header("Patrol")]
    [SerializeField] private float patrolSpeed = 1.5f;
    [Tooltip("Jak daleko od pozycji startowej chodzi w kazda strone. 0 = stoi w miejscu.")]
    [SerializeField] private float patrolRange = 3f;
    [SerializeField] private float turnPauseTime = 0.5f;

    [Header("Poscig")]
    [SerializeField] private float chaseSpeed = 3f;
    [Tooltip("Promien, w ktorym przeciwnik zauwaza gracza (player awareness)")]
    [SerializeField] private float playerAwareness = 5f;
    [Tooltip("Po przekroczeniu playerAwareness * ta wartosc przeciwnik rezygnuje z poscigu")]
    [SerializeField] private float loseInterestMultiplier = 1.5f;

    [Header("Skok")]
    [Tooltip("0 = przeciwnik nie skacze")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float jumpCooldown = 1f;

    [Header("Atak")]
    [SerializeField] private int attackDamage = 1;
    [Tooltip("Z jakiej odleglosci poziomej od gracza zaczyna atak")]
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float attackVerticalTolerance = 1f;
    [SerializeField] private float attackCooldown = 1.2f;
    [Tooltip("Opoznienie od startu animacji do trafienia (dopasuj do uderzenia w animacji)")]
    [SerializeField] private float attackHitDelay = 0.25f;
    [Tooltip("Srodek strefy trafienia wzgledem przeciwnika (X jest zawsze przed nim)")]
    [SerializeField] private Vector2 attackOffset = new Vector2(0.7f, 0f);
    [SerializeField] private float attackRadius = 0.6f;

    [Header("Otoczenie")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float wallCheckDistance = 0.3f;
    [SerializeField] private float ledgeCheckDistance = 0.6f;

    [Header("Reakcje")]
    [SerializeField] private float knockbackForce = 4f;
    [SerializeField] private float hurtStunTime = 0.3f;
    [Tooltip("Po ilu sekundach od smierci obiekt znika (czas na animacje smierci)")]
    [SerializeField] private float destroyDelay = 1.5f;

    [Header("Animacje")]
    [SerializeField] private Animator animator;
    [Tooltip("Zaznacz, jesli sprite domyslnie patrzy w prawo")]
    [SerializeField] private bool spriteFacesRight = true;

    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int YVelocityHash = Animator.StringToHash("YVelocity");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DieHash = Animator.StringToHash("Die");

    private readonly HashSet<int> animParams = new HashSet<int>();

    private Rigidbody2D rb;
    private Collider2D col;
    private Health health;
    private Transform player;

    private State state = State.Patrol;
    private int facing = 1;
    private float desiredDir;
    private bool isGrounded;

    private float patrolCenterX;
    private float pauseUntil;
    private float nextAttackTime;
    private float nextJumpTime;
    private float hurtUntil;
    private Coroutine attackRoutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        health = GetComponent<Health>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        // Zapamietaj, jakie parametry ma Animator - brakujace sa po prostu pomijane (bez ostrzezen)
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                animParams.Add(p.nameHash);
            }
        }
    }

    private void OnEnable()
    {
        health.Damaged += OnDamaged;
        health.Died += OnDied;
    }

    private void OnDisable()
    {
        health.Damaged -= OnDamaged;
        health.Died -= OnDied;
    }

    private void Start()
    {
        patrolCenterX = transform.position.x;
        facing = ((transform.localScale.x >= 0f) == spriteFacesRight) ? 1 : -1;

        Player_Movement p = FindFirstObjectByType<Player_Movement>();
        if (p != null)
        {
            player = p.transform;
        }
        else
        {
            Debug.LogWarning($"{name}: nie znaleziono gracza w scenie.");
        }
    }

    private void Update()
    {
        if (state == State.Dead) return;

        isGrounded = CheckGround();

        float distanceToPlayer = player != null
            ? Vector2.Distance(transform.position, player.position)
            : float.MaxValue;

        switch (state)
        {
            case State.Patrol:
                desiredDir = PatrolDirection();
                if (distanceToPlayer <= playerAwareness) state = State.Chase;
                break;

            case State.Chase:
                if (distanceToPlayer > playerAwareness * loseInterestMultiplier)
                {
                    state = State.Patrol;
                    desiredDir = 0f;
                    break;
                }

                desiredDir = ChaseDirection(out bool inAttackRange);
                if (inAttackRange && isGrounded && Time.time >= nextAttackTime)
                {
                    StartAttack();
                }
                break;

            case State.Attack:
                desiredDir = 0f;
                break;

            case State.Hurt:
                desiredDir = 0f;
                if (Time.time >= hurtUntil) state = State.Chase;
                break;
        }

        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        // W stanie Hurt nie nadpisujemy predkosci, zeby zadzialal odrzut
        if (state == State.Dead || state == State.Hurt) return;

        float speed = state == State.Chase ? chaseSpeed : patrolSpeed;
        rb.linearVelocity = new Vector2(desiredDir * speed, rb.linearVelocity.y);
    }

    // ---------- Patrol i poscig ----------

    private float PatrolDirection()
    {
        if (patrolRange <= 0f) return 0f;
        if (Time.time < pauseUntil) return 0f;

        float dx = transform.position.x - patrolCenterX;
        bool reachedEnd = (facing > 0 && dx >= patrolRange) || (facing < 0 && dx <= -patrolRange);
        bool blocked = WallAhead() || (isGrounded && LedgeAhead());

        if (reachedEnd || blocked)
        {
            facing = -facing;
            ApplyFacing();
            pauseUntil = Time.time + turnPauseTime;
            return 0f;
        }

        return facing;
    }

    private float ChaseDirection(out bool inAttackRange)
    {
        inAttackRange = false;
        if (player == null) return 0f;

        float dx = player.position.x - transform.position.x;
        float dy = player.position.y - transform.position.y;

        // Obracaj sie do gracza (z martwa strefa, zeby nie drzec przy tym samym X)
        if (Mathf.Abs(dx) > 0.1f)
        {
            int dir = dx > 0f ? 1 : -1;
            if (dir != facing)
            {
                facing = dir;
                ApplyFacing();
            }
        }

        if (Mathf.Abs(dx) <= attackRange && Mathf.Abs(dy) <= attackVerticalTolerance)
        {
            inAttackRange = true;
            return 0f;
        }

        // Skok przez przeszkode albo gdy gracz jest wyzej i blisko
        if (WallAhead())
        {
            TryJump();
        }
        else if (dy > 1.2f && Mathf.Abs(dx) < 2f)
        {
            TryJump();
        }

        // Nie wbiegaj w przepasc
        if (isGrounded && LedgeAhead()) return 0f;

        return facing;
    }

    private void TryJump()
    {
        if (jumpForce <= 0f || !isGrounded || Time.time < nextJumpTime) return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        nextJumpTime = Time.time + jumpCooldown;
    }

    // ---------- Atak ----------

    private void StartAttack()
    {
        state = State.Attack;
        nextAttackTime = Time.time + attackCooldown;
        attackRoutine = StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        SetTrigger(AttackHash);

        yield return new WaitForSeconds(attackHitDelay);

        // Przerwany (np. przeciwnik dostal cios)
        if (state != State.Attack) yield break;

        DealDamage();

        yield return new WaitForSeconds(Mathf.Min(0.4f, attackCooldown * 0.4f));

        if (state == State.Attack) state = State.Chase;
        attackRoutine = null;
    }

    private void DealDamage()
    {
        Vector2 center = (Vector2)transform.position + new Vector2(attackOffset.x * facing, attackOffset.y);
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, attackRadius);

        foreach (Collider2D hit in hits)
        {
            Player_Movement playerMovement = hit.GetComponentInParent<Player_Movement>();
            if (playerMovement == null) continue;

            IDamageable target = playerMovement.GetComponent<IDamageable>();
            if (target == null)
            {
                Debug.LogWarning("Gracz nie ma komponentu Health - dodaj go, zeby przeciwnik mogl zadawac obrazenia.");
                return;
            }

            target.TakeDamage(attackDamage, transform.position);
            return;
        }
    }

    // ---------- Reakcje na obrazenia i smierc ----------

    private void OnDamaged(Vector2 sourcePosition)
    {
        if (state == State.Dead) return;

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        state = State.Hurt;
        hurtUntil = Time.time + hurtStunTime;
        SetTrigger(HitHash);

        float dir = transform.position.x >= sourcePosition.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(dir * knockbackForce, Mathf.Max(rb.linearVelocity.y, knockbackForce * 0.5f));
    }

    private void OnDied()
    {
        state = State.Dead;
        StopAllCoroutines();

        SetTrigger(DieHash);
        SetBool(IsRunningHash, false);

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        Destroy(gameObject, destroyDelay);
    }

    // ---------- Otoczenie ----------

    private bool CheckGround()
    {
        Bounds b = col.bounds;
        Vector2 center = new Vector2(b.center.x, b.min.y - 0.03f);
        Vector2 size = new Vector2(b.size.x * 0.9f, 0.06f);
        return Physics2D.OverlapBox(center, size, 0f, groundLayer) != null;
    }

    private bool WallAhead()
    {
        Bounds b = col.bounds;
        Vector2 origin = new Vector2(b.center.x, b.min.y + 0.1f);
        return Physics2D.Raycast(origin, Vector2.right * facing, b.extents.x + wallCheckDistance, groundLayer);
    }

    // true, gdy przed przeciwnikiem nie ma podlogi
    private bool LedgeAhead()
    {
        Bounds b = col.bounds;
        Vector2 origin = new Vector2(b.center.x + facing * (b.extents.x + 0.1f), b.min.y + 0.05f);
        return !Physics2D.Raycast(origin, Vector2.down, ledgeCheckDistance, groundLayer);
    }

    private void ApplyFacing()
    {
        Vector3 scale = transform.localScale;
        float sign = spriteFacesRight ? facing : -facing;
        scale.x = Mathf.Abs(scale.x) * sign;
        transform.localScale = scale;
    }

    // ---------- Animator (pomija brakujace parametry) ----------

    private void UpdateAnimator()
    {
        bool moving = (state == State.Patrol || state == State.Chase) && Mathf.Abs(desiredDir) > 0.01f;

        SetBool(IsRunningHash, moving);
        SetBool(IsGroundedHash, isGrounded);
        SetFloat(YVelocityHash, rb.linearVelocity.y);
    }

    private void SetBool(int hash, bool value)
    {
        if (animator != null && animParams.Contains(hash)) animator.SetBool(hash, value);
    }

    private void SetFloat(int hash, float value)
    {
        if (animator != null && animParams.Contains(hash)) animator.SetFloat(hash, value);
    }

    private void SetTrigger(int hash)
    {
        if (animator != null && animParams.Contains(hash)) animator.SetTrigger(hash);
    }

    // Podglad w Scene (zaznacz przeciwnika): zolty = zasieg zauwazenia, czerwony = strefa ataku, zielony = patrol
    private void OnDrawGizmosSelected()
    {
        int dir = Application.isPlaying ? facing : 1;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, playerAwareness);

        Gizmos.color = Color.red;
        Vector2 attackCenter = (Vector2)transform.position + new Vector2(attackOffset.x * dir, attackOffset.y);
        Gizmos.DrawWireSphere(attackCenter, attackRadius);

        float centerX = Application.isPlaying ? patrolCenterX : transform.position.x;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(
            new Vector3(centerX - patrolRange, transform.position.y, 0f),
            new Vector3(centerX + patrolRange, transform.position.y, 0f));
    }
}