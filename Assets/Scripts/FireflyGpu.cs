using Firefly;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class FireflyGpu : MonoBehaviour
{
    [Range(0, 1)] public float TimeFlow = 0;

    [Header("Particle parameters")]
    [Range(1e-3F, 16F)] public float ParticleLife = 4F;
    [Range(1e-3F, 1F)] public float ParticleSize = 0.15F;
    [Range(1e-3F, 32F)] public float Frequency = 10F;
    [Range(1e-3F, 32F)] public float Amplitude = 10F;
    [Range(0, 4)] public float LocalTime = 0;
    public Material material;

    [Header("Kernel")]
    public ComputeShader KernelShader;

    [Header("Source mesh")]
    public Mesh mesh;

    #region internal parameters

    BulkMesh bulkMesh;

    ComputeBuffer Variant;
    ComputeBuffer ParticleBuffer;
    ComputeBuffer VertexBuffer;
    ComputeBuffer VerticesData;
    ComputeBuffer IndicesData;

    const int THREADS = 128;

    int BufferSize = 0;
    int Groups = 0;

    #endregion

    int CalcGroup(int numObjects, int numThreads)
    {        
        return Mathf.CeilToInt((float)numObjects / numThreads);
    }

    void ConstructBulkMesh(Mesh mesh, ref BulkMesh bulkMesh, ref int bufferSize)
    {
        Vector3[] vertices = mesh.vertices;
        int[] indices = mesh.GetIndices(0);
        Vector3[] normals = mesh.normals;
        Vector2[] uv = mesh.uv;

        bufferSize = indices.Length / 3;

        Vector2 uv0 = Vector2.zero, uv1 = Vector2.zero, uv2 = Vector2.zero;
        Vector3 n0 = Vector3.zero, n1 = Vector3.zero, n2 = Vector3.zero;

        for (int i = 0; i < bufferSize; ++i)
        {
            var v0 = vertices[indices[3 * i + 0]];
            var v1 = vertices[indices[3 * i + 1]];
            var v2 = vertices[indices[3 * i + 2]];
            var vc = (v0 + v1 + v2) / 3F;

            if (uv.Length > 0)
            {
                uv0 = uv[indices[3 * i + 0]];
                uv1 = uv[indices[3 * i + 1]];
                uv2 = uv[indices[3 * i + 2]];
            }

            if (normals.Length > 0)
            {
                n0 = normals[indices[3 * i + 0]];
                n1 = normals[indices[3 * i + 1]];
                n2 = normals[indices[3 * i + 2]];
            }
            bulkMesh.AddTriangle(v0 - vc, v1 - vc, v2 - vc, uv0, uv1, uv2, n0, n1, n2);
        }
        bulkMesh.Build();
    }

    // Start is called before the first frame update
    void Start()
    {
        bulkMesh = new BulkMesh();

        ConstructBulkMesh(mesh, ref bulkMesh, ref BufferSize);

        // Init in compute shader
        ParticleBuffer = new ComputeBuffer(BufferSize, Marshal.SizeOf(typeof(Particle)));
        VertexBuffer = new ComputeBuffer(BufferSize, Marshal.SizeOf(typeof(Vector3)));
        Variant = new ComputeBuffer(1, Marshal.SizeOf(typeof(ParticleVariant)));
        VerticesData = new ComputeBuffer(mesh.vertexCount, Marshal.SizeOf(typeof(Vector3)));
        VerticesData.SetData(mesh.vertices);
        IndicesData = new ComputeBuffer((int)mesh.GetIndexCount(0), Marshal.SizeOf(typeof(int)));
        IndicesData.SetData(mesh.GetIndices(0));
    
        Groups = CalcGroup(BufferSize, THREADS);

        int kernel = KernelShader.FindKernel("InitVertices");
        KernelShader.SetBuffer(kernel, "_IndicesData", IndicesData);        
        KernelShader.SetBuffer(kernel, "_VerticesData", VerticesData);
        KernelShader.SetBuffer(kernel, "_VertexBuffer", VertexBuffer);
        KernelShader.Dispatch(kernel, Groups, 1, 1);
        
        kernel = KernelShader.FindKernel("InitParticles");
        KernelShader.SetBuffer(kernel, "_VertexBuffer", VertexBuffer);        
        KernelShader.SetBuffer(kernel, "_ParticleBuffer", ParticleBuffer);        
        KernelShader.Dispatch(kernel, Groups, 1, 1);

        kernel = KernelShader.FindKernel("InitParticleVariant");
        KernelShader.SetFloat("_Weight", 0.5F);
        KernelShader.SetFloat("_Life", ParticleLife);
        KernelShader.SetFloat("_Size", ParticleSize);
        KernelShader.SetBuffer(kernel, "_Variant", Variant);
        KernelShader.Dispatch(kernel, 1, 1, 1);        
    }

    void OnDestroy()
    {
        VertexBuffer?.Release();
        ParticleBuffer?.Release();
        Variant?.Release();
        VerticesData?.Release();
        IndicesData?.Release();
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
        KernelShader.SetBuffer(kernel, "_VertexBuffer", VertexBuffer);        
        KernelShader.Dispatch(kernel, Groups, 1, 1);

        material.SetBuffer("_ParticleBuffer", ParticleBuffer);
        material.SetBuffer("_VertexBuffer", VertexBuffer);
        material.SetBuffer("_Variant", Variant);
        material.SetFloat("_LocalTime", LocalTime);        
    }
    
    void Update()
    {
        Graphics.DrawMesh(bulkMesh.mesh, Matrix4x4.identity, material, 0);
    }
}
