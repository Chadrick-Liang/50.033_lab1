using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class MainMenuController : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("LoadingScene");
    }

    public void ResetHighscore()
    {
        PlayerPrefs.SetInt("highscore", 0);
        PlayerPrefs.Save();
        Debug.Log("Highscore reset to 0.");
    }

    // Called after button click to deselect the button
    public void DeselectCurrentButton()
    {
        EventSystem.current.SetSelectedGameObject(null);
    }
}
