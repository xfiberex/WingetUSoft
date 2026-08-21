using Xunit;

namespace WingetUSoft.Tests;

public class CleanupScannerTests
{
    // Uses a name suffix unlikely to collide with real installed software.
    private const string TestSuffix = "_WUSoftScanTest";

    [Fact]
    public async Task ScanAsync_EmptyPackageList_ReturnsEmpty()
    {
        var results = await CleanupScanner.ScanAsync([]);
        Assert.Empty(results);
    }

    [Fact]
    public async Task ScanAsync_CancellationRequested_ThrowsOperationCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            CleanupScanner.ScanAsync([new WingetPackage { Name = "Foo", Id = "Pub.Foo" }], cts.Token));
    }

    [Fact]
    public async Task ScanAsync_ExistingDirectoryMatchingPackageName_IsFound()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string dirName = $"TestApp{TestSuffix}";
        string testDir = Path.Combine(localAppData, dirName);
        Directory.CreateDirectory(testDir);

        try
        {
            var packages = new[]
            {
                new WingetPackage { Name = dirName, Id = $"Publisher.{dirName}" }
            };

            var results = await CleanupScanner.ScanAsync(packages);

            Assert.True(
                results.Any(r => string.Equals(r.Path, testDir, StringComparison.OrdinalIgnoreCase)),
                $"Expected to find '{testDir}' in scan results.");
        }
        finally
        {
            Directory.Delete(testDir, recursive: false);
        }
    }

    [Fact]
    public async Task ScanAsync_NonExistentPaths_ReturnsEmpty()
    {
        var packages = new[]
        {
            new WingetPackage
            {
                Name = $"NonExistentApp{TestSuffix}",
                Id   = $"Nobody.NonExistentApp{TestSuffix}"
            }
        };

        var results = await CleanupScanner.ScanAsync(packages);

        Assert.Empty(results);
    }

    [Fact]
    public async Task ScanAsync_SamePathFromMultiplePackages_ReportedOnce()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string dirName = $"SharedApp{TestSuffix}";
        string testDir = Path.Combine(localAppData, dirName);
        Directory.CreateDirectory(testDir);

        try
        {
            // Two packages whose name/id both resolve to the same candidate path.
            var packages = new[]
            {
                new WingetPackage { Name = dirName, Id = $"Publisher.{dirName}" },
                new WingetPackage { Name = dirName, Id = $"OtherPub.{dirName}" }
            };

            var results = await CleanupScanner.ScanAsync(packages);

            int matches = results.Count(r =>
                string.Equals(r.Path, testDir, StringComparison.OrdinalIgnoreCase));

            Assert.True(matches == 1, "The same path should only appear once even if multiple packages resolve to it.");
        }
        finally
        {
            Directory.Delete(testDir, recursive: false);
        }
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
        var packages = new[]
        {
            new WingetPackage { Name = maliciousName, Id = $"Publisher.{maliciousName}" }
        };

        var results = await CleanupScanner.ScanAsync(packages);

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
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string dirName = $"EscapeTarget{TestSuffix}";
        string outsideDir = Path.Combine(localAppData, dirName);
        Directory.CreateDirectory(outsideDir);

        try
        {
            // El escáner usa "{LocalAppData}\Programs" como directorio base. Con "..\<dir>" se sale de
            // Programs y aterriza en la carpeta recién creada, que sí existe.
            var packages = new[]
            {
                new WingetPackage { Name = $@"..\{dirName}", Id = $@"Publisher...\{dirName}" }
            };

            var results = await CleanupScanner.ScanAsync(packages);

            // Se comparan las rutas NORMALIZADAS a propósito: sin la corrección, el candidato llegaba
            // como "…\Programs\..\EscapeTarget…", que no es igual carácter a carácter al directorio de
            // destino aunque apunte exactamente a él. Comparar las cadenas en crudo dejaba pasar el test
            // sin probar nada.
            Assert.DoesNotContain(results, r =>
                string.Equals(Path.GetFullPath(r.Path), outsideDir, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(outsideDir, recursive: false);
        }
    }

    /// <summary>
    /// La contraprueba: la validación no puede llevarse por delante los nombres normales. Sin esto, un
    /// filtro demasiado agresivo pasaría todos los tests de seguridad dejando la función inútil.
    /// </summary>
    [Fact]
    public async Task ScanAsync_OrdinaryNameWithSpacesAndPunctuation_IsStillFound()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string dirName = $"Mi Programa - Edición 2026{TestSuffix}";
        string testDir = Path.Combine(localAppData, dirName);
        Directory.CreateDirectory(testDir);

        try
        {
            var packages = new[] { new WingetPackage { Name = dirName, Id = $"Publisher.{dirName}" } };

            var results = await CleanupScanner.ScanAsync(packages);

            Assert.Contains(results, r =>
                string.Equals(r.Path, testDir, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(testDir, recursive: false);
        }
    }

    [Fact]
    public async Task ScanAsync_FoundItem_IsNotSelectedByDefault()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string dirName = $"DefaultSelTest{TestSuffix}";
        string testDir = Path.Combine(localAppData, dirName);
        Directory.CreateDirectory(testDir);

        try
        {
            var packages = new[] { new WingetPackage { Name = dirName, Id = $"Pub.{dirName}" } };
            var results = await CleanupScanner.ScanAsync(packages);

            var item = results.FirstOrDefault(r =>
                string.Equals(r.Path, testDir, StringComparison.OrdinalIgnoreCase));

            Assert.NotNull(item);
            Assert.False(item.IsSelected, "Cleanup items must NOT be pre-selected to avoid accidental deletion.");
        }
        finally
        {
            Directory.Delete(testDir, recursive: false);
        }
    }
}
