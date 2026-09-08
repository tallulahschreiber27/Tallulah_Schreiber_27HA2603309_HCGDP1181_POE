using System.Collections;
using UnityEngine;

public class Love : Stat, ITick
{
    [Header("Love Stat Settings")]
    [SerializeField] private float decayRate = 1f; // How fast love naturally fades over time
    [SerializeField] private float warningThreshold = 20f; // Threshold for low romance tracking

    private bool delayWarning;

    public Love(Trait trait) : base(trait)
    {
    }

    /// <summary>
    /// Automatically executed every frame by the studio's Creature.cs Update loop
    /// </summary>
    public void Tick()
    {
        // Love naturally decays slowly over time if creatures are ignored
        trait.Value -= decayRate * Time.deltaTime;

        // Clamp the lower boundary constraint to 0
        if (trait.Value < 0)
        {
            trait.Value = 0;
        }
    }

    /// <summary>
    /// Checks if the love metric has dropped below the warning threshold
    /// </summary>
    public bool CheckThreshold()
    {
        if (trait.Value < warningThreshold)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Triggers a temporary delay block for the notification system
    /// </summary>
    public void SetWarningDelay()
    {
        delayWarning = true;
        StartCoroutine(DelayWarningRoutine());
    }

    /// <summary>
    /// Returns true if a notification alert routine is currently pending execution delays
    /// </summary>
    public bool PendingWarning()
    {
        return delayWarning;
    }

    private IEnumerator DelayWarningRoutine()
    {
        yield return new WaitForSeconds(3);
        delayWarning = false;
    }
}
