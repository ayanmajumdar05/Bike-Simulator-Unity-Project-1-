using UnityEngine;
using UnityEditor;
using System.Drawing;
using Unity.Hierarchy;
using System.Runtime.CompilerServices;
using Microsoft.Win32.SafeHandles;

public class BikeControlTEST : MonoBehaviour
{
    public float targetSteeringAngle;
    public float frontWheelfcSFOriginalStiffness;
    public float LeanRatio;
    public float currentMotorTorque;
    public float horizontalInput;
    public float verticalInput;
    float verticalInputUp;
    float verticalInputDown;
    float horizontalInputLeft;
    float horizontalInputRight;
    public float COGShiftValue;
    public float engineBraking = 0.8f;
    public Rigidbody BikeRigidBody;
    public Vector3 OriginalRot;
    float motorTorqueIncrease;
    private Vector3 originalPos;
    Vector3 originalCOG;
    public Vector3 currentCOG;
    bool isAccelerating;
    bool isBraking;
    Rigidbody rb;
    public float handleModifier = 1f;
    public bool UseController;
    public bool HandleSteeringON;
    public bool HandleCOGMovement;

    [Header("Power/Braking")]
    [Space(5)]
    public float MaxSpeed = 70.0f;
    public float motorForce;
    public float brakeForce;
    public Vector3 COG_Offset;
    public bool modifyFrictionON = false;

    [Space(20)]
    [HeaderAttribute("Info")]
    public float currentSteeringAngle;
    [Tooltip("Dynamic steering angle based on the speed of the RB, affected by sterReductorAmmount")]
    [SerializeField] float current_maxSteeringAngle;
    [Tooltip("The current lean angle applied")]
    [Range(-45, 45)] public float currentLean;
    public float currentLeanAngle;
    [Space(20)]
    [HeaderAttribute("Speed")]
    public float currentSpeed;
    public float currentKMHSpeed;
    [SerializeField] float currentEnginePower;
    [SerializeField] float currentBrakePowerF;
    [SerializeField] float currentBrakePowerB;


    [Space(20)]
    [Header("Steering")]
    [Space(5)]
    [Tooltip("Defines the maximum steering angle for the bicycle")]
    [SerializeField] float maxSteeringAngle;
    [SerializeField] float LeanSpeed;
    [Tooltip("Sets how current_MaxSteering is reduced based on the speed of the RB, (0 - No effect) (1 - Full)")]
    [Range(0f, 1f)][SerializeField] float steerReductorAmmount;
    [Tooltip("Sets the Steering sensitivity [Steering Stiffness] 0 - No turn, 1 - FastTurn)")]
    [Range(0.001f, 1f)][SerializeField] float turnSmoothing;

    [Space(20)]
    [Header("Lean")]
    [Space(5)]
    [Tooltip("Defines the maximum leaning angle for this bicycle")]
    [SerializeField] float maxLeanAngle = 45f;
    [Tooltip("Sets the Leaning sensitivity (0 - None, 1 - full")]
    [Range(0.001f, 1f)][SerializeField] float leanSmoothing;
    float targetLeanAngle;

    [Space(20)]
    [Header("Object References")]
    public Transform handle;
    public Transform suspensionMoving;
    [Space(10)]
    [SerializeField] WheelCollider frontWheel;
    [SerializeField] WheelCollider backWheel;
    [Space(10)]
    [SerializeField] Transform frontWheelTransform;
    [SerializeField] Transform backWheelTransform;


    // Start is called before the first frame update
    void Start()
    {
        //frontContact = frontTrail.transform.GetChild(0).GetComponent<ContactProvider>();
        //rearContact = rearTrail.transform.GetChild(0).GetComponent<ContactProvider>();		
        //Important to stop bicycle from Jittering
        frontWheel.ConfigureVehicleSubsteps(5, 10, 18);
        backWheel.ConfigureVehicleSubsteps(5, 10, 18);
        rb = GetComponent<Rigidbody>();
        
        originalPos = transform.position;
        OriginalRot = transform.rotation.eulerAngles;
        originalCOG = rb.centerOfMass;
        rb.centerOfMass = COG_Offset;
    }
    void Update()
    {
        GetInput();
        //HandleEngine();
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        HandleEngineA();
        Speed_O_Meter();
        LeanOnTurn();
        if (HandleCOGMovement)
        {
            MoveCOG();
        }
        
        if (HandleSteeringON)
        {
            HandleSteering();
        }
        
        UpdateHandles();
        UpdateWheels();
        ModifyFriction();
        //EmitTrail();
        //DebugInfo();
    }

    private void GetInput()
    {
        //float rawHorizontalInput = Input.GetAxis("Horizontal");
        //horizontalInput = Mathf.Lerp(horizontalInput, rawHorizontalInput, 10*Time.deltaTime);

        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
        if(UseController)
        {
            verticalInputUp = Input.GetAxis("Throttle");
            verticalInputDown = Input.GetAxis("Brake");
        }
        else
        {
            verticalInputUp = Mathf.Clamp(verticalInput, 0.0f, 1.0f);
            verticalInputDown = Mathf.Clamp(verticalInput, -1.0f, 0.0f);
        }

        
        horizontalInputRight = Mathf.Clamp(verticalInput, 0.0f, 1.0f);
        horizontalInputLeft = Mathf.Clamp(verticalInput, -1.0f, 0.0f);


        if (Input.GetKey("r"))
        {
            transform.position = originalPos;
            transform.eulerAngles = OriginalRot;
        }
        
    }

    void HandleEngineA()
    {
        //OLD System
        /*
        HandleEngineState();
        if (UseController == true)
        {
            if (verticalInputUp > 0.0f)
            {
                verticalInput = verticalInputUp;
            }
            else if (verticalInputDown < 0.0f)
            {
                verticalInput = verticalInputDown;
            }
            
        }
        if(isAccelerating == true) //accelerating
        {
            
            frontWheel.brakeTorque = 0.0f;
            backWheel.brakeTorque = 0.0f;
            backWheel.motorTorque = Mathf.Lerp(0.0f,motorForce,verticalInputUp);
        }
        
        if (isAccelerating == false && isBraking == false) // empty controls - Engine Braking YES
        {
            frontWheel.brakeTorque = 0.0f;
            backWheel.motorTorque = 0.0f;
            backWheel.brakeTorque = (brakeForce * engineBraking);
            frontWheel.brakeTorque = (brakeForce * engineBraking);
        }
        
        if (isBraking == true) //braking
        {
            backWheel.motorTorque = 0.0f;
            frontWheel.brakeTorque = Mathf.Abs(verticalInputDown * brakeForce );
            backWheel.brakeTorque = Mathf.Abs(verticalInputDown * brakeForce );
            if(currentLean < 3.0f)
            {
                rb.AddForce(0, -1800, 0);
            }
            else
            {
                rb.AddForce(0, -600, 0);
            }
            
        }
        /*
        else
        {
            frontWheel.brakeTorque = 0.0f;
            backWheel.brakeTorque = 0.0f;
            backWheel.motorTorque = 0.0f;
        }
        */

        // NEW system
        /*
        if(verticalInputUp > 0.3f)
        {
            backWheel.motorTorque = Mathf.Lerp(0.0f, motorForce, verticalInputUp);
            rb.AddForce(0, -2000, 0);
            //Accelerate();
            rb.linearDamping= 0.0f;
        }
        else if(verticalInputUp < 0.8f && verticalInputUp >=0.0f)
        {
            backWheel.motorTorque = 0.0f;
            backWheel.brakeTorque = brakeForce * engineBraking;
            rb.AddForce(0, -2000, 0);
            rb.linearDamping = Mathf.Lerp(0.0f,0.5f,50*Time.deltaTime);
            //EngineBrake();
        }
        else
        {
            backWheel.motorTorque = (verticalInput * motorForce) / 2;
            frontWheel.motorTorque = (verticalInput * motorForce) / 2;
        }
        */
        if (verticalInputUp > 0.0f) 
        { 
            backWheel.motorTorque = Mathf.Lerp(0.0f, verticalInputUp * motorForce, verticalInputUp); 
        }

        if (verticalInputUp < 0.5f && verticalInputUp > 0.0f)
        {
            backWheel.motorTorque = 0.0f;
            backWheel.brakeTorque = brakeForce * engineBraking;
            rb.AddForce(0, -2000, 0);
            rb.linearDamping = Mathf.Lerp(0.0f, 0.5f, 50 * Time.deltaTime);
            //EngineBrake();
        }
        frontWheel.brakeTorque = Mathf.Abs(verticalInputDown * brakeForce);
        backWheel.brakeTorque = Mathf.Abs(verticalInputDown * brakeForce);
        
        currentEnginePower = backWheel.motorTorque;
        currentBrakePowerF = frontWheel.brakeTorque;
        currentBrakePowerB = backWheel.brakeTorque;
    }
    public void HandleEngineState()
    {
        if (verticalInput > 0.0f)
        {
            isAccelerating = true; isBraking = false;
        }
        if (verticalInput < 0.0f)
        {
            isAccelerating = false; isBraking = true;
        }
        if (verticalInput == 0.0f)
        {
            isAccelerating = false; isBraking = false;
        }
    }
    public void Accelerate()
    {
        
        
        //backWheel.motorTorque = Mathf.Lerp(0.0f,motorForce,verticalInputUp);
        backWheel.motorTorque = Mathf.Lerp(0.0f, motorForce, verticalInputUp);



    }
    public void EngineBrake()
    {
        if(verticalInputUp < 0.2f && verticalInputDown > -0.2f && rb.linearVelocity.magnitude > 4.0f)
        {
            backWheel.motorTorque = 0.0f;
            backWheel.brakeTorque = (brakeForce * engineBraking);
            
        }
        if(rb.linearVelocity.magnitude < 4.0f)
        {
            backWheel.brakeTorque = 0.0f;
            frontWheel.brakeTorque = 0.0f;
        }
    }
    public void ApplyBraking()
    {
        frontWheel.brakeTorque = Mathf.Abs(verticalInputDown * brakeForce);
        backWheel.brakeTorque = Mathf.Abs(verticalInputDown * brakeForce);
        if (currentLean < 3.0f)
        {
            rb.AddForce(0, -3000, 0);
        }
        else
        {
            rb.AddForce(0, -1000, 0);
        }
    }
    public void HandleSteering()
    {

        // Calculate the target steering angle based on the current lean angle
        targetSteeringAngle = (-currentLeanAngle / maxLeanAngle) * maxSteeringAngle;

        // Smoothly interpolate the current steering angle towards the target steering angle
        currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * 0.1f);

        // Apply the steering angle to the front wheel
        frontWheel.steerAngle = currentSteeringAngle;



    }
    public void UpdateHandles()
    {
        handle.localEulerAngles = new Vector3(handle.localEulerAngles.x * handleModifier, currentSteeringAngle * handleModifier, handle.localEulerAngles.z * handleModifier);
        //handle.Rotate(Vector3.up, currentSteeringAngle, Space.Self);

    }
    void LeanOnTurn()
    {
        Vector3 currentRot = transform.rotation.eulerAngles;
        targetLeanAngle = maxLeanAngle * -horizontalInput;
        currentLeanAngle = Mathf.LerpAngle(currentLeanAngle, targetLeanAngle, LeanSpeed*0.1f);
        transform.rotation = Quaternion.Euler(currentRot.x, currentRot.y, currentLeanAngle);
        currentLean = Mathf.Abs(currentLeanAngle);
        LeanRatio = -currentLeanAngle / maxLeanAngle;
    }
    void MoveCOG()
    {
        /*
        //rb.centerOfMass = COG_Offset;
        float shiftValue;
        shiftValue = Mathf.Lerp(0.0f, 1.5f, LeanRatio);
        if(currentLeanAngle < 0) // Leaning Right
        {
            rb.centerOfMass = new Vector3(shiftValue, COG_Offset.y, COG_Offset.z);
        }
        if(currentLeanAngle > 0)
        {
            rb.centerOfMass = new Vector3(-shiftValue, COG_Offset.y, COG_Offset.z);
        }
        //rb.centerOfMass = new Vector3(COG_Offset.x + shiftValue, COG_Offset.y, COG_Offset.z);
        */

        float shiftValue = Mathf.Lerp(-COGShiftValue, COGShiftValue, (LeanRatio + 1) / 2); 
        rb.centerOfMass = new Vector3(shiftValue, COG_Offset.y, COG_Offset.z);

    }
    public void UpdateWheels()
    {
        UpdateSingleWheel(frontWheel, frontWheelTransform);
        UpdateSingleWheel(backWheel, backWheelTransform);
    }
    /*
	private void EmitTrail() 
	{
		if (braking)
		{
			frontTrail.emitting = frontContact.GetCOntact();
			rearTrail.emitting = rearContact.GetCOntact();
		}
		else
		{			
			frontTrail.emitting = false;
			rearTrail.emitting = false;
		}		
	}
	*/
    void DebugInfo()
    {
        frontWheel.GetGroundHit(out WheelHit frontInfo);
        backWheel.GetGroundHit(out WheelHit backInfo);

        float backCoefficient = (backInfo.sidewaysSlip / backWheel.sidewaysFriction.extremumSlip);
        float frontCoefficient = (frontInfo.sidewaysSlip / frontWheel.sidewaysFriction.extremumSlip);

        //Debug.Log(" Back Coefficient = " + backCoefficient );
        //Debug.Log(" Front Coefficient = " + frontCoefficient);	
    }

    private void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelTransform)
    {
        Vector3 position;
        Quaternion rotation;
        wheelCollider.GetWorldPose(out position, out rotation);
        wheelTransform.rotation = rotation;
        wheelTransform.position = position;
    }

    void Speed_O_Meter()
    {
        currentSpeed = rb.linearVelocity.magnitude;
        currentKMHSpeed = currentSpeed * 3.6f;
        LeanRatio = currentLean / maxLeanAngle;
        currentCOG = rb.centerOfMass;

    }
    void ModifyFriction()
    {
        WheelFrictionCurve frontWheelfcSFOriginal;
        WheelFrictionCurve frontWheelfcSFChange;
        frontWheelfcSFOriginal = frontWheel.sidewaysFriction;
        frontWheelfcSFChange = frontWheel.sidewaysFriction;

        frontWheelfcSFOriginalStiffness = frontWheelfcSFOriginal.stiffness;
        if (modifyFrictionON)
        {
            // Friction Changes
            if (currentSpeed > 0 && currentSpeed <= 10) // 0-10 FwF Modifier 
            {
                WheelFrictionCurve frontWheelfcFF;
                WheelFrictionCurve frontWheelfcSF;

                WheelFrictionCurve backWheelfcFF;
                WheelFrictionCurve backWheelfcSF;
                frontWheelfcFF = frontWheel.forwardFriction;
                frontWheelfcSF = frontWheel.sidewaysFriction;

                backWheelfcFF = backWheel.forwardFriction;
                backWheelfcSF = backWheel.sidewaysFriction;

                frontWheelfcFF.stiffness = 1.0f;
                frontWheelfcSF.stiffness = 1.0f;

                backWheelfcFF.stiffness = 1.6f;
                backWheelfcSF.stiffness = 1.8f;
                frontWheel.forwardFriction = frontWheelfcFF;
                frontWheel.sidewaysFriction = frontWheelfcSF;
                backWheel.forwardFriction = backWheelfcFF;
                backWheel.sidewaysFriction = backWheelfcSF;
            }
            else if (currentSpeed < 30 && currentSpeed > 10) // 10-30 FwF modifier
            {
                WheelFrictionCurve frontWheelfcFF;
                WheelFrictionCurve frontWheelfcSF;

                WheelFrictionCurve backWheelfcFF;
                WheelFrictionCurve backWheelfcSF;
                frontWheelfcFF = frontWheel.forwardFriction;
                frontWheelfcSF = frontWheel.sidewaysFriction;

                backWheelfcFF = backWheel.forwardFriction;
                backWheelfcSF = backWheel.sidewaysFriction;

                frontWheelfcFF.stiffness = 1.1f;
                frontWheelfcSF.stiffness = 1.1f;

                backWheelfcFF.stiffness = 1.5f;
                backWheelfcSF.stiffness = 2.0f;
                frontWheel.forwardFriction = frontWheelfcFF;
                frontWheel.sidewaysFriction = frontWheelfcSF;

                backWheel.forwardFriction = backWheelfcFF;
                backWheel.sidewaysFriction = backWheelfcSF;
            }
            else if (currentSpeed < 50 && currentSpeed > 30) // 30-50 FwF modifier
            {
                WheelFrictionCurve frontWheelfcFF;
                WheelFrictionCurve frontWheelfcSF;

                WheelFrictionCurve backWheelfcFF;
                WheelFrictionCurve backWheelfcSF;
                frontWheelfcFF = frontWheel.forwardFriction;
                frontWheelfcSF = frontWheel.sidewaysFriction;

                backWheelfcFF = backWheel.forwardFriction;
                backWheelfcSF = backWheel.sidewaysFriction;

                frontWheelfcFF.stiffness = 1.15f;
                frontWheelfcSF.stiffness = 1.2f;

                backWheelfcFF.stiffness = 1.35f;
                backWheelfcSF.stiffness = 2.1f;
                frontWheel.forwardFriction = frontWheelfcFF;
                frontWheel.sidewaysFriction = frontWheelfcSF;
                backWheel.forwardFriction = backWheelfcFF;
                backWheel.sidewaysFriction = backWheelfcSF;
            }
            else if (currentSpeed < 70 && currentSpeed > 50) // 30-50 FwF modifier
            {
                WheelFrictionCurve frontWheelfcFF;
                WheelFrictionCurve frontWheelfcSF;

                WheelFrictionCurve backWheelfcFF;
                WheelFrictionCurve backWheelfcSF;
                frontWheelfcFF = frontWheel.forwardFriction;
                frontWheelfcSF = frontWheel.sidewaysFriction;

                backWheelfcFF = backWheel.forwardFriction;
                backWheelfcSF = backWheel.sidewaysFriction;

                frontWheelfcFF.stiffness = 1.1f;
                frontWheelfcSF.stiffness = 1.4f;

                backWheelfcFF.stiffness = 1.1f;
                backWheelfcSF.stiffness = 2.6f;
                frontWheel.forwardFriction = frontWheelfcFF;
                frontWheel.sidewaysFriction = frontWheelfcSF;
                backWheel.forwardFriction = backWheelfcFF;
                backWheel.sidewaysFriction = backWheelfcSF;
            }
            else if (currentSpeed < 90 && currentSpeed > 70) // 30-50 FwF modifier
            {
                WheelFrictionCurve frontWheelfcFF;
                WheelFrictionCurve frontWheelfcSF;

                WheelFrictionCurve backWheelfcFF;
                WheelFrictionCurve backWheelfcSF;
                frontWheelfcFF = frontWheel.forwardFriction;
                frontWheelfcSF = frontWheel.sidewaysFriction;

                backWheelfcFF = backWheel.forwardFriction;
                backWheelfcSF = backWheel.sidewaysFriction;

                frontWheelfcFF.stiffness = 1.0f;
                frontWheelfcSF.stiffness = 1.3f;

                backWheelfcFF.stiffness = 1.5f;
                backWheelfcSF.stiffness = 2.1f;
                frontWheel.forwardFriction = frontWheelfcFF;
                frontWheel.sidewaysFriction = frontWheelfcSF;
                backWheel.forwardFriction = backWheelfcFF;
                backWheel.sidewaysFriction = backWheelfcSF;
            }
            else
            {
                WheelFrictionCurve frontWheelfcFF;
                WheelFrictionCurve frontWheelfcSF;

                WheelFrictionCurve backWheelfcFF;
                WheelFrictionCurve backWheelfcSF;
                frontWheelfcFF = frontWheel.forwardFriction;
                frontWheelfcSF = frontWheel.sidewaysFriction;

                backWheelfcFF = backWheel.forwardFriction;
                backWheelfcSF = backWheel.sidewaysFriction;

                frontWheelfcFF.stiffness = 1.1f;
                frontWheelfcSF.stiffness = 1.35f;

                backWheelfcFF.stiffness = 1.2f;
                backWheelfcSF.stiffness = 2.0f;
                frontWheel.forwardFriction = frontWheelfcFF;
                frontWheel.sidewaysFriction = frontWheelfcSF;
                backWheel.forwardFriction = backWheelfcFF;
                backWheel.sidewaysFriction = backWheelfcSF;
            }

        }

    }
}

