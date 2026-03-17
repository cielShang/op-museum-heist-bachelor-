using UnityEngine;

public class GameStartManager : MonoBehaviour
{
    [Header("Spawn References")]
    public Transform playerSpawn;
    public Transform npcSpawn;

    [Header("Character References")]
    public GameObject player;
    public GameObject npc;

    void Start()
    {
        SpawnCharacters();
    }

    void SpawnCharacters()
    {
        if (player != null && playerSpawn != null)
        {
            player.transform.position = playerSpawn.position;
            player.transform.rotation = playerSpawn.rotation;
        }

        if (npc != null && npcSpawn != null)
        {
            npc.transform.position = npcSpawn.position;
            npc.transform.rotation = npcSpawn.rotation;
        }
    }
}