using System.Collections;
using TMPro;
using UnityEngine;

public class DiamondHUD : MonoBehaviour
{
    [Header("Elementy HUD")]
    [Tooltip("Panel z ikona i tekstem. Wymagany tylko, gdy Always Visible jest wylaczone.")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text counterText;

    [Header("Wyswietlanie")]
    [Tooltip("Zaznaczone: licznik jest widoczny caly czas. Odznaczone: pojawia sie na kilka sekund po zebraniu diamentu (jak w GDD).")]
    [SerializeField] private bool alwaysVisible = true;
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private string format = "x{0}";

    private Coroutine hideRoutine;

    private void Awake()
    {
        if (!alwaysVisible && panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void Start()
    {
        if (DiamondCounter.Instance == null)
        {
            Debug.LogWarning("DiamondHUD: brak DiamondCounter w scenie.");
            return;
        }

        DiamondCounter.Instance.Changed += OnChanged;
        UpdateText(DiamondCounter.Instance.Count);
    }

    private void OnDestroy()
    {
        if (DiamondCounter.Instance != null)
        {
            DiamondCounter.Instance.Changed -= OnChanged;
        }
    }

    private void OnChanged(int count)
    {
        UpdateText(count);

        if (alwaysVisible || panel == null) return;

        panel.SetActive(true);
        if (hideRoutine != null) StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private void UpdateText(int count)
    {
        counterText.text = string.Format(format, count);
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        panel.SetActive(false);
        hideRoutine = null;
    }
}