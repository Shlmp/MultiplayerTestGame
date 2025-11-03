using Fusion;
using UnityEngine;

/// <summary>
/// Estado de jugador en red. Ya no almacena NetworkId de la bandera.
/// Para saber si un jugador lleva una bandera se consulta FlagController.GetFlagHeldBy(PlayerRef).
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class PlayerNetwork : NetworkBehaviour
{
    [Networked] public Team Team { get; set; }
    [Networked] public int Health { get; set; }

    [Header("Local references")]
    public GameObject gunHolder; // visual only
    public Transform flagHoldPoint; // where the flag should visually attach when held

    private void Awake()
    {
        Team = Team.None;
        Health = 100;
    }

    public override void Spawned()
    {
        base.Spawned();
    }

    public void SetTeam(Team newTeam)
    {
        Team = newTeam;
        UpdateTeamVisual();
    }

    private void UpdateTeamVisual()
    {
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr)
        {
            if (Team == Team.Red) sr.color = Color.red;
            else if (Team == Team.Blue) sr.color = Color.cyan;
            else sr.color = Color.white;
        }
    }

    // Nota: el Source/Authority que llame a ApplyDamage debe ser el StateAuthority o un RPC validado por StateAuthority.
    public void ApplyDamage(int dmg, PlayerRef attacker)
    {
        Health -= dmg;
        if (Health <= 0)
        {
            Health = 0;
            OnDie();
        }
    }

    private void OnDie()
    {
        // Cuando muere, buscar si existe alguna bandera cuyo OwnerPlayer == este jugador
        var flag = FlagController.GetFlagHeldBy(Object.InputAuthority);
        if (flag != null)
        {
            // Instrucción para que la bandera haga el comportamiento de caída (se lanzará a StateAuthority por RPC
            // dentro del FlagController)
            flag.Server_DropOnDeath(Object.InputAuthority);
        }

        // Aquí puedes lanzar respawn, animaciones de muerte, etc.
    }
}
