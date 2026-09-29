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
    public GameObject rewindPanel;

    // which side of this goomba Mario was on last step (1 = right, -1 = left), used to detect jumping over it
    private float lastPlayerSide;

    void Start()
    {
        enemyBody = GetComponent<Rigidbody2D>();
        //remember where goomba started
        startPosition = transform.localPosition;
        // get the starting position
        originalX = transform.position.x;
        ComputeVelocity();

        player = GameObject.FindGameObjectWithTag("Player").transform; //get mario's game object to detect collision
        playerMovement = player.GetComponent<PlayerMovement>();
        lastPlayerSide = PlayerSide();

        rewindPanel.SetActive(false); // only shown while the rewind animation plays
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
        CheckJumpedOver();
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

    public void ResetHistory()
    {
        history.Clear();
        if (enemyBody != null) lastPlayerSide = PlayerSide();
    }

    private float PlayerSide()
    {
        return player.position.x >= transform.position.x ? 1f : -1f;
    }

    // score when Mario crosses from one side of this goomba to the other while in the air above it
    private void CheckJumpedOver()
    {
        float side = PlayerSide();
        if (side != lastPlayerSide && !playerMovement.IsGrounded && player.position.y > enemyBody.position.y + 0.5f)
        {
            playerMovement.jumpOverGoomba.AddScore();
        }
        lastPlayerSide = side;
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
        rewindPanel.SetActive(true);

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
        rewindPanel.SetActive(false);
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
        lastPlayerSide = PlayerSide(); // Mario teleported, don't count that as jumping over us
    }
}
