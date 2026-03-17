using UnityEngine;
using System.Collections; 
using Convai.Scripts.Runtime.Core;

public class StartDynamicIntro : MonoBehaviour
{
    public ConvaiNPC convaiNPC;

    void Start()
    {
        if (convaiNPC == null)
            convaiNPC = GetComponent<ConvaiNPC>();

        StartCoroutine(BeginDialogue());
    }

    IEnumerator BeginDialogue()
    {
        yield return new WaitForSeconds(2f);
        convaiNPC.TriggerSpeech("Start talking to the player about your current situation and what to do next.");
    }
}
