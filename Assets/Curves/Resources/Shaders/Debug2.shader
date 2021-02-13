Shader "Unlit/Curvey2"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

               // input info
            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float4 texcoord : TEXCOORD0;
                uint id : SV_VertexID;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            StructuredBuffer<float4x4> _PointBuffer;

            sampler2D _MainTex;
            float4 _MainTex_ST;

              #pragma target 4.5

            v2f vert (appdata IN)
            {

                int id = IN.id /(2 );
                int which = IN.id%2;

                float4x4 p = _PointBuffer[id];

                float3 left = mul( p , float4(1,0,0,0));
                float3 center = mul( p , float4(0,0,0,1));

                v2f o;

                float3 fPos = center;
                fPos += (float(which) -.5) * left;// * w;

                o.vertex = mul( UNITY_MATRIX_VP , float4(fPos,1));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = 1;
                return col;
            }
            ENDCG
        }
    }
}
