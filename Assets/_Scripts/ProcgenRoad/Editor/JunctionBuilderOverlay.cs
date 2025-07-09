using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Overlays;
using UnityEditor.Splines;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UIElements;
using static UnityEditor.Splines.SplineToolUtility;

[Overlay(typeof(SceneView), "Junction Builder", true)]
public class JunctionBuilderOverlay : Overlay
{
    private Label SelectionInfoLabel;
    private VisualElement root;
    private Button addJunction;
    public override VisualElement CreatePanelContent()
    {
        root = new() { name = "My Toolbar Root" };
        SelectionInfoLabel = new("Junction Overlay");
        addJunction = new(OnAddJunction)
        {
            text = "Add Junction"
        };

        root.Add(SelectionInfoLabel);
        root.Add(addJunction);
        EditorApplication.update += OnSelectionChanged;
        return root;
    }

    public override void OnWillBeDestroyed()
    {
        base.OnWillBeDestroyed();
        Selection.selectionChanged -= OnSelectionChanged;
    }

    private bool IsSplineMode()
    {
        return ToolManager.activeToolType?.Name.Contains("Spline") == true;
    }

    private void OnSelectionChanged()
    {
        if(!IsSplineMode()) return;
        ClearSelectionInfo();
        List<SelectedSplineElementInfo> infos = SplineToolUtility.GetSelection();
        foreach (SelectedSplineElementInfo element in infos)
        {
            SelectionInfoLabel.text += $"Spline {element.targetIndex}, Knot {element.knotIndex}\n";
        }
    }

    private void OnAddJunction()
    {
        List<SelectedSplineElementInfo> selections = SplineToolUtility.GetSelection();

        Intersection intersection = new();

        foreach (SelectedSplineElementInfo selection in selections)
        {
            SplineContainer container = (SplineContainer) selection.target;
            Spline spline = container.Splines[selection.targetIndex];
            intersection.AddTerminal(new SplineTerminalInfo(selection.targetIndex, selection.knotIndex, spline, spline.Knots.ToList()[selection.knotIndex]));
        }
        
        Selection.activeTransform.GetComponent<SplineRoad>().AddJunction(intersection);
    }

    private void ClearSelectionInfo()
    {
        SelectionInfoLabel.text = "Junction Overlay\n";
    }
}




