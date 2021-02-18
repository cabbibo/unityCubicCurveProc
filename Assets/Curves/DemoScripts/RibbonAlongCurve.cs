using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MagicCurve;
using static Unity.Mathematics.math;
using Unity.Mathematics;



[ExecuteInEditMode]

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(Curve))]
public class RibbonAlongCurve : MonoBehaviour
{

    public bool twoSided;
    public Curve curve;
    public int lengthSegments = 50;
    public int widthSegments = 8;
    public float width = 1;


    Vector3[] positions;
    Vector3[] normals;
    Vector4[] tangents;
    Vector2[] uvs;
    int[] triangles;

    public int totalVertCount;
    public int totalTriCount;

    MeshFilter filter;

    public void OnEnable(){
        filter = GetComponent<MeshFilter>();
        curve = GetComponent<Curve>();
        curve.BakeChanged.AddListener(BuildMesh);

    }

    public void OnDisable(){
        curve.BakeChanged.AddListener(BuildMesh);
    }



    void BuildMesh(Curve c){
        
        totalVertCount = lengthSegments * widthSegments;
        totalTriCount = (lengthSegments-1) * (widthSegments-1) * 3 * 2;

        if( twoSided ){
            totalTriCount *= 2;
            totalVertCount *= 2;
        }

        positions = new Vector3[totalVertCount];
        normals = new Vector3[totalVertCount];
        tangents = new Vector4[totalVertCount];
        uvs = new Vector2[totalVertCount];
        triangles = new int[totalTriCount];

        // Building the triangles first
        int index  = 0;
        for( int i = 0; i < lengthSegments-1; i++){
            for( int j = 0; j < widthSegments-1; j++ ){
                

                // Getting indicies to build a tube
                int baseID = widthSegments * i + j;
                int id1 = baseID;
                int id2 = baseID + 1;
                int id3 = baseID + widthSegments;
                int id4 = baseID + widthSegments + 1;

                triangles[index++] = id1;
                triangles[index++] = id2;
                triangles[index++] = id4;
                triangles[index++] = id1;
                triangles[index++] = id4;
                triangles[index++] = id3;

                if( twoSided ){
                    triangles[index++] = id1 + (totalVertCount/2);
                    triangles[index++] = id4 + (totalVertCount/2);
                    triangles[index++] = id2 + (totalVertCount/2);
                    triangles[index++] = id1 + (totalVertCount/2);
                    triangles[index++] = id3 + (totalVertCount/2);
                    triangles[index++] = id4 + (totalVertCount/2);
                }

            }
        }

        // reset index of array
        index  = 0;
        for( int i = 0; i < lengthSegments; i++){
            
            float lengthAlongTube = (float)i/(lengthSegments-1);
            float3 centerPos = curve.GetPositionFromValueAlongCurve( lengthAlongTube );
            float3 forward = curve.GetForwardFromValueAlongCurve(lengthAlongTube);
            for( int j = 0; j < widthSegments; j++ ){
                float widthness = (float)j / ((float)widthSegments-1);
               
                float w = curve.GetWidthFromValueAlongCurve(lengthAlongTube);
                float3 fPos = curve.GetOffsetPositionFromValueAlongCurve( lengthAlongTube , ((float)widthness-.5f)*w*width, 0 );
                float3 normal = curve.GetUpFromLengthAlongCurve(lengthAlongTube);
                float4 tangent = float4(cross(normal,forward),1);
                float2 uv = float2( lengthAlongTube, widthness);

                positions[index] = transform.InverseTransformPoint(fPos);
                tangents[index] = float4(transform.InverseTransformDirection(tangent.xyz),1);
                normals[index] = transform.InverseTransformDirection(normal);
                uvs[ index] = uv;

                if( twoSided ){
                    positions[index + totalVertCount/2] = transform.InverseTransformPoint(fPos);
                    tangents[index + totalVertCount/2] = float4(transform.InverseTransformDirection(tangent.xyz),1);
                    normals[index + totalVertCount/2] = -transform.InverseTransformDirection(normal);
                    uvs[ index + totalVertCount/2] = uv;
                }

                index++;

            }
        }


        Mesh m = new Mesh();

        
        m.Clear();

        m.vertices = positions;
        m.tangents = tangents;
        m.normals = normals;
        m.uv = uvs;
        m.triangles = triangles;

        filter.mesh = m;





    }

}