using UnityEngine;

// Diament do zebrania. Obiekt z Collider2D (Is Trigger) i tym skryptem.
[RequireComponent(typeof(Collider2D))]
public class Diamond : MonoBehaviour
{
    [Tooltip("Opcjonalny dzwiek zebrania")]
    [SerializeField] private AudioClip collectSound;

    private bool collected;

    private void Reset()
    {
        // Automatycznie ustaw collider jako trigger przy dodaniu skryptu
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (other.GetComponentInParent<Player_Movement>() == null) return;

        if (DiamondCounter.Instance == null)
        {
            Debug.LogWarning("Diamond: brak DiamondCounter w scenie.");
            return;
        }

        collected = true;
        DiamondCounter.Instance.Add();

        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }

        Destroy(gameObject);
    }
}