using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D), typeof(AudioSource))]
public class BrickPowerupController : MonoBehaviour, IPowerupController
{
    public Transform visual;
    public BasePowerup powerup;
    public AudioClip coinSound;

    [Header("Brick variant")]
    public bool containsCoin;
    public bool breakableBySuperMario = true;

    [Header("Bounce")]
    public float bounceHeight = 0.2f;
    [Min(0.01f)] public float bounceDuration = 0.18f;

    private AudioSource audioSource;
    private BoxCollider2D brickCollider;
    private GameManager gameManager;
    private Vector3 visualStartPosition;
    private bool bouncing;
    private bool contentsAvailable;
    private bool broken;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        brickCollider = GetComponent<BoxCollider2D>();
        visualStartPosition = visual.localPosition;
        ResetBlock();
    }

    private void Start()
    {
        gameManager = GameManager.instance;
        if (gameManager != null) gameManager.gameRestart.AddListener(ResetBlock);
    }

    private void OnDestroy()
    {
        if (gameManager != null) gameManager.gameRestart.RemoveListener(ResetBlock);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!enabled || bouncing || broken) return;
        PlayerMovement player = collision.gameObject.GetComponentInParent<PlayerMovement>();
        if (player == null || !player.alive || player.IsRewinding) return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);
            if (contact.normal.y > 0.5f && contact.point.y < brickCollider.bounds.center.y)
            {
                Hit(player);
                return;
            }
        }
    }

    private void Hit(PlayerMovement player)
    {
        // A full brick pays out first; Super Mario can break it on a later hit.
        if (contentsAvailable)
        {
            Disable();
            powerup.SpawnPowerup();
            if (coinSound != null) audioSource.PlayOneShot(coinSound);
        }
        else if (breakableBySuperMario && player.IsSuperMario)
        {
            broken = true;
            brickCollider.enabled = false;
            visual.gameObject.SetActive(false);
            return;
        }

        StartCoroutine(Bounce());
    }

    private IEnumerator Bounce()
    {
        bouncing = true;
        for (float elapsed = 0f; elapsed < bounceDuration; elapsed += Time.deltaTime)
        {
            float offset = Mathf.Sin(elapsed / bounceDuration * Mathf.PI) * bounceHeight;
            visual.localPosition = visualStartPosition + Vector3.up * offset;
            yield return null;
        }
        visual.localPosition = visualStartPosition;
        bouncing = false;
    }

    public void Disable()
    {
        // Exhaust the contents, but leave the brick solid and potentially breakable.
        contentsAvailable = false;
    }

    public void ResetBlock()
    {
        StopAllCoroutines();
        bouncing = false;
        broken = false;
        contentsAvailable = containsCoin && powerup != null;
        brickCollider.enabled = true;
        visual.gameObject.SetActive(true);
        visual.localPosition = visualStartPosition;
        if (powerup != null) powerup.ResetPowerup();
        audioSource.Stop();
    }
}
