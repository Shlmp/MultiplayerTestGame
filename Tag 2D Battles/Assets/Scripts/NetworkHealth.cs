using Fusion;
using UnityEngine;

public class NetworkHealth : NetworkBehaviour
{
    private PlayerNetwork pn;

    private void Awake()
    {
        pn = GetComponent<PlayerNetwork>();
    }

    public void ApplyDamageFromAuthority(int dmg, PlayerRef attacker)
    {
        // This should be invoked on the state authority or via RPC. We're using PlayerNetwork.ApplyDamage in our sample.
        pn.ApplyDamage(dmg, attacker);
    }
}
