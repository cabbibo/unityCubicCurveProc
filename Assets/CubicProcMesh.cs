using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


[ExecuteAlways]
public class CubicProcMesh : MonoBehaviour
{
    public Transform[] points;


    public int resolution;

    public ComputeBuffer pointBuffer;

    public bool dynamic;

    public Vector3[] pointPositions;

    public Material material;

    public float curveWidth = 1;
    public float velocityImportance = 1;

    Bounds bounds;
    MaterialPropertyBlock mpb;
    

    // Start is called before the first frame update
    void OnEnable(){
        pointPositions = new Vector3[points.Length];
        pointBuffer = new ComputeBuffer( points.Length ,  3 * sizeof(float));

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
        mpb.SetInt("_Resolution", resolution);
        Graphics.DrawProcedural(material, bounds , MeshTopology.Triangles , (resolution) * 3 * 2, 1, null, mpb, ShadowCastingMode.On, true, LayerMask.NameToLayer("Default"));

    }



    void UpdatePointBuffer(){

        bounds = new Bounds();
        for( int i = 0; i < points.Length; i++ ){
            pointPositions[i] = points[i].position;
            bounds.Encapsulate( points[i].position);
        }

        pointBuffer.SetData( pointPositions );

    
    }


}
