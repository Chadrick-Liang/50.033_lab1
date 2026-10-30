using UnityEngine;

[CreateAssetMenu(
    menuName = "PluggableSM/Decisions/Transform")]
public class TransformDecision : Decision
{
    public StateTransformMap[] map;

    public override bool Decide(
        StateController controller)
    {
        MarioStateController marioController =
            (MarioStateController)controller;

        MarioState currentMarioState =
            EnumExtension.ParseEnum<MarioState>(
                marioController.currentState.name);

        for (int i = 0; i < map.Length; i++)
        {
            if (currentMarioState == map[i].fromState &&
                marioController.currentPowerupType ==
                map[i].powerupCollected)
            {
                return true;
            }
        }

        return false;
    }
}

[System.Serializable]
public struct StateTransformMap
{
    public MarioState fromState;
    public PowerupType powerupCollected;
}