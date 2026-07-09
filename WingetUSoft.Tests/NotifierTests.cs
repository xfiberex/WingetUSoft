using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Pruebas de la decisión pura de avisar al terminar. El efecto real (sonido + parpadeo de la barra
/// de tareas, <see cref="Notifier.OperationFinished"/>) usa Win32 y no se cubre con pruebas unitarias.
/// </summary>
public sealed class NotifierTests
{
    private static readonly TimeSpan Threshold = TimeSpan.FromSeconds(10);

    [Fact]
    public void ShouldNotify_LongEnabledNotCancelled_IsTrue()
        => Assert.True(Notifier.ShouldNotify(TimeSpan.FromSeconds(30), enabled: true, cancelled: false, Threshold));

    [Fact]
    public void ShouldNotify_AtThreshold_IsTrue()
        => Assert.True(Notifier.ShouldNotify(Threshold, enabled: true, cancelled: false, Threshold));

    [Fact]
    public void ShouldNotify_BelowThreshold_IsFalse()
        => Assert.False(Notifier.ShouldNotify(TimeSpan.FromSeconds(3), enabled: true, cancelled: false, Threshold));

    [Fact]
    public void ShouldNotify_Disabled_IsFalse()
        => Assert.False(Notifier.ShouldNotify(TimeSpan.FromSeconds(30), enabled: false, cancelled: false, Threshold));

    [Fact]
    public void ShouldNotify_Cancelled_IsFalse()
        => Assert.False(Notifier.ShouldNotify(TimeSpan.FromSeconds(30), enabled: true, cancelled: true, Threshold));
}
