using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// "Omitir esta versión" (lógica pura). Lo que define la feature es que la omisión **caduca sola**: vale
/// para la versión descartada, no para el paquete. Winget no sabe hacer esto (sus anclajes congelan el
/// paquete), así que estos tests son la especificación real del comportamiento — ver <see cref="SkippedVersions"/>.
/// </summary>
public sealed class SkippedVersionsTests
{
    private static Dictionary<string, string> With(string id, string version) =>
        new() { [id] = version };

    private static WingetPackage Pkg(string id, string available) =>
        new() { Name = id, Id = id, Version = "1.0", Available = available, Source = "winget" };

    [Fact]
    public void IsSkipped_SameVersion_IsSkipped()
        => Assert.True(SkippedVersions.IsSkipped(With("Git.Git", "2.55.0.2"), "Git.Git", "2.55.0.2"));

    /// <summary>El corazón de la feature: sale una versión nueva y el paquete vuelve a aparecer solo.</summary>
    [Fact]
    public void IsSkipped_NewerVersionAvailable_IsNotSkipped()
        => Assert.False(SkippedVersions.IsSkipped(With("Git.Git", "2.55.0.2"), "Git.Git", "2.56.0"));

    [Fact]
    public void IsSkipped_OtherPackage_IsNotSkipped()
        => Assert.False(SkippedVersions.IsSkipped(With("Git.Git", "2.55.0.2"), "7zip.7zip", "2.55.0.2"));

    [Theory]
    [InlineData(null, "1.0")]
    [InlineData("Git.Git", null)]
    [InlineData("", "")]
    public void IsSkipped_MissingData_IsNotSkipped(string? id, string? version)
        => Assert.False(SkippedVersions.IsSkipped(With("Git.Git", "1.0"), id, version));

    [Fact]
    public void IsSkipped_NullMap_IsNotSkipped()
        => Assert.False(SkippedVersions.IsSkipped(null, "Git.Git", "1.0"));

    /// <summary>Winget puede ofrecer una versión con cota superior desconocida ("&lt; 13.5.0.359"): también se omite tal cual.</summary>
    [Fact]
    public void IsSkipped_UnknownUpperBoundVersion_IsSkipped()
        => Assert.True(SkippedVersions.IsSkipped(With("IObit.DriverBooster", "< 13.5.0.359"), "IObit.DriverBooster", "< 13.5.0.359"));

    [Fact]
    public void Skip_ReplacesPreviousSkipOfSamePackage()
    {
        var skipped = With("Git.Git", "2.55.0.2");

        SkippedVersions.Skip(skipped, "Git.Git", "2.56.0");

        Assert.Equal("2.56.0", Assert.Single(skipped).Value);
    }

    [Fact]
    public void Skip_TrimsIdAndVersion()
    {
        var skipped = new Dictionary<string, string>();

        SkippedVersions.Skip(skipped, "  Git.Git  ", "  2.55.0.2 ");

        Assert.True(SkippedVersions.IsSkipped(skipped, "Git.Git", "2.55.0.2"));
    }

    [Fact]
    public void Unskip_RemovesIt()
    {
        var skipped = With("Git.Git", "2.55.0.2");

        SkippedVersions.Unskip(skipped, "Git.Git");

        Assert.Empty(skipped);
    }

    [Fact]
    public void Unskip_UnknownPackage_DoesNotThrow()
    {
        var skipped = With("Git.Git", "2.55.0.2");

        SkippedVersions.Unskip(skipped, "No.Such.Package");

        Assert.Single(skipped);
    }

    /// <summary>La omisión sigue viva mientras winget siga ofreciendo esa misma versión.</summary>
    [Fact]
    public void Prune_KeepsSkipStillOffered()
    {
        var skipped = With("Git.Git", "2.55.0.2");

        int removed = SkippedVersions.Prune(skipped, [Pkg("Git.Git", "2.55.0.2")]);

        Assert.Equal(0, removed);
        Assert.Single(skipped);
    }

    /// <summary>Si winget ya ofrece otra versión, la omisión vieja no pinta nada: fuera del settings.json.</summary>
    [Fact]
    public void Prune_DropsSkipWhenNewerVersionIsOffered()
    {
        var skipped = With("Git.Git", "2.55.0.2");

        int removed = SkippedVersions.Prune(skipped, [Pkg("Git.Git", "2.56.0")]);

        Assert.Equal(1, removed);
        Assert.Empty(skipped);
    }

    /// <summary>El paquete desapareció de la lista (se actualizó por fuera, o se desinstaló): omisión muerta.</summary>
    [Fact]
    public void Prune_DropsSkipWhenPackageNoLongerListed()
    {
        var skipped = With("Git.Git", "2.55.0.2");

        int removed = SkippedVersions.Prune(skipped, [Pkg("7zip.7zip", "26.02")]);

        Assert.Equal(1, removed);
        Assert.Empty(skipped);
    }
}
