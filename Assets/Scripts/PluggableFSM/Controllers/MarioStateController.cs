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

    private SpriteRenderer spriteRenderer;

    public override void Start()
    {
        base.Start();
        GameRestart();
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
    }

    public void GameRestart()
    {
        currentPowerupType = PowerupType.Default;
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