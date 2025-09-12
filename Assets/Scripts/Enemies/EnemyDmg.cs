using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDmg : MonoBehaviour
{
    [SerializeField] private float damage;
    [SerializeField] private Health playerHealth; // Reference to the player health component

    private Enemy enemy; // Reference to the Enemy script

    private void Start()
    {
        // Ensure the Enemy script is attached to the same GameObject
        enemy = GetComponent<Enemy>();

        if (enemy == null)
        {
            Debug.LogError("Enemy script not found on the same GameObject! Please attach the Enemy script.");
        }

        // Ensure the playerHealth is assigned in the Inspector
        if (playerHealth == null)
        {
            Debug.LogError("PlayerHealth is not assigned. Please assign it in the Inspector.");
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if the enemy is not null, is not dead, and playerHealth is valid before applying damage
        if (collision.gameObject.CompareTag("Player"))
        {
            if (enemy != null && !enemy.IsDead() && playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
            else
            {
                Debug.LogError("Either enemy is dead or player health is missing.");
            }
        }
    }
}
