namespace MiBackend.Models;

public class Empresa
{
    public int Id { get; set; }
    public string Nit { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public ICollection<Sede> Sedes { get; set; } = [];
}
