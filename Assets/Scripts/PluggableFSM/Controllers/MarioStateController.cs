using System;
using System.Collections;
using UnityEngine;

public class MarioStateController : StateController
{
    public PowerupType currentPowerupType =
        PowerupType.Default;

    public MarioState shouldBeNextState =
        MarioState.Default;

    public GameConstants gameConstants;
    public PlayerSessionState playerSessionState;

    private SpriteRenderer spriteRenderer;

    public override void Start()
    {
        if (playerSessionState != null &&
            playerSessionState.marioState != null)
        {
            currentState = playerSessionState.marioState;
            stateTimeElapsed = playerSessionState.marioElapsed;
        }
        else
        {
            currentState = startState;
            stateTimeElapsed = 0f;
        }

        currentPowerupType = PowerupType.Default;
        base.Start();
    }

    public override void OnSetupState()
    {
        // Runs the state's setup actions first, like replacing the animator controller
        base.OnSetupState();

        Animator animator = GetComponent<Animator>();
        PlayerMovement movement = GetComponent<PlayerMovement>();
        Rigidbody2D body = GetComponent<Rigidbody2D>();

        if (animator != null && movement != null)
        {
            // Restore values that were reset when the controller changed.
            animator.SetBool(
                "onGround",
                movement.IsGrounded);

            if (body != null)
            {
                animator.SetFloat(
                    "xSpeed",
                    Mathf.Abs(body.linearVelocity.x));
            }
        }

        if (playerSessionState != null &&
            currentState != null)
        {
            playerSessionState.marioState = currentState;
            playerSessionState.marioElapsed = stateTimeElapsed;
        }
    }

    private void OnDisable()
    {
        if (playerSessionState == null ||
            currentState == null)
        {
            return;
        }

        playerSessionState.marioState = currentState;
        playerSessionState.marioElapsed = stateTimeElapsed;
    }

    public void GameRestart()
    {
        currentPowerupType = PowerupType.Default;

        if (playerSessionState != null)
        {
            playerSessionState.marioState = startState;
            playerSessionState.marioElapsed = 0f;
        }

        TransitionToState(startState);
    }

    public void SetPowerup(PowerupType powerupType)
    {
        currentPowerupType = powerupType;
    }

    public void Fire()
    {
        currentState.DoEventTriggeredActions(
            this,
            ActionType.Attack);
    }

    public void SetRendererToFlicker()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        StartCoroutine(BlinkSpriteRenderer());
    }

    private IEnumerator BlinkSpriteRenderer()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        while (string.Equals(
            currentState.name,
            "InvincibleSmallMario",
            StringComparison.OrdinalIgnoreCase))
        {
            spriteRenderer.enabled =
                !spriteRenderer.enabled;

            yield return new WaitForSeconds(
                gameConstants.flickerInterval);
        }

        spriteRenderer.enabled = true;
    }
}
