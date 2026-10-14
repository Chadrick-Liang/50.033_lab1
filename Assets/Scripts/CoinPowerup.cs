using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class CoinPowerup : BasePowerup
{
    [Min(1)] public int scoreValue = 1;
    public float coinHeight = 1.5f;
    [Min(0.01f)] public float coinDuration = 0.45f;

    private SpriteRenderer visual;
    private Animator coinAnimator;

    protected override void Awake()
    {
        base.Awake();
        type = PowerupType.Coin;
        ResetPowerup();
    }

    public override void SpawnPowerup()
    {
        if (spawned || consumed) return;
        spawned = true;
        StartCoroutine(PopCoin());
    }

    private IEnumerator PopCoin()
    {
        Vector3 startPosition = transform.localPosition;
        visual.enabled = true;
        if (coinAnimator != null)
        {
            coinAnimator.enabled = true;
            coinAnimator.Play("CoinSpin", 0, 0f);
            coinAnimator.Update(0f);
        }

        for (float elapsed = 0f; elapsed < coinDuration; elapsed += Time.deltaTime)
        {
            float progress = elapsed / coinDuration;
            float offset = 4f * coinHeight * progress * (1f - progress);
            transform.localPosition = startPosition + Vector3.up * offset;
            yield return null;
        }

        transform.localPosition = startPosition;
        ApplyPowerup(GameManager.instance);
        DestroyPowerup();
    }

    public override void ApplyPowerup(MonoBehaviour recipient)
    {
        if (!spawned || consumed || !(recipient is GameManager manager)) return;
        consumed = true;
        manager.IncreaseScore(scoreValue);
    }

    public override void DestroyPowerup()
    {
        // Keep the instance so its box can restore it on game restart.
        gameObject.SetActive(false);
    }

    public override void ResetPowerup()
    {
        base.ResetPowerup();
        type = PowerupType.Coin;
        visual = GetComponent<SpriteRenderer>();
        coinAnimator = GetComponent<Animator>();
        visual.enabled = false;
        if (coinAnimator != null) coinAnimator.enabled = false;
    }
}
