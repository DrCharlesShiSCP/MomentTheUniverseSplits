using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FreeCamera : MonoBehaviour
{
    [Header("Movement")]
    public float movementSpeed = 4f;
    public float fastMovementSpeed = 7f;
    public float freeLookSensitivity = 2.5f;
    public float zoomSensitivity = 10f;
    public float fastZoomSensitivity = 25f;

    [Header("FPS Capsule")]
    [SerializeField] private float capsuleHeight = 1.8f;
    [SerializeField] private float capsuleRadius = 0.35f;
    [SerializeField] private float eyeHeight = 1.6f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private bool lockCursorOnStart = true;
    [SerializeField] private KeyCode unlockCursorKey = KeyCode.Escape;

    private bool looking;
    private float pitch;
    private float verticalVelocity;
    private Camera cachedCamera;
    private Transform movementRoot;
    private CharacterController characterController;
    private bool loggedBodySetup;

    private void Awake()
    {
        cachedCamera = GetComponent<Camera>();
        EnsureCapsuleBody();
        pitch = NormalizePitch(transform.localEulerAngles.x);

        if (lockCursorOnStart)
        {
            StartLooking();
        }
        else
        {
            StopLooking();
        }
    }

    private void Update()
    {
        HandleCursorState();
        HandleMovement();
        HandleLook();
        HandleZoom();
    }

    private void OnDisable()
    {
        StopLooking();
    }

    public void StartLooking()
    {
        looking = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void StopLooking()
    {
        looking = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void HandleCursorState()
    {
        if (Input.GetKeyDown(unlockCursorKey))
        {
            StopLooking();
            return;
        }

        if (!looking && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)))
        {
            StartLooking();
        }
    }

    private void HandleMovement()
    {
        EnsureCapsuleBody();

        var fastMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        var currentMovementSpeed = fastMode ? fastMovementSpeed : movementSpeed;

        var horizontal = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            horizontal -= 1f;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            horizontal += 1f;
        }

        var forward = 0f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            forward += 1f;
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            forward -= 1f;
        }

        var movement = (movementRoot.forward * forward) + (movementRoot.right * horizontal);
        if (movement.sqrMagnitude > 1f)
        {
            movement.Normalize();
        }

        if (characterController != null)
        {
            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            verticalVelocity += gravity * Time.deltaTime;
            var velocity = (movement * currentMovementSpeed) + (Vector3.up * verticalVelocity);
            characterController.Move(velocity * Time.deltaTime);
            return;
        }

        movementRoot.position += movement * currentMovementSpeed * Time.deltaTime;
    }

    private void HandleLook()
    {
        if (!looking)
        {
            return;
        }

        EnsureCapsuleBody();

        var mouseX = Input.GetAxis("Mouse X") * freeLookSensitivity;
        var mouseY = Input.GetAxis("Mouse Y") * freeLookSensitivity;

        movementRoot.Rotate(Vector3.up * mouseX, Space.World);
        pitch = Mathf.Clamp(pitch - mouseY, -85f, 85f);
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleZoom()
    {
        if (cachedCamera == null)
        {
            return;
        }

        var fastMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        var axis = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(axis, 0f))
        {
            return;
        }

        var currentZoomSensitivity = fastMode ? fastZoomSensitivity : zoomSensitivity;
        cachedCamera.fieldOfView = Mathf.Clamp(cachedCamera.fieldOfView - (axis * currentZoomSensitivity), 35f, 85f);
    }

    private void EnsureCapsuleBody()
    {
        characterController = GetComponentInParent<CharacterController>();
        if (characterController == null)
        {
            var bodyObject = new GameObject($"{name}_Capsule");
            var currentParent = transform.parent;
            if (currentParent != null)
            {
                bodyObject.transform.SetParent(currentParent, false);
            }

            var clampedEyeHeight = Mathf.Clamp(eyeHeight, 0.6f, capsuleHeight - 0.1f);
            bodyObject.transform.position = transform.position - new Vector3(0f, clampedEyeHeight, 0f);
            bodyObject.transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

            characterController = bodyObject.AddComponent<CharacterController>();
            characterController.height = capsuleHeight;
            characterController.radius = capsuleRadius;
            characterController.center = new Vector3(0f, capsuleHeight * 0.5f, 0f);
            characterController.minMoveDistance = 0f;
            characterController.stepOffset = 0.3f;
            characterController.slopeLimit = 45f;

            transform.SetParent(bodyObject.transform, true);
        }

        movementRoot = characterController.transform;

        if (movementRoot.GetComponent<QuantumBranching.CharacterControllerDoorPusher>() == null)
        {
            movementRoot.gameObject.AddComponent<QuantumBranching.CharacterControllerDoorPusher>();
        }

        var cameraLocalHeight = Mathf.Clamp(eyeHeight, 0.6f, capsuleHeight - 0.1f);
        transform.localPosition = new Vector3(0f, cameraLocalHeight, 0f);
        movementRoot.rotation = Quaternion.Euler(0f, movementRoot.eulerAngles.y, 0f);
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        if (!loggedBodySetup)
        {
            Debug.Log($"{nameof(FreeCamera)} attached {name} to capsule body {movementRoot.name}.", movementRoot);
            loggedBodySetup = true;
        }
    }

    private static float NormalizePitch(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }
}
