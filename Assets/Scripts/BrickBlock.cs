using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D), typeof(AudioSource))]
public class BrickBlock : MonoBehaviour
{
    [Header("References")]
    public Transform visual;
    public SpriteRenderer coin;
    public AudioClip coinSound;

    [Header("Brick variant")]
    public bool containsCoin = false;

    [Header("Bounce")]
    public float bounceHeight = 0.2f;
    [Min(0.01f)] public float bounceDuration = 0.18f;

    [Header("Coin pop")]
    public float coinHeight = 1.5f;
    [Min(0.01f)] public float coinDuration = 0.45f;

    private AudioSource audioSource;
    private BoxCollider2D brickCollider;
    private Vector3 visualStartPosition;
    private Vector3 coinStartPosition;

    private bool bouncing;
    private bool coinAvailable;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        brickCollider = GetComponent<BoxCollider2D>();

        visualStartPosition = visual.localPosition;
        coinStartPosition = coin.transform.localPosition;

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
        if (!enabled || bouncing) return;

        PlayerMovement player =
            collision.gameObject.GetComponentInParent<PlayerMovement>();

        if (player == null || !player.alive) return;

        bool hitFromBelow = false;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);

            // This callback runs on the brick:
            // Mario underneath produces an upward-facing normal.
            if (contact.normal.y > 0.5f &&
                contact.point.y < brickCollider.bounds.center.y)
            {
                hitFromBelow = true;
                break;
            }
        }

        if (!hitFromBelow) return;

        StartCoroutine(Bounce());

        if (coinAvailable)
        {
            coinAvailable = false;

            if (coinSound != null)
                audioSource.PlayOneShot(coinSound);

            StartCoroutine(PopCoin());
        }
    }

    IEnumerator Bounce()
    {
        bouncing = true;

        for (float elapsed = 0;
             elapsed < bounceDuration;
             elapsed += Time.deltaTime)
        {
            float progress = elapsed / bounceDuration;
            float offset = Mathf.Sin(progress * Mathf.PI) * bounceHeight;

            visual.localPosition =
                visualStartPosition + Vector3.up * offset;

            yield return null;
        }

        visual.localPosition = visualStartPosition;
        bouncing = false;
    }

    IEnumerator PopCoin()
    {
        coin.enabled = true;

        for (float elapsed = 0;
             elapsed < coinDuration;
             elapsed += Time.deltaTime)
        {
            float progress = elapsed / coinDuration;

            // A simple upward-and-downward arc
            float offset =
                4f * coinHeight * progress * (1f - progress);

            coin.transform.localPosition =
                coinStartPosition + Vector3.up * offset;

            yield return null;
        }

        coin.enabled = false;
        coin.transform.localPosition = coinStartPosition;

        coinEvent.TriggerIntEvent();
    }

    public void ResetBlock()
    {
        StopAllCoroutines();

        bouncing = false;
        coinAvailable = containsCoin;

        visual.localPosition = visualStartPosition;
        coin.transform.localPosition = coinStartPosition;
        coin.enabled = false;

        audioSource.Stop();
    }
}