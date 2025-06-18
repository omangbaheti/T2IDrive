using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EulersCircuit
{
    
    [SerializeField] 

    public static List<TwoStepEulerConnection> TwoStepEulersCircuit(int items, bool shuffle)
    {
        string finalOutput2 = "";
        List<TwoStepEulerConnection> twoStepEulerConnections = new();
        
        
        List<int> els = Enumerable.Range(0, items).ToList();
        Shuffle(els,shuffle);

        Dictionary<int, List<List<int>>> adj = new();
        foreach (int el in els)
        {
            List<int> shuffledEls1 = new(els);
            List<int> shuffledEls2 = new(els);
            Shuffle(shuffledEls1, shuffle);
            Shuffle(shuffledEls2, shuffle);
            adj[el] = new List<List<int>> { shuffledEls1, shuffledEls2 };
        }

        List<int> circuit = new();
        List<int> currentPath = new()
        {
            adj.Keys.First()
        };

        bool step0 = false;

        while (currentPath.Count > 0)
        {
            int current_node = currentPath[^1];

            if (step0)
            {
                if (adj[current_node][0].Count > 0)
                {
                    int next_node = adj[current_node][0].Last();
                    adj[current_node][0].RemoveAt(adj[current_node][0].Count - 1);
                    currentPath.Add(next_node);
                    step0 = !step0;
                    //Debug.Log($"Step 0-- {string.Join(", ", adj[current_node][0])}");
                }
                else
                {
                    //Debug.Log("Adding");
                    circuit.Add(currentPath[^1]);
                    currentPath.RemoveAt(currentPath.Count - 1);
                }
            }
            else
            {
                if (adj[current_node][1].Count > 0)
                {
                    int next_node = adj[current_node][1].Last();
                    adj[current_node][1].RemoveAt(adj[current_node][1].Count - 1);
                    currentPath.Add(next_node);
                    step0 = !step0;
                    //Debug.Log($"Step 1 -- {string.Join(", ", adj[current_node][1])}");
                }
                else
                {
                    circuit.Add(currentPath[^1]);
                    currentPath.RemoveAt(currentPath.Count - 1);
                }
            }
        }

        List<(int, int)> type1pairs = new();
        List<(int, int)> type2pairs = new();
        int? prev = null;
        for (int i = circuit.Count - 1; i >= 0; i--)
        {
            TwoStepEulerConnection circuitConnection = new()
            {
                node = circuit[i],
                connectionType = step0 ? TwoStepEulerConnectionType.Gesture :TwoStepEulerConnectionType.Liftoff
            };
            twoStepEulerConnections.Add(circuitConnection);
            finalOutput2 += (circuit[i] + (step0 ? " -> " : " ==> "));

            if (prev.HasValue)
            {
                if (step0)
                {
                    type1pairs.Add((prev.Value, circuit[i]));
                }
                else
                {
                    type2pairs.Add((prev.Value, circuit[i]));
                }
            }
            step0 = !step0;
            prev = circuit[i];
        }
        Debug.Log(finalOutput2);
        //Debug.Log(string.Join(", ", type1pairs.OrderBy(t => t.Item1).ThenBy(t => t.Item2)) + " " + type1pairs.Count);
        //Debug.Log(string.Join(", ", type2pairs.OrderBy(t => t.Item1).ThenBy(t => t.Item2)) + " " + type2pairs.Count);
        return twoStepEulerConnections;
    }

    [Serializable]
    public struct TwoStepEulerConnection
    {
        public int node;
        public TwoStepEulerConnectionType connectionType;
    }

    [Serializable]
    public enum TwoStepEulerConnectionType
    {
        Invalid = -1,
        Gesture = 0,
        Liftoff = 1
    }

    static void Shuffle<T>(List<T> list, bool shuffle)
    {
        if(!shuffle)return;
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}