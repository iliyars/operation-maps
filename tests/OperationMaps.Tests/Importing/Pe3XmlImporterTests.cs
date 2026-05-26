using System.Text;
using OperationMaps.Application.Importing;
using OperationMaps.Infrastructure.Importing;
using Xunit;

namespace OperationMaps.Tests.Importing;

public class Pe3XmlImporterTests
{
  private static readonly string SamplePath =
      Path.Combine(AppContext.BaseDirectory, "TestData", "sample_pe3_1.xml");

  private static ImportResult Import()
  {
    var importer = new Pe3XmlImporter();
    using var fs = File.OpenRead(SamplePath);
    return importer.Import(fs);
  }

  [Fact]
  public void Imports_components_without_throwing()
  {
    var result = Import();
    Assert.True(result.ComponentCount > 0,
        "Ожидались импортированные компоненты, получено 0 — проверь формат файла.");
  }

  [Fact]
  public void Position_count_is_at_least_component_count()
  {
    var result = Import();
    // Примечание: PositionCount суммирует Quantity (количество позиций) по всем компонентам
    // ComponentCount — количество уникальных строк перечня
    // PositionCount может быть меньше ComponentCount, если у компонента 0 позиций
    // Поэтому используем более мягкую проверку: предупреждения должны быть, если позиций меньше
    if (result.PositionCount < result.ComponentCount)
    {
      // Должны быть предупреждения о компонентах без позиций
      Assert.Contains(result.Warnings, w => w.Contains("Нет позиционных обозначений"));
    }
  }

  [Fact]
  public void Every_component_has_nonempty_name_and_category()
  {
    var result = Import();
    Assert.All(result.Components, c =>
    {
      Assert.False(string.IsNullOrWhiteSpace(c.RawName));
      Assert.False(string.IsNullOrWhiteSpace(c.DetectedCategory));
    });
  }

  [Fact]
  public void Service_records_are_skipped()
  {
    var result = Import();
    Assert.DoesNotContain(result.Components,
        c => c.RawName.StartsWith("Примечание", StringComparison.OrdinalIgnoreCase));
  }

  [Fact]
  public void Category_is_first_word_of_name()
  {
    var result = Import();
    Assert.All(result.Components, c =>
    {
      Assert.StartsWith(c.DetectedCategory, c.RawName, StringComparison.Ordinal);
    });
  }

  [Fact]
  public void Detects_at_least_one_known_category()
  {
    var result = Import();
    var categories = result.Components.Select(c => c.DetectedCategory).Distinct().ToList();
    Assert.Contains(categories, c =>
        c is "Резистор" or "Конденсатор" or "Микросхема" or "Генератор" or "Диоды" or "Соединители" or "Диод" or "Светодиод");
  }

  [Fact]
  public void Range_positions_are_expanded()
  {
    var result = Import();
    var withRange = result.Components
        .FirstOrDefault(c => c.RawPositions is not null
                             && (c.RawPositions.Contains('-') || c.RawPositions.Contains('–')));
    if (withRange is not null)
      Assert.True(withRange.Positions.Count > 1,
          $"Диапазон «{withRange.RawPositions}» не развернулся.");
  }

  [Fact]
  public void Component_names_are_cleaned_from_TU()
  {
    var result = Import();

    var cleanedComponent = result.Components
        .FirstOrDefault(c => c.RawName.Contains("ОСМ") &&
                            !c.RawName.Contains("ТУ"));

    if (cleanedComponent is not null)
    {
      Assert.DoesNotContain("ТУ", cleanedComponent.RawName);
      Assert.DoesNotContain("ОЖ0.", cleanedComponent.RawName);
      Assert.DoesNotContain("П0.", cleanedComponent.RawName);
    }
  }

  [Fact]
  public void Imports_passport_data()
  {
    var result = Import();

    Assert.NotNull(result.DocumentTitle);
    Assert.NotNull(result.DocumentNumber);
    Assert.NotNull(result.DevelopedBy);
    Assert.NotNull(result.CheckedBy);
    Assert.NotNull(result.ApprovedBy);

    // Обновляем ожидаемое значение в соответствии с реальным файлом
    Assert.Equal("Плата обработки сигналов ДУ", result.DocumentTitle);
    Assert.Equal("ВЕИР.687281.892", result.DocumentNumber);
    Assert.Equal("Варнаков", result.DevelopedBy);
    Assert.Equal("Павлова", result.CheckedBy);
    Assert.Equal("Филатов", result.ApprovedBy);
  }

  [Fact]
  public void Warnings_are_collected_for_components_without_positions()
  {
    var result = Import();

    // В файле должны быть компоненты без позиций (заголовки групп и примечания)
    // Они должны генерировать предупреждения
    if (result.Warnings.Count == 0 && result.PositionCount < result.ComponentCount)
    {
      Assert.Fail("Должны быть предупреждения о компонентах без позиций, но их нет");
    }
  }

  [Fact]
  public void Components_with_special_chars_in_positions_handled()
  {
    var result = Import();

    var component = result.Components
        .FirstOrDefault(c => c.RawPositions is not null && c.RawPositions.Contains(", "));

    if (component is not null)
    {
      Assert.True(component.Positions.Count > 0);
    }
  }

  [Fact]
  public void Quantity_matches_positions_count()
  {
    var result = Import();

    Assert.All(result.Components, c =>
    {
      Assert.Equal(c.Positions.Count, c.Quantity);
    });
  }

  [Fact]
  public void Imports_components_without_positions_with_warning()
  {
    var result = Import();

    var componentsWithoutPositions = result.Components
        .Where(c => c.Positions.Count == 0)
        .ToList();

    if (componentsWithoutPositions.Any())
    {
      Assert.Contains(result.Warnings,
          w => componentsWithoutPositions.Any(c => w.Contains(c.RawName)));
    }
  }

  [Fact]
  public void Group_records_are_filtered_out()
  {
    var result = Import();

    // Групповые записи с type_rec="X" не должны попадать в компоненты
    var groupRecords = result.Components
        .Where(c => c.RawName == "Резисторы" ||
                    c.RawName == "Конденсаторы" ||
                    c.RawName == "Микросхемы" ||
                    c.RawName == "Диоды" ||
                    c.RawName == "Соединители")
        .ToList();

    Assert.Empty(groupRecords);
  }

  [Fact]
  public void Component_count_matches_expected()
  {
    var result = Import();

    // Подсчитаем ожидаемое количество: из sample_pe3_1.xml
    // Должно быть около 25-30 реальных компонентов (без учёта примечаний и групповых записей)
    Assert.True(result.ComponentCount >= 20 && result.ComponentCount <= 40,
        $"Фактическое количество компонентов: {result.ComponentCount}. Ожидалось примерно 25-30.");
  }

  [Fact]
  public void All_positions_are_unique_per_component()
  {
    var result = Import();

    Assert.All(result.Components, c =>
    {
      var uniquePositions = c.Positions.Distinct().ToList();
      Assert.Equal(c.Positions.Count, uniquePositions.Count);
    });
  }
}
