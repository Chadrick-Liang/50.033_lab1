using UnityEngine;

[CreateAssetMenu(
    menuName = "PluggableSM/Actions/ExitStarman")]
public class ExitStarmanAction : Action
{
    public override void Act(StateController controller)
    {
        BuffStateController buffController =
            (BuffStateController)controller;

        buffController.EndStarman();
    }
}
