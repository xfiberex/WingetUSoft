using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace WingetUSoft;

internal sealed record GitHubReleaseInfo(string TagName, string HtmlUrl, string Version, string DownloadUrl, string Notes);

internal static class GitHubUpdateService
{
    private const string Owner = "xfiberex";
    private const string Repo = "WingetUSoft";

    private static readonly Uri LatestReleaseUri =
        new($"https://api.github.com/repos/{Owner}/{Repo}/releases/latest");

    private static Uri ReleaseByTagUri(string tag) =>
        new($"https://api.github.com/repos/{Owner}/{Repo}/releases/tags/{tag}");

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(10),
        DefaultRequestHeaders = { { "User-Agent", $"{Repo}/updater" } }
    };

    private static readonly HttpClient DownloadHttp = new()
    {
        Timeout = Timeout.InfiniteTimeSpan,
        DefaultRequestHeaders = { { "User-Agent", $"{Repo}/updater" } }
    };

    /// <summary>
    /// Compara la versión del ensamblado en ejecución con el último GitHub Release.
    /// Devuelve null si no hay versión más reciente o si la comprobación falla.
    /// </summary>
    public static async Task<GitHubReleaseInfo?> CheckForUpdateAsync(
        CancellationToken cancellationToken = default)
    {
        GitHubReleaseInfo? release = await GetLatestReleaseAsync(cancellationToken).ConfigureAwait(false);
        if (release is null) return null;

        if (!Version.TryParse(NormalizeVersion(release.Version), out Version? remoteVersion))
            return null;

        Version currentVersion =
            Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);

        return remoteVersion <= currentVersion ? null : release;
    }

    /// <summary>Obtiene el último release publicado, sin comparar versiones. Null si la consulta falla.</summary>
    public static Task<GitHubReleaseInfo?> GetLatestReleaseAsync(CancellationToken cancellationToken = default) =>
        FetchReleaseAsync(LatestReleaseUri, cancellationToken);

    /// <summary>
    /// Obtiene el release publicado con el tag indicado (p. ej. <c>v1.3.0</c>), o null si no existe.
    /// Se usa para mostrar las novedades de la versión instalada tras una actualización.
    /// </summary>
    public static Task<GitHubReleaseInfo?> GetReleaseByTagAsync(string tag, CancellationToken cancellationToken = default) =>
        string.IsNullOrWhiteSpace(tag)
            ? Task.FromResult<GitHubReleaseInfo?>(null)
            : FetchReleaseAsync(ReleaseByTagUri(tag), cancellationToken);

    private static async Task<GitHubReleaseInfo?> FetchReleaseAsync(Uri uri, CancellationToken cancellationToken)
    {
        try
        {
            GitHubReleaseResponse? release = await Http
                .GetFromJsonAsync<GitHubReleaseResponse>(uri, cancellationToken)
                .ConfigureAwait(false);

            if (release is null || string.IsNullOrWhiteSpace(release.TagName))
                return null;

            string version = release.TagName.TrimStart('v', 'V');
            string downloadUrl = release.Assets
                .FirstOrDefault(a => a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                ?.BrowserDownloadUrl ?? release.HtmlUrl;
            return new GitHubReleaseInfo(release.TagName, release.HtmlUrl, version, downloadUrl, release.Body);
        }
        catch
        {
            return null;
        }
    }

    // Normalize to 4-part version so "1.2.0" (Revision=-1) compares equal to "1.2.0.0"
    private static string NormalizeVersion(string version)
    {
        while (version.Count(c => c == '.') < 3)
            version += ".0";
        return version;
    }

    public static async Task<string> DownloadInstallerAsync(
        string downloadUrl,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"{Repo}_Update.exe");

        using HttpResponseMessage response = await DownloadHttp
            .GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        long? totalBytes = response.Content.Headers.ContentLength;
        await using Stream contentStream = await response.Content
            .ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using FileStream fileStream = new(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);

        byte[] buffer = new byte[81920];
        long totalRead = 0;
        int bytesRead;
        while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
            totalRead += bytesRead;
            if (totalBytes > 0)
                progress?.Report((double)totalRead / totalBytes.Value);
        }

        if (!VerifyAuthenticodeSignature(tempPath))
        {
            File.Delete(tempPath);
            throw new InvalidOperationException(L.T("update.unsignedInstaller"));
        }

        return tempPath;
    }

    // Verifies that the file carries a valid Authenticode signature trusted by Windows.
    // Returns false for unsigned, expired, or untrusted signatures.
    internal static bool VerifyAuthenticodeSignature(string filePath)
    {
        var fileInfo = new NativeMethods.WINTRUST_FILE_INFO
        {
            cbStruct = (uint)Marshal.SizeOf<NativeMethods.WINTRUST_FILE_INFO>(),
            pcwszFilePath = filePath,
            hFile = IntPtr.Zero,
            pgKnownSubject = IntPtr.Zero
        };

        nint fileInfoPtr = Marshal.AllocHGlobal(Marshal.SizeOf<NativeMethods.WINTRUST_FILE_INFO>());
        try
        {
            Marshal.StructureToPtr(fileInfo, fileInfoPtr, false);

            var trustData = new NativeMethods.WINTRUST_DATA
            {
                cbStruct = (uint)Marshal.SizeOf<NativeMethods.WINTRUST_DATA>(),
                pPolicyCallbackData = IntPtr.Zero,
                pSIPClientData = IntPtr.Zero,
                dwUIChoice = NativeMethods.WTD_UI_NONE,
                fdwRevocationChecks = NativeMethods.WTD_REVOKE_NONE,
                dwUnionChoice = NativeMethods.WTD_CHOICE_FILE,
                pUnion = fileInfoPtr,
                dwStateAction = NativeMethods.WTD_STATEACTION_IGNORE,
                hWVTStateData = IntPtr.Zero,
                pwszURLReference = null,
                dwProvFlags = NativeMethods.WTD_SAFER_FLAG,
                dwUIContext = 0
            };

            nint trustDataPtr = Marshal.AllocHGlobal(Marshal.SizeOf<NativeMethods.WINTRUST_DATA>());
            try
            {
                Marshal.StructureToPtr(trustData, trustDataPtr, false);
                var actionId = new Guid("00AAC56B-CD44-11D0-8CC2-00C04FC295EE");
                uint result = NativeMethods.WinVerifyTrust(IntPtr.Zero, ref actionId, trustDataPtr);
                return result == 0;
            }
            finally
            {
                Marshal.FreeHGlobal(trustDataPtr);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(fileInfoPtr);
        }
    }

    private static class NativeMethods
    {
        internal const uint WTD_UI_NONE = 2;
        internal const uint WTD_REVOKE_NONE = 0;
        internal const uint WTD_CHOICE_FILE = 1;
        internal const uint WTD_STATEACTION_IGNORE = 0;
        internal const uint WTD_SAFER_FLAG = 0x100;

        [DllImport("wintrust.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern uint WinVerifyTrust(
            IntPtr hwnd,
            ref Guid pgActionID,
            IntPtr pWVTData);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct WINTRUST_FILE_INFO
        {
            public uint cbStruct;
            [MarshalAs(UnmanagedType.LPWStr)] public string? pcwszFilePath;
            public IntPtr hFile;
            public IntPtr pgKnownSubject;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct WINTRUST_DATA
        {
            public uint cbStruct;
            public IntPtr pPolicyCallbackData;
            public IntPtr pSIPClientData;
            public uint dwUIChoice;
            public uint fdwRevocationChecks;
            public uint dwUnionChoice;
            public IntPtr pUnion;
            public uint dwStateAction;
            public IntPtr hWVTStateData;
            [MarshalAs(UnmanagedType.LPWStr)] public string? pwszURLReference;
            public uint dwProvFlags;
            public uint dwUIContext;
        }
    }

    private sealed class GitHubReleaseResponse
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; init; } = "";

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; init; } = "";

        [JsonPropertyName("body")]
        public string Body { get; init; } = "";

        [JsonPropertyName("assets")]
        public List<GitHubAsset> Assets { get; init; } = [];
    }

    private sealed class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = "";

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; init; } = "";
    }
}
