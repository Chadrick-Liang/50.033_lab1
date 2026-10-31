using UnityEngine;

[CreateAssetMenu(
    menuName = "PluggableSM/Actions/StartStarman")]
public class StartStarmanAction : Action
{
    public AudioClip starmanClip;

    public override void Act(StateController controller)
    {
        BuffStateController buffController =
            (BuffStateController)controller;

        buffController.BeginStarman(starmanClip);
    }
}
