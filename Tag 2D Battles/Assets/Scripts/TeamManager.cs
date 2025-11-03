using Fusion;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// TeamManager: debe existir como NetworkObject en la escena y .Spawn() con StateAuthority.
/// Mantiene listas internas en StateAuthority para contar miembros y asignar.
/// UI: llamar a TeamManager.Instance.RequestJoinFromLocalPlayer() cuando el jugador pulse Join.
/// </summary>
public class TeamManager : NetworkBehaviour
{
    public static TeamManager Instance { get; private set; }

    // Estas listas sólo se mantienen en el objeto que tiene StateAuthority (el dueño de la lógica)
    // No son Networked porque solo la StateAuthority necesita la fuente de verdad para asignaciones.
    private List<PlayerRef> redPlayers = new List<PlayerRef>();
    private List<PlayerRef> bluePlayers = new List<PlayerRef>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public override void Spawned()
    {
        base.Spawned();
        Instance = this;
    }

    // Llamar desde el cliente local (UI) cuando pulsa Join
    public void RequestJoinFromLocalPlayer()
    {
        if (Runner == null) return;
        var myPlayerRef = Runner.LocalPlayer;
        RPC_RequestJoin(myPlayerRef);
    }

    // RPC del cliente -> StateAuthority: pide asignación
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_RequestJoin(PlayerRef requester, RpcInfo info = default)
    {
        // Sólo la StateAuthority decide. Contamos usando nuestras listas internas.
        int redCount = redPlayers.Count;
        int blueCount = bluePlayers.Count;

        Team assign = Team.Blue;
        if (redCount <= blueCount) assign = Team.Red;
        else assign = Team.Blue;

        // Añadir el PlayerRef a la lista correspondiente (si no está ya)
        if (assign == Team.Red)
        {
            if (!redPlayers.Contains(requester))
            {
                // en caso de que el jugador hubiera estado en blue, quitarlo
                bluePlayers.Remove(requester);
                redPlayers.Add(requester);
            }
        }
        else
        {
            if (!bluePlayers.Contains(requester))
            {
                redPlayers.Remove(requester);
                bluePlayers.Add(requester);
            }
        }

        // Notificar a todos los clientes la asignación (pasamos PlayerRef para encontrar el objeto de jugador)
        RPC_NotifyAssigned(requester, assign);
    }

    // RPC StateAuthority -> All: notifica la asignación
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_NotifyAssigned(PlayerRef player, Team assigned, RpcInfo info = default)
    {
        // Encontrar el objeto player en el Runner
        if (Runner.TryGetPlayerObject(player, out NetworkObject playerObj))
        {
            var pn = playerObj.GetComponent<PlayerNetwork>();
            if (pn != null)
            {
                pn.SetTeam(assigned);
            }
        }
    }

    // (Opcional) método para cuando un jugador desconecta - hay que quitarlo de las listas si lo deseas.
    public void RemovePlayerFromTeams(PlayerRef player)
    {
        redPlayers.Remove(player);
        bluePlayers.Remove(player);
    }
}
