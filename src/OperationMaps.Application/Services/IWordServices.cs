using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OperationMaps.Application.Word;

namespace OperationMaps.Application.Services
{
  public interface IWordServices
  {
    Task ExportAsync(
      WordFormData data,
      string templatePath,
      string outputPath,
      CancellationToken ct = default);

    Task<WordFormData> ImportAsync(
      string filePath,
      CancellationToken ct = default);
  }
}
