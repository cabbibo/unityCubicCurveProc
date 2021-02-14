using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
 using static Unity.Mathematics.math;
using Unity.Mathematics;
using UnityEngine.Events;


namespace MagicCurve{


  
[ExecuteInEditMode]
[RequireComponent(typeof(AudioSource))]
public class Curve : MonoBehaviour
{

    public bool closed;
    public bool showCurve = true;
    public bool showAddPositions=true;
    public bool showEvenBasis;
    public bool showEvenMovementBasis;
    public bool showCurveMovementBasis;
    public bool showPointArrows;

    public float curveVizSpeed = .1f;

    public bool showAllControls;
    public bool showRotateControls;
    public bool showScaleControls;
    public bool showMoveControls = true;

    public bool haveFun;

    public Font labelFont;

    [Range(.1f,2)]
    public float interfaceScale = 1;

    
    [Range(60,1000)]
    public int curveVizLength = 200;


  
    [Range(.1f,100)]
    public float stepLength = .3f;
    private float oStepLength;
 
    [Range(.001f,1)]
    public float stepResolution = .5f;
    private float oStepResolution;

    public int selectedPoint;

    public ComputeBuffer pointBuffer;
    public ComputeBuffer powerBuffer;

    public Matrix4x4[] pointMatrices;

    public List<Vector3> positions;
    public List<Quaternion> rotations;
    public  List<Vector3> powers;


    public Bounds bounds;
    public float totalCurveLength;

    [HideInInspector] public float[] bakedDists;
    [HideInInspector] public Vector3[] bakedPoints;
    [HideInInspector] public Vector3[] bakedTangents;
    [HideInInspector] public Vector3[] bakedDirections;
    [HideInInspector] public Vector3[] bakedNormals;
    [HideInInspector] public float[] bakedWidths;
    [HideInInspector] public Matrix4x4[] bakedMatrices;

     public int bakedDistCount;


     public float audioVolume;

     public AudioClip moveClip;
     public AudioClip scaleClip;
     public AudioClip rotateClip;

     public AudioClip addNodeClip;
     public AudioClip deleteNodeClip;
     public AudioClip errorClip;

     private AudioSource audioSource;


         
    private float3 p; private float3 d; private float3 t; private float w; 
    private float3 p2; private float3 d2; private float3 t2; private float w2; 

    // Start is called before the first frame update
    void OnEnable(){

      oStepLength = stepLength;
      oStepResolution = stepResolution;
      audioSource = GetComponent<AudioSource>();

      ResetValues();

    }

    void ResetValues(){

      if(positions == null){
      positions = new List<Vector3>();
      rotations = new List<Quaternion>();
      powers = new List<Vector3>();

      positions.Add(transform.position);
      rotations.Add(transform.rotation);
      powers.Add(transform.localScale);

      
      positions.Add(transform.position + transform.forward);
      rotations.Add(transform.rotation);
      powers.Add(transform.localScale);

      if(pointMatrices == null){
        pointMatrices = new Matrix4x4[2];

      }

      FullBake();
      //FullBake();

      }


    }


    public void FullBake(){
      UpdateMatrices();
      ArcLengthParameterization(stepLength,stepResolution);
    }
    public void DeletePoint(){

      if( positions.Count > 0 ){

      positions.Remove(positions[selectedPoint]);
      rotations.Remove(rotations[selectedPoint]);
      powers.Remove(powers[selectedPoint]);

      if( selectedPoint == positions.Count ){
        selectedPoint -= 1;
      }

     
        playClip( deleteNodeClip );
      }else{
        playClip( errorClip );
      }

    }
    
    public void AddPoint(int distID){

      float v = bakedDists[distID];
     

      for( int i = 0; i < positions.Count; i++ ){

        // see if this is where we should be creating the positions
        if( v > (float)i/((float)positions.Count-1)  && v <= ((float)i+1)/((float)positions.Count-1) ){


          GetCubicInformation( v , out p , out d, out t , out w);

    
          positions.Insert(i+1, p);
          rotations.Insert(i+1,Quaternion.LookRotation( d , -cross(d,t) ));
          powers.Insert(i+1,new Vector3(1,1,w));

          selectedPoint = i+1;

          //TODO calculate powers
          //powers[i].position

          Vector3 p1  = positions[i];
          Vector3 p2 = positions[i] + ( rotations[i] * float3(0,0,1) * powers[i].x );
          Vector3 p3 = positions[i+2] - ( rotations[i+2] * float3(0,0,1) * powers[i+2].y );
          Vector3 p4  = positions[i+2];

          Vector3 b1 = (p1 + p2)/2;
          Vector3 b2 = (p2+p3)/2;
          Vector3 b3 = (p3+p4)/2;

          Vector3 t1 = (b1 + b2)/2;
          Vector3 t2 = (b2 + b3)/2;

          float pow1 = (p1-b1).magnitude;
          float pow2 = (t1-positions[i+1]).magnitude;
          float pow3 = (t2-positions[i+1]).magnitude;
          float pow4 = (p4-b3).magnitude;

          ChangePowerX( i , pow1 );
          ChangePowerY(i+1, pow2 );
          ChangePowerX(i+1, pow3 );
          ChangePowerY(i+2, pow4 );
        
          break;
        
        }

      }





        playClip( addNodeClip );

    }



    public void AddPointAtEnd( Ray ray ){

      float dist = length(positions[selectedPoint] - ray.origin);
      float3 newPos = ray.origin + ray.direction * dist;
      Quaternion q = rotations[ selectedPoint ];
      float3 power = powers[ selectedPoint ];


        positions.Insert(selectedPoint+1, newPos);
        rotations.Insert(selectedPoint+1,q);
        powers.Insert(selectedPoint+1,power);

        selectedPoint = selectedPoint+1;

        playClip( addNodeClip );



    }



    public void playClip(AudioClip clip ){

      audioSource.clip = clip;
      audioSource.volume = audioVolume;
      audioSource.Play();

    }

    public void UpdateMatrices(){

      
      CurveChanged.Invoke(this);
      if( pointMatrices.Length != positions.Count ){
        pointMatrices = new Matrix4x4[positions.Count];
      }

      for( int i = 0; i < pointMatrices.Length; i++ ){
        pointMatrices[i].SetTRS( positions[i] , rotations[i] , Vector3.one);
      }
    }

    // Update is called once per frame
    void Update()
    {

      if( oStepLength != stepLength || oStepResolution != stepResolution ){
        FullBake();
      }

      oStepLength = stepLength;
      oStepResolution = stepResolution;

    }




    public float3 GetPositionAlongPath(float v){

        float3 pos; float3 tang; float3 dir; float width;

        GetCubicInformation( v, out pos, out tang, out dir , out  width);
        return pos;

    }


  public void ChangePowerX( int i , float v ){
    powers[i] = new Vector3( v , powers[i].y , powers[i].z);
  }
    public void ChangePowerY( int i , float v ){
    powers[i] = new Vector3( powers[i].x , v , powers[i].z);
  }
  public void ChangePowerZ( int i , float v ){
    powers[i] = new Vector3( powers[i].x , powers[i].y , v);
  }

  public void GetCubicInformation( float val , out float3 position , out float3 direction , out float3 tangent ,out float width){

        
            float3 p0 = 0;
            float3 v0 = 0;
            float3 p1 = 0;
            float3 v1 = 0;

            float3 p2 = float3( 0.0f , 0.0f , 0.0f );

            float vPP = positions.Count;

            float c = closed ?  0:1;
            float baseVal= val * (vPP-c);

            int baseUp   = (int)(floor( baseVal)) % positions.Count;
            int baseDown = (int)(ceil( baseVal))  % positions.Count;

            baseVal %= positions.Count;

            // TOD MAKE UNIFORM
            float3 up = float3(0,1,0);



            if( baseUp == baseDown  || (baseVal% 1) == 0){

                float4x4 pointMatrix = pointMatrices[(int)baseVal];

                float3 pointPower = powers[(int)baseVal];
                position = mul( pointMatrix , float4(0,0,0,1)).xyz;
                direction = normalize(mul( pointMatrix , float4(0,0,1,0)).xyz);

                
                tangent = normalize(mul( pointMatrix , float4(0,1,0,0)).xyz);
                tangent = normalize( cross( direction, tangent ));
                
                width = pointPower.z;//lerp( pointPower.z , pointDownPower.z , amount );
                
            }else{
                float amount = baseVal- (float)baseUp;

                float4x4 pointMatrixUp = pointMatrices[baseUp];
                float4x4 pointMatrixDown = pointMatrices[baseDown];

                float3 pointUpPower = powers[baseUp];
                float3 pointDownPower = powers[baseDown];


                    p0 = mul( pointMatrixUp , float4(0,0,0,1)).xyz;
                    p1 = mul( pointMatrixDown , float4(0,0,0,1)).xyz;


                    v0 = pointUpPower.x*normalize(mul( pointMatrixUp , float4(0,0,1,0)).xyz);
                    v1 = pointDownPower.y*normalize(mul( pointMatrixDown , float4(0,0,1,0)).xyz);


            


                // Todo : make the /3 a uniform also
                float3 c0 = p0;
                float3 c1 = p0 + v0;
                float3 c2 = p1 - v1;
                float3 c3 = p1;
                float smooth = amount * amount * (3 - 2 * amount);

                float3 pos = cubicCurve( amount , c0 , c1 , c2 , c3 );

                float3 upPos = cubicCurve( amount  + .001f , c0 , c1 , c2 , c3 );

                position = pos;
                direction = normalize((upPos - pos) * 1000);   

                
                tangent = lerp( normalize(mul( pointMatrixUp , float4(0,1,0,0)).xyz) , normalize(mul( pointMatrixDown , float4(0,1,0,0)).xyz) , smooth);

                tangent = normalize( cross( direction, tangent ));

                width = lerp( pointUpPower.z , pointDownPower.z , smooth );


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






  public void ArcLengthParameterization(float stepLength, float delta){

    List<float> segmentLocations  = new List<float>();
    List<float3> evenPoints       = new List<float3>();
    List<float3> evenDirections   = new List<float3>();
    List<float3> evenTangents     = new List<float3>();
    List<float>  evenWidths       = new List<float>();


  
    GetCubicInformation(0 , out p , out d , out t , out w );
        
    segmentLocations.Add(0);
    evenPoints.Add(p);
    evenDirections.Add(d);
    evenTangents.Add(t);
    evenWidths.Add(w);

    float3 prevPoint = p;

    totalCurveLength = 0;
    
    //float3 prevPoint = pointsAlongCurve[0];
    //float3 currPoint = pointsAlongCurve[0];
    float distSinceLastPoint = 0;

    int currentPoint = 0;
    float oDist = 0;

      float val = 0;
      while( val+delta <= 1 ){


        // step forward on our curve a lil baby bit
        val += delta;

        // Get our new Position
        GetCubicInformation( val , out p , out d , out t , out w );


        float dist = length(p - prevPoint);

        //
        distSinceLastPoint += dist;

        

        // if the lastPosition we've gone is too var, 
        // backtrack to correct location!
        if( distSinceLastPoint >= stepLength ){
          
          // Getting the FULL step
          float v3 = distSinceLastPoint - oDist;

          // Getting how long it would be to our correct length
          float v2 = stepLength - oDist;

          // Getting that ratio
          float amount  = v2/v3;

          // stepping back
          float lastPoint = val-delta;

          float newVal = lastPoint + delta * amount;

          // and then going forward by our new 'delta' amount
          GetCubicInformation( newVal, out p2 , out d2 , out t2 , out w2 );

          segmentLocations.Add( newVal );
          evenPoints.Add(p2);
          evenDirections.Add(d2);
          evenTangents.Add(t2);
          evenWidths.Add(w2);

          totalCurveLength += stepLength;
          

          val = newVal;
          
          // lets pray this is VERY close to 0....
          //print( length( p2 - evenPoints[currentPoint]));

          currentPoint ++;

          // set old info for next frame
          prevPoint = p2;
          oDist = 0;
          distSinceLastPoint = 0;
        

        }else{
          // set old info for next frame
          prevPoint = p;
          oDist = distSinceLastPoint;
        }


      }


      bakedDists      = segmentLocations.ToArray();
      bakedPoints     = new Vector3[evenPoints.Count];
      bakedDirections = new Vector3[evenPoints.Count];
      bakedTangents   = new Vector3[evenPoints.Count];
      bakedNormals    = new Vector3[evenPoints.Count];
      bakedWidths     = new float[evenPoints.Count];
      bakedMatrices   = new Matrix4x4[evenPoints.Count];
      int id = 0;
      
      foreach( float3 v  in evenPoints ){
        
        bakedPoints[id] = evenPoints[id];
        bakedDirections[id] = evenDirections[id];
        bakedTangents[id] = evenTangents[id];
        bakedWidths[id] = evenWidths[id];
        bakedNormals[id] = -cross( evenDirections[id], evenTangents[id]);
        
        bakedMatrices[id].SetTRS(
          bakedPoints[id] , 
          Quaternion.LookRotation( bakedDirections[id] , bakedNormals[id]) , 
          Vector3.one * bakedWidths[id] 
        );
        
        id ++;
      
      } 

      bakedDistCount = bakedDists.Length;


      
      BakeChanged.Invoke(this);


    }


    /*
      API
    */


    // normalized dist along
    public float getEvenDistAlong( float v ){
      if( v >= 1){ v = 1; }
      if( v <= 0){ v = 0;}
      float segment = v * (float)(bakedPoints.Length-1);
      float min = Mathf.Floor(segment);
      float max = Mathf.Max(segment);

      
      //print( segment - min );

       if( segment %1 == 0 || min == max){
        return bakedDists[(int)segment];
      }else{
        return lerp( bakedDists[(int)min] ,  bakedDists[(int)max] , (segment-min));
      }

    }


    public Vector3 GetPositionFromValueAlongCurve( float v ){
        GetCubicInformation( getEvenDistAlong(v) , out p , out d , out t , out w );
        return p;
    }

    public Vector3 GetPositionFromLengthAlongCurve( float v ){
        GetCubicInformation( getEvenDistAlong(v/totalCurveLength) , out p , out d , out t , out w );
        return p;
    }



    public Vector3 GetRightFromValueAlongCurve( float v ){
        GetCubicInformation( getEvenDistAlong(v) , out p , out d , out t , out w );
        return t;
    }

    public Vector3 GetRightFromLengthAlongCurve( float v ){
        GetCubicInformation( getEvenDistAlong(v/totalCurveLength) , out p , out d , out t , out w );
        return t;
    }


  public Vector3 GetForwardFromValueAlongCurve( float v ){
        GetCubicInformation( getEvenDistAlong(v) , out p , out d , out t , out w );
        return d;
    }

    public Vector3 GetForwardFromLengthAlongCurve( float v ){
        GetCubicInformation( getEvenDistAlong(v/totalCurveLength) , out p , out d , out t , out w );
        return d;
    }


    
    public Vector3 GetUpFromValueAlongCurve( float v ){
        GetCubicInformation( getEvenDistAlong(v) , out p , out d , out t , out w );
        return -cross( d,t);
    }

    public Vector3 GetUpFromLengthAlongCurve( float v ){
        GetCubicInformation( getEvenDistAlong(v/totalCurveLength) , out p , out d , out t , out w );
        return -cross( d,t);
    }



    public Quaternion GetRotationFromValueAlongCurve( float v ){
        GetCubicInformation( getEvenDistAlong(v) , out p , out d , out t , out w );
        return Quaternion.LookRotation( d , -cross( d,t));
    }

    public Quaternion GetRotationFromLengthAlongCurve( float v ){
        GetCubicInformation( getEvenDistAlong(v/totalCurveLength) , out p , out d , out t , out w );
        return Quaternion.LookRotation( d , -cross( d,t));
    }

    public float GetWidthFromValueAlongCurve( float v ){
        GetCubicInformation( getEvenDistAlong(v) , out p , out d , out t , out w );
        return w;
    }

    public float GetWidthFromLengthAlongCurve( float v ){
        GetCubicInformation( getEvenDistAlong(v/totalCurveLength) , out p , out d , out t , out w );
        return w;
    }

    public void SetTransformFromValueAlongCurve( float v , Transform transform){
        GetCubicInformation( getEvenDistAlong(v) , out p , out d , out t , out w );
        transform.position = p;
        transform.rotation = Quaternion.LookRotation( d , -cross( d,t));
    }


    public void SetTransformFromLengthAlongCurve( float v , Transform transform){
        GetCubicInformation( getEvenDistAlong(v/totalCurveLength) , out p , out d , out t , out w );
        transform.position = p;
        transform.rotation = Quaternion.LookRotation( d , -cross( d,t));
    }

    public Vector3 GetOffsetPositionFromValueAlongCurve( float v , float x , float y ){
       GetCubicInformation( getEvenDistAlong(v) , out p , out d , out t , out w );
       return p + t * x -cross( d,t)*y;
    }


    public Vector3 GetOffsetPositionFromLengthAlongCurve( float v , float x , float y ){
       GetCubicInformation( getEvenDistAlong(v/totalCurveLength) , out p , out d , out t , out w );
       return p + t * x -cross( d,t)*y;
    }

    public float GetCurveValueFromLengthAlongCurve( float v ){
      return  getEvenDistAlong(v/totalCurveLength);
    }

    public float GetCurveValueFromValueAlongCurve( float v ){
      return  getEvenDistAlong(v);
    }

    public float GetCurveLengthFromLengthAlongCurve( float v ){
      return  getEvenDistAlong(v/totalCurveLength)*totalCurveLength;
    }

    public float GetCurveLengthFromValueAlongCurve( float v ){
      return  getEvenDistAlong(v)*totalCurveLength;
    }


    public void GetDataFromValueAlongCurve( float v , out float3 pos , out float3 fwd , out float3 up , out float3 rit , out float scale){
      GetCubicInformation( getEvenDistAlong(v) , out pos , out fwd , out rit , out scale );
      up = -cross( d,t);
    }


    public void GetDataFromLengthAlongCurve( float v , out float3 pos , out float3 fwd , out float3 up , out float3 rit , out float scale){
//      print(v/totalCurveLength);
      GetCubicInformation( getEvenDistAlong(v/totalCurveLength) , out pos , out fwd , out rit , out scale );
      up = -cross( d,t);
    }


    public ComputeBuffer GetEvenPointTransformBuffer(){


      // THANK U PASTORAL MAC
      ComputeBuffer buffer= new ComputeBuffer(bakedMatrices.Length,sizeof(float)*16);
      buffer.SetData(bakedMatrices);

      return buffer;

    }





public CurveEvent BakeChanged = new CurveEvent();
public CurveEvent CurveChanged = new CurveEvent();






}


/*
EVENTS
*/
[System.Serializable]
public class CurveEvent : UnityEvent<Curve>
{
}


}




