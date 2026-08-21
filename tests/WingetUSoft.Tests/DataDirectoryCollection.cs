using Xunit;

namespace WingetUSoft.Tests;

/// <summary>
/// Clases que redirigen <see cref="AppSettings.DataDirectoryPath"/> a un directorio propio.
/// </summary>
/// <remarks>
/// Es estado **estático**: si xUnit corre dos de estas clases a la vez —y por defecto paraleliza por
/// clase— una le cambia el directorio a la otra a mitad de prueba, y el fallo sale intermitente y en
/// una clase que no tiene la culpa. Compartir colección las serializa entre sí sin frenar al resto.
/// </remarks>
[CollectionDefinition(Name)]
public sealed class DataDirectoryCollection
{
    public const string Name = "AppSettings.DataDirectoryPath";
}
