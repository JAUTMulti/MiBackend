namespace MiBackend.Models;

public class Servidor
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int? ServidorProxmoxId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public TipoServidor Tipo { get; set; }
    public string SistemaOperativo { get; set; } = string.Empty;
    public string VersionSistemaOperativo { get; set; } = string.Empty;
    public int Nucleos { get; set; }
    public decimal RamGb { get; set; }
    public Empresa Empresa { get; set; } = null!;
    public Servidor? ServidorProxmox { get; set; }
    public ICollection<Servidor> MaquinasVirtuales { get; set; } = [];
    public ICollection<InterfazRed> InterfacesRed { get; set; } = [];
}
