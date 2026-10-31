using UnityEngine;

[CreateAssetMenu(menuName = "Variables/Player Session State")]
public class PlayerSessionState : ScriptableObject
{
    public State marioState;
    public float marioElapsed;

    public State buffState;
    public float buffElapsed;

    public void ResetState()
    {
        marioState = null;
        marioElapsed = 0f;
        buffState = null;
        buffElapsed = 0f;
    }
}
