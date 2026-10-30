using System.Collections;
using UnityEngine;

public class FireballController : MonoBehaviour
{
    [SerializeField]
    private float scaleSpeed = 1.0f;

    [SerializeField]
    [Min(0.1f)]
    private float bounceHeight = 0.75f;

    private Rigidbody2D fireballBody;

    void Start()
    {
        fireballBody = GetComponent<Rigidbody2D>();
        StartCoroutine(ScaleAndDestroyCoroutine());
        GetComponent<AudioSource>().Play();
    }

    private IEnumerator ScaleAndDestroyCoroutine()
    {
        yield return new WaitForSecondsRealtime(2);

        while (transform.localScale.x > 0.01f)
        {
            transform.localScale -=
                Vector3.one * scaleSpeed * Time.deltaTime;

            yield return null;
        }

        transform.localScale = Vector3.zero;
        Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (TryHitEnemy(collision.gameObject))
        {
            return;
        }

        if (fireballBody == null)
        {
            return;
        }

        bool landedOnSurface = false;
        bool hitVerticalEdge = false;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);

            // A mostly horizontal normal means the ball hit a wall or edge
            if (Mathf.Abs(contact.normal.x) > 0.5f)
            {
                hitVerticalEdge = true;
            }

            // A mostly upward normal means the ball landed on a surface
            if (contact.normal.y > 0.5f)
            {
                landedOnSurface = true;
            }
        }

        if (hitVerticalEdge)
        {
            Destroy(gameObject);
            return;
        }

        if (landedOnSurface)
        {
            float gravity =
                Mathf.Abs(Physics2D.gravity.y) *
                fireballBody.gravityScale;

            float bounceVelocity =
                Mathf.Sqrt(2f * gravity * bounceHeight);

            fireballBody.linearVelocity = new Vector2(
                fireballBody.linearVelocity.x,
                bounceVelocity);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryHitEnemy(other.gameObject);
    }

    private bool TryHitEnemy(GameObject hitObject)
    {
        EnemyMovement enemy =
            hitObject.GetComponentInParent<EnemyMovement>();

        if (enemy == null)
        {
            return false;
        }

        enemy.HitByFireball();
        Destroy(gameObject);
        return true;
    }
}
