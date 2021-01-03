using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
 using static Unity.Mathematics.math;
using Unity.Mathematics;

[ExecuteAlways]
public class CubicProcMeshDirectionsMatter : MonoBehaviour
{
    public Transform[] points;


    public int resolution;

    public ComputeBuffer pointBuffer;

    public bool dynamic;

    public Matrix4x4[] pointMatrices;

    public Material material;

    public float curveWidth = 1;
    public float velocityImportance = 1;

    public float curveStart = .5f;
    public float curveLength = .1f;

    Bounds bounds;
    MaterialPropertyBlock mpb;
    

    // Start is called before the first frame update
    void OnEnable(){
        pointMatrices = new Matrix4x4[points.Length];
        pointBuffer = new ComputeBuffer( points.Length ,  16 * sizeof(float));

        mpb = new MaterialPropertyBlock();

        UpdatePointBuffer();

    

    }

    void OnDisable(){
        if( pointBuffer != null ){ pointBuffer.Dispose(); }
    }

    // Update is called once per frame
    void Update()
    {

        

        if( dynamic ){
            UpdatePointBuffer();
        }
           
        mpb.SetBuffer("_PointBuffer", pointBuffer);
        mpb.SetInt("_TotalCurvePoints", points.Length);
        mpb.SetFloat("_CurveWidth" , curveWidth );
        mpb.SetFloat("_VelocityImportance" , velocityImportance );
        mpb.SetFloat("_CurveStart" , curveStart );
        mpb.SetFloat("_CurveLength" , curveLength );
        mpb.SetInt("_Resolution", resolution);
        Graphics.DrawProcedural(material, bounds , MeshTopology.Triangles , (resolution) * 3 * 2, 1, null, mpb, ShadowCastingMode.On, true, LayerMask.NameToLayer("Default"));

    }

    public float3 GetPositionAlongPath(float v){

        float3 pos; float3 tang; float3 dir;

        GetCubicInformation( v, out pos, out tang, out dir );
        return pos;

    }



    void UpdatePointBuffer(){

        bounds = new Bounds();
        for( int i = 0; i < points.Length; i++ ){
            pointMatrices[i] = points[i].localToWorldMatrix;
            bounds.Encapsulate( points[i].position);
        }

        pointBuffer.SetData( pointMatrices );

    
    }



    
 void GetCubicInformation( float val , out float3 position , out float3 direction , out float3 tangent ){


  float3 p0 = 0;
  float3 v0 = 0;
  float3 p1 = 0;
  float3 v1 = 0;

  float3 p2 = float3( 0 , 0 , 0 );

  float vPP = (float)points.Length;

  float baseVal = val * (vPP-1);

  int baseUp   = (int)floor( baseVal );
  int baseDown = (int)ceil( baseVal );


// TOD MAKE UNIFORM
  float3 up = float3(0,1,0);



  if( baseUp == baseDown  || (baseVal % 1) == 0){

    float4x4 pointMatrix = points[(int)baseVal].localToWorldMatrix;

    position = mul( pointMatrix , float4(0,0,0,1)).xyz;
    direction = normalize(mul( pointMatrix , float4(0,0,1,0)).xyz);

    
    tangent = normalize(mul( pointMatrix , float4(1,0,0,0)).xyz);
    
  }else{
    float amount = baseVal - (float)baseUp;
    float4x4 pointMatrixUp = points[baseUp].localToWorldMatrix;
    float4x4 pointMatrixDown = points[baseDown].localToWorldMatrix;
   p0 = mul( pointMatrixUp , float4(0,0,0,1)).xyz;
   p1 = mul( pointMatrixDown , float4(0,0,0,1)).xyz;

   
   v0 = 20*mul( pointMatrixUp , float4(0,0,1,0)).xyz;
   v1 = 20*mul( pointMatrixDown , float4(0,0,1,0)).xyz;


   


    // Todo : make the /3 a uniform also
    float3 c0 = p0;
    float3 c1 = p0 + v0/(3/ velocityImportance);
    float3 c2 = p1 - v1/(3/ velocityImportance);
    float3 c3 = p1;

    float3 pos = cubicCurve( amount , c0 , c1 , c2 , c3 );

    float3 upPos = cubicCurve( amount  + .001f , c0 , c1 , c2 , c3 );

    position = pos;
    direction = normalize(upPos - pos);   

    
    tangent = lerp( normalize(mul( pointMatrixUp , float4(1,0,0,0)).xyz) , normalize(mul( pointMatrixDown , float4(1,0,0,0)).xyz) , amount);
  


  }


}


float3 cubicCurve( float t , float3  c0 , float3 c1 , float3 c2 , float3 c3 ){

  float s  = 1 - t;

  float3 v1 = c0 * ( s * s * s );
  float3 v2 = 3 * c1 * ( s * s ) * t;
  float3 v3 = 3 * c2 * s * ( t * t );
  float3 v4 = c3 * ( t * t * t );

  float3 value = v1 + v2 + v3 + v4;

  return value;

}




}
