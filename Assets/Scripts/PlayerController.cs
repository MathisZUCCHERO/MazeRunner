using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintMultiplier = 1.5f;
    public float gravity = -9.81f;
    
    [Header("Look Settings")]
    public Transform playerCamera;
    public float mouseSensitivity = 2f;
    public float lookXLimit = 85f;

    [Header("State")]
    public bool canMove = true;
    public bool isSprinting = false;

    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {

        // Don't move if paused or disabled
        if (!canMove || Time.timeScale == 0f) return;

        // --- Rotation ---
        rotationX += -Input.GetAxis("Mouse Y") * mouseSensitivity;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        
        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(rotationX, 0, 0);
        }
        
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * mouseSensitivity, 0);

        // --- Movement ---
        // Check sprint
        isSprinting = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isSprinting ? walkSpeed * sprintMultiplier : walkSpeed;

        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        
        float curSpeedX = currentSpeed * Input.GetAxis("Vertical");
        float curSpeedY = currentSpeed * Input.GetAxis("Horizontal");
        
        // Preserve Y movement (gravity/jumping if we add it)
        float movementDirectionY = moveDirection.y;
        
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        if (!characterController.isGrounded)
        {
            moveDirection.y += gravity * Time.deltaTime;
        }
        else
        {
             // Small downward force to keep grounded
             moveDirection.y = -2f;
        }

        characterController.Move(moveDirection * Time.deltaTime);
    }
    
    // Public method to modify speed from items
    public void ApplySpeedBoost(float multiplier, float duration)
    {
        StartCoroutine(SpeedBoostRoutine(multiplier, duration));
    }

    private System.Collections.IEnumerator SpeedBoostRoutine(float multiplier, float duration)
    {
        float originalWalk = walkSpeed;
        walkSpeed *= multiplier;
        yield return new WaitForSeconds(duration);
        walkSpeed = originalWalk;
    }
}
