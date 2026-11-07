using System;
using UnityEngine;


public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 3;

    [Tooltip("Po otrzymaniu obrazen przez ten czas kolejne ciosy sa ignorowane")]
    [SerializeField] private float invulnerabilityTime = 0.3f;

    [Tooltip("Wypisuje obrazenia w Console (do testow, potem wylacz)")]
    [SerializeField] private bool debugLog = true;

    public int Current { get; private set; }
    public int Max => maxHealth;
    public bool IsDead { get; private set; }

    public event Action<int, int> Changed;

    public event Action<Vector2> Damaged;

    public event Action Died;

    private float invulnerableUntil;

    private void Awake()
    {
        Current = maxHealth;
    }

    public void TakeDamage(int amount, Vector2 sourcePosition)
    {
        if (IsDead || amount <= 0) return;
        if (Time.time < invulnerableUntil) return;

        invulnerableUntil = Time.time + invulnerabilityTime;
        Current = Mathf.Max(0, Current - amount);

        if (debugLog)
        {
            Debug.Log($"{name} dostaje {amount} obrazen, zdrowie: {Current}/{maxHealth}");
        }

        Changed?.Invoke(Current, maxHealth);
        Damaged?.Invoke(sourcePosition);

        if (Current == 0)
        {
            IsDead = true;
            Died?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;

        Current = Mathf.Min(maxHealth, Current + amount);
        Changed?.Invoke(Current, maxHealth);
    }

    // Do odradzania gracza po smierci
    public void ResetHealth()
    {
        IsDead = false;
        Current = maxHealth;
        Changed?.Invoke(Current, maxHealth);
    }
}