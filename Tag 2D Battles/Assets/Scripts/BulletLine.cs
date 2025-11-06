using Fusion;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BulletLine : NetworkBehaviour
{
    private LineRenderer lr;

    public float lifeTime = 0.1f;

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        if (lr == null)
            lr = gameObject.AddComponent<LineRenderer>();
    }

    public override void Spawned()
    {
        base.Spawned();

        if (Object.HasStateAuthority)
        {
            Invoke(nameof(DespawnSelf), lifeTime);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SetPositions(Vector3 start, Vector3 end, RpcInfo info = default)
    {
        if (lr == null) lr = GetComponent<LineRenderer>();

        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }

    private void DespawnSelf()
    {
        if (Runner != null && Object != null && Object.HasStateAuthority)
        {
            Runner.Despawn(Object);
        }
    }
}
