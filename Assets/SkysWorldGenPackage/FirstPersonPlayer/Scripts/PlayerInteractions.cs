using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractions : MonoBehaviour
{
    public Material outline;
    public MeshRenderer lookMesh; // current mesh player is looking at
    public LayerMask interactionLayer;

    public Transform orientation;
    public Transform objSpawnPos;

    public GameObject throwObject;
    public Vector2 throwVelocity = new Vector2(2,1);
    public InputAction shootAction;


    List<MeshRenderer> highlightedMeshes = new List<MeshRenderer>();

    private void OnEnable()
    {
        shootAction.Enable();
    }

    private void OnDisable()
    {
        shootAction.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(new Vector2(Screen.width / 2, Screen.height / 2));
        RaycastHit hitPoint;
        if (Physics.Raycast(ray, out hitPoint, 100.0f))
        {
            // if mesh in interaction layer, set to look mesh
            //if (lookMesh.gameObject.layer == interactionLayer)
                //lookMesh = hitPoint.collider.gameObject.GetComponent<MeshRenderer>();
        }
        
        if (shootAction.triggered)
        {
            ThrowObject();

            //if (lookMesh != null) { OutlineToggle(lookMesh); }
        }
    }

    public void ThrowObject()
    {
        GameObject obj = Instantiate(throwObject, objSpawnPos.position, Quaternion.identity);
        obj.GetComponent<Rigidbody>().velocity = orientation.forward * throwVelocity.x;
        obj.GetComponent<Rigidbody>().velocity += new Vector3(0, throwVelocity.y);

        Destroy(obj, 3);
    }

    public void OutlineToggle(MeshRenderer m)
    {

        List<Material> meshMaterials = new List<Material>(m.materials);
        
        // if mesh is highlighted already, remove
        if (highlightedMeshes.Contains(m))
        {
            meshMaterials[1] = null;
            highlightedMeshes.Remove(m);
        }

        // else add
        else
        {
            meshMaterials[1] = outline;
            highlightedMeshes.Add(m);
        }

        m.sharedMaterials = meshMaterials.ToArray();

        Debug.Log("changed material", m);
    }
}
