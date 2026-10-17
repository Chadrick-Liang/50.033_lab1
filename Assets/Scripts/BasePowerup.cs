using UnityEngine;
using UnityEngine.Events;


public abstract class BasePowerup : MonoBehaviour, IPowerup
{
    public PowerupType type;
    public bool spawned = false;
    protected bool consumed = false;
    protected bool goRight = true;
    protected Rigidbody2D rigidBody;
    private Vector3 startLocalPosition;
    private bool initialized;

    // raised when this powerup is collected, link to OnPowerupCollected's Raise (Dynamic IPowerup) in the Inspector
    public UnityEvent<IPowerup> powerupCollected;

    protected virtual void Awake()
    {
        InitializePowerup();
    }

    protected void InitializePowerup()
    {
        if (initialized) return;
        initialized = true;
        rigidBody = GetComponent<Rigidbody2D>();
        startLocalPosition = transform.localPosition;
    }

    public virtual void ResetPowerup()
    {
        // A box can reset its child before that child's Awake has run.
        InitializePowerup();
        StopAllCoroutines();
        spawned = false;
        consumed = false;
        goRight = true;
        transform.localPosition = startLocalPosition;
        if (rigidBody != null)
        {
            rigidBody.linearVelocity = Vector2.zero;
            rigidBody.angularVelocity = 0f;
            rigidBody.simulated = false;
            rigidBody.position = transform.position;
        }
        gameObject.SetActive(true);
    }

    // base methods
    protected virtual void Start()
    {
        rigidBody = GetComponent<Rigidbody2D>();
    }

    // interface methods
    // 1. concrete methods
    public PowerupType powerupType
    {
        get // getter
        {
            return type;
        }
    }

    public bool hasSpawned
    {
        get // getter
        {
            return spawned;
        }
    }

    public virtual void DestroyPowerup()
    {
        Destroy(this.gameObject);
    }

    // 2. abstract methods, must be implemented by derived classes
    public abstract void SpawnPowerup();
    public abstract void ApplyPowerup(MonoBehaviour i);
}
