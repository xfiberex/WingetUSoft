namespace WingetUSoft;

/// <summary>
/// "Omitir esta versión": el usuario descarta **una versión concreta** de un paquete, y el paquete
/// vuelve a aparecer solo cuando salga otra distinta. Lógica pura y testeable.
/// </summary>
/// <remarks>
/// <para>
/// <b>Por qué no se usa <c>winget pin</c>.</b> Winget no tiene esta operación. Sus anclajes son otra
/// cosa: <c>pin add</c> excluye el paquete de <c>upgrade --all</c> (para siempre, hasta quitarlo),
/// <c>--blocking</c> lo bloquea del todo, y <c>--version</c> es un anclaje de *rango* (solo permite
/// actualizar dentro de él). Anclar a la versión disponible de hoy para "saltársela" dejaría el paquete
/// congelado **también para las versiones futuras**, que es justo lo contrario de omitir una y seguir
/// recibiendo las siguientes. Por eso se resuelve en la app, como hacen los actualizadores que sí tienen
/// "Omitir esta versión" (Chrome, Sparkle): se recuerda la versión descartada y se compara.
/// </para>
/// <para>
/// No confundir con la <b>lista de exclusiones</b> (<c>AppSettings.ExcludedIds</c>), que es permanente y
/// para el paquete entero. Omitir es temporal y para una versión.
/// </para>
/// </remarks>
public static class SkippedVersions
{
    /// <summary>
    /// True si <paramref name="availableVersion"/> es exactamente la versión que el usuario omitió para
    /// ese paquete. Si winget ofrece otra distinta (una más nueva), devuelve false: la omisión caduca
    /// sola, sin que el usuario tenga que acordarse de retirarla.
    /// </summary>
    public static bool IsSkipped(
        IReadOnlyDictionary<string, string>? skipped,
        string? packageId,
        string? availableVersion)
    {
        if (skipped is null || string.IsNullOrWhiteSpace(packageId) || string.IsNullOrWhiteSpace(availableVersion))
            return false;

        if (!skipped.TryGetValue(packageId.Trim(), out string? skippedVersion))
            return false;

        return SameVersion(skippedVersion, availableVersion);
    }

    /// <summary>Marca esa versión como omitida (sustituye la omisión previa del paquete, si la había).</summary>
    public static void Skip(IDictionary<string, string> skipped, string packageId, string availableVersion)
    {
        ArgumentNullException.ThrowIfNull(skipped);
        if (string.IsNullOrWhiteSpace(packageId) || string.IsNullOrWhiteSpace(availableVersion))
            return;

        skipped[packageId.Trim()] = availableVersion.Trim();
    }

    /// <summary>Retira la omisión del paquete. No falla si no había ninguna.</summary>
    public static void Unskip(IDictionary<string, string> skipped, string packageId)
    {
        ArgumentNullException.ThrowIfNull(skipped);
        if (string.IsNullOrWhiteSpace(packageId))
            return;

        skipped.Remove(packageId.Trim());
    }

    /// <summary>
    /// Limpia las omisiones que ya no aplican: el paquete desapareció de la lista de actualizaciones (se
    /// actualizó por otra vía o se desinstaló), o winget ofrece ya otra versión. Sin esto,
    /// <c>settings.json</c> acumularía omisiones muertas para siempre.
    /// </summary>
    /// <returns>Número de omisiones retiradas.</returns>
    public static int Prune(IDictionary<string, string> skipped, IEnumerable<WingetPackage> currentPackages)
    {
        ArgumentNullException.ThrowIfNull(skipped);
        ArgumentNullException.ThrowIfNull(currentPackages);

        var live = currentPackages
            .Where(p => !string.IsNullOrWhiteSpace(p.Id))
            .ToDictionary(p => p.Id.Trim(), p => p.Available ?? "", StringComparer.OrdinalIgnoreCase);

        var stale = skipped
            .Where(kv => !live.TryGetValue(kv.Key, out string? available) || !SameVersion(kv.Value, available))
            .Select(kv => kv.Key)
            .ToList();

        foreach (string id in stale)
            skipped.Remove(id);

        return stale.Count;
    }

    private static bool SameVersion(string? a, string? b) =>
        string.Equals((a ?? "").Trim(), (b ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
}
