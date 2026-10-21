namespace MiBackend.Models;

public class DispositivoRed
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int? SedeId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public TipoDispositivoRed Tipo { get; set; }
    public string Fabricante { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string? NumeroSerie { get; set; }
    public string? VersionFirmware { get; set; }
    public Empresa Empresa { get; set; } = null!;
    public Sede? Sede { get; set; }
    public ICollection<InterfazDispositivoRed> Interfaces { get; set; } = [];
}
