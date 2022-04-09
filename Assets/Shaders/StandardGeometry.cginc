// Standard geometry shader example
// https://github.com/keijiro/StandardGeometryShader

#include "UnityCG.cginc"
#include "Common.cginc"

// Shader uniforms
sampler2D _MainTex;
float4 _MainTex_ST;
float _LocalTime;
StructuredBuffer<Particle> _ParticleBuffer;
StructuredBuffer<float3> _VertexBuffer;
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
};

//
// Vertex stage
//
Attributes Vertex(Attributes input)
{
	// Only do object space.	
	input.position = input.position;
	input.normal = UnityObjectToWorldNormal(input.normal);
	input.tangent.xyz = UnityObjectToWorldDir(input.tangent.xyz);
	input.texcoord = TRANSFORM_TEX(input.texcoord, _MainTex);
	return input;
}

//
// Geometry stage
//
Varyings VertexOutput(float3 wpos, half3 wnrm, float2 uv)
{
	// Do clip space transform after particle mesh constructed.
	Varyings o = (Varyings)0;	
	o.position = UnityObjectToClipPos(float4(wpos, 1));	
	o.normal = wnrm;
	o.texcoord = uv;
	return o;
}

[maxvertexcount(12)]
void Geometry(
	triangle Attributes input[3], uint pid : SV_PrimitiveID,
	inout TriangleStream<Varyings> outStream
)
{	
	Particle p = _ParticleBuffer[pid];

	float time = p.Time;
	float lifeRandom = p.LifeRandom;
	float3 velocity = p.Velocity;
	
	// Scaling with simple lerp	
	float t_s = time / (_Variant[0].Life * lifeRandom);
	float size = _Variant[0].Size * max(1e-2F, 1.0 - t_s);
		
	// Look-at matrix from velocity
	float3 az = velocity + (float3)0.001;
	float3 ax = cross(float3(0, 1, 0), az);
	float3 ay = cross(az, ax);
	
	// Flapping
	float freq = 8 + nrand((float2)0, pid + 10000) * 20;
	float flap = sin(freq * time);

	// Axis vectors
	ax = normalize(ax) * size;
	ay = normalize(ay) * size * flap;
	az = normalize(az) * size;

	// Vertices	
	float3 pos = p.Position;
	
	float3 p0 = input[0].position.xyz;
	float3 p1 = input[1].position.xyz;
	float3 p2 = input[2].position.xyz;	

	float3 va1 = pos + p0;
	float3 va2 = pos + p1;
	float3 va3 = pos + p2;

	/*
		   vb4      vb6
		  /   \   ⁄   /
		vb3 -- vb2 - vb5
			\  /   ⁄
			  vb1
	 */
	float3 vb1 = pos + az * 0.2f;
	float3 vb2 = pos - az * 0.2f;
	float3 vb3 = pos - ax + ay + az;
	float3 vb4 = pos - ax + ay - az;
	float3 vb5 = vb3 + ax * 2;
	float3 vb6 = vb4 + ax * 2;
	
	float p_t = saturate(time);
	float3 v1 = lerp(va1, vb1, p_t);
	float3 v2 = lerp(va2, vb2, p_t);
	float3 v3 = lerp(va3, vb3, p_t);
	float3 v4 = lerp(va3, vb4, p_t);
	float3 v5 = lerp(va3, vb5, p_t);
	float3 v6 = lerp(va3, vb6, p_t);

	float2 uv1 = input[0].texcoord;
	float2 uv2 = input[1].texcoord;
	float2 uv3 = input[2].texcoord;

	float3 n1 = input[0].normal;
	float3 n2 = input[1].normal;
	float3 n3 = input[2].normal;

	// Output
	outStream.Append(VertexOutput(v1, n1, uv1));
	outStream.Append(VertexOutput(v3, n3, uv2));
	outStream.Append(VertexOutput(v2, n2, uv3));

	outStream.Append(VertexOutput(v2, n2, uv1));
	outStream.Append(VertexOutput(v3, n3, uv2));
	outStream.Append(VertexOutput(v4, n3, uv3));

	outStream.Append(VertexOutput(v1, n1, uv1));
	outStream.Append(VertexOutput(v5, n3, uv2));
	outStream.Append(VertexOutput(v2, n2, uv3));
	
	outStream.Append(VertexOutput(v2, n2, uv1));
	outStream.Append(VertexOutput(v5, n3, uv2));
	outStream.Append(VertexOutput(v6, n3, uv3));
}

//
// Fragment phase
//
half4 Fragment(Varyings input) : SV_Target
{
	half4 color = half4(0.5 * (input.normal + 1), 1);
	// half4 color = half4(input.texcoord.x, 0, 0, 1);
	return color;
}

