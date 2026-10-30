using UnityEngine;

[CreateAssetMenu(
    menuName = "PluggableSM/Actions/ClearPowerup")]
public class ClearPowerupAction : Action
{
    public override void Act(StateController controller)
    {
        MarioStateController marioController =
            (MarioStateController)controller;

        marioController.currentPowerupType =
            PowerupType.Default;
    }
}