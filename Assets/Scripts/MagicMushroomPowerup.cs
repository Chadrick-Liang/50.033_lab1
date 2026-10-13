using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class MagicMushroomPowerup : BasePowerup
{
    protected override void Start()
    {
        base.Start();
        type = PowerupType.MagicMushroom;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!spawned || consumed)
            return;

        if (collision.gameObject.CompareTag("Player"))
        {
            consumed = true;
            ApplyPowerup(collision.gameObject.GetComponentInParent<MonoBehaviour>());
            DestroyPowerup();
        }
        else if (collision.gameObject.layer == 10)
        {
            goRight = !goRight;
            rigidBody.AddForce(
                Vector2.right * 3f * (goRight ? 1f : -1f),
                ForceMode2D.Impulse);
        }
    }

    // Called by the final frame of the mushroom emergence animation.
    public override void SpawnPowerup()
    {
        if (spawned)
            return;

        spawned = true;

        Animator animator = GetComponent<Animator>();
        if (animator != null)
            animator.enabled = false;

        rigidBody.simulated = true;
        rigidBody.bodyType = RigidbodyType2D.Dynamic;
        rigidBody.linearVelocity = Vector2.zero;
        rigidBody.AddForce(Vector2.right * 3f, ForceMode2D.Impulse);
    }

    public override void ApplyPowerup(MonoBehaviour recipient)
    {
        // To do
    }
}
