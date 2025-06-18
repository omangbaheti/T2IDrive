using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EditorAttributes;
using ubco.ovilab.HPUI;
using ubco.ovilab.HPUI.utils;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "MicroGestureKeyboard", menuName = "GestureLayoutSetup", order = 0)]
public class GestureLayoutSetup : ScriptableObject
{
    [Tooltip("X implies Across the finger, Y is Along the finger")]
    [SerializeField, InlineButton(nameof(GenerateConditions),"Generate All Conditions", 200f)] public Vector2Int regions;

    [SerializeField, HideInInspector] public List<float> xDivisions;
    [SerializeField, HideInInspector] public List<float> yDivisions;
    [SerializeField] public List<MicrogestureAction> microGestureActions = new();

    [Button]
    private void ResetActions()
    {
        foreach (MicrogestureAction existingAction in microGestureActions)
        {
            existingAction.SwipeActions.Clear();
        }
        GenerateConditions();
    }

    private void SetDivisions()
    {
        xDivisions.Clear();
        yDivisions.Clear();
        for (int i = 0; i < regions.x; i++)
        {
            xDivisions.Add((float)i/regions.x);
        }

        for (int i = 0; i < regions.y; i++)
        {
            yDivisions.Add((float)i/regions.y);
        }
        xDivisions.Add(1f);
        yDivisions.Add(1f);
    }

    private void GenerateConditions()
    {
        SetDivisions();
        HashSet<(Vector2Int, Vector2Int)> existingActions = new();
        foreach (var existingAction in microGestureActions)
        {
            existingActions.Add((existingAction.startRegion, existingAction.endRegion));
        }
        for (int i = 0; i < regions.x; i++)
        {
            for (int j = 0; j < regions.y; j++)
            {
                for (int k = 0; k < regions.x; k++)
                {
                    for (int l = 0; l < regions.y; l++)
                    {
                        var start = new Vector2Int(i, j);
                        var end = new Vector2Int(k, l);

                        if (!existingActions.Contains((start, end)))
                        {
                            var action = new MicrogestureAction
                            {
                                startRegion = start,
                                endRegion = end
                            };

                            microGestureActions.Add(action);
                            existingActions.Add((start, end));
                        }
                        else
                        {
                            Debug.Log($"{start}, {end}" +" exists");
                        }
                    }
                }
            }
        }
        EditorUtility.SetDirty(this);
    }

    private List<MicrogestureAction> Sort(List<MicrogestureAction> actions)
    {
        actions.Sort((a, b) =>
        {
            // Sort by startRegion.y descending, then startRegion.x descending
            int startCompare = b.startRegion.x.CompareTo(a.startRegion.x);
            if (startCompare != 0) return startCompare;

            startCompare = b.startRegion.y.CompareTo(a.startRegion.y);
            if (startCompare != 0) return startCompare;

            // Sort by endRegion.y descending, then endRegion.x descending
            int endCompare = b.endRegion.x.CompareTo(a.endRegion.x);
            if (endCompare != 0) return endCompare;

            return b.endRegion.y.CompareTo(a.endRegion.y);
        });
        return actions;
    }
}

[Serializable]
public class MicrogestureAction
{
    [SerializeField] public Vector2Int startRegion;
    [SerializeField] public Vector2Int endRegion;
    [SerializeReference, SubclassSelector] public List<IHPUISwipeAction> SwipeActions;
}


