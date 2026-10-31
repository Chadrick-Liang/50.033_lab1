using System.Collections;
using UnityEngine;

public class BuffStateController : StateController
{
    public PowerupType currentBuffType = PowerupType.Default;
    public State starInvincibleState;
    public GameConstants gameConstants;
    public PlayerSessionState playerSessionState;

    private SpriteRenderer spriteRenderer;
    private AudioSource buffAudio;
    private AudioSource backgroundMusic;
    private Coroutine flickerCoroutine;
    private bool backgroundMusicWasPlaying;

    public bool IsStarInvincible =>
        currentState == starInvincibleState;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        AudioSource marioAudio = GetComponent<AudioSource>();
        buffAudio = gameObject.AddComponent<AudioSource>();
        buffAudio.playOnAwake = false;
        buffAudio.loop = true;
        buffAudio.spatialBlend = 0f;

        if (marioAudio != null)
        {
            buffAudio.outputAudioMixerGroup =
                marioAudio.outputAudioMixerGroup;
        }

        GameObject backgroundMusicObject = GameObject.Find("BGM");
        if (backgroundMusicObject == null)
        {
            backgroundMusicObject = GameObject.Find("BackgroundCanvas");
        }

        if (backgroundMusicObject != null)
        {
            backgroundMusic = backgroundMusicObject.GetComponent<AudioSource>();
        }
    }

    public override void Start()
    {
        if (playerSessionState != null &&
            playerSessionState.buffState != null)
        {
            currentState = playerSessionState.buffState;
            stateTimeElapsed = playerSessionState.buffElapsed;
        }
        else
        {
            currentState = startState;
            stateTimeElapsed = 0f;
        }

        currentBuffType = PowerupType.Default;
        base.Start();
    }

    public override void OnSetupState()
    {
        base.OnSetupState();

        if (playerSessionState != null &&
            currentState != null)
        {
            playerSessionState.buffState = currentState;
            playerSessionState.buffElapsed = stateTimeElapsed;
        }
    }

    private void OnDisable()
    {
        if (playerSessionState == null ||
            currentState == null)
        {
            return;
        }

        playerSessionState.buffState = currentState;
        playerSessionState.buffElapsed = stateTimeElapsed;
    }

    public void SetBuff(PowerupType buffType)
    {
        if (buffType == PowerupType.StarMan &&
            IsStarInvincible)
        {
            stateTimeElapsed = 0f;
            currentBuffType = PowerupType.Default;
            return;
        }

        currentBuffType = buffType;
    }

    public void BeginStarman(AudioClip clip)
    {
        if (backgroundMusic != null)
        {
            backgroundMusicWasPlaying = backgroundMusic.isPlaying;
            backgroundMusic.Pause();
        }

        if (buffAudio != null && clip != null)
        {
            buffAudio.clip = clip;
            buffAudio.Play();
        }

        if (flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
        }

        flickerCoroutine = StartCoroutine(Flicker());
    }

    public void EndStarman()
    {
        StopStarman(true);
    }

    public void StopStarmanForDeath()
    {
        StopStarman(false);
    }

    private void StopStarman(bool resumeBackgroundMusic)
    {
        if (flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
            flickerCoroutine = null;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        if (buffAudio != null)
        {
            buffAudio.Stop();
            buffAudio.clip = null;
        }

        if (resumeBackgroundMusic &&
            backgroundMusicWasPlaying &&
            backgroundMusic != null)
        {
            backgroundMusic.UnPause();
        }

        backgroundMusicWasPlaying = false;
    }

    private IEnumerator Flicker()
    {
        float interval = gameConstants != null
            ? gameConstants.flickerInterval
            : 0.1f;

        while (IsStarInvincible)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(interval);
        }

        spriteRenderer.enabled = true;
        flickerCoroutine = null;
    }

    public void GameRestart()
    {
        EndStarman();
        currentBuffType = PowerupType.Default;

        if (playerSessionState != null)
        {
            playerSessionState.buffState = startState;
            playerSessionState.buffElapsed = 0f;
        }

        if (currentState == startState)
        {
            stateTimeElapsed = 0f;
            return;
        }

        TransitionToState(startState);
    }
}
