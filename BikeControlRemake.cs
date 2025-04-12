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
using JetBrains.Annotations;
using Unity.Cinemachine;
using UnityEngine.Events;
using UnityEngine.ProBuilder.Shapes;
using UnityEngine.Rendering.PostProcessing;

//using System.Numerics;



public enum GearState { Neutral, Running, CheckingChange, Changing };
public enum GearBoxType { Manual, Automatic };

public enum CameraMode { FirstPerson, ThirdPerson };

public class BikeControlRemake : MonoBehaviour
{
    /*
        public float balanceStrength = 1.0f; // Strength of automatic balancing
        public float leanStrength = 1.0f; // Strength of leaning input
        //public float maxLeanAngle = 45f; // Maximum lean angle
        public float dampingFactor = 5f; // Smoothens correction
    */
    public bool validRotation = false;
    public float MaxWheelieDist = 0.5f;
    public GameObject WaterInteractionObject;
    public GameObject BodyCollisionColliderObject;
    public GameObject WheelieColliderObject;
    SphereCollider wheelieSphereCollider;
    public bool BodyCollided = false;
    public bool frontWheelHit;
    public bool backWheelHit;
    public bool EnableBouyancyScript;
    public bool TouchingWater;
    public float balanceKp = 12f;
    public float balanceKpModified;
    public float balanceKpSpeedMultiplier;
    public float balanceKi = 0.1f;
    public float balanceKiLeanMuliplier;
    public float balanceKd = 5f;
    public float balanceKdLeanMultiplier;
    public float steerSensitivity = 2f;
    public float maxSteerAngle = 30f;
    public float currentMaxSteeringAngle;
    public float currentError;

    public bool balanceKpModify;

    private float targetLeanAnglePID = 0f;
    private float balanceIntegral = 0f;
    private float lastError = 0f;


    public float rightSideDistance;
    public float leftSideDistance;
    public GameObject leftSideDistanceRayObject;
    public GameObject rightSideDistanceRayObject;
    [SerializeField] public UnityEvent TurnONHeadlights;
    [SerializeField] public UnityEvent TurnOFFHeadlights;
    PlayerControls controls;
    public Material emissiveMaterial;
    public Renderer objectToChange;

    public CinemachineCamera cinemachineCamera;
    public CinemachineOrbitalFollow orbitalFollow;
    public CinemachineBasicMultiChannelPerlin cameraShake;
    float throttleVal;
    float keyboardThrottleVal;
    float ReverseVal;
    float brakeVal;
    float keyboardBrakeVal;
    float wheelieVal;
    private float currentFrontBrakeTorque;
    private float currentRearBrakeTorque;
    public float leftStickXAxis;
    public Vector2 rightStick;
    public bool gearUp;
    public bool gearDown;
    public CameraMode cameraMode;
    public Vector3 camOffset;
    public float camXAxis;
    public float camYAxis;
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

        controls.Gameplay.KeyboardThrottle.performed += ctx => keyboardThrottleVal = ctx.ReadValue<float>();
        controls.Gameplay.KeyboardThrottle.canceled += ctx => keyboardThrottleVal = 0.0f;

        controls.Gameplay.Brake.performed += ctx => brakeVal = ctx.ReadValue<float>();
        controls.Gameplay.Brake.canceled += ctx => brakeVal = 0.0f;

        controls.Gameplay.KeyboardBrake.performed += ctx => keyboardBrakeVal = ctx.ReadValue<float>();
        controls.Gameplay.KeyboardBrake.canceled += ctx => keyboardBrakeVal = 0.0f;

        controls.Gameplay.Reverse.performed += ctx => ReverseVal = ctx.ReadValue<float>();
        controls.Gameplay.Reverse.canceled += ctx => ReverseVal = 0.0f;

        controls.Gameplay.Lean.performed += ctx => leftStickXAxis = ctx.ReadValue<float>();
        controls.Gameplay.Lean.canceled += ctx => leftStickXAxis = 0.0f;

        //controls.Gameplay.Camera.performed += ctx => rightStick = ctx.ReadValue<Vector2>();
        //controls.Gameplay.Camera.canceled += ctx => rightStick = new Vector2(0.0f, 0.0f);

        controls.Gameplay.GearUp.performed += ctx => GearShiftUp();
        controls.Gameplay.GearDown.performed += ctx => GearShiftDown();

        controls.Gameplay.WheelieInput.performed += ctx => wheelieVal = ctx.ReadValue<float>();
        controls.Gameplay.WheelieInput.canceled += ctx => wheelieVal = 0.0f;
    }
    void OnEnable()
    {
        controls.Gameplay.Enable();
    }
    void OnDisable()
    {
        controls.Gameplay.Disable();
    }
    public Material FullscreenMotionBlurMaterial;
    public GameObject ThirdPersonCameraObject;
    public GameObject FirstPersonCameraObject;
    CinemachineBasicMultiChannelPerlin FPPCamNoiseObject;
    CinemachineCamera FPPCameraCinemachineCamera;
    //public float ThrottleValue;
    public float targetSteeringAngle;
    //public float backCoefficient;
    //public float frontCoefficient;
    public float frontWheelForwardSlip;
    public float frontWheelSidewaysSlip;
    public float backWheelForwardSlip;
    public float backWheelSidewaysSlip;
    private bool isHandling;
    float frontWheelfcSFOriginalStiffness; //opt
    public float LeanRatio; //Imp
    float currentMotorTorque; //Opt
    public float horizontalInput; //Imp
    public float verticalInput; //Imp
    float verticalInputUp; //Imp
    float verticalInputDown; //Imp
    float horizontalInputLeft;
    float horizontalInputRight;
    public float COGShiftTarget; //Imp - Inspector
    public float wheelieShiftTarget; // Imp - Inspector
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
    public bool useController;
    public bool HandleSteeringON;
    public bool HandleCOGMovement;
    public bool useHandles;
    public bool moveSuspension;
    public float maxSteeringSpeed = 17.0f;

    [Header("Power/Braking")]
    [Space(5)]
    public float MaxSpeed = 88.0f;
    public float motorForce;
    public float brakeForce;
    public float brakeModifier;
    public Vector3 COG_Offset;
    public bool modifyFrictionON = false;

    [Space(20)]
    [HeaderAttribute("Info")]
    public float currentSteeringAngle;
    [Tooltip("Dynamic steering angle based on the speed of the RB, affected by sterReductorAmmount")]
    public float current_maxSteeringAngle;
    [Tooltip("The current lean angle applied")]
    [Range(-45, 45)] public float currentLean;
    public float currentLeanAngle;
    [Space(20)]
    [HeaderAttribute("Speed")]
    public float currentSpeed;
    public float currentKMHSpeed;
    public float currentEnginePower;
    public float currentBrakePowerF;
    public float currentBrakePowerB;


    [Space(20)]
    [Header("Steering")]
    [Space(5)]

    public float maxSteeringAngle;
    public float LeanSpeed = 0.2f;

    [Range(0f, 1f)] public float steerReductorAmmount;

    [Range(0.001f, 1f)] public float turnSmoothing;

    [Space(20)]
    [Header("Lean")]
    [Space(5)]

    public float maxLeanAngle = 45f;

    [Range(0.001f, 1f)] public float leanSmoothing;
    float targetLeanAngle;

    [Space(20)]
    [Header("Object References")]
    public Transform handle;
    public Transform suspensionMoving;
    //public Transform TripleClamp;
    //public Transform suspensionFixed;
    [Space(10)]
    public WheelCollider frontWheel;
    public WheelCollider backWheel;
    [Space(10)]
    public Transform frontWheelTransform;
    public Transform backWheelTransform;
    //public GameObject susTop;
    //public GameObject susBottom;
    public GameObject ClusterNeedle;
    //public GameObject TailLight;
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
    public float[] gearRatios;
    public float[] gearMaxSpeed;
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
    private Vector3 previousVelocity;


    void GearShiftUp()
    {
        if (currentGearSelect < 6)
        {
            currentGearSelect += 1;
        }
    }
    void GearShiftDown()
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

    // Start is called before the first frame update
    void Start()
    {

        frontWheel.ConfigureVehicleSubsteps(2, 5, 200);
        backWheel.ConfigureVehicleSubsteps(2, 5, 200);
        //WheelCollider backWheelColliderObject = backWheel.GetComponent<WheelCollider>();
        //JointSpring backWheelSpringObject = backWheel.suspensionSpring;
        rb = GetComponent<Rigidbody>();
        SphereCollider waterCheckRigidBody = WaterInteractionObject.GetComponent<SphereCollider>();
        //CapsuleCollider BodyCollisionCollider = BodyCollisionColliderObject.GetComponent<CapsuleCollider>();
        wheelieSphereCollider = WheelieColliderObject.GetComponent<SphereCollider>();
        //BodyCollided = BodyCollisionColliderObject.GetComponent<CapsuleCollisionDetection>().isColliding;

        emissiveMaterial = objectToChange.GetComponent<Renderer>().material;

        orbitalFollow = cinemachineCamera.GetComponent<CinemachineOrbitalFollow>();
        FPPCamNoiseObject = cameraShake.GetComponent<CinemachineBasicMultiChannelPerlin>();
        FPPCameraCinemachineCamera = FirstPersonCameraObject.GetComponent<CinemachineCamera>();
        originalPos = transform.position;
        OriginalRot = transform.rotation.eulerAngles;
        originalCOG = rb.centerOfMass;
        //rb.centerOfMass = COG_Offset;
        clusterCheck = true;
        gearBoxType = GearBoxType.Manual;
        //cameraMode = CameraMode.FirstPerson;
    }


    void Update()
    {
        GetInput();
        //HandleEngineA();
        HandleElectronics();
        updateSuspension();
        //ABSSystem();
        CheckCollisions();
        UpdateCluster();
        //CheckGroundDistanceRaycasts();
        MotionBlur();
    }
    void LateUpdate()
    {
        //CheckWater();


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
        HandleEngineState();
        Speed_O_Meter();
        //UpdateNeedle();
        //UpdateCluster();

        UpdateWheels();


        /*
        HandlePIDController();
        BalanceBike();
        ApplyCounterSteering();
        */

        LeanOnTurn();
        CheckRotationValidity();
        HandleSteering();


        if (HandleCOGMovement)
        {
            if (isAccelerating == true)
            {
                MoveCOG();
                //UpdateHandles();
            }
            if (isBraking == true)
            {
                //Dont move COG
                //MoveCOG();
            }
            if (isAccelerating == false || isBraking == false)
            {
                MoveCOG();
                //UpdateHandles();
            }
        }

        //MoveCOG();

        //HandleSteering();
        ModifyFriction();
        UpdateHandles();
        HandleCamera();

        if (currentGear != 0)
        {
            previousGearSpeed = gearMaxSpeed[currentGear - 1];
        }
        if (currentGear < 5)
        {
            nextGearSpeed = gearMaxSpeed[currentGear + 1];
        }
        else
        {
            nextGearSpeed = 0.0f;
        }
        currentGearSpeed = gearMaxSpeed[currentGear];
        CheckWheelHit();
        //CheckCollisions();
        //CheckWater();
        //updateSuspension();
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

    void GetInput()
    {
        //float rawHorizontalInput = Input.GetAxis("Horizontal");
        //horizontalInput = Mathf.Lerp(horizontalInput, rawHorizontalInput, 10*Time.deltaTime);


        //verticalInput = Input.GetAxis("Vertical");
        if (useController)
        {
            verticalInputUp = throttleVal;
            verticalInputDown = -brakeVal;
            float horizontalInputTemp = leftStickXAxis;

            float smoothCurrentVelocity = 0.0f;
            float speedHorizontalSmoothTime = Mathf.Lerp(0.02f, 0.06f, (currentSpeed / MaxSpeed));
            horizontalInput = Mathf.SmoothDamp(horizontalInput, horizontalInputTemp, ref smoothCurrentVelocity, speedHorizontalSmoothTime);

            //horizontalInput = horizontalInputTemp;
            if (currentSpeed < 5.0f)
            {
                if (ReverseVal > 0.01f)
                {



                    frontWheel.brakeTorque = 0.0f;
                    backWheel.brakeTorque = 0.0f;
                    frontWheel.motorTorque = -80.0f;
                }
                else
                {
                    frontWheel.motorTorque = 0.0f;
                }
            }

        }
        else
        {
            verticalInputUp = keyboardThrottleVal;
            verticalInputDown = -keyboardBrakeVal;
            float horizontalInputTemp = Input.GetAxis("Horizontal");
            float smoothCurrentVelocity = 0.0f;
            float speedHorizontalSmoothTime = Mathf.Lerp(0.045f, 0.065f, currentSpeed / MaxSpeed);
            if (horizontalInputTemp <= 0.15f || horizontalInputTemp >= 0.15f)
            {
                horizontalInput = horizontalInputTemp;
            }
            else
            {
                horizontalInput = Mathf.SmoothDamp(horizontalInput, horizontalInputTemp, ref smoothCurrentVelocity, speedHorizontalSmoothTime);
            }


            /*
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
            */

            //verticalInputUp = Mathf.Clamp(verticalInput, 0.0f, 1.0f);
            //verticalInputDown = Mathf.Clamp(verticalInput, -1.0f, 0.0f);
        }
        if (Input.GetKey("l"))
        {
            //rb.AddRelativeTorque(0.0f, 0.0f, 5.0f);
            //rb.AddTorque(Vector3.forward * 10.0f);
            rb.AddTorque(rb.transform.forward * 1000.0f);
        }
        if (Input.GetKey("j"))
        {
            //rb.AddRelativeTorque(0.0f, 0.0f, -5.0f);
            rb.AddTorque(rb.transform.forward * -1000.0f);
            //rb.AddTorque(Vector3.forward * -10.0f);
        }

        if (Input.GetKey("r"))
        {
            //transform.position = originalPos;
            rb.MovePosition(originalPos);
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
        if (Input.GetKeyDown("v")) // TOGGLE CAMERA MODE
        {
            if (cameraMode == CameraMode.FirstPerson)
            {
                cameraMode = CameraMode.ThirdPerson;
            }
            else
            {
                cameraMode = CameraMode.FirstPerson;
            }
        }
        if (Input.GetKeyDown("o")) //CONTROLLER TOGGLE
        {
            if (useController == false)
            {
                useController = true;
            }
            else
            {
                useController = false;
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

        if (Input.GetKeyDown("f")) //IGNITION TOGGLE
        {
            if (isLightON == false)
            {
                isLightON = true;
            }
            else
            {
                isLightON = false;
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
    void turnOnHeadlights()
    {
        FrontLeftLightReflect.SetActive(true);
        FrontRightLightReflect.SetActive(true);
        FrontLeftLightSpot.SetActive(true);
        FrontRightLightSpot.SetActive(true);
    }
    void turnOffHeadlights()
    {
        FrontLeftLightReflect.SetActive(false);
        FrontRightLightReflect.SetActive(false);
        FrontLeftLightSpot.SetActive(false);
        FrontRightLightSpot.SetActive(false);
    }
    void HandleElectronics()
    {
        if (verticalInputDown < 0.0f)
        {
            turnOnTailLight();
        }
        else
        {
            turnOffTailLight();
        }
        if (isLightON == true)
        {
            /*
            FrontLeftLightReflect.SetActive(true);
            FrontRightLightReflect.SetActive(true);
            FrontLeftLightSpot.SetActive(true);
            FrontRightLightSpot.SetActive(true);
            */
            TurnONHeadlights.Invoke();
        }
        if (isLightON == false)
        {
            TurnOFFHeadlights.Invoke();
            /*
            FrontLeftLightReflect.SetActive(false);
            FrontRightLightReflect.SetActive(false);
            FrontLeftLightSpot.SetActive(false);
            FrontRightLightSpot.SetActive(false);
            */
        }


    }

    void ApplyMotor()
    {
        currentTorque = CalculateTorque();
        //frontWheel.motorTorque = 0.0001f;
        backWheel.motorTorque = currentTorque * verticalInputUp;
        currentEnginePower = backWheel.motorTorque;
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
                    StartCoroutine(AdjustRPM(currentRPM, maxRPM, 0.65f));
                }
                if (clusterCheck == true && currentRPM == maxRPM)
                {
                    StartCoroutine(AdjustRPM(currentRPM, 0.0f, 0.65f));
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
                StartCoroutine(SmoothIncreaseRPM(0.0f, idleRPM, 0.6f));
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
                StartCoroutine(SmoothDecreaseRPM(currentRPM, 0.0f, 0.65f));
            }
            engineRunning = false;
            startEngine = false;
            clusterCheck = true;
            ClusterSpeedoReadout.SetActive(false);
            //StopCoroutine("EngineCheck");
            //StopAllCoroutines();
            StartCoroutine(SmoothDecreaseRPM(currentRPM, 0.0f, 0.65f));
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
        if (verticalInputUp > 0.0f)
        {
            isBraking = false;
            isAccelerating = true;
        }
        if (verticalInputDown < 0.0f)
        {
            isBraking = true;
            isAccelerating = false;
        }
        if (verticalInputUp == 0.0f || verticalInputDown == 0.0f)
        {
            isAccelerating = false;
            isBraking = false;
        }
    }
    void ApplyBrake()
    {
        if (isBraking == true)
        {
            backWheel.motorTorque = 0.0f;
        }
        else
        {
            backWheel.motorTorque = currentTorque * verticalInputUp;
        }

        frontWheel.brakeTorque = Mathf.Abs(verticalInputDown * brakeForce) * brakeModifier * Mathf.Lerp(0.5f, 0.85f, currentSpeed / MaxSpeed);
        //frontWheel.brakeTorque = currentFrontBrakeTorque * Mathf.Abs(verticalInputDown);
        backWheel.brakeTorque = Mathf.Abs(verticalInputDown * brakeForce) * 0.6f;

        currentBrakePowerF = frontWheel.brakeTorque;
        currentBrakePowerB = backWheel.brakeTorque;
    }
    void ABSSystem()
    {
        if (verticalInputDown < -0.01f)
        {
            if (frontWheelSidewaysSlip > 0.01f && frontWheelSidewaysSlip <= 1.0f || frontWheelSidewaysSlip < -0.01f && frontWheelSidewaysSlip >= -1.0f)
            {
                if (brakeModifier > 0.4)
                {
                    brakeModifier -= 0.05f;
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
        float frontWheelRPM = frontWheel.rpm;
        float backWheelRPM = backWheel.rpm;
        /*
        if (frontWheelRPM < 20.0f)
        {
            currentFrontBrakeTorque = brakeForce * 0.1f;
        }
        else
        {
            currentFrontBrakeTorque = brakeForce;
        }
        */


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
    /*
    public void HandleSteering()
    {
        if (currentSpeed <= 5.0f)
        {
            float maxSteeringAngletemp = 37.0f;
            maxSteeringAngle = maxSteeringAngletemp;
            targetSteeringAngle = horizontalInput * maxSteeringAngletemp;
            //targetSteeringAngle = maxSteeringAngletemp;
            currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * 0.1f);
            frontWheel.steerAngle = currentSteeringAngle;
        }
        if (currentSpeed >= 5.0f && currentSpeed < 15.0f)
        {
            float maxSteeringAngletemp = 30.0f;
            //maxSteeringAngle = maxSteeringAngletemp;
            targetSteeringAngle = (((-currentLeanAngle / maxLeanAngle) + horizontalInput) / 2.0f) * maxSteeringAngletemp;
            currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * 0.1f);
            frontWheel.steerAngle = currentSteeringAngle;
        }
        if (currentSpeed >= 15.0f && currentSpeed < 35.0f)
        {
            float maxSteeringAngletemp;
            if (isBraking == true)
            {
                maxSteeringAngletemp = 17.0f;
            }
            else
            {
                maxSteeringAngletemp = 14.0f;
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
                maxSteeringAngletemp = 14.0f;
            }
            else
            {
                maxSteeringAngletemp = 12.0f;
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
                maxSteeringAngletemp = 12.0f;
            }
            else
            {
                maxSteeringAngletemp = 10f;
            }
            maxSteeringAngle = maxSteeringAngletemp;
            targetSteeringAngle = (-currentLeanAngle / maxLeanAngle) * maxSteeringAngletemp;
            currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * 0.1f);
            frontWheel.steerAngle = currentSteeringAngle;
        }
        if (currentSpeed >= 60.0f && currentSpeed < 80.0f)
        {
            float maxSteeringAngletemp = 6.0f;
            maxSteeringAngle = maxSteeringAngletemp;
            targetSteeringAngle = (-currentLeanAngle / maxLeanAngle) * maxSteeringAngletemp;
            currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * 0.1f);
            frontWheel.steerAngle = currentSteeringAngle;
        }
        // Calculate the target steering angle based on the current lean angle
        targetSteeringAngle = (-currentLeanAngle / maxLeanAngle) * maxSteeringAngle;
        // Smoothly interpolate the current steering angle towards the target steering angle
        currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * 0.1f);
        frontWheel.steerAngle = currentSteeringAngle;
    }
    */
    public void HandleSteering()
    {
        if (currentSpeed <= 5.0f)
        {
            float t = (currentSpeed - 0.0f) / (5.0f - 0.0f);
            float maxSteeringAngletemp = Mathf.Lerp(31.0f, 26.0f, currentSpeed / 5.0f);
            //float maxSteeringAngletemp = Mathf.Lerp(27.0f, 24.0f, t);

            maxSteeringAngle = maxSteeringAngletemp;
            targetSteeringAngle = horizontalInput * maxSteeringAngletemp;
            //targetSteeringAngle = maxSteeringAngletemp;
            currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * 0.1f);
            frontWheel.steerAngle = currentSteeringAngle;
        }
        if (currentSpeed > 5.0f && currentSpeed <= 15.0f)
        {
            float t = (currentSpeed - 5.0f) / (15.0f - 5.0f);
            float maxSteeringAngletemp = Mathf.Lerp(26.0f, 14.5f, currentSpeed / 15.0f);
            //float maxSteeringAngletemp = Mathf.Lerp(24.0f, 20.0f, t);

            //maxSteeringAngle = maxSteeringAngletemp;
            //targetSteeringAngle = horizontalInput * maxSteeringAngletemp;
            //targetSteeringAngle = (-currentLeanAngle / 20.0f) * maxSteeringAngletemp;
            targetSteeringAngle = (((-currentLeanAngle / 20.0f) + horizontalInput) / 2.0f) * maxSteeringAngletemp;

            currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * 0.1f);
            frontWheel.steerAngle = currentSteeringAngle;
        }
        //-------------------------------------------------------------------------------------------------------------

        if (currentSpeed > 15.0f && currentSpeed <= 35.0f)
        {
            float maxSteeringAngletemp;

            float t = (currentSpeed - 15.0f) / (35.0f - 15.0f);
            maxSteeringAngletemp = Mathf.Lerp(15.5f, 9.5f, currentSpeed / 35.0f);
            //maxSteeringAngletemp = Mathf.Lerp(20.0f, 14f, t);



            maxSteeringAngle = maxSteeringAngletemp;
            //targetSteeringAngle = (-currentLeanAngle / maxLeanAngle) * maxSteeringAngletemp;
            targetSteeringAngle = (((-currentLeanAngle / maxLeanAngle) + horizontalInput) / 2.0f) * maxSteeringAngletemp;
            currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * 0.1f);
            frontWheel.steerAngle = currentSteeringAngle;

        }
        if (currentSpeed > 35.0f && currentSpeed <= 45.0f)
        {
            float maxSteeringAngletemp;

            float t = (currentSpeed - 35.0f) / (45.0f - 35.0f);
            maxSteeringAngletemp = Mathf.Lerp(9.5f, 8.0f, currentSpeed / 45.0f);
            //maxSteeringAngletemp = Mathf.Lerp(14f, 8.5f, t);

            //maxSteeringAngle = maxSteeringAngletemp;
            targetSteeringAngle = (((-currentLeanAngle / maxLeanAngle) + horizontalInput) / 2.0f) * maxSteeringAngletemp;
            currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * 0.1f);
            frontWheel.steerAngle = currentSteeringAngle;
        }
        if (currentSpeed > 45.0f && currentSpeed <= 60.0f)
        {
            float maxSteeringAngletemp;
            if (isBraking == true)
            {
                float t = (currentSpeed - 45.0f) / (60.0f - 45.0f);
                maxSteeringAngletemp = Mathf.Lerp(10.0f, 7.0f, currentSpeed / 60.0f);
                //maxSteeringAngletemp = Mathf.Lerp(8.5f, 5.5f, t);
            }
            else
            {
                float t = (currentSpeed - 45.0f) / (60.0f - 45.0f);
                maxSteeringAngletemp = Mathf.Lerp(8.0f, 5.5f, currentSpeed / 60.0f);
                //maxSteeringAngletemp = Mathf.Lerp(8.5f, 5f, t);
            }
            //maxSteeringAngle = maxSteeringAngletemp;
            targetSteeringAngle = (((-currentLeanAngle / maxLeanAngle) + horizontalInput) / 2.0f) * maxSteeringAngletemp;
            currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * 0.1f);
            frontWheel.steerAngle = currentSteeringAngle;
        }
        if (currentSpeed > 60.0f && currentSpeed <= MaxSpeed)
        {
            float maxSteeringAngletemp;
            //float maxSteeringAngletemp = 3.0f;
            float t = (currentSpeed - 45.0f) / (60.0f - 45.0f);
            maxSteeringAngletemp = Mathf.Lerp(5.5f, 3.0f, currentSpeed / 60.0f);
            //maxSteeringAngletemp = Mathf.Lerp(5f, 2.5f, t);

            targetSteeringAngle = (((-currentLeanAngle / maxLeanAngle) + horizontalInput) / 2.0f) * maxSteeringAngletemp;
            currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * 0.1f);
            frontWheel.steerAngle = currentSteeringAngle;
        }

        //----------------------------------------------------------------------------------------------------------
        /*
        // Calculate the target steering angle based on the current lean angle
        targetSteeringAngle = (-currentLeanAngle / maxLeanAngle) * Mathf.Lerp(maxSteeringAngle, 5.0f, currentSpeed / MaxSpeed);
        // Smoothly interpolate the current steering angle towards the target steering angle
        currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, turnSmoothing * 0.1f);
        frontWheel.steerAngle = currentSteeringAngle;
        */

    }

    public void UpdateHandles()
    {
        if (useHandles == true)
        {

            if (currentSpeed <= 5.0f)
            {
                float handleModifiertemp = currentSteeringAngle * 0.85f;
                //float handleModifierFinalTemp = Mathf.Lerp(handleModifiertemp, 0.0f, Mathf.Abs(LeanRatio));
                float leanFactor = 1.0f - Mathf.Abs(LeanRatio); // Inverse relationship with LeanRatio
                //float handleModifierFinalTemp = Mathf.Lerp(handleModifiertemp, -handleModifiertemp, 1.0f - leanFactor);
                float handleModifierFinalTemp = handleModifiertemp * leanFactor;
                //handle.localEulerAngles = new Vector3(handle.localEulerAngles.x, currentSteeringAngle * handleModifiertemp, handle.localEulerAngles.z);
                handle.localEulerAngles = new Vector3(handle.localEulerAngles.x, handleModifierFinalTemp, handle.localEulerAngles.z);
            }
            if (currentSpeed > 5.0f && currentSpeed <= 15.0f)
            {
                float t = (currentSpeed - 5.0f) / (15.0f - 5.0f);
                //float handleModifiertemp = Mathf.Lerp(0.85f, 0.5f, currentSpeed / 15.0f);
                float handleModifiertemp = currentSteeringAngle * Mathf.Lerp(0.85f, 0.0f, t);

                //float handleModifierFinalTemp = Mathf.Lerp(handleModifiertemp, 0.0f, Mathf.Abs(LeanRatio));
                float leanFactor = 1.0f - Mathf.Abs(LeanRatio); // Inverse relationship with LeanRatio
                //float handleModifierFinalTemp = Mathf.Lerp(handleModifiertemp, -handleModifiertemp, 1.0f - leanFactor);
                float handleModifierFinalTemp = handleModifiertemp * leanFactor;
                //handle.localEulerAngles = new Vector3(handle.localEulerAngles.x, currentSteeringAngle * handleModifiertemp, handle.localEulerAngles.z);
                handle.localEulerAngles = new Vector3(handle.localEulerAngles.x, handleModifierFinalTemp, handle.localEulerAngles.z);
            }

            if (currentSpeed > 15.0f)
            {
                //float handleModifiertemp = Mathf.Lerp(0.0f, 1.0f, currentSpeed / MaxSpeed);
                float handleModifiertemp;
                if (isBraking == true)
                {
                    handleModifiertemp = 0.25f;
                    handle.localEulerAngles = new Vector3(handle.localEulerAngles.x, currentSteeringAngle * handleModifiertemp, handle.localEulerAngles.z);
                    //handle.localEulerAngles = new Vector3(handle.localEulerAngles.x * handleModifiertemp * handleModifier, currentSteeringAngle * handleModifier * 0.0f, handle.localEulerAngles.z * handleModifier);
                }
                else
                {
                    //float t = (currentSpeed - 15.0f) / (MaxSpeed - 15.0f);
                    //handleModifiertemp = Mathf.Lerp(0.5f, 0.0f, currentSpeed / MaxSpeed);
                    //handleModifiertemp = Mathf.Lerp(0.15f, 0.0f, t);

                    handleModifiertemp = 0.0f;

                    handle.localEulerAngles = new Vector3(handle.localEulerAngles.x, currentSteeringAngle * handleModifiertemp, handle.localEulerAngles.z);
                }

            }

            //handle.localRotation = Quaternion.Euler(0, currentSteeringAngle, 0);
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
            frontWheel.transform.localEulerAngles = new Vector3(frontWheel.transform.rotation.x, 0.0f, frontWheel.transform.rotation.z * 0.5f);
        }



    }
    void LeanOnTurn()
    {
        Vector3 currentRot = transform.rotation.eulerAngles;
        if (currentSpeed < 1.0f)
        {
            float LeanSpeedtemp = 0.05f;
            float maxLeanAngleTemp = 3.0f;
            targetLeanAngle = maxLeanAngleTemp * -horizontalInput;
            currentLeanAngle = Mathf.LerpAngle(currentLeanAngle, targetLeanAngle, LeanSpeedtemp * 0.1f);
        }
        if (currentSpeed >= 1.0f && currentSpeed < 5.0f)
        {
            float LeanSpeedtemp = 0.135f;
            float maxLeanAngleTemp = 25.0f;
            targetLeanAngle = maxLeanAngleTemp * -horizontalInput;
            currentLeanAngle = Mathf.LerpAngle(currentLeanAngle, targetLeanAngle, LeanSpeedtemp * 0.1f);
        }
        if (currentSpeed >= 5.0f && currentSpeed < 15.0f)
        {
            float t = (currentSpeed - 5.0f) / (15.0f - 5.0f);
            float LeanSpeedtemp = Mathf.Lerp(0.135f, LeanSpeed, currentSpeed / 15.0f);
            float maxLeanAngleTemp = Mathf.Lerp(25.0f, maxLeanAngle, currentSpeed / 15.0f);

            targetLeanAngle = maxLeanAngleTemp * -horizontalInput;
            currentLeanAngle = Mathf.LerpAngle(currentLeanAngle, targetLeanAngle, LeanSpeedtemp * 0.1f);
        }

        if (currentSpeed >= 15.0f)
        {
            targetLeanAngle = maxLeanAngle * -horizontalInput;
            currentLeanAngle = Mathf.LerpAngle(currentLeanAngle, targetLeanAngle, LeanSpeed * 0.1f);
        }
        if (rotateBike == true)
        {
            if (TouchingWater == false)
            {
                if (validRotation == true)
                {
                    transform.rotation = Quaternion.Euler(currentRot.x, currentRot.y, currentLeanAngle);
                }

            }



            //transform.Rotate(0.0f, 0.0f, currentLeanAngle);

            /*
            // leanRatio can be -1.0f to 1.0f
            // amount to rotate = current rotation to targetLean(currentLeanAngle) according to how much you have already rotated/ maxAmount to rotate to
            float targetRotation = Mathf.Lerp(currentLeanAngle, -currentLeanAngle, targetLeanAngle);
            //float targetRotation = Mathf.Lerp(currentLeanAngle, -currentLeanAngle, transform.eulerAngles.z / maxLeanAngle);
            //transform.Rotate(0.0f, 0.0f, targetRotation * currentLeanAngle / maxLeanAngle);

            //Vector3 rotationChange = new Vector3(currentRot.x, currentRot.y, currentRot.z + targetRotation);
            //transform.eulerAngles = rotationChange;

            transform.Rotate(0.0f, 0.0f, targetRotation);

            */


        }
        currentLean = Mathf.Abs(currentLeanAngle);
        //LeanRatio = -currentLeanAngle / maxLeanAngle;
    }
    void MoveCOG()
    {
        if (HandleCOGMovement == true)
        {
            COGShiftValue = Mathf.Lerp(0.0f, COGShiftTarget, currentLean / maxLeanAngle);
            //float shiftValue = Mathf.Lerp(-COGShiftValue / COGShiftTarget, COGShiftValue / COGShiftTarget, (LeanRatio + 1) / 2);
            float shiftValue = Mathf.Lerp(-COGShiftValue / COGShiftTarget, COGShiftValue / COGShiftTarget, (LeanRatio + horizontalInput) / 2);
            //float wheelieValueMultiplier = Mathf.Lerp(1.0f, -0.5f, rb.centerOfMass.z / wheelieShiftTarget);
            RaycastHit WheelieHit;
            Ray downRay = new Ray(frontWheelTransform.transform.position, -Vector3.up);
            Physics.Raycast(downRay, out WheelieHit, 2.0f);
            Debug.DrawLine(frontWheelTransform.transform.position, WheelieHit.point, UnityEngine.Color.red);
            float wheelieHeightDiff = Mathf.Abs(WheelieHit.distance - 0.3f);
            if (wheelieVal >= 0.05f || frontWheel.isGrounded == false)
            {
                wheelieSphereCollider.radius = 0.275f;
            }
            else
            {
                wheelieSphereCollider.radius = 0.265f;
            }

            if (currentSpeed <= 45.0f)
            {
                /*
                //Modifying Suspension Spring inside BackWheel component 
                JointSpring backWheelSpringObject = backWheel.suspensionSpring;
                backWheelSpringObject.damper = Mathf.Lerp(1800f, 5000f, wheelieVal);
                backWheel.suspensionDistance = Mathf.Lerp(0.1f, 0.2f, wheelieVal);



                backWheel.suspensionSpring = backWheelSpringObject;
                */

                if (wheelieVal >= 0.25f && horizontalInput <= 0.275f && Mathf.Abs(LeanRatio) <= 0.2f)
                {
                    //Modifying Suspension Spring inside BackWheel component 
                    JointSpring backWheelSpringObject = backWheel.suspensionSpring;
                    //backWheelSpringObject.damper = Mathf.Lerp(1800f, 5000f, wheelieVal);
                    backWheelSpringObject.damper = 5000f;
                    backWheel.suspensionDistance = 0.2f;
                    //backWheel.suspensionDistance = Mathf.Lerp(0.1f, 0.2f, wheelieVal);



                    backWheel.suspensionSpring = backWheelSpringObject;

                    //WheelieColliderObject.SetActive(true);
                    wheelieShiftTarget = Mathf.Lerp(0.55f, 0.8f, ((currentSpeed / 45.0f) + (wheelieVal) / 2.0f));

                    //float wheelieValueMultiplier = Mathf.Lerp(1.0f, -0.5f, rb.centerOfMass.z / wheelieShiftTarget);

                    //float wheelieValueMultiplier = Mathf.Lerp(1.0f, 0.0f, rb.centerOfMass.z / wheelieShiftTarget);
                    float wheelieValueMultiplier = Mathf.Lerp(1.0f, 0.5f, wheelieHeightDiff / MaxWheelieDist);
                    //float wheelieValueMultiplier = Mathf.Lerp(0.85f, -0.5f, -rb.centerOfMass.z / wheelieShiftTarget);

                    //wheelieShiftTarget = Mathf.Lerp(1.1f, 0.8f, wheelieVal);
                    //float wheelieShiftValue = Mathf.Lerp(0.0f, wheelieShiftTarget * wheelieValueMultiplier, wheelieVal);
                    float wheelieShiftValue = Mathf.Lerp(0.0f, wheelieShiftTarget * wheelieValueMultiplier, 1.0f);
                    rb.centerOfMass = new Vector3(shiftValue * COGShiftTarget, COG_Offset.y + (0.05f * wheelieShiftValue * wheelieValueMultiplier), COG_Offset.z - wheelieShiftValue);
                    //rb.centerOfMass = new Vector3(shiftValue * COGShiftTarget, COG_Offset.y * wheelieShiftValue * wheelieValueMultiplier, COG_Offset.z - wheelieShiftValue);
                    //rb.centerOfMass = new Vector3(shiftValue * COGShiftTarget, COG_Offset.y, COG_Offset.z - wheelieShiftValue);
                }
                else
                {
                    if (LeanRatio >= 0.1f && wheelieVal <= 0.1f)
                    {
                        //WheelieColliderObject.SetActive(false);
                    }
                    else
                    {
                        //WheelieColliderObject.SetActive(true);
                    }
                    //Modifying Suspension Spring inside BackWheel component 
                    JointSpring backWheelSpringObject = backWheel.suspensionSpring;
                    //backWheelSpringObject.damper = Mathf.Lerp(1800f, 5000f, wheelieVal);
                    backWheelSpringObject.damper = 1800f;
                    backWheel.suspensionDistance = 0.1f;
                    //backWheel.suspensionDistance = Mathf.Lerp(0.1f, 0.2f, wheelieVal);



                    backWheel.suspensionSpring = backWheelSpringObject;
                    rb.centerOfMass = new Vector3(shiftValue * COGShiftTarget, COG_Offset.y, COG_Offset.z);
                }
                //wheelieShiftTarget = Mathf.Lerp(1.055f, 1.22f, currentSpeed / MaxSpeed);
                //wheelieShiftTarget = Mathf.Lerp(1.048f, 1.07f, ((currentSpeed / MaxSpeed) + (wheelieVal) / 2.0f));
                //wheelieShiftTarget = Mathf.Lerp(0.8f, 1f, ((currentSpeed / MaxSpeed) + (wheelieVal) / 2.0f));

                //float wheelieShiftValue = Mathf.Lerp(0.0f, wheelieShiftTarget * wheelieValueMultiplier, wheelieVal);
                //float wheelieShiftValue = Mathf.Lerp(0.0f, wheelieShiftTarget, Mathf.Abs(wheelieVal / 1.0f));

                //rb.centerOfMass = new Vector3(shiftValue * COGShiftTarget, COG_Offset.y + (0.05f * wheelieShiftValue * wheelieValueMultiplier), COG_Offset.z - wheelieShiftValue);
                //rb.centerOfMass = new Vector3(shiftValue * COGShiftTarget, COG_Offset.y, COG_Offset.z - wheelieShiftValue);


            }
            else
            {
                /*
                //Modifying Suspension Spring inside BackWheel component
                JointSpring backWheelSpringObject = backWheel.suspensionSpring;
                backWheelSpringObject.damper = 2000f;
                backWheel.suspensionSpring = backWheelSpringObject;
                */
                rb.centerOfMass = new Vector3(shiftValue * COGShiftTarget, COG_Offset.y, COG_Offset.z);
            }
        }
        else
        {
            if (LeanRatio >= 0.1f && wheelieVal <= 0.1f)
            {
                WheelieColliderObject.SetActive(false);
            }
            else
            {
                WheelieColliderObject.SetActive(true);
            }
            rb.centerOfMass = new Vector3(COG_Offset.x, COG_Offset.y, COG_Offset.z);
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
            /*
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
            else
            */
            {
                //frontWheelfcFF.extremumSlip = Mathf.Lerp(1.75f, 1.95f, Mathf.Abs(LeanRatio));
                //frontWheelfcSF.extremumSlip = Mathf.Lerp(1.65f, 1.85f, Mathf.Abs(LeanRatio));
                //backWheelfcSF.extremumSlip = Mathf.Lerp(1.35f, 1.15f, Mathf.Abs(LeanRatio));
                //backWheelfcFF.extremumSlip = Mathf.Lerp(1.2f, 1.35f, Mathf.Abs(LeanRatio));
                if (currentSpeed >= 0 && currentSpeed <= 10) // 0-10 FwF Modifier 
                {
                    frontWheelfcFF.stiffness = 4.5f;
                    frontWheelfcSF.stiffness = 6.5f;
                    backWheelfcFF.stiffness = 4.7f;
                    backWheelfcSF.stiffness = 10.0f;
                    frontWheel.forwardFriction = frontWheelfcFF;
                    frontWheel.sidewaysFriction = frontWheelfcSF;
                    backWheel.forwardFriction = backWheelfcFF;
                    backWheel.sidewaysFriction = backWheelfcSF;
                }
                else if (currentSpeed > 10 && currentSpeed <= 30) // 10-30 FwF modifier
                {
                    frontWheelfcFF.stiffness = 5.2f;
                    frontWheelfcSF.stiffness = 10.5f;
                    backWheelfcFF.stiffness = 3.3f;
                    backWheelfcSF.stiffness = 27.0f;
                    frontWheel.forwardFriction = frontWheelfcFF;
                    frontWheel.sidewaysFriction = frontWheelfcSF;
                    backWheel.forwardFriction = backWheelfcFF;
                    backWheel.sidewaysFriction = backWheelfcSF;
                }
                else if (currentSpeed > 30 && currentSpeed <= 50) // 30-50 FwF modifier
                {
                    frontWheelfcFF.stiffness = 6.8f;
                    frontWheelfcSF.stiffness = 13.0f;
                    backWheelfcFF.stiffness = 3.4f;
                    backWheelfcSF.stiffness = 36.0f;
                    frontWheel.forwardFriction = frontWheelfcFF;
                    frontWheel.sidewaysFriction = frontWheelfcSF;
                    backWheel.forwardFriction = backWheelfcFF;
                    backWheel.sidewaysFriction = backWheelfcSF;
                }
                else if (currentSpeed > 50 && currentSpeed <= 70) // 30-50 FwF modifier
                {
                    frontWheelfcFF.stiffness = 8.4f;
                    frontWheelfcSF.stiffness = 14.2f;
                    backWheelfcFF.stiffness = 4.2f;
                    backWheelfcSF.stiffness = 47.0f;
                    frontWheel.forwardFriction = frontWheelfcFF;
                    frontWheel.sidewaysFriction = frontWheelfcSF;
                    backWheel.forwardFriction = backWheelfcFF;
                    backWheel.sidewaysFriction = backWheelfcSF;
                }
                else if (currentSpeed > 70 && currentSpeed <= 90) // 70-90 FwF modifier
                {
                    frontWheelfcFF.stiffness = 10.1f;
                    frontWheelfcSF.stiffness = 20.0f;
                    backWheelfcFF.stiffness = 5.5f;
                    backWheelfcSF.stiffness = 50.0f;
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
    void HandleCamera()
    {
        if (cameraMode == CameraMode.FirstPerson)
        {
            ThirdPersonCameraObject.SetActive(false);
            FirstPersonCameraObject.SetActive(true);
            //float FPPCamRotate = FirstPersonCameraObject.transform.eulerAngles.y;
            //orbitalFollow.HorizontalAxis.Center = FPPCamRotate;

            if (currentSpeed >= 15.0f)
            {
                FPPCamNoiseObject.AmplitudeGain = Mathf.Lerp(0.006f, 0.1f, currentSpeed / MaxSpeed);
                FPPCamNoiseObject.FrequencyGain = Mathf.Lerp(0.7f, 1.45f, ((currentSpeed / (MaxSpeed - 15.0f) + Mathf.Abs(currentLean / maxLeanAngle)) / 2));
                FPPCameraCinemachineCamera.Lens.FieldOfView = Mathf.Lerp(64.0f, 73.0f, (currentSpeed - 15.0f) / (MaxSpeed - 15.0f));
            }
            else
            {
                FPPCameraCinemachineCamera.Lens.FieldOfView = 64.0f;
                FPPCamNoiseObject.AmplitudeGain = 0.0f;
                FPPCamNoiseObject.FrequencyGain = 0.0f;
            }
        }
        if (cameraMode == CameraMode.ThirdPerson)
        {
            ThirdPersonCameraObject.SetActive(true);
            FirstPersonCameraObject.SetActive(false);
            //orbitalFollow = cinemachineCamera.GetComponent<CinemachineOrbitalFollow>();
            if (currentSpeed > 12.0f)
            {
                orbitalFollow.HorizontalAxis.Recentering.Wait = Mathf.Lerp(0.5f, 0.2f, currentSpeed / MaxSpeed);
            }
            if (currentSpeed <= 12.0f)
            {
                orbitalFollow.HorizontalAxis.Recentering.Wait = 2.5f;
            }
        }

    }

    public void turnOffTailLight()
    {
        emissiveMaterial.DisableKeyword("_EMISSION");
    }
    public void turnOnTailLight()
    {
        emissiveMaterial.EnableKeyword("_EMISSION");
    }

    public void CheckGroundDistanceRaycasts()
    {
        Ray rayLeft = new Ray(leftSideDistanceRayObject.transform.position, -Vector3.up);
        Ray rayRight = new Ray(rightSideDistanceRayObject.transform.position, -Vector3.up);
        RaycastHit hitLeft;
        RaycastHit hitRight;
        Physics.Raycast(rayLeft, out hitLeft);
        Physics.Raycast(rayLeft, out hitRight);

        leftSideDistance = hitLeft.distance;
        rightSideDistance = hitRight.distance;


    }
    void BalanceBike()
    {
        targetLeanAnglePID = -horizontalInput * maxLeanAngle; // Non LeanOnTurn() implementation 
        //targetLeanAnglePID = currentLeanAngle; // LeanOnTurn() implementation
        // Get current lean angle
        float currentLeanAnglePID = Vector3.SignedAngle(Vector3.up, transform.up, transform.forward);
        float error = targetLeanAnglePID - currentLeanAnglePID;
        currentError = error;

        // PID calculations
        balanceIntegral += error * Time.fixedDeltaTime;
        float derivative = (error - lastError) / Time.fixedDeltaTime;

        //float correction = balanceKp * error + balanceKi * balanceIntegral + balanceKd * derivative;

        //float correction = balanceKpModified * error + balanceKi * balanceIntegral + balanceKd * derivative;
        float correction = (balanceKpModified * (error * Mathf.Abs(horizontalInput))) + (balanceKi * balanceIntegral * balanceKiLeanMuliplier) + (balanceKd * derivative * balanceKdLeanMultiplier);

        if (error >= 1.5f || error <= -1.5f)
        {
            // Apply torque for balancing
            rb.AddTorque(transform.forward * correction, ForceMode.Force);
        }


        // Store error for next frame
        lastError = error;
    }
    void ApplyCounterSteering()
    {
        // Calculate steering angle based on lean angle
        float currentLeanAnglePID = Vector3.SignedAngle(Vector3.up, transform.up, transform.forward);
        float steerAngle = -currentLeanAnglePID * steerSensitivity;
        steerAngle = Mathf.Clamp(steerAngle, -maxSteerAngle, maxSteerAngle); // Non handleSteering function implementation
        //steerAngle = Mathf.Clamp(steerAngle, -currentMaxSteeringAngle, currentMaxSteeringAngle); // HandleSteering function Implementation
        currentSteeringAngle = steerAngle;
        // Apply steering to the front wheel
        frontWheel.steerAngle = steerAngle;
    }

    public void HandlePIDController()
    {
        float balanceKpLeanMultiplier = Mathf.Lerp(1.0f, 1.5f, Mathf.Abs(currentLean / 40.0f));

        if (currentSpeed < 0.5f)
        {
            balanceKpSpeedMultiplier = 1.0f;
        }
        if (currentSpeed > 0.5f && currentSpeed < 5.0f)
        {
            balanceKpSpeedMultiplier = Mathf.Lerp(1.0f, 0.75f, currentSpeed / 5.0f);
        }
        if (currentSpeed >= 5.0f && currentSpeed < 15.0f)
        {
            balanceKpSpeedMultiplier = Mathf.Lerp(0.75f, 0.65f, currentSpeed / 15.0f);
        }
        if (currentSpeed >= 15.0f && currentSpeed < 35.0f)
        {
            balanceKpSpeedMultiplier = Mathf.Lerp(0.65f, 0.5f, currentSpeed / 35.0f);
        }
        if (currentSpeed >= 35.0f && currentSpeed < 45.0f)
        {
            balanceKpSpeedMultiplier = Mathf.Lerp(0.5f, 0.4f, currentSpeed / 45.0f);
        }
        if (currentSpeed >= 45.0f && currentSpeed < 60.0f)
        {
            balanceKpSpeedMultiplier = Mathf.Lerp(0.4f, 0.35f, currentSpeed / 60.0f);
        }
        if (currentSpeed >= 60.0f && currentSpeed < MaxSpeed)
        {
            balanceKpSpeedMultiplier = Mathf.Lerp(0.35f, 0.25f, currentSpeed / MaxSpeed);
        }
        /*
        else
        {
            balanceKpSpeedMultiplier = 0.5f;
        }
        */
        balanceKpModified = balanceKp * balanceKpLeanMultiplier * balanceKpSpeedMultiplier;


        //balanceKp = balanceKp * balanceKpLeanMultiplier * balanceKpSpeedMultiplier;

        //balanceKi = Mathf.Lerp(0.05f, 2.0f, Mathf.Abs(currentError / 45.0f));
        balanceKiLeanMuliplier = Mathf.Lerp(2.0f, 0.15f, Mathf.Abs(horizontalInput));



        if (horizontalInput >= 0.05f || horizontalInput <= -0.05f)
        {
            balanceKdLeanMultiplier = Mathf.Lerp(2.0f, 1.0f, (Mathf.Abs(currentError / 45.0f) + Mathf.Abs(horizontalInput)) / 2.0f);
        }
        else
        {
            balanceKdLeanMultiplier = Mathf.Lerp(3.0f, 1.0f, (Mathf.Abs(currentError / 45.0f) + Mathf.Abs(horizontalInput)) / 2.0f);
        }


    }


    private void OnTriggerEnter(Collider waterCheckRigidBody)
    {
        if (waterCheckRigidBody.CompareTag("Water Surface"))
        {
            Debug.Log("Entered water surface");
            TouchingWater = true;
            GetComponent<Bouyancy>().enabled = true;
        }
    }

    private void OnTriggerExit(Collider waterCheckRigidBody)
    {
        if (waterCheckRigidBody.CompareTag("Water Surface"))
        {
            Debug.Log("Exited water surface");
            TouchingWater = false;
            GetComponent<Bouyancy>().enabled = false;
        }
    }

    void CheckWheelHit()
    {
        frontWheel.GetGroundHit(out WheelHit frontInfo);
        backWheel.GetGroundHit(out WheelHit backInfo);
        if (frontWheel.isGrounded == true)
        {
            frontWheelHit = true;
        }
        else
        {
            frontWheelHit = false;
        }
        if (backWheel.isGrounded == true)
        {
            backWheelHit = true;
        }
        else
        {
            backWheelHit = false;
        }
    }
    void CheckCollisions() // Dependant on CheckWheelHit() ^
    {
        BodyCollided = BodyCollisionColliderObject.GetComponent<CapsuleCollisionDetection>().isColliding;
        if (TouchingWater == false)
        {
            if (BodyCollided == true && frontWheelHit == true && backWheelHit == true) // Grounded and collided
            {
                validRotation = false;

                rb.linearDamping = 1.0f;
                rb.angularDamping = 5.0f;
            }
            if (BodyCollided == false && frontWheelHit == true && backWheelHit == true) // [NORMAL] grounded and not collided
            {
                validRotation = true;

                rb.linearDamping = 0.005f;
                rb.angularDamping = 0.1f;
            }
            if (BodyCollided == false && frontWheelHit == false && backWheelHit == true) // Wheelie
            {
                validRotation = true;

                rb.linearDamping = 0.1f;
                rb.angularDamping = 4.0f;
            }
            if (BodyCollided == true && frontWheelHit == false && backWheelHit == true) // FrontWheel not grounded and collided
            {
                validRotation = false;

                rb.linearDamping = 0.1f;
                rb.angularDamping = 0.1f;
            }
            if (BodyCollided == true && frontWheelHit == false && backWheelHit == false) // mid air not grounded and collided
            {
                validRotation = false;

                rb.linearDamping = 1.0f;
                rb.angularDamping = 2.0f;
            }
            if (BodyCollided == false && frontWheelHit == false && backWheelHit == false) // mid air not grounded and not collided
            {
                validRotation = false;

                rb.linearDamping = 0.5f;
                rb.angularDamping = 2.0f;
            }
        }
    }


    void CheckWater()
    {
        SphereCollider waterCheckRigidBody = WaterInteractionObject.GetComponent<SphereCollider>();
        /*
        void onTriggerEnter(Collider waterCheckRigidBody)
        {
            if (waterCheckRigidBody.gameObject.tag == "Water Surface")
            {
                TouchingWater = true;
                GetComponent<Bouyancy>().enabled = true;
            }
            else
            {
                TouchingWater = false;
                GetComponent<Bouyancy>().enabled = false;
            }
        }
        */
        //OnTriggerEnter(waterCheckRigidBody);


        /*
        if (EnableBouyancyScript == true)
        {
            GetComponent<Bouyancy>().enabled = true;
        }
        else
        {
            GetComponent<Bouyancy>().enabled = false;
        }
        */
    }
    void MotionBlur()
    {
        float blurAmount = Mathf.Lerp(0.0f, 0.8f, currentSpeed / MaxSpeed);
        FullscreenMotionBlurMaterial.SetFloat("_GodRaysStrength", blurAmount);
    }
    void CheckRotationValidity()
    {
        if (validRotation == true)
        {

            rb.constraints = RigidbodyConstraints.FreezeRotationZ;
        }
        else
        {
            rb.constraints = RigidbodyConstraints.None;
        }
    }




}