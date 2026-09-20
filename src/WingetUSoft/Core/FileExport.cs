using System.Text;

namespace WingetUSoft;

/// <summary>
/// Escribe los archivos que exporta la aplicación (F-27).
/// </summary>
/// <remarks>
/// Hasta F-27 se escribían con <c>Windows.Storage.FileIO.WriteTextAsync</c>, que necesita un <c>StorageFile</c>
/// —lo que devolvían los selectores viejos—. Los de Windows App SDK devuelven una ruta, así que se escribe con
/// <see cref="File"/>. La marca de orden de bytes (BOM) se conserva a propósito: es lo que hacía
/// <c>WriteTextAsync</c> y es lo que hace que Excel abra un CSV con acentos sin destrozarlos.
/// </remarks>
internal static class FileExport
{
    private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);

    internal static Task WriteTextWithBomAsync(string path, string text) =>
        File.WriteAllTextAsync(path, text, Utf8WithBom);
}
