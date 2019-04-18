using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    void FixedUpdate()
    {
        this.transform.position = Camera.main.transform.position + new Vector3(0, 0, 1.0f);
    }
}
