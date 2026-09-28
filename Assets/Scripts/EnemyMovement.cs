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
    private const float rewindDuration = 3.0f;

    private const float rewindPlaybackDuration = 2.0f;

    private struct PositionSnapshot
    {
        public float time;
        public Vector2 enemyPosition;
        public Vector2 playerPosition;
    }
    private Queue<PositionSnapshot> history = new Queue<PositionSnapshot>();
    private bool isRewinding = false;
    public GameObject rewindPanel;

    void Start()
    {
        enemyBody = GetComponent<Rigidbody2D>();
        //remember where goomba started
        startPosition = transform.localPosition;
        // get the starting position
        originalX = transform.position.x;
        ComputeVelocity();

        player = GameObject.FindGameObjectWithTag("Player").transform; //get mario's game object to detect collision

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

    // note that this is Update(), which still works but not ideal. See below.
    void FixedUpdate()
    {
        if (isRewinding) return; // frozen while the rewind coroutine drives our position

        if (Mathf.Abs(enemyBody.position.x - originalX) < maxOffset)
        {// move goomba
            Movegoomba();
        }
        else
        {
            // change direction
            moveRight *= -1;
            ComputeVelocity();
            Movegoomba();
        }

        RecordHistoryIfPlayerInRange();
    }

    //rewind mechanic (recording function)
    private void RecordHistoryIfPlayerInRange()
    {
        if (player == null) return;

        float distance = Vector2.Distance(enemyBody.position, player.position);
        if (distance <= maxOffset) //if mario's distance < goomba's patrol radius
        {
            history.Enqueue(new PositionSnapshot //uses a queue mechanic to record player pos, enemy for a set durstion
            {
                time = Time.time,
                enemyPosition = enemyBody.position,
                playerPosition = player.position
            });
            //drops any snapshorts longer than the rewind duration
            while (history.Count > 0 && Time.time - history.Peek().time > rewindDuration)
            {
                history.Dequeue();
            }
        }
    }

    // rewind mechanic (playback function)
    public void Rewind(PlayerMovement playerMovement)
    {
        //will not rewind if no past history is recorded and rewind not triggered yet
        if (history.Count == 0 || isRewinding) return;

        //we use a corountine here (a type of unity object that allows a function to run over multiple frames)
        //normally, unity renders frames after executing finish scripts
        StartCoroutine(RewindCoroutine(playerMovement));
    }

    // rewind mechanic (per frame)
    private IEnumerator RewindCoroutine(PlayerMovement playerMovement)
    {
        //convert our queue to a array for easier indexing access
        PositionSnapshot[] snapshots = history.ToArray();
        //clear the queue to prevent edge cases like 2nd rewind containing the 1st rewind timing etc
        history.Clear();

        //set bool to prevent user action during animation
        isRewinding = true;
        playerMovement.SetRewinding(true);
        rewindPanel.SetActive(true);

        //play per frame rewind
        float elapsed = 0f;

        while (elapsed < rewindPlaybackDuration)
        {
            // Progress from 0 at the start to 1 at the end.
            float progress = Mathf.Clamp01(elapsed / rewindPlaybackDuration);

            // Travel backwards from the newest recording to the oldest.
            float index = (snapshots.Length - 1) * (1f - progress);

            int lower = Mathf.FloorToInt(index);
            int upper = Mathf.Min(lower + 1, snapshots.Length - 1);
            float blend = index - lower;

            enemyBody.position = Vector2.Lerp(
                snapshots[lower].enemyPosition,
                snapshots[upper].enemyPosition,
                blend
            );

            playerMovement.RewindTo(Vector2.Lerp(
                snapshots[lower].playerPosition,
                snapshots[upper].playerPosition,
                blend
            ));

            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;
        }

        // Finish exactly at the oldest recorded positions.
        enemyBody.position = snapshots[0].enemyPosition;
        playerMovement.RewindTo(snapshots[0].playerPosition);

        isRewinding = false;
        playerMovement.SetRewinding(false);
        rewindPanel.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        //Debug.Log(other.gameObject.name);
    }
}