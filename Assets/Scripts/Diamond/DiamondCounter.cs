using System;
using UnityEngine;

// Licznik diamentow na poziomie. Dodaj do pustego obiektu w scenie.
public class DiamondCounter : MonoBehaviour
{
    public static DiamondCounter Instance { get; private set; }

    [SerializeField] private int requiredCount = 5;

    public int Count { get; private set; }
    public int RequiredCount => requiredCount;
    public bool HasEnough => Count >= requiredCount;

    // Wywolywane przy kazdej zmianie liczby diamentow
    public event Action<int> Changed;

    // Wywolywane raz, gdy zebrano wymagana liczbe (np. do otwarcia drzwi)
    public event Action AllCollected;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Add(int amount = 1)
    {
        bool hadEnough = HasEnough;

        Count += amount;
        Changed?.Invoke(Count);

        if (!hadEnough && HasEnough)
        {
            AllCollected?.Invoke();
        }
    }
}