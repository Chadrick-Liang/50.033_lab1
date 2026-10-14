using UnityEngine;

public class PauseButtonController : MonoBehaviour, IInteractiveButton
{
    private bool isPaused = false;

    public Sprite pauseIcon;
    public Sprite playIcon;

    private UnityEngine.UI.Image image;

    void Start()
    {
        image = GetComponent<UnityEngine.UI.Image>();
    }

    public void ButtonClick()
    {
        Time.timeScale = isPaused ? 1.0f : 0.0f;
        isPaused = !isPaused;

        // pause/unpause BGM
        var bgm = GameObject.Find("BGM");
        if (bgm == null) bgm = GameObject.Find("BackgroundCanvas");
        if (bgm != null)
        {
            var audio = bgm.GetComponent<AudioSource>();
            if (isPaused) audio.Pause();
            else audio.UnPause();
        }

        if (isPaused)
        {
            image.sprite = playIcon;
        }
        else
        {
            image.sprite = pauseIcon;
        }
    }
}
