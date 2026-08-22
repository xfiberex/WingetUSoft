namespace WingetUSoft;

/// <summary>
/// Busca los restos que un desinstalador deja atrás en los directorios habituales de Windows.
/// Solo mira rutas candidatas concretas, derivadas del nombre y el Id del paquete: nunca recorre
/// carpetas del sistema en busca de coincidencias.
/// </summary>
/// <summary>
/// Los seis directorios que el escáner inspecciona. Van juntos en un tipo, y no como una lista, porque
/// los dos barridos usan subconjuntos distintos: el de un nivel mira los seis; el de dos niveles
/// (<c>{base}\{editor}\{app}</c>) deja fuera <c>%LocalAppData%\Programs</c>, donde nadie anida por
/// editor. Existe además para poder apuntarlos a un directorio temporal en las pruebas: antes creaban
/// carpetas reales en el perfil del usuario y un proceso de test muerto dejaba basura ahí.
/// </summary>
internal readonly record struct CleanupBaseDirectories(
    string Roaming,
    string Local,
    string LocalPrograms,
    string ProgramData,
    string ProgramFiles,
    string ProgramFilesX86)
{
    internal static CleanupBaseDirectories FromEnvironment()
    {
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        return new CleanupBaseDirectories(
            Roaming: Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Local: local,
            LocalPrograms: System.IO.Path.Combine(local, "Programs"),
            ProgramData: Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            ProgramFiles: Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            ProgramFilesX86: Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
    }

    internal string[] ForSingleLevel => [Roaming, Local, LocalPrograms, ProgramData, ProgramFiles, ProgramFilesX86];

    internal string[] ForTwoLevels => [ProgramFiles, ProgramFilesX86, Roaming, Local, ProgramData];
}

public static class CleanupScanner
{
    public static Task<List<CleanupItemViewModel>> ScanAsync(
        IEnumerable<WingetPackage> packages,
        CancellationToken ct = default)
        => ScanAsync(packages, CleanupBaseDirectories.FromEnvironment(), ct);

    internal static async Task<List<CleanupItemViewModel>> ScanAsync(
        IEnumerable<WingetPackage> packages,
        CleanupBaseDirectories baseDirectories,
        CancellationToken ct = default)
    {
        var results = new List<CleanupItemViewModel>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var package in packages)
        {
            ct.ThrowIfCancellationRequested();

            foreach (var candidate in GetCandidatePaths(package, baseDirectories))
            {
                ct.ThrowIfCancellationRequested();
                if (!seen.Add(candidate)) continue;

                bool isDir  = Directory.Exists(candidate);
                bool isFile = !isDir && File.Exists(candidate);
                if (!isDir && !isFile) continue;

                long size = isDir
                    ? await Task.Run(() => CalculateDirSize(candidate), ct).ConfigureAwait(false)
                    : TryGetFileSize(candidate);

                results.Add(new CleanupItemViewModel
                {
                    Path        = candidate,
                    IsDirectory = isDir,
                    SizeBytes   = size,
                    DisplaySize = FormatSize(size),
                    PackageName = package.Name
                });
            }
        }

        return results;
    }

    // ---- Generación de rutas candidatas --------------------------------------

    private static IEnumerable<string> GetCandidatePaths(WingetPackage package, CleanupBaseDirectories baseDirectories)
    {
        var baseDirs = baseDirectories.ForSingleLevel;
        var terms    = GetSearchTerms(package);

        // Un nivel: {baseDir}\{término}
        foreach (string baseDir in baseDirs)
        {
            if (string.IsNullOrEmpty(baseDir)) continue;
            foreach (string term in terms)
            {
                if (TryCombineInside(baseDir, term) is { } candidate)
                    yield return candidate;
            }
        }

        // Dos niveles: {baseDir}\{editor}\{aplicación}
        string[] parts = package.Id.Split('.', 2);
        if (parts.Length == 2
            && parts[0].Length >= 3
            && parts[1].Length >= 3)
        {
            foreach (string baseDir in baseDirectories.ForTwoLevels)
            {
                if (string.IsNullOrEmpty(baseDir)) continue;
                if (TryCombineInside(baseDir, parts[0], parts[1]) is { } candidate)
                    yield return candidate;
            }
        }
    }

    // ---- Seguridad de rutas --------------------------------------------------

    /// <summary>
    /// Caracteres que no pueden formar parte del nombre de una carpeta en Windows. Incluye los dos
    /// separadores de ruta y los dos puntos de unidad, que son justo los que permitirían salirse del
    /// directorio base.
    /// </summary>
    private static readonly char[] ForbiddenTermChars =
        [.. System.IO.Path.GetInvalidFileNameChars(), '/', '\\', ':'];

    /// <summary>
    /// Acepta un término solo si puede ser, literalmente, el nombre de una carpeta.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>El nombre y el Id de un paquete no son datos de confianza.</b> Los paquetes llegan de
    /// <c>winget list</c>, que incluye las entradas de Agregar o quitar programas: su
    /// <c>DisplayName</c> lo escribe el instalador de cualquier tercero, no un catálogo curado.
    /// </para>
    /// <para>
    /// Y lo que se hace después con estas rutas es <c>Directory.Delete(recursive: true)</c>
    /// (ver <c>UI/CleanupWindow</c>), así que componerlas sin validar era un borrado fuera de ámbito
    /// esperando a ocurrir: <c>Path.Combine</c> <b>no</b> normaliza <c>..</c> y <b>descarta</b> el
    /// directorio base si el segundo argumento ya trae raíz propia
    /// (<c>Path.Combine(@"C:\a", @"D:\Windows")</c> devuelve <c>D:\Windows</c>).
    /// </para>
    /// <para>
    /// Rechazar no cuesta nada: un nombre con estos caracteres no puede coincidir con una carpeta
    /// real, así que no se pierde ni un candidato legítimo.
    /// </para>
    /// </remarks>
    private static bool IsSafeTerm(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return false;

        if (term.IndexOfAny(ForbiddenTermChars) >= 0)
            return false;

        // "." y ".." son navegación, no nombres; un término compuesto solo de puntos tampoco nombra nada.
        return term.Trim().Trim('.').Length > 0;
    }

    /// <summary>
    /// Compone un candidato bajo <paramref name="baseDir"/> y lo devuelve solo si, ya normalizado,
    /// sigue estando por debajo. Devuelve <c>null</c> si algún segmento no es válido o si la ruta se
    /// sale.
    /// </summary>
    /// <remarks>
    /// Segunda barrera tras <see cref="IsSafeTerm"/>, a propósito: la normalización de Windows
    /// (puntos y espacios finales, nombres de dispositivo reservados) puede mover una ruta que
    /// carácter a carácter parecía inofensiva. Comparar los <c>GetFullPath</c> es lo único que
    /// responde a la pregunta que de verdad importa: ¿esto sigue estando dentro?
    /// </remarks>
    private static string? TryCombineInside(string baseDir, params string[] segments)
    {
        if (string.IsNullOrEmpty(baseDir))
            return null;

        foreach (string segment in segments)
        {
            if (!IsSafeTerm(segment))
                return null;
        }

        try
        {
            string root = System.IO.Path.GetFullPath(baseDir);
            string prefix = root.EndsWith(System.IO.Path.DirectorySeparatorChar)
                ? root
                : root + System.IO.Path.DirectorySeparatorChar;

            string candidate = System.IO.Path.GetFullPath(
                System.IO.Path.Combine([baseDir, .. segments]));

            return candidate.Length > prefix.Length
                && candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                    ? candidate
                    : null;
        }
        catch (ArgumentException)      { return null; }
        catch (PathTooLongException)   { return null; }
        catch (NotSupportedException)  { return null; }
    }

    private static IReadOnlyList<string> GetSearchTerms(WingetPackage package)
    {
        var terms = new List<string>(2);

        // Display name (most reliable match for folder names)
        if (!string.IsNullOrWhiteSpace(package.Name))
        {
            string name = SanitizeName(package.Name);
            if (name.Length >= 3)
                terms.Add(name);
        }

        // App portion of the ID, e.g. "VisualStudioCode" from "Microsoft.VisualStudioCode"
        string[] parts = package.Id.Split('.', 2);
        if (parts.Length == 2 && parts[1].Length >= 3)
        {
            if (!terms.Contains(parts[1], StringComparer.OrdinalIgnoreCase))
                terms.Add(parts[1]);
        }

        return terms;
    }

    private static string SanitizeName(string name)
    {
        // Strip parenthetical suffixes such as "(x64)", "(64-bit)", "(portable)"
        int paren = name.IndexOf('(');
        if (paren > 2)
            name = name[..paren];
        return name.Trim();
    }

    // ---- File system helpers ------------------------------------------------

    private static long CalculateDirSize(string path)
    {
        try
        {
            return new DirectoryInfo(path)
                .EnumerateFiles("*", SearchOption.AllDirectories)
                .Sum(f => { try { return f.Length; } catch { return 0L; } });
        }
        catch { return 0L; }
    }

    private static long TryGetFileSize(string path)
    {
        try { return new FileInfo(path).Length; }
        catch { return 0L; }
    }

    private static string FormatSize(long bytes)
    {
        if (bytes <= 0)           return "—";
        if (bytes < 1_024)        return $"{bytes} B";
        if (bytes < 1_048_576)    return $"{bytes / 1_024.0:F1} KB";
        if (bytes < 1_073_741_824) return $"{bytes / 1_048_576.0:F1} MB";
        return $"{bytes / 1_073_741_824.0:F2} GB";
    }
}
