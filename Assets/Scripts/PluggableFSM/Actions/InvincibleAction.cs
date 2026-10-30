using UnityEngine;

[CreateAssetMenu(
    menuName = "PluggableSM/Actions/SetupInvincibility")]
public class InvincibleAction : Action
{
    public AudioClip invincibilityStart;

    public override void Act(StateController controller)
    {
        MarioStateController marioController =
            (MarioStateController)controller;

        marioController.gameObject
            .GetComponent<AudioSource>()
            .PlayOneShot(invincibilityStart);

        marioController.SetRendererToFlicker();
    }
}