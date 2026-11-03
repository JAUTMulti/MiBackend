namespace MiBackend.Models;

public class Servidor
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int? ServidorAnfitrionId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public NaturalezaServidor Naturaleza { get; set; }
    public string Plataforma { get; set; } = string.Empty;
    public string VersionSistemaOperativo { get; set; } = string.Empty;
    public int Nucleos { get; set; }
    public decimal RamGb { get; set; }
    public TipoGestionHardware? TipoGestionHardware { get; set; }
    public string? IpGestionHardware { get; set; }
    public string? DatosOpcionales { get; set; }
    public Empresa Empresa { get; set; } = null!;
    public Servidor? ServidorAnfitrion { get; set; }
    public ICollection<Servidor> ServidoresAlojados { get; set; } = [];
    public ICollection<InterfazRed> InterfacesRed { get; set; } = [];
}
