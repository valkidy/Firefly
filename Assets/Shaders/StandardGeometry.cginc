// Standard geometry shader example
// https://github.com/keijiro/StandardGeometryShader

#include "UnityCG.cginc"
// #include "UnityStandardUtils.cginc"

inline float nrand(float2 uv, float salt)
{
	uv += float2(salt, 0);
	return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
}

// 
struct Particle
{
	float3 Position;
	float3 Velocity;
	float LifeRandom;
	float Time;
};

struct ButterflyParticle
{
	float Weight;
	float Life;
	float Size;
};

// Shader uniforms
half4 _Color;
sampler2D _MainTex;
float4 _MainTex_ST;

float _LocalTime;
float _ElapsedTime;

int _NumParticles;
StructuredBuffer<Particle> _ParticleBuffer;
StructuredBuffer<float3> _PositionBuffer;
StructuredBuffer<ButterflyParticle> _Variant;

// Vertex input attributes
struct Attributes
{
	float4 position : POSITION;
	float3 normal : NORMAL;
	float4 tangent : TANGENT;
	float2 texcoord : TEXCOORD;
};

// Fragment varyings
struct Varyings
{
	float4 position : SV_POSITION;
	float3 normal : NORMAL;
	float2 texcoord : TEXCOORD0;
	float4 tspace0 : TEXCOORD1;
	float4 tspace1 : TEXCOORD2;
	float4 tspace2 : TEXCOORD3;	
};

//
// Vertex stage
//

Attributes Vertex(Attributes input)
{
	// Only do object space to world space transform.
	// input.position = mul(unity_ObjectToWorld, input.position);
	input.position = input.position;
	input.normal = UnityObjectToWorldNormal(input.normal);
	input.tangent.xyz = UnityObjectToWorldDir(input.tangent.xyz);
	input.texcoord = TRANSFORM_TEX(input.texcoord, _MainTex);
	return input;
}

//
// Geometry stage
//

#if 0
Varyings VertexOutput(float3 wpos, half3 wnrm, half4 wtan, float2 uv)
{
	Varyings o;
	
	half3 bi = cross(wnrm, wtan) * wtan.w * unity_WorldTransformParams.w;
	o.position = UnityWorldToClipPos(float4(wpos, 1));
	o.normal = wnrm;
	o.texcoord = uv;
	o.tspace0 = float4(wtan.x, bi.x, wnrm.x, wpos.x);
	o.tspace1 = float4(wtan.y, bi.y, wnrm.y, wpos.y);
	o.tspace2 = float4(wtan.z, bi.z, wnrm.z, wpos.z);
	// o.ambient = ShadeSHPerVertex(wnrm, 0);
	return o;
}
#endif

Varyings VertexOutput(float3 wpos, half3 wnrm, float2 uv)
{
	Varyings o = (Varyings)0;

	// o.position = UnityWorldToClipPos(float4(wpos, 1));
	o.position = UnityObjectToClipPos(float4(wpos, 1));	
	o.normal = wnrm;
	o.texcoord = uv;
	return o;
}

float3 ConstructNormal(float3 v1, float3 v2, float3 v3)
{
	return normalize(cross(v2 - v1, v3 - v1));
}

#if 1
[maxvertexcount(12)]
void Geometry(
	triangle Attributes input[3], uint pid : SV_PrimitiveID,
	inout TriangleStream<Varyings> outStream
)
{
	int index = pid;
	// if (index >= _NumParticles)
	// 	index = _NumParticles - 1;

	Particle p = _ParticleBuffer[index];
	// Scaling with simple lerp	
	// float t_s = p.Time / (_Variant[0].Life * p.LifeRandom);
	float t_s = p.Time / _Variant[0].Life;
	float size = _Variant[0].Size * max(1e-2F, 1.0 - t_s);
	// float size = 1.0;

	// Look-at matrix from velocity
	float3 az = p.Velocity + (float3)0.001;
	// float3 az = (float3)0 + (float3)0.001;
	float3 ax = cross(float3(0, 1, 0), az);
	float3 ay = cross(az, ax);
	
	// Flapping
	// float freq = 8 + Random.Value01(pid + 10000) * 20;
	float freq = 8 + nrand((float2)0, pid + 10000) * 20;	
	// float freq = 8;
	// float flap = sin(freq * _ElapsedTime);
	float flap = sin(freq * p.Time);

	// Axis vectors
	ax = normalize(ax) * size;
	ay = normalize(ay) * size * flap;
	az = normalize(az) * size;

	// Vertices
	// var pos = p.Pos;
	float3 pos = _PositionBuffer[index];
	// float3 pos = (float3)0;

	float3 p0 = input[0].position.xyz;
	float3 p1 = input[1].position.xyz;
	float3 p2 = input[2].position.xyz;
	// float3 pc = (p0 + p1 + p2) / 3.0;

	float3 va1 = pos + p0;
	float3 va2 = pos + p1;
	float3 va3 = pos + p2;

	float3 vb1 = pos + az * 0.2f;
	float3 vb2 = pos - az * 0.2f;
	float3 vb3 = pos - ax + ay + az;
	float3 vb4 = pos - ax + ay - az;
	float3 vb5 = vb3 + ax * 2;
	float3 vb6 = vb4 + ax * 2;

	float p_t = saturate(p.Time);
	float3 v1 = lerp(va1, vb1, p_t);
	float3 v2 = lerp(va2, vb2, p_t);
	float3 v3 = lerp(va3, vb3, p_t);
	float3 v4 = lerp(va3, vb4, p_t);
	float3 v5 = lerp(va3, vb5, p_t);
	float3 v6 = lerp(va3, vb6, p_t);

	float2 uv1 = float2((float)pid/768.0, 0);// input[0].texcoord;
	float2 uv2 = float2((float)pid/768.0, 0);// input[1].texcoord;
	float2 uv3 = float2((float)pid/768.0, 0);// input[2].texcoord;
	
	float3 n1 = input[0].normal;
	float3 n2 = input[1].normal;
	float3 n3 = input[2].normal;

	// Output
	outStream.Append(VertexOutput(v1, n1, uv1));
	outStream.Append(VertexOutput(v2, n2, uv2));
	outStream.Append(VertexOutput(v5, n3, uv3));
	
	outStream.Append(VertexOutput(v5, n3, uv3));
	outStream.Append(VertexOutput(v2, n2, uv2));
	outStream.Append(VertexOutput(v6, n3, uv3));
							  
	outStream.Append(VertexOutput(v3, n3, uv3));
	outStream.Append(VertexOutput(v4, n3, uv3));
	outStream.Append(VertexOutput(v1, n1, uv1));
						  
	outStream.Append(VertexOutput(v1, n1, uv1));
	outStream.Append(VertexOutput(v4, n3, uv3));
	outStream.Append(VertexOutput(v2, n2, uv2));
}

#else
[maxvertexcount(15)]
void Geometry(
	triangle Attributes input[3], uint pid : SV_PrimitiveID,
	inout TriangleStream<Varyings> outStream
)
{
	// Vertex inputs
	float3 wp0 = input[0].position.xyz;
	float3 wp1 = input[1].position.xyz;
	float3 wp2 = input[2].position.xyz;

	float2 uv0 = input[0].texcoord;
	float2 uv1 = input[1].texcoord;
	float2 uv2 = input[2].texcoord;

	// Extrusion amount
	float ext = saturate(0.4 - cos(_LocalTime * UNITY_PI * 2) * 0.41);
	ext *= 1 + 0.3 * sin(pid * 832.37843 + _LocalTime * 88.76);

	// Extrusion points
	float3 offs = ConstructNormal(wp0, wp1, wp2) * ext;
	float3 wp3 = wp0 + offs;
	float3 wp4 = wp1 + offs;
	float3 wp5 = wp2 + offs;

	// Cap triangle
	float3 wn = ConstructNormal(wp3, wp4, wp5);
	float np = saturate(ext * 10);
	float3 wn0 = lerp(input[0].normal, wn, np);
	float3 wn1 = lerp(input[1].normal, wn, np);
	float3 wn2 = lerp(input[2].normal, wn, np);
	outStream.Append(VertexOutput(wp3, wn0, input[0].tangent, uv0));
	outStream.Append(VertexOutput(wp4, wn1, input[1].tangent, uv1));
	outStream.Append(VertexOutput(wp5, wn2, input[2].tangent, uv2));
	outStream.RestartStrip();

	// Side faces
	float4 wt = float4(normalize(wp3 - wp0), 1); // world space tangent
	wn = ConstructNormal(wp3, wp0, wp4);
	outStream.Append(VertexOutput(wp3, wn, wt, uv0));
	outStream.Append(VertexOutput(wp0, wn, wt, uv0));
	outStream.Append(VertexOutput(wp4, wn, wt, uv1));
	outStream.Append(VertexOutput(wp1, wn, wt, uv1));
	outStream.RestartStrip();

	wn = ConstructNormal(wp4, wp1, wp5);
	outStream.Append(VertexOutput(wp4, wn, wt, uv1));
	outStream.Append(VertexOutput(wp1, wn, wt, uv1));
	outStream.Append(VertexOutput(wp5, wn, wt, uv2));
	outStream.Append(VertexOutput(wp2, wn, wt, uv2));
	outStream.RestartStrip();

	wn = ConstructNormal(wp5, wp2, wp3);
	outStream.Append(VertexOutput(wp5, wn, wt, uv2));
	outStream.Append(VertexOutput(wp2, wn, wt, uv2));
	outStream.Append(VertexOutput(wp3, wn, wt, uv0));
	outStream.Append(VertexOutput(wp0, wn, wt, uv0));
	outStream.RestartStrip();
}
#endif

//
// Fragment phase
//
half4 Fragment(Varyings input) : SV_Target
{
	half4 color = half4(0.5 * (input.normal + 1), 1);
	// half4 color = half4(input.texcoord.x, 0, 0, 1);
	return color;
}

