using UnityEngine;

[RequireComponent(typeof(Collider))]
public class P3SecurityNode : MonoBehaviour
{
   // public Phase3RiskyTopDownManager manager;
    public string playerTag = "Player";
    public KeyCode interactKey = KeyCode.E;

    public bool completed;

    private bool _inside;

    void Reset()
    {
      //  var c = GetComponent<Collider>();
       // c.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag)) _inside = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag)) _inside = false;
    }

    void Update()
    {
        if (completed) return;
        if (!_inside) return;

        if (Input.GetKeyDown(interactKey))
        {
            completed = true;
           // if (manager != null) manager.OnNodeCompleted(this);

            // optional: visual feedback
            gameObject.SetActive(false);
        }
    }
}
