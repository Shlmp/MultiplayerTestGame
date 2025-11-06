using Fusion;
using Fusion.Sockets;
using UnityEngine;


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
    public NetworkObject bulletLinePrefab;

    private float lastFireTime;
    private WeaponType currentWeapon = WeaponType.Rifle;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
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

        Vector3 endPoint = hit.collider ? (Vector3)hit.point : origin + (Vector3)dir * range;
        DrawLocalBulletLine(origin, endPoint);

    }

    private void DrawLocalBulletLine(Vector3 start, Vector3 end)
    {
        Debug.DrawLine(start, end, Color.yellow, 0.1f);
    }

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

        if (bulletLinePrefab != null)
        {
            var spawnedNetObj = Runner.Spawn(bulletLinePrefab, origin, Quaternion.identity);
            if (spawnedNetObj != null)
            {
                var bl = spawnedNetObj.GetComponent<BulletLine>();
                if (bl != null)
                {
                    bl.RPC_SetPositions(origin, endPoint);
                }
            }
        }
    }
}
