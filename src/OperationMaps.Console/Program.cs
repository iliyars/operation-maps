using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OperationMaps.Application.Importing;
using OperationMaps.Infrastructure;
using OperationMaps.Infrastructure.Persistence;

namespace OperationMaps.Console;

class Program
{
  static async Task Main(string[] args)
  {
    System.Console.OutputEncoding = System.Text.Encoding.UTF8;

    // Расширяем консоль для длинных строк
    try
    {
      System.Console.WindowWidth = 140;
      System.Console.BufferWidth = 300;
    }
    catch
    {
      // Если консоль не поддерживает изменение размеров
    }

    var xmlPath = @"C:\Ilya\dev\CSharp\operation-maps\tests\OperationMaps.Tests\TestData\sample_pe3_1.XML";

    if (!File.Exists(xmlPath))
    {
      System.Console.WriteLine($"Файл не найден: {xmlPath}");
      System.Console.WriteLine("\nНажмите любую клавишу для выхода...");
      System.Console.ReadKey();
      return;
    }

    using var host = Host.CreateDefaultBuilder(args)
        .ConfigureServices((context, services) =>
        {
          var dbPath = Path.Combine(AppContext.BaseDirectory, "operationmaps_test.db");
          services.AddInfrastructure($"Data Source={dbPath}");
        })
        .Build();


    using var scope = host.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<OperationMapsDbContext>();
    await db.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(db);
    var importer = host.Services.GetRequiredService<IComponentListImporter>();

    await using var stream = File.OpenRead(xmlPath);
    var result = importer.Import(stream);

    System.Console.WriteLine("\n📋 ПАСПОРТНЫЕ ДАННЫЕ");
    System.Console.WriteLine($"  Наименование: {result.DocumentTitle}");
    System.Console.WriteLine($"  Обозначение:  {result.DocumentNumber}");
    System.Console.WriteLine($"  Разработчик:  {result.DevelopedBy}");
    System.Console.WriteLine($"  Проверил:     {result.CheckedBy}");
    System.Console.WriteLine($"  Утвердил:     {result.ApprovedBy}");

    System.Console.WriteLine($"\n📦 КОМПОНЕНТЫ: {result.ComponentCount}");
    System.Console.WriteLine($"📍 ПОЗИЦИИ:    {result.PositionCount}");

    if (result.Warnings.Any())
    {
      System.Console.WriteLine($"\n⚠️ ПРЕДУПРЕЖДЕНИЯ: {result.Warnings.Count}");
      foreach (var w in result.Warnings)
      {
        System.Console.WriteLine($"  - {w}");
      }
    }

    // Определяем максимальную длину имени для форматирования
    int maxNameLength = result.Components.Any()
        ? result.Components.Max(c => c.RawName.Length)
        : 40;
    maxNameLength = Math.Min(maxNameLength, 80); // Ограничиваем 80 символами

    System.Console.WriteLine("\n📋 СПИСОК КОМПОНЕНТОВ");
    var separator = new string('-', 15 + 25 + maxNameLength + 10);
    System.Console.WriteLine(separator);
    System.Console.WriteLine($"{"№",3} {"Категория",-15} {"Позиции",-25} {"Наименование"}");
    System.Console.WriteLine(separator);

    int i = 1;
    foreach (var c in result.Components)
    {
      var positions = string.Join(",", c.Positions.Take(5));
      if (c.Positions.Count > 5) positions += $"... +{c.Positions.Count - 5}";

      // Показываем полное имя (без обрезания)
      System.Console.WriteLine($"{i,3} {c.DetectedCategory,-15} {positions,-25} {c.RawName}");
      i++;
    }

    System.Console.WriteLine(separator);
    System.Console.WriteLine($"\n✅ Импорт завершён. Всего строк: {result.ComponentCount}");

    await ShowMatching(host.Services, result);

    System.Console.WriteLine("\n\nНажмите любую клавишу для выхода...");
    System.Console.ReadKey();
  }

  static async Task ShowMatching(IServiceProvider services, ImportResult result)
  {
    var matcher = services.GetRequiredService<IComponentMatcher>();
    var db = services.GetRequiredService<OperationMapsDbContext>();

    System.Console.WriteLine("\n\n🔍 МАТЧИНГ С БАЗОЙ ДАННЫХ");
    System.Console.WriteLine(new string('-', 85));

    var typesCount = await db.ComponentTypes.CountAsync();
    System.Console.WriteLine($"  Типов в БД: {typesCount}");

    if (typesCount == 0)
    {
      System.Console.WriteLine("\n⚠️ База данных пуста. Матчинг не будет работать.");
      System.Console.WriteLine("   Сначала заполните справочники (ComponentType, Family, Component)");
      return;
    }

    var familiesCount = await db.Families.CountAsync();
    var paramsCount = await db.FormParameters.CountAsync();
    System.Console.WriteLine($"  Семейств в БД: {familiesCount}");
    System.Console.WriteLine($"  Параметров форм: {paramsCount}");

    System.Console.WriteLine($"\n{"Категория",-15} {"Статус",-10} {"Семейство",-20} {"Наименование"}");
    System.Console.WriteLine(new string('-', 85));

    int matched = 0;
    int total = Math.Min(20, result.Components.Count);

    foreach (var c in result.Components.Take(total))
    {
      var match = await matcher.MatchAsync(c);
      var status = match.IsMatched ? "✅" : "❌";
      var family = match.MatchedFamily?.Name ?? "-";

      System.Console.WriteLine($"{c.DetectedCategory,-15} {status,-10} {family,-20} {c.RawName}");
      if (match.IsMatched) matched++;
    }

    System.Console.WriteLine($"\n🎯 Совпадений: {matched} из {total}");
  }
}
