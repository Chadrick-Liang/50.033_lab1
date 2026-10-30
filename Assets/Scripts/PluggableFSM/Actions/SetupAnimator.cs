using UnityEngine;

[CreateAssetMenu(
    menuName = "PluggableSM/Actions/SetupAnimator")]
public class SetupAnimator : Action
{
    public RuntimeAnimatorController animatorController;

    public override void Act(StateController controller)
    {
        controller.gameObject
            .GetComponent<Animator>()
            .runtimeAnimatorController = animatorController;
    }
}