using UnityEngine;

[CreateAssetMenu(
    menuName = "PluggableSM/Actions/FireAttack")]
public class FireAttackAction : Action
{
    public int maxPrefabInScene = 3;
    public float impulseForce = 1;
    public float degree = 45;

    public GameObject attackPrefab;
    public BoolVariable marioFaceRight;

    public override void Act(StateController controller)
    {
        GameObject[] instantiatedPrefabsInScene =
            GameObject.FindGameObjectsWithTag(
                attackPrefab.tag);

        if (instantiatedPrefabsInScene.Length <
            maxPrefabInScene)
        {
            bool isFacingRight = marioFaceRight.Value;

            Vector3 spawnOffset = new Vector3(
                isFacingRight ? 0.7f : -0.7f,
                0.8f,
                0f);

            GameObject fireball = Instantiate(
                attackPrefab,
                controller.transform.position + spawnOffset,
                Quaternion.identity);

            Rigidbody2D rigidBody =
                fireball.GetComponent<Rigidbody2D>();

            Collider2D fireballCollider =
                fireball.GetComponent<Collider2D>();

            Collider2D marioCollider =
                controller.GetComponent<Collider2D>();

            if (fireballCollider != null &&
                marioCollider != null)
            {
                Physics2D.IgnoreCollision(
                    fireballCollider,
                    marioCollider);
            }

            if (rigidBody != null)
            {
                Vector2 direction = CalculateDirection(
                    degree,
                    isFacingRight);

                rigidBody.AddForce(
                    direction * impulseForce,
                    ForceMode2D.Impulse);
            }
        }
    }

    public Vector2 CalculateDirection(
        float degrees,
        bool isFacingRight)
    {
        float radians = degrees * Mathf.Deg2Rad;

        float x = Mathf.Cos(radians);
        float y = Mathf.Sin(radians);

        if (!isFacingRight)
        {
            x = -x;
        }

        return new Vector2(x, y);
    }
}
