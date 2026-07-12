using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
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

    // Ruta propia por prueba: la de producción (DefaultInstallerPath) es fija, y escribir ahí borraría
    // un instalador real que el usuario tuviera a medio descargar.
    private static string ScratchInstallerPath() =>
        Path.Combine(Path.GetTempPath(), $"wus_setup_{Guid.NewGuid():N}.exe");

    /// <summary>
    /// Regresión de v1.4.1: la descarga dejaba su FileStream (FileShare.None) abierto mientras
    /// verificaba, así que la verificación no podía ni abrir el archivo —"lo está usando otro
    /// proceso", siendo el proceso ella misma— y la auto-actualización fallaba SIEMPRE.
    /// </summary>
    [Fact]
    public async Task DownloadInstallerAsync_ClosesFileBeforeVerifying_SoTheChecksumCanBeRead()
    {
        // Más grande que el buffer de 80 KB de la descarga, para ejercitar varias vueltas del bucle.
        byte[] installer = RandomNumberGenerator.GetBytes((81920 * 2) + 13);
        string hash = Convert.ToHexString(SHA256.HashData(installer));

        using var server = new LocalHttpServer(new Dictionary<string, byte[]>
        {
            ["/setup.exe"] = installer,
            // Mismo formato que publica release.ps1: "<hash> *<archivo>".
            ["/setup.exe.sha256"] = Encoding.UTF8.GetBytes($"{hash} *WingetUSoft-Setup-9.9.9.exe")
        });

        string destination = ScratchInstallerPath();
        try
        {
            string path = await GitHubUpdateService.DownloadInstallerAsync(
                server.UrlFor("/setup.exe"), server.UrlFor("/setup.exe.sha256"),
                destinationPath: destination);

            Assert.Equal(destination, path);
            Assert.Equal(installer, await File.ReadAllBytesAsync(path));
        }
        finally
        {
            if (File.Exists(destination)) File.Delete(destination);
        }
    }

    [Fact]
    public async Task DownloadInstallerAsync_ChecksumMismatch_ThrowsAndDeletesTheInstaller()
    {
        byte[] installer = [1, 2, 3, 4];
        string wrongHash = Convert.ToHexString(SHA256.HashData([9, 9, 9, 9]));

        using var server = new LocalHttpServer(new Dictionary<string, byte[]>
        {
            ["/setup.exe"] = installer,
            ["/setup.exe.sha256"] = Encoding.UTF8.GetBytes(wrongHash)
        });

        string destination = ScratchInstallerPath();
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                GitHubUpdateService.DownloadInstallerAsync(
                    server.UrlFor("/setup.exe"), server.UrlFor("/setup.exe.sha256"),
                    destinationPath: destination));

            // Un instalador que no se pudo verificar no puede quedarse en disco esperando a que
            // alguien lo ejecute como administrador.
            Assert.False(File.Exists(destination));
        }
        finally
        {
            if (File.Exists(destination)) File.Delete(destination);
        }
    }

    /// <summary>
    /// Servidor HTTP mínimo sobre <see cref="TcpListener"/> para servir el instalador y su hash desde
    /// localhost. No se usa HttpListener porque en Windows exige reservar la URL como administrador.
    /// </summary>
    private sealed class LocalHttpServer : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly Dictionary<string, byte[]> _routes;
        private readonly CancellationTokenSource _cts = new();

        public LocalHttpServer(Dictionary<string, byte[]> routes)
        {
            _routes = routes;
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            _ = Task.Run(AcceptLoopAsync);
        }

        public string UrlFor(string path) =>
            $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}{path}";

        private async Task AcceptLoopAsync()
        {
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    using TcpClient client = await _listener.AcceptTcpClientAsync(_cts.Token);
                    await using NetworkStream stream = client.GetStream();

                    string path = ParsePath(await ReadRequestHeadAsync(stream, _cts.Token));
                    bool found = _routes.TryGetValue(path, out byte[]? body);
                    body ??= [];

                    string head =
                        $"HTTP/1.1 {(found ? "200 OK" : "404 Not Found")}\r\n" +
                        "Content-Type: application/octet-stream\r\n" +
                        $"Content-Length: {body.Length}\r\n" +
                        "Connection: close\r\n\r\n";

                    await stream.WriteAsync(Encoding.ASCII.GetBytes(head), _cts.Token);
                    await stream.WriteAsync(body, _cts.Token);
                    await stream.FlushAsync(_cts.Token);
                    client.Client.Shutdown(SocketShutdown.Send);   // cierre limpio: sin RST en el cliente
                }
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (SocketException) { }
        }

        // Los GET no traen cuerpo: la petición termina en la línea en blanco.
        private static async Task<string> ReadRequestHeadAsync(NetworkStream stream, CancellationToken ct)
        {
            var head = new StringBuilder();
            byte[] one = new byte[1];
            while (!head.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal))
            {
                if (await stream.ReadAsync(one, ct) == 0) break;
                head.Append((char)one[0]);
            }
            return head.ToString();
        }

        private static string ParsePath(string requestHead)
        {
            string[] parts = requestHead.Split('\n')[0].Split(' ');   // "GET /setup.exe HTTP/1.1"
            return parts.Length > 1 ? parts[1] : "/";
        }

        public void Dispose()
        {
            _cts.Cancel();
            _listener.Dispose();
            _cts.Dispose();
        }
    }
}
