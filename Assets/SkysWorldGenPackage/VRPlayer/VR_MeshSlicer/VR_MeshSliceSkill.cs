using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Assets.Scripts;


public class VR_MeshSliceSkill : MonoBehaviour
{
    public GameObject laserSlicePointPrefab;
    public List<GameObject> laserSlicePoints;
    public bool laserMeshCreated;
    public Material laserMeshMaterial;
    public GameObject currSliceObject = null;

    [Space(20)]
    public GameObject defaultSliceObjPrefab;

    public List<Material> sliceMaterials = new List<Material>();
    public Material selectionMaterial;
    public List<GameObject> selectedObjects = new List<GameObject>();
    public List<Material> selectedObjectsOriginalMaterials = new List<Material>();

    public float forceAppliedToCut = 0.1f;

    private Mesh mesh;
    private GameObject meshSliceLaser;
    private List<Vector3> pointPos;


    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
        if (laserSlicePoints.Count == 4)
        {
            // create laser mesh when # of points are satisfied
            if (!laserMeshCreated)
            {
                CreateLaserMeshFromPoints(pointPos, laserMeshMaterial);
            }
        }
    }

    public GameObject CreateNewLaserPoint(Vector3 position)
    {
        if (laserSlicePoints.Count == 4) 
        { 
            
            DestroyObjectList(laserSlicePoints); 
            mesh.Clear();
            
            laserMeshCreated = false;
        }

        GameObject laserPoint = Instantiate(laserSlicePointPrefab, position, Quaternion.identity);

        laserSlicePoints.Add(laserPoint);

        // << SET POINT POS LIST >>
        pointPos = new List<Vector3>();
        foreach (GameObject obj in laserSlicePoints) { pointPos.Add(obj.transform.position); }

        return laserPoint;
    }

    #region MESH =======================================================
    public void CreateLaserMeshFromPoints(List<Vector3> laserPoints, Material material = null)
    {
        if (laserPoints.Count != 4)
        {
            Debug.LogError("Input list should contain exactly 4 points. Contains: " + laserPoints.Count);
            return;
        }

        Debug.Log("Create Mesh Slice Mesh");

        meshSliceLaser = new GameObject();
        meshSliceLaser.name = "MeshSliceLaser";


        mesh = new Mesh();

        // << SET MESH FILTER >>
        MeshFilter meshFilter = meshSliceLaser.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        // << SET MESH RENDERER >>
        MeshRenderer meshRenderer = meshSliceLaser.AddComponent<MeshRenderer>();
        meshRenderer.material = material;


        // << CREATE VERTICES >>
        Vector3[] vertices = new Vector3[4];
        vertices[0] = laserPoints[0];
        vertices[1] = laserPoints[1];
        vertices[2] = laserPoints[2];
        vertices[3] = laserPoints[3];

        mesh.vertices = vertices;


        // << CREATE TRIANGLES >>
        int[] triangles = new int[12];
        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;
        triangles[3] = 0;
        triangles[4] = 2;
        triangles[5] = 3;
        triangles[6] = 3;
        triangles[7] = 2;
        triangles[8] = 1;
        triangles[9] = 3;
        triangles[10] = 1;
        triangles[11] = 0;

        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        



        laserMeshCreated = true;
    }
    #endregion

    #region SLICE ======================================================
    public List<GameObject> Slice(GameObject obj = null, List<Vector3> points = null)
    {
        if (!obj) { obj = currSliceObject; }
        if (points == null) { points = pointPos; }

        // if obj or points still null
        if (obj == null || points == null) { Debug.LogWarning("Can't slice"); return null; }

        Debug.Log("Slice obj : " + obj.name, obj);

        //Create a triangle between the tip and base so that we can get the normal
        Vector3 side1 = points[2] - points[0];
        Vector3 side2 = points[2] - points[1];

        //Get the point perpendicular to the triangle above which is the normal
        //https://docs.unity3d.com/Manual/ComputingNormalPerpendicularVector.html
        Vector3 normal = Vector3.Cross(side1, side2).normalized;

        //Transform the normal so that it is aligned with the object we are slicing's transform.
        Vector3 transformedNormal = ((Vector3)(obj.transform.localToWorldMatrix.transpose * normal)).normalized;

        //Get the enter position relative to the object we're cutting's local transform
        Vector3 transformedStartingPoint = obj.transform.InverseTransformPoint(points[0]);

        Plane plane = new Plane();

        plane.Set3Points(points[0], points[1], points[2]);


        plane.SetNormalAndPosition(
                transformedNormal,
                transformedStartingPoint);

        var direction = Vector3.Dot(Vector3.up, transformedNormal);


        //Flip the plane so that we always know which side the positive mesh is on
        if (direction < 0)
        {
            plane = plane.flipped;
        }

        // << SAVE ORIGINAL VALUES >>
        Vector3 originalScale = obj.transform.localScale;
        Material originalMaterial = obj.GetComponent<MeshRenderer>().material;

        // create slices
        GameObject[] slices = Slicer.Slice(plane, obj);
        Destroy(obj);


        // apply given force to slices
        Rigidbody rigidbody = slices[1].GetComponent<Rigidbody>();
        Vector3 newNormal = transformedNormal + Vector3.up * forceAppliedToCut;
        //rigidbody.AddForce(newNormal, ForceMode.Impulse);

        // set slice materials
        if (sliceMaterials.Count == 2)
        {
            slices[0].GetComponent<MeshRenderer>().material = sliceMaterials[0];
            slices[1].GetComponent<MeshRenderer>().material = sliceMaterials[1];
        }

        List<GameObject> returnSlices = new List<GameObject>(slices);
        foreach (GameObject slice in returnSlices)
        {
            slice.transform.localScale = originalScale;
            NewSliceSetup(slice, true);
        }

        selectedObjects.Clear();

        return returnSlices;
    }

    #endregion

    #region COMBINE =====================================================
    public void Combine(List<GameObject> selectedObjects)
    {
        Debug.Log("Combine");

        if (!IsConnected(selectedObjects)) { Debug.LogWarning("Selected Mesh are not connected, cannot combine"); return; }

        // Create a new list to store the combined meshes
        List<CombineInstance> combine = new List<CombineInstance>();

        // Loop through each game object in the list
        foreach (GameObject obj in selectedObjects)
        {
            // Get the mesh filter component of the game object
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();

            // If the mesh filter is not null
            if (meshFilter != null)
            {
                // Create a new combine instance and add the mesh to the list
                CombineInstance instance = new CombineInstance();
                instance.mesh = meshFilter.sharedMesh;
                instance.transform = meshFilter.transform.localToWorldMatrix;
                combine.Add(instance);
            }

            Destroy(obj);
        }

        // Create a new mesh to store the combined meshes
        Mesh combinedMesh = new Mesh();

        // Combine all the meshes in the list
        combinedMesh.CombineMeshes(combine.ToArray());

        // Add a mesh filter and renderer to the game object
        GameObject combinedMeshObj = new GameObject();
        MeshFilter combinedMeshFilter = combinedMeshObj.AddComponent<MeshFilter>();
        combinedMeshFilter.mesh = combinedMesh;
        combinedMeshFilter.mesh.RecalculateBounds();
        combinedMeshFilter.mesh.RecalculateNormals();

        MeshRenderer combinedMeshRenderer = combinedMeshObj.AddComponent<MeshRenderer>();
        combinedMeshRenderer.material = sliceMaterials[0];

        MeshCollider combinedMeshCol = combinedMeshObj.AddComponent<MeshCollider>();
        combinedMeshCol.convex = true;

        Rigidbody combinedMeshRb = combinedMeshObj.AddComponent<Rigidbody>();
        combinedMeshRb.useGravity = false;

        Sliceable combinedMeshSliceable = combinedMeshObj.AddComponent<Sliceable>();
        combinedMeshSliceable.IsSolid = true;

        NewSliceSetup(combinedMeshObj);

    }

    public bool IsConnected(List<GameObject> gameObjects)
    {
        // Create a set to store the connected game objects
        HashSet<GameObject> connectedObjects = new HashSet<GameObject>();

        // Add the first game object to the set
        connectedObjects.Add(gameObjects[0]);

        // Loop through each game object in the list
        for (int i = 0; i < gameObjects.Count; i++)
        {
            // Get the mesh filter component of the current game object
            MeshFilter meshFilter = gameObjects[i].GetComponent<MeshFilter>();

            // If the mesh filter is not null
            if (meshFilter != null)
            {
                // Get the vertices of the mesh
                Vector3[] vertices = meshFilter.mesh.vertices;

                // Loop through each vertex in the mesh
                foreach (Vector3 vertex in vertices)
                {
                    // Convert the vertex from local to world space
                    Vector3 worldVertex = meshFilter.transform.TransformPoint(vertex);

                    // Loop through each game object in the list again
                    for (int j = 0; j < gameObjects.Count; j++)
                    {
                        // Skip the current game object
                        if (i == j)
                        {
                            continue;
                        }

                        // Get the mesh collider component of the other game object
                        MeshCollider collider = gameObjects[j].GetComponent<MeshCollider>();

                        // If the mesh collider is not null and contains the world vertex
                        if (collider != null && collider.bounds.Contains(worldVertex))
                        {
                            // Add the other game object to the set
                            connectedObjects.Add(gameObjects[j]);
                            break;
                        }
                    }
                }
            }
        }

        // Check if all the game objects are in the set
        return connectedObjects.Count == gameObjects.Count;
    }
    #endregion

    #region HELPER FUNCTIONS ===============================================
    public void DestroyObjectList(List<GameObject> objs)
    {
        foreach (GameObject obj in objs)
        {
            Destroy(obj);
        }

        objs.Clear();
    }

    public void NewSliceSetup(GameObject slice, bool freeze = false)
    {
        slice.tag = "sliceable";

        XRGrabInteractable interactable = slice.AddComponent<XRGrabInteractable>();
        interactable.trackPosition = false;
        interactable.trackRotation = false;

        if (freeze)
        {
            slice.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    public void SelectObj(GameObject obj)
    {
        currSliceObject = obj;

        // get renderer
        MeshRenderer renderer = obj.GetComponentInChildren<MeshRenderer>();
        if (renderer != null)
        {
            // remove from list
            if (selectedObjects.Contains(currSliceObject))
            {
                int objectIndex = selectedObjects.IndexOf(currSliceObject);
                renderer.material = selectedObjectsOriginalMaterials[objectIndex];

                selectedObjectsOriginalMaterials.RemoveAt(objectIndex);
                selectedObjects.Remove(currSliceObject);

                currSliceObject.GetComponentInParent<Rigidbody>().constraints = RigidbodyConstraints.None;

                currSliceObject.GetComponent<MeshCollider>().enabled = true;

            }
            // add to list
            else
            {
                Debug.Log("Selected " + currSliceObject, currSliceObject);

                selectedObjects.Add(currSliceObject);
                selectedObjectsOriginalMaterials.Add(renderer.material);

                renderer.material = selectionMaterial;

                currSliceObject.GetComponentInParent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;

                currSliceObject.GetComponent<MeshCollider>().enabled = false;
            }
        }
        else { Debug.LogWarning("Could not select, missing Mesh Renderer"); }
        
    }
    
    #endregion
}
