using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class MagicMushroomPowerup : BasePowerup
{
    protected override void Awake()
    {
        base.Awake();
        type = PowerupType.MagicMushroom;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!spawned || consumed)
            return;

        PlayerMovement player = collision.gameObject.GetComponentInParent<PlayerMovement>();
        if (player != null)
        {
            if (!player.alive || player.IsRewinding) return;
            player.RequestPowerupEffect(this);
            if (consumed) DestroyPowerup();
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
        if (!spawned || consumed || !(recipient is PlayerMovement player)) return;
        if (!player.alive || player.IsRewinding) return;
        consumed = true;
        player.BecomeSuperMario();
    }

    public override void DestroyPowerup()
    {
        gameObject.SetActive(false);
    }

    public override void ResetPowerup()
    {
        base.ResetPowerup();
        type = PowerupType.MagicMushroom;
        GetComponent<SpriteRenderer>().enabled = true;
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = true;
            animator.ResetTrigger("spawned");
            animator.Play("Hidden", 0, 0f);
            animator.Update(0f);
        }
    }
}
