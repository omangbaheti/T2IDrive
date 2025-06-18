using UnityEngine;


    public class HotSwapColor : MonoBehaviour
    {
        [SerializeField] private Color color;
        [SerializeField] private bool hasEmission;
        [SerializeField] private Color emissionColor;
        [SerializeField] private float emissionIntensity;
        [SerializeField] private MeshRenderer mr;

        private MaterialPropertyBlock mpb;
        private static readonly int ShaderProp = Shader.PropertyToID("_Color");

        private MaterialPropertyBlock Mpb => mpb ??= new();

        private void OnEnable()
        {
            ApplyColor();
            mr = GetComponent<MeshRenderer>();
        }

        private void OnValidate()
        {
            ApplyColor();
        }
        
        public void SetColor(Color color)
        {
            this.color = color;
            ApplyColor();
        }

        public void FetchMr()
        {
            mr = GetComponent<MeshRenderer>();
        }

        private void ApplyColor()
        {
            Mpb.SetColor(ShaderProp, color);
            if (mr != null)
            {
                mr.SetPropertyBlock(Mpb);
            }
        }
    }
