using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadAndWait : MonoBehaviour
{
    public CanvasGroup c;

    void Start()
    {
        if (c != null)
            StartCoroutine(Fade());
    }

    IEnumerator Fade()
    {
        // Fade the screen from fully visible (alpha=1) to invisible (alpha=0)
        for (float alpha = 1f; alpha >= -0.05f; alpha -= 0.05f)
        {
            c.alpha = alpha;
            yield return new WaitForSecondsRealtime(0.1f);
        }

        // After fade completes, load the game scene
        SceneManager.LoadSceneAsync("world_1-1", LoadSceneMode.Single);
    }

    public void ReturnToMain()
    {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene("MainMenu");
    }
}
