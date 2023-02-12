using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;


public class XRHandAbilities : MonoBehaviour
{
    public enum SKILL_TYPE { ROCK , SLICE }

    [SerializeField] CustomXRHandMovement handMovement;
    [SerializeField] XRRayInteractor rayInteractor;
    [SerializeField] XR_InputManager inputManager;
    public Transform handTransform;
    RaycastHit rayInteractor_hit;

    [Header("Object Pickup")]
    public string nonCollideLayer = "NonCollideable";
    private int heldObjOriginLayer;
    public GameObject currentHeldObject;
    Rigidbody heldObjRB;
    XRGrabInteractable heldObjInteractable;

    [Header("SKILLS ====================== ")]
    public SKILL_TYPE currSkill = SKILL_TYPE.SLICE;
    public GameObject prefabToSpawn;
    private bool primaryButtonDown;
    private bool secondaryButtonDown;
    private bool stickClickDown;


    [Header("Handheld Camera")]
    public GameObject handCamPrefab;
    private GameObject handCam;
    public XR_CameraHandler cameraHandler;
    private bool handCamSpawned;
    private bool handCamPlacedAlready;

    // Start is called before the first frame update
    void Start()
    {
        handMovement = GetComponent<CustomXRHandMovement>();
        rayInteractor = GetComponent<XRRayInteractor>();
        inputManager = handMovement.inputManager;
    }

    private void FixedUpdate()
    {
        // <<<< RAY INTERACTOR HIT >>>>
        rayInteractor.TryGetCurrent3DRaycastHit(out rayInteractor_hit);
        //if (debug && hit.collider != null) Debug.Log("Current Ray Hit " + hit.collider.name);


        // <<<< INPUT CONTROLS >>>>
        InputControls();

    }

    public void InputControls()
    {
        if (handMovement.isLeftController)
        {
            HandCamSkill();
            ObjectPickupSkill();

        }
        else if (handMovement.isRightController)
        {
            MeshSlicerSkill();
            ObjectPickupSkill();
        }
    }


    #region SKILLS ================================

    public void ObjectPickupSkill(bool zeroGRavRelease = false)
    {
        // <<<< SET CURRENT HELD OBJECT >>>> 
        if (currentHeldObject == null && rayInteractor.interactablesSelected.Count > 0)
            SetNewCurrentHeldObject();
        else if (currentHeldObject != null && rayInteractor.interactablesSelected.Count < 1)
            ResetCurrentHeldObject(zeroGRavRelease);

        // <<<< ADJUST THE VELOCITY OF CURRENT HELD OBJECT >>>>
        // stops object from stuttering while carrying it
        if (currentHeldObject != null)
        {
            //adjust velocity to move to hand
            if (heldObjInteractable.attachTransform != null)
            {
                heldObjRB.velocity = (handTransform.position - heldObjInteractable.attachTransform.position) / (Time.fixedDeltaTime);
            }
            else
            {
                heldObjRB.velocity = (handTransform.position - heldObjRB.transform.position) / (Time.fixedDeltaTime);
            }

            //adjust angular velocity 
            heldObjRB.maxAngularVelocity = 40;
        }
    }

    public void HandCamSkill()
    {
        bool primaryButton = handMovement.GetThisPrimaryButton();
        bool secondaryButton = handMovement.GetThisSecondaryButton();

        // <<<< SPAWN / MOVE HAND CAM >>>>
        if (secondaryButton)
        {
            PlaceHandCam();
        }
        else { handCamPlacedAlready = false; }

        // <<<< DELETE HAND CAM >>>>
        if (primaryButton && handCam != null)
        {
            Destroy(handCam);
            handCam = null;
            cameraHandler.handheldCam = null;
            handCamSpawned = false;
        }
    }

    public void SpawnRaycastObjSkill(GameObject prefab)
    {
        bool primaryButton = handMovement.GetThisPrimaryButton();
        bool secondaryButton = handMovement.GetThisSecondaryButton();

        // <<<< SPAWN OBJECT >>>>
        // r secondary button && object not spawned yet
        if (secondaryButton && !secondaryButtonDown)
        {
            SpawnObjectAtRayHit(prefab);

            secondaryButtonDown = true;
        }
        // r secondary button released, object not spawned
        else if (!secondaryButton) { secondaryButtonDown = false; }
    }

    public void MeshSlicerSkill()
    {
        VR_MeshSliceSkill meshSlicer = GetComponent<VR_MeshSliceSkill>();
        if (!meshSlicer) { Debug.LogError("VR Mesh Slice Skill not found on hand. "); }

        bool primaryButton = handMovement.GetThisPrimaryButton();
        bool secondaryButton = handMovement.GetThisSecondaryButton();
        bool stickClick = handMovement.GetTHisJoystickClick();

        // <<<< SPAWN LASER POINTS >>>>
        // r secondary button && object not spawned yet
        if (primaryButton && !primaryButtonDown)
        {
            meshSlicer.CreateNewLaserPoint(transform.position);

            primaryButtonDown = true;
        }
        // r secondary button released, object not spawned
        else if (!primaryButton) { primaryButtonDown = false; }

        // <<<< SELECT OBJECT >>>>
        // r secondary button && object not spawned yet
        List<IXRHoverInteractable> hoverObjs = rayInteractor.interactablesHovered;
        //Debug.Log("Hover Interactables: " + rayInteractor.interactablesHovered.Count);
        if (secondaryButton && hoverObjs.Count > 0 && !secondaryButtonDown)
        {
            // select hovered object
            meshSlicer.SelectObj(hoverObjs[0].colliders[0].gameObject);

            secondaryButtonDown = true;
        }
        // r secondary button released, object not spawned
        else if (!secondaryButton) { secondaryButtonDown = false; }


        // <<< SLICE OBJECT >>>
        //Debug.Log("Stick Click: " + stickClick);
        if (stickClick && !stickClickDown)
        {
            stickClickDown = true;

            if (meshSlicer.laserSlicePoints.Count == 4)
            {
                meshSlicer.Slice();
            }
            else
            {
                meshSlicer.Combine(meshSlicer.selectedObjects);
            }
        }
        else if (!stickClick) { stickClickDown = false; }


    }
    #endregion



    #region HELD OBEJCT FUNCTIONS >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
    public void SetNewCurrentHeldObject()
    {
        currentHeldObject = rayInteractor.interactablesSelected[0].colliders[0].transform.root.gameObject;

        // ** CHANGE LAYERS SO THAT OBJECT DOES NOT COLLIDE WHILE FORCE GRABBED **
        heldObjOriginLayer = currentHeldObject.layer;
        currentHeldObject.layer = LayerMask.NameToLayer(nonCollideLayer);

        currentHeldObject.transform.parent = handTransform;

        heldObjRB = currentHeldObject.GetComponent<Rigidbody>();
        heldObjInteractable = currentHeldObject.GetComponent<XRGrabInteractable>(); 

        Debug.Log("New Held Object: " + currentHeldObject.name);
    }

    public void ResetCurrentHeldObject(bool zeroGravRelease = false)
    {
        Debug.Log("Dropped Held Object: " + currentHeldObject.name);

        currentHeldObject.transform.parent = null;
        currentHeldObject.layer = heldObjOriginLayer;

        heldObjRB.useGravity = !zeroGravRelease;

        heldObjRB = null;
        currentHeldObject = null;

    }
    
    public void PlaceHandCam()
    {
        if (!handCamSpawned)
        {
            handCam = SpawnObjectAtRayHit(handCamPrefab);
            cameraHandler.handheldCam = handCam.transform;
            cameraHandler.LookAtPoint(handCam.transform, handTransform);
            handCamSpawned = true;
        }
        else if (!handCamPlacedAlready)
        {
            MoveObjToRayHit(handCam.transform);
            cameraHandler.LookAtPoint(handCam.transform, handTransform);
            handCamPlacedAlready = true;
        }
    }
    
    #endregion


    #region SPAWN OBJECT FUNCTIONS >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

    public GameObject SpawnObjectAtController(GameObject prefab, Transform parent = null)
    {
        GameObject obj = Instantiate(prefab, handMovement.transform.position, Quaternion.identity);
        obj.transform.parent = parent;

        return obj;
    }

    public GameObject SpawnObjectAtRayHit(GameObject prefab)
    {

        Debug.Log("Spawned Object " + prefab.name);
        return Instantiate(prefab, rayInteractor_hit.point, Quaternion.identity);
    }

    public void MoveObjToRayHit(Transform obj)
    {
        obj.position = rayInteractor_hit.point;

        Debug.Log("Moved Object " + obj.name);

    }
    #endregion



}
