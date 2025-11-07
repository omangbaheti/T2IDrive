using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EditorAttributes;
using ubco.ovilab.HPUI;
using ubco.ovilab.HPUI.utils;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

[CreateAssetMenu(fileName = "MicroGestureKeyboard", menuName = "GestureLayoutSetup", order = 0)]
public class GestureLayoutSetup : ScriptableObject
{
    [Tooltip("X implies Across the finger, Y is Along the finger")]
    [SerializeField, InlineButton(nameof(GenerateConditions), "Generate All Conditions", 200f)]
    public Vector2Int regions;

    [SerializeField] public TextAsset layout;
    [SerializeField] public IconLayoutSetup iconLayoutSetup;
    [SerializeField, HideInInspector] public List<float> xDivisions;
    [SerializeField, HideInInspector] public List<float> yDivisions;
    [SerializeField] public List<MicrogestureAction> microGestureActions = new();

    public static readonly Dictionary<Vector2Int, string> VectorToRegionDict = new()
    {
        { new(0, 0), "VolarProximal" },
        { new(0, 1), "VolarIntermediate" },
        { new(0, 2), "VolarDistal" },
        { new(1, 0), "RadialProximal" },
        { new(1, 1), "RadialIntermediate" },
        { new(1, 2), "RadialDistal" }
    };

    public static readonly Dictionary<string, Vector2Int> RegionToVectorDict = new()
    {
        { "VolarProximal", new(0, 0) },
        { "VolarIntermediate", new(0, 1) },
        { "VolarDistal", new(0, 2) },
        { "RadialProximal", new(1, 0) },
        { "RadialIntermediate", new(1, 1) },
        { "RadialDistal", new(1, 2) }
    };

    public static readonly Dictionary<string, string> IndexToRegionDict = new()
    {
        { "1", "RadialDistal" },
        { "2", "RadialIntermediate" },
        { "3", "RadialProximal" },
        { "4", "VolarProximal" },
        { "5", "VolarIntermediate" },
        { "6", "VolarDistal" }
    };

    public static readonly Dictionary<string, string> RegionToIndexDict = new()
    {
        { "RadialDistal", "1" },
        { "RadialIntermediate", "2" },
        { "RadialProximal", "3" },
        { "VolarProximal", "4" },
        { "VolarIntermediate", "5" },
        { "VolarDistal", "6" }
    };

    [Button]
    private void ResetActions()
    {
        foreach (MicrogestureAction existingAction in microGestureActions)
        {
            existingAction.SwipeActions.Clear();
        }

        GenerateConditions();
    }

    [Button]
    private void ApplyExperimentManager()
    {
        foreach (MicrogestureAction existingAction in microGestureActions)
        {
            existingAction.SwipeActions.Add(new ExperimentHandler());
        }
    }


    [Button]
    private void ApplyIconAction()
    {
        microGestureActions.Clear();
        string[] lines = layout.text.Split('\n');
        if (lines.Length < 2) return; // Ensure there's data beyond headers

        for (int i = 1; i < lines.Length; i++) // Start from 1 to skip header
        {
            string[] values = lines[i].Split(',');
            if (values.Length < 3) continue; // Ensure enough columns exist
            Debug.Log(values[0].ToString());
            MicrogestureAction action = new MicrogestureAction
            {
                startRegion = RegionToVectorDict[values[0].Trim()],
                endRegion = RegionToVectorDict[values[1].Trim()],
                SwipeActions = new List<IHPUISwipeAction>
                {
                    new IconAction()
                    {
                        actionLabel = values[2].Trim(),
                        displayImage = iconLayoutSetup.actionIconDict[values[2].Trim()],
                    }
                }
            };
            microGestureActions.Add(action);
        }
    }

    private void SetDivisions()
    {
        xDivisions.Clear();
        yDivisions.Clear();
        for (int i = 0; i < regions.x; i++)
        {
            xDivisions.Add((float)i / regions.x);
        }

        for (int i = 0; i < regions.y; i++)
        {
            yDivisions.Add((float)i / regions.y);
        }

        xDivisions.Add(1f);
        yDivisions.Add(1f);
    }

    private void GenerateConditions()
    {
        SetDivisions();
        HashSet<(Vector2Int, Vector2Int)> existingActions = new();
        foreach (MicrogestureAction existingAction in microGestureActions)
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
                            Debug.Log($"{start}, {end}" + " exists");
                        }
                    }
                }
            }
        }

        EditorUtility.SetDirty(this);
    }

    private void ShuffleList<T>(List<T> list, Random rng)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    [Button]
    private void GenerateAllLayouts(int participantNumbers)
    {
        for (int i = 1; i <= participantNumbers; i++)
        {
            AssignMicrogestureActions($"{i}_OnHand");
            AssignMicrogestureActions($"{i}_Windshield");
            AssignMicrogestureActions($"{i}_TouchScreen");
            AssignMicrogestureActions($"{i}_OnHand_Practice");
            AssignMicrogestureActions($"{i}_Windshield_Practice");
            AssignMicrogestureActions($"{i}_TouchScreen_Practice");
        }
        
    }

    [Button]
    private void AssignMicrogestureActions(string filename)
    {
        string directoryPath = Path.Combine(Application.dataPath, "Layouts/CSV");
        // Create directory if it doesn't exist
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string filePath = Path.Combine(directoryPath, $"{filename}.csv");
        // Parse TI_ actions and their subgroups
        Dictionary<string, string> topLevelActions = new(); // groupNum -> "TI_Action_N"
        Dictionary<string, List<string>> subActionsByGroup = new(); // groupNum -> list of sub-actions

        foreach (string label in iconLayoutSetup.actionIconDict.Keys)
        {
            if (label.StartsWith("TI_"))
            {
                string groupNum = label.Substring(label.LastIndexOf("_", StringComparison.Ordinal) + 1);
                topLevelActions[groupNum] = label;
            }
        }

        Random rand = new();
        List<string> keys = new(topLevelActions.Keys);
        List<string> values = new(topLevelActions.Values);

        // Shuffle only the keys using Fisher-Yates algorithm
        int keyCount = keys.Count;
        while (keyCount > 1)
        {
            keyCount--;
            int k = rand.Next(keyCount + 1);
            (keys[k], keys[keyCount]) = (keys[keyCount], keys[k]);
        }

        // Create new dictionary with shuffled keys mapped to original values in order
        Dictionary<string, string> shuffledTopLevelActions = new Dictionary<string, string>();
        for (int i = 0; i < keys.Count; i++)
        {
            shuffledTopLevelActions[keys[i]] = values[i];
        }

        foreach (string i in shuffledTopLevelActions.Values)
        {
            Debug.Log($"{i}");
        }

        foreach (string label in iconLayoutSetup.actionIconDict.Keys)
        {
            if (!label.StartsWith("TI_"))
            {
                string[] parts = label.Split("_");
                if (parts.Length <= 2 && int.TryParse(parts[1], out int groupNum))
                {
                    string group = topLevelActions[groupNum.ToString()];
                    if (!subActionsByGroup.ContainsKey(group))
                    {
                        subActionsByGroup[group] = new();
                    }

                    subActionsByGroup[group].Add(label);
                }
            }
        }

        foreach (var kvp in subActionsByGroup)
        {
            List<string> list = kvp.Value;
            int n = list.Count;

            while (n > 1)
            {
                n--;
                int k = rand.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        HashSet<string> actionUsageList = new();

        using (StreamWriter writer = new(filePath, false))
        {
            
            writer.WriteLine($"start_region,end_region,letter_association");
            Dictionary<string, string> regionToLabel = new();
            int index = 1;
            foreach (MicrogestureAction action in microGestureActions)
            {
                string startRegion = VectorToRegionDict[action.startRegion];
                string endRegion = VectorToRegionDict[action.endRegion];
                if (action.startRegion == action.endRegion)
                {
                    regionToLabel[startRegion] = shuffledTopLevelActions[index.ToString()];
                    Debug.Log($"{startRegion}, {endRegion},{index}, {regionToLabel[startRegion]}");
                    writer.WriteLine($"{startRegion},{endRegion},{regionToLabel[startRegion]}");
                    index++;
                }
            }

            foreach (MicrogestureAction action in microGestureActions)
            {
                string startRegion = VectorToRegionDict[action.startRegion];
                string endRegion = VectorToRegionDict[action.endRegion];
                string subActionLabel = "";
                if (action.startRegion != action.endRegion)
                {
                    string labelAssociatedWithStartRegion = regionToLabel[startRegion];
                    List<string> subActions = subActionsByGroup[labelAssociatedWithStartRegion];
                    foreach (string subAction in subActions)
                    {
                        if (actionUsageList.Contains(subAction))
                        {
                            continue;
                        }

                        subActionLabel = subAction;
                        writer.WriteLine($"{startRegion},{endRegion},{subActionLabel}");
                        actionUsageList.Add(subAction);
                    }
                }
            }
        }
    }

    [Button]
    public void SetupLayout(string ID)
    {
        string path = Path.Combine(Application.dataPath, "Layouts", "CSV", ID + ".csv");
        path = path.Replace("\\", "/");
        if (File.Exists(path))
        {
            string fileContent = File.ReadAllText(path);
            layout = new(fileContent);
            Debug.Log($"Loaded layout: {ID}");
#if UNITY_EDITOR
            // Mark the ScriptableObject as modified so Unity saves it
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
#endif
        }
        else
        {
            Debug.LogError($"Layout file not found at path: {path}");
        }
        ApplyIconAction();
    }
}


[Serializable]
public class MicrogestureAction
{
    [SerializeField] public Vector2Int startRegion;
    [SerializeField] public Vector2Int endRegion;
    [SerializeReference, SubclassSelector] public List<IHPUISwipeAction> SwipeActions;
}