using System;
using UnityEditor;

namespace ubco.ovilab.ViconUnityStream.Editor
{
    [CustomEditor(typeof(ViconOrigin), true)]
    [CanEditMultipleObjects]
    public class ViconOriginEditor : CustomSubjectScriptEditor
    {
        private SerializedProperty patternProperty;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            bool hasForward = false;
            bool hasRight = false;
            bool hasUp = false;
            int rightAxisCount = 0;
            int upAxisCount = 0;
            int forwardAxisCount = 0;



            // EditorGUILayout.PropertyField(patternProperty, includeChildren: true);
            patternProperty ??= serializedObject.FindProperty("subjectMarkerPattern");

            if (patternProperty != null && patternProperty.isArray)
            {
                for (int i = 0; i < patternProperty.arraySize; i++)
                {
                    SerializedProperty element = patternProperty.GetArrayElementAtIndex(i);
                    SerializedProperty axisProperty = element.FindPropertyRelative("axis");

                    ViconOrigin.Axis axis = (ViconOrigin.Axis)axisProperty.enumValueIndex;

                    switch (axis)
                    {
                        case ViconOrigin.Axis.PositiveForwardAxis:
                            hasForward = true;
                            forwardAxisCount += 1;
                            break;
                        case ViconOrigin.Axis.NegativeForwardAxis:
                            hasForward = true;
                            forwardAxisCount += 1;
                            break;
                        case ViconOrigin.Axis.PositiveRightAxis:
                            hasRight = true;
                            rightAxisCount += 1;
                            break;
                        case ViconOrigin.Axis.NegativeRightAxis:
                            hasRight = true;
                            rightAxisCount += 1;
                            break;
                        case ViconOrigin.Axis.PositiveUpAxis:
                            hasUp = true;
                            upAxisCount += 1;
                            break;
                        case ViconOrigin.Axis.NegativeUpAxis:
                            hasUp = true;
                            upAxisCount += 1;
                            break;
                        case ViconOrigin.Axis.None:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                }

            }

            if (rightAxisCount > 1)
            {
                EditorGUILayout.HelpBox(
                    "More than one Right axis is configured. Please specify only one",
                    MessageType.Warning
                );
            }

            if (upAxisCount > 1)
            {
                EditorGUILayout.HelpBox(
                    "More than one Up axis is configured. Please specify only one",
                    MessageType.Warning
                );
            }

            if (forwardAxisCount > 1)
            {
                EditorGUILayout.HelpBox(
                    "More than one Forward axis is configured. Please specify only one",
                    MessageType.Warning
                );
            }

            if (!(hasForward && (hasRight || hasUp)))
            {
                EditorGUILayout.HelpBox(
                    "Pattern must include at least one ForwardAxis and either RightAxis or UpAxis.",
                    MessageType.Warning
                );
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
