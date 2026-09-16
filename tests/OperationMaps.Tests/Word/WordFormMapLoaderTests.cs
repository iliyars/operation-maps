using OperationMaps.Infrastructure.Word;
using Xunit;

namespace OperationMaps.Tests.Word;

public class WordFormMapLoaderTests
{
  private static string ResourcesDir =>
      Path.Combine(AppContext.BaseDirectory, "Word", "Resources");

  [Fact]
  public void Ctor_throws_when_base_directory_missing()
  {
    var missing = Path.Combine(AppContext.BaseDirectory, "no-such-dir-" + Guid.NewGuid());
    Assert.Throws<DirectoryNotFoundException>(() => new WordFormMapLoader(missing));
  }

  [Fact]
  public void Load_parses_real_form4_map()
  {
    var loader = new WordFormMapLoader(ResourcesDir);

    var map = loader.Load("4");

    Assert.Equal("4", map.FormNumber);
    Assert.Equal(4, map.ComponentsPerPage);
    Assert.Equal(4, map.ComponentSlots.Count);
    // Form4 uses the old { row, col } format — loader must map col -> NtdCol
    // with no scheme/pins column.
    var firstSlotRow1 = map.ComponentSlots[0].ParameterCells["1"];
    Assert.Null(firstSlotRow1.SchemeCol);
    Assert.Null(firstSlotRow1.PinsCol);
    Assert.True(map.ComponentSlots.All(s => s.NoteCell.HasValue));
  }

  [Fact]
  public void Load_caches_result_on_second_call()
  {
    var loader = new WordFormMapLoader(ResourcesDir);

    var first = loader.Load("4");
    var second = loader.Load("4");

    Assert.Same(first, second);
  }

  [Fact]
  public void Load_throws_for_unknown_form()
  {
    var loader = new WordFormMapLoader(ResourcesDir);

    Assert.Throws<FileNotFoundException>(() => loader.Load("9999"));
  }

  [Fact]
  public void GetTemplatePath_throws_when_template_missing()
  {
    var loader = new WordFormMapLoader(ResourcesDir);

    Assert.Throws<FileNotFoundException>(() => loader.GetTemplatePath("9999"));
  }

  [Fact]
  public void Load_parses_new_format_with_scheme_and_pins_columns()
  {
    var dir = CreateTempResourcesDir();
    try
    {
      var loader = new WordFormMapLoader(dir);

      var map = loader.Load("64");

      var coord = map.ComponentSlots[0].ParameterCells["1"];
      Assert.Equal(0, coord.NtdCol); // col 1 -> 0-based 0
      Assert.Equal(1, coord.SchemeCol); // schemeCol 2 -> 0-based 1
      Assert.Equal(2, coord.PinsCol); // pins 3 -> 0-based 2
    }
    finally
    {
      Directory.Delete(dir, recursive: true);
    }
  }

  [Fact]
  public void Load_throws_when_slot_count_does_not_match_componentsPerPage()
  {
    var dir = CreateTempResourcesDir();
    try
    {
      // componentsPerPage says 2 but only one slot is provided.
      var formDir = Path.Combine(dir, "Form70");
      Directory.CreateDirectory(formDir);
      File.WriteAllText(Path.Combine(formDir, "map.json"), """
        {
          "componentsPerPage": 2,
          "componentSlots": [
            { "metaCells": {}, "parameterCells": {} }
          ]
        }
        """);

      var loader = new WordFormMapLoader(dir);

      Assert.Throws<InvalidDataException>(() => loader.Load("70"));
    }
    finally
    {
      Directory.Delete(dir, recursive: true);
    }
  }

  private static string CreateTempResourcesDir()
  {
    var dir = Path.Combine(Path.GetTempPath(), "OperationMapsTests_" + Guid.NewGuid());
    var formDir = Path.Combine(dir, "Form64");
    Directory.CreateDirectory(formDir);
    File.WriteAllText(Path.Combine(formDir, "map.json"), """
      {
        "componentsPerPage": 1,
        "componentSlots": [
          {
            "metaCells": {},
            "parameterCells": {
              "1": { "row": 1, "ntdCol": 1, "schemeCol": 2, "pins": 3 }
            }
          }
        ]
      }
      """);
    return dir;
  }
}
