using UnityEngine;
using System.Collections;

public class CombineMeshes : MonoBehaviour
{
    void Start()
    {
        Quaternion oldRot = transform.rotation;
        Vector3 oldPos = transform.position;

        transform.rotation = Quaternion.identity;
        transform.position = Vector3.zero;

        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        

        Debug.Log(name + " is combining " + meshFilters.Length + "meshes.");

        Mesh finalMesh = new Mesh();

        CombineInstance[] combiners = new CombineInstance[meshFilters.Length];

        for (int a = 0; a < meshFilters.Length; a++)
        {
            if (meshFilters[a].transform == transform)
                continue;
            combiners[a].subMeshIndex = 0;
            combiners[a].mesh = meshFilters[a].sharedMesh;
            combiners[a].transform = meshFilters[a].transform.localToWorldMatrix;
        }

        finalMesh.CombineMeshes(combiners);

        GetComponent<MeshFilter>().sharedMesh = finalMesh;

        transform.rotation = oldRot;
        transform.position = oldPos;

        // hide source meshes
        for (int a = 0; a < transform.childCount; a++)
        {
            transform.GetChild(a).gameObject.SetActive(false);
        }
    }
}