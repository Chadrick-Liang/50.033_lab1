using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{

    private float originalX;
    private float maxOffset = 5.0f;
    private float enemyPatroltime = 2.0f;
    private int moveRight = -1;
    private Vector2 velocity;

    private Rigidbody2D enemyBody;

    public Vector3 startPosition;

    private Transform player;
    private const float rewindDuration = 3.0f;

    private struct PositionSnapshot
    {
        public float time;
        public Vector2 enemyPosition;
        public Vector2 playerPosition;
    }
    private Queue<PositionSnapshot> history = new Queue<PositionSnapshot>();
    private bool isRewinding = false;
    public SetOverlay overlay;

    void Start()
    {
        enemyBody = GetComponent<Rigidbody2D>();
        //remember where goomba started
        startPosition = transform.localPosition;
        // get the starting position
        originalX = transform.position.x;
        ComputeVelocity();

        player = GameObject.FindGameObjectWithTag("Player").transform; //get mario's game object to detect collision
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

        //show overlay when rewinding
        bool hasOverlay = overlay != null;
        if (hasOverlay)
        {
            overlay.Show();
        }
        else
        {
            //Debug.LogWarning("EnemyMovement: overlay is not assigned in the Inspector, skipping rewind overlay.", this);
        }

        //play per frame rewind
        for (int i = snapshots.Length - 1; i >= 0; i--)
        {
            enemyBody.position = snapshots[i].enemyPosition;
            playerMovement.RewindTo(snapshots[i].playerPosition);
            yield return new WaitForFixedUpdate();
        }

        isRewinding = false;
        playerMovement.SetRewinding(false);
        if (hasOverlay)
        {
            overlay.Hide();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        //Debug.Log(other.gameObject.name);
    }
}