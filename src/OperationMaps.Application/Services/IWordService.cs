using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OperationMaps.Application.Word;

namespace OperationMaps.Application.Services
{
  public interface IWordService
  {
    /// <summary>
    /// Fills a Word template with form data and returns the result as bytes.
    /// </summary>
    /// <param name="data">Form data (components + header fields).</param>
    /// <param name="templatePath">Absolute path to the source <c>template.docx</c>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Raw bytes of the filled <c>.docx</c> file.</returns>
    Task<byte[]> ExportAsync(
        WordFormData data,
        string templatePath,
        CancellationToken ct = default);

    /// <summary>
    /// Reads cell values from an existing filled document and returns
    /// a <see cref="WordFormData"/> that can be used to populate the database.
    /// </summary>
    /// <param name="formNumber">Form number, e.g. "4" or "67".</param>
    /// <param name="documentPath">Absolute path to the existing filled <c>.docx</c>.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<WordFormData> ImportAsync(
        string formNumber,
        string documentPath,
        CancellationToken ct = default);


  }
}
