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
            history.Enqueue(new PositionSnapshot //record a snaphot of time, enemy pos, player pos
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

    // rewind mechanic
    public void Rewind(PlayerMovement playerMovement)
    {
        if (history.Count == 0) return;

        PositionSnapshot snapshot = history.Peek(); //take the oldests entry in the queue (how long to rewind back)
        enemyBody.position = snapshot.enemyPosition;
        playerMovement.RewindTo(snapshot.playerPosition);

        history.Clear();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log(other.gameObject.name);
    }
}