using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Mathematics.math;
using Unity.Mathematics;



using MagicCurve;


[ExecuteInEditMode]

[RequireComponent(typeof(MeshFilter))]
public class PlaceInstancedMeshAlongCurve : MonoBehaviour
{

    public Curve curve;
    public int numberOfObjects;
    public float scaleMultiplier;
    public Vector3 offset;
    public Mesh mesh;

    MeshFilter filter;



    public void OnEnable(){
        filter = GetComponent<MeshFilter>();
        curve.BakeChanged.AddListener(BuildMesh);
    }

    public void OnDisable(){
        curve.BakeChanged.AddListener(BuildMesh);
    }
void BuildMesh(Curve c){

    

    CombineInstance[] combine = new CombineInstance[numberOfObjects];

    for(int i = 0; i < numberOfObjects; i++){

        float val = (float)i/numberOfObjects;

        float3 p; float3 f; float3 u; float3 r; float s;

        curve.GetDataFromValueAlongCurve(val,out p,out f,out u,out r,out s);

        combine[i].transform = Matrix4x4.TRS( p + f * offset.z + u * offset.y + r * offset.z, Quaternion.LookRotation( f,u) , Vector3.one * s * scaleMultiplier);
        combine[i].mesh = mesh;




    }
   
   


    // GO ahead and combine the meshes 
    // ( using the big index incase there are too many verts! )
   filter.sharedMesh = new Mesh();
   filter.sharedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
   filter.sharedMesh.CombineMeshes(combine);


}
}
