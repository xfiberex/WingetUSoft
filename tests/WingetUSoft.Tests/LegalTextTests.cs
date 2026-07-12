using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Pruebas de los textos legales embebidos como recurso en el ensamblado (licencia MIT y avisos de
/// terceros). Sin esto, un error de nombre en el <c>LogicalName</c> del .csproj (o mover/renombrar los
/// archivos de la raíz del repo) fallaría <b>en silencio</b>: <see cref="LegalText"/> es defensivo y
/// devuelve cadena vacía, así que el diálogo mostraría "Texto no disponible" en vez de romperse — un
/// fallo que solo se vería abriendo el menú Ayuda a mano.
/// </summary>
public sealed class LegalTextTests
{
    [Fact]
    public void License_IsEmbeddedAndNotEmpty()
        => Assert.False(string.IsNullOrWhiteSpace(LegalText.License()));

    [Fact]
    public void License_IsTheMitLicense()
    {
        string text = LegalText.License();
        Assert.Contains("MIT License", text, StringComparison.Ordinal);
        Assert.Contains("Permission is hereby granted, free of charge", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ThirdParty_IsEmbeddedAndNotEmpty()
        => Assert.False(string.IsNullOrWhiteSpace(LegalText.ThirdParty()));

    /// <summary>
    /// Los avisos deben citar los componentes que la app <b>redistribuye</b> de verdad (los del
    /// .csproj). Si mañana se añade o quita un <c>PackageReference</c>, este test no lo detecta solo,
    /// pero sí ancla los que hay hoy para que el archivo no se quede en un placeholder vacío.
    /// </summary>
    [Theory]
    [InlineData("Windows App SDK")]
    [InlineData("H.NotifyIcon")]
    [InlineData(".NET Runtime")]
    public void ThirdParty_MentionsRedistributedComponent(string component)
        => Assert.Contains(component, LegalText.ThirdParty(), StringComparison.Ordinal);
}
