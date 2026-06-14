using ubco.ovilab.HPUI.Interaction;
using UnityEngine;
using UnityEditor;

namespace Array2DEditor
{
    [CustomPropertyDrawer(typeof(HPUIInteractable2DArray))]
    public class HPUIArray2DInteractableDrawer : Array2DObjectDrawer<HPUIMeshContinuousInteractable> { }
}