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
        // Feedback fix: Explicitly clear out inherited parent list data upon instantiation
        // This ensures two of the same stat (like Love) never get registered to a single baby
        stats = new List<Stat>();

        // Gather only the individual Stat components attached directly to this specific instance
        Stat[] attachedComponents = GetComponents<Stat>();

        foreach (Stat dynamicStat in attachedComponents)
        {
            if (!stats.Contains(dynamicStat))
            {
                stats.Add(dynamicStat);
                Debug.Log("[DNA Assembly] Registered unique local stat profile: " + dynamicStat.GetTrait().Name);
            }
        }
    }

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        Stat speedStat = GetStat("Speed");
        if (speedStat != null && agent != null)
        {
            agent.speed = speedStat.GetTrait().Value;
        }
    }

    void LateUpdate()
    {
        TickStats();
    }

    private void TickStats()
    {
        foreach (Stat stat in stats)
        {
            if (stat is ITick tick)
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

    public CreatureSpecies GetSpeciesType()
    {
        return speciesType;
    }

    public Stat GetStat(string statName)
    {
        return stats.Find(stat => stat.GetTrait().Name == statName);
    }

    public void SubtractStat(string statName, float value)
    {
        Stat stat = GetStat(statName);
        if (stat != null)
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
        if (stat != null)
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
}
