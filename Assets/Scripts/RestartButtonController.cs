using UnityEngine;
using UnityEngine.Events;

public class RestartButtonController : MonoBehaviour
{

    public UnityEvent gameRestart;

    void ButtonClick()
    {
        gameRestart.Invoke();
    }



}

