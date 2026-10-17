using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerMovement : MonoBehaviour, IPowerupApplicable
{
    public GameConstants gameConstants;
    private float speed;
    private Rigidbody2D marioBody;
    private float maxSpeed;
    private float upSpeed;
    private bool onGroundState = true;
    private SpriteRenderer marioSprite;
    private bool faceRightState = true;

    GameManager gameManager;

    private Vector2 marioStartPosition;
    private Vector2 lastSafePosition; // last place Mario landed on something solid, respawn point after falling into a pit

    public JumpOverGoomba jumpOverGoomba;

    private bool isRewinding = false;

    // Mario's position every physics step, oldest first, played back by the rewind coroutine
    public float rewindDuration = 3.0f;
    private List<Vector2> positionHistory = new List<Vector2>();
    public int MaxHistoryCount => Mathf.RoundToInt(rewindDuration / Time.fixedDeltaTime);
    public int HistoryCount => positionHistory.Count;
    public bool IsRewinding => isRewinding;

    // how many recorded steps to go back per physics step, 1 = real time (3s rewind takes 3s), 1.5 = 3s rewind takes 2s
    private const float rewindPlaybackSpeed = 1.5f;

    // rewind events, link each one to the matching GameEvent's Raise() in the Inspector
    public UnityEvent rewindStart;
    public UnityEvent rewindStep;
    public UnityEvent rewindEnd;
    public bool IsGrounded => onGroundState;
    public bool IsSuperMario { get; private set; }
    private Vector3 normalScale;

    private bool isGameOver = false;
    private int collisionLayerMask = (1 << 3) | (1 << 6) | (1 << 7) | (1 << 8);

    public Animator marioAnimator;

    public AudioSource marioAudio;
    public AudioClip marioDeath;
    private float deathImpulse;

    public float stompBounce = 10; // upward speed Mario gets after squashing a goomba
    public float stompTolerance = 0.25f; // how far below the goomba's top Mario's centre can be and still count as a stomp

    [System.NonSerialized]
    public bool alive = true;

    public AudioClip marioRewind;

    // second AudioSource for rewind/death, set its Output to the mixer group that ducks the music
    public AudioSource duckAudio;

    void Awake()
    {
        normalScale = transform.localScale;
        gameManager = GameManager.instance;
        gameManager.gameRestart.AddListener(GameRestart);
    }

    // Start is called before the first frame update
    void Start()
    {
        // Set constants
        speed = gameConstants.speed;
        maxSpeed = gameConstants.maxSpeed;
        deathImpulse = gameConstants.deathImpulse;
        upSpeed = gameConstants.upSpeed;
        if (duckAudio == null) duckAudio = marioAudio;

        marioSprite = GetComponent<SpriteRenderer>();
        // Set to be 30 FPS
        Application.targetFrameRate = 30;
        marioBody = GetComponent<Rigidbody2D>();
        // Record Mario's position at the beginning.
        marioStartPosition = marioBody.position;
        lastSafePosition = marioStartPosition;

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
        if (isRewinding || isGameOver || !alive) return; // position is driven by the rewind coroutine instead

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
        //preven mario from moving during pause
        if (Time.timeScale == 0) return;
        if (value == 0)
        {
            moving = false;
            // Stop horizontal movement without cancelling a jump or fall
            marioBody.linearVelocity =
    new Vector2(0f, marioBody.linearVelocity.y);
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
        //preven mario from jumping during pause
        if (Time.timeScale == 0) return;
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
                duckAudio.PlayOneShot(marioRewind);
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
            EnemyMovement enemyMovement = other.gameObject.GetComponent<EnemyMovement>();

            // landed on its head while falling: squash it and bounce off instead of getting hurt
            bool stomped = marioBody.linearVelocity.y < 0 && marioBody.position.y > other.bounds.max.y - stompTolerance;
            if (stomped && enemyMovement != null)
            {
                enemyMovement.Stomp();
                marioBody.linearVelocity = new Vector2(marioBody.linearVelocity.x, stompBounce);
                return;
            }

            if (gameManager.UseLife()) //if there are lives left
            {
                duckAudio.PlayOneShot(marioRewind);
                StartRewind();
            }
            else
            {
                BeginDeath();
            }
        }
    }

    // moves Mario to a stored position, used by the rewind coroutine and pit respawn
    public void RewindTo(Vector2 position)
    {
        marioBody.position = position;
        marioBody.linearVelocity = Vector2.zero;
        marioBody.angularVelocity = 0;
    }

    // freeze/unfreeze player control while the rewind animation plays
    public void SetRewinding(bool rewinding)
    {
        isRewinding = rewinding;
        if (rewinding)
        {
            marioBody.linearVelocity = Vector2.zero;
            marioBody.angularVelocity = 0;
        }
    }

    // rewind mechanic (playback function), Mario owns the history so he drives the rewind and raises events for everyone else
    private void StartRewind()
    {
        //will not rewind if nothing is recorded or a rewind is already playing
        if (positionHistory.Count == 0 || isRewinding) return;

        //we use a corountine here (a type of unity object that allows a function to run over multiple frames)
        StartCoroutine(RewindCoroutine());
    }

    // rewind mechanic (per frame)
    private IEnumerator RewindCoroutine()
    {
        //set bool to prevent user action during animation
        SetRewinding(true);
        rewindStart.Invoke(); // goombas freeze, HUD shows the rewind panel

        //play back the recorded steps newest to oldest until Mario's history (3s) runs out
        float stepsToPlay = 0f;
        while (positionHistory.Count > 0)
        {
            stepsToPlay += rewindPlaybackSpeed; // fractional speeds carry over to the next step
            while (stepsToPlay >= 1f && positionHistory.Count > 0)
            {
                stepsToPlay -= 1f;
                RewindTo(PopHistory());
                rewindStep.Invoke(); // every goomba steps back once too
            }
            yield return new WaitForFixedUpdate();
        }

        SetRewinding(false);
        rewindEnd.Invoke(); // goombas resume patrolling, HUD hides the rewind panel
    }

    public void RequestPowerupEffect(IPowerup powerup)
    {
        if (alive && !isRewinding && !isGameOver)
            powerup.ApplyPowerup(this);
    }

    public void BecomeSuperMario()
    {
        if (IsSuperMario) return;
        IsSuperMario = true;
        // Reuse the current artwork, keeping Mario's feet at the same height.
        Collider2D bodyCollider = GetComponent<Collider2D>();
        float feetY = bodyCollider != null ? bodyCollider.bounds.min.y : transform.position.y;
        transform.localScale = new Vector3(normalScale.x, normalScale.y * 2f, normalScale.z);
        if (bodyCollider != null)
        {
            Physics2D.SyncTransforms();
            marioBody.position += Vector2.up * (feetY - bodyCollider.bounds.min.y);
        }
    }

    // subscribed to GameManager.gameRestart
    public void GameRestart()
    {
        isGameOver = false;
        IsSuperMario = false;
        transform.localScale = normalScale;
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
        var bgm = GameObject.Find("BGM");
        if (bgm == null) bgm = GameObject.Find("BackgroundCanvas");
        if (bgm != null) bgm.GetComponent<AudioSource>().Play();


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
        var bgm = GameObject.Find("BGM");
        if (bgm == null) bgm = GameObject.Find("BackgroundCanvas");
        if (bgm != null) bgm.GetComponent<AudioSource>().Stop();


        alive = false;


        marioBody.linearVelocity = Vector2.zero;
        marioBody.angularVelocity = 0;

        jumpOverGoomba.enabled = false;

        marioAnimator.Play("mario-die", 0, 0f);
        duckAudio.PlayOneShot(marioDeath);
    }

    void GameOverScene()
    {
        if (alive || isGameOver) return;

        isGameOver = true;
        gameManager.GameOver();
    }
}
