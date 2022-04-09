using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Firefly
{
    public class Firefly : MonoBehaviour
    {
        public enum Type
        {
            ParticleMesh = 0,
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
        public Material Material;
        

        [Header("Source mesh")]
        public Mesh mesh;

        [Header("Gpu options")]
        public ComputeShader KernelShader;
        public Material GeometryMaterial;

        IVariant<VariantData, RenderData> emitter;

        void Start()
        {            
            if (VariantType == Type.GpuGeometry)
                emitter = new ButterflyGpuGeometryParticle();
            else if (VariantType == Type.ParticleMesh)
                emitter = new ButterflyMeshParticle();

            emitter.OnInit(
                new VariantData {
                    Amplitude = Amplitude,
                    Frequency = Frequency,
                    Life = ParticleLife,
                    Size = ParticleSize,
                }, 
                new RenderData {
                    KernelShader = KernelShader,
                    Mat = (VariantType == Type.GpuGeometry) ? GeometryMaterial : Material,
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

