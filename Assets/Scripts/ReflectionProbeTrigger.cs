using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReflectionProbeTrigger : MonoBehaviour
{
    ReflectionProbe rp;
    public LayerMask playerLayer;
    public float reflectionActivateRange = 20;
    public float resolutionUpgradeRange = 10;


    // Start is called before the first frame update
    void Start()
    {
        rp = GetComponentInChildren<ReflectionProbe>();
        
    }

    // Update is called once per frame
    void Update()
    {
        // if player in overlap sphere
        if (Physics.OverlapSphere(transform.position, reflectionActivateRange, playerLayer).Length > 0)
        {
            rp.gameObject.SetActive(true);

            /*
            // if player in resolution upgrade range
            if (Physics.OverlapSphere(transform.position, resolutionUpgradeRange, playerLayer).Length > 0)
            {
                rp.resolution = 512;
            }
            else
            {
                rp.resolution = 32;
            }
            */


        }
        else { rp.gameObject.SetActive(false); }
    }
}
