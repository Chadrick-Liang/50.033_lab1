using UnityEngine;

[CreateAssetMenu(
    menuName = "PluggableSM/Actions/ClearBuff")]
public class ClearBuffAction : Action
{
    public override void Act(StateController controller)
    {
        BuffStateController buffController =
            (BuffStateController)controller;

        buffController.currentBuffType = PowerupType.Default;
    }
}
