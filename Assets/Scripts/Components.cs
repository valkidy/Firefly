using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Firefly
{
    struct Triangle
    {
        public Vector3 Vertex1; 
        public Vector3 Vertex2; 
        public Vector3 Vertex3;

        public Vector2 TexCoord1;
        public Vector2 TexCoord2;
        public Vector2 TexCoord3;

        public Vector3 Normal1;
        public Vector3 Normal2;
        public Vector3 Normal3;
    }

    struct Vertex
    {
        public Vector3 Position;
        public Vector2 TexCoord;
        public Vector3 Normal;

        public Vertex(Vector3 v, Vector2 uv, Vector3 n)
        {
            Position = v;
            TexCoord = uv;
            Normal = n;
        }
    }

    struct Particle
    {
        public uint ID;
        public Vector3 Position;
        public Vector3 Velocity;
        public float LifeRandom;
        public float Time;
    };

    struct ParticleVariant
    {
        public float Weight;
        public float Life;
        public float Size;
    };

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