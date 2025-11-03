using Fusion;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Controlador de la bandera (círculo). Debe estar en un prefab NetworkObject con NetworkTransform.
/// OwnerPlayer es [Networked] (PlayerRef) indicando quién la porta.
/// Mantiene una lista estática allFlags para búsquedas sencillas desde PlayerNetwork, etc.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class FlagController : NetworkBehaviour
{
    // Quién la porta (PlayerRef.None si nadie)
    [Networked] public PlayerRef OwnerPlayer { get; set; }

    // Timer para retorno cuando se droppea
    [Networked] public float DropReturnTime { get; set; }

    public float defaultReturnSeconds = 5f;

    private Vector3 originalPosition;
    private Quaternion originalRotation;

    // Lista estática para localizar banderas
    private static List<FlagController> allFlags = new List<FlagController>();

    private void Awake()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
    }

    private void OnEnable() => RegisterFlag();
    private void OnDisable() => UnregisterFlag();

    private void RegisterFlag()
    {
        if (!allFlags.Contains(this)) allFlags.Add(this);
    }

    private void UnregisterFlag()
    {
        if (allFlags.Contains(this)) allFlags.Remove(this);
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        // Si está siendo cargada, actualizar visualmente su posición con el hold point del jugador (sólo para clientes)
        if (OwnerPlayer != PlayerRef.None)
        {
            if (Runner.TryGetPlayerObject(OwnerPlayer, out NetworkObject playerObj))
            {
                var pn = playerObj.GetComponent<PlayerNetwork>();
                if (pn != null && pn.flagHoldPoint != null)
                {
                    transform.position = pn.flagHoldPoint.position;
                }
            }
        }
        else
        {
            // Si tiene timer de retorno, descontarlo
            if (DropReturnTime > 0f)
            {
                DropReturnTime -= Runner.DeltaTime;
                if (DropReturnTime <= 0f)
                {
                    ReturnToOrigin();
                }
            }
        }
    }

    // Intento local de pickup -> se hace RPC al StateAuthority para validar y asignar OwnerPlayer
    public void LocalAttemptPickup(PlayerRef requester)
    {
        RPC_RequestPickup(requester);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void RPC_RequestPickup(PlayerRef requester, RpcInfo info = default)
    {
        if (OwnerPlayer != PlayerRef.None) return; // ya la porta alguien
        OwnerPlayer = requester;
        DropReturnTime = 0f;

        // Notificar a todos quién la recogió (pasamos PlayerRef)
        RPC_OnPickedUp(requester);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_OnPickedUp(PlayerRef player, RpcInfo info = default)
    {
        // Obtener el objeto de jugador para actualizar UI/estado local si hace falta
        if (Runner.TryGetPlayerObject(player, out NetworkObject playerObj))
        {
            var pn = playerObj.GetComponent<PlayerNetwork>();
            if (pn != null)
            {
                // Podríamos registrar localmente que el jugador porta la bandera; aquí actuamos visualmente
                // (el OwnerPlayer networked ya está sincronizado, y la FixedUpdateNetwork hace el follow)
            }
        }
    }

    // Llamado por jugador cuando entra a su base para intentar anotar
    public void LocalAttemptDropAtBase(PlayerRef requester, Team baseTeam)
    {
        RPC_RequestDropToBase(requester, baseTeam);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void RPC_RequestDropToBase(PlayerRef requester, Team baseTeam, RpcInfo info = default)
    {
        if (OwnerPlayer != requester) return;

        if (Runner.TryGetPlayerObject(requester, out NetworkObject playerObj))
        {
            var pn = playerObj.GetComponent<PlayerNetwork>();
            if (pn != null && pn.Team == baseTeam)
            {
                // Puntuar: (aquí deberías notificar un ScoreManager). Simplificamos:
                OwnerPlayer = PlayerRef.None;
                DropReturnTime = 0f;

                RPC_OnDroppedAndScored();
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_OnDroppedAndScored(RpcInfo info = default)
    {
        // reproducir VFX/sonido local si se desea
        ReturnToOrigin();
    }

    // Llamado por la lógica de muerte para que la bandera caiga en el punto actual y comience el timer
    public void Server_DropOnDeath(PlayerRef diedPlayer)
    {
        RPC_ServerDropOnDeath(diedPlayer);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void RPC_ServerDropOnDeath(PlayerRef diedPlayer, RpcInfo info = default)
    {
        if (OwnerPlayer != diedPlayer) return;

        // Liberar propiedad y poner timer de retorno (5s)
        OwnerPlayer = PlayerRef.None;
        DropReturnTime = defaultReturnSeconds;

        // Dejamos la bandera en su posición actual (la transform ya está sincronizada por NetworkTransform)
        RPC_OnDroppedAtPosition(transform.position);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_OnDroppedAtPosition(Vector3 pos, RpcInfo info = default)
    {
        transform.position = pos;
    }

    private void ReturnToOrigin()
    {
        OwnerPlayer = PlayerRef.None;
        DropReturnTime = 0f;
        transform.position = originalPosition;
        transform.rotation = originalRotation;
    }

    // Helper público: devuelve la bandera (FlagController) sostenida por given PlayerRef, o null
    public static FlagController GetFlagHeldBy(PlayerRef who)
    {
        foreach (var f in allFlags)
        {
            if (f != null && f.OwnerPlayer == who)
                return f;
        }
        return null;
    }
}
