using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class TopDownPlayerController : MonoBehaviour
{
    [Header("Camera")]
    public Camera topDownCamera;

    [Header("Movement")]
    public float moveSpeed = 4.5f;
    public float gravity = 20f;

    [Header("Optional")]
    public bool rotateTowardMove = true;
    public float rotationSpeed = 12f;

    private CharacterController _cc;
    private Vector3 _velocity;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    void OnEnable()
    {
        _velocity = Vector3.zero;
    }

    void Update()
    {
        // --- INPUT: use keys directly (most reliable) ---
        float h = 0f;
        float v = 0f;

        if (Input.GetKey(KeyCode.A)) h -= 1f;
        if (Input.GetKey(KeyCode.D)) h += 1f;
        if (Input.GetKey(KeyCode.W)) v += 1f;
        if (Input.GetKey(KeyCode.S)) v -= 1f;

        Vector3 input = new Vector3(h, 0f, v);
        input = Vector3.ClampMagnitude(input, 1f);

        // --- CAMERA RELATIVE MOVE ---
        Vector3 moveDir;

        if (topDownCamera != null)
        {
            // Project camera forward onto ground plane
            Vector3 camForward = Vector3.ProjectOnPlane(topDownCamera.transform.forward, Vector3.up);

            // If camera looks straight down, forward collapses -> fallback to camera.up
            if (camForward.sqrMagnitude < 0.0001f)
                camForward = Vector3.ProjectOnPlane(topDownCamera.transform.up, Vector3.up);

            camForward.Normalize();

            Vector3 camRight = Vector3.ProjectOnPlane(topDownCamera.transform.right, Vector3.up).normalized;

            moveDir = (camForward * input.z + camRight * input.x);
        }
        else
        {
            // Fallback: world axes
            moveDir = new Vector3(input.x, 0f, input.z);
        }

        moveDir = Vector3.ClampMagnitude(moveDir, 1f) * moveSpeed;

        // --- GRAVITY ---
        if (_cc.isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;

        _velocity.y -= gravity * Time.deltaTime;

        // --- MOVE ---
        Vector3 finalMove = (moveDir + _velocity) * Time.deltaTime;
        _cc.Move(finalMove);

        // --- ROTATE ---
        if (rotateTowardMove && moveDir.sqrMagnitude > 0.001f)
        {
            Vector3 faceDir = new Vector3(moveDir.x, 0f, moveDir.z);
            Quaternion targetRot = Quaternion.LookRotation(faceDir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }
}
