using UnityEngine;
using Convai.Scripts.Runtime.Core;

[RequireComponent(typeof(CharacterController))]
public class JumpLandingDetector : MonoBehaviour
{
    

    [Header("Landing Settings")]
    public Collider safeLandingZone;
    public ConvaiNPC sakuraNPC;
    public float landingMinHeight = 1.5f;
    public SakuraFollowPlayer sakuraFollow;   // assign in Inspector

    [Header("Drone Reaction")]
    public DroneNoiseResponder floorDrone;   // floor patrol drone that investigates

    [Header("Debug")]
    public bool debugLogs = true;

    [Header("Activation Gate")]
    public bool enableNoiseSystem = false; // only true during the risky jump section

    


    private CharacterController _controller;
    private bool _wasGrounded = true;
    private bool _landingHandled = false;
    private float _highestYWhileAirborne;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        bool isGrounded = _controller.isGrounded;

        // left the ground -> start tracking jump
        if (_wasGrounded && !isGrounded)
        {
            _landingHandled = false;
            _highestYWhileAirborne = transform.position.y;
            if (debugLogs) Debug.Log("[JumpLandingDetector] Left ground, starting jump.");
        }

        // track highest point while in air
        if (!isGrounded)
        {
            if (transform.position.y > _highestYWhileAirborne)
                _highestYWhileAirborne = transform.position.y;
        }

        // landed
        if (!_wasGrounded && isGrounded && !_landingHandled)
        {
            _landingHandled = true;
            float fallHeight = _highestYWhileAirborne - transform.position.y;

            if (debugLogs) Debug.Log($"[JumpLandingDetector] Landed. Fall height = {fallHeight:0.00}");

            if (fallHeight >= landingMinHeight)
            {
                HandleLanding();
            }
        }

        _wasGrounded = isGrounded;
    }
    public void SetNoiseSystemEnabled(bool enabled)
        {
            enableNoiseSystem = enabled;
        }

    private void HandleLanding()
    {
        if (!enableNoiseSystem) return;
        
        if (safeLandingZone == null)
        {
            if (debugLogs) Debug.LogWarning("[JumpLandingDetector] No safeLandingZone assigned.");
            return;
        }

        Vector3 feetPos = transform.position;
        feetPos.y = safeLandingZone.bounds.center.y;

        bool inSafeZone = safeLandingZone.bounds.Contains(feetPos);

        if (debugLogs)
        {
            Debug.Log($"[JumpLandingDetector] Landing position {feetPos} - inSafeZone = {inSafeZone}");
        }

        if (inSafeZone)
            OnSafeLanding();
        else
            OnNoisyLanding();
    }

private void OnSafeLanding()
{
    // Prevent re-firing if already BAD
    if (JumpOutcomeState.Current == JumpOutcomeState.Outcome.Bad)
    {
        if (debugLogs)
            Debug.Log("[JumpLandingDetector] Safe landing ignored (outcome already BAD).");
        return;
    }

    JumpOutcomeState.Current = JumpOutcomeState.Outcome.Good;

    if (debugLogs) Debug.Log("[JumpLandingDetector] Safe landing.");

    if (sakuraFollow != null)
        sakuraFollow.SetFollowEnabled(true);

    if (sakuraNPC != null)
    {
        sakuraNPC.TriggerSpeech(
            "[PHASE2_JUMP] You and the player just landed a risky jump..."
        );
    }
}



private void OnNoisyLanding()
{
    //  If outcome already decided as GOOD, do nothing
    if (JumpOutcomeState.Current == JumpOutcomeState.Outcome.Good)
    {
        if (debugLogs)
            Debug.Log("[JumpLandingDetector] Noisy landing ignored (outcome already GOOD).");
        return;
    }

    if (debugLogs) Debug.Log("[JumpLandingDetector] Noisy landing – drone will investigate.");

    JumpOutcomeState.Current = JumpOutcomeState.Outcome.Bad;

    if (sakuraFollow != null)
        sakuraFollow.SetFollowEnabled(true);

    if (sakuraNPC != null)
    {
        sakuraNPC.TriggerSpeech(
            "[WHISPER][PANIC] Too loud—floor drone’s coming. Get behind cover, now."
        );
    }

    if (floorDrone != null)
        floorDrone.InvestigateNoise(transform.position);
}

}
