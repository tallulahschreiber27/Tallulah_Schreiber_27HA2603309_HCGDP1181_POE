using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Creature : MonoBehaviour
{
    [SerializeField] private string creatureName;
    [SerializeField] private List<Stat> stats;
    private NavMeshAgent agent;

    private void Awake()
    {
        // Initialize the tracking list if it's completely empty or unassigned
        if (stats == null)
        {
            stats = new List<Stat>();
        }

        // Gather all individual Stat scripts attached to this creature (Hunger, Health, Love, etc.)
        Stat[] attachedComponents = GetComponents<Stat>();

        // Safely register them into the master data tracking list array automatically on launch
        foreach (Stat dynamicStat in attachedComponents)
        {
            if (!stats.Contains(dynamicStat))
            {
                stats.Add(dynamicStat);
                Debug.Log($"<color=cyan>[DNA Assembly]</color> Automatically registered stat profile: <b>{dynamicStat.GetTrait().Name}</b> onto creature tracker list.");
            }
        }
    }

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // Grab speed stat safely
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
