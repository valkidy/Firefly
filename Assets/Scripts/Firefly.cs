using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Firefly
{
    public class Firefly : MonoBehaviour
    {
        public enum Type
        {
            GpuInstance = 0,
            GpuGeometry,
        }

        [Range(0, 1)] public float TimeFlow = 0;

        [Header("Variant type")]
        public Type VariantType = Type.GpuGeometry;

        [Header("Particle parameters")]
        [Range(1e-3F, 16F)] public float ParticleLife = 4F;
        [Range(1e-3F, 1F)] public float ParticleSize = 0.15F;        
        [Range(1e-3F, 32F)] public float Frequency = 10F;
        [Range(1e-3F, 32F)] public float Amplitude = 10F;
        
        [Header("Source mesh")]
        public Mesh mesh;

        [Header("Gpu options")]
        public ComputeShader KernelShader;
        public Material GpuInstanceMaterial;
        public Material GpuGeometryMaterial;
        
        IVariant<VariantData, RenderData> emitter;

        void Start()
        {            
            if (VariantType == Type.GpuGeometry)
                emitter = new ButterflyGpuGeometryParticle();
            else if (VariantType == Type.GpuInstance)
                emitter = new ButterflyGpuInstanceParticle();
                
            emitter.OnInit(
                new VariantData {
                    Amplitude = Amplitude,
                    Frequency = Frequency,
                    Life = ParticleLife,
                    Size = ParticleSize,
                }, 
                new RenderData {
                    KernelShader = KernelShader,
                    Mat = (VariantType == Type.GpuGeometry) ? GpuGeometryMaterial : GpuInstanceMaterial,
                    Mesh = mesh,
                    LocalToWorld = this.transform.localToWorldMatrix
            });
        }

        void OnDestroy()
        {
            emitter.OnFinalize();
        }

        void FixedUpdate()
        {
            emitter.OnUpdate(TimeFlow);
        }

        void Update()
        {
            emitter.OnRender();
        }        
    }
}

