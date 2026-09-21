using System.Collections;
using System.Collections.Generic;
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

    // Start is called before the first frame update
    void Start()
    {
        marioSprite = GetComponent<SpriteRenderer>();
        // Set to be 30 FPS
        Application.targetFrameRate = 30;
        marioBody = GetComponent<Rigidbody2D>();
        // Record Mario's position at the beginning.
        marioStartPosition = marioBody.position;

    }

    // Update is called once per frame
    void Update()
    {
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
        if (other.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("Collided with goomba!");
            Time.timeScale = 0.0f;
        }
    }

    public void RestartButtonCallback(int input)
    {
        Debug.Log("Restart!");
        // reset everything
        ResetGame();
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

    }
}