namespace WingetUSoft;

/// <summary>Un paquete del catálogo de winget, tal como lo devuelve <c>winget search</c>.</summary>
public sealed class WingetSearchResult
{
    public string Name { get; init; } = "";
    public string Id { get; init; } = "";
    public string Version { get; init; } = "";
    public string Source { get; init; } = "";

    /// <summary>True si el paquete ya está instalado en el equipo (se cruza con la lista de instalados).</summary>
    public bool IsInstalled { get; set; }
}
