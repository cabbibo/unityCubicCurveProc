

            int _TotalCurvePoints;
            int _Resolution;
            int _ResolutionX;
            float _CurveWidthMultiplier;
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

            void GetCubicInformation( float val , out float3 position , out float3 direction , out float3 tangent ,out float width){

            #if defined(SHADER_API_D3D11) || defined(SHADER_API_PSSL) || defined(SHADER_API_METAL) || defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN) || defined(SHADER_API_SWITCH) // D3D11, D3D12, XB1, PS4, iOS, macOS, tvOS, glcore, gles3, webgl2.0, Switch

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

                float3 pointPower = _PowerBuffer[base];
                position = mul( pointMatrix , float4(0,0,0,1)).xyz;
                direction = normalize(mul( pointMatrix , float4(0,0,1,0)).xyz);

                
                tangent = normalize(mul( pointMatrix , float4(0,1,0,0)).xyz);
                tangent = normalize( cross( direction, tangent ));
                
                width = pointPower.z;//lerp( pointPower.z , pointDownPower.z , amount );
                
            }else{
                float amount = base - float(baseUp);
                float4x4 pointMatrixUp = _PointBuffer[baseUp];
                float4x4 pointMatrixDown = _PointBuffer[baseDown];

                float3 pointUpPower = _PowerBuffer[baseUp];
                float3 pointDownPower = _PowerBuffer[baseDown];


                    p0 = mul( pointMatrixUp , float4(0,0,0,1)).xyz;
                    p1 = mul( pointMatrixDown , float4(0,0,0,1)).xyz;


                    v0 = pointUpPower.x*normalize(mul( pointMatrixUp , float4(0,0,1,0)).xyz);
                    v1 = pointDownPower.y*normalize(mul( pointMatrixDown , float4(0,0,1,0)).xyz);


            


                // Todo : make the /3 a uniform also
                float3 c0 = p0;
                float3 c1 = p0 + v0;
                float3 c2 = p1 - v1;
                float3 c3 = p1;

                float3 pos = cubicCurve( amount , c0 , c1 , c2 , c3 );

                float3 upPos = cubicCurve( amount  + .001 , c0 , c1 , c2 , c3 );

                position = pos;
                direction = normalize((upPos - pos) * 1000);   


                float3 smooth = amount * amount * (3 - 2 * amount);
                
                tangent = lerp( normalize(mul( pointMatrixUp , float4(0,1,0,0)).xyz) , normalize(mul( pointMatrixDown , float4(0,1,0,0)).xyz) , smooth);

                tangent = normalize( cross( direction, tangent ));



                width = lerp( pointUpPower.z , pointDownPower.z , smooth );


            }


#endif

            }
