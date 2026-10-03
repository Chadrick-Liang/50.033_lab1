using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{

    private float originalX;
    public float maxOffset = 2.0f; // patrol distance each way
    private float enemyPatroltime = 2.0f;
    private int moveRight = -1;
    private Vector2 velocity;

    private Rigidbody2D enemyBody;

    public Vector3 startPosition;

    private Transform player;
    private PlayerMovement playerMovement;

    // how many recorded steps to go back per physics step, 1 = real time (3s rewind takes 3s), 1.5 = 3s rewind takes 2s
    private const float rewindPlaybackSpeed = 1.5f;

    // this goomba's position every physics step, oldest first, trimmed to PlayerMovement.rewindDuration
    private List<Vector2> history = new List<Vector2>();
    private bool isRewinding = false;
    private GameManager gameManager;

    public Animator enemyAnimator;

    // names of the states in Goomba.controller, played directly so the controller needs no parameters or transitions
    private const string idleState = "Goomba";
    private const string dieState = "Goomba-die";
    public float dieDuration = 0.5f; // how long the squashed goomba stays on screen before disappearing

    private Collider2D enemyCollider;
    private bool isDead = false;
    public bool IsDead => isDead;

    void Start()
    {
        enemyBody = GetComponent<Rigidbody2D>();
        enemyCollider = GetComponent<Collider2D>();
        if (enemyAnimator == null) enemyAnimator = GetComponent<Animator>();
        //remember where goomba started
        startPosition = transform.localPosition;
        // get the starting position
        originalX = transform.position.x;
        ComputeVelocity();

        player = GameObject.FindGameObjectWithTag("Player").transform; //get mario's game object to detect collision
        playerMovement = player.GetComponent<PlayerMovement>();
        gameManager = GameObject.FindGameObjectWithTag("Manager").GetComponent<GameManager>();
    }
    void ComputeVelocity()
    {
        velocity = new Vector2((moveRight) * maxOffset / enemyPatroltime, 0);
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
        playerMovement.jumpOverGoomba.AddScore();
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

    // rewind mechanic (playback function), called by PlayerMovement on the goomba Mario touched
    public void Rewind(PlayerMovement playerMovement)
    {
        //will not rewind if nothing is recorded or a rewind is already playing
        if (playerMovement.HistoryCount == 0 || playerMovement.IsRewinding) return;

        //we use a corountine here (a type of unity object that allows a function to run over multiple frames)
        //normally, unity renders frames after executing finish scripts
        StartCoroutine(RewindCoroutine(playerMovement));
    }

    // rewind mechanic (per frame)
    private IEnumerator RewindCoroutine(PlayerMovement playerMovement)
    {
        // rewind every goomba, not just this one, so the others don't walk into Mario's rewound position
        EnemyMovement[] enemies = FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None);

        //set bool to prevent user action during animation
        foreach (EnemyMovement enemy in enemies) enemy.isRewinding = true;
        playerMovement.SetRewinding(true);
        gameManager.RewindStart();

        //play back the recorded steps newest to oldest until Mario's history (3s) runs out
        float stepsToPlay = 0f;
        while (playerMovement.HistoryCount > 0)
        {
            stepsToPlay += rewindPlaybackSpeed; // fractional speeds carry over to the next step
            while (stepsToPlay >= 1f && playerMovement.HistoryCount > 0)
            {
                stepsToPlay -= 1f;
                playerMovement.RewindTo(playerMovement.PopHistory());
                foreach (EnemyMovement enemy in enemies) enemy.StepBack();
            }
            yield return new WaitForFixedUpdate();
        }

        foreach (EnemyMovement enemy in enemies) enemy.EndRewind();
        playerMovement.SetRewinding(false);
        gameManager.RewindEnd();
    }

    // move to the newest recorded position and forget it
    private void StepBack()
    {
        if (history.Count == 0) return;
        enemyBody.position = history[history.Count - 1];
        history.RemoveAt(history.Count - 1);
    }

    private void EndRewind()
    {
        isRewinding = false;
    }
}
