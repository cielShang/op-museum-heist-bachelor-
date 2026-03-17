// using System.Collections;
// using UnityEngine;

// [RequireComponent(typeof(Collider))]
// public class P3BypassNodeHold : MonoBehaviour
// {
//     public Phase3SafeRouteManager manager;

//     [Header("Laser Group to Control (SAFE ROUTE)")]
//     public P3LaserGroupPatternController laserGroup;

//     [Header("Hold Settings")]
//     public KeyCode interactKey = KeyCode.E;
//     public float holdSeconds = 2.5f;

//     [Tooltip("Prevents instant re-trigger spam.")]
//     public float cooldownSeconds = 0.5f;

//     [Tooltip("If true: this node becomes completed ONLY when the correct pattern is hit.")]
//     public bool completeOnlyWhenCorrectPattern = true;

//     [Header("UI (optional)")]
//     public CanvasGroup uiGroup;
//     public UnityEngine.UI.Image progressFill;   // Image with Fill Amount (0..1)
//     public TMPro.TMP_Text hintText;

//     [Header("State")]
//     public bool completed;

//     private bool _inside;
//     private float _t;
//     private bool _cooldown;

//     void Reset()
//     {
//         var col = GetComponent<Collider>();
//         col.isTrigger = true;
//     }

//     void Start()
//     {
//         SetUI(false);
//         SetProgress(0f);
//     }

//     public void ResetNode()
//     {
//         completed = false;
//         _inside = false;
//         _t = 0f;
//         _cooldown = false;

//         var col = GetComponent<Collider>();
//         if (col != null) col.enabled = true;

//         foreach (var r in GetComponentsInChildren<Renderer>(true))
//             r.enabled = true;

//         SetUI(false);
//         SetProgress(0f);

//         if (hintText != null)
//             hintText.text = "Hold E to reroute lasers";
//     }

//     void Update()
//     {
//         if (completed) return;
//         if (!_inside) return;
//         if (_cooldown) return;

//         bool holding = Input.GetKey(interactKey);

//         if (holding)
//         {
//             _t += Time.deltaTime;
//             float p = Mathf.Clamp01(_t / holdSeconds);
//             SetProgress(p);

//             if (p >= 1f)
//                 TriggerNode();
//         }
//         else
//         {
//             if (_t > 0f)
//             {
//                 _t = 0f;
//                 SetProgress(0f);
//             }
//         }
//     }

//     void OnTriggerEnter(Collider other)
//     {
//         if (!other.CompareTag("Player")) return;
//         if (completed) return;

//         _inside = true;
//         SetUI(true);
//     }

//     void OnTriggerExit(Collider other)
//     {
//         if (!other.CompareTag("Player")) return;
//         if (completed) return;

//         _inside = false;
//         _t = 0f;
//         SetProgress(0f);
//         SetUI(false);
//     }

//     private void TriggerNode()
//     {
//         // Reset hold progress immediately (so it feels responsive)
//         _t = 0f;
//         SetProgress(0f);

//         if (laserGroup == null)
//         {
//             Debug.LogWarning("[P3BypassNodeHold] No laserGroup assigned.", this);
//             StartCoroutine(Cooldown());
//             return;
//         }

//         // Cycle pattern (and if it becomes the correct one, the controller will drop lasers for downSeconds)
//         laserGroup.CyclePattern();

//         // Update UI text feedback
//         if (hintText != null)
//         {
//             bool isCorrect = (laserGroup.currentPattern == laserGroup.correctPattern);
//             hintText.text = isCorrect
//                 ? "Pattern found! Move now!"
//                 : "Rerouted… try again";
//         }

//         // Optional completion logic:
//         // If you want the node to be "done" only when correct pattern is hit:
//         if (completeOnlyWhenCorrectPattern)
//         {
//             if (laserGroup.currentPattern == laserGroup.correctPattern)
//                 Complete();
//         }

//         StartCoroutine(Cooldown());
//     }

//     private IEnumerator Cooldown()
//     {
//         _cooldown = true;
//         yield return new WaitForSeconds(cooldownSeconds);
//         _cooldown = false;

//         // restore default hint if still inside and not completed
//         if (!completed && _inside && hintText != null)
//             hintText.text = "Hold E to reroute lasers";
//     }

//     private void Complete()
//     {
//         completed = true;
//         _inside = false;

//         SetProgress(1f);
//         SetUI(false);

//         // disable collider so it can't be re-used
//         var col = GetComponent<Collider>();
//         if (col != null) col.enabled = false;

//         // optional: hide visuals
//         foreach (var r in GetComponentsInChildren<Renderer>(true))
//             r.enabled = false;

//         if (manager != null)
//             manager.OnBypassNodeCompleted(this);
//     }

//     private void SetUI(bool on)
//     {
//         if (uiGroup == null) return;

//         uiGroup.alpha = on ? 1f : 0f;
//         uiGroup.blocksRaycasts = on;
//         uiGroup.interactable = on;

//         if (hintText != null)
//             hintText.text = "Hold E to reroute lasers";
//     }

//     private void SetProgress(float v)
//     {
//         if (progressFill != null)
//             progressFill.fillAmount = v;
//     }
// }
