Shader "Firefly/GpuInstancing"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
		Cull Off
		Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma target 5.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
				float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
				float3 normal : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

			struct Vertex
			{
				float3 vertex;
				float2 uv;
				float3 normal;
			};

			struct VertexBatch
			{
				Vertex data[12];
			};

            sampler2D _MainTex;
            float4 _MainTex_ST;
			StructuredBuffer<VertexBatch> _VertexBuffer;

            v2f vert (appdata v, uint id : SV_InstanceID)
            {				
				Vertex input = _VertexBuffer[id].data[v.uv.x];

                v2f o;
                o.vertex = UnityObjectToClipPos(float4(input.vertex, 1));
                o.uv = TRANSFORM_TEX(input.uv, _MainTex);
				o.normal = input.normal;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
				half4 diffuse = tex2D(_MainTex, i.uv);				
				half3 n = 0.5 * (i.normal + 1.0);
				// half4 col = diffuse;
				half4 col = half4(n, 1.0);
                return col;
            }
            ENDCG
        }
    }
}
