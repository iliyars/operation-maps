using Microsoft.EntityFrameworkCore;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Domain.Enums;

namespace OperationMaps.Infrastructure.Persistence;

public static class DatabaseSeeder
{
  public static async Task SeedAsync(CatalogDbContext db)
  {
    if (await db.Forms.AnyAsync(f => f.Number == "4"))
      return;

    // ── 1. Форма 4 ────────────────────────────────────────────────────────
    var form4 = new Form
    {
      Number = "4",
      Title = "Карта оценки номенклатуры ЭРИ и сведений о соответствии условий эксплуатации и показателей надёжности требованиям НТД",
      IsUniversal = true,
      ColumnGrouping = ColumnGrouping.PerRegimeGroup,
    };

    var sec4Rel = new FormSection { Form = form4, Title = "Требования на изделие", Order = 1 };
    var sec4Op = new FormSection { Form = form4, Title = "Условия эксплуатации в аппаратуре", Order = 2 };
    form4.Sections.AddRange(new[] { sec4Rel, sec4Op });

    var params4 = new List<FormParameter>
        {
            new() { Form = form4, Section = sec4Rel, RowNumber =  1, Order =  1, Name = "Показатель ресурса",                                Unit = "ч"           },
            new() { Form = form4, Section = sec4Rel, RowNumber =  2, Order =  2, Name = "Показатель срока службы",                          Unit = "лет"         },
            new() { Form = form4, Section = sec4Rel, RowNumber =  3, Order =  3, Name = "Показатель сохраняемости",                         Unit = "лет"         },
            new() { Form = form4, Section = sec4Op,  RowNumber =  4, Order =  1, Name = "Акустический шум: диапазон частот",                Unit = "Гц"          },
            new() { Form = form4, Section = sec4Op,  RowNumber =  5, Order =  2, Name = "Акустический шум: уровень звукового давления",     Unit = "дБ"          },
            new() { Form = form4, Section = sec4Op,  RowNumber =  6, Order =  3, Name = "Линейное ускорение",                               Unit = "М.С.Е-2.(G)" },
            new() { Form = form4, Section = sec4Op,  RowNumber =  7, Order =  4, Name = "Давление окружающей среды: пониженное",            Unit = "мм рт. ст."  },
            new() { Form = form4, Section = sec4Op,  RowNumber =  8, Order =  5, Name = "Давление окружающей среды: повышенное",            Unit = "атм"         },
            new() { Form = form4, Section = sec4Op,  RowNumber =  9, Order =  6, Name = "Предельная (рабочая) температура: пониженная",     Unit = "°С"          },
            new() { Form = form4, Section = sec4Op,  RowNumber = 10, Order =  7, Name = "Предельная (рабочая) температура: повышенная",     Unit = "°С"          },
            new() { Form = form4, Section = sec4Op,  RowNumber = 11, Order =  8, Name = "Относительная влажность",                         Unit = "%"           },
            new() { Form = form4, Section = sec4Op,  RowNumber = 12, Order =  9, Name = "Относительная влажность: температура",            Unit = "°С"          },
            new() { Form = form4, Section = sec4Op,  RowNumber = 13, Order = 10, Name = "Роса, иней",                                      Unit = null          },
            new() { Form = form4, Section = sec4Op,  RowNumber = 14, Order = 11, Name = "Стойкость к воздействию специальных факторов, 7К", Unit = null          },
        };
    form4.Parameters.AddRange(params4);
    form4.ValueColumns.Add(new FormValueColumn
    {
      Form = form4,
      Key = "ntd",
      Title = "По НТД",
      Source = ColumnSource.DatasheetNdt,
      Order = 1,
    });
    db.Forms.Add(form4);

    // ── 2. Форма 67 ───────────────────────────────────────────────────────
    var form67 = new Form
    {
      Number = "67",
      Title = "Карта рабочих режимов конденсаторов, конденсаторных сборок, помехоподавляющих фильтров и ионисторов",
      IsUniversal = false,
      ColumnGrouping = ColumnGrouping.PerRegimeGroup,
    };

    var sec67V = new FormSection { Form = form67, Title = "Напряжение, В", Order = 1 };
    var sec67I = new FormSection { Form = form67, Title = "Максимальный ток, А", Order = 2 };
    var sec67O = new FormSection { Form = form67, Title = "Прочие параметры", Order = 3 };
    form67.Sections.AddRange(new[] { sec67V, sec67I, sec67O });

    var params67 = new List<FormParameter>
        {
            new() { Form = form67, Section = sec67V, RowNumber =  1, Order = 1, Name = "Постоянное",                      Unit = "В"   },
    new() { Form = form67, Section = sec67V, RowNumber =  2, Order = 2, Name = "Переменное (амплитудное)",         Unit = "В"   },
    new() { Form = form67, Section = sec67V, RowNumber =  3, Order = 3, Name = "Импульсное",                      Unit = "В"   },
    new() { Form = form67, Section = sec67V, RowNumber =  4, Order = 4, Name = "Суммарное",                       Unit = "В",
            Formula = "row1+row2+row3" },
    new() { Form = form67, Section = sec67I, RowNumber =  5, Order = 1, Name = "Переменный",                      Unit = "А"   },
    new() { Form = form67, Section = sec67I, RowNumber =  6, Order = 2, Name = "Проходной",                       Unit = "А"   },
    new() { Form = form67, Section = sec67I, RowNumber =  7, Order = 3, Name = "Разрядный",                       Unit = "А"   },
    new() { Form = form67, Section = sec67O, RowNumber =  8, Order = 1, Name = "Длительность зарядки (не менее)", Unit = "с"   },
    new() { Form = form67, Section = sec67O, RowNumber =  9, Order = 2, Name = "Реактивная мощность",             Unit = "Вар" },
    new() { Form = form67, Section = sec67O, RowNumber = 10, Order = 3, Name = "Частота максимальная",             Unit = "Гц"  },
    new() { Form = form67, Section = sec67O, RowNumber = 11, Order = 4, Name = "Длительность импульса",           Unit = "мкс" },
    new() { Form = form67, Section = sec67O, RowNumber = 12, Order = 5, Name = "Температура окружающей среды",    Unit = "°С"  },
    new() { Form = form67, Section = sec67O, RowNumber = 13, Order = 6, Name = "Температура перегрева",           Unit = "°С"  },
    new() { Form = form67, Section = sec67O, RowNumber = 14, Order = 7, Name = "Коэффициент нагрузки",            Unit = null  },
        };
    form67.Parameters.AddRange(params67);
    form67.ValueColumns.AddRange(new[]
    {
            new FormValueColumn { Form = form67, Key = "scheme", Title = "В схеме", Source = ColumnSource.ManualScheme, Order = 1 },
            new FormValueColumn { Form = form67, Key = "ntd",    Title = "По НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
        });
    db.Forms.Add(form67);

    // ── 3. Типы ───────────────────────────────────────────────────────────
    var typeResistor = new ComponentType { Name = "Резистор" };
    var typeCapacitor = new ComponentType { Name = "Конденсатор" };
    var typeMicrocircuit = new ComponentType { Name = "Микросхема" };
    var typeGenerator = new ComponentType { Name = "Генератор" };
    var typeConnector = new ComponentType { Name = "Вилка" };
    db.ComponentTypes.AddRange(typeResistor, typeCapacitor, typeMicrocircuit, typeGenerator, typeConnector);

    // ── 4. Примечания ─────────────────────────────────────────────────────
    var noteOblRezhim = new Note
    {
      Text = "Используется в облегчённом режиме, при U ≤ 0,6U_ном. " +
               "Стойкость к условиям пониженного давления обеспечивается применением дополнительных средств защиты"
    };
    db.Notes.Add(noteOblRezhim);

    // ── 5. Семейства + FamilyNtdValues (Форма 4) ─────────────────────────
    FormParameter P4(int row) => params4.First(p => p.RowNumber == row);
    FamilyNtdValue FNtd(Family f, int row, string val) =>
        new() { Family = f, FormParameter = P4(row), Value = val };

    var famR18MP = new Family { Name = "ОСМ Р1-8МП", ComponentType = typeResistor };
    famR18MP.NtdValues.AddRange(new[]
    {
            FNtd(famR18MP,  1, "100000"), FNtd(famR18MP,  2, "25"),
            FNtd(famR18MP,  3, "25"),     FNtd(famR18MP,  4, "—"),
            FNtd(famR18MP,  5, "—"),      FNtd(famR18MP,  6, "—"),
            FNtd(famR18MP,  7, "10⁻⁶"),  FNtd(famR18MP,  8, "—"),
            FNtd(famR18MP,  9, "−60 (−60)"), FNtd(famR18MP, 10, "+85 (+85)"),
            FNtd(famR18MP, 11, "98"),     FNtd(famR18MP, 12, "+35"),
            FNtd(famR18MP, 13, "—"),      FNtd(famR18MP, 14, "—"),
        });

    var famK1079 = new Family { Name = "К10-79", ComponentType = typeCapacitor };
    famK1079.NtdValues.AddRange(new[]
    {
            FNtd(famK1079,  1, "150000"), FNtd(famK1079,  2, "25"),
            FNtd(famK1079,  3, "25"),     FNtd(famK1079,  4, "100-10000"),
            FNtd(famK1079,  5, "175"),    FNtd(famK1079,  6, "5000 (500)"),
            FNtd(famK1079,  7, "10⁻⁶"),  FNtd(famK1079,  8, "2,88"),
            FNtd(famK1079,  9, "−60 (−60)"), FNtd(famK1079, 10, "+85 (+60)"),
            FNtd(famK1079, 11, "98"),     FNtd(famK1079, 12, "—"),
            FNtd(famK1079, 13, "—"),      FNtd(famK1079, 14, "—"),
        });

    var famK5367 = new Family { Name = "К53-67", ComponentType = typeCapacitor };
    famK5367.NtdValues.AddRange(new[]
    {
            FNtd(famK5367,  1, "150000"), FNtd(famK5367,  2, "25"),
            FNtd(famK5367,  3, "25"),     FNtd(famK5367,  4, "—"),
            FNtd(famK5367,  5, "—"),      FNtd(famK5367,  6, "—"),
            FNtd(famK5367,  7, "10⁻⁶"),  FNtd(famK5367,  8, "—"),
            FNtd(famK5367,  9, "−60 (−60)"), FNtd(famK5367, 10, "+125 (+85)"),
            FNtd(famK5367, 11, "98"),     FNtd(famK5367, 12, "+35"),
            FNtd(famK5367, 13, "—"),      FNtd(famK5367, 14, "7К1=2К, 7К4=2К"),
        });

    // ── 6. Тестовый компонент К10-79 с NTD для Формы 67 ──────────────────
    FormParameter P67(int row) => params67.First(p => p.RowNumber == row);
    ComponentNtdValue CNtd(Component c, int row, string val) =>
        new() { Component = c, FormParameter = P67(row), Value = val };

    var compK1079test = new Component
    {
      FullName = "К10-79-25 В-0,1 мкФ+80%-20%-Н90",
      Family = famK1079,
      OwnForm = form67,
      NeedsAdminReview = false,
    };

    compK1079test.NtdValues.AddRange(new[]
    {
            CNtd(compK1079test,  1, "16"),  CNtd(compK1079test,  2, "—"),
            CNtd(compK1079test,  3, "—"),   CNtd(compK1079test,  4, "16"),
            CNtd(compK1079test,  5, "—"),   CNtd(compK1079test,  6, "—"),
            CNtd(compK1079test,  7, "—"),   CNtd(compK1079test,  8, "—"),
            CNtd(compK1079test,  9, "—"),   CNtd(compK1079test, 10, "—"),
            CNtd(compK1079test, 11, "—"),   CNtd(compK1079test, 12, "60"),
            CNtd(compK1079test, 13, "—"),   CNtd(compK1079test, 14, "0,7"),
        });

    famK1079.Components.Add(compK1079test);
    db.Families.AddRange(famR18MP, famK1079, famK5367);

    // ── SaveChanges 1 — получаем Id ───────────────────────────────────────
    await db.SaveChangesAsync();

    // ── 7. FamilyForm ─────────────────────────────────────────────────────
    db.FamilyForms.AddRange(
        new FamilyForm { FamilyId = famR18MP.Id, FormId = form4.Id },
        new FamilyForm { FamilyId = famK1079.Id, FormId = form4.Id },
        new FamilyForm { FamilyId = famK5367.Id, FormId = form4.Id }
    );

    // ── 8. FamilyNote ─────────────────────────────────────────────────────
    db.FamilyNotes.AddRange(
        new FamilyNote { FamilyId = famK1079.Id, FormParameterId = P4(1).Id, NoteId = noteOblRezhim.Id },
        new FamilyNote { FamilyId = famK5367.Id, FormParameterId = P4(1).Id, NoteId = noteOblRezhim.Id }
    );

    await db.SaveChangesAsync();
  }
}
