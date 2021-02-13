Shader "Unlit/procMeshDirectionMatters"
{
    Properties
    {

        _Size ("Size", Float) = 0.1
        _AlphaCuttoff ("AlphaCuttoff", Float) = 0.1
        _SpriteSize("SpriteSize",int) = 1
        _MainTex ("tex" , 2D )  = "white" {}
        _BumpMap ("bump map" , 2D )  = "white" {}
        _StartingColor("_StartingColor", Color ) = (1,1,1,1)
        _EndingColor("_EndingColor", Color ) = (1,1,1,1)
        _Metallic("_Metallic" , float )  = 0
        _Smoothness("_Smoothness" , float )  = 0
    
    }
    SubShader
    {

        Cull Off
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, making it so we add shadows
        #pragma surface surf Standard   vertex:vert addshadow
        #pragma shader_feature WORLD_NORMAL
        // Use shader model 4.5 to make sure we have access to compute buffer
        #pragma target 4.5

            #include "UnityCG.cginc"
       
    
      

            // input info
            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float4 texcoord : TEXCOORD0;
                uint id : SV_VertexID;
            };


            // info passed to surface shader
            struct Input{
                float2 uv_BumpMap;
                float2 uv_MainTex;
                float2 uv_Band;
            };   

            sampler2D _MainTex;
            sampler2D _BumpMap;
 
            #if defined(SHADER_API_D3D11) || defined(SHADER_API_PSSL) || defined(SHADER_API_METAL) || defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN) || defined(SHADER_API_SWITCH) // D3D11, D3D12, XB1, PS4, iOS, macOS, tvOS, glcore, gles3, webgl2.0, Switch
                StructuredBuffer<float4x4> _PointBuffer;
                StructuredBuffer<float3> _PowerBuffer;
            #endif

            #include "CurveInfo.cginc"


            void vert (inout appdata v,out Input o) 
            {
             

                int vid = v.id;
                
                UNITY_INITIALIZE_OUTPUT(Input,o);

                #if defined(SHADER_API_D3D11) || defined(SHADER_API_PSSL) || defined(SHADER_API_METAL) || defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN) || defined(SHADER_API_SWITCH) // D3D11, D3D12, XB1, PS4, iOS, macOS, tvOS, glcore, gles3, webgl2.0, Switch

                int quadID = vid /6;

                int rowID = quadID / _ResolutionX;
                int colID = quadID % _ResolutionX;
                int inQuadID = vid % 6;

                float val = float(rowID) / float(_Resolution);
                float valUp = float(rowID+1) / float(_Resolution);

                float col = float(colID) / float(_ResolutionX);
                float colUp = float(colID+1) / float(_ResolutionX);


                val *= _CurveLength;
                val += _CurveStart;

                
                valUp *= _CurveLength;
                valUp += _CurveStart;

                float3 pos; float3 dir; float3 tang; float width;
                float3 posUp; float3 dirUp; float3 tangUp; float widthUp;

                GetCubicInformation( val , pos , dir , tang , width );
                GetCubicInformation( valUp , posUp , dirUp , tangUp , widthUp);

                float3 p1 = pos   + (col   -.5)* tang * _CurveWidthMultiplier * width;
                float3 p2 = pos   + (colUp -.5)* tang * _CurveWidthMultiplier * width;
                float3 p3 = posUp + (col   -.5)* tangUp * _CurveWidthMultiplier* widthUp;
                float3 p4 = posUp + (colUp -.5)* tangUp * _CurveWidthMultiplier* widthUp;

                float3 fNor;
                float3 fTan;

                float3 nor = normalize(cross(dir,tang));
                float3 norUp = normalize(cross(dirUp,tangUp));

                float3 fPos;
                float2 fUV;

                 if( inQuadID == 0 ){
                    fPos = p1;
                    fUV = float2( col,val);
                    fNor = nor;
                    fTan = tang;
                }else if( inQuadID == 1 ){
                    fPos = p2;
                    fUV = float2( colUp,val);
                    fNor = nor;
                    fTan = tang;
                }else if( inQuadID == 2 ){                         
                    fPos = p4;
                    fUV = float2( colUp,valUp);
                    fNor = norUp;
                    fTan = tangUp;
                }else if( inQuadID == 3 ){
                    fPos = p1;
                    fUV = float2( col,val);
                    fNor = nor;
                    fTan = tang;
                }else if( inQuadID == 4 ){
                    fPos = p4;
                    fUV = float2( colUp,valUp);
                    fNor = norUp;
                    fTan = tangUp;
                }else{
                    fPos = p3;
                    fUV = float2( col,valUp);
                    fNor = norUp;
                    fTan = tangUp;
                }

                v.texcoord = float4( fUV, 0, 0 );
                v.normal =  normalize(mul( unity_WorldToObject , float4( fNor ,0 )).xyz);
                v.vertex = float4(mul( unity_WorldToObject , float4( fPos ,1 )).xyz,1);
                v.tangent = float4(normalize(mul( unity_WorldToObject , float4( fTan ,0 )).xyz),1);
              
                #endif

            }

            float _Metallic;
            float _Smoothness;

            float4 _StartingColor;
            float4 _EndingColor;
            float _AlphaCuttoff;
          
        
            void surf (Input IN, inout SurfaceOutputStandard o)
            {
                // Albedo comes from a texture tinted by color

                float4 c = tex2D(_MainTex , IN.uv_MainTex );
                
                o.Albedo = c.xyz;//*  lerp(_StartingColor, _EndingColor, IN.life);
                
                if( c.a < _AlphaCuttoff ){discard;}
                // Metallic and smoothness come from slider variables
                o.Metallic = _Metallic;
                o.Smoothness = _Smoothness;
                o.Normal = UnpackNormal (tex2D (_BumpMap, IN.uv_BumpMap));
                // o.Alpha = c.a;

            }
            ENDCG



         Cull Back
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, making it so we add shadows
        #pragma surface surf Standard   vertex:vert addshadow
        #pragma shader_feature WORLD_NORMAL
        // Use shader model 4.5 to make sure we have access to compute buffer
        #pragma target 4.5

            #include "UnityCG.cginc"
       
    
      

            // input info
            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float4 texcoord : TEXCOORD0;
                uint id : SV_VertexID;
            };


            // info passed to surface shader
            struct Input{
                float2 uv_BumpMap;
                float2 uv_MainTex;
                float2 uv_Band;
            };   

            sampler2D _MainTex;
            sampler2D _BumpMap;
 
            #if defined(SHADER_API_D3D11) || defined(SHADER_API_PSSL) || defined(SHADER_API_METAL) || defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN) || defined(SHADER_API_SWITCH) // D3D11, D3D12, XB1, PS4, iOS, macOS, tvOS, glcore, gles3, webgl2.0, Switch
                StructuredBuffer<float4x4> _PointBuffer;
            #endif

            #include "CurveInfo.cginc"


            void vert (inout appdata v,out Input o) 
            {
             

                int vid = v.id;
                
                UNITY_INITIALIZE_OUTPUT(Input,o);

                #if defined(SHADER_API_D3D11) || defined(SHADER_API_PSSL) || defined(SHADER_API_METAL) || defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN) || defined(SHADER_API_SWITCH) // D3D11, D3D12, XB1, PS4, iOS, macOS, tvOS, glcore, gles3, webgl2.0, Switch

                int quadID = vid /6;

                int rowID = quadID / _ResolutionX;
                int colID = quadID % _ResolutionX;
                int inQuadID = vid % 6;

                float val = float(rowID) / float(_Resolution);
                float valUp = float(rowID+1) / float(_Resolution);

                float col = float(colID) / float(_ResolutionX);
                float colUp = float(colID+1) / float(_ResolutionX);


                val *= _CurveLength;
                val += _CurveStart;

                
                valUp *= _CurveLength;
                valUp += _CurveStart;

                float3 pos; float3 dir; float3 tang;
                float3 posUp; float3 dirUp; float3 tangUp;

                GetCubicInformation( val , pos , dir , tang );
                GetCubicInformation( valUp , posUp , dirUp , tangUp );

                float3 p1 = pos   + (col   -.5)* tang * _CurveWidthMultiplier;
                float3 p2 = pos   + (colUp -.5)* tang * _CurveWidthMultiplier;
                float3 p3 = posUp + (col   -.5)* tangUp * _CurveWidthMultiplier;
                float3 p4 = posUp + (colUp -.5)* tangUp * _CurveWidthMultiplier;

                float3 fNor;
                float3 fTan;

                float3 nor = normalize(cross(dir,tang));
                float3 norUp = normalize(cross(dirUp,tangUp));

                float3 fPos;
                float2 fUV;

                if( inQuadID == 0 ){
                    fPos = p1;
                    fUV = float2( col,val);
                    fNor = nor;
                    fTan = tang;
                }else if( inQuadID == 1 ){
                    fPos = p2;
                    fUV = float2( colUp,val);
                    fNor = nor;
                    fTan = tang;
                }else if( inQuadID == 2 ){                         
                    fPos = p4;
                    fUV = float2( colUp,valUp);
                    fNor = norUp;
                    fTan = tangUp;
                }else if( inQuadID == 3 ){
                    fPos = p1;
                    fUV = float2( col,val);
                    fNor = nor;
                    fTan = tang;
                }else if( inQuadID == 4 ){
                    fPos = p4;
                    fUV = float2( colUp,valUp);
                    fNor = norUp;
                    fTan = tangUp;
                }else{
                    fPos = p3;
                    fUV = float2( col,valUp);
                    fNor = norUp;
                    fTan = tangUp;
                }

                v.texcoord = float4( fUV, 0, 0 );
                v.normal =  normalize(mul( unity_WorldToObject , float4( -fNor ,0 )).xyz);
                v.vertex = float4(mul( unity_WorldToObject , float4( fPos ,1 )).xyz,1);
                v.tangent = float4(normalize(mul( unity_WorldToObject , float4( fTan ,0 )).xyz),1);
              
                #endif

            }

            float _Metallic;
            float _Smoothness;

            float4 _StartingColor;
            float4 _EndingColor;
            float _AlphaCuttoff;
          
        
            void surf (Input IN, inout SurfaceOutputStandard o)
            {
                // Albedo comes from a texture tinted by color

                float4 c = tex2D(_MainTex , IN.uv_MainTex );
                
                o.Albedo = c.xyz;//*  lerp(_StartingColor, _EndingColor, IN.life);
                
                if( c.a < _AlphaCuttoff ){discard;}
                // Metallic and smoothness come from slider variables
                o.Metallic = _Metallic;
                o.Smoothness = _Smoothness;
                o.Normal = UnpackNormal (tex2D (_BumpMap, IN.uv_BumpMap));
                // o.Alpha = c.a;

            }
            ENDCG

    }
    
    FallBack "Diffuse" 
}