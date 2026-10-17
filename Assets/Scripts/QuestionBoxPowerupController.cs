using UnityEngine;

[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
[RequireComponent(typeof(SpringJoint2D), typeof(AudioSource))]
public class QuestionBoxPowerupController : MonoBehaviour, IPowerupController
{
    [Header("Box")]
    public SpriteRenderer visual;
    public Animator boxAnimator;
    public Sprite usedSprite;

    [Header("Power-up")]
    public Animator powerupAnimator;
    public BasePowerup powerup;
    public AudioClip powerupSound;

    [Header("Spring bounce")]
    [Min(0.1f)] public float bounceSpeed = 4f;
    [Min(0.1f)] public float maximumBounceTime = 1f;

    private Rigidbody2D boxBody;
    private BoxCollider2D boxCollider;
    private AudioSource audioSource;

    private Vector3 boxStartLocalPosition;
    private bool used;
    private bool finishingBounce;
    private bool hasRisen;
    private float bounceElapsed;

    private Vector2 RestPosition =>
        transform.parent != null
            ? (Vector2)transform.parent.TransformPoint(boxStartLocalPosition)
            : (Vector2)boxStartLocalPosition;

    void Awake()
    {
        boxBody = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        audioSource = GetComponent<AudioSource>();

        boxStartLocalPosition = transform.localPosition;

        ResetBox();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!enabled || used || powerup == null || powerup.hasSpawned)
            return;

        PlayerMovement player =
            collision.gameObject.GetComponentInParent<PlayerMovement>();

        if (player == null || !player.alive || player.IsRewinding)
            return;

        bool hitFromBelow = false;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);

            if (contact.normal.y > 0.5f &&
                contact.point.y < boxCollider.bounds.center.y)
            {
                hitFromBelow = true;
                break;
            }
        }

        if (!hitFromBelow)
            return;

        used = true;

        if (boxAnimator != null)
            boxAnimator.enabled = false;

        if (visual != null && usedSprite != null)
            visual.sprite = usedSprite;

        boxBody.linearVelocity = Vector2.up * bounceSpeed;
        boxBody.WakeUp();

        finishingBounce = true;
        hasRisen = false;
        bounceElapsed = 0f;

        if (powerupAnimator != null)
            powerupAnimator.SetTrigger("spawned");
        else
            powerup.SpawnPowerup();

        if (powerupSound != null)
            audioSource.PlayOneShot(powerupSound);
    }

    void FixedUpdate()
    {
        if (!finishingBounce)
            return;

        bounceElapsed += Time.fixedDeltaTime;

        float heightAboveStart = boxBody.position.y - RestPosition.y;

        if (heightAboveStart > 0.01f)
            hasRisen = true;

        bool returned =
            hasRisen &&
            heightAboveStart <= 0.01f &&
            boxBody.linearVelocity.y <= 0f;

        if (returned || bounceElapsed >= maximumBounceTime)
            Disable();
    }

    public void Disable()
    {
        finishingBounce = false;
        boxBody.linearVelocity = Vector2.zero;
        boxBody.angularVelocity = 0f;
        boxBody.position = RestPosition;
        boxBody.bodyType = RigidbodyType2D.Static;
    }

    // listens to OnGameRestart
    public void ResetBox()
    {
        StopAllCoroutines();

        used = false;
        finishingBounce = false;
        hasRisen = false;
        bounceElapsed = 0f;

        boxBody.bodyType = RigidbodyType2D.Dynamic;
        boxBody.position = RestPosition;
        boxBody.linearVelocity = Vector2.zero;
        boxBody.angularVelocity = 0f;

        if (boxAnimator != null)
        {
            boxAnimator.enabled = true;
            boxAnimator.Play("QuestionBlink", 0, 0f);
            boxAnimator.Update(0f);
        }

        if (powerup != null)
            powerup.ResetPowerup();

        audioSource.Stop();
    }
}
