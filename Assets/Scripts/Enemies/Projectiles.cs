using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 3f;
    public int damage = 1;

    private Vector2 direction;

    private void Start()
    {
        Destroy(gameObject, lifeTime); // støela zmizí po urèité dobì
    }

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
    }

    private void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime);

        // volitelnì: otoèení sprite smìrem pohybu
        if (direction.x < 0)
            transform.localScale = new Vector3(-1, 1, 1);
        else
            transform.localScale = new Vector3(1, 1, 1);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // sem dáš logiku, co se stane pøi zásahu (damage hráèe, znièení projektilu, atd.)
        if (collision.CompareTag("Player"))
        {
            // napø. volání damage na hráèe
            // collision.GetComponent<PlayerHealth>()?.TakeDamage(damage);

            Destroy(gameObject); // znièí projektil po zásahu
        }

        if (collision.CompareTag("Ground")) // aby se støela znièila o zem
        {
            Destroy(gameObject);
        }
    }
}
