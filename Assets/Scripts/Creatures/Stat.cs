using System;
using UnityEngine;

public abstract class Stat : MonoBehaviour
{
    [SerializeField] protected Trait trait;

    public Stat(Trait trait)
    {
        this.trait = trait;
    }

    public virtual Trait GetTrait()
    {
        return trait;
    }

    public virtual void InitializeTrait(Trait newTrait)
    {
        this.trait = newTrait;
    }

    public override string ToString()
    {
        if (trait == null)
        {
            return "Uninitialized Trait";
        }

        string formattedString = trait.Name + ": " + Mathf.Round(trait.Value) + "/" + trait.MaxValue;
        return formattedString;
    }
}
