using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 3f;
    public int damage = 1;

    private Vector2 direction;

    private void Start()
    {
        Destroy(gameObject, lifeTime); // st�ela zmiz� po ur�it� dob�
    }

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
    }

    private void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime);

        // voliteln�: oto�en� sprite sm�rem pohybu
        if (direction.x < 0)
            transform.localScale = new Vector3(-1, 1, 1);
        else
            transform.localScale = new Vector3(1, 1, 1);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // sem d� logiku, co se stane p�i z�sahu (damage hr��e, zni�en� projektilu, atd.)
        if (collision.CompareTag("Player"))
        {
            // nap�. vol�n� damage na hr��e
            // collision.GetComponent<PlayerHealth>()?.TakeDamage(damage);

            Destroy(gameObject); // zni�� projektil po z�sahu
        }

        if (collision.CompareTag("Ground")) // aby se st�ela zni�ila o zem
        {
            Destroy(gameObject);
        }
    }
}
