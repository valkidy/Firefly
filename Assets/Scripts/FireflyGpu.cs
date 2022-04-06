﻿using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class FireflyGpu : MonoBehaviour
{
    struct Particle
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public float LifeRandom;
        public float Time;
    };

    struct ButterflyParticle
    {
        public float Weight;
        public float Life;
        public float Size;
    };

    [Range(0, 1)] public float TimeFlow = 0;
    [Range(1e-3F, 32F)] public float Frequency = 10F;
    [Range(1e-3F, 32F)] public float Amplitude = 10F;
    [Range(0, 1)] public float ElapsedTime = 0;
    
    public ComputeShader KernelShader;
    public Material materal;

    ComputeBuffer Variant;
    ComputeBuffer ParticleBuffer;
    ComputeBuffer PositionBuffer;
    int BufferSize = 0;
    const int THREADS = 128;
    Particle[] ParticleData;
    Vector3[] PositionData;

    static int CalcGroup(int numObjects, int numThreads)
    {
        // return numObjects / numThreads + Mathf.CeilToInt(numObjects % numThreads);
        return Mathf.CeilToInt((float)numObjects / numThreads);
    }

    // Start is called before the first frame update
    void Start()
    {
        var mesh = GetComponent<MeshFilter>().sharedMesh;
        Vector3[] vertices = mesh.vertices;
        int[] indices = mesh.GetIndices(0);
        BufferSize = 18 * indices.Length/3;
        PositionData = new Vector3[BufferSize];
        for (int i = 0; i < BufferSize/ 18; ++i) {
            var v0 = vertices[indices[3 * i + 0]];
            var v1 = vertices[indices[3 * i + 1]];
            var v2 = vertices[indices[3 * i + 2]];
            PositionData[i] = (v0 + v1 + v2) / 3F;
        }
        
        // TODO: Init in compute shader
        PositionBuffer = new ComputeBuffer(BufferSize, Marshal.SizeOf(typeof(Vector3)));
        PositionBuffer.SetData(PositionData);

        ParticleBuffer = new ComputeBuffer(BufferSize, Marshal.SizeOf(typeof(Particle)));
        ParticleData = new Particle[BufferSize];
        ParticleBuffer.SetData(ParticleData);

        Variant = new ComputeBuffer(1, Marshal.SizeOf(typeof(ButterflyParticle)));
        var VariantData = new ButterflyParticle[1] { new ButterflyParticle() {
            Weight = 0.5F,
            Life = 64F,
            Size = 0.5F
        }};
        Variant.SetData(VariantData);
    }

    void OnDestroy()
    {
        PositionBuffer.Release();
        ParticleBuffer.Release();
        Variant.Release();
    }

    void FixedUpdate()
    {     
        int kernel = KernelShader.FindKernel("ComputeParticles");
        KernelShader.SetFloat("_TimeFlow", TimeFlow);
        KernelShader.SetFloat("_Amplitude", Amplitude);
        KernelShader.SetFloat("_Frequency", Frequency);
        KernelShader.SetMatrix("_LocalToWorld", this.transform.localToWorldMatrix);
        KernelShader.SetBuffer(kernel, "_Variant", Variant);
        KernelShader.SetBuffer(kernel, "_ParticleBuffer", ParticleBuffer);
        KernelShader.SetBuffer(kernel, "_PositionBuffer", PositionBuffer);

        int Groups = CalcGroup(BufferSize, THREADS);
        KernelShader.Dispatch(kernel, Groups, 1, 1);

        // ParticleBuffer.GetData(ParticleData);
        // Debug.Log(ParticleData[0].Time + ", " + ParticleData[0].Velocity);
        // PositionBuffer.GetData(PositionData);
        // Debug.Log(PositionData[0]);
        // Debug.Log("BufferSize=" + BufferSize);

        materal.SetInt("_NumParticles", BufferSize);
        materal.SetBuffer("_ParticleBuffer", ParticleBuffer);
        materal.SetBuffer("_PositionBuffer", PositionBuffer);
        materal.SetBuffer("_Variant", Variant);
        materal.SetFloat("_ElapsedTime", ElapsedTime);        
    }
    
    void Update()
    {
        
    }
}
