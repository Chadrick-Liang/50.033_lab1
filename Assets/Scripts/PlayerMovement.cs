using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 10;
    private Rigidbody2D marioBody;
    public float maxSpeed = 20;
    public float upSpeed = 20;
    private bool onGroundState = true;
    private SpriteRenderer marioSprite;
    private bool faceRightState = true;

    //Stores input read by Update()
    private float moveHorizontal;
    private bool stopRequested;
    private bool jumpRequested;

    public TextMeshProUGUI scoreText;
    public GameObject enemies;

    private Vector2 marioStartPosition;
    private Vector2 lastSafePosition; // last place Mario landed on something solid, respawn point after falling into a pit

    public JumpOverGoomba jumpOverGoomba;

    public int maxRewinds = 2;
    private int rewindsUsed = 0;
    private GameObject[] lifeSprites;
    private bool isRewinding = false;

    // Mario's position every physics step, oldest first, played back by EnemyMovement's rewind coroutine
    public float rewindDuration = 3.0f;
    private List<Vector2> positionHistory = new List<Vector2>();
    public int MaxHistoryCount => Mathf.RoundToInt(rewindDuration / Time.fixedDeltaTime);
    public int HistoryCount => positionHistory.Count;
    public bool IsRewinding => isRewinding;
    public bool IsGrounded => onGroundState;

    public Transform gameCamera;
    private CameraController cameraController;
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;

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
        cameraController = gameCamera.GetComponent<CameraController>();

        // order life sprites by name (Life1, Life2, ...) so they disappear in order
        lifeSprites = GameObject.FindGameObjectsWithTag("Life").OrderBy(go => go.name).ToArray();

        //hide game over panel at start
        gameOverPanel.SetActive(false);
        // set animator state to onGround at start
        marioAnimator.SetBool("onGround", onGroundState);
    }

    // Update is called once per frame
    void Update()
    {
        if (isRewinding || isGameOver || !alive) return; // input disabled while the rewind animation plays or game over screen is up

        moveHorizontal = Input.GetAxisRaw("Horizontal");

        if (Input.GetKeyUp("a") || Input.GetKeyUp("d"))
        {
            stopRequested = true;
        }

        if (Input.GetKeyDown("space"))
        {
            jumpRequested = true;
        }
        //toggle state of facing right or left
        if (Input.GetKeyDown("a") && faceRightState)
        {
            faceRightState = false;
            marioSprite.flipX = true;
            if (marioBody.linearVelocity.x > 0.1f)
                marioAnimator.SetTrigger("onSkid");
        }

        if (Input.GetKeyDown("d") && !faceRightState)
        {
            faceRightState = true;
            marioSprite.flipX = false;
            if (marioBody.linearVelocity.x < -0.1f)
                marioAnimator.SetTrigger("onSkid");
        }

        marioAnimator.SetFloat("xSpeed", Mathf.Abs(marioBody.linearVelocity.x));

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

    // FixedUpdate is called 50 times a second
    void FixedUpdate()
    {
        if (isRewinding || isGameOver || !alive) return; // position is driven by EnemyMovement's rewind coroutine instead

        if (Mathf.Abs(moveHorizontal) > 0)
        {
            Vector2 movement = new Vector2(moveHorizontal, 0);
            // check if it doesn't go beyond maxSpeed
            if (marioBody.linearVelocity.magnitude < maxSpeed)
                marioBody.AddForce(movement * speed);
        }

        // stop
        if (stopRequested)
        {
            // stop
            marioBody.linearVelocity = Vector2.zero;
            stopRequested = false;
        }

        if (jumpRequested && onGroundState)
        {
            marioBody.AddForce(Vector2.up * upSpeed, ForceMode2D.Impulse);
            onGroundState = false;
            // update animator state
            marioAnimator.SetBool("onGround", onGroundState);
        }

        jumpRequested = false;

        //record one snapshot per physics step, drop anything older than the rewind duration
        positionHistory.Add(marioBody.position);
        while (positionHistory.Count > MaxHistoryCount)
        {
            positionHistory.RemoveAt(0);
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
            if (rewindsUsed < maxRewinds) //if there are lives left
            {
                lifeSprites[rewindsUsed].SetActive(false);
                rewindsUsed++;
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
            if (rewindsUsed < maxRewinds) //if there are lives left
            {
                lifeSprites[rewindsUsed].SetActive(false);

                rewindsUsed++;
                //Debug.Log("Rewind no:" + rewindsUsed + "/" + maxRewinds + ")");
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
            moveHorizontal = 0;
            stopRequested = false;
            jumpRequested = false;
            marioBody.linearVelocity = Vector2.zero;
            marioBody.angularVelocity = 0;
        }
    }

    public void RestartButtonCallback(int input)
    {
        //Debug.Log("Restart!");
        // reset everything
        ResetGame();
        isGameOver = false;
        gameOverPanel.SetActive(false);
        // resume time
        Time.timeScale = 1.0f;

        //deselect UI button so that keyboard can be used without accidentally triggering it again
        EventSystem.current.SetSelectedGameObject(null);
    }

    private void ResetGame()
    {
        // reset position
        //marioBody.transform.position = new Vector3(-5.33f, -4.69f, 0.0f);
        marioBody.position = marioStartPosition;
        lastSafePosition = marioStartPosition;
        //make sure to remove velocity present before reset and it might rocket off
        marioBody.linearVelocity = Vector2.zero;
        marioBody.angularVelocity = 0;
        // reset sprite direction
        faceRightState = true;
        marioSprite.flipX = false;
        // reset score
        scoreText.text = "Score: 0";
        // reset Goomba
        foreach (Transform eachChild in enemies.transform)
        {
            EnemyMovement enemyMovement = eachChild.GetComponent<EnemyMovement>();
            eachChild.transform.localPosition = enemyMovement.startPosition;
            enemyMovement.ResetHistory();
        }
        positionHistory.Clear();

        //reset score
        jumpOverGoomba.score = 0;

        // reset rewind lives
        rewindsUsed = 0;
        foreach (GameObject lifeSprite in lifeSprites)
        {
            lifeSprite.SetActive(true);
        }
        cameraController.ResetCamera();

        marioAnimator.SetTrigger("gameRestart");
        alive = true;

        jumpOverGoomba.enabled = true;

        moveHorizontal = 0;
        stopRequested = false;
        jumpRequested = false;

        onGroundState = true; // Assuming Mario restarts on the ground
        marioAnimator.SetBool("onGround", onGroundState);
        marioAnimator.SetFloat("xSpeed", 0f);
        marioAnimator.ResetTrigger("onSkid");

        //restore the coins in bricks
        foreach (BrickBlock brick in
         FindObjectsByType<BrickBlock>(FindObjectsSortMode.None))
        {
            brick.ResetBlock();
        }

    }

    private void GameOver()
    {
        isGameOver = true;

        moveHorizontal = 0;
        stopRequested = false;
        jumpRequested = false;

        finalScoreText.text = "Score: " + jumpOverGoomba.score;
        gameOverPanel.SetActive(true);

        Time.timeScale = 0f;
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

        moveHorizontal = 0;
        stopRequested = false;
        jumpRequested = false;

        marioBody.linearVelocity = Vector2.zero;
        marioBody.angularVelocity = 0;

        jumpOverGoomba.enabled = false;

        marioAnimator.Play("mario-die", 0, 0f);
        marioAudio.PlayOneShot(marioDeath);
    }

    void GameOverScene()
    {
        if (alive || isGameOver) return;

        GameOver();
    }
}