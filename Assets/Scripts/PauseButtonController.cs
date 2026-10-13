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
