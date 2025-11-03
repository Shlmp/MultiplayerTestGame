using Fusion;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BulletLine : NetworkBehaviour
{
    private LineRenderer lr;

    public float lifeTime = 0.1f; // tiempo que se muestra la línea

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        if (lr == null)
            lr = gameObject.AddComponent<LineRenderer>();
    }

    public override void Spawned()
    {
        base.Spawned();

        // Si esta instancia es la StateAuthority, programamos el despawn localmente
        // solo la StateAuthority debe llamar Runner.Despawn(Object)
        if (Object.HasStateAuthority)
        {
            // Usamos Invoke para ejecutar DespawnSelf() después de lifeTime segundos
            Invoke(nameof(DespawnSelf), lifeTime);
        }
    }

    // RPC: StateAuthority -> All. Setea la línea en todos los clientes
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SetPositions(Vector3 start, Vector3 end, RpcInfo info = default)
    {
        if (lr == null) lr = GetComponent<LineRenderer>();

        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }

    // Método que ejecuta la StateAuthority para despawnear este NetworkObject
    private void DespawnSelf()
    {
        if (Runner != null && Object != null && Object.HasStateAuthority)
        {
            Runner.Despawn(Object);
        }
    }
}
