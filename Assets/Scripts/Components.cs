using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Firefly
{
    struct Triangle
    {
        public Vector3 Vertex1; public Vector2 TexCoord1;
        public Vector3 Vertex2; public Vector2 TexCoord2;
        public Vector3 Vertex3; public Vector2 TexCoord3;
    }

    class Vertex
    {
        public Vector3 basePos;
        public Vector3 localPos;        
    }

    class BulkMesh
    {
        Mesh _mesh;
        public Mesh mesh { get { return _mesh; } }

        List<Vector3> vertices;
        List<Vector2> uv;
        List<Vector3> normals;
        List<int> indices;

        public BulkMesh()
        {
            _mesh = new Mesh();

            vertices = new List<Vector3>();
            uv = new List<Vector2>();
            normals = new List<Vector3>();
            indices = new List<int>();
        }

        public void Reset()
        {
            vertices.Clear();
            uv.Clear();
            normals.Clear();
            indices.Clear();
        }

        public void Build()
        {
            // When index count > 65535.
            _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            _mesh.vertices = vertices.ToArray();
            _mesh.normals = normals.ToArray();
            _mesh.uv = uv.ToArray();

            _mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            _mesh.Optimize();

            // This only for temporary use. Don't save.
            _mesh.hideFlags = HideFlags.DontSave;

            // Avoid being culled.
            _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000);
        }

        public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Vector2 uv1, Vector2 uv2, Vector2 uv3)
        {
            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);

            uv.Add(uv1);
            uv.Add(uv2);
            uv.Add(uv3);

            var n = -Vector3.Cross(v2 - v1, v3 - v1).normalized;
            /// FIXME: change (v1 + v2 + v3) / 3f to  (v1 + v2 + v3) / 3f - center in mesh vertices 
            if (Vector3.Dot(n, (v1 + v2 + v3) / 3f) < 0)
                n = -n;

            normals.Add(n);
            normals.Add(n);
            normals.Add(n);

            var i = indices.Count;
            indices.Add(i);
            indices.Add(i + 1);
            indices.Add(i + 2);
        }    
    }

    struct Particle
    {
        public Vector3 Velocity;
        public uint ID;
        public float LifeRandom;
        public float Time;
    }

    interface IParticleVariant
    {
        float GetSize();
        float GetLife();
        float GetWeight();
    }

    struct ButterflyParticle : IParticleVariant
    {
        public float Weight;
        public float GetWeight() { return Weight; }

        public float Life;
        public float GetLife() { return Life; }

        public float Size;
        public float GetSize() { return Size; }
    }
}