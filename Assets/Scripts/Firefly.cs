using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Firefly
{
    public class Firefly : MonoBehaviour
    {
        [Range(1e-3F, 16F)] public float particleLife = 4F;
        [Range(1e-3F, 1F)] public float particleSize = 0.15F;

        [Range(0, 1)] public float timeFlow = 0;
        [Range(1e-3F, 32F)] public float frequency = 10F;
        [Range(1e-3F, 32F)] public float amplitude = 10F;
        
        [SerializeField] Mesh mesh;
        [SerializeField] Material material;

        #region internal parameters

        BulkMesh bulkMesh;
        ButterflyParticle variant;

        List<Particle> particles = new List<Particle>();
        List<Triangle> faces = new List<Triangle>();
        List<Vertex> vertices = new List<Vertex>();

        #endregion

        void Start()
        {           
            BuildVertexAndTriangle(mesh, ref vertices, ref faces);
            
            bulkMesh = new BulkMesh();

            variant = new ButterflyParticle()
            {
                Weight = 0.5F,
                Life = particleLife,
                Size = particleSize
            };

            for (int i = 0; i < faces.Count; ++i)
            {
                particles.Add(new Particle()
                {
                    Velocity = Vector3.zero,
                    ID = (uint)i,
                    LifeRandom = Random.Value01((uint)i) * 0.8f + 0.2f,
                    Time = 0,
                });
            }
        }

        void FixedUpdate()
        {
            bulkMesh.Reset();

            for (int i = 0; i < particles.Count; ++i)
            {
                UpdateParticle(i, ref particles, ref vertices);

                ButterflyReconstruction(vertices[i], faces[i], particles[i], variant, ref bulkMesh);
            }

            bulkMesh.Build();
        }

        void Update()
        {
            Graphics.DrawMesh(bulkMesh.mesh, Matrix4x4.identity, material, 0);
        }

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

                var uv0 = uv[indices[3 * i]];
                var uv1 = uv[indices[3 * i + 1]];
                var uv2 = uv[indices[3 * i + 2]];

                _faces.Add(new Triangle
                {
                    Vertex1 = v0 - vc,
                    TexCoord1 = uv0,
                    Vertex2 = v1 - vc,
                    TexCoord2 = uv1,
                    Vertex3 = v2 - vc,
                    TexCoord3 = uv2,
                });

                _vertices.Add(new Vertex
                {
                    basePos = vc,
                    localPos = vc,
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
            var pos = vertex.localPos;

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

        float Amplitude(Vector3 p)
        {
            var localToWorld = this.transform.localToWorldMatrix;
            var z = localToWorld.MultiplyVector(p).z;
            return Mathf.Clamp(z + 0.5f, 0.5F, 2F);
        }

        void UpdateParticle(int index, ref List<Particle> particles, ref List<Vertex> vertices)
        {
            var dt = Time.deltaTime;
            var time = Time.timeSinceLevelLoad;

            var particle = particles[index];
            var life = variant.Life * particle.LifeRandom;

            var basePos = vertices[index].basePos;
            var currPos = vertices[index].localPos;
            var acc = Utility.DFNoise(currPos * time, frequency) * amplitude;

            dt *= Amplitude(currPos);

            if (particle.Time > 1e-1F)
            {
                particle.Velocity += acc * dt;
            }
            else
            {
                particle.Velocity = Vector3.zero;
            }

            particle.Time += (timeFlow > 0) ? dt : -dt;
            particle.Time = Mathf.Clamp(particle.Time, 0, 2F * life);

            if (timeFlow > 0 || (basePos - currPos).sqrMagnitude > 1e-1F)
            {
                var invVelocity = (basePos - currPos) + timeFlow * particle.Velocity;
                currPos += Math.Lerp(invVelocity * dt, particle.Velocity * dt, timeFlow);
            }
            else
            {
                currPos = basePos;
            }

            particles[index] = particle;
            vertices[index].localPos = currPos;
        }
    }
}

