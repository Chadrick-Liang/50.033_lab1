using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 10;
    private Rigidbody2D marioBody;
    public float maxSpeed = 20;
    public float upSpeed = 20;
    private bool onGroundState = true;
    private SpriteRenderer marioSprite;
    private bool faceRightState = true;

    GameManager gameManager;

    private Vector2 marioStartPosition;
    private Vector2 lastSafePosition; // last place Mario landed on something solid, respawn point after falling into a pit

    public JumpOverGoomba jumpOverGoomba;

    private bool isRewinding = false;

    // Mario's position every physics step, oldest first, played back by EnemyMovement's rewind coroutine
    public float rewindDuration = 3.0f;
    private List<Vector2> positionHistory = new List<Vector2>();
    public int MaxHistoryCount => Mathf.RoundToInt(rewindDuration / Time.fixedDeltaTime);
    public int HistoryCount => positionHistory.Count;
    public bool IsRewinding => isRewinding;
    public bool IsGrounded => onGroundState;

    private bool isGameOver = false;
    private int collisionLayerMask = (1 << 3) | (1 << 6) | (1 << 7) | (1 << 8);

    public Animator marioAnimator;

    public AudioSource marioAudio;
    public AudioClip marioDeath;
    public float deathImpulse = 15;

    [System.NonSerialized]
    public bool alive = true;

    public AudioClip marioRewind;

    // Start is called before the first frame update
    void Start()
    {
        marioSprite = GetComponent<SpriteRenderer>();
        // Set to be 30 FPS
        Application.targetFrameRate = 30;
        marioBody = GetComponent<Rigidbody2D>();
        // Record Mario's position at the beginning.
        marioStartPosition = marioBody.position;
        lastSafePosition = marioStartPosition;
        gameManager = GameObject.FindGameObjectWithTag("Manager").GetComponent<GameManager>();

        // set animator state to onGround at start
        marioAnimator.SetBool("onGround", onGroundState);
    }

    // Update is called once per frame
    void Update()
    {
        if (isRewinding || isGameOver || !alive) return; // input disabled while the rewind animation plays or game over screen is up

        marioAnimator.SetFloat("xSpeed", Mathf.Abs(marioBody.linearVelocity.x));
    }

    void FlipMarioSprite(int value)
    {
        if (value == -1 && faceRightState)
        {
            faceRightState = false;
            marioSprite.flipX = true;
            if (marioBody.linearVelocity.x > 0.05f)
                marioAnimator.SetTrigger("onSkid");

        }

        else if (value == 1 && !faceRightState)
        {
            faceRightState = true;
            marioSprite.flipX = false;
            if (marioBody.linearVelocity.x < -0.05f)
                marioAnimator.SetTrigger("onSkid");
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (!alive || isRewinding || isGameOver) return;

        bool solidSurface =
            (collisionLayerMask & (1 << col.gameObject.layer)) != 0;

        if (!solidSurface) return;

        for (int i = 0; i < col.contactCount; i++)
        {
            // This callback runs on Mario
            // an upward normal means a surface supports him.
            if (col.GetContact(i).normal.y > 0.5f)
            {
                onGroundState = true;
                marioAnimator.SetBool("onGround", true);
                lastSafePosition = marioBody.position;
                break;
            }
        }
    }

    private bool moving = false;
    // FixedUpdate is called 50 times a second
    void FixedUpdate()
    {
        if (isRewinding || isGameOver || !alive) return; // position is driven by EnemyMovement's rewind coroutine instead

        if (alive && moving)
        {
            Move(faceRightState == true ? 1 : -1);
        }

        //record one snapshot per physics step, drop anything older than the rewind duration
        positionHistory.Add(marioBody.position);
        while (positionHistory.Count > MaxHistoryCount)
        {
            positionHistory.RemoveAt(0);
        }
    }

    void Move(int value)
    {

        Vector2 movement = new Vector2(value, 0);
        // check if it doesn't go beyond maxSpeed
        if (marioBody.linearVelocity.magnitude < maxSpeed)
            marioBody.AddForce(movement * speed);
    }

    public void MoveCheck(int value)
    {
        if (value == 0)
        {
            moving = false;
            marioBody.linearVelocity = Vector2.zero;
        }
        else
        {
            FlipMarioSprite(value);
            moving = true;
            Move(value);
        }
    }

    private bool jumpedState = false;

    public void Jump()
    {
        if (alive && onGroundState)
        {
            // jump
            marioBody.AddForce(Vector2.up * upSpeed, ForceMode2D.Impulse);
            onGroundState = false;
            jumpedState = true;
            // update animator state
            marioAnimator.SetBool("onGround", onGroundState);

        }
    }

    public void JumpHold()
    {
        if (alive && jumpedState)
        {
            // jump higher
            marioBody.AddForce(Vector2.up * upSpeed * 30, ForceMode2D.Force);
            jumpedState = false;

        }
    }

    // returns the newest recorded position and forgets it
    public Vector2 PopHistory()
    {
        Vector2 position = positionHistory[positionHistory.Count - 1];
        positionHistory.RemoveAt(positionHistory.Count - 1);
        return position;
    }


    void OnTriggerEnter2D(Collider2D other)
    {
        if (isRewinding || isGameOver || !alive) return; // ignore collisions while the rewind animation plays or game over screen is up

        // fell into a pit: lose a life and respawn on the last ground Mario landed on
        if (other.gameObject.CompareTag("DeathZone"))
        {
            if (gameManager.UseLife()) //if there are lives left
            {
                marioAudio.PlayOneShot(marioRewind);
                RewindTo(lastSafePosition); // also clears velocity so he doesn't keep falling
                positionHistory.Clear(); // so a later goomba rewind doesn't replay the fall into the pit
            }
            else
            {
                BeginDeath();
            }
            return;
        }

        if (other.gameObject.CompareTag("Enemy"))
        {
            if (gameManager.UseLife()) //if there are lives left
            {
                marioAudio.PlayOneShot(marioRewind);
                EnemyMovement enemyMovement = other.gameObject.GetComponent<EnemyMovement>();
                if (enemyMovement != null)
                {
                    enemyMovement.Rewind(this); //call rewind function
                }
            }
            else
            {
                BeginDeath();
            }
        }
    }

    // called by EnemyMovement to rewind the player back to a stored position
    public void RewindTo(Vector2 position)
    {
        marioBody.position = position;
        marioBody.linearVelocity = Vector2.zero;
        marioBody.angularVelocity = 0;
    }

    // called by EnemyMovement to freeze/unfreeze player control while the rewind animation plays
    public void SetRewinding(bool rewinding)
    {
        isRewinding = rewinding;
        if (rewinding)
        {
            marioBody.linearVelocity = Vector2.zero;
            marioBody.angularVelocity = 0;
        }
    }

    // subscribed to GameManager.gameRestart
    public void GameRestart()
    {
        isGameOver = false;
        // reset position
        marioBody.position = marioStartPosition;
        lastSafePosition = marioStartPosition;
        //make sure to remove velocity present before reset and it might rocket off
        marioBody.linearVelocity = Vector2.zero;
        marioBody.angularVelocity = 0;
        // reset sprite direction
        faceRightState = true;
        marioSprite.flipX = false;
        positionHistory.Clear();

        marioAnimator.SetTrigger("gameRestart");
        alive = true;

        jumpOverGoomba.enabled = true;


        onGroundState = true; // Assuming Mario restarts on the ground
        marioAnimator.SetBool("onGround", onGroundState);
        marioAnimator.SetFloat("xSpeed", 0f);
        marioAnimator.ResetTrigger("onSkid");
    }

    void PlayJumpSound()
    {
        // play jump sound
        marioAudio.PlayOneShot(marioAudio.clip);
    }

    void PlayDeathImpulse()
    {
        marioBody.AddForce(Vector2.up * deathImpulse, ForceMode2D.Impulse);
    }

    private void BeginDeath()
    {
        if (!alive || isGameOver) return;
        marioAudio.Stop(); //prevent any leftover audio

        alive = false;


        marioBody.linearVelocity = Vector2.zero;
        marioBody.angularVelocity = 0;

        jumpOverGoomba.enabled = false;

        marioAnimator.Play("mario-die", 0, 0f);
        marioAudio.PlayOneShot(marioDeath);
    }

    void GameOverScene()
    {
        if (alive || isGameOver) return;

        isGameOver = true;
        gameManager.GameOver();
    }
}