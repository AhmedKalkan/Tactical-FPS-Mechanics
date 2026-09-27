using UnityEngine;
using System.Collections;

public class UltimateWeapon : MonoBehaviour
{
    [Header("NAMNU (IŞIN ÇIKIŞ)")]
    public Transform shootPoint;

    [Header("ATEŞ")]
    public float damage = 25f;
    public float range = 200f;
    public float fireRate = 10f;
    public LayerMask ignoreLayer;

    [Header("NİŞAN (ADS) - KAMERA")]
    public float normalFOV = 60f;
    public float aimFOV = 40f;
    public float fovSpeed = 10f;
    public float normalSensitivity = 150f;
    public float aimSensitivity = 100f;

    [Header("SWAY (SİLAH SALINIMI)")]
    public float swayIntensity = 4f;
    public float swaySmoothing = 8f;
    public float maxSwayAngle = 10f;
    public float aimSwayIntensity = 5f;
    public float aimSwaySmoothing = 5f;
    public float aimMaxSwayAngle = 2f;

    [Header("SİLAH GÖRSEL RECOIL")]
    public Vector3 positionKick = new Vector3(0.02f, 0.06f, -0.04f);
    public Vector3 rotationKick = new Vector3(3f, 1.5f, 1f);
    public float returnSpeed = 8f;
    public float aimPositionMultiplier = 0.4f;
    public float aimRotationMultiplier = 0.5f;

    [Header("KAMERA RECOIL (GERÇEK TEPME)")]
    public Vector3 cameraRecoilKick = new Vector3(2f, 1f, 0f);
    public float aimCameraRecoilMultiplier = 0.4f;

    [Header("NİŞANDA SİLAH POZİSYONU")]
    public Vector3 normalWeaponPos;
    public Vector3 aimWeaponPos = new Vector3(0.014f, 0f, 0.7f);
    public float weaponAimSpeed = 15f;

    [Header("SİLAH SALLANMA (BOBBING)")]
    public bool enableBobbing = true;
    public float walkBobSpeed = 6f;
    public float walkBobAmount = 0.03f;
    public float runBobSpeed = 10f;
    public float runBobAmount = 0.06f;
    public float sideBobAmount = 0.02f;
    public float aimBobMultiplier = 0.3f;

    [Header("NEFES SALLANMASI (IDLE SWAY)")]
    public float idleSwayAmount = 0.008f;
    public float idleSwaySpeed = 1.5f;
    public float aimIdleSwayMultiplier = 0.3f;

    [Header("ZIPLAMA REAKSİYONU")]
    public Vector3 jumpPositionOffset = new Vector3(0f, -0.02f, -0.03f);
    public Vector3 jumpRotationOffset = new Vector3(2f, 0f, 1f);
    public float jumpReactDuration = 0.3f;

    [Header("MOUSE WHEEL SİLAH İNDİRME")]
    public bool enableMouseWheelHolster = true;
    public Vector3 holsterPosition = new Vector3(0f, -1.5f, 0.2f);
    public float holsterSpeed = 10f;

    [Header("GÖRÜNÜR IŞIN")]
    public LineRenderer tracerLine;
    public float tracerDuration = 0.5f;

    private Camera cam;
    private FPSController fpsController;
    private Vector3 originalWeaponPos;
    private Quaternion originalWeaponRot;
    private Quaternion originalSwayRot;
    private Vector3 currentPosKick;
    private Vector3 currentRotKick;
    private float nextTimeToFire = 0f;
    private bool isAiming = false;

    private float bobTimer = 0f;
    private Vector3 bobOffset;
    private float currentSpeed = 0f;
    private float idleSwayTimer = 0f;
    private float jumpReactTimer = 0f;
    private Vector3 currentJumpPosOffset;
    private Vector3 currentJumpRotOffset;
    private bool isHolstered = false;
    private bool isHolstering = false;

    void Start()
    {
        cam = Camera.main;
        if (cam == null) cam = FindAnyObjectByType<Camera>();
        fpsController = FindAnyObjectByType<FPSController>();

        if (fpsController != null)
            fpsController.OnJump += OnJumpReaction;

        originalWeaponPos = transform.localPosition;
        originalWeaponRot = transform.localRotation;
        originalSwayRot = transform.localRotation;
        normalWeaponPos = originalWeaponPos;

        Cursor.lockState = CursorLockMode.Locked;

        if (tracerLine == null)
        {
            tracerLine = GetComponent<LineRenderer>();
            if (tracerLine == null) tracerLine = gameObject.AddComponent<LineRenderer>();
        }
        tracerLine.startWidth = 0.03f;
        tracerLine.endWidth = 0.03f;
        tracerLine.startColor = Color.yellow;
        tracerLine.endColor = Color.red;
        tracerLine.enabled = false;
        tracerLine.material = new Material(Shader.Find("Sprites/Default"));
    }

    void OnJumpReaction()
    {
        jumpReactTimer = jumpReactDuration;
        currentJumpPosOffset = jumpPositionOffset;
        currentJumpRotOffset = jumpRotationOffset;
    }

    void Update()
    {
        if (cam == null) return;

        // Mouse wheel silah indirme
        if (enableMouseWheelHolster)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0 && !isHolstering)
            {
                if (scroll > 0f && !isHolstered)
                    StartCoroutine(HolsterWeaponCoroutine(true));
                else if (scroll < 0f && isHolstered)
                    StartCoroutine(HolsterWeaponCoroutine(false));
            }

            if (isHolstered)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, holsterPosition, holsterSpeed * Time.deltaTime);
                return;
            }
        }

        isAiming = Input.GetMouseButton(1);
        if (fpsController != null)
            fpsController.SetAiming(isAiming);

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, isAiming ? aimFOV : normalFOV, fovSpeed * Time.deltaTime);
        if (fpsController != null)
            fpsController.mouseSensitivity = isAiming ? aimSensitivity : normalSensitivity;

        if (fpsController != null)
            currentSpeed = fpsController.GetCurrentSpeed();

        // Idle Sway
        float currentIdleAmount = idleSwayAmount * (isAiming ? aimIdleSwayMultiplier : 1f);
        idleSwayTimer += Time.deltaTime * idleSwaySpeed;
        float idleY = Mathf.Sin(idleSwayTimer) * currentIdleAmount;
        float idleX = Mathf.Sin(idleSwayTimer * 0.7f) * currentIdleAmount * 0.5f;
        Vector3 idleOffset = new Vector3(idleX, idleY, 0);

        // Bobbing
        Vector3 moveBobOffset = Vector3.zero;
        if (enableBobbing && IsGrounded() && (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0))
        {
            bool isRunning = Input.GetKey(KeyCode.LeftShift) && currentSpeed > 3f;
            float bobFreq = isRunning ? runBobSpeed : walkBobSpeed;
            float bobAmpY = isRunning ? runBobAmount : walkBobAmount;
            float bobAmpX = sideBobAmount;
            if (isAiming)
            {
                bobAmpY *= aimBobMultiplier;
                bobAmpX *= aimBobMultiplier;
                bobFreq *= aimBobMultiplier;
            }
            bobTimer += Time.deltaTime * bobFreq;
            float sinY = Mathf.Sin(bobTimer) * bobAmpY;
            float sinX = Mathf.Sin(bobTimer * 1.5f) * bobAmpX;
            moveBobOffset = new Vector3(sinX, sinY, 0);
        }
        else
            bobTimer = 0f;

        Vector3 totalBob = idleOffset + moveBobOffset;
        bobOffset = Vector3.Lerp(bobOffset, totalBob, 12f * Time.deltaTime);

        // Jump reaction
        if (jumpReactTimer > 0f)
        {
            jumpReactTimer -= Time.deltaTime;
            float t = jumpReactTimer / jumpReactDuration;
            currentJumpPosOffset = Vector3.Lerp(jumpPositionOffset, Vector3.zero, 1f - t);
            currentJumpRotOffset = Vector3.Lerp(jumpRotationOffset, Vector3.zero, 1f - t);
        }
        else
        {
            currentJumpPosOffset = Vector3.zero;
            currentJumpRotOffset = Vector3.zero;
        }

        // ADS + Bob + Jump
        Vector3 targetAdsPos = isAiming ? aimWeaponPos : normalWeaponPos;
        Vector3 desiredPos = targetAdsPos + bobOffset + currentJumpPosOffset;
        transform.localPosition = Vector3.Lerp(transform.localPosition, desiredPos, weaponAimSpeed * Time.deltaTime);

        // Sway
        float curIntensity = isAiming ? aimSwayIntensity : swayIntensity;
        float curSmoothing = isAiming ? aimSwaySmoothing : swaySmoothing;
        float curMax = isAiming ? aimMaxSwayAngle : maxSwayAngle;
        float mouseX = Input.GetAxis("Mouse X") * curIntensity;
        float mouseY = Input.GetAxis("Mouse Y") * curIntensity;
        float tiltX = Mathf.Clamp(mouseY, -curMax, curMax);
        float tiltY = Mathf.Clamp(mouseX, -curMax, curMax);
        Quaternion targetSwayRot = originalSwayRot * Quaternion.Euler(-tiltX, tiltY, 0);
        Quaternion swayOnlyRot = Quaternion.Slerp(transform.localRotation, targetSwayRot, curSmoothing * Time.deltaTime);

        // Recoil
        currentPosKick = Vector3.Lerp(currentPosKick, Vector3.zero, returnSpeed * Time.deltaTime);
        currentRotKick = Vector3.Lerp(currentRotKick, Vector3.zero, returnSpeed * Time.deltaTime);
        Vector3 finalPosKick = currentPosKick * (isAiming ? aimPositionMultiplier : 1f);
        Vector3 finalRotKick = currentRotKick * (isAiming ? aimRotationMultiplier : 1f);

        transform.localPosition += finalPosKick;
        Quaternion jumpRot = Quaternion.Euler(currentJumpRotOffset);
        transform.localRotation = swayOnlyRot * Quaternion.Euler(finalRotKick) * jumpRot;

        // Ateş (sadece sol fare)
        if (Input.GetMouseButton(0) && Time.time >= nextTimeToFire)
        {
            nextTimeToFire = Time.time + 1f / fireRate;
            Shoot();
            AddVisualAndCameraKick();
        }
    }

    // PUBLIC METOT - PickUp.cs çağıracak
    public void HolsterWeapon(bool holster)
    {
        if (!isHolstering)
            StartCoroutine(HolsterWeaponCoroutine(holster));
    }

    private IEnumerator HolsterWeaponCoroutine(bool holster)
    {
        isHolstering = true;
        float duration = 0.2f;
        float elapsed = 0f;

        Vector3 startPos = transform.localPosition;
        Quaternion startRot = transform.localRotation;
        Vector3 targetPos = holster ? holsterPosition : normalWeaponPos;
        Quaternion targetRot = holster ? Quaternion.identity : originalWeaponRot;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t);
            transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            transform.localRotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        transform.localPosition = targetPos;
        transform.localRotation = targetRot;
        isHolstered = holster;
        isHolstering = false;
    }

    bool IsGrounded() => fpsController != null && fpsController.IsGrounded();

    void Shoot()
    {
        if (shootPoint == null) return;
        Vector3 origin = shootPoint.position;
        Vector3 direction = shootPoint.forward;
        RaycastHit hit;
        Vector3 targetPoint;
        if (Physics.Raycast(origin, direction, out hit, range, ~ignoreLayer))
            targetPoint = hit.point;
        else
            targetPoint = origin + direction * range;
        StartCoroutine(DrawTracer(origin, targetPoint));
    }

    void AddVisualAndCameraKick()
    {
        Vector3 rndPos = new Vector3(
            Random.Range(positionKick.x * 0.7f, positionKick.x * 1.3f),
            Random.Range(positionKick.y * 0.7f, positionKick.y * 1.3f),
            Random.Range(positionKick.z * 0.7f, positionKick.z * 1.3f)
        );
        Vector3 rndRot = new Vector3(
            Random.Range(rotationKick.x * 0.8f, rotationKick.x * 1.2f),
            Random.Range(-rotationKick.y, rotationKick.y),
            Random.Range(-rotationKick.z, rotationKick.z)
        );
        currentPosKick += rndPos;
        currentRotKick += rndRot;

        currentPosKick.x = Mathf.Clamp(currentPosKick.x, -0.1f, 0.1f);
        currentPosKick.y = Mathf.Clamp(currentPosKick.y, 0f, 0.2f);
        currentPosKick.z = Mathf.Clamp(currentPosKick.z, -0.15f, 0.05f);
        currentRotKick.x = Mathf.Clamp(currentRotKick.x, -10f, 15f);
        currentRotKick.y = Mathf.Clamp(currentRotKick.y, -8f, 8f);
        currentRotKick.z = Mathf.Clamp(currentRotKick.z, -5f, 5f);

        if (fpsController != null)
        {
            float camX = Random.Range(cameraRecoilKick.x * 0.8f, cameraRecoilKick.x * 1.2f);
            float camY = Random.Range(-cameraRecoilKick.y, cameraRecoilKick.y);
            if (isAiming)
            {
                camX *= aimCameraRecoilMultiplier;
                camY *= aimCameraRecoilMultiplier;
            }
            fpsController.AddRecoil(new Vector3(camX, camY, 0f));
        }
    }

    IEnumerator DrawTracer(Vector3 start, Vector3 end)
    {
        tracerLine.enabled = true;
        tracerLine.positionCount = 2;
        tracerLine.SetPosition(0, start);
        tracerLine.SetPosition(1, end);
        yield return new WaitForSeconds(tracerDuration);
        tracerLine.enabled = false;
    }
}