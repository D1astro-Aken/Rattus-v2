using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapDmg : MonoBehaviour
{
    [SerializeField] private float damage;
    [SerializeField] private Health playerHealth;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
        }
    }
}