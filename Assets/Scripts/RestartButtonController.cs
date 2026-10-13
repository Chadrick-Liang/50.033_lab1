using UnityEngine;

public class RestartButtonController : MonoBehaviour, IInteractiveButton
{
    public void ButtonClick()
    {
        Debug.Log("OnClick restart button");
        GameManager.instance.GameRestart();
    }
}