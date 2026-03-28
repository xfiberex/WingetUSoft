using System.Diagnostics;
using System.Globalization;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WingetUSoft;

public static class WingetService
{
    private const int UserCancelledErrorCode = 1223;
    private const int ElevatedWorkerGracePeriodSeconds = 2;
    private const int ElevatedPipeConnectTimeoutMs = 5_000;
    private const int PipeReaderBufferSize = 4096;
    private const string ElevatedWorkerSwitch = "--elevated-batch-worker";
    private const string PipeNameArgument = "--pipe-name";
    private const string AuthTokenArgument = "--auth-token";
    private const string CancelEventArgument = "--cancel-event";
    private const string PackageIdArgument = "--package-id";
    private static readonly object WingetPathSync = new();
    private static string? _cachedWingetExecutablePath;

    // Matches: "45.3 MB / 200.0 MB" or "500 KB / 1.2 GB" (supports comma decimal separator)
    private static readonly Regex SizeProgressRegex = new(
        @"([\d]+(?:[.,][\d]+)?)\s*(KB|MB|GB)\s*/\s*([\d]+(?:[.,][\d]+)?)\s*(KB|MB|GB)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Matches: "8.5 MB/s" or "500 KB/s"
    private static readonly Regex SpeedRegex = new(
        @"([\d]+(?:[.,][\d]+)?)\s*(KB|MB|GB)/s",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Checks if winget is installed. Returns the version string or null.</summary>
    public static async Task<string?> CheckWingetAvailableAsync()
    {
        try
        {
            var result = await RunWingetAsync(["--version"]);
            string version = result.Output.Trim();
            return string.IsNullOrWhiteSpace(version) ? null : version;
        }
        catch { return null; }
    }
    public static async Task<List<WingetPackage>> GetUpgradablePackagesAsync(
        bool includeUnknown = false,
        CancellationToken cancellationToken = default)
    {
        var result = await RunWingetAsync(BuildListUpgradableArguments(includeUnknown), cancellationToken);

        if (result.ExitCode != 0)
            throw new InvalidOperationException(BuildWingetCommandErrorMessage(
                "consultar actualizaciones",
                result.ExitCode,
                result.Output,
                result.Error));

        return ParseUpgradeOutput(result.Output);
    }

    public static async Task<UpgradeResult> UpgradePackageAsync(
        string packageId,
        bool silent,
        bool runAsAdministrator = false,
        IProgress<WingetProgressInfo>? progress = null,
        CancellationToken cancellationToken = default,
        IProgress<string>? logProgress = null)
    {
        if (runAsAdministrator)
        {
            var batchResult = await UpgradePackagesAsAdministratorAsync([packageId], silent, cancellationToken, logProgress);

            if (batchResult.UserCancelled)
            {
                return new UpgradeResult
                {
                    Success = false,
                    ExitCode = UserCancelledErrorCode,
                    UserCancelled = true,
                    ErrorOutput = batchResult.ErrorOutput
                };
            }

            if (batchResult.Items.Count > 0)
                return batchResult.Items[0].Result;

            return new UpgradeResult
            {
                Success = false,
                ExitCode = batchResult.ExitCode,
                ErrorOutput = string.IsNullOrWhiteSpace(batchResult.ErrorOutput)
                    ? "No se recibió resultado de la actualización elevada."
                    : batchResult.ErrorOutput
            };
        }

        return await RunWingetInteractiveAsync(packageId, silent, progress, cancellationToken, logProgress);
    }

    public static async Task<UpgradeBatchResult> UpgradePackagesAsAdministratorAsync(
        IEnumerable<string> packageIds,
        bool silent,
        CancellationToken cancellationToken = default,
        IProgress<string>? logProgress = null,
        IProgress<UpgradeBatchStatusInfo>? statusProgress = null)
    {
        List<string> normalizedPackageIds = [..
            packageIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)];

        if (normalizedPackageIds.Count == 0)
            return new UpgradeBatchResult();

        cancellationToken.ThrowIfCancellationRequested();

        string pipeName = $"WingetUSoft.{Guid.NewGuid():N}";
        string authToken = Guid.NewGuid().ToString("N");
        string cancelEventName = $"Local\\WingetUSoft.Cancel.{Guid.NewGuid():N}";

        using var cancelEvent = new EventWaitHandle(false, EventResetMode.ManualReset, cancelEventName);
        using var pipeServer = new NamedPipeServerStream(
            pipeName,
            PipeDirection.In,
            1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        using var messageReaderCts = new CancellationTokenSource();
        using var cancellationRegistration = cancellationToken.Register(() => cancelEvent.Set());

        Task<UpgradeBatchResult> messageTask = ReadElevatedWorkerMessagesAsync(
            pipeServer,
            authToken,
            statusProgress,
            messageReaderCts.Token);

        try
        {
            logProgress?.Report(normalizedPackageIds.Count == 1
                ? "Solicitando permisos de administrador..."
                : "Solicitando permisos de administrador para el lote seleccionado...");

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = GetCurrentProcessExecutablePath(),
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden
            };
            foreach (string argument in BuildElevatedWorkerArguments(normalizedPackageIds, silent, pipeName, authToken, cancelEventName))
                process.StartInfo.ArgumentList.Add(argument);

            process.Start();
            logProgress?.Report(normalizedPackageIds.Count == 1
                ? "Actualización elevada en curso. El progreso detallado no está disponible en este modo."
                : "Lote elevado en curso. Se usará una sola elevación para todas las actualizaciones seleccionadas.");
            await process.WaitForExitAsync();

            if (!messageTask.IsCompleted)
            {
                Task completedTask = await Task.WhenAny(messageTask, Task.Delay(TimeSpan.FromSeconds(ElevatedWorkerGracePeriodSeconds)));
                if (completedTask != messageTask)
                    messageReaderCts.Cancel();
            }

            UpgradeBatchResult parsedResult;
            try
            {
                parsedResult = await messageTask;
            }
            catch (OperationCanceledException)
            {
                parsedResult = new UpgradeBatchResult
                {
                    ErrorOutput = "La sesión elevada finalizó sin enviar resultados válidos."
                };
            }
            catch (Exception ex)
            {
                parsedResult = new UpgradeBatchResult
                {
                    ErrorOutput = $"No se pudo leer el resultado de la sesión elevada: {ex.Message}"
                };
            }

            string errorOutput = parsedResult.ErrorOutput;
            if (string.IsNullOrWhiteSpace(errorOutput) && process.ExitCode != 0)
                errorOutput = $"El lote elevado finalizó con el código {process.ExitCode}.";

            return new UpgradeBatchResult
            {
                UserCancelled = false,
                CancelledAfterCurrentPackage = parsedResult.CancelledAfterCurrentPackage,
                ExitCode = process.ExitCode,
                ErrorOutput = errorOutput,
                Items = parsedResult.Items
            };
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == UserCancelledErrorCode)
        {
            messageReaderCts.Cancel();
            return new UpgradeBatchResult
            {
                UserCancelled = true,
                ExitCode = UserCancelledErrorCode,
                ErrorOutput = "La elevación de permisos fue cancelada por el usuario."
            };
        }
        finally
        {
            messageReaderCts.Cancel();
        }
    }

    internal static string BuildUpgradeArguments(string packageId, bool silent)
    {
        string mode = silent ? "--silent" : "--interactive";
        return $"upgrade --id \"{packageId}\" --accept-source-agreements --accept-package-agreements {mode}";
    }

    internal static bool IsElevatedWorkerInvocation(string[] args) =>
        args.Length > 0 && string.Equals(args[0], ElevatedWorkerSwitch, StringComparison.OrdinalIgnoreCase);

    internal static async Task<int> RunElevatedBatchWorkerAsync(string[] args)
    {
        ElevatedWorkerOptions options;
        StreamWriter? writer = null;

        try
        {
            options = ParseElevatedWorkerOptions(args);

            using var pipe = new NamedPipeClientStream(".", options.PipeName, PipeDirection.Out, PipeOptions.Asynchronous);
            await pipe.ConnectAsync(ElevatedPipeConnectTimeoutMs);

            using var streamWriter = new StreamWriter(pipe, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
            {
                AutoFlush = true
            };

            writer = streamWriter;

            await SendWorkerMessageAsync(writer, new ElevatedWorkerMessage
            {
                Type = "hello",
                Token = options.AuthToken
            });

            using var cancelEvent = EventWaitHandle.OpenExisting(options.CancelEventName);
            await SendWorkerStatusAsync(writer, "starting", 0, string.Empty, options.PackageIds.Count);

            int completedItems = 0;
            bool batchCancelled = false;

            foreach (string packageId in options.PackageIds)
            {
                if (cancelEvent.WaitOne(0))
                {
                    batchCancelled = true;
                    await SendWorkerStatusAsync(writer, "cancelled", completedItems, packageId, options.PackageIds.Count);
                    break;
                }

                completedItems++;
                await SendWorkerStatusAsync(writer, "running", completedItems, packageId, options.PackageIds.Count);

                UpgradeResult result = await RunWingetInteractiveAsync(
                    packageId,
                    options.Silent,
                    progress: null,
                    cancellationToken: CancellationToken.None,
                    logProgress: null);

                await SendWorkerMessageAsync(writer, new ElevatedWorkerMessage
                {
                    Type = "result",
                    PackageId = packageId,
                    Success = result.Success,
                    ExitCode = result.ExitCode,
                    UserCancelled = result.UserCancelled,
                    Output = result.Output,
                    ErrorOutput = result.ErrorOutput
                });
            }

            await SendWorkerStatusAsync(writer, "completed", completedItems, string.Empty, options.PackageIds.Count);
            await SendWorkerMessageAsync(writer, new ElevatedWorkerMessage
            {
                Type = "summary",
                BatchCancelled = batchCancelled
            });

            return 0;
        }
        catch (Exception ex)
        {
            if (writer is not null)
            {
                try
                {
                    await SendWorkerMessageAsync(writer, new ElevatedWorkerMessage
                    {
                        Type = "summary",
                        ErrorOutput = ex.Message
                    });
                }
                catch
                {
                }
            }

            return 1;
        }
    }

    private static IReadOnlyList<string> BuildListUpgradableArguments(bool includeUnknown)
    {
        var arguments = new List<string>
        {
            "upgrade",
            "--accept-source-agreements"
        };

        if (includeUnknown)
            arguments.Add("--include-unknown");

        return arguments;
    }

    private static IReadOnlyList<string> BuildUpgradeArgumentList(string packageId, bool silent)
    {
        string mode = silent ? "--silent" : "--interactive";

        return [
            "upgrade",
            "--id",
            packageId,
            "--accept-source-agreements",
            "--accept-package-agreements",
            mode
        ];
    }

    private static IReadOnlyList<string> BuildElevatedWorkerArguments(
        IReadOnlyList<string> packageIds,
        bool silent,
        string pipeName,
        string authToken,
        string cancelEventName)
    {
        var arguments = new List<string>
        {
            ElevatedWorkerSwitch,
            PipeNameArgument,
            pipeName,
            AuthTokenArgument,
            authToken,
            CancelEventArgument,
            cancelEventName,
            silent ? "--silent" : "--interactive"
        };

        foreach (string packageId in packageIds)
        {
            arguments.Add(PackageIdArgument);
            arguments.Add(packageId);
        }

        return arguments;
    }

    private static ElevatedWorkerOptions ParseElevatedWorkerOptions(string[] args)
    {
        var options = new ElevatedWorkerOptions();

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case PipeNameArgument:
                    options.PipeName = GetRequiredArgumentValue(args, ref i);
                    break;

                case AuthTokenArgument:
                    options.AuthToken = GetRequiredArgumentValue(args, ref i);
                    break;

                case CancelEventArgument:
                    options.CancelEventName = GetRequiredArgumentValue(args, ref i);
                    break;

                case PackageIdArgument:
                    options.PackageIds.Add(GetRequiredArgumentValue(args, ref i));
                    break;

                case "--silent":
                    options.Silent = true;
                    break;

                case "--interactive":
                    options.Silent = false;
                    break;

                default:
                    throw new ArgumentException($"Argumento no admitido para el worker elevado: {args[i]}");
            }
        }

        options.PackageIds = [..
            options.PackageIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)];

        if (string.IsNullOrWhiteSpace(options.PipeName)
            || string.IsNullOrWhiteSpace(options.AuthToken)
            || string.IsNullOrWhiteSpace(options.CancelEventName)
            || options.PackageIds.Count == 0)
            throw new ArgumentException("La invocación del worker elevado no contiene todos los argumentos requeridos.");

        return options;
    }

    private static string GetRequiredArgumentValue(string[] args, ref int index)
    {
        if (index + 1 >= args.Length)
            throw new ArgumentException($"Falta el valor para el argumento {args[index]}.");

        index++;
        return args[index];
    }

    private static async Task<UpgradeBatchResult> ReadElevatedWorkerMessagesAsync(
        NamedPipeServerStream pipeServer,
        string authToken,
        IProgress<UpgradeBatchStatusInfo>? statusProgress,
        CancellationToken cancellationToken)
    {
        await pipeServer.WaitForConnectionAsync(cancellationToken);

        using var reader = new StreamReader(pipeServer, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: PipeReaderBufferSize, leaveOpen: true);

        bool authenticated = false;
        bool batchCancelled = false;
        string errorOutput = string.Empty;
        var items = new List<UpgradeBatchItemResult>();

        while (true)
        {
            string? line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
                break;

            ElevatedWorkerMessage? message;
            try
            {
                message = JsonSerializer.Deserialize<ElevatedWorkerMessage>(line);
            }
            catch (JsonException)
            {
                continue;
            }

            if (message is null)
                continue;

            if (!authenticated)
            {
                if (!string.Equals(message.Type, "hello", StringComparison.Ordinal)
                    || !string.Equals(message.Token, authToken, StringComparison.Ordinal))
                    throw new InvalidOperationException("La sesión elevada no pudo validarse correctamente.");

                authenticated = true;
                continue;
            }

            switch (message.Type)
            {
                case "status":
                    statusProgress?.Report(new UpgradeBatchStatusInfo
                    {
                        Phase = message.Phase ?? string.Empty,
                        PackageId = message.PackageId ?? string.Empty,
                        CurrentIndex = message.CurrentIndex,
                        TotalCount = message.TotalCount
                    });
                    break;

                case "result":
                    items.Add(new UpgradeBatchItemResult
                    {
                        PackageId = message.PackageId ?? string.Empty,
                        Result = new UpgradeResult
                        {
                            Success = message.Success,
                            ExitCode = message.ExitCode,
                            UserCancelled = message.UserCancelled,
                            Output = message.Output ?? string.Empty,
                            ErrorOutput = message.ErrorOutput ?? string.Empty
                        }
                    });
                    break;

                case "summary":
                    batchCancelled = message.BatchCancelled;
                    errorOutput = message.ErrorOutput ?? string.Empty;
                    break;
            }
        }

        return authenticated
            ? new UpgradeBatchResult
            {
                CancelledAfterCurrentPackage = batchCancelled,
                Items = items,
                ErrorOutput = errorOutput
            }
            : new UpgradeBatchResult
            {
                ErrorOutput = "No se pudo establecer comunicación con la sesión elevada."
            };
    }

    private static Task SendWorkerMessageAsync(StreamWriter writer, ElevatedWorkerMessage message) =>
        writer.WriteLineAsync(JsonSerializer.Serialize(message));

    private static Task SendWorkerStatusAsync(
        StreamWriter writer,
        string phase,
        int currentIndex,
        string packageId,
        int totalCount) =>
        SendWorkerMessageAsync(writer, new ElevatedWorkerMessage
        {
            Type = "status",
            Phase = phase,
            CurrentIndex = currentIndex,
            PackageId = packageId,
            TotalCount = totalCount
        });

    private static async Task<UpgradeResult> RunWingetInteractiveAsync(
        string packageId,
        bool silent,
        IProgress<WingetProgressInfo>? progress,
        CancellationToken cancellationToken,
        IProgress<string>? logProgress)
    {

        using var process = new Process();
        process.StartInfo = CreateWingetStartInfo(BuildUpgradeArgumentList(packageId, silent));

        process.Start();

        using var _ = cancellationToken.Register(() =>
        {
            try { process.Kill(entireProcessTree: true); }
            catch { }
        });

        var fullOutput = new StringBuilder();
        var fullError = new StringBuilder();

        void OnOutputLine(string line)
        {
            var info = ParseProgressLine(line);
            if (info is not null)
                progress?.Report(info);
        }

        var stdoutTask = ReadStreamWithProgressAsync(
            process.StandardOutput.BaseStream,
            fullOutput,
            OnOutputLine,
            line => logProgress?.Report(line));
        var stderrTask = ReadStreamSimpleAsync(process.StandardError.BaseStream, fullError);

        await Task.WhenAll(stdoutTask, stderrTask);
        await process.WaitForExitAsync();

        cancellationToken.ThrowIfCancellationRequested();

        return new UpgradeResult
        {
            Success = process.ExitCode == 0,
            ExitCode = process.ExitCode,
            Output = fullOutput.ToString(),
            ErrorOutput = fullError.ToString()
        };
    }

    internal static WingetProgressInfo? ParseProgressLine(string line)
    {
        var sizeMatch = SizeProgressRegex.Match(line);
        if (!sizeMatch.Success) return null;

        if (!double.TryParse(sizeMatch.Groups[1].Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out double downloaded))
            return null;
        if (!double.TryParse(sizeMatch.Groups[3].Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out double total))
            return null;

        long downloadedBytes = ToBytes(downloaded, sizeMatch.Groups[2].Value);
        long totalBytes = ToBytes(total, sizeMatch.Groups[4].Value);

        double speed = 0;
        var speedMatch = SpeedRegex.Match(line);
        if (speedMatch.Success && double.TryParse(speedMatch.Groups[1].Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out double speedVal))
            speed = ToBytes(speedVal, speedMatch.Groups[2].Value);

        return new WingetProgressInfo(downloadedBytes, totalBytes, speed);
    }

    private static long ToBytes(double value, string unit) => unit.ToUpperInvariant() switch
    {
        "KB" => (long)(value * 1_024),
        "MB" => (long)(value * 1_048_576),
        "GB" => (long)(value * 1_073_741_824L),
        _ => (long)value
    };

    // Reads stdout char by char, firing the callback on both \n and \r line endings
    // so that winget's carriage-return progress updates are captured in real time.
    private static async Task ReadStreamWithProgressAsync(
        Stream stream,
        StringBuilder output,
        Action<string>? onLine,
        Action<string>? onLogLine = null)
    {
        var decoder = Encoding.UTF8.GetDecoder();
        var byteBuffer = new byte[4096];
        var charBuffer = new char[Encoding.UTF8.GetMaxCharCount(4096)];
        var lineBuilder = new StringBuilder();

        while (true)
        {
            int bytesRead = await stream.ReadAsync(byteBuffer);
            if (bytesRead == 0) break;

            int charsDecoded = decoder.GetChars(byteBuffer, 0, bytesRead, charBuffer, 0);
            for (int i = 0; i < charsDecoded; i++)
            {
                char c = charBuffer[i];
                switch (c)
                {
                    case '\n':
                        string completeLine = lineBuilder.ToString().TrimEnd('\r');
                        output.AppendLine(completeLine);
                        onLine?.Invoke(completeLine);
                        onLogLine?.Invoke(completeLine);
                        lineBuilder.Clear();
                        break;
                    case '\r':
                        if (lineBuilder.Length > 0)
                        {
                            onLine?.Invoke(lineBuilder.ToString());
                            lineBuilder.Clear();
                        }
                        break;
                    default:
                        lineBuilder.Append(c);
                        break;
                }
            }
        }

        if (lineBuilder.Length > 0)
        {
            string remaining = lineBuilder.ToString();
            output.AppendLine(remaining);
            onLine?.Invoke(remaining);
        }
    }

    private static async Task ReadStreamSimpleAsync(Stream stream, StringBuilder output)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        string? line;
        while ((line = await reader.ReadLineAsync()) is not null)
            output.AppendLine(line);
    }

    private sealed record ProcessResult(int ExitCode, string Output, string Error);

    internal static UpgradeBatchStatusInfo? ParseElevatedBatchStatus(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            var payload = JsonSerializer.Deserialize<ElevatedBatchStatusPayload>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (payload is null)
                return null;

            return new UpgradeBatchStatusInfo
            {
                Phase = payload.Phase ?? "",
                PackageId = payload.PackageId ?? "",
                CurrentIndex = payload.CurrentIndex,
                TotalCount = payload.TotalCount
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    internal static UpgradeBatchResult ParseElevatedBatchResult(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new UpgradeBatchResult
            {
                ErrorOutput = "No se recibieron resultados del lote elevado."
            };
        }

        try
        {
            var payload = JsonSerializer.Deserialize<ElevatedBatchPayload>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return new UpgradeBatchResult
            {
                CancelledAfterCurrentPackage = payload?.BatchCancelled ?? false,
                Items = payload?.Results?.Select(item => new UpgradeBatchItemResult
                {
                    PackageId = item.PackageId ?? "",
                    Result = new UpgradeResult
                    {
                        Success = item.Success,
                        ExitCode = item.ExitCode,
                        UserCancelled = item.UserCancelled,
                        Output = item.Output ?? "",
                        ErrorOutput = item.ErrorOutput ?? ""
                    }
                }).ToList() ?? []
            };
        }
        catch (JsonException ex)
        {
            return new UpgradeBatchResult
            {
                ErrorOutput = $"No se pudieron leer los resultados del lote elevado: {ex.Message}"
            };
        }
    }

    private static async Task<ProcessResult> RunWingetAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken = default)
    {
        using var process = new Process();
        process.StartInfo = CreateWingetStartInfo(arguments);

        var output = new StringBuilder();
        var error = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                output.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                error.AppendLine(e.Data);
        };

        process.Start();

        using var _ = cancellationToken.Register(() =>
        {
            try { process.Kill(entireProcessTree: true); }
            catch { }
        });

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();

        cancellationToken.ThrowIfCancellationRequested();

        return new ProcessResult(process.ExitCode, output.ToString(), error.ToString());
    }

    internal static List<WingetPackage> ParseUpgradeOutput(string output)
    {
        var packages = new List<WingetPackage>();

        if (string.IsNullOrWhiteSpace(output))
            return packages;

        string[] lines = output.Replace("\r", string.Empty).Split('\n');

        // Find the separator line (----) and use the line before it as header
        int separatorIndex = -1;
        for (int i = 0; i < lines.Length; i++)
        {
            string trimmed = lines[i].Trim();
            if (trimmed.StartsWith("--") && trimmed.Length > 10)
            {
                separatorIndex = i;
                break;
            }
        }

        if (separatorIndex < 1)
            return packages;

        string header = lines[separatorIndex - 1];

        List<int> columnStarts = GetColumnStarts(header);
        if (columnStarts.Count < 4)
            return packages;

        int namePos = columnStarts[0];
        int idPos = columnStarts[1];
        int versionPos = columnStarts[2];
        int availablePos = columnStarts[3];
        int sourcePos = columnStarts.Count > 4 ? columnStarts[4] : -1;

        for (int i = separatorIndex + 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;
            if (line.Length <= idPos)
                continue;

            WingetPackage? package = TryParseFixedWidthPackageLine(line, namePos, idPos, versionPos, availablePos, sourcePos)
                ?? TryParseDelimitedPackageLine(line);

            if (package is null)
                continue;

            packages.Add(package);
        }

        return packages;
    }

    private static string SafeSubstring(string s, int start, int length)
    {
        if (start >= s.Length) return "";
        if (length < 0) length = 0;
        if (start + length > s.Length) length = s.Length - start;
        return s.Substring(start, length);
    }

    private static List<int> GetColumnStarts(string header)
    {
        var starts = new List<int>();

        for (int i = 0; i < header.Length; i++)
        {
            if (!char.IsWhiteSpace(header[i]) && (i == 0 || char.IsWhiteSpace(header[i - 1])))
                starts.Add(i);
        }

        return starts;
    }

    private static WingetPackage? TryParseFixedWidthPackageLine(
        string line,
        int namePos,
        int idPos,
        int versionPos,
        int availablePos,
        int sourcePos)
    {
        string name = SafeSubstring(line, namePos, idPos - namePos).Trim();
        string id = SafeSubstring(line, idPos, versionPos - idPos).Trim();
        string version = SafeSubstring(line, versionPos, availablePos - versionPos).Trim();
        string available = sourcePos >= 0
            ? SafeSubstring(line, availablePos, sourcePos - availablePos).Trim()
            : SafeSubstring(line, availablePos, line.Length - availablePos).Trim();
        string source = sourcePos >= 0
            ? SafeSubstring(line, sourcePos, line.Length - sourcePos).Trim()
            : "";

        return CreatePackageIfValid(name, id, version, available, source);
    }

    private static WingetPackage? TryParseDelimitedPackageLine(string line)
    {
        string[] parts = Regex.Split(line.Trim(), @"\s{2,}");
        if (parts.Length < 4)
            return null;

        string name = parts[0].Trim();
        string id = parts[1].Trim();
        string version = parts[2].Trim();
        string available = parts[3].Trim();
        string source = parts.Length > 4
            ? string.Join(" ", parts.Skip(4)).Trim()
            : "";

        return CreatePackageIfValid(name, id, version, available, source);
    }

    private static WingetPackage? CreatePackageIfValid(
        string name,
        string id,
        string version,
        string available,
        string source)
    {
        if (!LooksLikePackageRow(name, id, version, available))
            return null;

        return new WingetPackage
        {
            Name = name,
            Id = id,
            Version = version,
            Available = available,
            Source = source
        };
    }

    private static bool LooksLikePackageRow(string name, string id, string version, string available)
    {
        if (string.IsNullOrWhiteSpace(name)
            || string.IsNullOrWhiteSpace(id)
            || string.IsNullOrWhiteSpace(version)
            || string.IsNullOrWhiteSpace(available))
            return false;

        return id.IndexOfAny([' ', '\t']) < 0;
    }

    internal static string BuildWingetCommandErrorMessage(string action, int exitCode, string output, string errorOutput)
    {
        string combined = string.IsNullOrWhiteSpace(errorOutput)
            ? output
            : $"{errorOutput}\n{output}";

        string detail = ExtractLastMeaningfulLine(combined);
        return string.IsNullOrWhiteSpace(detail)
            ? $"winget no pudo {action}. Código de salida: {exitCode}."
            : $"winget no pudo {action}. {detail} (código de salida: {exitCode}).";
    }

    private static string ExtractLastMeaningfulLine(string text)
    {
        string[] lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        for (int i = lines.Length - 1; i >= 0; i--)
        {
            string line = lines[i].Trim();
            if (line.Length > 5 && !line.StartsWith("--", StringComparison.Ordinal))
                return line;
        }

        return "";
    }

    internal static string GetWingetExecutablePath()
    {
        lock (WingetPathSync)
        {
            if (_cachedWingetExecutablePath is not null && File.Exists(_cachedWingetExecutablePath))
                return _cachedWingetExecutablePath;

            _cachedWingetExecutablePath = ResolveWingetExecutablePath();
            return _cachedWingetExecutablePath;
        }
    }

    internal static void ResetCache()
    {
        lock (WingetPathSync)
            _cachedWingetExecutablePath = null;
    }

    private static string ResolveWingetExecutablePath()
    {
        foreach (string candidate in EnumerateWingetCandidates())
        {
            if (IsTrustedWingetPath(candidate))
                return Path.GetFullPath(candidate);
        }

        throw new FileNotFoundException("No se encontró una instalación confiable de winget.");
    }

    private static IEnumerable<string> EnumerateWingetCandidates()
    {
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft",
            "WindowsApps",
            "winget.exe");

        string whereExecutable = Path.Combine(Environment.SystemDirectory, "where.exe");
        if (!File.Exists(whereExecutable))
            yield break;

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = whereExecutable,
            Arguments = "winget",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        foreach (string line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            yield return line.Trim();
    }

    private static bool IsTrustedWingetPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch
        {
            return false;
        }

        if (!File.Exists(fullPath)
            || !string.Equals(Path.GetFileName(fullPath), "winget.exe", StringComparison.OrdinalIgnoreCase))
            return false;

        string trustedAliasPath = Path.GetFullPath(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft",
            "WindowsApps",
            "winget.exe"));

        if (string.Equals(fullPath, trustedAliasPath, StringComparison.OrdinalIgnoreCase))
            return true;

        string windowsAppsDirectory = Path.GetFullPath(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "WindowsApps"));

        return fullPath.StartsWith(windowsAppsDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static ProcessStartInfo CreateWingetStartInfo(IEnumerable<string> arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = GetWingetExecutablePath(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (string argument in arguments)
            startInfo.ArgumentList.Add(argument);

        return startInfo;
    }

    private static string GetCurrentProcessExecutablePath() =>
        Environment.ProcessPath
        ?? throw new InvalidOperationException("No se pudo determinar la ruta del ejecutable actual.");

    private sealed class ElevatedBatchPayload
    {
        public bool BatchCancelled { get; set; }
        public List<ElevatedBatchItemPayload> Results { get; set; } = [];
    }

    private sealed class ElevatedBatchItemPayload
    {
        public string PackageId { get; set; } = "";
        public bool Success { get; set; }
        public int ExitCode { get; set; }
        public bool UserCancelled { get; set; }
        public string Output { get; set; } = "";
        public string ErrorOutput { get; set; } = "";
    }

    private sealed class ElevatedBatchStatusPayload
    {
        public string Phase { get; set; } = "";
        public string PackageId { get; set; } = "";
        public int CurrentIndex { get; set; }
        public int TotalCount { get; set; }
    }

    private sealed class ElevatedWorkerOptions
    {
        public string PipeName { get; set; } = string.Empty;
        public string AuthToken { get; set; } = string.Empty;
        public string CancelEventName { get; set; } = string.Empty;
        public bool Silent { get; set; }
        public List<string> PackageIds { get; set; } = [];
    }

    private sealed class ElevatedWorkerMessage
    {
        public string Type { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string Phase { get; set; } = string.Empty;
        public string PackageId { get; set; } = string.Empty;
        public int CurrentIndex { get; set; }
        public int TotalCount { get; set; }
        public bool Success { get; set; }
        public int ExitCode { get; set; }
        public bool UserCancelled { get; set; }
        public bool BatchCancelled { get; set; }
        public string Output { get; set; } = string.Empty;
        public string ErrorOutput { get; set; } = string.Empty;
    }
}
