using UnityEngine;

namespace Firefly
{
    interface IVariant<TParam, TAsset>
        where TParam : struct
        where TAsset : struct
    {
        void OnInit(TParam param, TAsset source);
        void OnUpdate(float timeFlow);
        void OnRender();
        void OnFinalize();
    }

    struct VariantData
    {
        public float Life;
        public float Size;
        public float Frequency;
        public float Amplitude;
    }

    struct RenderData
    {
        public Matrix4x4 LocalToWorld;
        public Material Mat;
        public Mesh Mesh;
        public ComputeShader KernelShader;
    }
}
