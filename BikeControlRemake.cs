using UnityEngine;
using UnityEditor;
using System.Drawing;
using Unity.Hierarchy;
using System.Runtime.CompilerServices;
using Microsoft.Win32.SafeHandles;
using Unity.VisualScripting;
using UnityEngine.InputSystem;
using System.Collections;
using System;
using TMPro;
using UnityEngine.Analytics;


public enum GearState { Neutral, Running, CheckingChange, Changing };
public enum GearBoxType { Manual, Automatic };

public class BikeControlRemake : MonoBehaviour
{
    PlayerControls controls;
    float throttleVal;
    float brakeVal;
    public float previousGearSpeed;
    public float currentGearSpeed;
    public float nextGearSpeed;
    public float susDiffDistance;
    public bool rotateBike;
    public bool MoveSuspension;
    public bool shiftCollider;
    public bool rotateWheel;
    public GameObject ClusterSpeedoReadout;
    void Awake()
    {
        controls = new PlayerControls();

        controls.Gameplay.Throttle.performed += ctx => throttleVal = ctx.ReadValue<float>();
        controls.Gameplay.Throttle.canceled += ctx => throttleVal = 0.0f;

        controls.Gameplay.Brake.performed += ctx => brakeVal = ctx.ReadValue<float>();
        controls.Gameplay.Brake.canceled += ctx => brakeVal = 0.0f;


    }
    void OnEnable()
    {
        controls.Gameplay.Enable();
    }
    void OnDisable()
    {
        controls.Gameplay.Disable();
    }
    public float ThrottleValue;
    public float targetSteeringAngle;
    public float backCoefficient;
    public float frontCoefficient;
    public float frontWheelForwardSlip;
    public float frontWheelSidewaysSlip;
    public float backWheelForwardSlip;
    public float backWheelSidewaysSlip;
    private bool isHandling;
    float frontWheelfcSFOriginalStiffness; //opt
    public float LeanRatio; //Imp
    float currentMotorTorque; //Opt
    float horizontalInput; //Imp
    public float verticalInput; //Imp
    float verticalInputUp; //Imp
    float verticalInputDown; //Imp
    float horizontalInputLeft;
    float horizontalInputRight;
    public float COGShiftTarget; //Imp - Inspector
    float COGShiftValue; //Imp
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
    public bool useHandles;
    public bool moveSuspension;
    public float maxSteeringSpeed = 17.0f;

    [Header("Power/Braking")]
    [Space(5)]
    public float MaxSpeed = 70.0f;
    public float motorForce;
    public float brakeForce;
    public float brakeModifier;
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

    [SerializeField] float maxSteeringAngle;
    [SerializeField] float LeanSpeed;

    [Range(0f, 1f)][SerializeField] float steerReductorAmmount;

    [Range(0.001f, 1f)][SerializeField] float turnSmoothing;

    [Space(20)]
    [Header("Lean")]
    [Space(5)]

    [SerializeField] float maxLeanAngle = 45f;

    [Range(0.001f, 1f)][SerializeField] float leanSmoothing;
    float targetLeanAngle;

    [Space(20)]
    [Header("Object References")]
    public Transform handle;
    public Transform suspensionMoving;
    public Transform TripleClamp;
    public Transform suspensionFixed;
    [Space(10)]
    [SerializeField] WheelCollider frontWheel;
    [SerializeField] WheelCollider backWheel;
    [Space(10)]
    public Transform frontWheelTransform;
    public Transform backWheelTransform;
    public GameObject susTop;
    public GameObject susBottom;
    public GameObject ClusterNeedle;
    public GameObject TailLight;
    public GameObject FrontLeftLightReflect;
    public GameObject FrontRightLightReflect;
    public GameObject FrontLeftLightSpot;
    public GameObject FrontRightLightSpot;
    public bool isLightON;
    public TMP_Text RPMText;
    public TMP_Text GearText;
    [Space(10)]
    [Header("Engine")]
    public AnimationCurve hptoRPMCurve;
    public float clutch;
    public float currentRPM;
    public float variableValue;
    private float idleRPM = 1200.0f;
    public float maxRPM = 10000.0f;
    public float ratioRPM;
    public float wheelRPM;
    private float ClusterstartPosition = 0.0f;
    private float ClusterendPosition = -270.0f;
    private float ClusterdesiredPosition;
    [Serialize] public float[] gearRatios;
    [Serialize] public float[] gearMaxSpeed;
    private GearState gearState;
    public GearBoxType gearBoxType;
    public bool isChangingGear;
    public float increaseGearRPM;
    public float decreaseGearRPM;
    public float ShiftUpTime = 0.4f;
    public float changeGearTime = 0.5f;
    public float currentGearRatio;
    private float currentTorque;
    public bool ignitionON;
    public bool engineRunning = false;
    public bool startEngine;
    public bool clusterCheck = true;
    public int currentGear;
    public int currentGearSelect = 0;
    public float gearModifier;
    public float gearSpeedRatio;


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
        clusterCheck = true;
        gearBoxType = GearBoxType.Manual;
    }
    void Update()
    {
        GetInput();
        //HandleEngineA();
        HandleElectronics();

        updateSuspension();
        UpdateWheels();

    }
    // Update is called once per frame
    void FixedUpdate()
    {
        //HandleGearBox();
        HandleEngineA();
        //HandleEngineB();
        //ApplyMotor();
        CheckSlip();
        ABSSystem();
        ApplyBrake();

        Speed_O_Meter();
        //UpdateNeedle();
        UpdateCluster();
        LeanOnTurn();
        if (HandleCOGMovement)
        {
            if (isAccelerating == true)
            {
                MoveCOG();
            }
            if (isBraking == true)
            {
                //Dont move COG
            }
            if (isAccelerating == false || isBraking == true)
            {
                MoveCOG();
            }

        }
        /*
        if (currentSpeed <= 15.0f)
        {

            if (HandleSteeringON)
            {
                isHandling = true;

                HandleSteering();
            }
            handleModifier = 1.0f;

        }
        if (currentSpeed >= 15.0f && isAccelerating == false && isBraking == false) // empty controls - Engine Braking YES
        {
            if (HandleSteeringON)
            {
                isHandling = true;
                HandleSteering();
            }
            handleModifier = 0.35f;
        }
        if (currentSpeed >= 15.0f && isAccelerating == true && isBraking == false) // Accelerating
        {
            handleModifier = 0.02f;
            HandleSteering();
            isHandling = false;

        }
        if (currentSpeed >= 15.0f && isBraking == true && isAccelerating == false) // Braking
        {
            isHandling = true;
            HandleSteering();
            handleModifier = 0.3f;
        }
        */
        HandleSteering();
        ModifyFriction();
        UpdateHandles();

        if (currentGear != 0)
        {
            previousGearSpeed = gearMaxSpeed[currentGear - 1];
        }
        if (currentGear != 6)
        {
            nextGearSpeed = gearMaxSpeed[currentGear + 1];
        }
        currentGearSpeed = gearMaxSpeed[currentGear];
    }
    void UpdateCluster()
    {
        UpdateNeedle();
        string RPMTextVal;
        if (currentRPM < 10000)
        {
            RPMTextVal = currentRPM.ToString("0000");
            RPMText.SetText(RPMTextVal);
        }
        if (currentRPM > 9999)
        {
            RPMTextVal = currentRPM.ToString("00000");
            RPMText.SetText(RPMTextVal);
        }
        string GearTextVal = (gearState == GearState.Neutral) ? "N" : (currentGear + 1).ToString();
        GearText.SetText(GearTextVal);
    }

    private void GetInput()
    {
        //float rawHorizontalInput = Input.GetAxis("Horizontal");
        //horizontalInput = Mathf.Lerp(horizontalInput, rawHorizontalInput, 10*Time.deltaTime);

        float horizontalInputTemp = Input.GetAxis("Horizontal");
        float smoothCurrentVelocity = 0.0f;
        horizontalInput = Mathf.SmoothDamp(horizontalInput, horizontalInputTemp, ref smoothCurrentVelocity, 0.005f);
        verticalInput = Input.GetAxis("Vertical");
        if (UseController)
        {
            verticalInputUp = throttleVal;
            verticalInputDown = -brakeVal;
        }
        else
        {
            if (verticalInput == 0.0 && verticalInputUp != 0.0f)
            {
                verticalInputUp = 0.0f;
            }
            if (verticalInput == 0.0 && verticalInputDown != 0.0f)
            {
                verticalInputDown = 0.0f;
            }
            if (verticalInput > 0.0f)
            {
                verticalInputUp = Mathf.Clamp(verticalInput, 0.0f, 1.0f);
                verticalInputDown = 0.0f;
            }
            if (verticalInput < 0.0f)
            {
                verticalInputDown = Mathf.Clamp(verticalInput, -1.0f, 0.0f);
                verticalInputUp = 0.0f;
            }
            if (verticalInput == 0.0f)
            {
                verticalInputUp = 0.0f;
                verticalInputDown = 0.0f;
            }

            //verticalInputUp = Mathf.Clamp(verticalInput, 0.0f, 1.0f);
            //verticalInputDown = Mathf.Clamp(verticalInput, -1.0f, 0.0f);
        }


        horizontalInputRight = Mathf.Clamp(verticalInput, 0.0f, 1.0f);
        horizontalInputLeft = Mathf.Clamp(verticalInput, -1.0f, 0.0f);


        if (Input.GetKey("r"))
        {
            transform.position = originalPos;
            transform.eulerAngles = OriginalRot;
            currentGear = 0;
            currentGearSelect = 0;

        }
        if (Input.GetKeyDown(KeyCode.LeftShift)) // UPSHIFT
        {

            if (currentGearSelect < 6)
            {
                currentGearSelect += 1;
            }


        }
        if (Input.GetKeyDown(KeyCode.LeftControl)) // DOWNSHIFT
        {

            if (currentGearSelect > 0)
            {
                currentGearSelect -= 1;
            }
            if (currentGearSelect < 1)
            {
                currentGearSelect = 0;
            }


        }
        if (Input.GetKeyDown("q")) // TOGGLE GEARBOX MODE
        {
            if (gearBoxType == GearBoxType.Manual)
            {
                gearBoxType = GearBoxType.Automatic;
            }
            else
            {
                gearBoxType = GearBoxType.Manual;
            }
        }
        if (Input.GetKeyDown("i")) //IGNITION TOGGLE
        {
            if (ignitionON == false)
            {
                ignitionON = true;
            }
            else
            {
                ignitionON = false;
            }
        }
        if (Input.GetKeyDown("h")) // ENGINE START/STOP TOGGLE
        {
            if (startEngine == false)
            {
                startEngine = true;
            }
            else
            {
                startEngine = false;
            }
        }

        if (gearState != GearState.Changing)
        {
            if (gearState == GearState.Neutral)
            {
                clutch = 0;
                if (Mathf.Abs(verticalInputUp) > 0) gearState = GearState.Running;
            }
            else
            {
                clutch = Input.GetKey("g") ? 0 : Mathf.Lerp(clutch, 1, Time.deltaTime);
            }
        }
        clutch = Input.GetKey("g") ? 0 : Mathf.Lerp(clutch, 1.0f, Time.deltaTime);

    }
    void HandleElectronics()
    {
        if (verticalInputDown < 0.0f)
        {
            TailLight.SetActive(true);
        }
        else
        {
            TailLight.SetActive(false);
        }
        if (isLightON == true)
        {
            FrontLeftLightReflect.SetActive(true);
            FrontRightLightReflect.SetActive(true);
            FrontLeftLightSpot.SetActive(true);
            FrontRightLightSpot.SetActive(true);
        }
        else
        {
            FrontLeftLightReflect.SetActive(false);
            FrontRightLightReflect.SetActive(false);
            FrontLeftLightSpot.SetActive(false);
            FrontRightLightSpot.SetActive(false);
        }

    }
    void ApplyBrake()
    {
        frontWheel.brakeTorque = Mathf.Abs(verticalInputDown * brakeForce) * brakeModifier * 0.75f;
        backWheel.brakeTorque = Mathf.Abs(verticalInputDown * brakeForce) * brakeModifier * 0.5f;
    }
    void ApplyMotor()
    {
        currentTorque = CalculateTorque();
        backWheel.motorTorque = currentTorque * verticalInputUp;
    }
    float CalculateTorque()
    {
        float torque = 0;
        if (currentRPM < idleRPM + 200 && verticalInputUp < 0.1f && currentGear == 0)
        {
            gearState = GearState.Neutral;


        }
        if (gearBoxType == GearBoxType.Automatic)
        {
            float increaseGearRPMTemp = increaseGearRPM;
            float decreaseGearRPMTemp = decreaseGearRPM;
            if (gearState == GearState.Running && clutch > 0)
            {
                if (currentRPM > increaseGearRPMTemp)
                {
                    StartCoroutine(ChangeGear(1));
                }
                if (currentRPM < decreaseGearRPMTemp)
                {
                    StartCoroutine(ChangeGear(-1));
                }
            }
        }
        if (gearBoxType == GearBoxType.Manual)
        {
            float increaseGearRPMTemp = idleRPM;
            float decreaseGearRPMTemp = maxRPM;
            HandleGearBox(increaseGearRPMTemp, decreaseGearRPMTemp);
        }
        if (engineRunning == true)
        {
            if (clutch < 0.1f) //Clutch Engaged
            {
                currentRPM = Mathf.Lerp(currentRPM, Mathf.Max(idleRPM, maxRPM * verticalInputUp) + UnityEngine.Random.Range(-50, 50), 10 * Time.deltaTime);
            }
            else //Clutch disengaged - Connected
            {
                wheelRPM = Mathf.Abs(backWheel.rpm) * gearRatios[currentGear];
                currentRPM = Mathf.Lerp(currentRPM, Mathf.Max(idleRPM - 100, wheelRPM), Time.deltaTime * 3f);
                torque = (hptoRPMCurve.Evaluate(currentRPM / maxRPM) * motorForce / currentRPM) * gearRatios[currentGear] * 5252f * clutch;
            }
        }
        return torque;
    }


    void HandleEngineA()
    {
        if (ignitionON == true)
        {
            ClusterSpeedoReadout.SetActive(true);
            if (!startEngine && !engineRunning)
            {
                if (clusterCheck == true && currentRPM == 0.0f)
                {
                    StartCoroutine(AdjustRPM(currentRPM, maxRPM, 1.0f));
                }
                if (clusterCheck == true && currentRPM == maxRPM)
                {
                    StartCoroutine(AdjustRPM(currentRPM, 0.0f, 1.0f));
                    clusterCheck = false;
                }

                if (clusterCheck == false && currentRPM == 0.0f)
                {
                    StopAllCoroutines();
                }
                else if (clusterCheck == false && currentRPM > 0.0f)
                {
                    //IGNORE
                }
            }


            if (startEngine && !engineRunning)
            {
                engineRunning = true;
                StopAllCoroutines();
                StartCoroutine(SmoothIncreaseRPM(0.0f, idleRPM, 1.0f));
            }
            else if (engineRunning && startEngine)
            {
                ApplyMotor();
            }
            else if (!startEngine && engineRunning)
            {
                engineRunning = false;
                StopAllCoroutines();
                StartCoroutine(SmoothDecreaseRPM(currentRPM, 0.0f, 1.0f));
                if (currentRPM == 0.0f)
                {
                    StopAllCoroutines();
                }
            }
        }
        if (ignitionON == false)
        {
            if (ignitionON == false && engineRunning == true)
            {
                StartCoroutine(SmoothDecreaseRPM(currentRPM, 0.0f, 1.0f));
            }
            engineRunning = false;
            startEngine = false;
            clusterCheck = true;
            ClusterSpeedoReadout.SetActive(false);
            //StopCoroutine("EngineCheck");
            //StopAllCoroutines();
            StartCoroutine(SmoothDecreaseRPM(currentRPM, 0.0f, 1.0f));
            if (currentRPM == 0.0f)
            {
                StopAllCoroutines();
            }
            //StopAllCoroutines();
            //currentRPM = 0.0f;
        }

    }

    public IEnumerator SmoothIncreaseRPM(float startRPM, float endRPM, float duration)
    {
        float elapsed = 0.0f;
        while (elapsed < duration)
        {
            currentRPM = Mathf.Lerp(startRPM, endRPM, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        currentRPM = endRPM;
    }

    public IEnumerator SmoothDecreaseRPM(float startRPM, float endRPM, float duration)
    {
        float elapsed = 0.0f;
        while (elapsed < duration)
        {
            currentRPM = Mathf.Lerp(startRPM, endRPM, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        currentRPM = endRPM;
    }
    public IEnumerator EngineCheck(float endRPM, float duration)
    {
        //Zero to Max case
        float elapsed = 0.0f;
        if (currentRPM < maxRPM)
        {
            while (elapsed < duration)
            {
                currentRPM = Mathf.Lerp(0.0f, endRPM, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        if (currentRPM >= maxRPM - 100.0f)
        {
            //Max to Zero case
            elapsed = 0.0f;

            while (elapsed < duration)
            {
                currentRPM = Mathf.Lerp(endRPM, 0.0f, elapsed / duration);
                elapsed += Time.deltaTime;
                if (currentRPM == 0.0f) { clusterCheck = false; }
                yield return null;
            }

        }

    }
    IEnumerator ChangeGear(int gearChange)
    {
        if (isChangingGear)
        {
            yield break; // Exit if the coroutine is already running
        }

        isChangingGear = true;
        gearState = GearState.CheckingChange;
        if (currentGear + gearChange >= 0)
        {
            if (gearBoxType == GearBoxType.Automatic)
            {
                if (gearChange > 0)
                {
                    yield return new WaitForSeconds(ShiftUpTime);
                    //increase gear
                    if (currentRPM < increaseGearRPM || currentGear >= gearRatios.Length - 1)
                    {
                        gearState = GearState.Running;
                        isChangingGear = false;
                        yield break;
                    }
                }
                if (gearChange < 0)
                {
                    //decrease gear
                    yield return new WaitForSeconds(changeGearTime);


                    if (currentRPM > decreaseGearRPM || currentGear <= 0)
                    {
                        gearState = GearState.Running;
                        isChangingGear = false;
                        yield break;
                    }

                }
                gearState = GearState.Changing;
                yield return new WaitForSeconds(changeGearTime);
                currentGear += gearChange;
            }
            if (gearBoxType == GearBoxType.Manual)
            {
                if (gearChange > 0)
                {
                    yield return new WaitForSeconds(0.1f);
                    //increase gear
                    if (currentGear >= gearRatios.Length - 1)
                    {
                        gearState = GearState.Running;
                        isChangingGear = false;
                        yield break;
                    }
                }
                if (gearChange < 0)
                {
                    //decrease gear
                    yield return new WaitForSeconds(0.1f);

                    if (currentGear <= 0)
                    {
                        gearState = GearState.Running;
                        isChangingGear = false;
                        yield break;
                    }
                }
                gearState = GearState.Changing;
                yield return new WaitForSeconds(0.1f);
                currentGear += gearChange;
            }
        }

        if (gearState != GearState.Neutral) { gearState = GearState.Running; }
        isChangingGear = false;
    }
    IEnumerator AdjustRPM(float startValue, float endValue, float time)
    {
        float elapsed = 0.0f;

        while (elapsed < time)
        {
            currentRPM = Mathf.Lerp(startValue, endValue, elapsed / time);
            elapsed += Time.deltaTime;
            yield return null;
        }
        currentRPM = endValue; // Ensure exact final value
    }

    // --------------------------------------------------------------------------------------------------
    void HandleGearBox(float increaseGearRPMTemp, float decreaseGearRPMTemp)
    {
        if (isChangingGear == false)
        {
            if (currentGearSelect > currentGear)
            {
                StartCoroutine(ChangeGear(1));
            }
            if (currentGearSelect < currentGear)
            {
                /*
                if (currentSpeed > previousGearSpeed / 3.6) 
                {
                    currentGearSelect = currentGear;
                    Debug.Log("Cannot downshift at this speed");
                }
                */
                if (currentSpeed > previousGearSpeed / 3.6 && currentRPM > 7600) //Higher speed and higher RPM-NOT VALID
                {
                    currentGearSelect = currentGear;
                    Debug.Log("Cannot downshift at this speed");
                }
                if (currentSpeed > previousGearSpeed / 3.6 && currentRPM <= 7600) // Higher Speed but lower RPM-VALID 
                {
                    StartCoroutine(ChangeGear(-1));
                }
                if (currentSpeed < previousGearSpeed / 3.6) // Lower Speed - VALID
                {
                    StartCoroutine(ChangeGear(-1));
                }





            }
        }
        else
        {
            //IGNORE
        }
        float currentGearMaxSpeed = gearMaxSpeed[currentGear];
        gearSpeedRatio = currentSpeed / currentGearMaxSpeed;
        gearModifier = motorForce * gearSpeedRatio;

    }
    //---------------------------------------------------------------------------------------------------
    void HandleEngineB()
    {
        if (engineRunning == true)
        {
            ratioRPM = currentRPM / maxRPM;
            if (clutch < 1f)
            {
                wheelRPM = Mathf.Abs(backWheel.rpm * gearRatios[currentGear]);
                currentRPM = Mathf.Lerp(currentRPM, Mathf.Max(idleRPM - 100, wheelRPM), Time.deltaTime * 3f);
                backWheel.motorTorque = ((currentRPM / maxRPM) * motorForce / currentRPM) * gearRatios[currentGear] * clutch;


            }
            else
            {
                currentEnginePower = Mathf.Lerp(0.0f, (currentRPM / 1000.0f) * motorForce, clutch) * gearModifier;
                backWheel.motorTorque = currentEnginePower * clutch;
            }
        }
        if (engineRunning == false)
        {
            currentEnginePower = 0.0f;
            backWheel.motorTorque = 0.0f;
        }
        if (verticalInputDown < 0.0f)
        {
            frontWheel.brakeTorque = Mathf.Abs(verticalInputDown * brakeForce) * brakeModifier;
            backWheel.brakeTorque = Mathf.Abs(verticalInputDown * brakeForce) * brakeModifier * 0.7f;
        }
        else
        {
            frontWheel.brakeTorque = 0.0f;
            backWheel.brakeTorque = 0.0f;
        }
        currentEnginePower = backWheel.motorTorque;
        currentBrakePowerF = frontWheel.brakeTorque;
        currentBrakePowerB = backWheel.brakeTorque;
    }
    public void HandleEngineState()
    {
        if (verticalInput > 0.0f)
        {
            isBraking = false;
            isAccelerating = true;
        }
        if (verticalInput < 0.0f)
        {
            isBraking = true;
            isAccelerating = false;
        }
        if (verticalInput == 0.0f)
        {
            isAccelerating = false;
            isBraking = false;
        }
    }

    void ABSSystem()
    {
        if (verticalInputDown < -0.01f)
        {
            if (frontWheelSidewaysSlip > 0.01f && frontWheelSidewaysSlip <= 1.0f || frontWheelSidewaysSlip < -0.01f && frontWheelSidewaysSlip >= -1.0f)
            {
                if (brakeModifier > 0.4)
                {
                    brakeModifier -= 2.0f * Time.deltaTime;
                }
                else { brakeModifier = 0.65f; }
            }
            if (frontWheelSidewaysSlip <= 0.01f && frontWheelSidewaysSlip > 0.0f || frontWheelSidewaysSlip >= -0.01f && frontWheelSidewaysSlip < 0.0f)
            {
                brakeModifier = 1.0f;
            }
        }
        else
        {
            brakeModifier = 1.0f;
        }
    }
    public void Accelerate()
    {
        backWheel.motorTorque = Mathf.Lerp(0.0f, motorForce, verticalInputUp);
    }
    public void EngineBrake()
    {
        if (verticalInputUp < 0.2f && verticalInputDown > -0.2f && rb.linearVelocity.magnitude > 4.0f)
        {
            backWheel.motorTorque = 0.0f;
            backWheel.brakeTorque = (brakeForce * engineBraking);

        }
        if (rb.linearVelocity.magnitude < 4.0f)
        {
            backWheel.brakeTorque = 0.0f;
            frontWheel.brakeTorque = 0.0f;
        }
    }
    public void ApplyBraking()
    {
        frontWheel.brakeTorque = Mathf.Abs(verticalInputDown * brakeForce);
        backWheel.brakeTorque = Mathf.Abs(verticalInputDown * brakeForce);

    }
    public void HandleSteering()
    {
        /*
        if (currentSpeed <= 5.0f)
        {
            float maxSteeringAngletemp = 30.0f;
            maxSteeringAngle = maxSteeringAngletemp;
            targetSteeringAngle = (-currentLeanAngle / maxLeanAngle) * maxSteeringAngletemp;
            //targetSteeringAngle = maxSteeringAngletemp;
            currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * 0.1f);
            frontWheel.steerAngle = currentSteeringAngle;
        }
        */

        if (currentSpeed <= 5.0f)
        {
            float maxSteeringAngletemp = 37.0f;
            maxSteeringAngle = maxSteeringAngletemp;
            targetSteeringAngle = horizontalInput * maxSteeringAngletemp;
            //targetSteeringAngle = maxSteeringAngletemp;
            currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * 0.1f);
            frontWheel.steerAngle = currentSteeringAngle;
        }
        /*
        if (currentSpeed > 5.0f && currentSpeed < 10.0f)
        {
            float maxSteeringAngletemp = 24.0f;
            maxSteeringAngle = maxSteeringAngletemp;
            targetSteeringAngle = horizontalInput * maxSteeringAngletemp;
            currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * 0.1f);
            frontWheel.steerAngle = currentSteeringAngle;
        }
        if (currentSpeed > 10.0f && currentSpeed < 15.0f)
        {
            float maxSteeringAngletemp = 18.0f;
            maxSteeringAngle = maxSteeringAngletemp;
            targetSteeringAngle = (-currentLeanAngle / maxLeanAngle) * maxSteeringAngletemp;
            currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * 0.1f);
            frontWheel.steerAngle = currentSteeringAngle;
        }
        */
        //------------------------------------
        if (currentSpeed >= 5.0f && currentSpeed < 15.0f)
        {
            float maxSteeringAngletemp = 30.0f;
            //maxSteeringAngle = maxSteeringAngletemp;
            targetSteeringAngle = ((-currentLeanAngle / maxLeanAngle) + horizontalInput) / 2 * maxSteeringAngletemp;
            currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * 0.1f);
            frontWheel.steerAngle = currentSteeringAngle;
        }
        //-----------------------------------
        if (currentSpeed >= 15.0f && currentSpeed < 35.0f)
        {
            float maxSteeringAngletemp;
            if (isBraking == true)
            {
                maxSteeringAngletemp = 15.0f;
            }
            else
            {
                maxSteeringAngletemp = 10.0f;
            }

            maxSteeringAngle = maxSteeringAngletemp;
            targetSteeringAngle = (-currentLeanAngle / maxLeanAngle) * maxSteeringAngletemp;
            currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * 0.1f);
            frontWheel.steerAngle = currentSteeringAngle;

        }
        if (currentSpeed >= 35.0f && currentSpeed < 45.0f)
        {
            float maxSteeringAngletemp;
            if (isBraking == true)
            {
                maxSteeringAngletemp = 10.0f;
            }
            else
            {
                maxSteeringAngletemp = 8.0f;
            }
            maxSteeringAngle = maxSteeringAngletemp;
            targetSteeringAngle = (-currentLeanAngle / maxLeanAngle) * maxSteeringAngletemp;
            currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * 0.1f);
            frontWheel.steerAngle = currentSteeringAngle;
        }
        if (currentSpeed >= 45.0f && currentSpeed < 60.0f)
        {
            float maxSteeringAngletemp;
            if (isBraking == true)
            {
                maxSteeringAngletemp = 8.0f;
            }
            else
            {
                maxSteeringAngletemp = 6.5f;
            }
            maxSteeringAngle = maxSteeringAngletemp;
            targetSteeringAngle = (-currentLeanAngle / maxLeanAngle) * maxSteeringAngletemp;
            currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * 0.1f);
            frontWheel.steerAngle = currentSteeringAngle;
        }
        if (currentSpeed >= 60.0f && currentSpeed < 80.0f)
        {
            float maxSteeringAngletemp = 2.0f;
            maxSteeringAngle = maxSteeringAngletemp;
            targetSteeringAngle = (-currentLeanAngle / maxLeanAngle) * maxSteeringAngletemp;
            currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * 0.1f);
            frontWheel.steerAngle = currentSteeringAngle;
        }



        // Calculate the target steering angle based on the current lean angle
        targetSteeringAngle = (-currentLeanAngle / maxLeanAngle) * maxSteeringAngle;

        // Smoothly interpolate the current steering angle towards the target steering angle
        currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * 0.1f);

        // Apply the steering angle to the front wheel
        frontWheel.steerAngle = currentSteeringAngle;
        //frontWheel.steerAngle = 0.0f;



    }
    public void UpdateHandles()
    {
        if (useHandles == true)
        {
            if (currentSpeed <= 15.0f)
            {
                float handleModifiertemp = 1.0f;
                handle.localEulerAngles = new Vector3(handle.localEulerAngles.x * handleModifiertemp * handleModifier, currentSteeringAngle * handleModifier, handle.localEulerAngles.z * handleModifier);
            }
            if (currentSpeed > 15.0f)
            {
                float handleModifiertemp = Mathf.Lerp(0.0f, 1.0f, 0.5f);
                handle.localEulerAngles = new Vector3(handle.localEulerAngles.x * handleModifiertemp * handleModifier, currentSteeringAngle * handleModifier, handle.localEulerAngles.z * handleModifier);
            }
        }
    }
    public void updateSuspension()
    {
        float Y = frontWheelTransform.transform.localPosition.y;
        /*
        float Y_min = 0.258f;
        float Y_max = 0.357f;
        */
        float Y_min = -0.731f;
        float Y_max = -0.633f;
        // Normalize Y to range [0.0, 1.0]
        float Y_normalized = (Y - Y_min) / (Y_max - Y_min);

        susDiffDistance = Y_normalized;
        float suspensionMovingYDiff = Mathf.Lerp(-0.616f, -0.516f, susDiffDistance);
        //suspensionMoving.transform.localPosition = new Vector3(0.0f,suspensionMovingYDiff, -0.027f );
        float suspensionMovingZDiff = Mathf.Lerp(0.308f, 0.263f, susDiffDistance);
        float wheelColliderZDiff = Mathf.Lerp(0.335f, 0.290f, susDiffDistance);
        if (MoveSuspension == true)
        {
            suspensionMoving.transform.localPosition = new Vector3(0.0f, suspensionMovingYDiff, suspensionMovingZDiff); // Move Suspension Mesh on Y and Z axis
        }
        if (shiftCollider == true)
        {
            frontWheel.transform.localPosition = new Vector3(0.0f, -0.682f, wheelColliderZDiff); // Move Wheel Collider on Z axis    
            frontWheelTransform.position = new Vector3(handle.position.x, frontWheel.transform.position.y, wheelColliderZDiff);
        }

        float wheelMeshRotVal = currentSteeringAngle * handleModifier;
        if (rotateWheel == true)
        {
            frontWheelTransform.localEulerAngles = new Vector3(handle.localEulerAngles.x, handle.localEulerAngles.y, frontWheel.transform.rotation.z);
            frontWheel.transform.localEulerAngles = new Vector3(frontWheel.transform.rotation.x, 0.0f, frontWheel.transform.rotation.z);
        }



    }
    void LeanOnTurn()
    {
        Vector3 currentRot = transform.rotation.eulerAngles;
        if (currentSpeed < 5.0f)
        {
            float LeanSpeedtemp = 0.1f;
            float maxLeanAngleTemp = 25.0f;
            targetLeanAngle = maxLeanAngleTemp * -horizontalInput;
            currentLeanAngle = Mathf.LerpAngle(currentLeanAngle, targetLeanAngle, LeanSpeedtemp * 0.1f);
            if (rotateBike == true)
            {
                transform.rotation = Quaternion.Euler(currentRot.x, currentRot.y, currentLeanAngle);
            }

        }
        if (currentSpeed >= 5.0f && currentSpeed < 15.0f)
        {
            float LeanSpeedtemp = 0.1f;
            float maxLeanAngleTemp = Mathf.LerpAngle(25.0f, maxLeanAngle, currentSpeed / 15.0f);
            targetLeanAngle = maxLeanAngleTemp * -horizontalInput;
            currentLeanAngle = Mathf.LerpAngle(currentLeanAngle, targetLeanAngle, LeanSpeedtemp * 0.1f);
            if (rotateBike == true)
            {
                transform.rotation = Quaternion.Euler(currentRot.x, currentRot.y, currentLeanAngle);
            }

        }

        if (currentSpeed >= 15.0f)
        {

            targetLeanAngle = maxLeanAngle * -horizontalInput;
            currentLeanAngle = Mathf.LerpAngle(currentLeanAngle, targetLeanAngle, LeanSpeed * 0.1f);
            if (rotateBike == true) { transform.rotation = Quaternion.Euler(currentRot.x, currentRot.y, currentLeanAngle); }
        }
        currentLean = Mathf.Abs(currentLeanAngle);
        LeanRatio = -currentLeanAngle / maxLeanAngle;
    }
    void MoveCOG()
    {
        COGShiftValue = Mathf.Lerp(0.0f, COGShiftTarget, currentLean / maxLeanAngle);

        float shiftValue = Mathf.Lerp(-COGShiftValue / COGShiftTarget, COGShiftValue / COGShiftTarget, (LeanRatio + 1) / 2);
        if (isBraking == true)
        {
            rb.centerOfMass = new Vector3(shiftValue * COGShiftTarget * 1.3f, COG_Offset.y + shiftValue * COGShiftTarget * 0.3f, COG_Offset.z);
        }
        else
        {
            rb.centerOfMass = new Vector3(shiftValue * COGShiftTarget, COG_Offset.y, COG_Offset.z);
        }

    }
    public void UpdateWheels()
    {
        //UpdateSingleWheel(frontWheel, frontWheelTransform);
        //Vector3 position;
        //Quaternion rotation;
        //frontWheel.GetWorldPose(out position, out rotation);
        //handle.localEulerAngles = new Vector3(handle.localEulerAngles.x * handleModifiertemp * handleModifier, currentSteeringAngle * handleModifier, handle.localEulerAngles.z * handleModifier);
        /*
        float wheelMeshRotVal = currentSteeringAngle * handleModifier;
        frontWheelTransform.localEulerAngles = new Vector3(frontWheel.transform.rotation.x, wheelMeshRotVal, frontWheel.transform.rotation.z);
        frontWheelTransform.localPosition = frontWheel.transform.position;
        */



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
            WheelFrictionCurve frontWheelfcFF;
            WheelFrictionCurve frontWheelfcSF;

            WheelFrictionCurve backWheelfcFF;
            WheelFrictionCurve backWheelfcSF;
            frontWheelfcFF = frontWheel.forwardFriction;
            frontWheelfcSF = frontWheel.sidewaysFriction;

            backWheelfcFF = backWheel.forwardFriction;
            backWheelfcSF = backWheel.sidewaysFriction;
            // Friction Changes
            if (isAccelerating == true) // is Accelerating
            {
                if (currentSpeed > 0 && currentSpeed <= 10) // 0-10 FwF Modifier 
                {
                    frontWheelfcFF.stiffness = 2.5f;
                    frontWheelfcSF.stiffness = 3.35f;
                    backWheelfcFF.stiffness = 2.5f;
                    backWheelfcSF.stiffness = 15.0f;
                    frontWheel.forwardFriction = frontWheelfcFF;
                    frontWheel.sidewaysFriction = frontWheelfcSF;
                    backWheel.forwardFriction = backWheelfcFF;
                    backWheel.sidewaysFriction = backWheelfcSF;
                }
                else if (currentSpeed > 10 && currentSpeed < 30) // 10-30 FwF modifier
                {
                    frontWheelfcFF.stiffness = 2.5f;
                    frontWheelfcSF.stiffness = 3.65f;
                    backWheelfcFF.stiffness = 2.9f;
                    backWheelfcSF.stiffness = 25.0f;
                    frontWheel.forwardFriction = frontWheelfcFF;
                    frontWheel.sidewaysFriction = frontWheelfcSF;
                    backWheel.forwardFriction = backWheelfcFF;
                    backWheel.sidewaysFriction = backWheelfcSF;
                }
                else if (currentSpeed > 30 && currentSpeed < 50) // 30-50 FwF modifier
                {
                    frontWheelfcFF.stiffness = 2.65f;
                    frontWheelfcSF.stiffness = 4.8f;
                    backWheelfcFF.stiffness = 4.0f;
                    backWheelfcSF.stiffness = 38.0f;
                    frontWheel.forwardFriction = frontWheelfcFF;
                    frontWheel.sidewaysFriction = frontWheelfcSF;
                    backWheel.forwardFriction = backWheelfcFF;
                    backWheel.sidewaysFriction = backWheelfcSF;
                }
                else if (currentSpeed > 50 && currentSpeed < 70) // 30-50 FwF modifier
                {
                    frontWheelfcFF.stiffness = 3.2f;
                    frontWheelfcSF.stiffness = 5.5f;
                    backWheelfcFF.stiffness = 4.7f;
                    backWheelfcSF.stiffness = 45.0f;
                    frontWheel.forwardFriction = frontWheelfcFF;
                    frontWheel.sidewaysFriction = frontWheelfcSF;
                    backWheel.forwardFriction = backWheelfcFF;
                    backWheel.sidewaysFriction = backWheelfcSF;
                }
                else if (currentSpeed > 70 && currentSpeed < 90) // 70-90 FwF modifier
                {
                    frontWheelfcFF.stiffness = 4.5f;
                    frontWheelfcSF.stiffness = 6.5f;
                    backWheelfcFF.stiffness = 5.5f;
                    backWheelfcSF.stiffness = 60.0f;
                    frontWheel.forwardFriction = frontWheelfcFF;
                    frontWheel.sidewaysFriction = frontWheelfcSF;
                    backWheel.forwardFriction = backWheelfcFF;
                    backWheel.sidewaysFriction = backWheelfcSF;
                }
                else
                {
                    frontWheelfcFF.stiffness = 3.1f;
                    frontWheelfcSF.stiffness = 4.5f;
                    backWheelfcFF.stiffness = 4.0f;
                    backWheelfcSF.stiffness = 35.0f;
                    frontWheel.forwardFriction = frontWheelfcFF;
                    frontWheel.sidewaysFriction = frontWheelfcSF;
                    backWheel.forwardFriction = backWheelfcFF;
                    backWheel.sidewaysFriction = backWheelfcSF;
                }
            }
            else //Not Accelerating
            {
                if (currentSpeed > 0 && currentSpeed <= 10) // 0-10 FwF Modifier 
                {
                    frontWheelfcFF.stiffness = 4.5f;
                    frontWheelfcSF.stiffness = 6.5f;
                    backWheelfcFF.stiffness = 3.0f;
                    backWheelfcSF.stiffness = 10.0f;
                    frontWheel.forwardFriction = frontWheelfcFF;
                    frontWheel.sidewaysFriction = frontWheelfcSF;
                    backWheel.forwardFriction = backWheelfcFF;
                    backWheel.sidewaysFriction = backWheelfcSF;
                }
                else if (currentSpeed > 10 && currentSpeed < 30) // 10-30 FwF modifier
                {
                    frontWheelfcFF.stiffness = 6.2f;
                    frontWheelfcSF.stiffness = 8.5f;
                    backWheelfcFF.stiffness = 3.3f;
                    backWheelfcSF.stiffness = 18.0f;
                    frontWheel.forwardFriction = frontWheelfcFF;
                    frontWheel.sidewaysFriction = frontWheelfcSF;
                    backWheel.forwardFriction = backWheelfcFF;
                    backWheel.sidewaysFriction = backWheelfcSF;
                }
                else if (currentSpeed > 30 && currentSpeed < 50) // 30-50 FwF modifier
                {
                    frontWheelfcFF.stiffness = 6.8f;
                    frontWheelfcSF.stiffness = 10.0f;
                    backWheelfcFF.stiffness = 3.4f;
                    backWheelfcSF.stiffness = 25.0f;
                    frontWheel.forwardFriction = frontWheelfcFF;
                    frontWheel.sidewaysFriction = frontWheelfcSF;
                    backWheel.forwardFriction = backWheelfcFF;
                    backWheel.sidewaysFriction = backWheelfcSF;
                }
                else if (currentSpeed > 50 && currentSpeed < 70) // 30-50 FwF modifier
                {
                    frontWheelfcFF.stiffness = 8.4f;
                    frontWheelfcSF.stiffness = 13.2f;
                    backWheelfcFF.stiffness = 4.2f;
                    backWheelfcSF.stiffness = 43.0f;
                    frontWheel.forwardFriction = frontWheelfcFF;
                    frontWheel.sidewaysFriction = frontWheelfcSF;
                    backWheel.forwardFriction = backWheelfcFF;
                    backWheel.sidewaysFriction = backWheelfcSF;
                }
                else if (currentSpeed > 70 && currentSpeed < 90) // 70-90 FwF modifier
                {
                    frontWheelfcFF.stiffness = 10.1f;
                    frontWheelfcSF.stiffness = 20.0f;
                    backWheelfcFF.stiffness = 5.5f;
                    backWheelfcSF.stiffness = 48.0f;
                    frontWheel.forwardFriction = frontWheelfcFF;
                    frontWheel.sidewaysFriction = frontWheelfcSF;
                    backWheel.forwardFriction = backWheelfcFF;
                    backWheel.sidewaysFriction = backWheelfcSF;
                }
                else
                {
                    frontWheelfcFF.stiffness = 7.5f;
                    frontWheelfcSF.stiffness = 10.2f;
                    backWheelfcFF.stiffness = 3.5f;
                    backWheelfcSF.stiffness = 35.0f;
                    frontWheel.forwardFriction = frontWheelfcFF;
                    frontWheel.sidewaysFriction = frontWheelfcSF;
                    backWheel.forwardFriction = backWheelfcFF;
                    backWheel.sidewaysFriction = backWheelfcSF;
                }
            }
        }
    }
    void UpdateNeedle()
    {
        ClusterdesiredPosition = ClusterstartPosition - ClusterendPosition;
        float temp = currentRPM / maxRPM;
        ClusterNeedle.transform.localEulerAngles = new Vector3(ClusterNeedle.transform.localEulerAngles.x, ClusterNeedle.transform.localEulerAngles.y, ClusterstartPosition - temp * ClusterdesiredPosition);
    }
    void CheckSlip()
    {
        frontWheel.GetGroundHit(out WheelHit frontInfo);
        backWheel.GetGroundHit(out WheelHit backInfo);

        float frontWheelForwardSlipTemp = frontInfo.forwardSlip;
        frontWheelForwardSlip = frontWheelForwardSlipTemp;
        float frontWheelSidewaysSlipTemp = frontInfo.sidewaysSlip;
        frontWheelSidewaysSlip = frontWheelSidewaysSlipTemp;
        float backWheelForwardSlipTemp = backInfo.forwardSlip;
        backWheelForwardSlip = backWheelForwardSlipTemp;
        float backWheelSidewaysSlipTemp = backInfo.sidewaysSlip;
        backWheelSidewaysSlip = backWheelSidewaysSlipTemp;
        frontWheelForwardSlip = Mathf.Round(frontWheelForwardSlipTemp);
        //backCoefficient = (backInfo.sidewaysSlip / backWheel.sidewaysFriction.extremumSlip);
        //frontCoefficient = (frontInfo.sidewaysSlip / frontWheel.sidewaysFriction.extremumSlip);
    }
}

