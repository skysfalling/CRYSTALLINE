using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveObjWithMouse : MonoBehaviour
{
    [Range(1, 15)]
    public float z_offset = 5;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 mousePos = Input.mousePosition;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, z_offset));
        transform.position = worldPos;
    }
}
