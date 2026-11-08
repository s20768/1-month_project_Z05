using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Koniec gry po smierci ostatniego bossa: chwila czekania (na animacje smierci),
// blokada gracza, zatrzymanie gry i ekran "Koniec gry".
// Dodaj do OSOBNEGO pustego obiektu (nie do bossa - boss jest niszczony po smierci).
public class EndGameManager : MonoBehaviour
{
    [Header("Ostatni boss")]
    [SerializeField] private Health bossHealth;

    [Header("Ekran konca gry")]
    [Tooltip("Canvas (lub panel) z napisem i przyciskami. Skrypt ukrywa go na starcie.")]
    [SerializeField] private GameObject endScreen;
    [SerializeField] private float delayBeforeEndScreen = 2f;

    [Header("Sceny")]
    [SerializeField] private string mainMenuSceneName = "Main Menu";

    private bool ended;

    private void Awake()
    {
        if (endScreen != null)
        {
            endScreen.SetActive(false);
        }
    }

    private void Start()
    {
        if (bossHealth != null)
        {
            bossHealth.Died += OnBossDied;
        }
        else
        {
            Debug.LogWarning("EndGameManager: nie przypisano bossa (Boss Health).");
        }
    }

    private void OnDestroy()
    {
        if (bossHealth != null)
        {
            bossHealth.Died -= OnBossDied;
        }

        // Zabezpieczenie: nie zostawiaj zatrzymanego czasu po zmianie sceny
        Time.timeScale = 1f;
    }

    private void OnBossDied()
    {
        if (ended) return;
        ended = true;

        StartCoroutine(EndRoutine());
    }

    private IEnumerator EndRoutine()
    {
        yield return new WaitForSeconds(delayBeforeEndScreen);

        Player_Movement player = FindFirstObjectByType<Player_Movement>();
        if (player != null)
        {
            player.SetInputLocked(true);
        }

        if (endScreen != null)
        {
            endScreen.SetActive(true);
        }

        // Zatrzymuje cala gre (przyciski UI dzialaja dalej)
        Time.timeScale = 0f;
    }

    // --- Przyciski ekranu konca gry ---

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}