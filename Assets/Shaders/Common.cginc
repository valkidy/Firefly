struct Particle
{
	uint ID;
	float3 Position;
	float3 Velocity;
	float LifeRandom;
	float Time;
};

struct ParticleVariant
{
	float Weight;
	float Life;
	float Size;
};

inline float nrand(float2 uv, float salt)
{
	uv += float2(salt, 0);
	return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
}
