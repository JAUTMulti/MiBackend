namespace MiBackend.Models;

public class DireccionIp
{
    public int Id { get; set; }
    public int InterfazRedId { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public InterfazRed InterfazRed { get; set; } = null!;
}
