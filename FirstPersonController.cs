using UnityEngine;

public class FPSController : MonoBehaviour
{
    [Header("Hareket Hızları")]
    public float walkSpeed = 5f;
    public float runSpeed = 10f;
    public float aimWalkSpeed = 2.5f;
    public float aimRunSpeed = 5f;

    [Header("Çömelme (Crouch)")]
    public float crouchSpeed = 2.5f;
    public float crouchHeight = 1.0f;      // Çömelince capsule boyu
    public float standHeight = 1.8f;       // Normalde capsule boyu (1.8)
    public float crouchTransitionSpeed = 10f;

    [Header("Yumuşak Geçiş")]
    public float acceleration = 5f;
    public float deceleration = 25f;
    public float turnSmoothing = 6f;

    [Header("Zıplama Enerji Sistemi")]
    public float maxJumpForce = 1.6f;
    public float minJumpForce = 0.2f;
    public float jumpStaminaDrain = 0.2f;
    public float jumpStaminaRegen = 0.6f;
    public float jumpCooldown = 0.3f;

    [Header("Yerçekimi (Gravity)")]
    public float gravity = -15f;            // Sen bu değeri değiştireceksin!

    [Header("Mouse")]
    public float mouseSensitivity = 100f;
    public Transform cameraTransform;

    [Header("Recoil")]
    public float recoilRecoverySpeed = 8f;

    private CharacterController controller;
    private float yVelocity;
    private float xRotation = 0f;
    private Vector3 currentRecoil;
    private bool isAiming = false;

    private float currentSpeed = 0f;
    private Vector3 currentMoveDirection = Vector3.zero;

    private float currentJumpForce;
    private float lastJumpTime = -10f;

    private bool isCrouching = false;
    private float originalHeight;
    private Vector3 originalCameraPosition;

    public System.Action OnJump;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        currentJumpForce = maxJumpForce;

        originalHeight = controller.height;
        originalCameraPosition = cameraTransform.localPosition;

        // Başlangıçta capsule height'ı standHeight yap
        controller.height = standHeight;
    }

    void Update()
    {
        // Mouse Look
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        Vector3 finalRot = new Vector3(xRotation, transform.eulerAngles.y + mouseX, 0);
        finalRot += currentRecoil;
        cameraTransform.localRotation = Quaternion.Euler(finalRot.x, 0, 0);
        transform.rotation = Quaternion.Euler(0, finalRot.y, 0);

        currentRecoil = Vector3.Lerp(currentRecoil, Vector3.zero, recoilRecoverySpeed * Time.deltaTime);

        // ========== ÇÖMELME ==========
        if (Input.GetKeyDown(KeyCode.LeftControl) && IsGrounded())
        {
            isCrouching = !isCrouching;
        }

        float targetHeight = isCrouching ? crouchHeight : standHeight;
        controller.height = Mathf.Lerp(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);

        // Kameranın Y pozisyonunu ayarla (göz seviyesi) - çömelince kamera aşağı insin
        Vector3 camPos = cameraTransform.localPosition;
        float targetCamY = isCrouching ? originalCameraPosition.y - (standHeight - crouchHeight) : originalCameraPosition.y;
        camPos.y = Mathf.Lerp(camPos.y, targetCamY, crouchTransitionSpeed * Time.deltaTime);
        cameraTransform.localPosition = camPos;

        // ========== HAREKET ==========
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        bool isMoving = (x != 0 || z != 0);

        float baseTargetSpeed;
        if (isCrouching)
        {
            baseTargetSpeed = crouchSpeed;
        }
        else if (isRunning)
        {
            baseTargetSpeed = isAiming ? aimRunSpeed : runSpeed;
        }
        else
        {
            baseTargetSpeed = isAiming ? aimWalkSpeed : walkSpeed;
        }

        // Koşarken A/D basılırsa hız yarıya düşsün
        if (!isCrouching && isRunning && Mathf.Abs(x) > 0.1f && isMoving)
        {
            baseTargetSpeed *= 0.5f;
        }

        if (!isMoving) baseTargetSpeed = 0f;

        float smoothDelta = (baseTargetSpeed > currentSpeed) ? acceleration : deceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, baseTargetSpeed, smoothDelta * Time.deltaTime);

        Vector3 targetDirection = (transform.right * x + transform.forward * z).normalized;
        if (targetDirection.magnitude > 0.1f)
        {
            currentMoveDirection = Vector3.Lerp(currentMoveDirection, targetDirection, turnSmoothing * Time.deltaTime);
        }
        else
        {
            currentMoveDirection = Vector3.zero;
        }

        Vector3 move = currentMoveDirection * currentSpeed;

        // ========== ZIPLAMA ENERJİ ==========
        if (currentJumpForce < maxJumpForce)
            currentJumpForce += jumpStaminaRegen * Time.deltaTime;
        currentJumpForce = Mathf.Clamp(currentJumpForce, minJumpForce, maxJumpForce);

        if (controller.isGrounded && yVelocity < 0)
            yVelocity = -2f;

        // Çömelirken zıplama yapılamasın
        if (Input.GetKeyDown(KeyCode.Space) && controller.isGrounded && !isCrouching && Time.time >= lastJumpTime + jumpCooldown && currentJumpForce > minJumpForce)
        {
            float jumpPower = currentJumpForce;
            yVelocity = Mathf.Sqrt(jumpPower * -2f * gravity);
            lastJumpTime = Time.time;
            currentJumpForce -= jumpStaminaDrain;
            if (currentJumpForce < minJumpForce) currentJumpForce = minJumpForce;

            OnJump?.Invoke();
        }

        yVelocity += gravity * Time.deltaTime;
        move.y = yVelocity;

        controller.Move(move * Time.deltaTime);
    }

    public void SetAiming(bool aiming) { isAiming = aiming; }
    public void AddRecoil(Vector3 recoil) { currentRecoil += recoil; currentRecoil.x = Mathf.Clamp(currentRecoil.x, -15f, 15f); currentRecoil.y = Mathf.Clamp(currentRecoil.y, -10f, 10f); }
    public float GetCurrentSpeed() { return currentSpeed; }
    public bool IsGrounded() { return controller.isGrounded; }
    public float GetCurrentJumpForceNormalized() { return (currentJumpForce - minJumpForce) / (maxJumpForce - minJumpForce); }
}