using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SliceBlade : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The empty game object located at the tip of the blade")]
    private GameObject _tip = null;

    [SerializeField]
    [Tooltip("The empty game object located at the base of the blade")]
    private GameObject _base = null;

    [SerializeField]
    [Tooltip("The amount of force applied to each side of a slice")]
    private float _forceAppliedToCut = 3f;

    public bool canSlice = true;
    public bool earlyExit;
    public Collider sliceCollider;

    public List<Material> sliceMaterials = new List<Material>();

    private Mesh _mesh;
    private Vector3 _previousTipPosition;
    private Vector3 _previousBasePosition;
    private Vector3 _triggerEnterTipPosition;
    private Vector3 _triggerEnterBasePosition;
    private Vector3 _triggerExitTipPosition;

    void LateUpdate()
    {

        if (Input.GetKeyDown(KeyCode.E)) { earlyExit = true; } // toggle early exit

        if (Input.GetKeyDown(KeyCode.W)) { canSlice = true; } // toggle can slice


        if (earlyExit && sliceCollider) { SliceExit(sliceCollider); earlyExit = false; canSlice = false; }

        //Track the previous base and tip positions for the next frame
        _previousTipPosition = _tip.transform.position;
        _previousBasePosition = _base.transform.position;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (canSlice)
        {
            SliceEnter(other);
        }
    }

    public void OnTriggerExit(Collider other)
    {
        SliceExit(other);
    }

    public void SliceEnter(Collider other)
    {
        sliceCollider = other;

        _triggerEnterTipPosition = _tip.transform.position;
        _triggerEnterBasePosition = _base.transform.position;
    }

    public void SliceExit(Collider other)
    {
        _triggerExitTipPosition = _tip.transform.position;

        //Create a triangle between the tip and base so that we can get the normal
        Vector3 side1 = _triggerExitTipPosition - _triggerEnterTipPosition;
        Vector3 side2 = _triggerExitTipPosition - _triggerEnterBasePosition;

        //Get the point perpendicular to the triangle above which is the normal
        //https://docs.unity3d.com/Manual/ComputingNormalPerpendicularVector.html
        Vector3 normal = Vector3.Cross(side1, side2).normalized;

        //Transform the normal so that it is aligned with the object we are slicing's transform.
        Vector3 transformedNormal = ((Vector3)(other.gameObject.transform.localToWorldMatrix.transpose * normal)).normalized;

        //Get the enter position relative to the object we're cutting's local transform
        Vector3 transformedStartingPoint = other.gameObject.transform.InverseTransformPoint(_triggerEnterTipPosition);

        Plane plane = new Plane();

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
        GameObject[] slices = Slicer.Slice(plane, other.gameObject);
        Destroy(other.gameObject);


        // apply given force to slices
        Rigidbody rigidbody = slices[1].GetComponent<Rigidbody>();
        Vector3 newNormal = transformedNormal + Vector3.up * _forceAppliedToCut;
        rigidbody.AddForce(newNormal, ForceMode.Impulse);


        // set slice materials
        if (sliceMaterials.Count == 2)
        {
            slices[0].GetComponent<MeshRenderer>().material = sliceMaterials[0];
            slices[1].GetComponent<MeshRenderer>().material = sliceMaterials[1];

        }
    }
}
