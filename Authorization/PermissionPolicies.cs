namespace MiBackend.Authorization;

public static class PermissionPolicies
{
    public const string EmpresasRead = "Empresas.Read";
    public const string EmpresasWrite = "Empresas.Write";
    public const string EquiposRedRead = "EquiposRed.Read";
    public const string EquiposRedWrite = "EquiposRed.Write";
    public const string ServidoresRead = "Servidores.Read";
    public const string ServidoresWrite = "Servidores.Write";
    public const string ProductosRead = "Productos.Read";
    public const string ProductosWrite = "Productos.Write";
    public const string ManageUsers = "Users.Manage";
    public const string PermissionClaim = "permission";

    public static readonly string[] ReadPermissions =
    [
        EmpresasRead,
        EquiposRedRead,
        ServidoresRead,
        ProductosRead
    ];

    public static readonly string[] WritePermissions =
    [
        EmpresasWrite,
        EquiposRedWrite,
        ServidoresWrite,
        ProductosWrite
    ];
}
