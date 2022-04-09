using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Firefly
{
    class ButterflyGpuGeometryParticle : IVariant<VariantData, RenderData>
    {
        #region Internal members

        BulkMesh bulkMesh = new BulkMesh();
        VariantData variantData;
        RenderData renderData;

        ComputeShader kernelShader;
        Material material;

        ComputeBuffer Variant;
        ComputeBuffer ParticleBuffer;
        ComputeBuffer VertexBuffer;
        ComputeBuffer VerticesData;
        ComputeBuffer IndicesData;

        const int THREADS = 128;

        int BufferSize = 0;
        int Groups = 0;

        #endregion

        public void OnInit(VariantData param, RenderData source)
        {
            variantData = param;
            renderData = source;

            ConstructBulkMesh(source.Mesh, ref bulkMesh, ref BufferSize);

            // Init in compute shader
            var mesh = source.Mesh;
            kernelShader = source.KernelShader;
            material = source.Mat;

            ParticleBuffer = new ComputeBuffer(BufferSize, Marshal.SizeOf(typeof(Particle)));
            VertexBuffer = new ComputeBuffer(BufferSize, Marshal.SizeOf(typeof(Vector3)));
            Variant = new ComputeBuffer(1, Marshal.SizeOf(typeof(ParticleVariant)));
            VerticesData = new ComputeBuffer(mesh.vertexCount, Marshal.SizeOf(typeof(Vector3)));
            VerticesData.SetData(mesh.vertices);
            IndicesData = new ComputeBuffer((int)mesh.GetIndexCount(0), Marshal.SizeOf(typeof(int)));
            IndicesData.SetData(mesh.GetIndices(0));

            // KernelShader = source
            Groups = CalcGroup(BufferSize, THREADS);

            int kernel = kernelShader.FindKernel("InitVertices");
            kernelShader.SetBuffer(kernel, "_IndicesData", IndicesData);
            kernelShader.SetBuffer(kernel, "_VerticesData", VerticesData);
            kernelShader.SetBuffer(kernel, "_VertexBuffer", VertexBuffer);
            kernelShader.Dispatch(kernel, Groups, 1, 1);

            kernel = kernelShader.FindKernel("InitParticles");
            kernelShader.SetBuffer(kernel, "_VertexBuffer", VertexBuffer);
            kernelShader.SetBuffer(kernel, "_ParticleBuffer", ParticleBuffer);
            kernelShader.Dispatch(kernel, Groups, 1, 1);

            kernel = kernelShader.FindKernel("InitParticleVariant");
            kernelShader.SetFloat("_Weight", 0.5F);
            kernelShader.SetFloat("_Life", param.Life);
            kernelShader.SetFloat("_Size", param.Size);
            kernelShader.SetBuffer(kernel, "_Variant", Variant);
            kernelShader.Dispatch(kernel, 1, 1, 1);
        }

        public void OnRender()
        {
            Graphics.DrawMesh(bulkMesh.mesh, Matrix4x4.identity, renderData.Mat, 0);
        }

        public void OnUpdate(float timeFlow)
        {
            int kernel = kernelShader.FindKernel("ComputeParticles");
            kernelShader.SetFloat("_TimeFlow", timeFlow);
            kernelShader.SetFloat("_Amplitude", variantData.Amplitude);
            kernelShader.SetFloat("_Frequency", variantData.Frequency);
            kernelShader.SetMatrix("_LocalToWorld", renderData.LocalToWorld);
            kernelShader.SetBuffer(kernel, "_Variant", Variant);
            kernelShader.SetBuffer(kernel, "_ParticleBuffer", ParticleBuffer);
            kernelShader.SetBuffer(kernel, "_VertexBuffer", VertexBuffer);
            kernelShader.Dispatch(kernel, Groups, 1, 1);

            material.SetBuffer("_ParticleBuffer", ParticleBuffer);
            material.SetBuffer("_VertexBuffer", VertexBuffer);
            material.SetBuffer("_Variant", Variant);
            // material.SetFloat("_LocalTime", LocalTime);
        }

        public void OnFinalize()
        {
            VertexBuffer?.Release();
            ParticleBuffer?.Release();
            Variant?.Release();
            VerticesData?.Release();
            IndicesData?.Release();
        }

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
            
            for (int i = 0; i < bufferSize; ++i)
            {
                var v0 = vertices[indices[3 * i + 0]];
                var v1 = vertices[indices[3 * i + 1]];
                var v2 = vertices[indices[3 * i + 2]];
                var vc = (v0 + v1 + v2) / 3F;
                
                var uv0 = (uv.Length > 0) ? uv[indices[3 * i + 0]] : Vector2.zero;
                var uv1 = (uv.Length > 0) ? uv[indices[3 * i + 1]] : Vector2.zero;
                var uv2 = (uv.Length > 0) ? uv[indices[3 * i + 2]] : Vector2.zero;
                
                var n0 = (normals.Length > 0) ? normals[indices[3 * i + 0]] : Vector3.zero;
                var n1 = (normals.Length > 0) ? normals[indices[3 * i + 1]] : Vector3.zero;
                var n2 = (normals.Length > 0) ? normals[indices[3 * i + 2]] : Vector3.zero;

                bulkMesh.AddTriangle(v0 - vc, v1 - vc, v2 - vc, uv0, uv1, uv2, n0, n1, n2);
            }
            bulkMesh.Build();
        }
    }
}
