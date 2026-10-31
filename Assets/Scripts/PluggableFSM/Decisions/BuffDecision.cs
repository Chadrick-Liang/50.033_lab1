using UnityEngine;

[CreateAssetMenu(
    menuName = "PluggableSM/Decisions/Buff")]
public class BuffDecision : Decision
{
    public PowerupType buffType = PowerupType.StarMan;

    public override bool Decide(StateController controller)
    {
        BuffStateController buffController =
            (BuffStateController)controller;

        return buffController.currentBuffType == buffType;
    }
}
