namespace WingetUSoft;

public sealed record WingetProgressInfo(
    long DownloadedBytes,
    long TotalBytes,
    double SpeedBytesPerSecond);
