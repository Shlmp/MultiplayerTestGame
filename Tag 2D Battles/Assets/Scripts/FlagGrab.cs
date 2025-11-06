using UnityEngine;
using Fusion;

public class FlagGrab : NetworkBehaviour
{
    private GameObject player;
    private PlayerInputs playerInputs;

    private void Awake()
    {
        playerInputs = new PlayerInputs();
        playerInputs.Player.Enable();
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        player = collision.gameObject;
        Debug.Log(player + "  --  Detected");

        if (player != null)
        {
            Debug.Log("Parent is now  --  " + player);
            RPC_GrabFlag();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.StateAuthority)]
    private void RPC_GrabFlag()
    {
        transform.SetParent(player.transform);
        transform.position = new Vector2(transform.parent.position.x, transform.parent.position.y + 1);
    }
}