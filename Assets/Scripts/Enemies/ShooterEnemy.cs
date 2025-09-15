using UnityEngine;

public class ShooterEnemy : MonsterPatrol
{
    [Header("Shooter Settings")]
    public Transform playerTransform;
    public float shootingDistance = 5f;
    public float shootCooldown = 2f;
    public GameObject projectilePrefab;
    public Transform firePoint;

    private float lastShootTime = -Mathf.Infinity;

    protected override void Update()
    {
        if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) < shootingDistance)
        {
            FacePlayer();

            if (Time.time >= lastShootTime + shootCooldown)
            {
                Shoot();
                lastShootTime = Time.time;
            }

            if (anim != null)
                anim.SetBool("isShooting", true);
        }
        else
        {
            base.Update(); // Patrol
            if (anim != null)
                anim.SetBool("isShooting", false);
        }
    }

    private void FacePlayer()
    {
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        if (direction.x < 0)
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        else
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
    }

    private void Shoot()
    {
        if (projectilePrefab != null && firePoint != null)
        {
            GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

            // pošleme smìrový vektor do støely
            Vector2 direction = (playerTransform.position - firePoint.position).normalized;

            Projectile projectile = proj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.SetDirection(direction);
            }
        }
    }
}
