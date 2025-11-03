using UnityEngine;
using Fusion;

public class PlayerSpawn : SimulationBehaviour, IPlayerJoined
{
    [Header("Prefabs")]
    public NetworkObject playerPrefab;
    public NetworkObject teamManagerPrefab;

    public void PlayerJoined(PlayerRef player)
    {
        // Spawnear al jugador local
        if (player == Runner.LocalPlayer)
        {
            Runner.Spawn(playerPrefab, Vector3.up, Quaternion.identity, player);
        }

        // Solo el cliente con StateAuthority global debe spawnear el TeamManager una sola vez.
        // En modo Shared, este cliente suele ser el primero en unirse.
        if (Runner.IsSharedModeMasterClient)
        {
            // Verificar que no exista ya un TeamManager
            if (TeamManager.Instance == null)
            {
                Runner.Spawn(teamManagerPrefab, Vector3.zero, Quaternion.identity, null);
            }
        }
    }
}
