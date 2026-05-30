using Microsoft.EntityFrameworkCore;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Domain.Enums;

namespace OperationMaps.Infrastructure.Persistence;

public static class DatabaseSeeder
{
  public static async Task SeedAsync(OperationMapsDbContext db)
  {
        if (await db.Froms.AnyAsync(f => f.Number == "4"))
            return;

        // ── 1. Форма 4 ────────────────────────────────────────────────────────
        var form4 = new Form
        {
            Number = "4",
            Title = "Карта оценки номенклатуры ЭРИ и сведений о соответствии условий эксплуатации и показателей надёжности требованиям НТД",
            IsUniversal = true,
            ColumnGrouping = ColumnGrouping.PerRegimeGroup,
        };

        var secReliability = new FormSection { Form = form4, Title = "Требования на изделие", Order = 1 };
        var secOperation = new FormSection { Form = form4, Title = "Условия эксплуатации в аппаратуре", Order = 2 };
        form4.Sections.AddRange(new[] { secReliability, secOperation });

        var parameters = new List<FormParameter>
        {
            new() { Form = form4, Section = secReliability, RowNumber =  1, Order =  1, Name = "Показатель ресурса",                                Unit = "ч"              },
            new() { Form = form4, Section = secReliability, RowNumber =  2, Order =  2, Name = "Показатель срока службы",                           Unit = "лет"            },
            new() { Form = form4, Section = secReliability, RowNumber =  3, Order =  3, Name = "Показатель сохраняемости",                          Unit = "лет"            },
            new() { Form = form4, Section = secOperation,   RowNumber =  4, Order =  1, Name = "Акустический шум: диапазон частот",                 Unit = "Гц"             },
            new() { Form = form4, Section = secOperation,   RowNumber =  5, Order =  2, Name = "Акустический шум: уровень звукового давления",      Unit = "дБ"             },
            new() { Form = form4, Section = secOperation,   RowNumber =  6, Order =  3, Name = "Линейное ускорение",                                Unit = "М.С.Е-2.(G)"   },
            new() { Form = form4, Section = secOperation,   RowNumber =  7, Order =  4, Name = "Давление окружающей среды: пониженное",             Unit = "мм рт. ст."    },
            new() { Form = form4, Section = secOperation,   RowNumber =  8, Order =  5, Name = "Давление окружающей среды: повышенное",             Unit = "атм"            },
            new() { Form = form4, Section = secOperation,   RowNumber =  9, Order =  6, Name = "Предельная (рабочая) температура: пониженная",      Unit = "°С"             },
            new() { Form = form4, Section = secOperation,   RowNumber = 10, Order =  7, Name = "Предельная (рабочая) температура: повышенная",      Unit = "°С"             },
            new() { Form = form4, Section = secOperation,   RowNumber = 11, Order =  8, Name = "Относительная влажность",                          Unit = "%"              },
            new() { Form = form4, Section = secOperation,   RowNumber = 12, Order =  9, Name = "Относительная влажность: температура",              Unit = "°С"             },
            new() { Form = form4, Section = secOperation,   RowNumber = 13, Order = 10, Name = "Роса, иней",                                       Unit = null             },
            new() { Form = form4, Section = secOperation,   RowNumber = 14, Order = 11, Name = "Стойкость к воздействию специальных факторов, 7К", Unit = null             },
        };

        form4.Parameters.AddRange(parameters);
        form4.ValueColumns.Add(new FormValueColumn
        {
            Form = form4,
            Key = "ntd",
            Title = "По НТД",
            Source = ColumnSource.DatasheetNdt,
            Order = 1,
        });

        db.Froms.Add(form4);

        // ── 2. Типы компонентов ───────────────────────────────────────────────
        var typeResistor = new ComponentType { Name = "Резистор" };
        var typeCapacitor = new ComponentType { Name = "Конденсатор" };
        var typeMicrocircuit = new ComponentType { Name = "Микросхема" };
        var typeGenerator = new ComponentType { Name = "Генератор" };
        var typeConnector = new ComponentType { Name = "Вилка" };

        db.ComponentTypes.AddRange(
            typeResistor, typeCapacitor, typeMicrocircuit, typeGenerator, typeConnector);

        // ── 3. Примечания ─────────────────────────────────────────────────────
        var noteOblRezhim = new Note
        {
            Text = "Используется в облегчённом режиме, при U ≤ 0,6U_ном. " +
                   "Стойкость к условиям пониженного давления обеспечивается применением дополнительных средств защиты"
        };
        db.Notes.Add(noteOblRezhim);

        // ── 4. Семейства + NtdValues ──────────────────────────────────────────
        FormParameter P(int row) => parameters.First(p => p.RowNumber == row);

        FamilyNtdValue Ntd(Family fam, int row, string val) =>
            new() { Family = fam, FormParameter = P(row), Value = val };

        // Р1-8МП (резисторы)
        var famR18MP = new Family { Name = "ОСМ Р1-8МП", ComponentType = typeResistor };
        famR18MP.NtdValues.AddRange(new[]
        {
            Ntd(famR18MP,  1, "100000"), Ntd(famR18MP,  2, "25"),
            Ntd(famR18MP,  3, "25"),     Ntd(famR18MP,  4, "—"),
            Ntd(famR18MP,  5, "—"),      Ntd(famR18MP,  6, "—"),
            Ntd(famR18MP,  7, "10⁻⁶"),  Ntd(famR18MP,  8, "—"),
            Ntd(famR18MP,  9, "−60 (−60)"), Ntd(famR18MP, 10, "+85 (+85)"),
            Ntd(famR18MP, 11, "98"),     Ntd(famR18MP, 12, "+35"),
            Ntd(famR18MP, 13, "—"),      Ntd(famR18MP, 14, "—"),
        });

        // К10-79 (конденсаторы)
        var famK1079 = new Family { Name = "К10-79", ComponentType = typeCapacitor };
        famK1079.NtdValues.AddRange(new[]
        {
            Ntd(famK1079,  1, "150000*"), Ntd(famK1079,  2, "25"),
            Ntd(famK1079,  3, "25"),      Ntd(famK1079,  4, "100-10000"),
            Ntd(famK1079,  5, "175"),     Ntd(famK1079,  6, "5000 (500)"),
            Ntd(famK1079,  7, "10⁻⁶"),   Ntd(famK1079,  8, "2,88"),
            Ntd(famK1079,  9, "−60 (−60)"), Ntd(famK1079, 10, "+85 (+60)"),
            Ntd(famK1079, 11, "98"),      Ntd(famK1079, 12, "—"),
            Ntd(famK1079, 13, "—"),       Ntd(famK1079, 14, "—"),
        });

        // К53-67 (конденсаторы)
        var famK5367 = new Family { Name = "К53-67", ComponentType = typeCapacitor };
        famK5367.NtdValues.AddRange(new[]
        {
            Ntd(famK5367,  1, "150000*"), Ntd(famK5367,  2, "25"),
            Ntd(famK5367,  3, "25"),      Ntd(famK5367,  4, "—"),
            Ntd(famK5367,  5, "—"),       Ntd(famK5367,  6, "—"),
            Ntd(famK5367,  7, "10⁻⁶"),   Ntd(famK5367,  8, "—"),
            Ntd(famK5367,  9, "−60 (−60)"), Ntd(famK5367, 10, "+125 (+85)"),
            Ntd(famK5367, 11, "98"),      Ntd(famK5367, 12, "+35"),
            Ntd(famK5367, 13, "—"),       Ntd(famK5367, 14, "7К1=2К, 7К4=2К"),
        });

        db.Families.AddRange(famR18MP, famK1079, famK5367);

        // ── Первый SaveChanges — получаем Id ──────────────────────────────────
        await db.SaveChangesAsync();

        // ── 5. FamilyForm — после получения Id ───────────────────────────────
        // Форма 4 обязательна для всех семейств
        db.FamilyForms.AddRange(
            new FamilyForm { FamilyId = famR18MP.Id, FormId = form4.Id },
            new FamilyForm { FamilyId = famK1079.Id, FormId = form4.Id },
            new FamilyForm { FamilyId = famK5367.Id, FormId = form4.Id }
        );

        // ── 6. FamilyNote — после получения Id ───────────────────────────────
        db.FamilyNotes.AddRange(
            new FamilyNote { FamilyId = famK1079.Id, FormParameterId = P(1).Id, NoteId = noteOblRezhim.Id },
            new FamilyNote { FamilyId = famK5367.Id, FormParameterId = P(1).Id, NoteId = noteOblRezhim.Id }
        );

        await db.SaveChangesAsync();
    }

}
