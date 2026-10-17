using UnityEngine;
using UnityEngine.Events;

// listens to OnPowerupCollected and forwards each powerup to whoever it affects
public class PowerupManager : MonoBehaviour
{
    // link to OnPowerupAffectsPlayer's / OnPowerupAffectsManager's Raise (Dynamic IPowerup) in the Inspector
    public UnityEvent<IPowerup> powerupAffectsPlayer;
    public UnityEvent<IPowerup> powerupAffectsManager;

    // listens to OnPowerupCollected
    public void HandlePowerupCollected(IPowerup powerup)
    {
        if (powerup.powerupType == PowerupType.Coin || powerup.powerupType == PowerupType.OneUpMushroom)
        {
            powerupAffectsManager.Invoke(powerup);
        }
        else
        {
            powerupAffectsPlayer.Invoke(powerup);
        }
    }
}
