using UnityEngine;
using UnityEditor;
using System.Drawing;
using Unity.Hierarchy;
using System.Runtime.CompilerServices;
using Microsoft.Win32.SafeHandles;

public class BikeControlCopy : MonoBehaviour
{
    public float frontWheelfcSFOriginalStiffness;
    public float horizontalInput;
    public float verticalInput;
    public float verticalInputUp;
    public float verticalInputDown;
    public float engineBraking = 0.5f;
    public Rigidbody BikeRigidBody;
    float motorTorqueIncrease;
    private Vector3 originalPos;
    //bool braking;
    Rigidbody rb;
    public float handleModifier = 0.7f;

    [Header("Power/Braking")]
    [Space(5)]
    [SerializeField] float motorForce;
    [SerializeField] float brakeForce;
    public Vector3 COG;
    public bool modifyFrictionON = false;

    [Space(20)]
    [HeaderAttribute("Info")]
    [SerializeField] float currentSteeringAngle;
    [Tooltip("Dynamic steering angle based on the speed of the RB, affected by sterReductorAmmount")]
    [SerializeField] float current_maxSteeringAngle;
    [Tooltip("The current lean angle applied")]
    [Range(-45, 45)] public float currentLean;
    private float currentLeanAngle;
    [Space(20)]
    [HeaderAttribute("Speed")]
    [SerializeField] float currentSpeed;
    [SerializeField] float currentKMHSpeed;
    [SerializeField] float currentEnginePower;
    [SerializeField] float currentBrakePowerF;
    [SerializeField] float currentBrakePowerB;


    [Space(20)]
    [Header("Steering")]
    [Space(5)]
    [Tooltip("Defines the maximum steering angle for the bicycle")]
    [SerializeField] float maxSteeringAngle;
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
        //frontWheel.ConfigureVehicleSubsteps(5, 12, 15);
        //backWheel.ConfigureVehicleSubsteps(5, 12, 15);
        rb = GetComponent<Rigidbody>();
        //braking = false;
        originalPos = transform.position;
    }
    private void Update()
    {
        GetInput();
        HandleEngine();
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        Speed_O_Meter();
        HandleEngine();
        LeanOnTurn();
        HandleSteering();
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
        verticalInputUp = Mathf.Clamp(verticalInput, 0.0f, 1.0f);
        verticalInputDown = Mathf.Clamp(verticalInput, -1.0f, 0.0f);


        if (Input.GetKey("r"))
        {
            transform.position = originalPos;
        }
        /*
		if (Input.GetKey("g"))
		{
            WheelFrictionCurve frontWheelfcFF;
            WheelFrictionCurve frontWheelfcSF;
            frontWheelfcFF = frontWheel.forwardFriction;
            frontWheelfcSF = frontWheel.sidewaysFriction;
            frontWheelfcFF.stiffness = 1.2f;
            frontWheelfcSF.stiffness = 1.15f;
            frontWheel.forwardFriction = frontWheelfcFF;
            frontWheel.sidewaysFriction = frontWheelfcSF;

        }
        if (Input.GetKey("h"))
        {
            WheelFrictionCurve backWheelfcFF;
            WheelFrictionCurve backWheelfcSF;
            backWheelfcFF = frontWheel.forwardFriction;
            backWheelfcSF = frontWheel.sidewaysFriction;
            backWheelfcFF.stiffness = 1.2f;
            backWheelfcSF.stiffness = 1.15f;
            frontWheel.forwardFriction = backWheelfcFF;
            frontWheel.sidewaysFriction = backWheelfcSF;

        }
		*/
    }

    private void HandleEngine()
    {
        if (verticalInputUp > 0.0f && verticalInputDown <= 0.0f)
        {
            motorTorqueIncrease = Mathf.Lerp(0.0f, 1.0f, verticalInputUp);

            backWheel.motorTorque = motorTorqueIncrease * motorForce;
            currentEnginePower = backWheel.motorTorque;
        }
        else if(verticalInputUp <= 0.4f)
        {
            backWheel.motorTorque = 0.0f;
        }
        else if (verticalInputDown < 0.0f && verticalInputUp <= 0.0f)
        {
            frontWheel.motorTorque = 0.0f;
            //backWheel.motorTorque = 0.0f;
            backWheel.motorTorque = Mathf.Lerp(backWheel.motorTorque, 0.0f, (100 * Time.deltaTime) * engineBraking);
            frontWheel.brakeTorque = -verticalInputDown * brakeForce;
            backWheel.brakeTorque = -verticalInputDown * brakeForce;
        }
        else if (verticalInputUp < 1.0f && verticalInputDown > 0.0f)
        {
            backWheel.brakeTorque = brakeForce * engineBraking;
            backWheel.motorTorque = -motorForce;

        }
        else if (currentSpeed <= 0.5f && verticalInputUp <= 0.1f)
        {
            backWheel.brakeTorque = 0.0f;
            frontWheel.brakeTorque = 0.0f;
            backWheel.motorTorque = verticalInputDown * motorForce;
        }
        else
        {
            backWheel.motorTorque = Mathf.Lerp(backWheel.motorTorque, 0.0f, (10 * Time.deltaTime) * engineBraking);
            frontWheel.brakeTorque = 0.0f;
            backWheel.brakeTorque = 0.0f;
            frontWheel.motorTorque = 0.0f;
            //backWheel.motorTorque = 0.0f;

        }
        currentEnginePower = backWheel.motorTorque;
        currentBrakePowerF = frontWheel.brakeTorque;
        currentBrakePowerB = backWheel.brakeTorque;

        /*
		//backWheel.motorTorque = braking? 0f : verticalInput * motorForce;
		if (braking)
		{
			backWheel.motorTorque = 0f;
		}
		else
		{
			backWheel.motorTorque = verticalInput * motorForce;
		}
		*/
        //If we are braking, ApplyBreaking applies brakeForce conditional is embedded in parameter	
        //float force = braking ? brakeForce : 0f;

    }
    public void ApplyBraking(float brakeForce)
    {
        frontWheel.brakeTorque = brakeForce;
        backWheel.brakeTorque = brakeForce;

    }

    //This replaces the (Magic numbers) that controlled an exponential decay function for maxteeringAngle (maxSteering angle was not adjustable)
    //This one allows to customize Default bike maxSteeringAngle parameters and maxSpeed allowing for better scalability for each vehicle	
    /// <summary>
    /// Reduces the current maximum Steering based on the speed of the Rigidbody multiplied by SteerReductionAmmount (0-1)  
    /// </summary>
    void MaxSteeringReductor()
    {
        //30 is the value of MaxSpeed at which currentMaxSteering will be at its minimum,			
        float maxSpeed = 30;
        float t = (rb.linearVelocity.magnitude / maxSpeed) * steerReductorAmmount;
        t = t > 1 ? 1.5f : t;
        current_maxSteeringAngle = Mathf.LerpAngle(maxSteeringAngle, 5, t); //5 is the lowest possible degrees of Steering	
    }

    public void HandleSteering()
    {
        MaxSteeringReductor();

        currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, current_maxSteeringAngle * horizontalInput, turnSmoothing * 0.1f);
        frontWheel.steerAngle = currentSteeringAngle;

        //We set the target lean angle to the + or - input value of our steering 
        //We invert our input for rotating in the correct axis
        targetLeanAngle = maxLeanAngle * -horizontalInput;
    }
    public void UpdateHandles()
    {
        handle.localEulerAngles = new Vector3(handle.localEulerAngles.x * handleModifier, currentSteeringAngle * handleModifier, handle.localEulerAngles.z * handleModifier);
        //handle.Rotate(Vector3.up, currentSteeringAngle, Space.Self);

    }
    private void LeanOnTurn()
    {
        Vector3 currentRot = transform.rotation.eulerAngles;
        //Case: not moving much		
        if (rb.linearVelocity.magnitude < 1)
        {
            currentLeanAngle = Mathf.LerpAngle(currentLeanAngle, 0f, 0.1f);
            transform.rotation = Quaternion.Euler(currentRot.x, currentRot.y, currentLeanAngle);
            //return;
        }
        //Case: Not steering or steering a tiny amount
        if (currentSteeringAngle < 0.5f && currentSteeringAngle > -0.5)
        {
            currentLeanAngle = Mathf.LerpAngle(currentLeanAngle, 0f, leanSmoothing * 0.1f);
        }
        //Case: Steering
        else
        {
            //currentLeanAngle = Mathf.LerpAngle(currentLeanAngle, targetLeanAngle, leanSmoothing * 0.1f );

            currentLeanAngle = Mathf.LerpAngle(currentLeanAngle, targetLeanAngle, leanSmoothing * 0.1f);
            rb.centerOfMass = new Vector3(rb.centerOfMass.x, COG.y, rb.centerOfMass.z);
        }
        transform.rotation = Quaternion.Euler(currentRot.x, currentRot.y, currentLeanAngle);
        currentLean = -currentLeanAngle;




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

                backWheelfcFF.stiffness = 1.8f;
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
                frontWheelfcSF.stiffness = 1.25f;

                backWheelfcFF.stiffness = 1.2f;
                backWheelfcSF.stiffness = 2.0f;
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
                frontWheelfcSF.stiffness = 1.0f;

                backWheelfcFF.stiffness = 1.1f;
                backWheelfcSF.stiffness = 2.1f;
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

