using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Cubre el cálculo del hash con el que la auto-actualización verifica el instalador descargado
/// antes de ejecutarlo como administrador (<c>GitHubUpdateService.VerifyInstallerAsync</c>).
///
/// Mientras los instaladores se publiquen sin firmar, este hash es la ÚNICA verificación que hay:
/// se compara, sin distinguir mayúsculas, contra el asset <c>...exe.sha256</c> del release (que
/// genera <c>installer/build-installer.ps1</c> con <c>Get-FileHash -Algorithm SHA256</c>). Si el
/// formato de esta salida cambiara —minúsculas, guiones, Base64— la comparación fallaría siempre y
/// la app rechazaría su propio instalador, así que el formato se fija aquí.
/// </summary>
public sealed class GitHubUpdateServiceTests
{
    [Fact]
    public async Task ComputeSha256Async_MatchesKnownHash_AsUppercaseHexWithoutSeparators()
    {
        // SHA-256 de "abc" (vector de prueba estándar de NIST).
        const string expected = "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD";

        string path = Path.Combine(Path.GetTempPath(), $"wus_sha_{Guid.NewGuid():N}.bin");
        try
        {
            await File.WriteAllTextAsync(path, "abc");

            string actual = await GitHubUpdateService.ComputeSha256Async(path);

            // Mismo formato que produce Get-FileHash en build-installer.ps1: hex en mayúsculas y sin
            // guiones (lo que devuelve Convert.ToHexString).
            Assert.Equal(expected, actual);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ComputeSha256Async_DifferentContent_ProducesDifferentHash()
    {
        // El punto de la verificación: un instalador manipulado (aunque sea en un byte) no pasa.
        string original = Path.Combine(Path.GetTempPath(), $"wus_sha_{Guid.NewGuid():N}.bin");
        string tampered = Path.Combine(Path.GetTempPath(), $"wus_sha_{Guid.NewGuid():N}.bin");
        try
        {
            await File.WriteAllBytesAsync(original, [1, 2, 3, 4]);
            await File.WriteAllBytesAsync(tampered, [1, 2, 3, 5]);

            Assert.NotEqual(
                await GitHubUpdateService.ComputeSha256Async(original),
                await GitHubUpdateService.ComputeSha256Async(tampered));
        }
        finally
        {
            File.Delete(original);
            File.Delete(tampered);
        }
    }
}
