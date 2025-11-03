using Fusion;
using UnityEngine;

public class BaseZone : MonoBehaviour
{
    public Team baseTeam; // Asigna en el Inspector (Red o Blue)

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerNetwork>();
        if (player == null) return;

        // Comprobar si este jugador lleva una bandera
        var flag = FlagController.GetFlagHeldBy(player.Object.InputAuthority);
        if (flag != null)
        {
            // Llamar para anotar
            flag.LocalAttemptDropAtBase(player.Object.InputAuthority, baseTeam);
        }
    }
}
