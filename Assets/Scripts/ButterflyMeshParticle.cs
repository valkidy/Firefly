using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Firefly
{    
    /// ToDo : How to update mesh vertices in gpu
    class ButterflyMeshParticle : IVariant<VariantData, RenderData>
    {        
        #region Internal members

        BulkMesh bulkMesh = new BulkMesh();
        VariantData variantData;
        RenderData renderData;
        ButterflyParticle variant;

        List<Particle> particles = new List<Particle>();
        List<Triangle> faces = new List<Triangle>();
        List<Vertex> vertices = new List<Vertex>();

        #endregion

        #region Interface implementation

        public void OnInit(VariantData param, RenderData source)
        {
            variantData = param;
            renderData = source;

            variant = new ButterflyParticle() { Weight = 0.5F, Life = param.Life, Size = param.Size };

            BuildVertexAndTriangle(renderData.Mesh, ref vertices, ref faces);

            for (int i = 0; i < faces.Count; ++i)
            {
                particles.Add(new Particle()
                {
                    ID = (uint)i,
                    Position = vertices[i].Position,
                    Velocity = Vector3.zero,
                    LifeRandom = Random.Value01((uint)i) * 0.8f + 0.2f,
                    Time = 0,
                });
            }
        }

        public void OnUpdate(float timeFlow)
        {
            bulkMesh.Reset();

            for (int i = 0; i < particles.Count; ++i)
            {
                UpdateParticle(timeFlow, i, ref particles, ref vertices);

                ButterflyReconstruction(vertices[i], faces[i], particles[i], variant, ref bulkMesh);
            }

            bulkMesh.Build();
        }

        public void OnRender()
        {
            Graphics.DrawMesh(bulkMesh.mesh, Matrix4x4.identity, renderData.Mat, 0);
        }

        public void OnFinalize(){}

        #endregion

        void BuildVertexAndTriangle(
            Mesh mesh,
            ref List<Vertex> _vertices,
            ref List<Triangle> _faces)
        {
            // Only support one submesh
            var vertices = mesh.vertices;
            var indices = mesh.GetIndices(0);
            var normals = mesh.normals;
            var uv = mesh.uv;

            for (int i = 0; i < indices.Length / 3; ++i)
            {
                var v0 = vertices[indices[3 * i]];
                var v1 = vertices[indices[3 * i + 1]];
                var v2 = vertices[indices[3 * i + 2]];
                var vc = (v0 + v1 + v2) / 3F;

                var uv0 = (uv.Length > 0) ? uv[indices[3 * i]] : Vector2.zero;
                var uv1 = (uv.Length > 0) ? uv[indices[3 * i + 1]] : Vector2.zero;
                var uv2 = (uv.Length > 0) ? uv[indices[3 * i + 2]] : Vector2.zero;

                _faces.Add(new Triangle
                {
                    Vertex1 = v0 - vc,
                    Vertex2 = v1 - vc,
                    Vertex3 = v2 - vc,

                    TexCoord1 = uv0,
                    TexCoord2 = uv1,
                    TexCoord3 = uv2,
                });

                _vertices.Add(new Vertex
                {
                    Position = vc
                });
            }
        }

        void ButterflyReconstruction(
            Vertex vertex,
            Triangle face,
            Particle particle,
            IParticleVariant variant,
            ref BulkMesh bulkMesh)
        {
            // Scaling with simple lerp
            var t_s = particle.Time / (variant.GetLife() * particle.LifeRandom);
            var size = variant.GetSize() * Mathf.Max(1e-2F, 1 - t_s);

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

            // Output
            bulkMesh.AddTriangle(v1, v2, v5, uv1, uv2, uv3);
            bulkMesh.AddTriangle(v5, v2, v6, uv3, uv2, uv3);
            bulkMesh.AddTriangle(v3, v4, v1, uv3, uv3, uv1);
            bulkMesh.AddTriangle(v1, v4, v2, uv1, uv3, uv2);
        }

        float CalcAmplitude(Vector3 p)
        {
            var localToWorld = renderData.LocalToWorld;
            var z = localToWorld.MultiplyVector(p).z;
            return Mathf.Clamp(z + 0.5f, 0.5F, 2F);
        }

        void UpdateParticle(float timeFlow, int index, ref List<Particle> particles, ref List<Vertex> vertices)
        {
            var dt = Time.deltaTime;
            var time = Time.timeSinceLevelLoad;

            var particle = particles[index];
            var life = variant.Life * particle.LifeRandom;

            var pos = particle.Position;
            var acc = Utility.DFNoise(pos * time, variantData.Frequency) * variantData.Amplitude;

            dt *= CalcAmplitude(pos);

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
                particle.Time - Mathf.Sign(-timeFlow) * dt, 0, 1.2F * life);

            var dir = vertices[index].Position - pos;
            if (timeFlow > 0 || dir.sqrMagnitude > 1e-1F)
            {
                var invVelocity = dir + timeFlow * particle.Velocity;
                particle.Position += Math.Lerp(invVelocity * dt, particle.Velocity * dt, timeFlow);
            }
            else
            {
                particle.Position = vertices[index].Position;
            }

            particles[index] = particle;
        }
    }
}

