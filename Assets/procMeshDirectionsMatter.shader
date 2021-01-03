Shader "Unlit/procMeshDirectionMatters"
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
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            StructuredBuffer<float4x4> _PointBuffer;
            int _TotalCurvePoints;
            int _Resolution;
            float _CurveWidth;
            float _CurveLength;
            float _CurveStart;
            float _VelocityImportance;

float3 cubicCurve( float t , float3  c0 , float3 c1 , float3 c2 , float3 c3 ){

  float s  = 1. - t;

  float3 v1 = c0 * ( s * s * s );
  float3 v2 = 3. * c1 * ( s * s ) * t;
  float3 v3 = 3. * c2 * s * ( t * t );
  float3 v4 = c3 * ( t * t * t );

  float3 value = v1 + v2 + v3 + v4;

  return value;

}





 void GetCubicInformation( float val , out float3 position , out float3 direction , out float3 tangent ){


  float3 p0 = 0;
  float3 v0 = 0;
  float3 p1 = 0;
  float3 v1 = 0;

  float3 p2 = float3( 0. , 0. , 0. );

  float vPP = float(_TotalCurvePoints);

  float base = val * (vPP-1);

  int baseUp   = int(floor( base ));
  int baseDown = int(ceil( base ));


// TOD MAKE UNIFORM
  float3 up = float3(0,1,0);



  if( baseUp == baseDown  || (base % 1) == 0){

    float4x4 pointMatrix = _PointBuffer[base];

    position = mul( pointMatrix , float4(0,0,0,1)).xyz;
    direction = normalize(mul( pointMatrix , float4(0,0,1,0)).xyz);

    
    tangent = normalize(mul( pointMatrix , float4(1,0,0,0)).xyz);
    
  }else{
    float amount = base - float(baseUp);
    float4x4 pointMatrixUp = _PointBuffer[baseUp];
    float4x4 pointMatrixDown = _PointBuffer[baseDown];
   p0 = mul( pointMatrixUp , float4(0,0,0,1)).xyz;
   p1 = mul( pointMatrixDown , float4(0,0,0,1)).xyz;

   
   v0 = 20*mul( pointMatrixUp , float4(0,0,1,0)).xyz;
   v1 = 20*mul( pointMatrixDown , float4(0,0,1,0)).xyz;


   


    // Todo : make the /3 a uniform also
    float3 c0 = p0;
    float3 c1 = p0 + v0/(3./_VelocityImportance);
    float3 c2 = p1 - v1/(3./_VelocityImportance);
    float3 c3 = p1;

    float3 pos = cubicCurve( amount , c0 , c1 , c2 , c3 );

    float3 upPos = cubicCurve( amount  + .001 , c0 , c1 , c2 , c3 );

    position = pos;
    direction = normalize(upPos - pos);   

    
    tangent = lerp( normalize(mul( pointMatrixUp , float4(1,0,0,0)).xyz) , normalize(mul( pointMatrixDown , float4(1,0,0,0)).xyz) , amount);
  


  }


}



            v2f vert (uint vid : SV_VertexID)
            {
                v2f o;

                int quadID = vid /6;
                int inQuadID = vid % 6;

                float val = float(quadID) / float(_Resolution);
                float valUp = float(quadID+1) / float(_Resolution);


                val *= _CurveLength;
                val += _CurveStart;

                
                valUp *= _CurveLength;
                valUp += _CurveStart;

                float3 pos; float3 dir; float3 tang;
                float3 posUp; float3 dirUp; float3 tangUp;

                GetCubicInformation( val , pos , dir , tang );
                GetCubicInformation( valUp , posUp , dirUp , tangUp );

                float3 p1 = pos - tang * _CurveWidth;
                float3 p2 = pos + tang * _CurveWidth;
                float3 p3 = posUp - tangUp * _CurveWidth;
                float3 p4 = posUp + tangUp * _CurveWidth;


                float3 fPos;
                float2 fUV;

                if( inQuadID == 0 ){
                    fPos = p1;
                    fUV = float2( 0,val);
                }else if( inQuadID == 1 ){
                    fPos = p2;
                    fUV = float2( 1,val);
                }else if( inQuadID == 2 ){                         
                    fPos = p4;
                    fUV = float2( 1,valUp);
                }else if( inQuadID == 3 ){
                    fPos = p1;
                    fUV = float2( 0,val);
                }else if( inQuadID == 4 ){
                    fPos = p4;
                    fUV = float2( 1,valUp);
                }else{
                    fPos = p3;
                    fUV = float2( 0,valUp);
                }

                o.uv = fUV;
                o.vertex = mul(UNITY_MATRIX_VP , float4(fPos,1));//(v.vertex);
              

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = sin(i.uv.y * 100) + sin(i.uv.x * 40);//float4(1,1,1,1);
                return col;
            }
            ENDCG
        }
    }
}
