using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    public IntVariable gameScore;
    public TextMeshProUGUI topScoreText;

    void Start()
    {
        int highscore = PlayerPrefs.GetInt("highscore", 0);
        topScoreText.text = "TOP- " + highscore.ToString("D6");
    }

    public void StartGame()
    {
        SceneManager.LoadScene("LoadingScene");
    }

    public void ResetHighscore()
    {
        gameScore.ResetHighestValue();
        topScoreText.text = "TOP- " + 0.ToString("D6");
        Debug.Log("Highscore reset to 0.");
    }


    public void DeselectCurrentButton()
    {
        EventSystem.current.SetSelectedGameObject(null);
    }
}

