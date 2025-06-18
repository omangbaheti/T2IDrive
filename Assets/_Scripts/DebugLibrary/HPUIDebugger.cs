using UnityEngine;

namespace HPUIDebug
{
    public class HPUIDebugger : MonoBehaviour
    {
        public static class DebugPoint
        {
            public static GameObject Create(Transform parent = null, float scale = 0f, Color color = default, string name = "")
            {
                GameObject point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                if(parent!=null) point.transform.SetParent(parent);
                DestroyImmediate(point.GetComponent<Collider>());
                if(scale>0f) point.transform.localScale *= scale;
                if(color!=default)
                {
                    point.AddComponent<HotSwapColor>();
                    point.GetComponent<HotSwapColor>().FetchMr();
                    point.GetComponent<HotSwapColor>().SetColor(color);
                }
                point.gameObject.name = name;
                return point;
            }
        }
    }
}