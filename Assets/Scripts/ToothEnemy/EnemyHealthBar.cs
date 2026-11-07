using UnityEngine;
using UnityEngine.UI;


public class EnemyHealthBar : MonoBehaviour
{
    [Tooltip("Jesli puste, skrypt szuka Health w obiekcie nadrzednym")]
    [SerializeField] private Health health;
    [SerializeField] private Image fillImage;

    private Vector3 baseScale;

    private void Awake()
    {
        if (health == null)
        {
            health = GetComponentInParent<Health>();
        }

        baseScale = transform.localScale;
    }

    private void OnEnable()
    {
        if (health == null) return;

        health.Changed += Refresh;
        health.Died += Hide;
    }

    private void OnDisable()
    {
        if (health == null) return;

        health.Changed -= Refresh;
        health.Died -= Hide;
    }

    private void Start()
    {
        if (health != null)
        {
            Refresh(health.Current, health.Max);
        }
    }

    private void Refresh(int current, int max)
    {
        fillImage.fillAmount = max > 0 ? (float)current / max : 0f;
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    // Gdy przeciwnik odwraca sie (ujemna skala X), pasek nie moze sie odbijac w lustrze
    private void LateUpdate()
    {
        Transform parent = transform.parent;
        if (parent == null) return;

        Vector3 scale = baseScale;
        if (parent.lossyScale.x < 0f) scale.x = -scale.x;
        transform.localScale = scale;
    }
}