using UnityEngine;

[CreateAssetMenu(
    menuName = "PluggableSM/Actions/SetupCollider")]
public class SetupCollider : Action
{
    public Vector2 size;
    public Vector2 offset;

    public override void Act(StateController controller)
    {
        BoxCollider2D marioCollider =
            controller.GetComponent<BoxCollider2D>();

        Rigidbody2D marioBody =
            controller.GetComponent<Rigidbody2D>();

        if (marioCollider == null)
        {
            return;
        }

        // Remember where the bottom of the old collider is
        float feetY = marioCollider.bounds.min.y;

        marioCollider.size = size;
        marioCollider.offset = offset;

        // Ensure bounds reflect the new collider settings
        Physics2D.SyncTransforms();

        if (marioBody != null)
        {
            // Move Mario so the new collider has the same bottom position
            float heightCorrection =
                feetY - marioCollider.bounds.min.y;

            marioBody.position +=
                Vector2.up * heightCorrection;
        }
    }
}