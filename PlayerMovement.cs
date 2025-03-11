using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    public Rigidbody rb;
    public float Forwardforce = 5000f;
    public float Sideforce = 2000f;

    

    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    { 
        if (Input.GetKey("v"))
        {
            rb.AddForce(0, 5000, 0);
        }
        if (Input.GetKey("b"))
        {
            rb.AddForce(0, -5000, 0);
        }
    }
}
