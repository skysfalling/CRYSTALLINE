using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRRigManager : MonoBehaviour
{
    public XR_PhysicsHand leftPhysicsHand;
    public XR_PhysicsHand rightPhysicsHand;

    public void ResetPhysicsHandPosition()
    {
        if (leftPhysicsHand != null && rightPhysicsHand != null)
        {
            leftPhysicsHand.SetPositionToPlayer();
            rightPhysicsHand.SetPositionToPlayer();
        }
    }
}
