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

    _matcher = new ComponentMatcher(TestCatalogDbContextFactory.From(_db), new ComponentNameParser());

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

  [Fact]
  public async Task Formatting_variations_match_and_keep_xml_name_for_display_and_word()
  {
    var xmlName = KnownDesignation.Replace("-", "–").Replace(" ", "\u00a0");
    var imported = Imported(xmlName, Category);
    var single = await _matcher.MatchAsync(imported);
    var batch = await _matcher.MatchAllAsync([imported]);
    Assert.True(single.IsMatched);
    var entry = Assert.Single(batch.Matched);
    Assert.Equal(single.MatchedComponent!.Id, entry.MatchResult.MatchedComponent!.Id);
    Assert.Equal(xmlName, imported.RawName);
    var vm = new OperationMaps.Wpf.Features.Components.ProjectComponentVm(entry);
    Assert.Equal(xmlName, vm.Name);
    var data = OperationMaps.Wpf.Features.OwnForm.OwnFormWordDataBuilder.Build("67", "", [],
      [new OperationMaps.Wpf.Features.OwnForm.FormColumnVm(vm)]);
    Assert.Equal(xmlName, Assert.Single(data.Components).Name);
    Assert.Equal(KnownDesignation, await _db.Components.Select(c => c.FullName).SingleAsync());
  }

  [Fact]
  public async Task Hyphen_or_space_before_tolerance_letter_suffix_matches()
  {
    var family = await _db.Families.SingleAsync();
    _db.Components.Add(new Component { FamilyId = family.Id, FullName = "Р2-105-0,75-0,75 Ом+-1%-А" });
    await _db.SaveChangesAsync();
    Assert.True((await _matcher.MatchAsync(Imported("Р2-105-0,75-0,75 Ом+-1% А", "Резистор"))).IsMatched);
  }

  [Fact]
  public async Task Quotes_spaces_and_visual_latin_cyrillic_letters_are_compared_without_rewriting_names()
  {
    var family = await _db.Families.SingleAsync();
    _db.Components.Add(new Component { FamilyId = family.Id, FullName = "ОС К53-68 «А»-50 В-0,1 мкФ +-10%" });
    await _db.SaveChangesAsync();
    var xml = Imported("ОС К53-68\"A\"-50В-0,1мкФ+-10 %", Category);
    Assert.True((await _matcher.MatchAsync(xml)).IsMatched);
  }

  [Fact]
  public async Task Ambiguous_normalized_names_require_manual_choice_but_exact_match_wins()
  {
    var family = await _db.Families.SingleAsync();
    var original = await _db.Components.SingleAsync();
    var variant = new Component { FamilyId = family.Id, FullName = KnownDesignation.Replace(" ", "") };
    _db.Components.Add(variant);
    await _db.SaveChangesAsync();
    Assert.Equal(original.Id, (await _matcher.MatchAsync(Imported(KnownDesignation, Category))).MatchedComponent?.Id);
    Assert.Equal(variant.Id, (await _matcher.MatchAsync(Imported(variant.FullName, Category))).MatchedComponent?.Id);
    var result = await _matcher.MatchAllAsync([Imported(KnownDesignation.Replace("-", "–"), Category)]);
    Assert.Empty(result.Matched);
    Assert.Contains("Неоднозначное", Assert.Single(result.Unresolved).MatchResult.Warning);
    Assert.Null(result.Unresolved[0].MatchResult.MatchedComponent);
    Assert.Null(result.Unresolved[0].MatchResult.MatchedFamily);
    Assert.Empty(result.Unresolved[0].MatchResult.RequiredForms);
  }

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task Family_normalization_ambiguity_is_explicit_and_does_not_select_a_family(bool reverseOrder)
  {
    var family = await _db.Families.SingleAsync();
    var firstName = reverseOrder ? "К10–79" : "K10-79";
    var secondName = reverseOrder ? "K10-79" : "К10–79";
    family.Name = firstName;
    _db.Families.Add(new Family { Name = secondName, ComponentTypeId = family.ComponentTypeId });
    await _db.SaveChangesAsync();

    var imported = Imported(UnseededDesignationSameFamily, Category);
    var batch = await _matcher.MatchAllAsync([imported]);
    var result = Assert.Single(batch.Unresolved).MatchResult;
    Assert.Empty(batch.Matched);
    Assert.False(result.IsMatched);
    Assert.NotNull(result.MatchedType);
    Assert.Null(result.MatchedFamily);
    Assert.Null(result.MatchedComponent);
    Assert.Empty(result.RequiredForms);
    Assert.Contains("Неоднозначное семейство", result.Warning);
    Assert.Contains(firstName, result.Warning);
    Assert.Contains(secondName, result.Warning);
    Assert.Equal(result.Warning, Assert.Single(batch.Warnings));
    Assert.Equal(UnseededDesignationSameFamily, imported.RawName);
  }

  [Fact]
  public async Task Exact_family_wins_over_a_normalized_collision()
  {
    var family = await _db.Families.SingleAsync();
    _db.Families.Add(new Family { Name = "K10-79", ComponentTypeId = family.ComponentTypeId });
    await _db.SaveChangesAsync();

    var result = await _matcher.MatchAsync(Imported(UnseededDesignationSameFamily, Category));
    Assert.False(result.IsMatched); // family alone is not an exact component
    Assert.Equal(family.Id, result.MatchedFamily?.Id);
    Assert.Equal("67", Assert.Single(result.RequiredForms).Number);
    Assert.DoesNotContain("Неоднозначное", result.Warning);
  }

  [Fact]
  public async Task Single_normalized_family_keeps_required_forms_without_inventing_a_component()
  {
    var family = await _db.Families.SingleAsync();
    family.Name = "K10-79";
    await _db.SaveChangesAsync();

    var result = await _matcher.MatchAsync(Imported(UnseededDesignationSameFamily, Category));
    Assert.False(result.IsMatched);
    Assert.Null(result.MatchedComponent);
    Assert.Equal(family.Id, result.MatchedFamily?.Id);
    Assert.Equal("67", Assert.Single(result.RequiredForms).Number);
  }

  [Theory]
  [InlineData("Деталь-0,125-1МОм", "Деталь-0,25-1МОм")]
  [InlineData("Деталь-0,125-1МОм", "Деталь-0,125-1мОм")]
  [InlineData("Деталь-12", "Деталь-1-2")]
  [InlineData("ОСМ 1325ЕР1У", "1325ЕР1У")]
  [InlineData("ОСМ 1325ЕР1У", "ОС 1325ЕР1У")]
  [InlineData("Деталь-10В+-1%", "Деталь-10В+-5%")]
  [InlineData("Деталь-10В", "Деталь-100В")]
  [InlineData("Деталь-10мА", "Деталь-10А")]
  public async Task Meaningful_differences_are_not_ignored(string catalog, string xml)
  {
    var family = await _db.Families.SingleAsync();
    _db.Components.Add(new Component { FamilyId = family.Id, FullName = catalog });
    await _db.SaveChangesAsync();
    Assert.False((await _matcher.MatchAsync(Imported(xml, Category))).IsMatched);
  }

  [Fact]
  public async Task Boards_need_no_catalog_entries_in_single_or_batch_matching()
  {
    var board = new ImportedComponent
    { RawName = "ВЕИР.687282.840", DetectedCategory = "Плата", Positions = ["A1"] };
    var single = await _matcher.MatchAsync(board);
    Assert.True(single.IsMatched);
    Assert.Null(single.Warning);
    Assert.Null(single.MatchedComponent);
    Assert.Empty(single.RequiredForms);
    var batch = await _matcher.MatchAllAsync([board]);
    Assert.Single(batch.Matched);
    Assert.Empty(batch.Unresolved);
    Assert.Empty(batch.Warnings);
    var vm = new OperationMaps.Wpf.Features.Components.ProjectComponentVm(batch.Matched.Single());
    Assert.True(vm.IsMatched);
    Assert.False(vm.HasFamily);
    Assert.Equal(OperationMaps.Wpf.Features.Components.ComponentMatchStatus.Automatic, vm.MatchStatus);
  }

  private static ImportedComponent Imported(string rawName, string category, int index = 0) => new()
  {
    ImportIndex = index,
    RawName = rawName,
    DetectedCategory = category,
    Positions = ["C1"],
  };

  [Theory]
  [InlineData("Модуль фильтрации и защиты")]
  [InlineData("Резистор")]
  public async Task Exact_name_uses_catalog_type_family_and_forms_when_xml_category_differs(string xmlCategory)
  {
    var imported = Imported(KnownDesignation, xmlCategory);
    var single = await _matcher.MatchAsync(imported);
    Assert.True(single.IsMatched);
    Assert.Equal(Category, single.MatchedType!.Name);
    Assert.Equal("К10-79", single.MatchedFamily!.Name);
    Assert.Contains(single.RequiredForms, f => f.Number == "67");
    Assert.Null(single.Warning);
    var batch = await _matcher.MatchAllAsync([imported]);
    var entry = Assert.Single(batch.Matched);
    Assert.Equal(single.MatchedComponent!.Id, entry.MatchResult.MatchedComponent!.Id);
    Assert.Equal(Category, entry.MatchResult.MatchedType!.Name);
    Assert.Contains(entry.MatchResult.RequiredForms, f => f.Number == "67");
    Assert.Empty(batch.Unresolved);
    Assert.Empty(batch.Warnings);
  }

  [Fact]
  public async Task Repeated_match_reads_catalog_changes_without_recreating_service()
  {
    var imported = Imported(KnownDesignation, Category);
    var before = await _matcher.MatchAsync(imported);
    Assert.False(before.MatchedComponent!.NeedsAdminReview);

    await using (var writer = TestCatalogDbContextFactory.From(_db).CreateDbContext())
    {
      var component = await writer.Components.SingleAsync(c => c.FullName == KnownDesignation);
      component.NeedsAdminReview = true;
      await writer.SaveChangesAsync();
    }

    var after = await _matcher.MatchAsync(imported);
    Assert.True(after.MatchedComponent!.NeedsAdminReview);
    Assert.False(before.MatchedComponent.NeedsAdminReview);
  }

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
  public async Task Unknown_xml_type_reaches_unresolved_components_without_losing_name_or_positions()
  {
    using var xml = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(
        "<AVS><RecordsData><Record field_5=\"UNKNOWN123\" field_32=\"A1, A2\"/></RecordsData></AVS>"));
    var imported = await new OperationMaps.Infrastructure.Importing.Pe3XmlImporter(new ComponentNameParser()).ImportAsync(xml);
    var result = await _matcher.MatchAllAsync(imported.Components);

    Assert.Empty(result.Matched);
    var unresolved = Assert.Single(result.Unresolved);
    Assert.Equal("UNKNOWN123", unresolved.Imported.RawName);
    Assert.Equal(["A1", "A2"], unresolved.Imported.Positions);
    Assert.Empty(unresolved.MatchResult.RequiredForms);
    Assert.NotEmpty(imported.Warnings);
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
