namespace MiBackend.Models;

public class Empresa
{
    public int Id { get; set; }
    public string Nit { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool Habilitada { get; set; } = true;
    public ICollection<Sede> Sedes { get; set; } = [];
    public ICollection<Servidor> Servidores { get; set; } = [];
    public ICollection<DispositivoRed> DispositivosRed { get; set; } = [];
}
