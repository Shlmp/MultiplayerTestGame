using Fusion;
using Fusion.Sockets;
using UnityEngine;

/// <summary>
/// Attach to the player prefab (NetworkObject). Handles mouse-aimed hitscan firing for Rifle & SMG.
/// Shows visual line (raycast bullet) for all clients.
/// </summary>
[RequireComponent(typeof(PlayerNetwork))]
public class WeaponSystem : NetworkBehaviour
{
    public enum WeaponType { Rifle, SMG }

    [Header("Weapon Stats")]
    public float rifleRange = 12f;
    public int rifleDamage = 25;
    public float rifleFireRate = 0.5f;

    public float smgRange = 16f;
    public int smgDamage = 12;
    public float smgFireRate = 0.12f;

    [Header("References")]
    public Transform muzzlePoint;
    public LayerMask hitMask;
    public NetworkObject bulletLinePrefab; // Prefab with LineRenderer + script to auto-destroy

    private float lastFireTime;
    private WeaponType currentWeapon = WeaponType.Rifle;

    private Camera mainCamera;
    private PlayerNetwork pn;

    private void Awake()
    {
        mainCamera = Camera.main;
        pn = GetComponent<PlayerNetwork>();
    }

    public override void FixedUpdateNetwork() { }

    private void Update()
    {
        if (!Runner) return;
        if (!Object.HasInputAuthority) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) currentWeapon = WeaponType.Rifle;
        if (Input.GetKeyDown(KeyCode.Alpha2)) currentWeapon = WeaponType.SMG;

        if (Input.GetMouseButton(0))
        {
            TryFire();
        }
    }

    private void TryFire()
    {
        float now = Time.time;

        if (currentWeapon == WeaponType.Rifle)
        {
            if (now - lastFireTime < rifleFireRate) return;
            lastFireTime = now;

            PerformLocalShot(rifleRange);
            RPC_RequestFire(currentWeapon, mainCamera.ScreenToWorldPoint(Input.mousePosition));
        }
        else if (currentWeapon == WeaponType.SMG)
        {
            if (now - lastFireTime < smgFireRate) return;
            lastFireTime = now;

            PerformLocalShot(smgRange);
            RPC_RequestFire(currentWeapon, mainCamera.ScreenToWorldPoint(Input.mousePosition));
        }
    }

    private void PerformLocalShot(float range)
    {
        Vector3 origin = muzzlePoint ? muzzlePoint.position : transform.position;
        Vector3 targetWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = new Vector2(targetWorld.x - origin.x, targetWorld.y - origin.y).normalized;

        RaycastHit2D hit = Physics2D.Raycast(origin, dir, range, hitMask);

        // Draw local line for feedback (instant)
        Vector3 endPoint = hit.collider ? (Vector3)hit.point : origin + (Vector3)dir * range;
        DrawLocalBulletLine(origin, endPoint);

        // Local feedback (no daño aquí, el StateAuthority valida con RPC)
    }

    private void DrawLocalBulletLine(Vector3 start, Vector3 end)
    {
        // Simple visual using Debug.DrawLine or a temporary LineRenderer
        Debug.DrawLine(start, end, Color.yellow, 0.1f);
    }

    // Client -> StateAuthority: authoritative shot (validated on host)
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_RequestFire(WeaponType weapon, Vector2 mouseWorldPos, RpcInfo info = default)
    {
        var shooterPlayer = info.Source;
        if (!Runner.TryGetPlayerObject(shooterPlayer, out NetworkObject shooterObj)) return;

        var shooter = shooterObj.transform;
        var ws = shooterObj.GetComponent<WeaponSystem>();

        Vector2 origin = ws && ws.muzzlePoint ? ws.muzzlePoint.position : shooter.position;
        Vector2 dir = (mouseWorldPos - origin).normalized;

        float range = weapon == WeaponType.Rifle ? rifleRange : smgRange;
        int damage = weapon == WeaponType.Rifle ? rifleDamage : smgDamage;

        var hit = Physics2D.Raycast(origin, dir, range, hitMask);

        Vector3 endPoint = hit.collider ? (Vector3)hit.point : origin + dir * range;

        // ... dentro de RPC_RequestFire en WeaponSystem, después de spawnear:
        if (bulletLinePrefab != null)
        {
            var spawnedNetObj = Runner.Spawn(bulletLinePrefab, origin, Quaternion.identity);
            if (spawnedNetObj != null)
            {
                var bl = spawnedNetObj.GetComponent<BulletLine>();
                if (bl != null)
                {
                    // Llamada RPC desde StateAuthority -> All para que todos configuren la línea
                    bl.RPC_SetPositions(origin, endPoint);
                }
            }
        }


        // If we hit a player, apply damage
        if (hit.collider != null)
        {
            var hitPlayerObj = hit.collider.GetComponentInParent<NetworkObject>();
            if (hitPlayerObj != null)
            {
                var targetPn = hitPlayerObj.GetComponent<PlayerNetwork>();
                if (targetPn != null)
                {
                    targetPn.ApplyDamage(damage, shooterPlayer);
                }
            }
        }
    }
}
