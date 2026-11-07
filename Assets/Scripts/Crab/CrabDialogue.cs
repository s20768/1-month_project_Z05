using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Okienko z tekstem Kraba. Podepnij do obiektu Canvas / panelu dialogowego.
public class CrabDialogue : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text messageText;

    [Tooltip("Opcjonalny napis typu 'Press E', widoczny tylko, gdy czekamy na kontynuacje")]
    [SerializeField] private GameObject continueHint;

    private Coroutine hideRoutine;

    private void Awake()
    {
        panel.SetActive(false);
        if (continueHint != null) continueHint.SetActive(false);
    }

    // duration = 0 -> tekst zostaje, dopoki nie pojawi sie nastepny lub Hide()
    public void Show(string message, float duration = 0f)
    {
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }

        messageText.text = message;
        panel.SetActive(true);

        if (duration > 0f)
        {
            hideRoutine = StartCoroutine(HideAfter(duration));
        }
    }

    public void Hide()
    {
        panel.SetActive(false);
        if (continueHint != null) continueHint.SetActive(false);
    }

    // Pokazuje kolejne kwestie; kazda czeka na wcisniecie E lub Enter.
    // Uzycie: yield return dialogue.ShowLines("a", "b");
    public IEnumerator ShowLines(params string[] lines)
    {
        foreach (string line in lines)
        {
            Show(line);
            if (continueHint != null) continueHint.SetActive(true);

            // Pomin jedna klatke, zeby nie zjesc tego samego wcisniecia klawisza
            yield return null;

            while (!ContinuePressed())
            {
                yield return null;
            }
        }

        Hide();
    }

    private bool ContinuePressed()
    {
        var keyboard = Keyboard.current;
        return keyboard != null &&
               (keyboard.eKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame);
    }

    private IEnumerator HideAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        panel.SetActive(false);
        hideRoutine = null;
    }
}