namespace MiBackend.Models;

public class InterfazDispositivoRed
{
    public int Id { get; set; }
    public int DispositivoRedId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? DireccionMac { get; set; }
    public DispositivoRed Dispositivo { get; set; } = null!;
    public ICollection<IpDispositivoRed> DireccionesIp { get; set; } = [];
}
