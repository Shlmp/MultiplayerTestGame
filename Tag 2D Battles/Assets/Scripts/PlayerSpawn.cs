using UnityEngine;
using Fusion;

public class PlayerSpawn : SimulationBehaviour, IPlayerJoined
{
    public GameObject playerPrefab;
    public void PlayerJoined(PlayerRef player)
    {
        if (player == Runner.LocalPlayer)
        {
            Runner.Spawn(playerPrefab, Vector3.up, Quaternion.identity, player);
        }
    }
}
