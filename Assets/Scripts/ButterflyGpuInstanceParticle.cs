using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Firefly
{    
    /// ToDo : How to update mesh vertices in gpu
    class ButterflyGpuInstanceParticle : IVariant<VariantData, RenderData>
    {        
        #region Internal members

        BulkMesh bulkMesh;
        VariantData variantData;
        RenderData renderData;
        ButterflyParticle variant;

        // List<Particle> Particles = new List<Particle>();
        // List<Triangle> Faces = new List<Triangle>();
        // List<Vertex> Vertices = new List<Vertex>();

        // Vertex[] VertexData;
        NativeArray<Particle> Particles;
        NativeArray<Triangle> Faces;
        NativeArray<Vertex> Vertices;
        NativeArray<VertexBatch> VertexBatchData;

        ComputeBuffer VertexBuffer;
        ComputeBuffer ArgsBuffer;
       
        int InstancingCount;

        #endregion

        #region Interface implementation

        public void OnInit(VariantData param, RenderData source)
        {
            variantData = param;
            renderData = source;

            variant = new ButterflyParticle() { Weight = 0.5F, Life = param.Life, Size = param.Size };

            bulkMesh = new BulkMesh();
            bulkMesh.AddTriangle(Vector3.one, Vector3.one, Vector3.one, 0 * Vector2.one, 1 * Vector2.one, 2 * Vector2.one);
            bulkMesh.AddTriangle(Vector3.one, Vector3.one, Vector3.one, 3 * Vector2.one, 4 * Vector2.one, 5 * Vector2.one);
            bulkMesh.AddTriangle(Vector3.one, Vector3.one, Vector3.one, 6 * Vector2.one, 7 * Vector2.one, 8 * Vector2.one);
            bulkMesh.AddTriangle(Vector3.one, Vector3.one, Vector3.one, 9 * Vector2.one, 10 * Vector2.one, 11 * Vector2.one);
            bulkMesh.Build();

            // Only support one submesh
            var mesh = renderData.Mesh;
            var vertices = new NativeArray<Vector3>(mesh.vertices, Allocator.TempJob);
            var indices = new NativeArray<int>(mesh.GetIndices(0), Allocator.TempJob);
            var normals = (mesh.normals != null)
                ? new NativeArray<Vector3>(mesh.normals, Allocator.TempJob)
                : new NativeArray<Vector3>(new Vector3[1], Allocator.TempJob);                
            var uv = (mesh.uv != null)
                ? new NativeArray<Vector2>(mesh.uv, Allocator.TempJob)
                : new NativeArray<Vector2>(new Vector2[1], Allocator.TempJob);

            InstancingCount = indices.Length / 3;

            Faces = new NativeArray<Triangle>(InstancingCount, Allocator.Persistent);
            Vertices = new NativeArray<Vertex>(InstancingCount, Allocator.Persistent);

            MeshConstructionJob jobConstructor = new MeshConstructionJob();
            jobConstructor.FacesOut = Faces;
            jobConstructor.VerticesOut = Vertices;
            jobConstructor.Vertices = vertices;
            jobConstructor.Indices = indices;
            jobConstructor.TexCoords = uv;
            jobConstructor.Normals = normals;
            JobHandle handleConstructor = jobConstructor.Schedule(InstancingCount, 1);
            handleConstructor.Complete();

            vertices.Dispose();
            indices.Dispose();
            normals.Dispose();
            uv.Dispose();
            // BuildVertexAndTriangle(renderData.Mesh);

            VertexBuffer = new ComputeBuffer(InstancingCount, Marshal.SizeOf(typeof(VertexBatch)));
            VertexBatchData = new NativeArray<VertexBatch>(InstancingCount, Allocator.Persistent);
            Particles = new NativeArray<Particle>(InstancingCount, Allocator.Persistent);

            InitParticleJob jobInitlalizer = new InitParticleJob();
            jobInitlalizer.Particles = Particles;
            jobInitlalizer.Vertices = Vertices;

            JobHandle handleInitlalizer = jobInitlalizer.Schedule(Particles.Length, 1);
            handleInitlalizer.Complete();
        }
        
        public void OnUpdate(float timeFlow)
        {            
            UpdateParticleJob jobUpdater = new UpdateParticleJob();
            jobUpdater.Particles = Particles;
            jobUpdater.Vertices = Vertices;
            jobUpdater.TimeFlow = timeFlow;
            jobUpdater.DeltaTime = Time.deltaTime;
            jobUpdater.Time = Time.timeSinceLevelLoad;
            jobUpdater.Life = variantData.Life;
            jobUpdater.Frequency = variantData.Frequency;
            jobUpdater.Amplitude = variantData.Amplitude;
            jobUpdater.LocalToWorld = renderData.LocalToWorld;

            JobHandle handleUpdater = jobUpdater.Schedule(Particles.Length, 1);

            ButterflyReconstructionJob jobReconstructor = new ButterflyReconstructionJob();
            jobReconstructor.VertexBatchOutput = VertexBatchData;
            jobReconstructor.Vertices = Vertices;
            jobReconstructor.Faces = Faces;
            jobReconstructor.Particles = Particles;
            jobReconstructor.Life = variantData.Life;
            jobReconstructor.Size = variantData.Size;

            JobHandle handleReconstructor = jobReconstructor.Schedule(Vertices.Length, 1, handleUpdater);
            handleReconstructor.Complete();

            VertexBuffer.SetData(VertexBatchData);
        }

        public void OnRender()
        {
            UpdateBufferIfNeeded();

            renderData.Mat.SetBuffer("_VertexBuffer", VertexBuffer);

            Graphics.DrawMeshInstancedIndirect(
                bulkMesh.mesh,
                0, renderData.Mat,
                new Bounds(Vector3.zero, Vector3.one * 1000F),
                ArgsBuffer,
                0,
                null,
                UnityEngine.Rendering.ShadowCastingMode.Off,
                false);
        }

        public void OnFinalize()
        {
            Particles.Dispose();
            Faces.Dispose(); 
            Vertices.Dispose();
            VertexBatchData.Dispose(); 

            VertexBuffer?.Release();
            ArgsBuffer?.Release();
        }

        #endregion

        #region Job 

        struct VertexBatch
        {
            public Vertex v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11;
        }

        [BurstCompile]
        struct MeshConstructionJob : IJobParallelFor
        {
            [WriteOnly]
            public NativeArray<Triangle> FacesOut;
            [WriteOnly]
            public NativeArray<Vertex> VerticesOut;
            [ReadOnly]
            public NativeArray<Vector3> Vertices;
            [ReadOnly]
            public NativeArray<int> Indices;
            [ReadOnly]
            public NativeArray<Vector3> Normals;
            [ReadOnly]
            public NativeArray<Vector2> TexCoords;

            public void Execute(int index)
            {
                var v0 = Vertices[Indices[3 * index]];
                var v1 = Vertices[Indices[3 * index + 1]];
                var v2 = Vertices[Indices[3 * index + 2]];
                var vc = (v0 + v1 + v2) / 3F;

                var uv0 = (TexCoords.Length > 1) ? TexCoords[Indices[3 * index]] : Vector2.zero;
                var uv1 = (TexCoords.Length > 1) ? TexCoords[Indices[3 * index + 1]] : Vector2.zero;
                var uv2 = (TexCoords.Length > 1) ? TexCoords[Indices[3 * index + 2]] : Vector2.zero;

                var n0 = (Normals.Length > 1) ? Normals[Indices[3 * index]] : Vector3.zero;
                var n1 = (Normals.Length > 1) ? Normals[Indices[3 * index + 1]] : Vector3.zero;
                var n2 = (Normals.Length > 1) ? Normals[Indices[3 * index + 2]] : Vector3.zero;

                FacesOut[index] = new Triangle
                {
                    Vertex1 = v0 - vc,
                    Vertex2 = v1 - vc,
                    Vertex3 = v2 - vc,

                    TexCoord1 = uv0,
                    TexCoord2 = uv1,
                    TexCoord3 = uv2,

                    Normal1 = n0,
                    Normal2 = n1,
                    Normal3 = n2,
                };

                VerticesOut[index] = new Vertex
                {
                    Position = vc
                };
            }
        }

        [BurstCompile]
        struct InitParticleJob : IJobParallelFor
        {
            public NativeArray<Particle> Particles;
            [ReadOnly]
            public NativeArray<Vertex> Vertices;

            public void Execute(int index)
            {
                Particles[index] = new Particle()
                {
                    ID = (uint)index,
                    Position = Vertices[index].Position,
                    Velocity = Vector3.zero,
                    LifeRandom = Random.Value01((uint)index) * 0.8f + 0.2f,
                    Time = 0,
                };
            }
        }

        [BurstCompile]
        struct UpdateParticleJob : IJobParallelFor
        {
            public NativeArray<Particle> Particles;
            [ReadOnly]
            public NativeArray<Vertex> Vertices;
            [ReadOnly]
            public float TimeFlow;
            [ReadOnly]
            public float DeltaTime;
            [ReadOnly]
            public float Time;
            [ReadOnly]
            public float Life;            
            [ReadOnly]
            public float Frequency;
            [ReadOnly]
            public float Amplitude;
            [ReadOnly]
            public Matrix4x4 LocalToWorld;

            public void Execute(int index)
            {
                var dt = DeltaTime;
                var time = Time;

                var particle = Particles[index];
                var life = Life * particle.LifeRandom;

                var pos = particle.Position;
                var acc = Utility.DFNoise(pos * time, Frequency) * Amplitude;

                var z = LocalToWorld.MultiplyVector(pos).z;
                dt *= Mathf.Clamp(z + 0.5f, 0.5F, 2F);                

                if (particle.Time > 1e-1F)
                {
                    particle.Velocity += acc * dt;
                }
                else
                {
                    particle.Velocity = Vector3.zero;
                }

                // Equals to (timeFlow > 0) ? dt : -dt            
                particle.Time = Mathf.Clamp(
                    particle.Time - Mathf.Sign(-TimeFlow) * dt, 0, 1.2F * life);

                var dir = Vertices[index].Position - pos;
                if (TimeFlow > 0 || dir.sqrMagnitude > 1e-1F)
                {
                    var invVelocity = dir + TimeFlow * particle.Velocity;
                    particle.Position += Math.Lerp(invVelocity * dt, particle.Velocity * dt, TimeFlow);
                }
                else
                {
                    particle.Position = Vertices[index].Position;
                }

                Particles[index] = particle;
            }
        }

        [BurstCompile]
        struct ButterflyReconstructionJob : IJobParallelFor
        {
            [WriteOnly]
            public NativeArray<VertexBatch> VertexBatchOutput;
            [ReadOnly]
            public NativeArray<Vertex> Vertices;
            [ReadOnly]
            public NativeArray<Triangle> Faces;
            [ReadOnly]
            public NativeArray<Particle> Particles;
            [ReadOnly]
            public float Life;
            [ReadOnly]
            public float Size;

            public void Execute(int index)
            {
                var vertex = Vertices[index];
                var face = Faces[index];
                var particle = Particles[index];

                // Scaling with simple lerp
                var t_s = particle.Time / (Life * particle.LifeRandom);
                var size = Size * Mathf.Max(1e-2F, 1 - t_s);

                // Look-at matrix from velocity
                var az = particle.Velocity + Vector3.one * 0.001f;
                var ax = Vector3.Cross(Vector3.up, az);
                var ay = Vector3.Cross(az, ax);

                // Flapping
                var freq = 8 + Random.Value01(particle.ID + 10000) * 20;
                var flap = Mathf.Sin(freq * particle.Time);

                // Axis vectors
                ax = (ax.normalized) * size;
                ay = (ay.normalized) * size * flap;
                az = (az.normalized) * size;

                // Vertices
                var pos = particle.Position;

                var va1 = pos + face.Vertex1;
                var va2 = pos + face.Vertex2;
                var va3 = pos + face.Vertex3;

                var vb1 = pos + az * 0.2f;
                var vb2 = pos - az * 0.2f;
                var vb3 = pos - ax + ay + az;
                var vb4 = pos - ax + ay - az;
                var vb5 = vb3 + ax * 2;
                var vb6 = vb4 + ax * 2;

                var p_t = Mathf.Clamp01(particle.Time);
                var v1 = va1.Lerp(vb1, p_t);
                var v2 = va2.Lerp(vb2, p_t);
                var v3 = va3.Lerp(vb3, p_t);
                var v4 = va3.Lerp(vb4, p_t);
                var v5 = va3.Lerp(vb5, p_t);
                var v6 = va3.Lerp(vb6, p_t);

                var uv1 = face.TexCoord1;
                var uv2 = face.TexCoord2;
                var uv3 = face.TexCoord3;

                var n1 = face.Normal1;
                var n2 = face.Normal2;
                var n3 = face.Normal3;

                // Output
                VertexBatchOutput[index] = new VertexBatch
                {
                    v0 = new Vertex(v1, uv1, n1),
                    v1 = new Vertex(v2, uv2, n2),
                    v2 = new Vertex(v5, uv3, n3),
                    v3 = new Vertex(v5, uv3, n3),
                    v4 = new Vertex(v2, uv2, n2),
                    v5 = new Vertex(v6, uv3, n3),
                    v6 = new Vertex(v3, uv3, n3),
                    v7 = new Vertex(v4, uv3, n3),
                    v8 = new Vertex(v1, uv1, n1),
                    v9 = new Vertex(v1, uv1, n1),
                    v10 = new Vertex(v4, uv3, n3),
                    v11 = new Vertex(v2, uv2, n2),
                };
            }
        }
        
        #endregion

        void BuildVertexAndTriangle(Mesh mesh)            
        {
            // Only support one submesh
            var vertices = mesh.vertices;
            var indices = mesh.GetIndices(0);
            var normals = mesh.normals;
            var uv = mesh.uv;

            InstancingCount = indices.Length / 3;
            
            Faces = new NativeArray<Triangle>(InstancingCount, Allocator.Persistent);
            Vertices = new NativeArray<Vertex>(InstancingCount, Allocator.Persistent);

            for (int i = 0; i < InstancingCount; ++i)
            {
                var v0 = vertices[indices[3 * i]];
                var v1 = vertices[indices[3 * i + 1]];
                var v2 = vertices[indices[3 * i + 2]];
                var vc = (v0 + v1 + v2) / 3F;

                var uv0 = (uv.Length > 0) ? uv[indices[3 * i]] : Vector2.zero;
                var uv1 = (uv.Length > 0) ? uv[indices[3 * i + 1]] : Vector2.zero;
                var uv2 = (uv.Length > 0) ? uv[indices[3 * i + 2]] : Vector2.zero;

                var n0 = (normals.Length > 0) ? normals[indices[3 * i]] : Vector3.zero;
                var n1 = (normals.Length > 0) ? normals[indices[3 * i + 1]] : Vector3.zero;
                var n2 = (normals.Length > 0) ? normals[indices[3 * i + 2]] : Vector3.zero;

                Faces[i] = new Triangle()
                {
                    Vertex1 = v0 - vc,
                    Vertex2 = v1 - vc,
                    Vertex3 = v2 - vc,

                    TexCoord1 = uv0,
                    TexCoord2 = uv1,
                    TexCoord3 = uv2,

                    Normal1 = n0,
                    Normal2 = n1,
                    Normal3 = n2,
                };

                // Vertices[i] = new Vertex(vc, Vector2.zero, Vector3.zero);
            }
        }
              
        void UpdateBufferIfNeeded()
        {
            if (ArgsBuffer != null)
                return;

            var mesh = bulkMesh.mesh;
            uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
            ArgsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            
            args[0] = (uint)mesh.GetIndexCount(0);
            args[1] = (uint)InstancingCount;
            args[2] = (uint)mesh.GetIndexStart(0);
            args[3] = (uint)mesh.GetBaseVertex(0);
            args[4] = 0;

            ArgsBuffer.SetData(args);
        }
    }
}

