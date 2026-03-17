using UnityEngine;

public static class NPCFollowController
{
    public static void ResumeFollowing()
    {
        var npc = NPCBootstrapper.ActiveNPC;
        if (npc == null) return;

        var follow = npc.GetComponent<SakuraFollowPlayer>();
        if (follow == null) return;

        follow.SetTalking(false);
        follow.SetFollowEnabled(true);

        Debug.Log("[NPCFollowController] NPC follow resumed.");
    }

    public static void StopFollowing()
    {
        var npc = NPCBootstrapper.ActiveNPC;
        if (npc == null) return;

        var follow = npc.GetComponent<SakuraFollowPlayer>();
        if (follow == null) return;

        follow.SetFollowEnabled(false);

        Debug.Log("[NPCFollowController] NPC follow stopped.");
    }
}
