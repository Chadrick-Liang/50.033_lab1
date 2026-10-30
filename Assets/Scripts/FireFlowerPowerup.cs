using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class FireFlowerPowerup : BasePowerup
{
    protected override void Awake()
    {
        base.Awake();
        type = PowerupType.FireFlower;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!spawned || consumed)
            return;

        PlayerMovement player = collision.gameObject.GetComponentInParent<PlayerMovement>();
        if (player != null)
        {
            if (!player.alive || player.IsRewinding) return;
            powerupCollected.Invoke(this); // PowerupManager routes it back to Mario's RequestPowerupEffect
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

    // Called by the final frame of the fire flower emergence animation.
    public override void SpawnPowerup()
    {
        if (spawned)
        {
            return;
        }

        spawned = true;

        Animator animator = GetComponent<Animator>();

        if (animator != null)
        {
            animator.enabled = false;
        }

        rigidBody.simulated = true;
        rigidBody.bodyType = RigidbodyType2D.Kinematic;
        rigidBody.gravityScale = 0f;
        rigidBody.linearVelocity = Vector2.zero;
        rigidBody.angularVelocity = 0f;
    }

    public override void ApplyPowerup(
    MonoBehaviour recipient)
    {
        if (!spawned ||
            consumed ||
            !(recipient is PlayerMovement player))
        {
            return;
        }

        if (!player.alive || player.IsRewinding)
        {
            return;
        }

        consumed = true;

        MarioStateController marioController;

        bool foundController =
            recipient.TryGetComponent<MarioStateController>(
                out marioController);

        if (foundController)
        {
            marioController.SetPowerup(
                this.powerupType);
        }
    }

    public override void DestroyPowerup()
    {
        gameObject.SetActive(false);
    }

    public override void ResetPowerup()
    {
        base.ResetPowerup();
        type = PowerupType.FireFlower;
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
