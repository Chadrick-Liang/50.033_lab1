using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
[RequireComponent(typeof(SpringJoint2D), typeof(AudioSource))]
public class QuestionBlock : MonoBehaviour
{
    [Header("Box")]
    public SpriteRenderer visual;
    public Animator boxAnimator;
    public Sprite usedSprite;

    [Header("Coin")]
    public SpriteRenderer coin;
    public Animator coinAnimator;
    public AudioClip coinSound;

    [Header("Spring bounce")]
    [Min(0.1f)] public float bounceSpeed = 4f;
    [Min(0.1f)] public float maximumBounceTime = 1f;

    [Header("Coin pop")]
    public float coinHeight = 1.5f;
    [Min(0.01f)] public float coinDuration = 0.45f;

    private Rigidbody2D boxBody;
    private BoxCollider2D boxCollider;
    private AudioSource audioSource;

    private Vector3 boxStartLocalPosition;
    private Vector3 coinStartLocalPosition;

    private bool used;
    private bool finishingBounce;
    private bool hasRisen;
    private float bounceElapsed;

    // The parent is the stationary spring anchor.
    private Vector2 RestPosition =>
        (Vector2)transform.parent.TransformPoint(boxStartLocalPosition);

    void Awake()
    {
        boxBody = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        audioSource = GetComponent<AudioSource>();

        boxStartLocalPosition = transform.localPosition;
        coinStartLocalPosition = coin.transform.localPosition;

        ResetBlock();
    }

    GameManager gameManager;
    private AnimationEventIntTool coinEvent;

    void Start()
    {
        // observe the GameManager's restart event so this block resets itself
        gameManager = GameObject.FindGameObjectWithTag("Manager").GetComponent<GameManager>();
        gameManager.gameRestart.AddListener(ResetBlock);

        // the coin announces "collected, worth X" and GameManager adds it to the score
        // linked here because a prefab can't reference the scene's GameManager in the inspector
        if (!coin.TryGetComponent(out coinEvent))
        {
            coinEvent = coin.gameObject.AddComponent<AnimationEventIntTool>();
            coinEvent.parameter = 1;
        }
        coinEvent.useInt ??= new UnityEngine.Events.UnityEvent<int>();
        coinEvent.useInt.AddListener(gameManager.IncreaseScore);
    }

    void OnDestroy()
    {
        if (gameManager == null) return;
        gameManager.gameRestart.RemoveListener(ResetBlock);
        coinEvent.useInt.RemoveListener(gameManager.IncreaseScore);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!enabled || used) return;

        PlayerMovement player =
            collision.gameObject.GetComponentInParent<PlayerMovement>();

        if (player == null || !player.alive || player.IsRewinding)
            return;

        bool hitFromBelow = false;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);

            // This callback runs on the moving box.
            if (contact.normal.y > 0.5f &&
                contact.point.y < boxCollider.bounds.center.y)
            {
                hitFromBelow = true;
                break;
            }
        }

        if (!hitFromBelow) return;

        // Reject further activations immediately.
        used = true;

        boxAnimator.enabled = false;
        visual.sprite = usedSprite;

        // Give the physics bounce a consistent starting speed.
        // The SpringJoint2D controls the movement after this.
        boxBody.linearVelocity = Vector2.up * bounceSpeed;
        boxBody.WakeUp();

        finishingBounce = true;
        hasRisen = false;
        bounceElapsed = 0f;

        StartCoroutine(PopCoin());

        if (coinSound != null)
            audioSource.PlayOneShot(coinSound);
    }

    void FixedUpdate()
    {
        if (!finishingBounce) return;

        bounceElapsed += Time.fixedDeltaTime;

        float heightAboveStart = boxBody.position.y - RestPosition.y;

        if (heightAboveStart > 0.01f)
            hasRisen = true;

        bool returned =
            hasRisen &&
            heightAboveStart <= 0.01f &&
            boxBody.linearVelocity.y <= 0f;

        // Stop on the first return so the spring cannot keep oscillating.
        // The timeout also handles a bounce obstructed by another object.
        if (returned || bounceElapsed >= maximumBounceTime)
            FinishBounce();
    }

    private void FinishBounce()
    {
        finishingBounce = false;

        boxBody.linearVelocity = Vector2.zero;
        boxBody.angularVelocity = 0f;
        boxBody.position = RestPosition;

        // Keep the solid collider, but prevent further physical movement.
        boxBody.bodyType = RigidbodyType2D.Static;
    }

    IEnumerator PopCoin()
    {
        coin.transform.localPosition = coinStartLocalPosition;

        coinAnimator.enabled = true;
        coinAnimator.Play("CoinSpin", 0, 0f);
        coinAnimator.Update(0f);
        coin.enabled = true;

        for (float elapsed = 0f;
             elapsed < coinDuration;
             elapsed += Time.deltaTime)
        {
            float progress = elapsed / coinDuration;

            float offset =
                4f * coinHeight * progress * (1f - progress);

            coin.transform.localPosition =
                coinStartLocalPosition + Vector3.up * offset;

            yield return null;
        }

        coin.transform.localPosition = coinStartLocalPosition;
        coin.enabled = false;
        coinAnimator.enabled = false;

        coinEvent.TriggerIntEvent();
    }

    public void ResetBlock()
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

        coin.transform.localPosition = coinStartLocalPosition;
        coin.enabled = false;
        coinAnimator.enabled = false;

        audioSource.Stop();

        boxAnimator.enabled = true;
        boxAnimator.Play("QuestionBlink", 0, 0f);
        boxAnimator.Update(0f);
    }
}