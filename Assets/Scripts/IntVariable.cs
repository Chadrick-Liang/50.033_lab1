using UnityEngine;

[CreateAssetMenu(fileName = "IntVariable", menuName = "ScriptableObjects/IntVariable", order = 2)]
public class IntVariable : Variable<int>
{

    // only GameScore saves a highscore, other IntVariables (e.g. GameLives) must leave PlayerPrefs alone
    public bool persistHighscore = false;
    public int previousHighestValue;

    void OnEnable()
    {
        if (persistHighscore)
            previousHighestValue = PlayerPrefs.GetInt("highscore", 0);
    }

    public override void SetValue(int value)
    {
        if (persistHighscore && value > previousHighestValue)
        {
            previousHighestValue = value;
            PlayerPrefs.SetInt("highscore", previousHighestValue);
            PlayerPrefs.Save();
        }
        _value = value;
    }

    // overload
    public void SetValue(IntVariable value)
    {
        SetValue(value.Value);
    }

    public void ApplyChange(int amount)
    {
        this.Value += amount;
    }

    public void ApplyChange(IntVariable amount)
    {
        ApplyChange(amount.Value);
    }

    public void ResetHighestValue()
    {
        previousHighestValue = 0;
        PlayerPrefs.SetInt("highscore", 0);
        PlayerPrefs.Save();
    }


}