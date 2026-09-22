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

    public JumpOverGoomba jumpOverGoomba;

    public int maxRewinds = 2;
    private int rewindsUsed = 0;
    private GameObject[] lifeSprites;
    private bool isRewinding = false;

    public Transform gameCamera;
    private CameraController cameraController;

    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;

    private bool isGameOver = false;

    // Start is called before the first frame update
    void Start()
    {
        marioSprite = GetComponent<SpriteRenderer>();
        // Set to be 30 FPS
        Application.targetFrameRate = 30;
        marioBody = GetComponent<Rigidbody2D>();
        // Record Mario's position at the beginning.
        marioStartPosition = marioBody.position;
        cameraController = gameCamera.GetComponent<CameraController>();

        // order life sprites by name (Life1, Life2, ...) so they disappear in order
        lifeSprites = GameObject.FindGameObjectsWithTag("Life").OrderBy(go => go.name).ToArray();

        //hide game over panel at start
        gameOverPanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (isRewinding || isGameOver) return; // input disabled while the rewind animation plays or game over screen is up

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
        }

        if (Input.GetKeyDown("d") && !faceRightState)
        {
            faceRightState = true;
            marioSprite.flipX = false;
        }

    }

    // FixedUpdate is called 50 times a second

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Ground")) onGroundState = true;
    }
    void FixedUpdate()
    {
        if (isRewinding || isGameOver) return; // position is driven by EnemyMovement's rewind coroutine instead

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
        }

        jumpRequested = false;
    }


    void OnTriggerEnter2D(Collider2D other)
    {
        if (isRewinding || isGameOver) return; // ignore collisions while the rewind animation plays or game over screen is up

        if (other.gameObject.CompareTag("Enemy"))
        {
            if (rewindsUsed < maxRewinds) //if there are lives left
            {
                lifeSprites[rewindsUsed].SetActive(false);


                rewindsUsed++;
                //Debug.Log("Rewind no:" + rewindsUsed + "/" + maxRewinds + ")");
                EnemyMovement enemyMovement = other.gameObject.GetComponent<EnemyMovement>();
                if (enemyMovement != null)
                {
                    enemyMovement.Rewind(this); //call rewind function
                }
            }
            else
            {
                GameOver();
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
            eachChild.transform.localPosition = eachChild.GetComponent<EnemyMovement>().startPosition;
        }

        //reset score
        jumpOverGoomba.score = 0;

        // reset rewind lives
        rewindsUsed = 0;
        foreach (GameObject lifeSprite in lifeSprites)
        {
            lifeSprite.SetActive(true);
        }
        cameraController.ResetCamera();
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
}