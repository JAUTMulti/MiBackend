namespace MiBackend.Models;

public class Sede
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public Empresa Empresa { get; set; } = null!;
    public ICollection<DispositivoRed> DispositivosRed { get; set; } = [];
}
