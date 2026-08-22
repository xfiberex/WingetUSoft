using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// El escáner de residuos. Todas las carpetas de prueba viven **dentro de un directorio temporal
/// propio de cada test** (T3-13).
/// </summary>
/// <remarks>
/// Antes creaban carpetas reales en <c>%LOCALAPPDATA%</c>. Estaban protegidas con <c>try/finally</c>,
/// pero un proceso de test que muriera —o un `dotnet test` interrumpido con Ctrl+C— dejaba basura en el
/// perfil del usuario. Ahora el escáner recibe sus seis directorios base como parámetro y aquí se le
/// apuntan a subcarpetas de un temporal que se borra entero al final; el `IDisposable` de cada instancia
/// de la clase de test es lo que xUnit ejecuta aunque el test falle.
/// </remarks>
public sealed class CleanupScannerTests : IDisposable
{
    private readonly string _root;
    private readonly CleanupBaseDirectories _dirs;

    public CleanupScannerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "WingetUSoft.ScanTests", Guid.NewGuid().ToString("N"));

        string local = Sub("Local");
        _dirs = new CleanupBaseDirectories(
            Roaming: Sub("Roaming"),
            Local: local,
            LocalPrograms: Sub(Path.Combine("Local", "Programs")),
            ProgramData: Sub("ProgramData"),
            ProgramFiles: Sub("ProgramFiles"),
            ProgramFilesX86: Sub("ProgramFilesX86"));
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private string Sub(string relative)
    {
        string path = Path.Combine(_root, relative);
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>Crea una carpeta bajo <c>Local</c>, que es el directorio base de un solo nivel más usado aquí.</summary>
    private string CreateInLocal(string name)
    {
        string path = Path.Combine(_dirs.Local, name);
        Directory.CreateDirectory(path);
        return path;
    }

    private Task<List<CleanupItemViewModel>> ScanAsync(params WingetPackage[] packages)
        => CleanupScanner.ScanAsync(packages, _dirs);

    [Fact]
    public async Task ScanAsync_EmptyPackageList_ReturnsEmpty()
    {
        var results = await ScanAsync();
        Assert.Empty(results);
    }

    [Fact]
    public async Task ScanAsync_CancellationRequested_ThrowsOperationCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            CleanupScanner.ScanAsync([new WingetPackage { Name = "Foo", Id = "Pub.Foo" }], _dirs, cts.Token));
    }

    [Fact]
    public async Task ScanAsync_ExistingDirectoryMatchingPackageName_IsFound()
    {
        const string dirName = "TestApp";
        string testDir = CreateInLocal(dirName);

        var results = await ScanAsync(new WingetPackage { Name = dirName, Id = $"Publisher.{dirName}" });

        Assert.Contains(results, r => string.Equals(r.Path, testDir, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ScanAsync_NonExistentPaths_ReturnsEmpty()
    {
        var results = await ScanAsync(new WingetPackage
        {
            Name = "NonExistentApp",
            Id = "Nobody.NonExistentApp"
        });

        Assert.Empty(results);
    }

    [Fact]
    public async Task ScanAsync_SamePathFromMultiplePackages_ReportedOnce()
    {
        const string dirName = "SharedApp";
        string testDir = CreateInLocal(dirName);

        // Dos paquetes cuyo nombre e Id resuelven al mismo candidato.
        var results = await ScanAsync(
            new WingetPackage { Name = dirName, Id = $"Publisher.{dirName}" },
            new WingetPackage { Name = dirName, Id = $"OtherPub.{dirName}" });

        int matches = results.Count(r => string.Equals(r.Path, testDir, StringComparison.OrdinalIgnoreCase));

        Assert.Equal(1, matches);
    }

    // ── Contención de rutas (T0-01) ─────────────────────────────────────────
    //
    // El nombre y el Id de un paquete NO son datos de confianza: llegan de `winget list`, que incluye
    // las entradas de Agregar o quitar programas, y ese DisplayName lo escribe el instalador de
    // cualquier tercero. Como lo que se hace después con estas rutas es Directory.Delete(recursive),
    // un candidato que se salga del directorio base es un borrado fuera de ámbito.

    [Theory]
    [InlineData(@"C:\Windows")]              // ruta con raíz: Path.Combine DESCARTA el directorio base
    [InlineData(@"\\server\share")]          // UNC: mismo efecto
    [InlineData(@"..\..\..\..\..\Windows")]  // travesía: Path.Combine NO normaliza ".."
    [InlineData(@"a\b")]                     // separador incrustado
    [InlineData("..")]
    [InlineData("...")]
    public async Task ScanAsync_NameEscapesTheBaseDirectory_ProducesNoCandidate(string maliciousName)
    {
        var results = await ScanAsync(
            new WingetPackage { Name = maliciousName, Id = $"Publisher.{maliciousName}" });

        Assert.True(
            results.Count == 0,
            "Ningún término capaz de salirse del directorio base debe producir un candidato. Se obtuvo: "
                + string.Join(" | ", results.Select(r => r.Path)));
    }

    /// <summary>
    /// El caso que de verdad importa: una carpeta que **existe** y a la que solo se llega escapando del
    /// directorio base. Sin la validación, el escáner la ofrecía como residuo eliminable.
    /// </summary>
    [Fact]
    public async Task ScanAsync_ExistingDirectoryReachableOnlyByTraversal_IsNotReported()
    {
        const string dirName = "EscapeTarget";
        string outsideDir = CreateInLocal(dirName);

        // "Local\Programs" es uno de los directorios base. Con "..\<dir>" se sale de Programs y
        // aterriza en la carpeta recién creada bajo Local, que sí existe.
        var results = await ScanAsync(
            new WingetPackage { Name = $@"..\{dirName}", Id = $@"Publisher...\{dirName}" });

        // Se comparan las rutas NORMALIZADAS a propósito: sin la corrección, el candidato llegaba
        // como "…\Programs\..\EscapeTarget", que no es igual carácter a carácter al directorio de
        // destino aunque apunte exactamente a él. Comparar las cadenas en crudo dejaba pasar el test
        // sin probar nada.
        Assert.DoesNotContain(results, r =>
            string.Equals(Path.GetFullPath(r.Path), outsideDir, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// La contraprueba: la validación no puede llevarse por delante los nombres normales. Sin esto, un
    /// filtro demasiado agresivo pasaría todos los tests de seguridad dejando la función inútil.
    /// </summary>
    [Fact]
    public async Task ScanAsync_OrdinaryNameWithSpacesAndPunctuation_IsStillFound()
    {
        const string dirName = "Mi Programa - Edición 2026";
        string testDir = CreateInLocal(dirName);

        var results = await ScanAsync(new WingetPackage { Name = dirName, Id = $"Publisher.{dirName}" });

        Assert.Contains(results, r => string.Equals(r.Path, testDir, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ScanAsync_FoundItem_IsNotSelectedByDefault()
    {
        const string dirName = "DefaultSelTest";
        string testDir = CreateInLocal(dirName);

        var results = await ScanAsync(new WingetPackage { Name = dirName, Id = $"Pub.{dirName}" });

        var item = results.FirstOrDefault(r => string.Equals(r.Path, testDir, StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(item);
        Assert.False(item.IsSelected, "Los residuos NUNCA vienen preseleccionados: el borrado es irreversible.");
    }

    /// <summary>
    /// El barrido de dos niveles (<c>{base}\{editor}\{app}</c>), que es el que encuentra lo que instala
    /// un editor bajo su propia carpeta.
    /// </summary>
    [Fact]
    public async Task ScanAsync_PublisherAndAppFromTheId_IsFound()
    {
        string nested = Path.Combine(_dirs.ProgramFiles, "Publisher", "SomeApp");
        Directory.CreateDirectory(nested);

        var results = await ScanAsync(new WingetPackage { Name = "Nada que ver", Id = "Publisher.SomeApp" });

        Assert.Contains(results, r => string.Equals(r.Path, nested, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// El escáner no puede salirse de los directorios que se le dan. Es lo que hace que este test se
    /// pueda ejecutar sin tocar el perfil real, y a la vez lo que garantiza que en producción no mire
    /// donde no debe.
    /// </summary>
    [Fact]
    public async Task ScanAsync_NeverReportsAnythingOutsideItsBaseDirectories()
    {
        CreateInLocal("AlgoQueExiste");

        var results = await ScanAsync(
            new WingetPackage { Name = "AlgoQueExiste", Id = "Pub.AlgoQueExiste" });

        Assert.NotEmpty(results);
        Assert.All(results, r =>
            Assert.StartsWith(_root, Path.GetFullPath(r.Path), StringComparison.OrdinalIgnoreCase));
    }
}
