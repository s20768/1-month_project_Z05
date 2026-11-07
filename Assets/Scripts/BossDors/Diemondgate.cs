using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DiamondGate : MonoBehaviour
{
    [Header("Warunek otwarcia")]
    [Tooltip("Zaznaczone: po zebraniu diamentow brama otworzy sie dopiero, gdy gracz podejdzie. Odznaczone: otwiera sie od razu.")]
    [SerializeField] private bool requirePlayerNearby = true;

    [Tooltip("Jak daleko od krawedzi obiektu (w jednostkach) gracz ma byc, zeby brama sie otworzyla. Zobacz niebieska ramke w Scene.")]
    [SerializeField] private Vector2 activationMargin = new Vector2(3f, 3f);

    [Header("Ruch po otwarciu")]
    [Tooltip("O ile jednostek przesunac obiekt. (0, -6) = spada w dol, (0, 6) = podnosi sie w gore")]
    [SerializeField] private Vector2 moveOffset = new Vector2(0f, -6f);
    [SerializeField] private float moveDuration = 1.0f;
    [SerializeField] private float delayBeforeMove = 0.3f;

    [Tooltip("Zaznaczone: ruch przyspiesza jak spadanie. Odznaczone: plynny ruch (dobre dla drzwi).")]
    [SerializeField] private bool fallEasing = true;

    [Header("Po otwarciu")]
    [Tooltip("Wylacza collider od razu na poczatku ruchu (gracz stojacy na moscie spada razem z nim)")]
    [SerializeField] private bool disableColliderAtStart = true;
    [SerializeField] private bool disableColliderWhenOpen = true;
    [Tooltip("Po zakonczeniu ruchu ukrywa obiekt (np. zeby most nie wisial na dnie szybu)")]
    [SerializeField] private bool disableObjectWhenOpen = true;

    private bool unlocked;  
    private bool opened;  

    private Transform playerTransform;
    private Bounds activationBounds;

    private void Start()
    {
        Collider2D col = GetComponent<Collider2D>();
        Vector3 size = col.bounds.size + new Vector3(activationMargin.x * 2f, activationMargin.y * 2f, 100f);
        activationBounds = new Bounds(col.bounds.center, size);

        Player_Movement player = FindFirstObjectByType<Player_Movement>();
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else if (requirePlayerNearby)
        {
            Debug.LogWarning("DiamondGate: nie znaleziono gracza w scenie.");
        }

        DiamondCounter counter = DiamondCounter.Instance;
        if (counter == null)
        {
            Debug.LogWarning("DiamondGate: brak DiamondCounter w scenie.");
            return;
        }

        if (counter.HasEnough)
        {
            Unlock();
        }
        else
        {
            counter.AllCollected += Unlock;
        }
    }

    private void OnDestroy()
    {
        if (DiamondCounter.Instance != null)
        {
            DiamondCounter.Instance.AllCollected -= Unlock;
        }
    }

    private void Update()
    {
        if (opened || !unlocked || !requirePlayerNearby || playerTransform == null) return;

        if (activationBounds.Contains(playerTransform.position))
        {
            Open();
        }
    }

    // Wywolywane po zebraniu wymaganej liczby diamentow
    private void Unlock()
    {
        unlocked = true;

        if (!requirePlayerNearby)
        {
            Open();
        }
    }

    public void Open()
    {
        if (opened) return;
        opened = true;
        StartCoroutine(OpenRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        if (delayBeforeMove > 0f)
        {
            yield return new WaitForSeconds(delayBeforeMove);
        }

        if (disableColliderAtStart)
        {
            SetCollidersEnabled(false);
        }

        Vector3 from = transform.position;
        Vector3 to = from + (Vector3)moveOffset;

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float x = Mathf.Clamp01(elapsed / moveDuration);
            float t = fallEasing ? x * x : Mathf.SmoothStep(0f, 1f, x);
            transform.position = Vector3.Lerp(from, to, t);
            yield return null;
        }

        transform.position = to;

        if (disableColliderWhenOpen)
        {
            SetCollidersEnabled(false);
        }

        if (disableObjectWhenOpen)
        {
            gameObject.SetActive(false);
        }
    }

    private void SetCollidersEnabled(bool value)
    {
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>())
        {
            c.enabled = value;
        }
    }

    // Podglad w Scene: zolta linia = dokad obiekt sie przesunie, niebieska ramka = strefa podejscia gracza
    private void OnDrawGizmosSelected()
    {
        Vector3 target = transform.position + (Vector3)moveOffset;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, target);
        Gizmos.DrawWireSphere(target, 0.3f);

        Collider2D col = GetComponent<Collider2D>();
        if (col != null && requirePlayerNearby)
        {
            Vector3 size = col.bounds.size + new Vector3(activationMargin.x * 2f, activationMargin.y * 2f, 0f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(col.bounds.center, size);
        }
    }
}