namespace WingetUSoft;

/// <summary>
/// Scans common Windows directories for residual files and folders left behind
/// after a package uninstall. Only checks targeted candidate paths derived from
/// the package name and ID — never recursively scans system folders.
/// </summary>
public static class CleanupScanner
{
    public static async Task<List<CleanupItemViewModel>> ScanAsync(
        IEnumerable<WingetPackage> packages,
        CancellationToken ct = default)
    {
        var results = new List<CleanupItemViewModel>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var package in packages)
        {
            ct.ThrowIfCancellationRequested();

            foreach (var candidate in GetCandidatePaths(package))
            {
                ct.ThrowIfCancellationRequested();
                if (!seen.Add(candidate)) continue;

                bool isDir  = Directory.Exists(candidate);
                bool isFile = !isDir && File.Exists(candidate);
                if (!isDir && !isFile) continue;

                long size = isDir
                    ? await Task.Run(() => CalculateDirSize(candidate), ct)
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

    // ---- Candidate path generation ------------------------------------------

    private static IEnumerable<string> GetCandidatePaths(WingetPackage package)
    {
        string roaming       = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string local         = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string localPrograms = System.IO.Path.Combine(local, "Programs");
        string progData      = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        string pf            = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string pf86          = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        var baseDirs = new[] { roaming, local, localPrograms, progData, pf, pf86 };
        var terms    = GetSearchTerms(package);

        // Single-level: {baseDir}\{term}
        foreach (string baseDir in baseDirs)
        {
            if (string.IsNullOrEmpty(baseDir)) continue;
            foreach (string term in terms)
                yield return System.IO.Path.Combine(baseDir, term);
        }

        // Two-level: {baseDir}\{publisher}\{appName}
        string[] parts = package.Id.Split('.', 2);
        if (parts.Length == 2
            && parts[0].Length >= 3
            && parts[1].Length >= 3)
        {
            foreach (string baseDir in new[] { pf, pf86, roaming, local, progData })
            {
                if (string.IsNullOrEmpty(baseDir)) continue;
                yield return System.IO.Path.Combine(baseDir, parts[0], parts[1]);
            }
        }
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
