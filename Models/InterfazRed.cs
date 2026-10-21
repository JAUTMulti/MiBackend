namespace MiBackend.Models;

public class InterfazRed
{
    public int Id { get; set; }
    public int ServidorId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? DireccionMac { get; set; }
    public Servidor Servidor { get; set; } = null!;
    public ICollection<DireccionIp> DireccionesIp { get; set; } = [];
}
