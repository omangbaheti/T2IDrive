using ArtificeToolkit.Runtime.SerializedDictionary;
using UnityEngine;

[CreateAssetMenu(fileName = "IconLayoutSetup", menuName = "IconLayout", order = 0)]
public class IconLayoutSetup : ScriptableObject
{
    [SerializeField] public SerializedDictionary<string, Sprite> actionIconDict = new();
}