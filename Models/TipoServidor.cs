namespace MiBackend.Models;

/// <summary>Clasificación del servidor en el inventario de infraestructura.</summary>
public enum TipoServidor
{
    /// <summary>Host de virtualización Proxmox.</summary>
    Proxmox,

    /// <summary>Máquina virtual alojada en un hipervisor.</summary>
    Virtual,

    /// <summary>Servidor físico independiente.</summary>
    Fisico
}
