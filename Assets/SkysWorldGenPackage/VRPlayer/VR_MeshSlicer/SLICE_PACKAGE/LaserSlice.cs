using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserSlice : MonoBehaviour
{
    public GameObject sliceObjectPrefab;
    public GameObject currSliceObject;

    public LayerMask sliceableLayer;
    public GameObject laserSlicePointPrefab;
    public List<GameObject> laserSlicePoints;
    private Mesh mesh;
    public List<Material> sliceMaterials = new List<Material>();
    public Material selectionMaterial;
    public List<GameObject> selectedObjects =  new List<GameObject>();

    public Vector3 mouseWorldPos;
    public float mouseZPos = 1;

    public float _forceAppliedToCut = 1;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // << GET MOUSE POSITION >>
        Vector3 mousePos = Input.mousePosition;
        mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, mouseZPos));

        // when left mouse is clicked, spawn object
        if (Input.GetMouseButtonDown(0))
        {
            CreateNewLaserPoint(mouseWorldPos);

            // Spawn pair laser point
            Vector3 spawnPos = mouseWorldPos + Camera.main.transform.forward * 20;
            CreateNewLaserPoint(spawnPos);
        }

        // << POINT POS >>
        List<Vector3> pointPos = new List<Vector3>();
        foreach (GameObject obj in laserSlicePoints) { pointPos.Add(obj.transform.position); }

        // create laser mesh when # of points are satisfied
        if (laserSlicePoints.Count == 4)
        {
            CreateLaserMeshFromPoints(pointPos);
        }

        // SLICE
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (currSliceObject)
            {
                Slice(currSliceObject, pointPos);
                selectedObjects.Clear();

            }

        }

        // COMBINE
        if (Input.GetKeyDown(KeyCode.Return))
        {
            Combine(selectedObjects);
        }

        // SELECT COLLIDER
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // check to see if clicking on object
            if (Physics.Raycast(ray, out hit, 100, sliceableLayer))
            {
                currSliceObject = hit.collider.gameObject;

                // get renderer
                MeshRenderer renderer = hit.collider.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    // remove from list
                    if (selectedObjects.Contains(currSliceObject))
                    {
                        selectedObjects.Remove(currSliceObject);
                        renderer.material = sliceMaterials[0];

                        currSliceObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;

                    }
                    // add to list
                    else
                    {
                        renderer.material = selectionMaterial;
                        selectedObjects.Add(currSliceObject);

                        currSliceObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;

                    }
                }
            }
        }

    }

    public GameObject CreateNewLaserPoint(Vector3 position)
    {
        if (laserSlicePoints.Count == 4) { DestroyObjectList(laserSlicePoints); mesh.Clear(); }

        GameObject laserPoint = Instantiate(laserSlicePointPrefab, position, Quaternion.identity);

        laserSlicePoints.Add(laserPoint);

        return laserPoint;
    }

    public void CreateLaserMeshFromPoints(List<Vector3> laserPoints)
    {
        if (laserPoints.Count != 4)
        {
            Debug.LogError("Input list should contain exactly 4 points. Contains: " + laserPoints.Count);
            return;
        }

        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = laserPoints[0];
        vertices[1] = laserPoints[1];
        vertices[2] = laserPoints[2];
        vertices[3] = laserPoints[3];

        mesh.vertices = vertices;

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
    }

    public List<GameObject> Slice(GameObject obj, List<Vector3> points)
    {
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
        
        // create slices
        GameObject[] slices = Slicer.Slice(plane, obj);
        Destroy(obj);

        // apply given force to slices
        Rigidbody rigidbody = slices[1].GetComponent<Rigidbody>();
        Vector3 newNormal = transformedNormal + Vector3.up * _forceAppliedToCut;
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
            SetObjToSliceableLayer(slice);

        }

        return returnSlices;
    }

    public void Combine(List<GameObject> selectedObjects)
    {
        Debug.Log("Combine");

        if (!IsConnected(selectedObjects)) { Debug.LogWarning("Selected Mesh are not connected, cannot combine"); return; }

        // Create a new list to store the combined meshes
        List<CombineInstance> combine = new List<CombineInstance>();

        // Loop through each game object in the list
        foreach (GameObject go in selectedObjects)
        {
            // Get the mesh filter component of the game object
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();

            // If the mesh filter is not null
            if (meshFilter != null)
            {
                // Create a new combine instance and add the mesh to the list
                CombineInstance instance = new CombineInstance();
                instance.mesh = meshFilter.sharedMesh;
                instance.transform = meshFilter.transform.localToWorldMatrix;
                combine.Add(instance);
            }
        }

        // Create a new mesh to store the combined meshes
        Mesh combinedMesh = new Mesh();

        // Combine all the meshes in the list
        combinedMesh.CombineMeshes(combine.ToArray());

        // Add a mesh filter and renderer to the game object
        GameObject combinedMeshObj = new GameObject();
        MeshFilter combinedMeshFilter = combinedMeshObj.AddComponent<MeshFilter>();
        combinedMeshFilter.mesh = combinedMesh;

        MeshRenderer combinedMeshRenderer = combinedMeshObj.AddComponent<MeshRenderer>();
        combinedMeshRenderer.material = sliceMaterials[0];

        MeshCollider combinedMeshCol = combinedMeshObj.AddComponent<MeshCollider>();
        combinedMeshCol.convex = true;

        Rigidbody combinedMeshRb = combinedMeshObj.AddComponent<Rigidbody>();
        combinedMeshRb.useGravity = false;

        Sliceable combinedMeshSliceable = combinedMeshObj.AddComponent<Sliceable>();
        combinedMeshSliceable.IsSolid = true;

        SetObjToSliceableLayer(combinedMeshObj);

        DestroyObjectList(selectedObjects);
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

    public void DestroyObjectList(List<GameObject> objs)
    {
        foreach(GameObject obj in objs)
        {
            Destroy(obj);
        }

        objs.Clear();
    }

    public void SetObjToSliceableLayer(GameObject slice)
    {
        slice.tag = "sliceable";
        slice.layer = LayerMask.NameToLayer("Sliceable");
    }
}
