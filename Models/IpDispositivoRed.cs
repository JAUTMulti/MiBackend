namespace MiBackend.Models;

public class IpDispositivoRed
{
    public int Id { get; set; }
    public int InterfazDispositivoRedId { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public InterfazDispositivoRed Interfaz { get; set; } = null!;
}
