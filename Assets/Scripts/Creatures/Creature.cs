using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum CreatureSpecies { MonsterA, MonsterB, MonsterC }

public class Creature : MonoBehaviour
{
    [Header("Identity Details")]
    [SerializeField] private string creatureName;
    [SerializeField] private CreatureSpecies speciesType;

    [Header("Data Architecture")]
    [SerializeField] private List<Stat> stats;
    private NavMeshAgent agent;

    private void Awake()
    {
        // Explicitly clear out inherited parent list data upon instantiation
        // This ensures duplicate stat references never get registered to a single baby
        stats = new List<Stat>();

        // Gather only the individual Stat components attached directly to this specific instance
        Stat[] attachedComponents = GetComponents<Stat>();

        foreach (Stat dynamicStat in attachedComponents)
        {
            if (dynamicStat != null && dynamicStat.GetTrait() != null && !stats.Contains(dynamicStat))
            {
                stats.Add(dynamicStat);
                Debug.Log("[DNA Assembly] Registered unique local stat profile: " + dynamicStat.GetTrait().Name);
            }
        }
    }

    private void Start()
    {
        RefreshMovementSpeed();
    }

    void LateUpdate()
    {
        TickStats();
    }

    private void TickStats()
    {
        foreach (Stat stat in stats)
        {
            if (stat is ITick tick && stat.GetTrait() != null)
            {
                if (stat.GetTrait().Value >= 0)
                {
                    tick.Tick();
                }
            }
        }
    }

    public string GetName()
    {
        return creatureName;
    }

    public void SetName(string newName)
    {
        creatureName = newName;
    }

    public CreatureSpecies GetSpeciesType()
    {
        return speciesType;
    }

    public Stat GetStat(string statName)
    {
        return stats.Find(stat => stat != null && stat.GetTrait() != null && stat.GetTrait().Name == statName);
    }

    public void SubtractStat(string statName, float value)
    {
        Stat stat = GetStat(statName);
        if (stat != null && stat.GetTrait() != null)
        {
            stat.GetTrait().Value -= value;

            if (stat.GetTrait().Value < 0)
            {
                stat.GetTrait().Value = 0;
            }
        }
    }

    public void AddStat(string statName, float value)
    {
        Stat stat = GetStat(statName);
        if (stat != null && stat.GetTrait() != null)
        {
            stat.GetTrait().Value += value;

            if (stat.GetTrait().Value > stat.GetTrait().MaxValue)
            {
                stat.GetTrait().Value = stat.GetTrait().MaxValue;
            }
        }
    }

    public List<Stat> GetStats()
    {
        return stats;
    }

    public void RegisterStat(Stat newStat)
    {
        if (newStat != null && !stats.Contains(newStat))
        {
            stats.Add(newStat);
            Debug.Log("[DNA Inheritance] Added new inherited trait to offspring: " + newStat.GetTrait().Name);
        }
    }

    public void RemoveStat(Stat statToRemove)
    {
        if (statToRemove != null && stats.Contains(statToRemove))
        {
            stats.Remove(statToRemove);
            Destroy(statToRemove);
        }
    }

    public void RefreshMovementSpeed()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        Stat speedStat = GetStat("Speed");
        if (speedStat != null && speedStat.GetTrait() != null && agent != null)
        {
            agent.speed = speedStat.GetTrait().Value;
        }
    }
}
