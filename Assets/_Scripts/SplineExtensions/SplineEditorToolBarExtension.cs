using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Splines
{
    // This is kind of a hacky solution to get reference to the selected spline knot by using unity internal api's
    //This needs to be placed in a folder with a Assemby Definition Reference with a reference to Unity.Splines.Editor
    public static class SplineToolUtility
    {
        public static bool HasSelection()
        {
            return SplineSelection.HasActiveSplineSelection();
        }

        public static List<SelectedSplineElementInfo> GetSelection()
        {
            List<SelectableSplineElement> elements = SplineSelection.selection;
            List<SelectedSplineElementInfo> infos = new();

            foreach (SelectableSplineElement element in elements)      
            {
                infos.Add(new SelectedSplineElementInfo(element.target, element.targetIndex, element.knotIndex));
            }

            return infos;
        }

        public struct SelectedSplineElementInfo
        {
            public Object target;
            public int targetIndex;
            public int knotIndex;

            public SelectedSplineElementInfo(Object target, int targetIndex, int knotIndex)
            {
                this.target = target;
                this.targetIndex = targetIndex;
                this.knotIndex = knotIndex;
            }
        }
    }
}
