using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Player_Movement))]
public class PlayerMeleeAttack : MonoBehaviour
{
    [Tooltip("Pusty obiekt-dziecko gracza, ustawiony przed mieczem. Odwraca sie razem z graczem.")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRadius = 0.6f;
    [SerializeField] private int damage = 1;

    [Tooltip("Opoznienie od klikniecia do trafienia (dopasuj do momentu uderzenia w animacji)")]
    [SerializeField] private float hitDelay = 0.15f;

    [Tooltip("Warstwy, ktore mozna trafic (np. Enemy)")]
    [SerializeField] private LayerMask damageableLayers;

    private Player_Movement player;

    private void Awake()
    {
        player = GetComponent<Player_Movement>();
    }

    private void OnEnable()
    {
        player.Attacked += OnAttacked;
    }

    private void OnDisable()
    {
        player.Attacked -= OnAttacked;
    }

    private void OnAttacked()
    {
        StartCoroutine(HitRoutine());
    }

    private IEnumerator HitRoutine()
    {
        yield return new WaitForSeconds(hitDelay);

        if (attackPoint == null) yield break;

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, damageableLayers);

        // Ten sam wrog moze miec kilka colliderow, a dostaje tylko jeden cios
        var alreadyHit = new HashSet<IDamageable>();

        foreach (Collider2D hit in hits)
        {
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null || !alreadyHit.Add(target)) continue;

            target.TakeDamage(damage, transform.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }
}