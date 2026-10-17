using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemyMovement : MonoBehaviour
{
    public GameConstants gameConstants;

    private float originalX;
    private float maxOffset; // patrol distance each way
    private bool hasMaxOffsetOverride;

    public bool turnAtObstacles = false;
    public LayerMask obstacleLayers;
    private float enemyPatrolTime;
    private int moveRight = -1;
    private Vector2 velocity;

    private Rigidbody2D enemyBody;

    public Vector3 startPosition;

    private Transform player;
    private PlayerMovement playerMovement;

    // this goomba's position every physics step, oldest first, trimmed to PlayerMovement.rewindDuration
    private List<Vector2> history = new List<Vector2>();
    private bool isRewinding = false;

    public Animator enemyAnimator;

    // names of the states in Goomba.controller, played directly so the controller needs no parameters or transitions
    private const string idleState = "Goomba";
    private const string dieState = "Goomba-die";
    public float dieDuration = 0.5f; // how long the squashed goomba stays on screen before disappearing

    // AudioSource on this goomba, set its Output to the mixer's Enemies group
    public AudioSource enemyAudio;
    public AudioClip stompClip;

    // raised when Mario stomps this goomba, link to OnIncrementScore's Raise (Dynamic int) in the Inspector
    public int stompScore = 1;
    public UnityEvent<int> stomped;

    private Collider2D enemyCollider;
    private bool isDead = false;
    public bool IsDead => isDead;

    void Start()
    {
        if (!hasMaxOffsetOverride)
            maxOffset = gameConstants.goombaMaxOffset;
        enemyPatrolTime = gameConstants.goombaPatrolTime;
        enemyBody = GetComponent<Rigidbody2D>();
        enemyCollider = GetComponent<Collider2D>();
        if (enemyAnimator == null) enemyAnimator = GetComponent<Animator>();
        if (enemyAudio == null) enemyAudio = GetComponent<AudioSource>();
        //remember where goomba started
        startPosition = transform.localPosition;
        // get the starting position
        originalX = transform.position.x;
        ComputeVelocity();

        player = GameObject.FindGameObjectWithTag("Player").transform; //get mario's game object to detect collision
        playerMovement = player.GetComponent<PlayerMovement>();
    }

    public void SetMaxOffset(float value)
    {
        maxOffset = value;
        hasMaxOffsetOverride = true;
    }
    void ComputeVelocity()
    {
        velocity = new Vector2((moveRight) * maxOffset / enemyPatrolTime, 0);
    }
    void Movegoomba()
    {
        enemyBody.MovePosition(enemyBody.position + velocity * Time.fixedDeltaTime);
    }

    void FixedUpdate()
    {
        if (isDead) return; // squashed, waiting to disappear
        if (isRewinding) return; // frozen while the rewind coroutine drives our position

        // turn around at either end of the patrol, based on which end we're past so a rewind can't leave us stuck flipping
        float offset = enemyBody.position.x - originalX;
        if (offset >= maxOffset && moveRight == 1 || offset <= -maxOffset && moveRight == -1)
        {
            // change direction
            moveRight *= -1;
            ComputeVelocity();
        }
        else if (turnAtObstacles)
        {
            float lookAhead = enemyCollider.bounds.extents.x
                + Mathf.Abs(velocity.x) * Time.fixedDeltaTime
                + 0.1f;

            RaycastHit2D hit = Physics2D.Raycast(
                (Vector2)enemyCollider.bounds.center,
                Vector2.right * moveRight,
                lookAhead,
                obstacleLayers
            );

            if (hit.collider != null)
            {
                moveRight *= -1;
                ComputeVelocity();
            }
            else
            {
                Vector2 direction = Vector2.right * moveRight;
                Vector2 rayStart = (Vector2)enemyCollider.bounds.center
                    + direction * (enemyCollider.bounds.extents.x + 0.01f);
                float enemyLookAhead = Mathf.Abs(velocity.x) * Time.fixedDeltaTime + 0.1f;

                RaycastHit2D enemyHit = Physics2D.Raycast(
                    rayStart,
                    direction,
                    enemyLookAhead,
                    LayerMask.GetMask("Enemies")
                );

                if (enemyHit.collider != null &&
                    enemyHit.collider != enemyCollider &&
                    enemyHit.collider.GetComponent<EnemyMovement>() != null)
                {
                    moveRight *= -1;
                    ComputeVelocity();
                }
            }
        }
        Movegoomba();
        RecordHistory();
    }

    // called by PlayerMovement when Mario lands on top of this goomba
    public void Stomp()
    {
        if (isDead) return;
        isDead = true;
        enemyCollider.enabled = false; // so the squashed goomba can't hurt Mario
        enemyAnimator.Play(dieState, 0, 0f);
        if (enemyAudio != null && stompClip != null) enemyAudio.PlayOneShot(stompClip);
        stomped.Invoke(stompScore);
        StartCoroutine(DisappearAfterDeath());
    }

    private IEnumerator DisappearAfterDeath()
    {
        yield return new WaitForSeconds(dieDuration);
        gameObject.SetActive(false); // not destroyed, so GameRestart can bring it back
    }

    //rewind mechanic (recording function), one snapshot per physics step, same as PlayerMovement
    private void RecordHistory()
    {
        history.Add(enemyBody.position);
        //drops any snapshots older than the rewind duration
        while (history.Count > playerMovement.MaxHistoryCount)
        {
            history.RemoveAt(0);
        }
    }

    public void GameRestart()
    {
        // revive if it was stomped
        StopAllCoroutines();
        isDead = false;
        isRewinding = false;
        gameObject.SetActive(true);
        enemyCollider.enabled = true;
        enemyAnimator.Play(idleState, 0, 0f);

        transform.localPosition = startPosition;
        originalX = transform.position.x;
        moveRight = -1;
        ComputeVelocity();
        ResetHistory();
    }

    public void ResetHistory()
    {
        history.Clear();
    }

    // rewind mechanic, called through the OnRewindStart / OnRewindStep / OnRewindEnd GameEventListeners on this goomba
    public void BeginRewind()
    {
        isRewinding = true; // frozen while Mario's rewind coroutine plays
    }

    // move to the newest recorded position and forget it
    public void StepBack()
    {
        if (history.Count == 0) return;
        enemyBody.position = history[history.Count - 1];
        history.RemoveAt(history.Count - 1);
    }

    public void EndRewind()
    {
        isRewinding = false;
    }
}
