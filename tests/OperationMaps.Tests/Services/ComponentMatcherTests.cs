using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Importing;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Infrastructure.Services;
using Xunit;

namespace OperationMaps.Tests.Services;

public class ComponentMatcherTests : IDisposable
{
  // ImportedComponent.RawName is what Pe3XmlImporter stores AFTER stripping
  // the type word and any ТУ tail (parsed.Name) — not the original XML
  // field_5 string. "К10-79" is a real known family (see
  // ComponentNameParser.KnownFamilies), so ExtractFamilyForRcl resolves it
  // deterministically regardless of the surrounding text.
  private const string KnownDesignation = "К10-79-25 М-0,1 мкФ+80%-20%-В90";
  private const string UnseededDesignationSameFamily = "К10-79-50 М-0,01 мкФ+-20%-В30";
  private const string Category = "Конденсатор";

  private readonly SqliteConnection _connection;
  private readonly CatalogDbContext _db;
  private readonly ComponentMatcher _matcher;

  public ComponentMatcherTests()
  {
    // Keep-alive in-memory Sqlite connection — EF Core's Sqlite provider
    // needs an open connection for the in-memory DB to survive between
    // operations (this project has no reference to the real EF InMemory
    // provider, only Sqlite).
    _connection = new SqliteConnection("DataSource=:memory:");
    _connection.Open();

    var options = new DbContextOptionsBuilder<CatalogDbContext>()
        .UseSqlite(_connection)
        .Options;

    _db = new CatalogDbContext(options);
    _db.Database.EnsureCreated();

    _matcher = new ComponentMatcher(_db, new ComponentNameParser());

    Seed();
  }

  private void Seed()
  {
    var capacitorType = new ComponentType { Name = Category };
    var form67 = new Form { Number = "67", Title = "Форма 67" };

    var family = new Family
    {
      Name = "К10-79",
      ComponentType = capacitorType,
    };
    family.FamilyForms.Add(new FamilyForm { Family = family, Form = form67 });

    var component = new Component
    {
      FullName = KnownDesignation,
      Family = family,
    };

    _db.ComponentTypes.Add(capacitorType);
    _db.Forms.Add(form67);
    _db.Families.Add(family);
    _db.Components.Add(component);
    _db.SaveChanges();
  }

  private static ImportedComponent Imported(string rawName, string category, int index = 0) => new()
  {
    ImportIndex = index,
    RawName = rawName,
    DetectedCategory = category,
    Positions = ["C1"],
  };

  [Fact]
  public async Task MatchAsync_finds_known_component_and_its_required_form()
  {
    var imported = Imported(KnownDesignation, Category);

    var result = await _matcher.MatchAsync(imported);

    Assert.True(result.IsMatched);
    Assert.NotNull(result.MatchedComponent);
    Assert.NotNull(result.MatchedFamily);
    Assert.Equal("К10-79", result.MatchedFamily!.Name);
    Assert.Contains(result.RequiredForms, f => f.Number == "67");
    Assert.Null(result.Warning);
  }

  [Fact]
  public async Task MatchAsync_returns_unmatched_for_unknown_type()
  {
    var imported = Imported("2Д212А", "Диод");

    var result = await _matcher.MatchAsync(imported);

    Assert.False(result.IsMatched);
    Assert.Null(result.MatchedType);
    Assert.Contains("Неизвестный тип", result.Warning);

    // Regression: RequiredForms used to have no default, so it was null
    // here — ShellViewModel.OnProjectLoaded does
    // .SelectMany(e => e.MatchResult.RequiredForms) over every imported
    // component, which threw NullReferenceException the moment a project
    // contained even one totally unrecognized component type.
    Assert.NotNull(result.RequiredForms);
    Assert.Empty(result.RequiredForms);
  }

  [Fact]
  public async Task MatchAsync_finds_type_and_family_but_not_component_for_unknown_designation()
  {
    // Same family as the seeded component, but a designation that isn't in
    // the catalog — should still resolve type + family + required forms,
    // just not IsMatched.
    var imported = Imported(UnseededDesignationSameFamily, Category);

    var result = await _matcher.MatchAsync(imported);

    Assert.False(result.IsMatched);
    Assert.NotNull(result.MatchedFamily);
    Assert.Contains(result.RequiredForms, f => f.Number == "67");
    Assert.Contains("не найден", result.Warning);
  }

  [Fact]
  public async Task MatchAllAsync_batches_and_matches_the_same_way_as_MatchAsync()
  {
    var known = Imported(KnownDesignation, Category, 0);
    var unknownType = Imported("2Д212А", "Диод", 1);

    var result = await _matcher.MatchAllAsync([known, unknownType]);

    Assert.Single(result.Matched);
    Assert.Single(result.Unresolved);
    Assert.Equal(known, result.Matched[0].Imported);
    Assert.Equal(unknownType, result.Unresolved[0].Imported);
    Assert.Single(result.Warnings);

    // Regression: this is the exact shape ShellViewModel.OnProjectLoaded
    // consumes (matchResult.Matched.Concat(Unresolved).SelectMany(e =>
    // e.MatchResult.RequiredForms)) — it crashed on any project containing
    // an unrecognized component type before RequiredForms got a default.
    var allForms = result.Matched.Concat(result.Unresolved)
        .SelectMany(e => e.MatchResult.RequiredForms)
        .ToList();
    Assert.Single(allForms);
  }

  public void Dispose()
  {
    _db.Dispose();
    _connection.Dispose();
  }
}
