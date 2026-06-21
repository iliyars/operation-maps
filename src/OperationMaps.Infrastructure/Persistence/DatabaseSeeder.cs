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


                var form4 = AddForm4(db);
                AddForm6(db);
                AddForm7(db);
                AddForm8(db);
                AddForm9(db);
                AddForm13(db);
                AddForm14(db);
                AddForm15(db);
                AddForm16(db);
                AddForm17(db);
                AddForm19(db);
                AddForm20(db);
                AddForm21(db);
                AddForm22(db);
                AddForm24(db);
                AddForm25(db);
                AddForm26(db);
                AddForm28(db);
                AddForm30(db);
                AddForm31(db);
                AddForm32(db);
                AddForm33(db);
                AddForm34(db);
                AddForm36(db);
                AddForm37(db);
                AddForm38(db);
                AddForm39(db);
                AddForm40(db);
                AddForm41(db);
                AddForm44(db);
                AddForm45(db);
                AddForm46(db);
                AddForm47(db);
                AddForm48(db);
                AddForm49(db);
                AddForm50(db);
                AddForm51(db);
                AddForm52(db);
                AddForm53(db);
                AddForm54(db);
                AddForm55(db);
                AddForm56(db);
                AddForm57(db);
                AddForm59(db);
                AddForm61(db);
                AddForm63(db);
                AddForm64(db);
                AddForm65(db);
                AddForm65A(db);
                AddForm66(db);
                var form67 = AddForm67(db);
                var form68 = AddForm68(db);
                AddForm69(db);
                AddForm70(db);
                AddForm71(db);
                AddForm72(db);
                AddForm73(db);
                AddForm74(db);
                AddForm77(db);
                AddForm78(db);
                AddForm79(db);
                AddForm80(db);
                AddForm81(db);
                AddForm82(db);
                AddForm83(db);
                AddForm84(db);
                AddForm85(db);
                AddForm86(db);
                AddForm87(db);



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
                FormParameter P4(int row) => form4.Parameters.First(p => p.RowNumber == row);
                FamilyNtdValue FNtd(Family f, int row, string val) =>
                    new() { Family = f, FormParameter = P4(row), Value = val };

                var famR18MP = new Family { Name = "ОСМ Р1-8МП", ComponentType = typeResistor };
                famR18MP.NtdValues.AddRange(new[]
                {
            FNtd(famR18MP,  1, "Да"), FNtd(famR18MP,  2, "—"),
            FNtd(famR18MP,  3, "100000"), FNtd(famR18MP,  4, "25"),
            FNtd(famR18MP,  5, "25"),     FNtd(famR18MP,  6, "—"),
            FNtd(famR18MP,  7, "—"),      FNtd(famR18MP,  8, "—"),
            FNtd(famR18MP,  9, "10⁻⁶"),  FNtd(famR18MP,  10, "—"),
            FNtd(famR18MP,  11, "−60 (−60)"), FNtd(famR18MP, 12, "+85 (+85)"),
            FNtd(famR18MP, 13, "98"),     FNtd(famR18MP, 14, "+35"),
            FNtd(famR18MP, 15, "—"),      FNtd(famR18MP, 16, "—"),
        });

                var famK1079 = new Family { Name = "К10-79", ComponentType = typeCapacitor };
                famK1079.NtdValues.AddRange(new[]
                {
            FNtd(famK1079,  1, "Да"), FNtd(famK1079,  2, "—"),
            FNtd(famK1079,  3, "1"), FNtd(famK1079,  4, "2"),
            FNtd(famK1079,  5, "3"),     FNtd(famK1079,  6, "4"),
            FNtd(famK1079,  7, "5"),    FNtd(famK1079,  8, "6"),
            FNtd(famK1079,  9, "7"),  FNtd(famK1079,  10, "8"),
            FNtd(famK1079,  11, "9"), FNtd(famK1079, 12, "10"),
            FNtd(famK1079, 13, "11"),     FNtd(famK1079, 14, "12"),
            FNtd(famK1079, 15, "13"),      FNtd(famK1079, 16, "14"),
        });

                var famK5367 = new Family { Name = "К53-67", ComponentType = typeCapacitor };
                famK5367.NtdValues.AddRange(new[]
                {
            FNtd(famK5367,  1, "Да"), FNtd(famK5367,  2, "—"),
            FNtd(famK5367,  3, "150000"), FNtd(famK5367,  4, "25"),
            FNtd(famK5367,  5, "25"),     FNtd(famK5367,  6, "—"),
            FNtd(famK5367,  7, "—"),      FNtd(famK5367,  8, "—"),
            FNtd(famK5367,  9, "10⁻⁶"),  FNtd(famK5367,  10, "—"),
            FNtd(famK5367,  11, "−60 (−60)"), FNtd(famK5367, 12, "+125 (+85)"),
            FNtd(famK5367, 13, "98"),     FNtd(famK5367, 14, "+35"),
            FNtd(famK5367, 15, "—"),      FNtd(famK5367, 16, "7К1=2К, 7К4=2К"),
        });

                // ── 6. Тестовый компонент К10-79 с NTD для Формы 67 ──────────────────
                FormParameter P67(int row) => form67.Parameters.First(p => p.RowNumber == row);
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

                // Тестовый резистор для Формы 68
                FormParameter P68(int row) => form68.Parameters.First(p => p.RowNumber == row);
                ComponentNtdValue CNtd68(Component c, int row, string val) =>
                    new() { Component = c, FormParameter = P68(row), Value = val };

                var compR18MPtest = new Component
                {
                        FullName = "ОСМ Р1-8МП-0,1-10 кОм+-1 %-Л-А-М",
                        Family = famR18MP,
                        OwnForm = form68,
                        NeedsAdminReview = false,
                };

                compR18MPtest.NtdValues.AddRange(new[]
                {
    CNtd68(compR18MPtest,  1, "50"),
    CNtd68(compR18MPtest,  2, "—"),
    CNtd68(compR18MPtest,  3, "—"),
    CNtd68(compR18MPtest,  4, "50"),
    CNtd68(compR18MPtest,  5, "—"),
    CNtd68(compR18MPtest,  6, "—"),
    CNtd68(compR18MPtest,  7, "—"),
    CNtd68(compR18MPtest,  8, "—"),
    CNtd68(compR18MPtest,  9, "—"),
    CNtd68(compR18MPtest, 10, "—"),
    CNtd68(compR18MPtest, 11, "-"),
    CNtd68(compR18MPtest, 12, "—"),
    CNtd68(compR18MPtest, 13, "0,125"),
    CNtd68(compR18MPtest, 14, "125"),
    CNtd68(compR18MPtest, 15, "0,5"),
});

                famR18MP.Components.Add(compR18MPtest);

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
        private static Form AddForm4(CatalogDbContext db)
        {
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
            new() { Form = form4, Section = sec4Rel, RowNumber =  1, Order =  1, Name = "Наличие в перечнях при утверждении ТТЗ",                                Unit = "ч"           },
            new() { Form = form4, Section = sec4Rel, RowNumber =  2, Order =  2, Name = "Наличие в перечнях последних редакций",                                Unit = "ч"           },
            new() { Form = form4, Section = sec4Rel, RowNumber =  3, Order =  3, Name = "Показатель ресурса",                                Unit = "ч"           },
            new() { Form = form4, Section = sec4Rel, RowNumber =  4, Order =  4, Name = "Показатель срока службы",                          Unit = "лет"         },
            new() { Form = form4, Section = sec4Rel, RowNumber =  5, Order =  5, Name = "Показатель сохраняемости",                         Unit = "лет"         },
            new() { Form = form4, Section = sec4Op,  RowNumber =  6, Order =  1, Name = "Акустический шум: диапазон частот",                Unit = "Гц"          },
            new() { Form = form4, Section = sec4Op,  RowNumber =  7, Order =  2, Name = "Акустический шум: уровень звукового давления",     Unit = "дБ"          },
            new() { Form = form4, Section = sec4Op,  RowNumber =  8, Order =  3, Name = "Линейное ускорение",                               Unit = "М.С.Е-2.(G)" },
            new() { Form = form4, Section = sec4Op,  RowNumber =  9, Order =  4, Name = "Давление окружающей среды: пониженное",            Unit = "мм рт. ст."  },
            new() { Form = form4, Section = sec4Op,  RowNumber =  10, Order =  5, Name = "Давление окружающей среды: повышенное",            Unit = "атм"         },
            new() { Form = form4, Section = sec4Op,  RowNumber =  11, Order =  6, Name = "Предельная (рабочая) температура: пониженная",     Unit = "°С"          },
            new() { Form = form4, Section = sec4Op,  RowNumber = 12, Order =  7, Name = "Предельная (рабочая) температура: повышенная",     Unit = "°С"          },
            new() { Form = form4, Section = sec4Op,  RowNumber = 13, Order =  8, Name = "Относительная влажность",                         Unit = "%"           },
            new() { Form = form4, Section = sec4Op,  RowNumber = 14, Order =  9, Name = "Относительная влажность: температура",            Unit = "°С"          },
            new() { Form = form4, Section = sec4Op,  RowNumber = 15, Order = 10, Name = "Роса, иней",                                      Unit = null          },
            new() { Form = form4, Section = sec4Op,  RowNumber = 16, Order = 11, Name = "Стойкость к воздействию специальных факторов, 7К", Unit = null          },
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
                return form4;
        }
        private static Form AddForm6(CatalogDbContext db)
        {
                var form6 = new Form
                {
                        Number = "6",
                        Title = "Карта рабочих режимов магнетронов импульсного и непрерывного действия",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup // или PerColumn, если нужно
                };

                // Секции (логическая группировка параметров)
                var sec6Filament = new FormSection { Form = form6, Title = "Накал", Order = 1 };
                var sec6Timing = new FormSection { Form = form6, Title = "Временные параметры запуска", Order = 2 };
                var sec6Anode = new FormSection { Form = form6, Title = "Анодные параметры (постоянные/импульсные)", Order = 3 };
                var sec6Pulse = new FormSection { Form = form6, Title = "Параметры импульсного магнетрона", Order = 4 };
                var sec6Env = new FormSection { Form = form6, Title = "Условия эксплуатации", Order = 5 };

                form6.Sections.AddRange(new[] { sec6Filament, sec6Timing, sec6Anode, sec6Pulse, sec6Env });

                var param6 = new List<FormParameter>
{
    // ---- Накал (Filament)
    new() { Form = form6, Section = sec6Filament, RowNumber = 1,
            Order = 1, Name = "Напряжение в режиме запуска (макс/мин)", Unit = "В", IsRequired = false },
    new() { Form = form6, Section = sec6Filament, RowNumber = 2,
            Order = 2, Name = "Напряжение в рабочем режиме (макс/мин)", Unit = "В", IsRequired = false },
    new() { Form = form6, Section = sec6Filament, RowNumber = 3,
            Order = 3, Name = "Бросок тока", Unit = "%", IsRequired = false },

    // ---- Временные параметры запуска
    new() { Form = form6, Section = sec6Timing, RowNumber = 4,
            Order = 1, Name = "Время задержки включения высокого напряжения", Unit = "мин", IsRequired = false },

    // ---- Анодные параметры (постоянные/импульсные)
    new() { Form = form6, Section = sec6Anode, RowNumber = 5,
            Order = 1, Name = "Напряжение постоянное (импульсное) макс/мин", Unit = "кВ", IsRequired = false },
    new() { Form = form6, Section = sec6Anode, RowNumber = 6,
            Order = 2, Name = "Пульсация напряжения", Unit = "% (В)", IsRequired = false },
    new() { Form = form6, Section = sec6Anode, RowNumber = 7,
            Order = 3, Name = "Ток средний (импульсный) макс/мин", Unit = "мА (А)", IsRequired = false },
    new() { Form = form6, Section = sec6Anode, RowNumber = 8,
            Order = 4, Name = "Длительность фронта", Unit = "нс", IsRequired = false },
    new() { Form = form6, Section = sec6Anode, RowNumber = 9,
            Order = 5, Name = "Выброс на вершине", Unit = "%", IsRequired = false },
    new() { Form = form6, Section = sec6Anode, RowNumber = 10,
            Order = 6, Name = "Скос плоской части", Unit = "%", IsRequired = false },
    new() { Form = form6, Section = sec6Anode, RowNumber = 11,
            Order = 7, Name = "Колебания на плоской части", Unit = "%", IsRequired = false },
    new() { Form = form6, Section = sec6Anode, RowNumber = 12,
            Order = 8, Name = "Отрицательный выброс напряжения после импульса", Unit = "%", IsRequired = false },
    new() { Form = form6, Section = sec6Anode, RowNumber = 13,
            Order = 9, Name = "Длительность спада", Unit = "нс", IsRequired = false },
    new() { Form = form6, Section = sec6Anode, RowNumber = 14,
            Order = 10, Name = "Длительность импульса", Unit = "мкс", IsRequired = false },

    // ---- Параметры импульсного магнетрона
    new() { Form = form6, Section = sec6Pulse, RowNumber = 15,
            Order = 1, Name = "Скважность средняя", Unit = "", IsRequired = false },
    new() { Form = form6, Section = sec6Pulse, RowNumber = 16,
            Order = 2, Name = "Скважность в пакете", Unit = "", IsRequired = false },
    new() { Form = form6, Section = sec6Pulse, RowNumber = 17,
            Order = 3, Name = "Период следования пакетов", Unit = "мс (мкс)", IsRequired = false },
    new() { Form = form6, Section = sec6Pulse, RowNumber = 18,
            Order = 4, Name = "КСВН нагрузки", Unit = "", IsRequired = false },

    // ---- Условия эксплуатации
    new() { Form = form6, Section = sec6Env, RowNumber = 19,
            Order = 1, Name = "Напряженность магнитного поля", Unit = "Э", IsRequired = false },
    new() { Form = form6, Section = sec6Env, RowNumber = 20,
            Order = 2, Name = "Напряженность стороннего магнитного поля", Unit = "Э", IsRequired = false },
    new() { Form = form6, Section = sec6Env, RowNumber = 21,
            Order = 3, Name = "Расстояние до ферромагнитных материалов", Unit = "мм", IsRequired = false },
    new() { Form = form6, Section = sec6Env, RowNumber = 22,
            Order = 4, Name = "Напряжение электроразрядного насоса", Unit = "кВ", IsRequired = false },
    new() { Form = form6, Section = sec6Env, RowNumber = 23,
            Order = 5, Name = "Температура анодного блока (корпуса)", Unit = "°С", IsRequired = false },
    new() { Form = form6, Section = sec6Env, RowNumber = 24,
            Order = 6, Name = "Температура охлаждающей жидкости на входе", Unit = "°С", IsRequired = false },
    new() { Form = form6, Section = sec6Env, RowNumber = 25,
            Order = 7, Name = "Расход охлаждающей жидкости", Unit = "л/мин", IsRequired = false },
    new() { Form = form6, Section = sec6Env, RowNumber = 26,
            Order = 8, Name = "Температура окружающей среды", Unit = "°С", IsRequired = false },
    new() { Form = form6, Section = sec6Env, RowNumber = 27,
            Order = 9, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
};

                form6.Parameters.AddRange(param6);

                // Добавляем два столбца для значений (аналог "В схеме" / "По НТД")
                // На картинке - два столбца: скорее всего, "мин" и "макс" или "по ТУ" и "реальное значение"
                form6.ValueColumns.AddRange(new[]
                {
    new FormValueColumn { Form = form6, Key = "rated", Title = "Допустимые значения (по НТД)", Source = ColumnSource.DatasheetNdt, Order = 1 },
    new FormValueColumn { Form = form6, Key = "actual", Title = "Фактические значения (измеренные)", Source = ColumnSource.ManualScheme, Order = 2 },
    });

                db.Forms.Add(form6);
                return form6;
        }
        private static Form AddForm7(CatalogDbContext db)
        {
                // ---- Форма 7: Карта рабочих режимов ламп обратной волны
                var form7 = new Form
                {
                        Number = "7",
                        Title = "Карта рабочих режимов ламп обратной волны",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var sec7Filament = new FormSection { Form = form7, Title = "Параметры накала", Order = 1 };
                var sec7Anode = new FormSection { Form = form7, Title = "Анодные параметры", Order = 2 };
                var sec7InputPulse = new FormSection { Form = form7, Title = "Параметры входного СВЧ импульса", Order = 3 };
                var sec7ModulatingPulse = new FormSection { Form = form7, Title = "Параметры импульса моделирующего напряжения", Order = 4 };
                var sec7SWR = new FormSection { Form = form7, Title = "КСВН", Order = 5 };
                var sec7Env = new FormSection { Form = form7, Title = "Условия эксплуатации", Order = 6 };

                form7.Sections.AddRange(new[] { sec7Filament, sec7Anode, sec7InputPulse, sec7ModulatingPulse, sec7SWR, sec7Env });

                var param7 = new List<FormParameter>
    {
        // ---- Параметры накала
        new() { Form = form7, Section = sec7Filament, RowNumber = 1,
                Order = 1, Name = "Напряжение накала", Unit = "В", IsRequired = false },
        new() { Form = form7, Section = sec7Filament, RowNumber = 2,
                Order = 2, Name = "Бросок тока накала", Unit = "А", IsRequired = false },
        new() { Form = form7, Section = sec7Filament, RowNumber = 3,
                Order = 3, Name = "Время задержки включения высокого напряжения", Unit = "мин", IsRequired = false },

        // ---- Анодные параметры
        new() { Form = form7, Section = sec7Anode, RowNumber = 4,
                Order = 1, Name = "Напряжение на аноде (постоянное/импульсное) мин/макс", Unit = "кВ", IsRequired = false },
        new() { Form = form7, Section = sec7Anode, RowNumber = 5,
                Order = 2, Name = "Пульсация напряжения на аноде", Unit = "%", IsRequired = false },
        new() { Form = form7, Section = sec7Anode, RowNumber = 6,
                Order = 3, Name = "Ток анода средний (импульсный) мин/макс", Unit = "мА (А)", IsRequired = false },

        // ---- Параметры входного СВЧ импульса
        new() { Form = form7, Section = sec7InputPulse, RowNumber = 7,
                Order = 1, Name = "Длительность входного СВЧ импульса", Unit = "мкс", IsRequired = false },
        new() { Form = form7, Section = sec7InputPulse, RowNumber = 8,
                Order = 2, Name = "Скважность (средняя)", Unit = "", IsRequired = false },
        new() { Form = form7, Section = sec7InputPulse, RowNumber = 9,
                Order = 3, Name = "Скважность в пакете", Unit = "", IsRequired = false },
        new() { Form = form7, Section = sec7InputPulse, RowNumber = 10,
                Order = 4, Name = "Период следования пакетов", Unit = "мкс/с", IsRequired = false },

        // ---- Параметры импульса моделирующего напряжения
        new() { Form = form7, Section = sec7ModulatingPulse, RowNumber = 11,
                Order = 1, Name = "Длительность фронта", Unit = "мкс", IsRequired = false },
        new() { Form = form7, Section = sec7ModulatingPulse, RowNumber = 12,
                Order = 2, Name = "Скос плоской части", Unit = "%", IsRequired = false },
        new() { Form = form7, Section = sec7ModulatingPulse, RowNumber = 13,
                Order = 3, Name = "Колебания на плоской части", Unit = "%", IsRequired = false },
        new() { Form = form7, Section = sec7ModulatingPulse, RowNumber = 14,
                Order = 4, Name = "Положительный выброс напряжения", Unit = "%", IsRequired = false },
        new() { Form = form7, Section = sec7ModulatingPulse, RowNumber = 15,
                Order = 5, Name = "Отрицательный выброс напряжения", Unit = "%", IsRequired = false },
        new() { Form = form7, Section = sec7ModulatingPulse, RowNumber = 16,
                Order = 6, Name = "Длительность спада", Unit = "мкс", IsRequired = false },
        new() { Form = form7, Section = sec7ModulatingPulse, RowNumber = 17,
                Order = 7, Name = "Длительность импульса", Unit = "мкс", IsRequired = false },
        new() { Form = form7, Section = sec7ModulatingPulse, RowNumber = 18,
                Order = 8, Name = "Период следования", Unit = "мкс", IsRequired = false },
        new() { Form = form7, Section = sec7ModulatingPulse, RowNumber = 19,
                Order = 9, Name = "Входная мощность (импульсная)", Unit = "Вт (мВт)", IsRequired = false },

        // ---- КСВН
        new() { Form = form7, Section = sec7SWR, RowNumber = 20,
                Order = 1, Name = "КСВН на входе (в рабочей полосе)", Unit = "", IsRequired = false },
        new() { Form = form7, Section = sec7SWR, RowNumber = 21,
                Order = 2, Name = "КСВН на входе (за рабочей полосой)", Unit = "", IsRequired = false },
        new() { Form = form7, Section = sec7SWR, RowNumber = 22,
                Order = 3, Name = "КСВН на выходе (в рабочей полосе)", Unit = "", IsRequired = false },
        new() { Form = form7, Section = sec7SWR, RowNumber = 23,
                Order = 4, Name = "КСВН на выходе (за рабочей полосой)", Unit = "", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form7, Section = sec7Env, RowNumber = 24,
                Order = 1, Name = "Напряженность магнитного поля", Unit = "Э", IsRequired = false },
        new() { Form = form7, Section = sec7Env, RowNumber = 25,
                Order = 2, Name = "Напряженность стороннего магнитного поля", Unit = "Э", IsRequired = false },
        new() { Form = form7, Section = sec7Env, RowNumber = 26,
                Order = 3, Name = "Расстояние до ферромагнитных материалов", Unit = "мм", IsRequired = false },
        new() { Form = form7, Section = sec7Env, RowNumber = 27,
                Order = 4, Name = "Напряжение электроразрядного насоса", Unit = "кВ", IsRequired = false },
        new() { Form = form7, Section = sec7Env, RowNumber = 28,
                Order = 5, Name = "Температура охлаждающей жидкости на входе", Unit = "°С", IsRequired = false },
        new() { Form = form7, Section = sec7Env, RowNumber = 29,
                Order = 6, Name = "Расход охлаждающей жидкости", Unit = "л/мин", IsRequired = false },
        new() { Form = form7, Section = sec7Env, RowNumber = 30,
                Order = 7, Name = "Температура окружающей среды (корпуса)", Unit = "°С", IsRequired = false },
        new() { Form = form7, Section = sec7Env, RowNumber = 31,
                Order = 8, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form7.Parameters.AddRange(param7);

                // Добавляем два столбца: "в схеме" и "по НТД" (как на картинке)
                form7.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form7, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form7, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form7);
                return form7;
        }
        private static Form AddForm8(CatalogDbContext db)
        {
                // ---- Форма 8: Карта рабочих режимов ламп обратной волны
                var form8 = new Form
                {
                        Number = "8",
                        Title = "Карта рабочих режимов ламп обратной волны",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var sec8Voltage = new FormSection { Form = form8, Title = "Напряжение", Order = 1 };
                var sec8Stability = new FormSection { Form = form8, Title = "Стабильность напряжения", Order = 2 };
                var sec8Ripple = new FormSection { Form = form8, Title = "Пульсация напряжения", Order = 3 };
                var sec8Other = new FormSection { Form = form8, Title = "Прочие параметры", Order = 4 };

                form8.Sections.AddRange(new[] { sec8Voltage, sec8Stability, sec8Ripple, sec8Other });

                var param8 = new List<FormParameter>
    {
        // ---- Напряжение
        new() { Form = form8, Section = sec8Voltage, RowNumber = 1,
                Order = 1, Name = "Напряжение накала (мин)", Unit = "В", IsRequired = false },
        new() { Form = form8, Section = sec8Voltage, RowNumber = 2,
                Order = 2, Name = "Напряжение накала (макс)", Unit = "В", IsRequired = false },
        new() { Form = form8, Section = sec8Voltage, RowNumber = 3,
                Order = 3, Name = "Напряжение анода (первого) (мин)", Unit = "В", IsRequired = false },
        new() { Form = form8, Section = sec8Voltage, RowNumber = 4,
                Order = 4, Name = "Напряжение анода (первого) (макс)", Unit = "В", IsRequired = false },
        new() { Form = form8, Section = sec8Voltage, RowNumber = 5,
                Order = 5, Name = "Напряжение между анодом и отрицательным электродом", Unit = "В", IsRequired = false },
        new() { Form = form8, Section = sec8Voltage, RowNumber = 6,
                Order = 6, Name = "Напряжение управляющего (фокусирующего) электрода", Unit = "В", IsRequired = false },
        new() { Form = form8, Section = sec8Voltage, RowNumber = 7,
                Order = 7, Name = "Напряжение отрицательного электрода (замедляющей системы)", Unit = "В", IsRequired = false },

        // ---- Стабильность напряжения
        new() { Form = form8, Section = sec8Stability, RowNumber = 8,
                Order = 1, Name = "Стабильность напряжения анода", Unit = "%", IsRequired = false },
        new() { Form = form8, Section = sec8Stability, RowNumber = 9,
                Order = 2, Name = "Стабильность напряжения управляющего электрода", Unit = "%", IsRequired = false },

        // ---- Пульсация напряжения
        new() { Form = form8, Section = sec8Ripple, RowNumber = 10,
                Order = 1, Name = "Пульсация напряжения анода", Unit = "%", IsRequired = false },
        new() { Form = form8, Section = sec8Ripple, RowNumber = 11,
                Order = 2, Name = "Пульсация напряжения управляющего электрода", Unit = "%", IsRequired = false },

        // ---- Прочие параметры
        new() { Form = form8, Section = sec8Other, RowNumber = 12,
                Order = 1, Name = "Бросок тока накала при включении", Unit = "А", IsRequired = false },
        new() { Form = form8, Section = sec8Other, RowNumber = 13,
                Order = 2, Name = "Время задержки включения высокого напряжения", Unit = "мин", IsRequired = false },
        new() { Form = form8, Section = sec8Other, RowNumber = 14,
                Order = 3, Name = "КСВН нагрузки", Unit = "", IsRequired = false },
        new() { Form = form8, Section = sec8Other, RowNumber = 15,
                Order = 4, Name = "Напряженность магнитного поля", Unit = "Э", IsRequired = false },
        new() { Form = form8, Section = sec8Other, RowNumber = 16,
                Order = 5, Name = "Напряженность стороннего магнитного поля", Unit = "Э", IsRequired = false },
        new() { Form = form8, Section = sec8Other, RowNumber = 17,
                Order = 6, Name = "Расстояние до ферромагнитных материалов", Unit = "мм", IsRequired = false },
        new() { Form = form8, Section = sec8Other, RowNumber = 18,
                Order = 7, Name = "Расход охлаждающей жидкости", Unit = "л/мин", IsRequired = false },
        new() { Form = form8, Section = sec8Other, RowNumber = 19,
                Order = 8, Name = "Температура окружающей среды (корпуса)", Unit = "°С", IsRequired = false },
        new() { Form = form8, Section = sec8Other, RowNumber = 20,
                Order = 9, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form8.Parameters.AddRange(param8);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form8.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form8, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form8, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form8);
                return form8;
        }
        private static Form AddForm9(CatalogDbContext db) // Или AddFormReflexKlystron, если хотите другое название
        {
                // ---- Форма: Карта рабочих режимов отражательных клистров
                var form = new Form
                {
                        Number = "9", // Укажите нужный номер формы (возможно, это отдельная форма)
                        Title = "Карта рабочих режимов отражательных клистров",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secFilament = new FormSection { Form = form, Title = "Накал", Order = 1 };
                var secTiming = new FormSection { Form = form, Title = "Временные параметры", Order = 2 };
                var secHeater = new FormSection { Form = form, Title = "Катод-подогреватель", Order = 3 };
                var secResonator = new FormSection { Form = form, Title = "Резонатор", Order = 4 };
                var secOther = new FormSection { Form = form, Title = "Прочие параметры", Order = 5 };

                form.Sections.AddRange(new[] { secFilament, secTiming, secHeater, secResonator, secOther });

                var parameters = new List<FormParameter>
    {
        // ---- Накал
        new() { Form = form, Section = secFilament, RowNumber = 1,
                Order = 1, Name = "Напряжение накала (мин)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secFilament, RowNumber = 2,
                Order = 2, Name = "Напряжение накала (макс)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secFilament, RowNumber = 3,
                Order = 3, Name = "Бросок тока накала", Unit = "А", IsRequired = false },

        // ---- Временные параметры
        new() { Form = form, Section = secTiming, RowNumber = 4,
                Order = 1, Name = "Время задержки включения высокого напряжения", Unit = "мин", IsRequired = false },

        // ---- Катод-подогреватель
        new() { Form = form, Section = secHeater, RowNumber = 5,
                Order = 1, Name = "Напряжение между катодом и подогревателем", Unit = "В", IsRequired = false },

        // ---- Резонатор
        new() { Form = form, Section = secResonator, RowNumber = 6,
                Order = 1, Name = "Напряжение резонатора (мин)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secResonator, RowNumber = 7,
                Order = 2, Name = "Напряжение резонатора (макс)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secResonator, RowNumber = 8,
                Order = 3, Name = "Пульсация напряжения резонатора", Unit = "%", IsRequired = false },
        new() { Form = form, Section = secResonator, RowNumber = 9,
                Order = 4, Name = "Сопротивление в цепи отражателя", Unit = "МОм", IsRequired = false },

        // ---- Прочие параметры
        new() { Form = form, Section = secOther, RowNumber = 10,
                Order = 1, Name = "Температура корпуса", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secOther, RowNumber = 11,
                Order = 2, Name = "КСВН нагрузки", Unit = "", IsRequired = false },
        new() { Form = form, Section = secOther, RowNumber = 12,
                Order = 3, Name = "Примечание", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return form;
        }
        private static Form AddForm13(CatalogDbContext db)
        {
                // ---- Форма 13: Карта рабочих режимов защитных устройств СВЧ
                var form = new Form
                {
                        Number = "13",
                        Title = "Карта рабочих режимов защитных устройств СВЧ",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secFrequency = new FormSection { Form = form, Title = "Частотный диапазон", Order = 1 };
                var secPower = new FormSection { Form = form, Title = "Падающая мощность", Order = 2 };
                var secPulse = new FormSection { Form = form, Title = "Импульсные параметры", Order = 3 };
                var secControlDC = new FormSection { Form = form, Title = "Управление от источника постоянного напряжения", Order = 4 };
                var secControlPulse = new FormSection { Form = form, Title = "Управление от источника импульсного напряжения", Order = 5 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 6 };

                form.Sections.AddRange(new[] { secFrequency, secPower, secPulse, secControlDC, secControlPulse, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Частотный диапазон
        new() { Form = form, Section = secFrequency, RowNumber = 1,
                Order = 1, Name = "Соответствие диапазону частот", Unit = "(да, нет)", IsRequired = false },

        // ---- Падающая мощность
        new() { Form = form, Section = secPower, RowNumber = 2,
                Order = 1, Name = "Падающая мощность импульсная (мин)", Unit = "кВт", IsRequired = false },
        new() { Form = form, Section = secPower, RowNumber = 3,
                Order = 2, Name = "Падающая мощность импульсная (макс)", Unit = "кВт", IsRequired = false },

        // ---- Импульсные параметры
        new() { Form = form, Section = secPulse, RowNumber = 4,
                Order = 1, Name = "Скважность", Unit = "", IsRequired = false },
        new() { Form = form, Section = secPulse, RowNumber = 5,
                Order = 2, Name = "Длительность импульса передатчика (мин)", Unit = "мкс", IsRequired = false },
        new() { Form = form, Section = secPulse, RowNumber = 6,
                Order = 3, Name = "Длительность импульса передатчика (макс)", Unit = "мкс", IsRequired = false },

        // ---- Управление от источника постоянного напряжения
        new() { Form = form, Section = secControlDC, RowNumber = 7,
                Order = 1, Name = "Напряжение управления (постоянное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secControlDC, RowNumber = 8,
                Order = 2, Name = "Ток управления", Unit = "мкА", IsRequired = false },

        // ---- Управление от источника импульсного напряжения
        new() { Form = form, Section = secControlPulse, RowNumber = 9,
                Order = 1, Name = "Амплитуда импульса управления", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secControlPulse, RowNumber = 10,
                Order = 2, Name = "Длительность импульса управления", Unit = "мкс", IsRequired = false },
        new() { Form = form, Section = secControlPulse, RowNumber = 11,
                Order = 3, Name = "Опережение управляющего импульса относительно импульса передатчика", Unit = "мкс", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 12,
                Order = 1, Name = "Температура окружающей среды (корпуса)", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 13,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return form;
        }
        private static Form AddForm14(CatalogDbContext db)
        {
                // ---- Форма 14: Карта рабочих режимов защитных устройств СВЧ
                var form = new Form
                {
                        Number = "14",
                        Title = "Карта рабочих режимов защитных устройств СВЧ",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secPulse = new FormSection { Form = form, Title = "Параметры импульса", Order = 1 };
                var secVoltage = new FormSection { Form = form, Title = "Напряжение", Order = 2 };
                var secCurrent = new FormSection { Form = form, Title = "Ток", Order = 3 };
                var secPower = new FormSection { Form = form, Title = "Мощность", Order = 4 };
                var secOther = new FormSection { Form = form, Title = "Прочие параметры", Order = 5 };

                form.Sections.AddRange(new[] { secPulse, secVoltage, secCurrent, secPower, secOther });

                var parameters = new List<FormParameter>
    {
        // ---- Параметры импульса
        new() { Form = form, Section = secPulse, RowNumber = 1,
                Order = 1, Name = "Длительность импульса", Unit = "мкс", IsRequired = false },
        new() { Form = form, Section = secPulse, RowNumber = 2,
                Order = 2, Name = "Частота следования", Unit = "Гц", IsRequired = false },

        // ---- Напряжение
        new() { Form = form, Section = secVoltage, RowNumber = 3,
                Order = 1, Name = "Постоянное обратное напряжение", Unit = "В", IsRequired = false },

        // ---- Ток
        new() { Form = form, Section = secCurrent, RowNumber = 4,
                Order = 1, Name = "Ток прямого положительного смещения", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secCurrent, RowNumber = 5,
                Order = 2, Name = "Ток постоянный обратный", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secCurrent, RowNumber = 6,
                Order = 3, Name = "Ток выпрямленный", Unit = "А", IsRequired = false },

        // ---- Мощность
        new() { Form = form, Section = secPower, RowNumber = 7,
                Order = 1, Name = "Мощность рассеиваемая (непрерывная/импульсная)", Unit = "Вт", IsRequired = false },
        new() { Form = form, Section = secPower, RowNumber = 8,
                Order = 2, Name = "Мощность коммутируемая (непрерывная/импульсная)", Unit = "Вт", IsRequired = false },

        // ---- Прочие параметры
        new() { Form = form, Section = secOther, RowNumber = 9,
                Order = 1, Name = "Энергия пика просачивающегося импульса", Unit = "Дж", IsRequired = false },
        new() { Form = form, Section = secOther, RowNumber = 10,
                Order = 2, Name = "Соответствие диапазону частот", Unit = "(да, нет)", IsRequired = false },
        new() { Form = form, Section = secOther, RowNumber = 11,
                Order = 3, Name = "Температура окружающей среды", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secOther, RowNumber = 12,
                Order = 4, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return form;
        }
        private static Form AddForm15(CatalogDbContext db)
        {
                // ---- Форма 15: Карта рабочих режимов ВЧ и СВЧ транзисторов
                var form = new Form
                {
                        Number = "15",
                        Title = "Карта рабочих режимов ВЧ и СВЧ транзисторов",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secStatic = new FormSection { Form = form, Title = "Статический режим", Order = 1 };
                var secDynamic = new FormSection { Form = form, Title = "Динамический режим", Order = 2 };

                form.Sections.AddRange(new[] { secStatic, secDynamic });

                var parameters = new List<FormParameter>
    {
        // ---- Статический режим
        new() { Form = form, Section = secStatic, RowNumber = 1,
                Order = 1, Name = "Постоянное напряжение коллектор-эмиттер (коллектор-база)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secStatic, RowNumber = 2,
                Order = 2, Name = "Постоянное напряжение эмиттер-база", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secStatic, RowNumber = 3,
                Order = 3, Name = "Постоянный ток коллектора", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secStatic, RowNumber = 4,
                Order = 4, Name = "Постоянная рассеиваемая мощность", Unit = "Вт", IsRequired = false },
        new() { Form = form, Section = secStatic, RowNumber = 5,
                Order = 5, Name = "Сопротивление в цепи базы", Unit = "Ом", IsRequired = false },
        new() { Form = form, Section = secStatic, RowNumber = 6,
                Order = 6, Name = "Температура окружающей среды (корпуса)", Unit = "°С", IsRequired = false },

        // ---- Динамический режим
        new() { Form = form, Section = secDynamic, RowNumber = 7,
                Order = 1, Name = "Нижняя рабочая частота выходного сигнала", Unit = "МГц", IsRequired = false },
        new() { Form = form, Section = secDynamic, RowNumber = 8,
                Order = 2, Name = "Пиковое значение напряжения коллектор-эмиттер (коллектор-база)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secDynamic, RowNumber = 9,
                Order = 3, Name = "Постоянная составляющая коллекторного тока", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secDynamic, RowNumber = 10,
                Order = 4, Name = "Входная ВЧ (СВЧ) мощность", Unit = "Вт", IsRequired = false },
        new() { Form = form, Section = secDynamic, RowNumber = 11,
                Order = 5, Name = "Рассеиваемая мощность", Unit = "Вт", IsRequired = false },
        new() { Form = form, Section = secDynamic, RowNumber = 12,
                Order = 6, Name = "Температура окружающей среды (корпуса)", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secDynamic, RowNumber = 13,
                Order = 7, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return form;
        }
        private static Form AddForm16(CatalogDbContext db)
        {
                // ---- Форма 16: Карта рабочих режимов полупроводниковых параметрических усилителей и усилителей на туннельных диодах
                var form = new Form
                {
                        Number = "16",
                        Title = "Карта рабочих режимов полупроводниковых параметрических усилителей и усилителей на туннельных диодах",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secBias = new FormSection { Form = form, Title = "Напряжение смещения", Order = 1 };
                var secInput = new FormSection { Form = form, Title = "Вход усилителя", Order = 2 };
                var secOutput = new FormSection { Form = form, Title = "Выход усилителя", Order = 3 };
                var secThermostat = new FormSection { Form = form, Title = "Термостат", Order = 4 };
                var secPump = new FormSection { Form = form, Title = "Питание генератора накачки", Order = 5 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 6 };

                form.Sections.AddRange(new[] { secBias, secInput, secOutput, secThermostat, secPump, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Напряжение смещения
        new() { Form = form, Section = secBias, RowNumber = 1,
                Order = 1, Name = "Напряжение смещения (минимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secBias, RowNumber = 2,
                Order = 2, Name = "Напряжение смещения (максимальное)", Unit = "В", IsRequired = false },

        // ---- Вход усилителя
        new() { Form = form, Section = secInput, RowNumber = 3,
                Order = 1, Name = "Уровень просачивающейся мощности на входе", Unit = "мкВт", IsRequired = false },
        new() { Form = form, Section = secInput, RowNumber = 4,
                Order = 2, Name = "КСВН на входе усилителя", Unit = "", IsRequired = false },

        // ---- Выход усилителя
        new() { Form = form, Section = secOutput, RowNumber = 5,
                Order = 1, Name = "КСВН на выходе усилителя", Unit = "", IsRequired = false },

        // ---- Термостат
        new() { Form = form, Section = secThermostat, RowNumber = 6,
                Order = 1, Name = "Напряжение питания термостата", Unit = "В", IsRequired = false },

        // ---- Питание генератора накачки
        new() { Form = form, Section = secPump, RowNumber = 7,
                Order = 1, Name = "Напряжение питания генератора накачки", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secPump, RowNumber = 8,
                Order = 2, Name = "Пульсация напряжения накачки", Unit = "%", IsRequired = false },
        new() { Form = form, Section = secPump, RowNumber = 9,
                Order = 3, Name = "Стабильность напряжения накачки", Unit = "%", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 10,
                Order = 1, Name = "Температура окружающей среды (корпуса)", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 11,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return form;
        }
        private static Form AddForm17(CatalogDbContext db)
        {
                // ---- Форма 17: Карта рабочих режимов генераторов и усилителей на диодах Ганна
                var form = new Form
                {
                        Number = "17",
                        Title = "Карта рабочих режимов генераторов и усилителей на диодах Ганна",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secSupply = new FormSection { Form = form, Title = "Напряжение питания", Order = 1 };
                var secCurrent = new FormSection { Form = form, Title = "Ток", Order = 2 };
                var secPower = new FormSection { Form = form, Title = "Мощность", Order = 3 };
                var secSWR = new FormSection { Form = form, Title = "КСВН", Order = 4 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 5 };

                form.Sections.AddRange(new[] { secSupply, secCurrent, secPower, secSWR, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Напряжение питания
        new() { Form = form, Section = secSupply, RowNumber = 1,
                Order = 1, Name = "Напряжение питания (минимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secSupply, RowNumber = 2,
                Order = 2, Name = "Напряжение питания (максимальное)", Unit = "В", IsRequired = false },

        // ---- Ток
        new() { Form = form, Section = secCurrent, RowNumber = 3,
                Order = 1, Name = "Рабочий ток", Unit = "мА", IsRequired = false },

        // ---- Мощность
        new() { Form = form, Section = secPower, RowNumber = 4,
                Order = 1, Name = "Мощность, потребляемая диодом", Unit = "Вт", IsRequired = false },

        // ---- КСВН
        new() { Form = form, Section = secSWR, RowNumber = 5,
                Order = 1, Name = "Максимальный КСВН нагрузки", Unit = "", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 6,
                Order = 1, Name = "Температура окружающей среды (корпуса)", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 7,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm19(CatalogDbContext db)
        {
                // ---- Форма 19: Карта рабочих режимов приемных и передающих СВЧ модулей
                var form = new Form
                {
                        Number = "19",
                        Title = "Карта рабочих режимов приемных и передающих СВЧ модулей",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secInput = new FormSection { Form = form, Title = "Входная мощность", Order = 1 };
                var secSWR = new FormSection { Form = form, Title = "КСВН", Order = 2 };
                var secSupply = new FormSection { Form = form, Title = "Напряжение питания", Order = 3 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 4 };

                form.Sections.AddRange(new[] { secInput, secSWR, secSupply, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Входная мощность
        new() { Form = form, Section = secInput, RowNumber = 1,
                Order = 1, Name = "Минимальная входная мощность", Unit = "Вт", IsRequired = false },
        new() { Form = form, Section = secInput, RowNumber = 2,
                Order = 2, Name = "Максимальная входная мощность", Unit = "Вт", IsRequired = false },

        // ---- КСВН
        new() { Form = form, Section = secSWR, RowNumber = 3,
                Order = 1, Name = "КСВН нагрузки", Unit = "", IsRequired = false },

        // ---- Напряжение питания
        new() { Form = form, Section = secSupply, RowNumber = 4,
                Order = 1, Name = "Напряжение питания (минимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secSupply, RowNumber = 5,
                Order = 2, Name = "Напряжение питания (максимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secSupply, RowNumber = 6,
                Order = 3, Name = "Пульсация напряжения питания", Unit = "%", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 7,
                Order = 1, Name = "Температура окружающей среды (корпуса)", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 8,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm20(CatalogDbContext db)
        {
                // ---- Форма 20: Карта рабочих режимов полупроводниковых фазовращателей, переключателей, аттенюаторов и модуляторов
                var form = new Form
                {
                        Number = "20",
                        Title = "Карта рабочих режимов полупроводниковых фазовращателей, переключателей, аттенюаторов и модуляторов",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secFrequency = new FormSection { Form = form, Title = "Частотный диапазон", Order = 1 };
                var secInputPower = new FormSection { Form = form, Title = "Входная мощность", Order = 2 };
                var secControl = new FormSection { Form = form, Title = "Управление", Order = 3 };
                var secPulse = new FormSection { Form = form, Title = "Импульсные параметры", Order = 4 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 5 };

                form.Sections.AddRange(new[] { secFrequency, secInputPower, secControl, secPulse, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Частотный диапазон
        new() { Form = form, Section = secFrequency, RowNumber = 1,
                Order = 1, Name = "Соответствие диапазону частот", Unit = "(да, нет)", IsRequired = false },

        // ---- Входная мощность
        new() { Form = form, Section = secInputPower, RowNumber = 2,
                Order = 1, Name = "Максимальная мощность на входе (импульсная)", Unit = "кВт", IsRequired = false },
        new() { Form = form, Section = secInputPower, RowNumber = 3,
                Order = 2, Name = "Максимальная мощность на входе (средняя)", Unit = "Вт", IsRequired = false },

        // ---- Управление
        new() { Form = form, Section = secControl, RowNumber = 4,
                Order = 1, Name = "Ток управления (минимальное)", Unit = "мА", IsRequired = false },
        new() { Form = form, Section = secControl, RowNumber = 5,
                Order = 2, Name = "Ток управления (максимальное)", Unit = "мА", IsRequired = false },
        new() { Form = form, Section = secControl, RowNumber = 6,
                Order = 3, Name = "Обратное напряжение (минимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secControl, RowNumber = 7,
                Order = 4, Name = "Обратное напряжение (максимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secControl, RowNumber = 8,
                Order = 5, Name = "Мощность управления", Unit = "Вт", IsRequired = false },

        // ---- Импульсные параметры
        new() { Form = form, Section = secPulse, RowNumber = 9,
                Order = 1, Name = "Длительность импульса", Unit = "мкс", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 10,
                Order = 1, Name = "Температура окружающей среды (корпуса)", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 11,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm21(CatalogDbContext db)
        {
                // ---- Форма 21: Карта рабочих режимов ферритовых циркуляторов, вентилей, переключателей и ограничителей
                var form = new Form
                {
                        Number = "21",
                        Title = "Карта рабочих режимов ферритовых циркуляторов, вентилей, переключателей и ограничителей",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secFrequency = new FormSection { Form = form, Title = "Частотный диапазон", Order = 1 };
                var secInputPower = new FormSection { Form = form, Title = "Входная мощность", Order = 2 };
                var secPulse = new FormSection { Form = form, Title = "Импульсные параметры", Order = 3 };
                var secMagnetic = new FormSection { Form = form, Title = "Магнитные параметры", Order = 4 };
                var secSWR = new FormSection { Form = form, Title = "КСВН", Order = 5 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 6 };

                form.Sections.AddRange(new[] { secFrequency, secInputPower, secPulse, secMagnetic, secSWR, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Частотный диапазон
        new() { Form = form, Section = secFrequency, RowNumber = 1,
                Order = 1, Name = "Соответствие диапазону частот", Unit = "(да, нет)", IsRequired = false },

        // ---- Входная мощность
        new() { Form = form, Section = secInputPower, RowNumber = 2,
                Order = 1, Name = "Максимальная мощность на входе (импульсная)", Unit = "кВт", IsRequired = false },
        new() { Form = form, Section = secInputPower, RowNumber = 3,
                Order = 2, Name = "Максимальная мощность на входе (средняя)", Unit = "Вт", IsRequired = false },

        // ---- Импульсные параметры
        new() { Form = form, Section = secPulse, RowNumber = 4,
                Order = 1, Name = "Скважность импульсов", Unit = "", IsRequired = false },

        // ---- Магнитные параметры
        new() { Form = form, Section = secMagnetic, RowNumber = 5,
                Order = 1, Name = "Расстояние до ферромагнитных материалов", Unit = "мм", IsRequired = false },
        new() { Form = form, Section = secMagnetic, RowNumber = 6,
                Order = 2, Name = "Напряженность внешнего магнитного поля на месте установки прибора", Unit = "Э", IsRequired = false },

        // ---- КСВН
        new() { Form = form, Section = secSWR, RowNumber = 7,
                Order = 1, Name = "КСВН нагрузки", Unit = "", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 8,
                Order = 1, Name = "Температура окружающей среды (корпуса)", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 9,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm22(CatalogDbContext db)
        {
                // ---- Форма 22: Карта рабочих режимов полупроводниковых генераторов шума
                var form = new Form
                {
                        Number = "22",
                        Title = "Карта рабочих режимов полупроводниковых генераторов шума",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secCurrent = new FormSection { Form = form, Title = "Ток", Order = 1 };
                var secVoltage = new FormSection { Form = form, Title = "Напряжение", Order = 2 };
                var secLoad = new FormSection { Form = form, Title = "Нагрузка", Order = 3 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 4 };

                form.Sections.AddRange(new[] { secCurrent, secVoltage, secLoad, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Ток
        new() { Form = form, Section = secCurrent, RowNumber = 1,
                Order = 1, Name = "Максимальный ток", Unit = "мА", IsRequired = false },
        new() { Form = form, Section = secCurrent, RowNumber = 2,
                Order = 2, Name = "Минимальный ток", Unit = "мА", IsRequired = false },

        // ---- Напряжение
        new() { Form = form, Section = secVoltage, RowNumber = 3,
                Order = 1, Name = "Обратное напряжение", Unit = "В", IsRequired = false },

        // ---- Нагрузка
        new() { Form = form, Section = secLoad, RowNumber = 4,
                Order = 1, Name = "Емкость нагрузки", Unit = "пФ", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 5,
                Order = 1, Name = "Температура окружающей среды (корпуса)", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 6,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }

        //TODO: Form23

        private static Form AddForm24(CatalogDbContext db)
        {
                // ---- Форма 24: Карта рабочих режимов генераторных коаксиально-волноводных модулей СВЧ на металлокерамических лампах непрерывного режима
                var form = new Form
                {
                        Number = "24",
                        Title = "Карта рабочих режимов генераторных коаксиально-волноводных модулей СВЧ на металлокерамических лампах непрерывного режима",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secFilament = new FormSection { Form = form, Title = "Накал", Order = 1 };
                var secTiming = new FormSection { Form = form, Title = "Временные параметры", Order = 2 };
                var secCathode = new FormSection { Form = form, Title = "Катодная цепь", Order = 3 };
                var secGrid = new FormSection { Form = form, Title = "Сетка", Order = 4 };
                var secInput = new FormSection { Form = form, Title = "Входная мощность", Order = 5 };
                var secAnode = new FormSection { Form = form, Title = "Анод", Order = 6 };
                var secSWR = new FormSection { Form = form, Title = "КСВН", Order = 7 };
                var secFrequency = new FormSection { Form = form, Title = "Частотный диапазон", Order = 8 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 9 };
                var secNote = new FormSection { Form = form, Title = "Примечание", Order = 10 };

                form.Sections.AddRange(new[] { secFilament, secTiming, secCathode, secGrid, secInput, secAnode, secSWR, secFrequency, secEnv, secNote });

                var parameters = new List<FormParameter>
    {
        // ---- Накал
        new() { Form = form, Section = secFilament, RowNumber = 1,
                Order = 1, Name = "Напряжение накала (мин/макс)", Unit = "В", IsRequired = false },

        // ---- Временные параметры
        new() { Form = form, Section = secTiming, RowNumber = 2,
                Order = 1, Name = "Время задержки включения высокого напряжения", Unit = "с", IsRequired = false },

        // ---- Катодная цепь
        new() { Form = form, Section = secCathode, RowNumber = 3,
                Order = 1, Name = "Ток катода", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secCathode, RowNumber = 4,
                Order = 2, Name = "Величина сопротивления в цепи катода", Unit = "Ом", IsRequired = false },

        // ---- Сетка
        new() { Form = form, Section = secGrid, RowNumber = 5,
                Order = 1, Name = "Напряжение сетки", Unit = "В", IsRequired = false },

        // ---- Входная мощность
        new() { Form = form, Section = secInput, RowNumber = 6,
                Order = 1, Name = "Входная СВЧ мощность", Unit = "мВт", IsRequired = false },

        // ---- Анод
        new() { Form = form, Section = secAnode, RowNumber = 7,
                Order = 1, Name = "Напряжение анода", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secAnode, RowNumber = 8,
                Order = 2, Name = "Ток анода", Unit = "мА", IsRequired = false },
        new() { Form = form, Section = secAnode, RowNumber = 9,
                Order = 3, Name = "Мощность, подводимая к аноду", Unit = "мВт", IsRequired = false },

        // ---- КСВН
        new() { Form = form, Section = secSWR, RowNumber = 10,
                Order = 1, Name = "Коэффициент стоячей волны по направлению нагрузки", Unit = "", IsRequired = false },

        // ---- Частотный диапазон
        new() { Form = form, Section = secFrequency, RowNumber = 11,
                Order = 1, Name = "Соответствие диапазону частот", Unit = "(да, нет)", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 12,
                Order = 1, Name = "Температура окружающей среды (корпуса)", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 13,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },

        // ---- Примечание
        new() { Form = form, Section = secNote, RowNumber = 14,
                Order = 1, Name = "Примечание", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm25(CatalogDbContext db)
        {
                // ---- Форма 25: Карта рабочих режимов генераторных и усилительных коаксиально-волноводных модулей СВЧ на металлокерамических лампах импульсного режима
                var form = new Form
                {
                        Number = "25",
                        Title = "Карта рабочих режимов генераторных и усилительных коаксиально-волноводных модулей СВЧ на металлокерамических лампах импульсного режима",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secFilament = new FormSection { Form = form, Title = "Накал", Order = 1 };
                var secTiming = new FormSection { Form = form, Title = "Временные параметры", Order = 2 };
                var secCathode = new FormSection { Form = form, Title = "Катодная цепь", Order = 3 };
                var secGrid = new FormSection { Form = form, Title = "Сетка", Order = 4 };
                var secInput = new FormSection { Form = form, Title = "Входная мощность", Order = 5 };
                var secAnode = new FormSection { Form = form, Title = "Анод", Order = 6 };
                var secModulation = new FormSection { Form = form, Title = "Параметры моделирующих импульсов", Order = 7 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 8 };
                var secSWR = new FormSection { Form = form, Title = "КСВН", Order = 9 };
                var secLoad = new FormSection { Form = form, Title = "Коэффициент нагрузки", Order = 10 };

                form.Sections.AddRange(new[] { secFilament, secTiming, secCathode, secGrid, secInput, secAnode, secModulation, secEnv, secSWR, secLoad });

                var parameters = new List<FormParameter>
    {
        // ---- Накал
        new() { Form = form, Section = secFilament, RowNumber = 1,
                Order = 1, Name = "Напряжение накала (мин/макс)", Unit = "В", IsRequired = false },

        // ---- Временные параметры
        new() { Form = form, Section = secTiming, RowNumber = 2,
                Order = 1, Name = "Время задержки включения высокого напряжения", Unit = "с", IsRequired = false },

        // ---- Катодная цепь
        new() { Form = form, Section = secCathode, RowNumber = 3,
                Order = 1, Name = "Ток катода (средний)", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secCathode, RowNumber = 4,
                Order = 2, Name = "Ток катода (импульсный)", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secCathode, RowNumber = 5,
                Order = 3, Name = "Значение сопротивления в цепи катода", Unit = "Ом", IsRequired = false },

        // ---- Сетка
        new() { Form = form, Section = secGrid, RowNumber = 6,
                Order = 1, Name = "Напряжение сетки", Unit = "В", IsRequired = false },

        // ---- Входная мощность
        new() { Form = form, Section = secInput, RowNumber = 7,
                Order = 1, Name = "Входная СВЧ-мощность", Unit = "Вт", IsRequired = false },

        // ---- Анод
        new() { Form = form, Section = secAnode, RowNumber = 8,
                Order = 1, Name = "Напряжение анода (постоянное)", Unit = "кВ", IsRequired = false },
        new() { Form = form, Section = secAnode, RowNumber = 9,
                Order = 2, Name = "Напряжение анода (импульсное)", Unit = "кВ", IsRequired = false },
        new() { Form = form, Section = secAnode, RowNumber = 10,
                Order = 3, Name = "Ток анода (средний)", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secAnode, RowNumber = 11,
                Order = 4, Name = "Ток анода (импульсный)", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secAnode, RowNumber = 12,
                Order = 5, Name = "Мощность, подводимая к аноду", Unit = "Вт", IsRequired = false },

        // ---- Параметры моделирующих импульсов
        new() { Form = form, Section = secModulation, RowNumber = 13,
                Order = 1, Name = "Вид модуляции", Unit = "", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 14,
                Order = 1, Name = "Температура окружающей среды (корпуса)", Unit = "°С", IsRequired = false },

        // ---- КСВН
        new() { Form = form, Section = secSWR, RowNumber = 15,
                Order = 1, Name = "Коэффициент стоячей волны по напряжению нагрузки", Unit = "", IsRequired = false },

        // ---- Коэффициент нагрузки
        new() { Form = form, Section = secLoad, RowNumber = 16,
                Order = 1, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm26(CatalogDbContext db)
        {
                // ---- Форма 26: Карта рабочих режимов стабилитронов газонаполненных
                var form = new Form
                {
                        Number = "26",
                        Title = "Карта рабочих режимов стабилитронов газонаполненных",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secSupply = new FormSection { Form = form, Title = "Источник питания", Order = 1 };
                var secCurrent = new FormSection { Form = form, Title = "Ток стабилизации", Order = 2 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 3 };

                form.Sections.AddRange(new[] { secSupply, secCurrent, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Источник питания
        new() { Form = form, Section = secSupply, RowNumber = 1,
                Order = 1, Name = "Минимальное напряжение источника питания", Unit = "В", IsRequired = false },

        // ---- Ток стабилизации
        new() { Form = form, Section = secCurrent, RowNumber = 2,
                Order = 1, Name = "Ток стабилизации (максимальный)", Unit = "мА", IsRequired = false },
        new() { Form = form, Section = secCurrent, RowNumber = 3,
                Order = 2, Name = "Ток стабилизации (минимальный)", Unit = "мА", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 4,
                Order = 1, Name = "Температура окружающей среды (корпуса)", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 5,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        //TODO: Form27

        private static Form AddForm28(CatalogDbContext db)
        {
                // ---- Форма 28: Карта рабочих режимов кенотронов выпрямительных и импульсных
                var form = new Form
                {
                        Number = "28",
                        Title = "Карта рабочих режимов кенотронов выпрямительных и импульсных",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secFilament = new FormSection { Form = form, Title = "Накал", Order = 1 };
                var secTiming = new FormSection { Form = form, Title = "Временные параметры", Order = 2 };
                var secHeater = new FormSection { Form = form, Title = "Катод-подогреватель", Order = 3 };
                var secAnode = new FormSection { Form = form, Title = "Анод", Order = 4 };
                var secCurrentPulse = new FormSection { Form = form, Title = "Параметры импульса тока", Order = 5 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 6 };

                form.Sections.AddRange(new[] { secFilament, secTiming, secHeater, secAnode, secCurrentPulse, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Накал
        new() { Form = form, Section = secFilament, RowNumber = 1,
                Order = 1, Name = "Напряжение накала (мин)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secFilament, RowNumber = 2,
                Order = 2, Name = "Напряжение накала (макс)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secFilament, RowNumber = 3,
                Order = 3, Name = "Бросок тока накала", Unit = "А", IsRequired = false },

        // ---- Временные параметры
        new() { Form = form, Section = secTiming, RowNumber = 4,
                Order = 1, Name = "Время задержки включения высокого напряжения", Unit = "мин", IsRequired = false },

        // ---- Катод-подогреватель
        new() { Form = form, Section = secHeater, RowNumber = 5,
                Order = 1, Name = "Напряжение между катодом и подогревателем", Unit = "В", IsRequired = false },

        // ---- Анод
        new() { Form = form, Section = secAnode, RowNumber = 6,
                Order = 1, Name = "Амплитуда обратного напряжения", Unit = "кВ", IsRequired = false },
        new() { Form = form, Section = secAnode, RowNumber = 7,
                Order = 2, Name = "Длительность импульса обратного напряжения", Unit = "мкс", IsRequired = false },
        new() { Form = form, Section = secAnode, RowNumber = 8,
                Order = 3, Name = "Амплитуда тока в режиме выпрямления", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secAnode, RowNumber = 9,
                Order = 4, Name = "Амплитуда тока в импульсном режиме", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secAnode, RowNumber = 10,
                Order = 5, Name = "Средний (выпрямленный) ток", Unit = "мА", IsRequired = false },
        new() { Form = form, Section = secAnode, RowNumber = 11,
                Order = 6, Name = "Рассеиваемая мощность", Unit = "Вт", IsRequired = false },
        new() { Form = form, Section = secAnode, RowNumber = 12,
                Order = 7, Name = "Количество электричества в импульсе", Unit = "А·мкс", IsRequired = false },

        // ---- Параметры импульса тока
        new() { Form = form, Section = secCurrentPulse, RowNumber = 13,
                Order = 1, Name = "Частота следования", Unit = "Гц", IsRequired = false },
        new() { Form = form, Section = secCurrentPulse, RowNumber = 14,
                Order = 2, Name = "Длительность импульса", Unit = "мкс", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 15,
                Order = 1, Name = "Температура окружающей среды (корпуса)", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 16,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "В схеме" и "По НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "В схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "По НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }

        //TODO:Form29

        private static Form AddForm30(CatalogDbContext db)
        {
                // ---- Форма 30: Карта рабочих режимов цветных и монохромных кинескопов,
                // индикаторных и осциллографических цветных и монохромных электроннолучевых трубок
                var form = new Form
                {
                        Number = "30",
                        Title = "Карта рабочих режимов цветных и монохромных кинескопов, индикаторных и осциллографических цветных и монохромных электроннолучевых трубок",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secFilament = new FormSection { Form = form, Title = "Накал", Order = 1 };
                var secElectrodes = new FormSection { Form = form, Title = "Электроды", Order = 2 };
                var secVideo = new FormSection { Form = form, Title = "Видеосигнал", Order = 3 };
                var secCurrent = new FormSection { Form = form, Title = "Ток", Order = 4 };
                var secOtherElectrodes = new FormSection { Form = form, Title = "Другие электроды", Order = 5 };

                form.Sections.AddRange(new[] { secFilament, secElectrodes, secVideo, secCurrent, secOtherElectrodes });

                var parameters = new List<FormParameter>
    {
        // ---- Накал
        new() { Form = form, Section = secFilament, RowNumber = 1,
                Order = 1, Name = "Напряжение накала", Unit = "В", IsRequired = false },

        // ---- Электроды (основные)
        new() { Form = form, Section = secElectrodes, RowNumber = 2,
                Order = 1, Name = "Напряжение модулятора (запирающее)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secElectrodes, RowNumber = 3,
                Order = 2, Name = "Напряжение ускоряющего электрода", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secElectrodes, RowNumber = 4,
                Order = 3, Name = "Напряжение фокусирующего электрода", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secElectrodes, RowNumber = 5,
                Order = 4, Name = "Напряжение анода", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secElectrodes, RowNumber = 6,
                Order = 5, Name = "Напряжение между катодом и подогревателем", Unit = "В", IsRequired = false },

        // ---- Видеосигнал
        new() { Form = form, Section = secVideo, RowNumber = 7,
                Order = 1, Name = "Максимальная амплитуда видеосигнала", Unit = "В", IsRequired = false },

        // ---- Ток
        new() { Form = form, Section = secCurrent, RowNumber = 8,
                Order = 1, Name = "Суммарный ток анода", Unit = "мкА", IsRequired = false },

        // ---- Другие электроды (с подномерами на картинке)
        new() { Form = form, Section = secOtherElectrodes, RowNumber = 9,
                Order = 1, Name = "Напряжение других электродов", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secOtherElectrodes, RowNumber = 10,
                Order = 2, Name = "Напряжение второго анода", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secOtherElectrodes, RowNumber = 11,
                Order = 3, Name = "Напряжение корректирующего электрода", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secOtherElectrodes, RowNumber = 12,
                Order = 4, Name = "Напряжение сетки", Unit = "В", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm31(CatalogDbContext db)
        {
                // ---- Форма 31: Карта рабочих режимов индикаторов знакоинтегрирующих вакуумных люминесцентных
                var form = new Form
                {
                        Number = "31",
                        Title = "Карта рабочих режимов индикаторов знакоинтегрирующих вакуумных люминесцентных",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secAnode = new FormSection { Form = form, Title = "Напряжение анода (элементов)", Order = 1 };
                var secGrid = new FormSection { Form = form, Title = "Напряжение сетки", Order = 2 };
                var secStart = new FormSection { Form = form, Title = "Напряжение начала переменного", Order = 3 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 4 };

                form.Sections.AddRange(new[] { secAnode, secGrid, secStart, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Напряжение анода (элементов)
        new() { Form = form, Section = secAnode, RowNumber = 1,
                Order = 1, Name = "Напряжение элементов (постоянное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secAnode, RowNumber = 2,
                Order = 2, Name = "Напряжение элементов (импульсное)", Unit = "В", IsRequired = false },

        // ---- Напряжение сетки
        new() { Form = form, Section = secGrid, RowNumber = 3,
                Order = 1, Name = "Напряжение сетки (постоянное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secGrid, RowNumber = 4,
                Order = 2, Name = "Напряжение сетки (импульсное)", Unit = "В", IsRequired = false },

        // ---- Напряжение начала переменного
        new() { Form = form, Section = secStart, RowNumber = 5,
                Order = 1, Name = "Напряжение начала переменного (минимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secStart, RowNumber = 6,
                Order = 2, Name = "Напряжение начала переменного (максимальное)", Unit = "В", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 7,
                Order = 1, Name = "Температура окружающей среды (корпуса)", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 8,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm32(CatalogDbContext db)
        {
                // ---- Форма 32: Карта рабочих режимов индикаторов знакосинтезирующих жидкокристаллических
                var form = new Form
                {
                        Number = "32",
                        Title = "Карта рабочих режимов индикаторов знакосинтезирующих жидкокристаллических",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secIndicatorVoltage = new FormSection { Form = form, Title = "Напряжение на индикаторе", Order = 1 };
                var secIndicatorFreq = new FormSection { Form = form, Title = "Частота напряжения на индикаторе", Order = 2 };
                var secDCComponent = new FormSection { Form = form, Title = "Постоянная составляющая", Order = 3 };
                var secLogicSupply = new FormSection { Form = form, Title = "Питание логической части", Order = 4 };
                var secOutputSupply = new FormSection { Form = form, Title = "Питание выходных каскадов", Order = 5 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 6 };

                form.Sections.AddRange(new[] { secIndicatorVoltage, secIndicatorFreq, secDCComponent, secLogicSupply, secOutputSupply, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Напряжение на индикаторе
        new() { Form = form, Section = secIndicatorVoltage, RowNumber = 1,
                Order = 1, Name = "Напряжение на индикаторе (минимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secIndicatorVoltage, RowNumber = 2,
                Order = 2, Name = "Напряжение на индикаторе (максимальное)", Unit = "В", IsRequired = false },

        // ---- Частота напряжения на индикаторе
        new() { Form = form, Section = secIndicatorFreq, RowNumber = 3,
                Order = 1, Name = "Частота напряжения на индикаторе (минимальная)", Unit = "Гц", IsRequired = false },
        new() { Form = form, Section = secIndicatorFreq, RowNumber = 4,
                Order = 2, Name = "Частота напряжения на индикаторе (максимальная)", Unit = "Гц", IsRequired = false },

        // ---- Постоянная составляющая
        new() { Form = form, Section = secDCComponent, RowNumber = 5,
                Order = 1, Name = "Максимальная величина постоянной составляющей напряжения на индикаторе", Unit = "В", IsRequired = false },

        // ---- Питание логической части схемы управления
        new() { Form = form, Section = secLogicSupply, RowNumber = 6,
                Order = 1, Name = "Напряжение питания логической части схемы управления (минимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secLogicSupply, RowNumber = 7,
                Order = 2, Name = "Напряжение питания логической части схемы управления (максимальное)", Unit = "В", IsRequired = false },

        // ---- Питание выходных каскадов схемы управления
        new() { Form = form, Section = secOutputSupply, RowNumber = 8,
                Order = 1, Name = "Напряжение питания выходных каскадов схемы управления", Unit = "В", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 9,
                Order = 1, Name = "Температура окружающей среды (корпуса) (минимальная)", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 10,
                Order = 2, Name = "Температура окружающей среды (корпуса) (максимальная)", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 11,
                Order = 3, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm33(CatalogDbContext db)
        {
                // ---- Форма 33: Карта рабочих режимов индикаторов знакосинтезирующих газоразрядных постоянного тока
                var form = new Form
                {
                        Number = "33",
                        Title = "Карта рабочих режимов индикаторов знакосинтезирующих газоразрядных постоянного тока",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secAnodeSupply = new FormSection { Form = form, Title = "Напряжение питания на аноде", Order = 1 };
                var secPulseParams = new FormSection { Form = form, Title = "Импульсные параметры", Order = 2 };
                var secScanFreq = new FormSection { Form = form, Title = "Частота сканирования", Order = 3 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 4 };

                form.Sections.AddRange(new[] { secAnodeSupply, secPulseParams, secScanFreq, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Напряжение питания на аноде
        new() { Form = form, Section = secAnodeSupply, RowNumber = 1,
                Order = 1, Name = "Напряжение питания на аноде (минимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secAnodeSupply, RowNumber = 2,
                Order = 2, Name = "Напряжение питания на аноде (максимальное)", Unit = "В", IsRequired = false },

        // ---- Импульсные параметры
        new() { Form = form, Section = secPulseParams, RowNumber = 3,
                Order = 1, Name = "Максимальная длительность фронта импульсов", Unit = "мкс", IsRequired = false },
        new() { Form = form, Section = secPulseParams, RowNumber = 4,
                Order = 2, Name = "Длительность импульсов напряжения анодов", Unit = "мкс", IsRequired = false },

        // ---- Частота сканирования
        new() { Form = form, Section = secScanFreq, RowNumber = 5,
                Order = 1, Name = "Частота повторения циклов сканирования", Unit = "Гц", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 6,
                Order = 1, Name = "Температура окружающей среды (корпуса) (минимальная)", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 7,
                Order = 2, Name = "Температура окружающей среды (корпуса) (максимальная)", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 8,
                Order = 3, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm34(CatalogDbContext db)
        {
                // ---- Форма 34: Карта рабочих режимов индикаторов знакосинтезирующих газоразрядных переменного тока
                var form = new Form
                {
                        Number = "34",
                        Title = "Карта рабочих режимов индикаторов знакосинтезирующих газоразрядных переменного тока",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secReferenceVoltage = new FormSection { Form = form, Title = "Опорное напряжение индикации", Order = 1 };
                var secReferenceFreq = new FormSection { Form = form, Title = "Частота опорного напряжения", Order = 2 };
                var secReferenceDuration = new FormSection { Form = form, Title = "Длительность опорного напряжения", Order = 3 };
                var secWriteVoltage = new FormSection { Form = form, Title = "Превышение напряжения записи", Order = 4 };
                var secWriteDuration = new FormSection { Form = form, Title = "Длительность напряжения записи", Order = 5 };
                var secAgingVoltage = new FormSection { Form = form, Title = "Напряжение старения", Order = 6 };
                var secAgingDuration = new FormSection { Form = form, Title = "Длительность напряжения старения", Order = 7 };
                var secAuxiliary = new FormSection { Form = form, Title = "Напряжение вспомогательного разряда", Order = 8 };

                form.Sections.AddRange(new[] { secReferenceVoltage, secReferenceFreq, secReferenceDuration,
                                   secWriteVoltage, secWriteDuration, secAgingVoltage,
                                   secAgingDuration, secAuxiliary });

                var parameters = new List<FormParameter>
    {
        // ---- Опорное напряжение индикации
        new() { Form = form, Section = secReferenceVoltage, RowNumber = 1,
                Order = 1, Name = "Опорное напряжение индикации (минимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secReferenceVoltage, RowNumber = 2,
                Order = 2, Name = "Опорное напряжение индикации (максимальное)", Unit = "В", IsRequired = false },

        // ---- Частота повторения импульсов опорного напряжения
        new() { Form = form, Section = secReferenceFreq, RowNumber = 3,
                Order = 1, Name = "Частота повторения импульсов опорного напряжения (минимальная)", Unit = "кГц", IsRequired = false },
        new() { Form = form, Section = secReferenceFreq, RowNumber = 4,
                Order = 2, Name = "Частота повторения импульсов опорного напряжения (максимальная)", Unit = "кГц", IsRequired = false },

        // ---- Длительность импульсов опорного напряжения индикации
        new() { Form = form, Section = secReferenceDuration, RowNumber = 5,
                Order = 1, Name = "Длительность импульсов опорного напряжения индикации (минимальная)", Unit = "мкс", IsRequired = false },
        new() { Form = form, Section = secReferenceDuration, RowNumber = 6,
                Order = 2, Name = "Длительность импульсов опорного напряжения индикации (максимальная)", Unit = "мкс", IsRequired = false },

        // ---- Превышение напряжения записи над опорным напряжением индикации
        new() { Form = form, Section = secWriteVoltage, RowNumber = 7,
                Order = 1, Name = "Превышение напряжения записи над опорным напряжением индикации (минимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secWriteVoltage, RowNumber = 8,
                Order = 2, Name = "Превышение напряжения записи над опорным напряжением индикации (максимальное)", Unit = "В", IsRequired = false },

        // ---- Длительность импульсов напряжения записи
        new() { Form = form, Section = secWriteDuration, RowNumber = 9,
                Order = 1, Name = "Длительность импульсов напряжения записи (минимальная)", Unit = "мкс", IsRequired = false },
        new() { Form = form, Section = secWriteDuration, RowNumber = 10,
                Order = 2, Name = "Длительность импульсов напряжения записи (максимальная)", Unit = "мкс", IsRequired = false },

        // ---- Напряжение старения
        new() { Form = form, Section = secAgingVoltage, RowNumber = 11,
                Order = 1, Name = "Напряжение старения (минимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secAgingVoltage, RowNumber = 12,
                Order = 2, Name = "Напряжение старения (максимальное)", Unit = "В", IsRequired = false },

        // ---- Длительность импульсов напряжения старения
        new() { Form = form, Section = secAgingDuration, RowNumber = 13,
                Order = 1, Name = "Длительность импульсов напряжения старения (минимальная)", Unit = "мкс", IsRequired = false },
        new() { Form = form, Section = secAgingDuration, RowNumber = 14,
                Order = 2, Name = "Длительность импульсов напряжения старения (максимальная)", Unit = "мкс", IsRequired = false },

        // ---- Напряжение вспомогательного разряда
        new() { Form = form, Section = secAuxiliary, RowNumber = 15,
                Order = 1, Name = "Напряжение вспомогательного разряда (минимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secAuxiliary, RowNumber = 16,
                Order = 2, Name = "Напряжение вспомогательного разряда (максимальное)", Unit = "В", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        //TODO:35
        private static Form AddForm36(CatalogDbContext db)
        {
                // ---- Форма 36: Карта рабочих режимов диссекторов
                var form = new Form
                {
                        Number = "36",
                        Title = "Карта рабочих режимов диссекторов",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secSpectral = new FormSection { Form = form, Title = "Спектральная чувствительность", Order = 1 };
                var secIllumination = new FormSection { Form = form, Title = "Освещённость", Order = 2 };
                var secSupply = new FormSection { Form = form, Title = "Питание общее", Order = 3 };
                var secElectrodes = new FormSection { Form = form, Title = "Электроды", Order = 4 };
                var secDivider = new FormSection { Form = form, Title = "Делитель напряжения", Order = 5 };
                var secFocus = new FormSection { Form = form, Title = "Фокусировка", Order = 6 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 7 };

                form.Sections.AddRange(new[] { secSpectral, secIllumination, secSupply, secElectrodes, secDivider, secFocus, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Спектральная чувствительность
        new() { Form = form, Section = secSpectral, RowNumber = 1,
                Order = 1, Name = "Область спектральной чувствительности", Unit = "мкм", IsRequired = false },

        // ---- Освещённость
        new() { Form = form, Section = secIllumination, RowNumber = 2,
                Order = 1, Name = "Рабочая освещенность", Unit = "лк", IsRequired = false },

        // ---- Питание общее
        new() { Form = form, Section = secSupply, RowNumber = 3,
                Order = 1, Name = "Напряжение питания общее", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secSupply, RowNumber = 4,
                Order = 2, Name = "Нестабильность питания", Unit = "%", IsRequired = false },
        new() { Form = form, Section = secSupply, RowNumber = 5,
                Order = 3, Name = "Пульсация питания", Unit = "%", IsRequired = false },

        // ---- Электроды
        new() { Form = form, Section = secElectrodes, RowNumber = 6,
                Order = 1, Name = "Напряжение ускоряющего электрода", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secElectrodes, RowNumber = 7,
                Order = 2, Name = "Напряжение первого диода", Unit = "В", IsRequired = false },

        // ---- Делитель напряжения
        new() { Form = form, Section = secDivider, RowNumber = 8,
                Order = 1, Name = "Тип делителя напряжения", Unit = "", IsRequired = false },
        new() { Form = form, Section = secDivider, RowNumber = 9,
                Order = 2, Name = "Сопротивление делителя", Unit = "Ом", IsRequired = false },

        // ---- Фокусировка
        new() { Form = form, Section = secFocus, RowNumber = 10,
                Order = 1, Name = "Ток фокусирующей катушки", Unit = "мА", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 11,
                Order = 1, Name = "Температура окружающей среды", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 12,
                Order = 2, Name = "Пониженное атмосферное давление", Unit = "мм рт.ст.", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 13,
                Order = 3, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm37(CatalogDbContext db)
        {
                // ---- Форма 37: Карта рабочих режимов видиконов
                var form = new Form
                {
                        Number = "37",
                        Title = "Карта рабочих режимов видиконов",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secSpectral = new FormSection { Form = form, Title = "Спектральная чувствительность", Order = 1 };
                var secIllumination = new FormSection { Form = form, Title = "Освещённость", Order = 2 };
                var secFilament = new FormSection { Form = form, Title = "Накал", Order = 3 };
                var secElectrodes = new FormSection { Form = form, Title = "Напряжения на электродах", Order = 4 };
                var secStability = new FormSection { Form = form, Title = "Стабильность и пульсация", Order = 5 };
                var secDeflection = new FormSection { Form = form, Title = "Отклоняющие пластины", Order = 6 };
                var secTiming = new FormSection { Form = form, Title = "Временные параметры", Order = 7 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 8 };

                form.Sections.AddRange(new[] { secSpectral, secIllumination, secFilament, secElectrodes,
                                   secStability, secDeflection, secTiming, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Спектральная чувствительность
        new() { Form = form, Section = secSpectral, RowNumber = 1,
                Order = 1, Name = "Область спектральной чувствительности", Unit = "мкм", IsRequired = false },

        // ---- Освещённость
        new() { Form = form, Section = secIllumination, RowNumber = 2,
                Order = 1, Name = "Рабочая освещенность", Unit = "лк", IsRequired = false },

        // ---- Накал
        new() { Form = form, Section = secFilament, RowNumber = 3,
                Order = 1, Name = "Напряжение накала", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secFilament, RowNumber = 4,
                Order = 2, Name = "Ток накала", Unit = "мА", IsRequired = false },

        // ---- Напряжения на электродах
        new() { Form = form, Section = secElectrodes, RowNumber = 5,
                Order = 1, Name = "Напряжение первого анода", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secElectrodes, RowNumber = 6,
                Order = 2, Name = "Напряжение второго анода", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secElectrodes, RowNumber = 7,
                Order = 3, Name = "Напряжение сетки", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secElectrodes, RowNumber = 8,
                Order = 4, Name = "Напряжение катод-подогреватель", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secElectrodes, RowNumber = 9,
                Order = 5, Name = "Напряжение отклоняющих пластин", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secElectrodes, RowNumber = 10,
                Order = 6, Name = "Напряжение сигнальной пластины", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secElectrodes, RowNumber = 11,
                Order = 7, Name = "Напряжение модулятора", Unit = "В", IsRequired = false },

        // ---- Стабильность и пульсация
        new() { Form = form, Section = secStability, RowNumber = 12,
                Order = 1, Name = "Стабильность напряжений", Unit = "%", IsRequired = false },
        new() { Form = form, Section = secStability, RowNumber = 13,
                Order = 2, Name = "Пульсация напряжений", Unit = "%", IsRequired = false },

        // ---- Отклоняющие пластины
        new() { Form = form, Section = secDeflection, RowNumber = 14,
                Order = 1, Name = "Переменное напряжение отклоняющих пластин", Unit = "В", IsRequired = false },

        // ---- Временные параметры
        new() { Form = form, Section = secTiming, RowNumber = 15,
                Order = 1, Name = "Время задержки включения высокого напряжения", Unit = "с", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 16,
                Order = 1, Name = "Температура окружающей среды", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 17,
                Order = 2, Name = "Пониженное атмосферное давление", Unit = "мм рт.ст.", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 18,
                Order = 3, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm38(CatalogDbContext db)
        {
                // ---- Форма 38: Карта рабочих режимов супервидиков
                var form = new Form
                {
                        Number = "38",
                        Title = "Карта рабочих режимов супервидиков",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secSpectral = new FormSection { Form = form, Title = "Спектральная чувствительность", Order = 1 };
                var secIllumination = new FormSection { Form = form, Title = "Освещённость", Order = 2 };
                var secFilament = new FormSection { Form = form, Title = "Накал", Order = 3 };
                var secElectrodes = new FormSection { Form = form, Title = "Постоянные напряжения на электродах", Order = 4 };
                var secStability = new FormSection { Form = form, Title = "Стабильность и пульсация", Order = 5 };
                var secTiming = new FormSection { Form = form, Title = "Временные параметры", Order = 6 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 7 };

                form.Sections.AddRange(new[] { secSpectral, secIllumination, secFilament, secElectrodes,
                                   secStability, secTiming, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Спектральная чувствительность
        new() { Form = form, Section = secSpectral, RowNumber = 1,
                Order = 1, Name = "Область спектральной чувствительности", Unit = "мкм", IsRequired = false },

        // ---- Освещённость
        new() { Form = form, Section = secIllumination, RowNumber = 2,
                Order = 1, Name = "Рабочая освещенность", Unit = "лк", IsRequired = false },

        // ---- Накал
        new() { Form = form, Section = secFilament, RowNumber = 3,
                Order = 1, Name = "Напряжение накала", Unit = "В", IsRequired = false },

        // ---- Постоянные напряжения на электродах
        new() { Form = form, Section = secElectrodes, RowNumber = 4,
                Order = 1, Name = "Напряжение фотокатода", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secElectrodes, RowNumber = 5,
                Order = 2, Name = "Напряжение ускоряющего электрода", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secElectrodes, RowNumber = 6,
                Order = 3, Name = "Напряжение первого анода", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secElectrodes, RowNumber = 7,
                Order = 4, Name = "Напряжение второго анода", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secElectrodes, RowNumber = 8,
                Order = 5, Name = "Напряжение защитной сетки", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secElectrodes, RowNumber = 9,
                Order = 6, Name = "Напряжение выравнивающей сетки", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secElectrodes, RowNumber = 10,
                Order = 7, Name = "Напряжение катод-подогреватель", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secElectrodes, RowNumber = 11,
                Order = 8, Name = "Напряжение сигнальной пластины", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secElectrodes, RowNumber = 12,
                Order = 9, Name = "Напряжение модулятора", Unit = "В", IsRequired = false },

        // ---- Стабильность и пульсация
        new() { Form = form, Section = secStability, RowNumber = 13,
                Order = 1, Name = "Стабильность напряжений", Unit = "%", IsRequired = false },
        new() { Form = form, Section = secStability, RowNumber = 14,
                Order = 2, Name = "Пульсация напряжений", Unit = "%", IsRequired = false },

        // ---- Временные параметры
        new() { Form = form, Section = secTiming, RowNumber = 15,
                Order = 1, Name = "Время задержки включения высокого напряжения", Unit = "с", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 16,
                Order = 1, Name = "Температура окружающей среды", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 17,
                Order = 2, Name = "Пониженное атмосферное давление", Unit = "мм рт.ст.", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 18,
                Order = 3, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm39(CatalogDbContext db)
        {
                // ---- Форма 39: Карта рабочих режимов суперортиконов
                var form = new Form
                {
                        Number = "39",
                        Title = "Карта рабочих режимов суперортиконов",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secSpectral = new FormSection { Form = form, Title = "Спектральная чувствительность", Order = 1 };
                var secIllumination = new FormSection { Form = form, Title = "Освещённость", Order = 2 };
                var secFilament = new FormSection { Form = form, Title = "Накал", Order = 3 };
                var secElectrodes = new FormSection { Form = form, Title = "Постоянные напряжения на электродах", Order = 4 };
                var secStability = new FormSection { Form = form, Title = "Стабильность и пульсация", Order = 5 };
                var secTiming = new FormSection { Form = form, Title = "Временные параметры", Order = 6 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 7 };

                form.Sections.AddRange(new[] { secSpectral, secIllumination, secFilament, secElectrodes,
                                   secStability, secTiming, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Спектральная чувствительность
        new() { Form = form, Section = secSpectral, RowNumber = 1,
                Order = 1, Name = "Область спектральной чувствительности", Unit = "мкм", IsRequired = false },

        // ---- Освещённость
        new() { Form = form, Section = secIllumination, RowNumber = 2,
                Order = 1, Name = "Рабочая освещенность", Unit = "лк", IsRequired = false },

        // ---- Накал
        new() { Form = form, Section = secFilament, RowNumber = 3,
                Order = 1, Name = "Напряжение накала", Unit = "В", IsRequired = false },

        // ---- Постоянные напряжения на электродах
        new() { Form = form, Section = secElectrodes, RowNumber = 4,
                Order = 1, Name = "Напряжение ускоряющего электрода фотокатода", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secElectrodes, RowNumber = 5,
                Order = 2, Name = "Напряжение фотокатода", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secElectrodes, RowNumber = 6,
                Order = 3, Name = "Напряжение сетки мишени", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secElectrodes, RowNumber = 7,
                Order = 4, Name = "Напряжение тормозящего электрода", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secElectrodes, RowNumber = 8,
                Order = 5, Name = "Напряжение фокусирующего электрода", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secElectrodes, RowNumber = 9,
                Order = 6, Name = "Напряжение цилиндра умножителя", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secElectrodes, RowNumber = 10,
                Order = 7, Name = "Напряжение между каскадами умножителя", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secElectrodes, RowNumber = 11,
                Order = 8, Name = "Напряжение анода", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secElectrodes, RowNumber = 12,
                Order = 9, Name = "Напряжение коллектора", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secElectrodes, RowNumber = 13,
                Order = 10, Name = "Напряжение модулятора", Unit = "В", IsRequired = false },

        // ---- Стабильность и пульсация
        new() { Form = form, Section = secStability, RowNumber = 14,
                Order = 1, Name = "Стабильность напряжений", Unit = "%", IsRequired = false },
        new() { Form = form, Section = secStability, RowNumber = 15,
                Order = 2, Name = "Пульсация напряжений", Unit = "%", IsRequired = false },

        // ---- Временные параметры
        new() { Form = form, Section = secTiming, RowNumber = 16,
                Order = 1, Name = "Время задержки включения высокого напряжения", Unit = "с", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 17,
                Order = 1, Name = "Температура окружающей среды", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 18,
                Order = 2, Name = "Пониженное атмосферное давление", Unit = "мм рт.ст.", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 19,
                Order = 3, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm40(CatalogDbContext db)
        {
                // ---- Форма 40: Карта рабочих режимов фотоумножителей
                var form = new Form
                {
                        Number = "40",
                        Title = "Карта рабочих режимов фотоумножителей",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secSpectral = new FormSection { Form = form, Title = "Спектральная чувствительность", Order = 1 };
                var secSupply = new FormSection { Form = form, Title = "Питание", Order = 2 };
                var secDivider = new FormSection { Form = form, Title = "Делитель напряжения", Order = 3 };
                var secAnode = new FormSection { Form = form, Title = "Анодный ток", Order = 4 };
                var secLoad = new FormSection { Form = form, Title = "Нагрузка", Order = 5 };
                var secMagnetic = new FormSection { Form = form, Title = "Магнитное поле", Order = 6 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 7 };

                form.Sections.AddRange(new[] { secSpectral, secSupply, secDivider, secAnode, secLoad, secMagnetic, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Спектральная чувствительность
        new() { Form = form, Section = secSpectral, RowNumber = 1,
                Order = 1, Name = "Область спектральной чувствительности", Unit = "мкм", IsRequired = false },

        // ---- Питание
        new() { Form = form, Section = secSupply, RowNumber = 2,
                Order = 1, Name = "Напряжение питания", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secSupply, RowNumber = 3,
                Order = 2, Name = "Стабильность напряжения питания", Unit = "%", IsRequired = false },
        new() { Form = form, Section = secSupply, RowNumber = 4,
                Order = 3, Name = "Пульсация напряжения питания", Unit = "%", IsRequired = false },

        // ---- Делитель напряжения
        new() { Form = form, Section = secDivider, RowNumber = 5,
                Order = 1, Name = "Тип делителя напряжения", Unit = "", IsRequired = false },
        new() { Form = form, Section = secDivider, RowNumber = 6,
                Order = 2, Name = "Сопротивление делителя", Unit = "Ом", IsRequired = false },

        // ---- Анодный ток
        new() { Form = form, Section = secAnode, RowNumber = 7,
                Order = 1, Name = "Анодный ток", Unit = "А", IsRequired = false },

        // ---- Нагрузка
        new() { Form = form, Section = secLoad, RowNumber = 8,
                Order = 1, Name = "Сопротивление нагрузки", Unit = "Ом", IsRequired = false },
        new() { Form = form, Section = secLoad, RowNumber = 9,
                Order = 2, Name = "Емкость нагрузки", Unit = "мкФ", IsRequired = false },

        // ---- Магнитное поле
        new() { Form = form, Section = secMagnetic, RowNumber = 10,
                Order = 1, Name = "Напряженность магнитного поля", Unit = "Э", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 11,
                Order = 1, Name = "Температура окружающей среды", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 12,
                Order = 2, Name = "Пониженное атмосферное давление", Unit = "мм рт.ст.", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 13,
                Order = 3, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm41(CatalogDbContext db)
        {
                // ---- Форма 41: Карта рабочих режимов электронно-оптических преобразователей
                var form = new Form
                {
                        Number = "41",
                        Title = "Карта рабочих режимов электронно-оптических преобразователей",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secSpectral = new FormSection { Form = form, Title = "Спектральная чувствительность", Order = 1 };
                var secIllumination = new FormSection { Form = form, Title = "Освещённость", Order = 2 };
                var secSupply = new FormSection { Form = form, Title = "Питание", Order = 3 };
                var secProtection = new FormSection { Form = form, Title = "Схема защиты от засветок", Order = 4 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 5 };

                form.Sections.AddRange(new[] { secSpectral, secIllumination, secSupply, secProtection, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Спектральная чувствительность
        new() { Form = form, Section = secSpectral, RowNumber = 1,
                Order = 1, Name = "Область спектральной чувствительности", Unit = "мкм", IsRequired = false },

        // ---- Освещённость
        new() { Form = form, Section = secIllumination, RowNumber = 2,
                Order = 1, Name = "Рабочая освещенность", Unit = "лк", IsRequired = false },

        // ---- Питание
        new() { Form = form, Section = secSupply, RowNumber = 3,
                Order = 1, Name = "Напряжение питания общее", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secSupply, RowNumber = 4,
                Order = 2, Name = "Напряжение на 1-й камере", Unit = "В", IsRequired = false },

        // ---- Схема защиты от засветок
        new() { Form = form, Section = secProtection, RowNumber = 5,
                Order = 1, Name = "Уровень освещенности срабатывания защиты", Unit = "лк", IsRequired = false },
        new() { Form = form, Section = secProtection, RowNumber = 6,
                Order = 2, Name = "Время срабатывания защиты", Unit = "с", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 7,
                Order = 1, Name = "Температура окружающей среды", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 8,
                Order = 2, Name = "Пониженное атмосферное давление", Unit = "мм рт.ст.", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 9,
                Order = 3, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }

        //TODO: Form42

        //TODO:Form43

        private static Form AddForm44(CatalogDbContext db)
        {
                // ---- Форма 44: Карта рабочих режимов фоторезисторов, фотодиодов, фототранзисторов и тепловых приемников излучения
                var form = new Form
                {
                        Number = "44",
                        Title = "Карта рабочих режимов фоторезисторов, фотодиодов, фототранзисторов и тепловых приемников излучения",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secSpectral = new FormSection { Form = form, Title = "Спектральная чувствительность", Order = 1 };
                var secIllumination = new FormSection { Form = form, Title = "Освещённость", Order = 2 };
                var secOpticalPower = new FormSection { Form = form, Title = "Оптическая мощность", Order = 3 };
                var secSupply = new FormSection { Form = form, Title = "Питание", Order = 4 };
                var secDissipation = new FormSection { Form = form, Title = "Рассеиваемая мощность", Order = 5 };
                var secLoad = new FormSection { Form = form, Title = "Нагрузка", Order = 6 };
                var secTemp = new FormSection { Form = form, Title = "Температурные параметры", Order = 7 };
                var secCoolant = new FormSection { Form = form, Title = "Охлаждение", Order = 8 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 9 };

                form.Sections.AddRange(new[] { secSpectral, secIllumination, secOpticalPower, secSupply,
                                   secDissipation, secLoad, secTemp, secCoolant, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Спектральная чувствительность
        new() { Form = form, Section = secSpectral, RowNumber = 1,
                Order = 1, Name = "Область спектральной чувствительности", Unit = "мкм", IsRequired = false },

        // ---- Освещённость
        new() { Form = form, Section = secIllumination, RowNumber = 2,
                Order = 1, Name = "Рабочая освещенность", Unit = "лк", IsRequired = false },

        // ---- Оптическая мощность
        new() { Form = form, Section = secOpticalPower, RowNumber = 3,
                Order = 1, Name = "Мощность оптического излучения", Unit = "Вт", IsRequired = false },

        // ---- Питание
        new() { Form = form, Section = secSupply, RowNumber = 4,
                Order = 1, Name = "Напряжение питания", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secSupply, RowNumber = 5,
                Order = 2, Name = "Стабильность напряжения питания", Unit = "%", IsRequired = false },
        new() { Form = form, Section = secSupply, RowNumber = 6,
                Order = 3, Name = "Пульсация напряжения питания", Unit = "%", IsRequired = false },

        // ---- Рассеиваемая мощность
        new() { Form = form, Section = secDissipation, RowNumber = 7,
                Order = 1, Name = "Мощность рассеивания", Unit = "Вт", IsRequired = false },

        // ---- Нагрузка
        new() { Form = form, Section = secLoad, RowNumber = 8,
                Order = 1, Name = "Емкость нагрузки", Unit = "пФ", IsRequired = false },

        // ---- Температурные параметры
        new() { Form = form, Section = secTemp, RowNumber = 9,
                Order = 1, Name = "Рабочая температура чувствительного элемента", Unit = "°К", IsRequired = false },

        // ---- Охлаждение
        new() { Form = form, Section = secCoolant, RowNumber = 10,
                Order = 1, Name = "Рабочее давление хладагента", Unit = "мПа", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 11,
                Order = 1, Name = "Температура окружающей среды", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 12,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm45(CatalogDbContext db)
        {
                // ---- Форма 45: Карта рабочих режимов фотоприемных устройств и тепловых приемных устройств
                var form = new Form
                {
                        Number = "45",
                        Title = "Карта рабочих режимов фотоприемных устройств и тепловых приемных устройств",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secSpectral = new FormSection { Form = form, Title = "Спектральная чувствительность", Order = 1 };
                var secOptical = new FormSection { Form = form, Title = "Параметры оптического излучения", Order = 2 };
                var secSupply = new FormSection { Form = form, Title = "Напряжение питания", Order = 3 };
                var secCurrent = new FormSection { Form = form, Title = "Ток потребления", Order = 4 };
                var secDissipation = new FormSection { Form = form, Title = "Мощность рассеивания", Order = 5 };
                var secLoad = new FormSection { Form = form, Title = "Нагрузка", Order = 6 };
                var secTemp = new FormSection { Form = form, Title = "Температурные параметры", Order = 7 };
                var secCoolant = new FormSection { Form = form, Title = "Охлаждение", Order = 8 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 9 };

                form.Sections.AddRange(new[] { secSpectral, secOptical, secSupply, secCurrent,
                                   secDissipation, secLoad, secTemp, secCoolant, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Спектральная чувствительность
        new() { Form = form, Section = secSpectral, RowNumber = 1,
                Order = 1, Name = "Область спектральной чувствительности", Unit = "мкм", IsRequired = false },

        // ---- Параметры оптического излучения
        new() { Form = form, Section = secOptical, RowNumber = 2,
                Order = 1, Name = "Освещенность", Unit = "лк", IsRequired = false },
        new() { Form = form, Section = secOptical, RowNumber = 3,
                Order = 2, Name = "Мощность оптического излучения", Unit = "Вт", IsRequired = false },
        new() { Form = form, Section = secOptical, RowNumber = 4,
                Order = 3, Name = "Частота модуляции", Unit = "Гц", IsRequired = false },

        // ---- Напряжение питания (чувствительность элемента и усилителя разделены)
        new() { Form = form, Section = secSupply, RowNumber = 5,
                Order = 1, Name = "Напряжение питания чувствительного элемента", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secSupply, RowNumber = 6,
                Order = 2, Name = "Напряжение питания усилителя", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secSupply, RowNumber = 7,
                Order = 3, Name = "Стабильность напряжения питания", Unit = "%", IsRequired = false },
        new() { Form = form, Section = secSupply, RowNumber = 8,
                Order = 4, Name = "Пульсация напряжения питания", Unit = "%", IsRequired = false },

        // ---- Ток потребления
        new() { Form = form, Section = secCurrent, RowNumber = 9,
                Order = 1, Name = "Ток потребления", Unit = "А", IsRequired = false },

        // ---- Мощность рассеивания
        new() { Form = form, Section = secDissipation, RowNumber = 10,
                Order = 1, Name = "Мощность рассеивания", Unit = "Вт", IsRequired = false },

        // ---- Нагрузка
        new() { Form = form, Section = secLoad, RowNumber = 11,
                Order = 1, Name = "Сопротивление нагрузки", Unit = "Ом", IsRequired = false },
        new() { Form = form, Section = secLoad, RowNumber = 12,
                Order = 2, Name = "Емкость нагрузки", Unit = "пФ", IsRequired = false },

        // ---- Температурные параметры
        new() { Form = form, Section = secTemp, RowNumber = 13,
                Order = 1, Name = "Рабочая температура чувствительного элемента", Unit = "°К", IsRequired = false },

        // ---- Охлаждение
        new() { Form = form, Section = secCoolant, RowNumber = 14,
                Order = 1, Name = "Рабочее давление хладагента", Unit = "мПа", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 15,
                Order = 1, Name = "Температура окружающей среды", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 16,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm46(CatalogDbContext db)
        {
                // ---- Форма 46: Карта рабочих режимов оптоэлектронных приемных устройств
                var form = new Form
                {
                        Number = "46",
                        Title = "Карта рабочих режимов оптоэлектронных приемных устройств",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secOptical = new FormSection { Form = form, Title = "Параметры оптического излучения", Order = 1 };
                var secPulse = new FormSection { Form = form, Title = "Временные параметры импульса", Order = 2 };
                var secCurrent = new FormSection { Form = form, Title = "Ток потребления", Order = 3 };
                var secSupply = new FormSection { Form = form, Title = "Питание", Order = 4 };
                var secOutput = new FormSection { Form = form, Title = "Выходной ток", Order = 5 };
                var secLoad = new FormSection { Form = form, Title = "Нагрузка", Order = 6 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 7 };

                form.Sections.AddRange(new[] { secOptical, secPulse, secCurrent, secSupply, secOutput, secLoad, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Параметры оптического излучения
        new() { Form = form, Section = secOptical, RowNumber = 1,
                Order = 1, Name = "Длина волны оптического излучения", Unit = "мкм", IsRequired = false },
        new() { Form = form, Section = secOptical, RowNumber = 2,
                Order = 2, Name = "Средняя мощность импульса", Unit = "Вт", IsRequired = false },
        new() { Form = form, Section = secOptical, RowNumber = 3,
                Order = 3, Name = "Скважность", Unit = "отн. ед.", IsRequired = false },
        new() { Form = form, Section = secOptical, RowNumber = 4,
                Order = 4, Name = "Частота следования импульсов", Unit = "Гц", IsRequired = false },

        // ---- Временные параметры импульса
        new() { Form = form, Section = secPulse, RowNumber = 5,
                Order = 1, Name = "Длительность импульса", Unit = "нс", IsRequired = false },
        new() { Form = form, Section = secPulse, RowNumber = 6,
                Order = 2, Name = "Длительность фронта импульса", Unit = "нс", IsRequired = false },
        new() { Form = form, Section = secPulse, RowNumber = 7,
                Order = 3, Name = "Длительность среза импульса", Unit = "нс", IsRequired = false },
        new() { Form = form, Section = secPulse, RowNumber = 8,
                Order = 4, Name = "Неравномерность вершин импульсов", Unit = "%", IsRequired = false },

        // ---- Ток потребления
        new() { Form = form, Section = secCurrent, RowNumber = 9,
                Order = 1, Name = "Ток потребления", Unit = "А", IsRequired = false },

        // ---- Питание
        new() { Form = form, Section = secSupply, RowNumber = 10,
                Order = 1, Name = "Напряжение питания", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secSupply, RowNumber = 11,
                Order = 2, Name = "Стабильность напряжения питания", Unit = "%", IsRequired = false },
        new() { Form = form, Section = secSupply, RowNumber = 12,
                Order = 3, Name = "Пульсация напряжения питания", Unit = "%", IsRequired = false },

        // ---- Выходной ток
        new() { Form = form, Section = secOutput, RowNumber = 13,
                Order = 1, Name = "Выходной ток высокого уровня", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secOutput, RowNumber = 14,
                Order = 2, Name = "Выходной ток низкого уровня", Unit = "А", IsRequired = false },

        // ---- Нагрузка
        new() { Form = form, Section = secLoad, RowNumber = 15,
                Order = 1, Name = "Сопротивление нагрузки", Unit = "Ом", IsRequired = false },
        new() { Form = form, Section = secLoad, RowNumber = 16,
                Order = 2, Name = "Емкость нагрузки", Unit = "пФ", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 17,
                Order = 1, Name = "Температура окружающей среды (корпуса)", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 18,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm47(CatalogDbContext db)
        {
                // ---- Форма 47: Карта рабочих режимов оптопар
                var form = new Form
                {
                        Number = "47",
                        Title = "Карта рабочих режимов оптопар",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secInput = new FormSection { Form = form, Title = "Режим входа (излучатель)", Order = 1 };
                var secOutput = new FormSection { Form = form, Title = "Режим выхода (фотоприемник)", Order = 2 };
                var secIsolation = new FormSection { Form = form, Title = "Развязка", Order = 3 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 4 };

                form.Sections.AddRange(new[] { secInput, secOutput, secIsolation, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Режим входа (излучатель - обычно светодиод)
        new() { Form = form, Section = secInput, RowNumber = 1,
                Order = 1, Name = "Средний (постоянный) ток входа", Unit = "мА", IsRequired = false },
        new() { Form = form, Section = secInput, RowNumber = 2,
                Order = 2, Name = "Импульсный ток входа", Unit = "мА", IsRequired = false },
        new() { Form = form, Section = secInput, RowNumber = 3,
                Order = 3, Name = "Длительность импульса входа", Unit = "мкс", IsRequired = false },
        new() { Form = form, Section = secInput, RowNumber = 4,
                Order = 4, Name = "Скважность импульсов входа", Unit = "", IsRequired = false },
        new() { Form = form, Section = secInput, RowNumber = 5,
                Order = 5, Name = "Максимальное обратное напряжение входа", Unit = "В", IsRequired = false },

        // ---- Режим выхода (фотоприемник - фотодиод, фототранзистор и т.д.)
        new() { Form = form, Section = secOutput, RowNumber = 6,
                Order = 1, Name = "Максимальное обратное напряжение выхода", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secOutput, RowNumber = 7,
                Order = 2, Name = "Максимальный ток выхода", Unit = "мА", IsRequired = false },
        new() { Form = form, Section = secOutput, RowNumber = 8,
                Order = 3, Name = "Максимальное прямое напряжение выхода", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secOutput, RowNumber = 9,
                Order = 4, Name = "Мощность рассеивания выхода", Unit = "мВт", IsRequired = false },

        // ---- Развязка
        new() { Form = form, Section = secIsolation, RowNumber = 10,
                Order = 1, Name = "Напряжение развязки (изоляции)", Unit = "кВ", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 11,
                Order = 1, Name = "Температура окружающей среды (корпуса)", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 12,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm48(CatalogDbContext db)
        {
                // ---- Форма 48: Карта рабочих режимов оптоэлектронных переключателей логического сигнала
                var form = new Form
                {
                        Number = "48",
                        Title = "Карта рабочих режимов оптоэлектронных переключателей логического сигнала",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secInput = new FormSection { Form = form, Title = "Режим входа", Order = 1 };
                var secOutput = new FormSection { Form = form, Title = "Режим выхода", Order = 2 };
                var secIsolation = new FormSection { Form = form, Title = "Развязка", Order = 3 };
                var secSupply = new FormSection { Form = form, Title = "Питание", Order = 4 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 5 };

                form.Sections.AddRange(new[] { secInput, secOutput, secIsolation, secSupply, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Режим входа
        new() { Form = form, Section = secInput, RowNumber = 1,
                Order = 1, Name = "Средний (постоянный) ток входа", Unit = "мА", IsRequired = false },
        new() { Form = form, Section = secInput, RowNumber = 2,
                Order = 2, Name = "Импульсный ток входа", Unit = "мА", IsRequired = false },
        new() { Form = form, Section = secInput, RowNumber = 3,
                Order = 3, Name = "Длительность импульса входа", Unit = "мкс", IsRequired = false },
        new() { Form = form, Section = secInput, RowNumber = 4,
                Order = 4, Name = "Скважность", Unit = "", IsRequired = false },
        new() { Form = form, Section = secInput, RowNumber = 5,
                Order = 5, Name = "Максимальное обратное напряжение входа", Unit = "В", IsRequired = false },

        // ---- Режим выхода (логические уровни)
        new() { Form = form, Section = secOutput, RowNumber = 6,
                Order = 1, Name = "Максимальный ток логического нуля", Unit = "мА", IsRequired = false },
        new() { Form = form, Section = secOutput, RowNumber = 7,
                Order = 2, Name = "Максимальный ток логической единицы", Unit = "мА", IsRequired = false },
        new() { Form = form, Section = secOutput, RowNumber = 8,
                Order = 3, Name = "Максимальная емкость нагрузки", Unit = "пФ", IsRequired = false },

        // ---- Развязка
        new() { Form = form, Section = secIsolation, RowNumber = 9,
                Order = 1, Name = "Скорость нарастания напряжения между входом и выходом", Unit = "В/мкс", IsRequired = false },
        new() { Form = form, Section = secIsolation, RowNumber = 10,
                Order = 2, Name = "Напряжение развязки (изоляции)", Unit = "кВ", IsRequired = false },

        // ---- Питание
        new() { Form = form, Section = secSupply, RowNumber = 11,
                Order = 1, Name = "Напряжение источника питания", Unit = "В", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 12,
                Order = 1, Name = "Температура окружающей среды (корпуса)", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 13,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm49(CatalogDbContext db)
        {
                // ---- Форма 49: Карта рабочих режимов газовых лазеров непрерывного и импульсного режима работы
                var form = new Form
                {
                        Number = "49",
                        Title = "Карта рабочих режимов газовых лазеров непрерывного и импульсного режима работы",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secPump = new FormSection { Form = form, Title = "Накачка", Order = 1 };
                var secDischarge = new FormSection { Form = form, Title = "Разряд", Order = 2 };
                var secRadiation = new FormSection { Form = form, Title = "Излучение", Order = 3 };
                var secBeam = new FormSection { Form = form, Title = "Диаграмма направленности", Order = 4 };
                var secCooling = new FormSection { Form = form, Title = "Система охлаждения", Order = 5 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 6 };

                form.Sections.AddRange(new[] { secPump, secDischarge, secRadiation, secBeam, secCooling, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Накачка
        new() { Form = form, Section = secPump, RowNumber = 1,
                Order = 1, Name = "Напряжение накачки (мин/макс)", Unit = "В", IsRequired = false },

        // ---- Частота повторения импульсов излучения
        new() { Form = form, Section = secPump, RowNumber = 2,
                Order = 2, Name = "Частота повторения импульсов излучения (мин/макс)", Unit = "Гц", IsRequired = false },

        // ---- Разряд
        new() { Form = form, Section = secDischarge, RowNumber = 3,
                Order = 1, Name = "Ток разряда активного элемента (мин/макс)", Unit = "мА", IsRequired = false },
        new() { Form = form, Section = secDischarge, RowNumber = 4,
                Order = 2, Name = "Ток разряда импульсный (мин/макс)", Unit = "А", IsRequired = false },

        // ---- Излучение
        new() { Form = form, Section = secRadiation, RowNumber = 5,
                Order = 1, Name = "Мощность излучения (непрерывная)", Unit = "Вт", IsRequired = false },
        new() { Form = form, Section = secRadiation, RowNumber = 6,
                Order = 2, Name = "Средняя мощность излучения", Unit = "Вт", IsRequired = false },
        new() { Form = form, Section = secRadiation, RowNumber = 7,
                Order = 3, Name = "Мощность импульса излучения", Unit = "Вт", IsRequired = false },

        // ---- Диаграмма направленности
        new() { Form = form, Section = secBeam, RowNumber = 8,
                Order = 1, Name = "Нестабильность оси диаграммы направленности", Unit = "рад", IsRequired = false },

        // ---- Система охлаждения
        new() { Form = form, Section = secCooling, RowNumber = 9,
                Order = 1, Name = "Давление охлаждающей жидкости на входе", Unit = "кгс/см²", IsRequired = false },
        new() { Form = form, Section = secCooling, RowNumber = 10,
                Order = 2, Name = "Расход охлаждающей жидкости", Unit = "л/мин", IsRequired = false },
        new() { Form = form, Section = secCooling, RowNumber = 11,
                Order = 3, Name = "Температура охлаждающей жидкости", Unit = "°С", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 12,
                Order = 1, Name = "Температура окружающей среды", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 13,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm50(CatalogDbContext db)
        {
                // ---- Форма 50: Карта рабочих режимов твердотельных лазеров непрерывного и импульсного режима работы
                var form = new Form
                {
                        Number = "50",
                        Title = "Карта рабочих режимов твердотельных лазеров непрерывного и импульсного режима работы",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secRadiation = new FormSection { Form = form, Title = "Излучение", Order = 1 };
                var secPump = new FormSection { Form = form, Title = "Накачка", Order = 2 };
                var secPulse = new FormSection { Form = form, Title = "Импульсные параметры", Order = 3 };
                var secLamp = new FormSection { Form = form, Title = "Лампа накачки", Order = 4 };
                var secCooling = new FormSection { Form = form, Title = "Система охлаждения", Order = 5 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 6 };

                form.Sections.AddRange(new[] { secRadiation, secPump, secPulse, secLamp, secCooling, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Излучение
        new() { Form = form, Section = secRadiation, RowNumber = 1,
                Order = 1, Name = "Мощность излучения (непрерывная)", Unit = "Вт", IsRequired = false },

        // ---- Накачка
        new() { Form = form, Section = secPump, RowNumber = 2,
                Order = 1, Name = "Мощность накачки", Unit = "Вт", IsRequired = false },

        // ---- Импульсные параметры
        new() { Form = form, Section = secPulse, RowNumber = 3,
                Order = 1, Name = "Энергия импульса излучения", Unit = "Дж", IsRequired = false },
        new() { Form = form, Section = secPulse, RowNumber = 4,
                Order = 2, Name = "Длительность импульса излучения", Unit = "мкс", IsRequired = false },
        new() { Form = form, Section = secPulse, RowNumber = 5,
                Order = 3, Name = "Частота повторения импульсов излучения", Unit = "Гц", IsRequired = false },
        new() { Form = form, Section = secPulse, RowNumber = 6,
                Order = 4, Name = "Энергия накачки", Unit = "мДж", IsRequired = false },

        // ---- Лампа накачки
        new() { Form = form, Section = secLamp, RowNumber = 7,
                Order = 1, Name = "Рабочее напряжение лампы накачки", Unit = "В", IsRequired = false },

        // ---- Система охлаждения
        new() { Form = form, Section = secCooling, RowNumber = 8,
                Order = 1, Name = "Давление охлаждающей жидкости на входе", Unit = "кгс/см²", IsRequired = false },
        new() { Form = form, Section = secCooling, RowNumber = 9,
                Order = 2, Name = "Расход охлаждающей жидкости", Unit = "л/мин", IsRequired = false },
        new() { Form = form, Section = secCooling, RowNumber = 10,
                Order = 3, Name = "Температура охлаждающей жидкости", Unit = "°С", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 11,
                Order = 1, Name = "Температура окружающей среды", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 12,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm51(CatalogDbContext db)
        {
                // ---- Форма 51: Карта рабочих режимов полупроводниковых лазеров непрерывного и импульсного режима работы
                var form = new Form
                {
                        Number = "51",
                        Title = "Карта рабочих режимов полупроводниковых лазеров непрерывного и импульсного режима работы",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secPower = new FormSection { Form = form, Title = "Мощность", Order = 1 };
                var secPulse = new FormSection { Form = form, Title = "Импульсные параметры", Order = 2 };
                var secWavelength = new FormSection { Form = form, Title = "Спектральные параметры", Order = 3 };
                var secCurrent = new FormSection { Form = form, Title = "Ток накачки", Order = 4 };
                var secNonlinearity = new FormSection { Form = form, Title = "Линейность ВАХ", Order = 5 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 6 };

                form.Sections.AddRange(new[] { secPower, secPulse, secWavelength, secCurrent, secNonlinearity, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Мощность
        new() { Form = form, Section = secPower, RowNumber = 1,
                Order = 1, Name = "Средняя мощность излучения", Unit = "Вт", IsRequired = false },
        new() { Form = form, Section = secPower, RowNumber = 2,
                Order = 2, Name = "Средняя мощность накачки", Unit = "Вт", IsRequired = false },

        // ---- Импульсные параметры
        new() { Form = form, Section = secPulse, RowNumber = 3,
                Order = 1, Name = "Длительность импульса излучения", Unit = "мкс", IsRequired = false },

        // ---- Спектральные параметры
        new() { Form = form, Section = secWavelength, RowNumber = 4,
                Order = 1, Name = "Длина волны излучения", Unit = "мкм", IsRequired = false },

        // ---- Ток накачки
        new() { Form = form, Section = secCurrent, RowNumber = 5,
                Order = 1, Name = "Ток накачки излучателя (амплитуда импульсов тока накачки)", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secCurrent, RowNumber = 6,
                Order = 2, Name = "Частота повторения импульсов тока накачки", Unit = "Гц", IsRequired = false },

        // ---- Линейность ватт-амперной характеристики
        new() { Form = form, Section = secNonlinearity, RowNumber = 7,
                Order = 1, Name = "Линейность ватт-амперной характеристики", Unit = "отн. ед.", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 8,
                Order = 1, Name = "Температура окружающей среды", Unit = "°С", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm52(CatalogDbContext db)
        {
                // ---- Форма 52: Карта рабочих режимов полупроводниковых излучающих диодов ИК диапазона
                var form = new Form
                {
                        Number = "52",
                        Title = "Карта рабочих режимов полупроводниковых излучающих диодов ИК диапазона",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secCurrent = new FormSection { Form = form, Title = "Прямой ток", Order = 1 };
                var secReverse = new FormSection { Form = form, Title = "Обратное напряжение", Order = 2 };
                var secPulse = new FormSection { Form = form, Title = "Импульсные параметры", Order = 3 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 4 };

                form.Sections.AddRange(new[] { secCurrent, secReverse, secPulse, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Прямой ток
        new() { Form = form, Section = secCurrent, RowNumber = 1,
                Order = 1, Name = "Постоянный прямой ток", Unit = "мА", IsRequired = false },
        new() { Form = form, Section = secCurrent, RowNumber = 2,
                Order = 2, Name = "Импульсный прямой ток", Unit = "мА", IsRequired = false },
        new() { Form = form, Section = secCurrent, RowNumber = 3,
                Order = 3, Name = "Среднее значение импульсного прямого тока", Unit = "мА", IsRequired = false },

        // ---- Обратное напряжение
        new() { Form = form, Section = secReverse, RowNumber = 4,
                Order = 1, Name = "Максимальное обратное напряжение", Unit = "В", IsRequired = false },

        // ---- Импульсные параметры
        new() { Form = form, Section = secPulse, RowNumber = 5,
                Order = 1, Name = "Длительность импульса", Unit = "мкс", IsRequired = false },
        new() { Form = form, Section = secPulse, RowNumber = 6,
                Order = 2, Name = "Частота следования импульсов", Unit = "Гц", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 7,
                Order = 1, Name = "Температура окружающей среды (корпуса)", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 8,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm53(CatalogDbContext db)
        {
                // ---- Форма 53: Карта рабочих режимов полупроводниковых тетродов биполярных (дефензоров)
                var form = new Form
                {
                        Number = "53",
                        Title = "Карта рабочих режимов полупроводниковых тетродов биполярных (дефензоров)",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secAnodeCathode = new FormSection { Form = form, Title = "Анод-катод", Order = 1 };
                var secHold = new FormSection { Form = form, Title = "Электрод удержания", Order = 2 };
                var secStart = new FormSection { Form = form, Title = "Электрод запуска", Order = 3 };
                var secFreq = new FormSection { Form = form, Title = "Рабочая частота", Order = 4 };
                var secPower = new FormSection { Form = form, Title = "Рассеиваемая мощность", Order = 5 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 6 };

                form.Sections.AddRange(new[] { secAnodeCathode, secHold, secStart, secFreq, secPower, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Анод-катод
        new() { Form = form, Section = secAnodeCathode, RowNumber = 1,
                Order = 1, Name = "Напряжение анод-катод (постоянное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secAnodeCathode, RowNumber = 2,
                Order = 2, Name = "Напряжение анод-катод (импульсное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secAnodeCathode, RowNumber = 3,
                Order = 3, Name = "Ток анод-катод (постоянный)", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secAnodeCathode, RowNumber = 4,
                Order = 4, Name = "Ток анод-катод (импульсный)", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secAnodeCathode, RowNumber = 5,
                Order = 5, Name = "Длительность импульса анод-катод", Unit = "мкс", IsRequired = false },

        // ---- Электрод удержания
        new() { Form = form, Section = secHold, RowNumber = 6,
                Order = 1, Name = "Напряжение электрода удержания (постоянное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secHold, RowNumber = 7,
                Order = 2, Name = "Напряжение электрода удержания (импульсное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secHold, RowNumber = 8,
                Order = 3, Name = "Ток электрода удержания (постоянный)", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secHold, RowNumber = 9,
                Order = 4, Name = "Ток электрода удержания (импульсный)", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secHold, RowNumber = 10,
                Order = 5, Name = "Длительность импульса электрода удержания", Unit = "мкс", IsRequired = false },

        // ---- Электрод запуска
        new() { Form = form, Section = secStart, RowNumber = 11,
                Order = 1, Name = "Импульсное обратное напряжение электрода запуска", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secStart, RowNumber = 12,
                Order = 2, Name = "Импульсный ток электрода запуска", Unit = "А", IsRequired = false },

        // ---- Рабочая частота
        new() { Form = form, Section = secFreq, RowNumber = 13,
                Order = 1, Name = "Рабочая частота", Unit = "Гц", IsRequired = false },

        // ---- Рассеиваемая мощность
        new() { Form = form, Section = secPower, RowNumber = 14,
                Order = 1, Name = "Рассеиваемая мощность", Unit = "Вт", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 15,
                Order = 1, Name = "Температура окружающей среды (корпуса)", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 16,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm54(CatalogDbContext db)
        {
                // ---- Форма 54: Карта рабочих режимов полупроводниковых ограничителей напряжения
                var form = new Form
                {
                        Number = "54",
                        Title = "Карта рабочих режимов полупроводниковых ограничителей напряжения",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secNormalReverse = new FormSection { Form = form, Title = "Режим при отсутствии импульсов перегрузки (обратное напряжение)", Order = 1 };
                var secNormalForward = new FormSection { Form = form, Title = "Режим при отсутствии импульсов перегрузки (прямое напряжение)", Order = 2 };
                var secOverload = new FormSection { Form = form, Title = "Режим во время воздействия импульсов перегрузки", Order = 3 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 4 };

                form.Sections.AddRange(new[] { secNormalReverse, secNormalForward, secOverload, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Режим при отсутствии импульсов перегрузки (обратное напряжение)
        new() { Form = form, Section = secNormalReverse, RowNumber = 1,
                Order = 1, Name = "Максимальное постоянное/импульсное (амплитудное) обратное напряжение", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secNormalReverse, RowNumber = 2,
                Order = 2, Name = "Максимальный постоянный обратный ток (в режиме пробоя)", Unit = "мА", IsRequired = false },

        // ---- Режим при отсутствии импульсов перегрузки (прямое напряжение)
        new() { Form = form, Section = secNormalForward, RowNumber = 3,
                Order = 1, Name = "Максимальное постоянное/импульсное (амплитудное) прямое напряжение", Unit = "В", IsRequired = false },

        // ---- Режим во время воздействия импульсов перегрузки
        new() { Form = form, Section = secOverload, RowNumber = 4,
                Order = 1, Name = "Максимальный импульсный ток ограничения", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secOverload, RowNumber = 5,
                Order = 2, Name = "Длительность импульсов перегрузки", Unit = "мкс", IsRequired = false },
        new() { Form = form, Section = secOverload, RowNumber = 6,
                Order = 3, Name = "Скважность импульсов перегрузки", Unit = "", IsRequired = false },
        new() { Form = form, Section = secOverload, RowNumber = 7,
                Order = 4, Name = "Число импульсов перегрузки", Unit = "", IsRequired = false },
        new() { Form = form, Section = secOverload, RowNumber = 8,
                Order = 5, Name = "Постоянная (средняя) рассеиваемая мощность", Unit = "Вт", IsRequired = false },
        new() { Form = form, Section = secOverload, RowNumber = 9,
                Order = 6, Name = "Максимальная импульсная (повторяющаяся/неповторяющаяся) рассеиваемая мощность", Unit = "Вт", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 10,
                Order = 1, Name = "Температура окружающей среды", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 11,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm55(CatalogDbContext db)
        {
                // ---- Форма 55: Карта рабочих режимов диодов (выпрямительных, импульсных, универсальных), варикапов и диодных сборок
                var form = new Form
                {
                        Number = "55",
                        Title = "Карта рабочих режимов диодов (выпрямительных, импульсных, универсальных), варикапов и диодных сборок",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secRectifier = new FormSection { Form = form, Title = "Выпрямительный режим", Order = 1 };
                var secOverload = new FormSection { Form = form, Title = "Перегрузка", Order = 2 };
                var secPulse = new FormSection { Form = form, Title = "Импульсный режим", Order = 3 };
                var secReverse = new FormSection { Form = form, Title = "Обратное напряжение", Order = 4 };
                var secFreq = new FormSection { Form = form, Title = "Частота", Order = 5 };
                var secPower = new FormSection { Form = form, Title = "Рассеиваемая мощность", Order = 6 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 7 };

                form.Sections.AddRange(new[] { secRectifier, secOverload, secPulse, secReverse, secFreq, secPower, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Выпрямительный режим
        new() { Form = form, Section = secRectifier, RowNumber = 1,
                Order = 1, Name = "Постоянный или средний выпрямленный ток", Unit = "мА", IsRequired = false },

        // ---- Перегрузка
        new() { Form = form, Section = secOverload, RowNumber = 2,
                Order = 1, Name = "Максимальный импульс тока при включении", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secOverload, RowNumber = 3,
                Order = 2, Name = "Длительность режима перегрузки", Unit = "мс", IsRequired = false },

        // ---- Импульсный режим
        new() { Form = form, Section = secPulse, RowNumber = 4,
                Order = 1, Name = "Максимальный импульсный прямой ток", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secPulse, RowNumber = 5,
                Order = 2, Name = "Длительность импульса", Unit = "мкс", IsRequired = false },

        // ---- Обратное напряжение
        new() { Form = form, Section = secReverse, RowNumber = 6,
                Order = 1, Name = "Максимальное обратное напряжение", Unit = "В", IsRequired = false },

        // ---- Частота
        new() { Form = form, Section = secFreq, RowNumber = 7,
                Order = 1, Name = "Частота выпрямленного тока (частота следования импульсов)", Unit = "Гц", IsRequired = false },

        // ---- Рассеиваемая мощность
        new() { Form = form, Section = secPower, RowNumber = 8,
                Order = 1, Name = "Рассеиваемая мощность", Unit = "мВт", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 9,
                Order = 1, Name = "Температура окружающей среды (корпуса)", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 10,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm56(CatalogDbContext db)
        {
                // ---- Форма 56: Карта рабочих режимов полупроводниковых стабилитронов и стабисторов
                var form = new Form
                {
                        Number = "56",
                        Title = "Карта рабочих режимов полупроводниковых стабилитронов и стабисторов",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secCurrent = new FormSection { Form = form, Title = "Ток стабилизации", Order = 1 };
                var secPulse = new FormSection { Form = form, Title = "Импульсные параметры", Order = 2 };
                var secForward = new FormSection { Form = form, Title = "Прямой ток", Order = 3 };
                var secReverse = new FormSection { Form = form, Title = "Обратное напряжение", Order = 4 };
                var secPower = new FormSection { Form = form, Title = "Рассеиваемая мощность", Order = 5 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 6 };

                form.Sections.AddRange(new[] { secCurrent, secPulse, secForward, secReverse, secPower, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Ток стабилизации
        new() { Form = form, Section = secCurrent, RowNumber = 1,
                Order = 1, Name = "Ток стабилизации (минимальный постоянный)", Unit = "мА", IsRequired = false },
        new() { Form = form, Section = secCurrent, RowNumber = 2,
                Order = 2, Name = "Ток стабилизации (максимальный постоянный)", Unit = "мА", IsRequired = false },
        new() { Form = form, Section = secCurrent, RowNumber = 3,
                Order = 3, Name = "Ток стабилизации (импульсный)", Unit = "мА", IsRequired = false },

        // ---- Импульсные параметры
        new() { Form = form, Section = secPulse, RowNumber = 4,
                Order = 1, Name = "Длительность импульса тока стабилизации", Unit = "мкс", IsRequired = false },
        new() { Form = form, Section = secPulse, RowNumber = 5,
                Order = 2, Name = "Частота следования импульсов", Unit = "Гц", IsRequired = false },

        // ---- Прямой ток
        new() { Form = form, Section = secForward, RowNumber = 6,
                Order = 1, Name = "Прямой постоянный или средний ток", Unit = "мА", IsRequired = false },
        new() { Form = form, Section = secForward, RowNumber = 7,
                Order = 2, Name = "Импульсный прямой ток", Unit = "мА", IsRequired = false },

        // ---- Обратное напряжение
        new() { Form = form, Section = secReverse, RowNumber = 8,
                Order = 1, Name = "Обратное напряжение", Unit = "В", IsRequired = false },

        // ---- Рассеиваемая мощность
        new() { Form = form, Section = secPower, RowNumber = 9,
                Order = 1, Name = "Рассеиваемая мощность", Unit = "мВт", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 10,
                Order = 1, Name = "Температура окружающей среды (корпуса)", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 11,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm57(CatalogDbContext db)
        {
                // ---- Форма 57: Карта рабочих режимов туннельных и обращенных диодов
                var form = new Form
                {
                        Number = "57",
                        Title = "Карта рабочих режимов туннельных и обращенных диодов",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secForward = new FormSection { Form = form, Title = "Прямой ток", Order = 1 };
                var secReverse = new FormSection { Form = form, Title = "Обратный ток", Order = 2 };
                var secVoltage = new FormSection { Form = form, Title = "Прямое напряжение", Order = 3 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 4 };

                form.Sections.AddRange(new[] { secForward, secReverse, secVoltage, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Прямой ток
        new() { Form = form, Section = secForward, RowNumber = 1,
                Order = 1, Name = "Постоянный прямой ток", Unit = "мА", IsRequired = false },
        new() { Form = form, Section = secForward, RowNumber = 2,
                Order = 2, Name = "Импульсный прямой ток", Unit = "мА", IsRequired = false },

        // ---- Обратный ток
        new() { Form = form, Section = secReverse, RowNumber = 3,
                Order = 1, Name = "Постоянный обратный ток", Unit = "мА", IsRequired = false },
        new() { Form = form, Section = secReverse, RowNumber = 4,
                Order = 2, Name = "Импульсный обратный ток", Unit = "мА", IsRequired = false },

        // ---- Прямое напряжение
        new() { Form = form, Section = secVoltage, RowNumber = 5,
                Order = 1, Name = "Прямое напряжение (пиковое)", Unit = "В", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 6,
                Order = 1, Name = "Температура окружающей среды (корпуса)", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 7,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        // TODO:Form58

        private static Form AddForm59(CatalogDbContext db)
        {
                // ---- Форма 59: Карта рабочих режимов однопереходных транзисторов
                var form = new Form
                {
                        Number = "59",
                        Title = "Карта рабочих режимов однопереходных транзисторов",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secBase = new FormSection { Form = form, Title = "Межбазовые параметры", Order = 1 };
                var secEmitter = new FormSection { Form = form, Title = "Параметры эмиттера", Order = 2 };
                var secPulse = new FormSection { Form = form, Title = "Импульсные параметры", Order = 3 };
                var secPower = new FormSection { Form = form, Title = "Рассеиваемая мощность", Order = 4 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 5 };

                form.Sections.AddRange(new[] { secBase, secEmitter, secPulse, secPower, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Межбазовые параметры
        new() { Form = form, Section = secBase, RowNumber = 1,
                Order = 1, Name = "Межбазовое напряжение", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secBase, RowNumber = 2,
                Order = 2, Name = "Обратное напряжение между эмиттером и базой-2", Unit = "В", IsRequired = false },

        // ---- Параметры эмиттера
        new() { Form = form, Section = secEmitter, RowNumber = 3,
                Order = 1, Name = "Постоянный ток эмиттера в открытом состоянии", Unit = "мА", IsRequired = false },
        new() { Form = form, Section = secEmitter, RowNumber = 4,
                Order = 2, Name = "Импульсный ток эмиттера в открытом состоянии", Unit = "мА", IsRequired = false },
        new() { Form = form, Section = secEmitter, RowNumber = 5,
                Order = 3, Name = "Импульсный ток эмиттера", Unit = "мА", IsRequired = false },

        // ---- Импульсные параметры
        new() { Form = form, Section = secPulse, RowNumber = 6,
                Order = 1, Name = "Длительность импульса", Unit = "мкс", IsRequired = false },

        // ---- Рассеиваемая мощность
        new() { Form = form, Section = secPower, RowNumber = 7,
                Order = 1, Name = "Рассеиваемая мощность", Unit = "мВт", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 8,
                Order = 1, Name = "Температура окружающей среды (корпуса)", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 9,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }

        //TODO:Form60

        private static Form AddForm61(CatalogDbContext db)
        {
                // ---- Форма 61: Карта рабочих режимов полупроводниковых транзисторных усилителей
                var form = new Form
                {
                        Number = "61",
                        Title = "Карта рабочих режимов полупроводниковых транзисторных усилителей",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secSupply = new FormSection { Form = form, Title = "Напряжение источника питания", Order = 1 };
                var secInput = new FormSection { Form = form, Title = "Вход усилителя", Order = 2 };
                var secOutput = new FormSection { Form = form, Title = "Выход усилителя", Order = 3 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 4 };

                form.Sections.AddRange(new[] { secSupply, secInput, secOutput, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Напряжение источника питания
        new() { Form = form, Section = secSupply, RowNumber = 1,
                Order = 1, Name = "Напряжение источника питания (минимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secSupply, RowNumber = 2,
                Order = 2, Name = "Напряжение источника питания (максимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secSupply, RowNumber = 3,
                Order = 3, Name = "Пульсация напряжения источника питания", Unit = "%", IsRequired = false },

        // ---- Вход усилителя
        new() { Form = form, Section = secInput, RowNumber = 4,
                Order = 1, Name = "Уровень мощности, просачивающейся на вход", Unit = "Вт", IsRequired = false },
        new() { Form = form, Section = secInput, RowNumber = 5,
                Order = 2, Name = "КСВН входа", Unit = "", IsRequired = false },

        // ---- Выход усилителя
        new() { Form = form, Section = secOutput, RowNumber = 6,
                Order = 1, Name = "КСВН выхода", Unit = "", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 7,
                Order = 1, Name = "Температура окружающей среды (корпуса)", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 8,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }

        //TODO:62


        //TODO: Проверить WithPins
        private static Form AddForm63(CatalogDbContext db)
        {
                // ---- Форма 63: Карта рабочих режимов аналоговых функциональных узлов (модулей, микромодулей, микросхем)
                var form = new Form
                {
                        Number = "63",
                        Title = "Карта рабочих режимов аналоговых функциональных узлов (модулей, микромодулей, микросхем)",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secSupply = new FormSection { Form = form, Title = "Цепи питания", Order = 1 };
                var secInput = new FormSection { Form = form, Title = "Входные цепи", Order = 2 };
                var secOutput = new FormSection { Form = form, Title = "Выходные цепи", Order = 3 };
                var secPower = new FormSection { Form = form, Title = "Мощность рассеивания", Order = 4 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 5 };
                var secLoad = new FormSection { Form = form, Title = "Нагрузка", Order = 6 };

                form.Sections.AddRange(new[] { secSupply, secInput, secOutput, secPower, secEnv, secLoad });

                var parameters = new List<FormParameter>
    {
        // ---- Цепи питания
        new() { Form = form, Section = secSupply, RowNumber = 1,
                Order = 1, Name = "Напряжение питания", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secSupply, RowNumber = 2,
                Order = 2, Name = "Порядок подачи напряжения питания и входных сигналов", Unit = "", IsRequired = false },

        // ---- Входные цепи
        new() { Form = form, Section = secInput, RowNumber = 3,
                Order = 1, Name = "Входное (дифференциальное) напряжение", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secInput, RowNumber = 4,
                Order = 2, Name = "Входное синфазное напряжение", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secInput, RowNumber = 5,
                Order = 3, Name = "Входное напряжение", Unit = "В", IsRequired = false },

        // ---- Выходные цепи
        new() { Form = form, Section = secOutput, RowNumber = 6,
                Order = 1, Name = "Сопротивление нагрузки", Unit = "кОм", IsRequired = false },
        new() { Form = form, Section = secOutput, RowNumber = 7,
                Order = 2, Name = "Емкость нагрузки", Unit = "пФ", IsRequired = false },
        new() { Form = form, Section = secOutput, RowNumber = 8,
                Order = 3, Name = "Выходной ток", Unit = "мА", IsRequired = false },

        // ---- Мощность рассеивания
        new() { Form = form, Section = secPower, RowNumber = 9,
                Order = 1, Name = "Мощность рассеивания", Unit = "мВт", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 10,
                Order = 1, Name = "Температура окружающей среды (корпуса)", Unit = "°С", IsRequired = false },

        // ---- Нагрузка (позиционное обозначение выводов)
        new() { Form = form, Section = secLoad, RowNumber = 11,
                Order = 1, Name = "Позиционное обозначение, тип и номера входных выводов схемных нагрузок", Unit = "", IsRequired = false },

        // ---- Коэффициент нагрузки
        new() { Form = form, Section = secLoad, RowNumber = 12,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем столбцы с учётом указания номеров выводов (№ выводов)
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm64(CatalogDbContext db)
        {
                // ---- Форма 64: Карта рабочих режимов цифровых функциональных узлов (модулей, микромодулей, микросхем)
                var form = new Form
                {
                        Number = "64",
                        Title = "Карта рабочих режимов цифровых функциональных узлов (модулей, микромодулей, микросхем)",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secSupply = new FormSection { Form = form, Title = "Цепи питания", Order = 1 };
                var secInput = new FormSection { Form = form, Title = "Входные цепи", Order = 2 };
                var secOutput = new FormSection { Form = form, Title = "Выходные цепи", Order = 3 };
                var secPower = new FormSection { Form = form, Title = "Мощность рассеивания", Order = 4 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 5 };
                var secLoad = new FormSection { Form = form, Title = "Нагрузка", Order = 6 };

                form.Sections.AddRange(new[] { secSupply, secInput, secOutput, secPower, secEnv, secLoad });

                var parameters = new List<FormParameter>
    {
        // ---- Цепи питания
        new() { Form = form, Section = secSupply, RowNumber = 1,
                Order = 1, Name = "Напряжение питания", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secSupply, RowNumber = 2,
                Order = 2, Name = "Порядок подачи напряжения питания и входных сигналов", Unit = "", IsRequired = false },

        // ---- Входные цепи (логические уровни и временные параметры)
        new() { Form = form, Section = secInput, RowNumber = 3,
                Order = 1, Name = "Напряжение низкого уровня (вход)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secInput, RowNumber = 4,
                Order = 2, Name = "Напряжение высокого уровня (вход)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secInput, RowNumber = 5,
                Order = 3, Name = "Длительность импульса", Unit = "нс", IsRequired = false },
        new() { Form = form, Section = secInput, RowNumber = 6,
                Order = 4, Name = "Время перехода при включении (время нарастания)", Unit = "нс", IsRequired = false },
        new() { Form = form, Section = secInput, RowNumber = 7,
                Order = 5, Name = "Время перехода при выключении (время спада)", Unit = "нс", IsRequired = false },
        new() { Form = form, Section = secInput, RowNumber = 8,
                Order = 6, Name = "Максимальная рабочая частота", Unit = "МГц", IsRequired = false },
        new() { Form = form, Section = secInput, RowNumber = 9,
                Order = 7, Name = "Время t1 (время задержки распространения)", Unit = "нс", IsRequired = false },
        new() { Form = form, Section = secInput, RowNumber = 10,
                Order = 8, Name = "Время t2 (время задержки распространения)", Unit = "нс", IsRequired = false },

        // ---- Выходные цепи
        new() { Form = form, Section = secOutput, RowNumber = 11,
                Order = 1, Name = "Выходной ток низкого уровня", Unit = "мА", IsRequired = false },
        new() { Form = form, Section = secOutput, RowNumber = 12,
                Order = 2, Name = "Выходной ток высокого уровня", Unit = "мА", IsRequired = false },
        new() { Form = form, Section = secOutput, RowNumber = 13,
                Order = 3, Name = "Емкость нагрузки", Unit = "пФ", IsRequired = false },

        // ---- Мощность рассеивания
        new() { Form = form, Section = secPower, RowNumber = 14,
                Order = 1, Name = "Мощность рассеивания", Unit = "мВт", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 15,
                Order = 1, Name = "Температура окружающей среды (корпуса)", Unit = "°С", IsRequired = false },

        // ---- Нагрузка
        new() { Form = form, Section = secLoad, RowNumber = 16,
                Order = 1, Name = "Позиционное обозначение, тип и номера входных выводов схемных нагрузок", Unit = "", IsRequired = false },
        new() { Form = form, Section = secLoad, RowNumber = 17,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },

        // ---- Опциональный параметр: второе напряжение питания (Form 64).
        // RowNumber = 18 — следующий свободный номер строки в этой форме.
        // IsOptional = true помечает его как "не основная строка таблицы" —
        // UI рисует его не как обычную строку, а как кнопку
        // "+ Добавить второе напряжение" рядом с RowNumber = 1, и физически
        // вставляет клонированную строку в Word-шаблон только когда у
        // конкретного компонента реально заполнено значение для этого
        // параметра (см. WordService.FillOptionalRows).
        // OptionalForRowNumber = 1 связывает его с основным параметром
        // "Напряжение питания".
        new() { Form = form, Section = secSupply, RowNumber = 18,
                Order = 3, Name = "Напряжение питания (второе)", Unit = "В",
                IsRequired = false, IsOptional = true, OptionalForRowNumber = 1 },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем столбцы с учётом номеров выводов (№ выводов)
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }

        private static Form AddForm65(CatalogDbContext db)
        {
                // ---- Форма 65: Карта рабочих режимов функциональных узлов (модулей, микромодулей, микросхем)
                var form = new Form
                {
                        Number = "65",
                        Title = "Карта рабочих режимов функциональных узлов (модулей, микромодулей, микросхем)",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secSupply = new FormSection { Form = form, Title = "Цепи питания", Order = 1 };
                var secInput = new FormSection { Form = form, Title = "Входные цепи", Order = 2 };
                var secOutput = new FormSection { Form = form, Title = "Выходные цепи", Order = 3 };
                var secPower = new FormSection { Form = form, Title = "Мощность рассеивания", Order = 4 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 5 };
                var secLoad = new FormSection { Form = form, Title = "Нагрузка", Order = 6 };
                var secNote = new FormSection { Form = form, Title = "Примечание", Order = 7 };

                form.Sections.AddRange(new[] { secSupply, secInput, secOutput, secPower, secEnv, secLoad, secNote });

                var parameters = new List<FormParameter>
    {
        // ---- Цепи питания
        new() { Form = form, Section = secSupply, RowNumber = 1,
                Order = 1, Name = "Напряжение питания", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secSupply, RowNumber = 2,
                Order = 2, Name = "Порядок подачи напряжения питания и входных сигналов", Unit = "", IsRequired = false },

        // ---- Входные цепи
        new() { Form = form, Section = secInput, RowNumber = 3,
                Order = 1, Name = "Напряжение низкого уровня (вход)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secInput, RowNumber = 4,
                Order = 2, Name = "Напряжение высокого уровня (вход)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secInput, RowNumber = 5,
                Order = 3, Name = "Длительность импульса", Unit = "нс", IsRequired = false },
        new() { Form = form, Section = secInput, RowNumber = 6,
                Order = 4, Name = "Время перехода при включении (время нарастания)", Unit = "нс", IsRequired = false },
        new() { Form = form, Section = secInput, RowNumber = 7,
                Order = 5, Name = "Время перехода при выключении (время спада)", Unit = "нс", IsRequired = false },
        new() { Form = form, Section = secInput, RowNumber = 8,
                Order = 6, Name = "Максимальная рабочая частота", Unit = "МГц", IsRequired = false },
        new() { Form = form, Section = secInput, RowNumber = 9,
                Order = 7, Name = "Время t1 (задержка распространения)", Unit = "нс", IsRequired = false },
        new() { Form = form, Section = secInput, RowNumber = 10,
                Order = 8, Name = "Время t2 (задержка распространения)", Unit = "нс", IsRequired = false },

        // ---- Выходные цепи
        new() { Form = form, Section = secOutput, RowNumber = 11,
                Order = 1, Name = "Выходной ток низкого уровня", Unit = "мА", IsRequired = false },
        new() { Form = form, Section = secOutput, RowNumber = 12,
                Order = 2, Name = "Выходной ток высокого уровня", Unit = "мА", IsRequired = false },
        new() { Form = form, Section = secOutput, RowNumber = 13,
                Order = 3, Name = "Емкость нагрузки", Unit = "пФ", IsRequired = false },

        // ---- Мощность рассеивания
        new() { Form = form, Section = secPower, RowNumber = 14,
                Order = 1, Name = "Мощность рассеивания", Unit = "мВт", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 15,
                Order = 1, Name = "Температура окружающей среды (корпуса)", Unit = "°С", IsRequired = false },

        // ---- Нагрузка
        new() { Form = form, Section = secLoad, RowNumber = 16,
                Order = 1, Name = "Позиционное обозначение, тип и номера входных выводов схемных нагрузок", Unit = "", IsRequired = false },
        new() { Form = form, Section = secLoad, RowNumber = 17,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },

        // ---- Примечание
        new() { Form = form, Section = secNote, RowNumber = 18,
                Order = 1, Name = "Примечание", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm65A(CatalogDbContext db)
        {
                // ---- Форма 65А: Карта рабочих режимов функциональных узлов (модулей, микромодулей, микросхем) по временным параметрам
                var form = new Form
                {
                        Number = "65А",
                        Title = "Карта рабочих режимов функциональных узлов (модулей, микромодулей, микросхем) по временным параметрам",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secPinInfo = new FormSection { Form = form, Title = "Информация о выводе", Order = 1 };
                var secSignal = new FormSection { Form = form, Title = "Параметры сигнала", Order = 2 };
                var secTiming = new FormSection { Form = form, Title = "Временные параметры", Order = 3 };

                form.Sections.AddRange(new[] { secPinInfo, secSignal, secTiming });

                var parameters = new List<FormParameter>
    {
        // ---- Информация о выводе
        new() { Form = form, Section = secPinInfo, RowNumber = 1,
                Order = 1, Name = "Номер вывода", Unit = "", IsRequired = false },
        new() { Form = form, Section = secPinInfo, RowNumber = 2,
                Order = 2, Name = "Обозначение вывода", Unit = "", IsRequired = false },

        // ---- Параметры сигнала
        new() { Form = form, Section = secSignal, RowNumber = 3,
                Order = 1, Name = "Длительность сигнала", Unit = "нс", IsRequired = false },
        new() { Form = form, Section = secSignal, RowNumber = 4,
                Order = 2, Name = "Фронт сигнала (время нарастания)", Unit = "нс", IsRequired = false },
        new() { Form = form, Section = secSignal, RowNumber = 5,
                Order = 3, Name = "Спад сигнала (время спада)", Unit = "нс", IsRequired = false },

        // ---- Временные параметры измеряемого сигнала относительно фронтов сигналов
        new() { Form = form, Section = secTiming, RowNumber = 6,
                Order = 1, Name = "Временной параметр 1 (измеряемый сигнал относительно фронтов)", Unit = "нс", IsRequired = false },
        new() { Form = form, Section = secTiming, RowNumber = 7,
                Order = 2, Name = "Временной параметр 2", Unit = "нс", IsRequired = false },
        new() { Form = form, Section = secTiming, RowNumber = 8,
                Order = 3, Name = "Временной параметр 3", Unit = "нс", IsRequired = false },
        new() { Form = form, Section = secTiming, RowNumber = 9,
                Order = 4, Name = "Временной параметр 4", Unit = "нс", IsRequired = false },
        new() { Form = form, Section = secTiming, RowNumber = 10,
                Order = 5, Name = "Временной параметр 5", Unit = "нс", IsRequired = false },
        new() { Form = form, Section = secTiming, RowNumber = 11,
                Order = 6, Name = "Временной параметр 6", Unit = "нс", IsRequired = false },
        new() { Form = form, Section = secTiming, RowNumber = 12,
                Order = 7, Name = "Временной параметр 7", Unit = "нс", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm66(CatalogDbContext db)
        {
                // ---- Форма 66: Карта рабочих режимов цифровых функциональных узлов (модулей, микромодулей, микросхем)
                // Пример заполнения для БИС АЦП
                var form = new Form
                {
                        Number = "66",
                        Title = "Карта рабочих режимов цифровых функциональных узлов (модулей, микромодулей, микросхем). Пример заполнения для БИС АЦП",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secSupply = new FormSection { Form = form, Title = "Цепи питания", Order = 1 };
                var secInput = new FormSection { Form = form, Title = "Входные цепи", Order = 2 };
                var secOutput = new FormSection { Form = form, Title = "Выходные цепи", Order = 3 };
                var secPower = new FormSection { Form = form, Title = "Мощность рассеивания", Order = 4 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 5 };
                var secLoad = new FormSection { Form = form, Title = "Нагрузка", Order = 6 };

                form.Sections.AddRange(new[] { secSupply, secInput, secOutput, secPower, secEnv, secLoad });

                var parameters = new List<FormParameter>
    {
        // ---- Цепи питания
        new() { Form = form, Section = secSupply, RowNumber = 1,
                Order = 1, Name = "Напряжение питания", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secSupply, RowNumber = 2,
                Order = 2, Name = "Порядок подачи напряжения питания и входных сигналов", Unit = "", IsRequired = false },

        // ---- Входные цепи (аналоговые входы АЦП)
        new() { Form = form, Section = secInput, RowNumber = 3,
                Order = 1, Name = "Входное (дифференциальное) напряжение", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secInput, RowNumber = 4,
                Order = 2, Name = "Входное синфазное напряжение", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secInput, RowNumber = 5,
                Order = 3, Name = "Входное напряжение", Unit = "В", IsRequired = false },

        // ---- Выходные цепи (цифровые выходы АЦП)
        new() { Form = form, Section = secOutput, RowNumber = 6,
                Order = 1, Name = "Сопротивление нагрузки", Unit = "кОм", IsRequired = false },
        new() { Form = form, Section = secOutput, RowNumber = 7,
                Order = 2, Name = "Емкость нагрузки", Unit = "пФ", IsRequired = false },
        new() { Form = form, Section = secOutput, RowNumber = 8,
                Order = 3, Name = "Выходной ток", Unit = "мА", IsRequired = false },

        // ---- Мощность рассеивания
        new() { Form = form, Section = secPower, RowNumber = 9,
                Order = 1, Name = "Мощность рассеивания", Unit = "мВт", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 10,
                Order = 1, Name = "Температура окружающей среды (корпуса)", Unit = "°С", IsRequired = false },

        // ---- Нагрузка
        new() { Form = form, Section = secLoad, RowNumber = 11,
                Order = 1, Name = "Позиционное обозначение, тип и номера входных выводов схемных нагрузок", Unit = "", IsRequired = false },
        new() { Form = form, Section = secLoad, RowNumber = 12,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm67(CatalogDbContext db)
        {
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
    new() { Form = form67, Section = sec67V, RowNumber =  1, Order = 1,
            Name = "Постоянное",                      Unit = "В",
            IsRequired = true },
    new() { Form = form67, Section = sec67V, RowNumber =  2, Order = 2,
            Name = "Переменное (амплитудное)",         Unit = "В" },
    new() { Form = form67, Section = sec67V, RowNumber =  3, Order = 3,
            Name = "Импульсное",                      Unit = "В" },
    new() { Form = form67, Section = sec67V, RowNumber =  4, Order = 4,
            Name = "Суммарное",                       Unit = "В",
            Formula = "row1+row2+row3",
            IsRequired = true },
    new() { Form = form67, Section = sec67I, RowNumber =  5, Order = 1,
            Name = "Переменный",                      Unit = "А" },
    new() { Form = form67, Section = sec67I, RowNumber =  6, Order = 2,
            Name = "Проходной",                       Unit = "А" },
    new() { Form = form67, Section = sec67I, RowNumber =  7, Order = 3,
            Name = "Разрядный",                       Unit = "А" },
    new() { Form = form67, Section = sec67O, RowNumber =  8, Order = 1,
            Name = "Длительность зарядки (не менее)", Unit = "с" },
    new() { Form = form67, Section = sec67O, RowNumber =  9, Order = 2,
            Name = "Реактивная мощность",             Unit = "Вар" },
    new() { Form = form67, Section = sec67O, RowNumber = 10, Order = 3,
            Name = "Частота максимальная",             Unit = "Гц" },
    new() { Form = form67, Section = sec67O, RowNumber = 11, Order = 4,
            Name = "Длительность импульса",           Unit = "мкс" },
    new() { Form = form67, Section = sec67O, RowNumber = 12, Order = 5,
            Name = "Температура окружающей среды",    Unit = "°С",
            IsRequired = true },
    new() { Form = form67, Section = sec67O, RowNumber = 13, Order = 6,
            Name = "Температура перегрева",           Unit = "°С" },
    new() { Form = form67, Section = sec67O, RowNumber = 14, Order = 7,
            Name = "Коэффициент нагрузки",            Unit = null,
            IsRequired = true },


        };
                form67.Parameters.AddRange(params67);
                form67.ValueColumns.AddRange(new[]
                {
            new FormValueColumn { Form = form67, Key = "scheme", Title = "В схеме", Source = ColumnSource.ManualScheme, Order = 1 },
            new FormValueColumn { Form = form67, Key = "ntd",    Title = "По НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
        });
                db.Forms.Add(form67);
                return (form67);
        }
        private static Form AddForm68(CatalogDbContext db)
        {
                // ---- 2. Форма 68
                var form68 = new Form
                {
                        Number = "68",
                        Title = "Карта рабочих режимов резисторов, резисторных сборок, терморезисторов, поглотителей и потенциометров",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                var sec68V = new FormSection { Form = form68, Title = "Напряжение, В", Order = 1 };
                var sec68I = new FormSection { Form = form68, Title = "Импульсный режим", Order = 2 };
                var sec68O = new FormSection { Form = form68, Title = "Прочие параметры", Order = 3 };
                form68.Sections.AddRange(new[] { sec68V, sec68I, sec68O });

                var param68 = new List<FormParameter>
    {
      new() {Form = form68, Section = sec68V, RowNumber = 1,
             Order = 1, Name = "Постоянное напряжение", Unit = "В", IsRequired = false},
      new() {Form = form68, Section = sec68V, RowNumber = 2,
             Order = 2, Name = "Переменное (амплитудное) напряжение", Unit = "В", IsRequired = false},
      new() {Form = form68, Section = sec68V, RowNumber = 3,
             Order = 3, Name = "Импульсное напряжение", Unit = "В", IsRequired = false},
      new() {Form = form68, Section = sec68V, RowNumber = 4,
             Order = 4, Name = "Суммарное напряжение", Unit = "В", Formula = "row1+row2+row3",
             IsRequired = true},
      new() {Form = form68, Section = sec68I, RowNumber = 5,
             Order = 1, Name = "Импульсный режим. Частота", Unit = "Гц", IsRequired = false},
      new() {Form = form68, Section = sec68I, RowNumber = 6,
             Order = 2, Name = "Импульсный режим. Длительность импульса", Unit = "мкс", IsRequired = false},
      new() {Form = form68, Section = sec68I, RowNumber = 7,
             Order = 3, Name = "Импульсная мощность", Unit = "Вт", IsRequired = false},
      new() {Form = form68, Section = sec68I, RowNumber = 8,
             Order = 4, Name = "Средняя мощность", Unit = "Вт", IsRequired = false},
      new() {Form = form68, Section = sec68I, RowNumber = 9,
             Order = 5, Name = "Импульсный режим. Коэффициент нагрузки", Unit = "мкс", IsRequired = false},
      new() {Form = form68, Section = sec68O, RowNumber = 10,
             Order = 1, Name = "Ток через подвижный контакт переменного резистора", Unit = "мА", IsRequired = false},
      new() {Form = form68, Section = sec68O, RowNumber = 11,
             Order = 2, Name = "Температура окружающей среды", Unit = "°С", IsRequired = false},
      new() {Form = form68, Section = sec68O, RowNumber = 12,
             Order = 3, Name = "Температура перегрева", Unit = "°С", IsRequired = false},
      new() {Form = form68, Section = sec68O, RowNumber = 13,
             Order = 4, Name = "Суммарная мощность", Unit = "Вт", IsRequired = true},
      new() {Form = form68, Section = sec68O, RowNumber = 14,
             Order = 5, Name = "Температура окружающей среды (Корпуса)", Unit = "°С", IsRequired = true},
      new() {Form = form68, Section = sec68O, RowNumber = 15,
             Order = 6, Name = "Коэффициент нагрузки", Unit = null, IsRequired = true},
    };

                form68.Parameters.AddRange(param68);
                form68.ValueColumns.AddRange(new[]
                {
            new FormValueColumn { Form = form68, Key = "scheme", Title = "В схеме", Source = ColumnSource.ManualScheme, Order = 1 },
            new FormValueColumn { Form = form68, Key = "ntd",    Title = "По НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
        });
                db.Forms.Add(form68);
                return (form68);
        }
        private static Form AddForm69(CatalogDbContext db)
        {
                // ---- Форма 69: Карта рабочих режимов кварцевых резонаторов, кварцевых микрогенераторов,
                // пьезоэлектрических и электромеханических фильтров и линий задержки на поверхностных акустических волнах
                var form = new Form
                {
                        Number = "69",
                        Title = "Карта рабочих режимов кварцевых резонаторов, кварцевых микрогенераторов, пьезоэлектрических и электромеханических фильтров и линий задержки на поверхностных акустических волнах",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secPower = new FormSection { Form = form, Title = "Мощность резонатора", Order = 1 };
                var secLoadCap = new FormSection { Form = form, Title = "Нагрузочная ёмкость", Order = 2 };
                var secResonance = new FormSection { Form = form, Title = "Резонанс", Order = 3 };
                var secSupply = new FormSection { Form = form, Title = "Питание", Order = 4 };
                var secInput = new FormSection { Form = form, Title = "Вход", Order = 5 };
                var secInputLoad = new FormSection { Form = form, Title = "Нагрузка на входе", Order = 6 };
                var secOutputLoad = new FormSection { Form = form, Title = "Нагрузка на выходе", Order = 7 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 8 };

                form.Sections.AddRange(new[] { secPower, secLoadCap, secResonance, secSupply, secInput, secInputLoad, secOutputLoad, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Мощность, рассеиваемая на резонаторе
        new() { Form = form, Section = secPower, RowNumber = 1,
                Order = 1, Name = "Мощность, рассеиваемая на резонаторе", Unit = "мВт", IsRequired = false },

        // ---- Нагрузочная ёмкость
        new() { Form = form, Section = secLoadCap, RowNumber = 2,
                Order = 1, Name = "Нагрузочная ёмкость", Unit = "пФ", IsRequired = false },

        // ---- Резонанс (частота)
        new() { Form = form, Section = secResonance, RowNumber = 3,
                Order = 1, Name = "Частота параллельного резонанса", Unit = "МГц", IsRequired = false },
        new() { Form = form, Section = secResonance, RowNumber = 4,
                Order = 2, Name = "Частота последовательного резонанса", Unit = "МГц", IsRequired = false },

        // ---- Питание
        new() { Form = form, Section = secSupply, RowNumber = 5,
                Order = 1, Name = "Напряжение питания", Unit = "В", IsRequired = false },

        // ---- Вход
        new() { Form = form, Section = secInput, RowNumber = 6,
                Order = 1, Name = "Напряжение на входе", Unit = "В", IsRequired = false },

        // ---- Нагрузка на входе
        new() { Form = form, Section = secInputLoad, RowNumber = 7,
                Order = 1, Name = "Активная нагрузка на входе", Unit = "кОм", IsRequired = false },
        new() { Form = form, Section = secInputLoad, RowNumber = 8,
                Order = 2, Name = "Реактивная нагрузка на входе (ёмкостная)", Unit = "пФ", IsRequired = false },

        // ---- Нагрузка на выходе
        new() { Form = form, Section = secOutputLoad, RowNumber = 9,
                Order = 1, Name = "Активная нагрузка на выходе", Unit = "кОм", IsRequired = false },
        new() { Form = form, Section = secOutputLoad, RowNumber = 10,
                Order = 2, Name = "Реактивная нагрузка на выходе (ёмкостная)", Unit = "пФ", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 11,
                Order = 1, Name = "Температура окружающей среды (корпуса)", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 12,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm70(CatalogDbContext db)
        {
                // ---- Форма 70: Карта рабочих режимов двигателей постоянного и переменного тока,
                // электромагнитных муфт и электровентиляторов
                var form = new Form
                {
                        Number = "70",
                        Title = "Карта рабочих режимов двигателей постоянного и переменного тока, электромагнитных муфт и электровентиляторов",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secSupply = new FormSection { Form = form, Title = "Питание", Order = 1 };
                var secCurrent = new FormSection { Form = form, Title = "Ток", Order = 2 };
                var secSpeed = new FormSection { Form = form, Title = "Частота вращения", Order = 3 };
                var secTemp = new FormSection { Form = form, Title = "Температура", Order = 4 };
                var secMode = new FormSection { Form = form, Title = "Режим работы", Order = 5 };
                var secLoad = new FormSection { Form = form, Title = "Коэффициент нагрузки", Order = 6 };

                form.Sections.AddRange(new[] { secSupply, secCurrent, secSpeed, secTemp, secMode, secLoad });

                var parameters = new List<FormParameter>
    {
        // ---- Напряжение питания
        new() { Form = form, Section = secSupply, RowNumber = 1,
                Order = 1, Name = "Напряжение питания (минимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secSupply, RowNumber = 2,
                Order = 2, Name = "Напряжение питания (максимальное)", Unit = "В", IsRequired = false },

        // ---- Частота напряжения питания (для двигателей переменного тока)
        new() { Form = form, Section = secSupply, RowNumber = 3,
                Order = 3, Name = "Частота напряжения питания (минимальная)", Unit = "Гц", IsRequired = false },
        new() { Form = form, Section = secSupply, RowNumber = 4,
                Order = 4, Name = "Частота напряжения питания (максимальная)", Unit = "Гц", IsRequired = false },

        // ---- Ток, потребляемый обмоткой
        new() { Form = form, Section = secCurrent, RowNumber = 5,
                Order = 1, Name = "Ток, потребляемый обмоткой (номинальный)", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secCurrent, RowNumber = 6,
                Order = 2, Name = "Ток, потребляемый обмоткой (максимальный)", Unit = "А", IsRequired = false },

        // ---- Частота вращения
        new() { Form = form, Section = secSpeed, RowNumber = 7,
                Order = 1, Name = "Частота вращения (номинальная)", Unit = "об/мин", IsRequired = false },
        new() { Form = form, Section = secSpeed, RowNumber = 8,
                Order = 2, Name = "Частота вращения (максимальная)", Unit = "об/мин", IsRequired = false },

        // ---- Температура
        new() { Form = form, Section = secTemp, RowNumber = 9,
                Order = 1, Name = "Температура окружающей среды (минимальная)", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secTemp, RowNumber = 10,
                Order = 2, Name = "Температура окружающей среды (максимальная)", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secTemp, RowNumber = 11,
                Order = 3, Name = "Температура перегрева обмотки", Unit = "°С", IsRequired = false },

        // ---- Характер режима работы
        new() { Form = form, Section = secMode, RowNumber = 12,
                Order = 1, Name = "Характер режима работы (S1, S2, S3...)", Unit = "", IsRequired = false },

        // ---- Коэффициент нагрузки
        new() { Form = form, Section = secLoad, RowNumber = 13,
                Order = 1, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
        new() { Form = form, Section = secLoad, RowNumber = 14,
                Order = 2, Name = "Коэффициент нагрузки (пусковой)", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm71(CatalogDbContext db)
        {
                // ---- Форма 71: Карта рабочих режимов шаговых электродвигателей электромашинного типа
                var form = new Form
                {
                        Number = "71",
                        Title = "Карта рабочих режимов шаговых электродвигателей электромашинного типа",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secSupply = new FormSection { Form = form, Title = "Напряжение питания", Order = 1 };
                var secCurrent = new FormSection { Form = form, Title = "Ток", Order = 2 };
                var secFrequency = new FormSection { Form = form, Title = "Частота", Order = 3 };
                var secTemp = new FormSection { Form = form, Title = "Температура", Order = 4 };
                var secMode = new FormSection { Form = form, Title = "Режим работы", Order = 5 };
                var secLoad = new FormSection { Form = form, Title = "Коэффициент нагрузки", Order = 6 };

                form.Sections.AddRange(new[] { secSupply, secCurrent, secFrequency, secTemp, secMode, secLoad });

                var parameters = new List<FormParameter>
    {
        // ---- Напряжение питания
        new() { Form = form, Section = secSupply, RowNumber = 1,
                Order = 1, Name = "Напряжение питания (минимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secSupply, RowNumber = 2,
                Order = 2, Name = "Напряжение питания (максимальное)", Unit = "В", IsRequired = false },

        // ---- Ток, потребляемый в режиме фиксированной стоянки
        new() { Form = form, Section = secCurrent, RowNumber = 3,
                Order = 1, Name = "Ток, потребляемый в режиме фиксированной стоянки", Unit = "А", IsRequired = false },

        // ---- Частота следования импульсов
        new() { Form = form, Section = secFrequency, RowNumber = 4,
                Order = 1, Name = "Частота следования импульсов", Unit = "шаг/с", IsRequired = false },

        // ---- Температура
        new() { Form = form, Section = secTemp, RowNumber = 5,
                Order = 1, Name = "Температура окружающей среды", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secTemp, RowNumber = 6,
                Order = 2, Name = "Температура обмотки (корпуса)", Unit = "°С", IsRequired = false },

        // ---- Характер режима работы
        new() { Form = form, Section = secMode, RowNumber = 7,
                Order = 1, Name = "Характер режима работы", Unit = "", IsRequired = false },

        // ---- Коэффициент нагрузки
        new() { Form = form, Section = secLoad, RowNumber = 8,
                Order = 1, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm72(CatalogDbContext db)
        {
                // ---- Форма 72: Карта рабочих режимов тахогенераторов и двигателей-генераторов
                var form = new Form
                {
                        Number = "72",
                        Title = "Карта рабочих режимов тахогенераторов и двигателей-генераторов",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secSupply = new FormSection { Form = form, Title = "Питание", Order = 1 };
                var secCurrent = new FormSection { Form = form, Title = "Ток потребляемый", Order = 2 };
                var secSpeed = new FormSection { Form = form, Title = "Частота вращения", Order = 3 };
                var secLoad = new FormSection { Form = form, Title = "Нагрузка", Order = 4 };
                var secTemp = new FormSection { Form = form, Title = "Температура", Order = 5 };
                var secMode = new FormSection { Form = form, Title = "Режим работы", Order = 6 };
                var secCoeff = new FormSection { Form = form, Title = "Коэффициент нагрузки", Order = 7 };

                form.Sections.AddRange(new[] { secSupply, secCurrent, secSpeed, secLoad, secTemp, secMode, secCoeff });

                var parameters = new List<FormParameter>
    {
        // ---- Напряжение питания
        new() { Form = form, Section = secSupply, RowNumber = 1,
                Order = 1, Name = "Напряжение питания (минимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secSupply, RowNumber = 2,
                Order = 2, Name = "Напряжение питания (максимальное)", Unit = "В", IsRequired = false },

        // ---- Частота напряжения питания (для синхронных тахогенераторов)
        new() { Form = form, Section = secSupply, RowNumber = 3,
                Order = 3, Name = "Частота напряжения питания (минимальная)", Unit = "Гц", IsRequired = false },
        new() { Form = form, Section = secSupply, RowNumber = 4,
                Order = 4, Name = "Частота напряжения питания (максимальная)", Unit = "Гц", IsRequired = false },

        // ---- Ток, потребляемый (для режима двигателя)
        new() { Form = form, Section = secCurrent, RowNumber = 5,
                Order = 1, Name = "Ток, потребляемый (номинальный)", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secCurrent, RowNumber = 6,
                Order = 2, Name = "Ток, потребляемый (максимальный)", Unit = "А", IsRequired = false },

        // ---- Частота вращения
        new() { Form = form, Section = secSpeed, RowNumber = 7,
                Order = 1, Name = "Частота вращения (минимальная)", Unit = "об/мин", IsRequired = false },
        new() { Form = form, Section = secSpeed, RowNumber = 8,
                Order = 2, Name = "Частота вращения (максимальная)", Unit = "об/мин", IsRequired = false },

        // ---- Сопротивление нагрузки (для режима генератора/тахогенератора)
        new() { Form = form, Section = secLoad, RowNumber = 9,
                Order = 1, Name = "Сопротивление нагрузки (минимальное)", Unit = "кОм", IsRequired = false },
        new() { Form = form, Section = secLoad, RowNumber = 10,
                Order = 2, Name = "Сопротивление нагрузки (максимальное)", Unit = "кОм", IsRequired = false },

        // ---- Температура
        new() { Form = form, Section = secTemp, RowNumber = 11,
                Order = 1, Name = "Температура окружающей среды", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secTemp, RowNumber = 12,
                Order = 2, Name = "Температура перегрева обмотки (корпуса)", Unit = "°С", IsRequired = false },

        // ---- Характер режима работы
        new() { Form = form, Section = secMode, RowNumber = 13,
                Order = 1, Name = "Характер режима работы (S1, S2, S3...)", Unit = "", IsRequired = false },

        // ---- Коэффициент нагрузки
        new() { Form = form, Section = secCoeff, RowNumber = 14,
                Order = 1, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm73(CatalogDbContext db)
        {
                // ---- Форма 73: Карта рабочих режимов сельсинов, вращающихся трансформаторов и фазовращателей
                var form = new Form
                {
                        Number = "73",
                        Title = "Карта рабочих режимов сельсинов, вращающихся трансформаторов и фазовращателей",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secSupply = new FormSection { Form = form, Title = "Питание обмотки возбуждения", Order = 1 };
                var secCurrent = new FormSection { Form = form, Title = "Ток", Order = 2 };
                var secSpeed = new FormSection { Form = form, Title = "Частота вращения", Order = 3 };
                var secReceivers = new FormSection { Form = form, Title = "Приёмники", Order = 4 };
                var secLoad = new FormSection { Form = form, Title = "Нагрузка", Order = 5 };
                var secTemp = new FormSection { Form = form, Title = "Температура", Order = 6 };
                var secMode = new FormSection { Form = form, Title = "Режим работы", Order = 7 };
                var secCoeff = new FormSection { Form = form, Title = "Коэффициент нагрузки", Order = 8 };

                form.Sections.AddRange(new[] { secSupply, secCurrent, secSpeed, secReceivers, secLoad, secTemp, secMode, secCoeff });

                var parameters = new List<FormParameter>
    {
        // ---- Напряжение питания обмотки возбуждения
        new() { Form = form, Section = secSupply, RowNumber = 1,
                Order = 1, Name = "Напряжение питания обмотки возбуждения (минимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secSupply, RowNumber = 2,
                Order = 2, Name = "Напряжение питания обмотки возбуждения (максимальное)", Unit = "В", IsRequired = false },

        // ---- Ток, потребляемый обмоткой возбуждения
        new() { Form = form, Section = secCurrent, RowNumber = 3,
                Order = 1, Name = "Ток, потребляемый обмоткой возбуждения (номинальный)", Unit = "А", IsRequired = false },

        // ---- Частота вращения
        new() { Form = form, Section = secSpeed, RowNumber = 4,
                Order = 1, Name = "Частота вращения", Unit = "об/мин", IsRequired = false },

        // ---- Количество приемников, подключаемых к датчику
        new() { Form = form, Section = secReceivers, RowNumber = 5,
                Order = 1, Name = "Количество приемников, подключаемых к датчику", Unit = "шт.", IsRequired = false },

        // ---- Сопротивление нагрузки (для вращающихся трансформаторов)
        new() { Form = form, Section = secLoad, RowNumber = 6,
                Order = 1, Name = "Сопротивление нагрузки", Unit = "Ом", IsRequired = false },

        // ---- Температура
        new() { Form = form, Section = secTemp, RowNumber = 7,
                Order = 1, Name = "Температура окружающей среды", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secTemp, RowNumber = 8,
                Order = 2, Name = "Температура обмотки (корпуса)", Unit = "°С", IsRequired = false },

        // ---- Характер режима работы
        new() { Form = form, Section = secMode, RowNumber = 9,
                Order = 1, Name = "Характер режима работы", Unit = "", IsRequired = false },

        // ---- Коэффициент нагрузки
        new() { Form = form, Section = secCoeff, RowNumber = 10,
                Order = 1, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm74(CatalogDbContext db)
        {
                // ---- Форма 74: Карта рабочих режимов цифровых преобразователей угла
                var form = new Form
                {
                        Number = "74",
                        Title = "Карта рабочих режимов цифровых преобразователей угла",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secSupply = new FormSection { Form = form, Title = "Питание", Order = 1 };
                var secControl = new FormSection { Form = form, Title = "Параметры управляющих сигналов (опрос)", Order = 2 };
                var secLogic = new FormSection { Form = form, Title = "Логические уровни", Order = 3 };
                var secLoad = new FormSection { Form = form, Title = "Параметры нагрузки", Order = 4 };
                var secSpeed = new FormSection { Form = form, Title = "Частота вращения", Order = 5 };
                var secEnv = new FormSection { Form = form, Title = "Температура", Order = 6 };

                form.Sections.AddRange(new[] { secSupply, secControl, secLogic, secLoad, secSpeed, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Напряжение питания
        new() { Form = form, Section = secSupply, RowNumber = 1,
                Order = 1, Name = "Напряжение питания (минимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secSupply, RowNumber = 2,
                Order = 2, Name = "Напряжение питания (максимальное)", Unit = "В", IsRequired = false },

        // ---- Потребляемый ток
        new() { Form = form, Section = secSupply, RowNumber = 3,
                Order = 3, Name = "Потребляемый ток", Unit = "мА", IsRequired = false },

        // ---- Параметры управляющих сигналов (опрос)
        new() { Form = form, Section = secControl, RowNumber = 4,
                Order = 1, Name = "Амплитуда напряжения сигналов опроса", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secControl, RowNumber = 5,
                Order = 2, Name = "Амплитуда тока сигналов опроса", Unit = "мА", IsRequired = false },
        new() { Form = form, Section = secControl, RowNumber = 6,
                Order = 3, Name = "Частота следования сигналов опроса", Unit = "Гц", IsRequired = false },
        new() { Form = form, Section = secControl, RowNumber = 7,
                Order = 4, Name = "Длительность сигналов опроса", Unit = "мкс", IsRequired = false },

        // ---- Логические уровни
        new() { Form = form, Section = secLogic, RowNumber = 8,
                Order = 1, Name = "Напряжение логического нуля", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secLogic, RowNumber = 9,
                Order = 2, Name = "Напряжение логической единицы", Unit = "В", IsRequired = false },

        // ---- Параметры нагрузки
        new() { Form = form, Section = secLoad, RowNumber = 10,
                Order = 1, Name = "Активная нагрузка", Unit = "Ом", IsRequired = false },
        new() { Form = form, Section = secLoad, RowNumber = 11,
                Order = 2, Name = "Ёмкостная нагрузка", Unit = "мкФ", IsRequired = false },

        // ---- Частота вращения
        new() { Form = form, Section = secSpeed, RowNumber = 13,
                Order = 1, Name = "Частота вращения", Unit = "об/мин", IsRequired = false },

        // ---- Температура окружающей среды
        new() { Form = form, Section = secEnv, RowNumber = 14,
                Order = 1, Name = "Температура окружающей среды", Unit = "°С", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }


        // TODO: Form75

        // TODO:Form76

        private static Form AddForm77(CatalogDbContext db)
        {
                // ---- Форма 77: Карта рабочих режимов электромагнитных реле, контакторов,
                // вакуумных выключателей и переключателей, магнитоуправляемых контактов
                var form = new Form
                {
                        Number = "77",
                        Title = "Карта рабочих режимов электромагнитных реле, контакторов, вакуумных выключателей и переключателей, магнитоуправляемых контактов",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secCoil = new FormSection { Form = form, Title = "Параметры катушки", Order = 1 };
                var secSwitching = new FormSection { Form = form, Title = "Коммутируемые параметры", Order = 2 };
                var secCurrent = new FormSection { Form = form, Title = "Ток", Order = 3 };
                var secPower = new FormSection { Form = form, Title = "Мощность", Order = 4 };
                var secLoad = new FormSection { Form = form, Title = "Нагрузка", Order = 5 };
                var secTiming = new FormSection { Form = form, Title = "Временные параметры", Order = 6 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 7 };

                form.Sections.AddRange(new[] { secCoil, secSwitching, secCurrent, secPower, secLoad, secTiming, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Параметры катушки
        new() { Form = form, Section = secCoil, RowNumber = 1,
                Order = 1, Name = "Рабочее напряжение катушки", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secCoil, RowNumber = 2,
                Order = 2, Name = "Рабочий ток катушки", Unit = "А", IsRequired = false },

        // ---- Коммутируемые параметры
        new() { Form = form, Section = secSwitching, RowNumber = 3,
                Order = 1, Name = "Коммутируемое напряжение (минимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secSwitching, RowNumber = 4,
                Order = 2, Name = "Коммутируемое напряжение (максимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secSwitching, RowNumber = 5,
                Order = 3, Name = "Коммутируемый ток (минимальный)", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secSwitching, RowNumber = 6,
                Order = 4, Name = "Коммутируемый ток (максимальный)", Unit = "А", IsRequired = false },

        // ---- Ток
        new() { Form = form, Section = secCurrent, RowNumber = 7,
                Order = 1, Name = "Пропускаемый ток (минимальный)", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secCurrent, RowNumber = 8,
                Order = 2, Name = "Пропускаемый ток (максимальный)", Unit = "А", IsRequired = false },

        // ---- Мощность
        new() { Form = form, Section = secPower, RowNumber = 9,
                Order = 1, Name = "Максимальная коммутируемая (пропускаемая) мощность", Unit = "ВА", IsRequired = false },

        // ---- Характер нагрузки
        new() { Form = form, Section = secLoad, RowNumber = 10,
                Order = 1, Name = "Род тока (постоянный, переменный)", Unit = "", IsRequired = false },
        new() { Form = form, Section = secLoad, RowNumber = 11,
                Order = 2, Name = "Длительность протекания тока перегрузки", Unit = "с", IsRequired = false },
        new() { Form = form, Section = secLoad, RowNumber = 12,
                Order = 3, Name = "Параметр (характер) нагрузки", Unit = "", IsRequired = false },

        // ---- Частота и циклы
        new() { Form = form, Section = secTiming, RowNumber = 13,
                Order = 1, Name = "Частота коммутации", Unit = "Гц", IsRequired = false },
        new() { Form = form, Section = secTiming, RowNumber = 14,
                Order = 2, Name = "Число коммутационных циклов", Unit = "", IsRequired = false },
        new() { Form = form, Section = secTiming, RowNumber = 15,
                Order = 3, Name = "Время неполной коммутации", Unit = "с", IsRequired = false },
        new() { Form = form, Section = secTiming, RowNumber = 16,
                Order = 4, Name = "Время коммутации (срабатывания)", Unit = "с", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 17,
                Order = 1, Name = "Температура окружающей среды", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 18,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm78(CatalogDbContext db)
        {
                // ---- Форма 78: Карта рабочих режимов электромагнитных реле максимального тока и электротепловых токовых реле
                var form = new Form
                {
                        Number = "78",
                        Title = "Карта рабочих режимов электромагнитных реле максимального тока и электротепловых токовых реле",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secMainCircuit = new FormSection { Form = form, Title = "Режим работы главной цепи", Order = 1 };
                var secContacts = new FormSection { Form = form, Title = "Режим работы контактов", Order = 2 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 3 };

                form.Sections.AddRange(new[] { secMainCircuit, secContacts, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Режим работы главной цепи
        new() { Form = form, Section = secMainCircuit, RowNumber = 1,
                Order = 1, Name = "Номинальное напряжение главной цепи", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secMainCircuit, RowNumber = 2,
                Order = 2, Name = "Частота тока главной цепи", Unit = "Гц", IsRequired = false },
        new() { Form = form, Section = secMainCircuit, RowNumber = 3,
                Order = 3, Name = "Номинальный ток главной цепи", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secMainCircuit, RowNumber = 4,
                Order = 4, Name = "Установка номинального тока", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secMainCircuit, RowNumber = 5,
                Order = 5, Name = "Установка тока срабатывания", Unit = "I/ном.", IsRequired = false },
        new() { Form = form, Section = secMainCircuit, RowNumber = 6,
                Order = 6, Name = "Перегрузка по току", Unit = "I/ном.", IsRequired = false },
        new() { Form = form, Section = secMainCircuit, RowNumber = 7,
                Order = 7, Name = "Длительность перегрузки по току", Unit = "с", IsRequired = false },

        // ---- Режим работы контактов
        new() { Form = form, Section = secContacts, RowNumber = 8,
                Order = 1, Name = "Номера выводов контактов", Unit = "", IsRequired = false },
        new() { Form = form, Section = secContacts, RowNumber = 9,
                Order = 2, Name = "Частота тока контактов", Unit = "Гц", IsRequired = false },
        new() { Form = form, Section = secContacts, RowNumber = 10,
                Order = 3, Name = "Номинальное напряжение контактов", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secContacts, RowNumber = 11,
                Order = 4, Name = "Коммутируемый ток контактов", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secContacts, RowNumber = 12,
                Order = 5, Name = "Параметр (характер) нагрузки контактов", Unit = "", IsRequired = false },
        new() { Form = form, Section = secContacts, RowNumber = 13,
                Order = 6, Name = "Количество срабатываний", Unit = "", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 14,
                Order = 1, Name = "Температура окружающей среды", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 15,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm79(CatalogDbContext db)
        {
                // ---- Форма 79: Карта рабочих режимов реле времени
                var form = new Form
                {
                        Number = "79",
                        Title = "Карта рабочих режимов реле времени",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secCoil = new FormSection { Form = form, Title = "Параметры катушки", Order = 1 };
                var secTiming = new FormSection { Form = form, Title = "Временные параметры", Order = 2 };
                var secSwitching = new FormSection { Form = form, Title = "Коммутируемые параметры", Order = 3 };
                var secLoad = new FormSection { Form = form, Title = "Нагрузка", Order = 4 };
                var secReliability = new FormSection { Form = form, Title = "Надёжность", Order = 5 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 6 };

                form.Sections.AddRange(new[] { secCoil, secTiming, secSwitching, secLoad, secReliability, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Параметры катушки
        new() { Form = form, Section = secCoil, RowNumber = 1,
                Order = 1, Name = "Рабочее напряжение катушки", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secCoil, RowNumber = 2,
                Order = 2, Name = "Потребляемый ток до срабатывания", Unit = "мА", IsRequired = false },
        new() { Form = form, Section = secCoil, RowNumber = 3,
                Order = 3, Name = "Потребляемый ток после срабатывания", Unit = "мА", IsRequired = false },

        // ---- Временные параметры
        new() { Form = form, Section = secTiming, RowNumber = 4,
                Order = 1, Name = "Время срабатывания", Unit = "с", IsRequired = false },
        new() { Form = form, Section = secTiming, RowNumber = 5,
                Order = 2, Name = "Время восстановления", Unit = "с", IsRequired = false },
        new() { Form = form, Section = secTiming, RowNumber = 6,
                Order = 3, Name = "Время подготовки", Unit = "с", IsRequired = false },

        // ---- Коммутируемые параметры
        new() { Form = form, Section = secSwitching, RowNumber = 7,
                Order = 1, Name = "Коммутируемое напряжение (минимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secSwitching, RowNumber = 8,
                Order = 2, Name = "Коммутируемое напряжение (максимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secSwitching, RowNumber = 9,
                Order = 3, Name = "Коммутируемый ток (минимальный)", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secSwitching, RowNumber = 10,
                Order = 4, Name = "Коммутируемый ток (максимальный)", Unit = "А", IsRequired = false },

        // ---- Нагрузка
        new() { Form = form, Section = secLoad, RowNumber = 11,
                Order = 1, Name = "Максимальная коммутируемая мощность", Unit = "Вт (ВА)", IsRequired = false },
        new() { Form = form, Section = secLoad, RowNumber = 12,
                Order = 2, Name = "Род тока (постоянный, переменный)", Unit = "", IsRequired = false },
        new() { Form = form, Section = secLoad, RowNumber = 13,
                Order = 3, Name = "Параметр (характер) нагрузки", Unit = "", IsRequired = false },

        // ---- Надёжность
        new() { Form = form, Section = secReliability, RowNumber = 14,
                Order = 1, Name = "Число коммутационных циклов", Unit = "", IsRequired = false },
        new() { Form = form, Section = secReliability, RowNumber = 15,
                Order = 2, Name = "Частота коммутации", Unit = "Гц", IsRequired = false },
        new() { Form = form, Section = secReliability, RowNumber = 16,
                Order = 3, Name = "Непрерывное пребывание под рабочим напряжением во включенном состоянии", Unit = "ч", IsRequired = false },
        new() { Form = form, Section = secReliability, RowNumber = 17,
                Order = 4, Name = "Суммарное пребывание под рабочим напряжением во включенном состоянии", Unit = "ч", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 18,
                Order = 1, Name = "Температура окружающей среды", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 19,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm80(CatalogDbContext db)
        {
                // ---- Форма 80: Карта рабочих режимов бесконтактных коммутационных устройств
                var form = new Form
                {
                        Number = "80",
                        Title = "Карта рабочих режимов бесконтактных коммутационных устройств",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secSupply = new FormSection { Form = form, Title = "Питание", Order = 1 };
                var secControl = new FormSection { Form = form, Title = "Сигнал управления", Order = 2 };
                var secSwitching = new FormSection { Form = form, Title = "Коммутируемые параметры", Order = 3 };
                var secLoad = new FormSection { Form = form, Title = "Нагрузка и защита", Order = 4 };
                var secPower = new FormSection { Form = form, Title = "Рассеиваемая мощность", Order = 5 };
                var secTemp = new FormSection { Form = form, Title = "Температура", Order = 6 };
                var secCoeff = new FormSection { Form = form, Title = "Коэффициент нагрузки", Order = 7 };

                form.Sections.AddRange(new[] { secSupply, secControl, secSwitching, secLoad, secPower, secTemp, secCoeff });

                var parameters = new List<FormParameter>
    {
        // ---- Питание
        new() { Form = form, Section = secSupply, RowNumber = 1,
                Order = 1, Name = "Напряжение питания (мин/макс)", Unit = "В", IsRequired = false },

        // ---- Сигнал управления
        new() { Form = form, Section = secControl, RowNumber = 2,
                Order = 1, Name = "Напряжение сигнала управления (мин/макс)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secControl, RowNumber = 3,
                Order = 2, Name = "Ток сигнала управления (мин/макс)", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secControl, RowNumber = 4,
                Order = 3, Name = "Длительность импульса управления", Unit = "мкс", IsRequired = false },

        // ---- Коммутируемые параметры
        new() { Form = form, Section = secSwitching, RowNumber = 5,
                Order = 1, Name = "Коммутируемое напряжение (мин/макс)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secSwitching, RowNumber = 6,
                Order = 2, Name = "Коммутируемый ток (мин/макс)", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secSwitching, RowNumber = 7,
                Order = 3, Name = "Род тока (постоянный, переменный)", Unit = "", IsRequired = false },

        // ---- Нагрузка и защита
        new() { Form = form, Section = secLoad, RowNumber = 8,
                Order = 1, Name = "Параметры (характер) нагрузки", Unit = "", IsRequired = false },
        new() { Form = form, Section = secLoad, RowNumber = 9,
                Order = 2, Name = "Ток нагрузки (номинальный)", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secLoad, RowNumber = 10,
                Order = 3, Name = "Длительность протекания тока перегрузки", Unit = "с", IsRequired = false },
        new() { Form = form, Section = secLoad, RowNumber = 11,
                Order = 4, Name = "Ток срабатывания схемы защиты от короткого замыкания", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secLoad, RowNumber = 12,
                Order = 5, Name = "Время срабатывания схемы защиты от короткого замыкания", Unit = "с", IsRequired = false },

        // ---- Рассеиваемая мощность
        new() { Form = form, Section = secPower, RowNumber = 13,
                Order = 1, Name = "Рассеиваемая мощность", Unit = "Вт (ВА)", IsRequired = false },

        // ---- Температура
        new() { Form = form, Section = secTemp, RowNumber = 14,
                Order = 1, Name = "Температура окружающей среды", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secTemp, RowNumber = 15,
                Order = 2, Name = "Температура корпуса", Unit = "°С", IsRequired = false },

        // ---- Коэффициент нагрузки
        new() { Form = form, Section = secCoeff, RowNumber = 16,
                Order = 1, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm81(CatalogDbContext db)
        {
                // ---- Форма 81: Карта рабочих режимов микровыключателей и микропереключателей,
                // тумблеров, кнопок, кнопочных, движковых, поворотных и пакетных переключателей
                var form = new Form
                {
                        Number = "81",
                        Title = "Карта рабочих режимов микровыключателей и микропереключателей, тумблеров, кнопок, кнопочных, движковых, поворотных и пакетных переключателей",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secGeneral = new FormSection { Form = form, Title = "Общие параметры", Order = 1 };
                var secSwitching = new FormSection { Form = form, Title = "Коммутируемые параметры", Order = 2 };
                var secLoad = new FormSection { Form = form, Title = "Параметры нагрузки", Order = 3 };
                var secMechanical = new FormSection { Form = form, Title = "Механические параметры", Order = 4 };
                var secReliability = new FormSection { Form = form, Title = "Надёжность", Order = 5 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 6 };

                form.Sections.AddRange(new[] { secGeneral, secSwitching, secLoad, secMechanical, secReliability, secEnv });

                var parameters = new List<FormParameter>
    {
        // ---- Общие параметры
        new() { Form = form, Section = secGeneral, RowNumber = 1,
                Order = 1, Name = "Род тока (постоянный, переменный)", Unit = "", IsRequired = false },

        // ---- Коммутируемые параметры
        new() { Form = form, Section = secSwitching, RowNumber = 2,
                Order = 1, Name = "Коммутируемое напряжение (минимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secSwitching, RowNumber = 3,
                Order = 2, Name = "Коммутируемое напряжение (максимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secSwitching, RowNumber = 4,
                Order = 3, Name = "Коммутируемый ток (минимальный)", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secSwitching, RowNumber = 5,
                Order = 4, Name = "Коммутируемый ток (максимальный)", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secSwitching, RowNumber = 6,
                Order = 5, Name = "Максимальная коммутируемая мощность", Unit = "Вт (ВА)", IsRequired = false },

        // ---- Параметры нагрузки
        new() { Form = form, Section = secLoad, RowNumber = 7,
                Order = 1, Name = "Параметр (характер) нагрузки", Unit = "", IsRequired = false },
        new() { Form = form, Section = secLoad, RowNumber = 8,
                Order = 2, Name = "Ток перегрузки", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secLoad, RowNumber = 9,
                Order = 3, Name = "Время протекания тока перегрузки", Unit = "с", IsRequired = false },

        // ---- Механические параметры
        new() { Form = form, Section = secMechanical, RowNumber = 10,
                Order = 1, Name = "Полный ход приводного элемента", Unit = "мм", IsRequired = false },
        new() { Form = form, Section = secMechanical, RowNumber = 11,
                Order = 2, Name = "Частота срабатывания", Unit = "Гц", IsRequired = false },

        // ---- Надёжность
        new() { Form = form, Section = secReliability, RowNumber = 12,
                Order = 1, Name = "Число коммутационных циклов", Unit = "", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 13,
                Order = 1, Name = "Температура окружающей среды", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secEnv, RowNumber = 14,
                Order = 2, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm82(CatalogDbContext db)
        {
                // ---- Форма 82: Карта рабочих режимов линейных интегральных стабилизаторов напряжения
                var form = new Form
                {
                        Number = "82",
                        Title = "Карта рабочих режимов линейных интегральных стабилизаторов напряжения",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secInput = new FormSection { Form = form, Title = "Входное напряжение", Order = 1 };
                var secControlSupply = new FormSection { Form = form, Title = "Питание схемы управления", Order = 2 };
                var secOutput = new FormSection { Form = form, Title = "Выходное напряжение", Order = 3 };
                var secLoad = new FormSection { Form = form, Title = "Ток нагрузки", Order = 4 };
                var secDivider = new FormSection { Form = form, Title = "Делитель", Order = 5 };
                var secPower = new FormSection { Form = form, Title = "Рассеиваемая мощность", Order = 6 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 7 };
                var secCoeff = new FormSection { Form = form, Title = "Коэффициент нагрузки", Order = 8 };

                form.Sections.AddRange(new[] { secInput, secControlSupply, secOutput, secLoad, secDivider, secPower, secEnv, secCoeff });

                var parameters = new List<FormParameter>
    {
        // ---- Входное напряжение (первый вход)
        new() { Form = form, Section = secInput, RowNumber = 1,
                Order = 1, Name = "Входное напряжение на первом входе (минимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secInput, RowNumber = 2,
                Order = 2, Name = "Входное напряжение на первом входе (максимальное)", Unit = "В", IsRequired = false },

        // ---- Входное напряжение (второй вход) - для двухполярных стабилизаторов
        new() { Form = form, Section = secInput, RowNumber = 3,
                Order = 3, Name = "Входное напряжение на втором входе (минимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secInput, RowNumber = 4,
                Order = 4, Name = "Входное напряжение на втором входе (максимальное)", Unit = "В", IsRequired = false },

        // ---- Напряжение питания схемы управления
        new() { Form = form, Section = secControlSupply, RowNumber = 5,
                Order = 1, Name = "Напряжение питания схемы управления", Unit = "В", IsRequired = false },

        // ---- Выходное напряжение (первый выход)
        new() { Form = form, Section = secOutput, RowNumber = 6,
                Order = 1, Name = "Выходное напряжение на первом выходе (минимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secOutput, RowNumber = 7,
                Order = 2, Name = "Выходное напряжение на первом выходе (максимальное)", Unit = "В", IsRequired = false },

        // ---- Выходное напряжение (второй выход) - для двухполярных стабилизаторов
        new() { Form = form, Section = secOutput, RowNumber = 8,
                Order = 3, Name = "Выходное напряжение на втором выходе (минимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secOutput, RowNumber = 9,
                Order = 4, Name = "Выходное напряжение на втором выходе (максимальное)", Unit = "В", IsRequired = false },

        // ---- Ток нагрузки
        new() { Form = form, Section = secLoad, RowNumber = 10,
                Order = 1, Name = "Ток нагрузки на первом выходе", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secLoad, RowNumber = 11,
                Order = 2, Name = "Ток нагрузки на втором выходе", Unit = "А", IsRequired = false },

        // ---- Минимальный ток делителя
        new() { Form = form, Section = secDivider, RowNumber = 12,
                Order = 1, Name = "Минимальный ток делителя", Unit = "мА", IsRequired = false },

        // ---- Рассеиваемая мощность
        new() { Form = form, Section = secPower, RowNumber = 13,
                Order = 1, Name = "Рассеиваемая мощность", Unit = "Вт", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 14,
                Order = 1, Name = "Температура окружающей среды (корпуса)", Unit = "°С", IsRequired = false },

        // ---- Коэффициент нагрузки
        new() { Form = form, Section = secCoeff, RowNumber = 15,
                Order = 1, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm83(CatalogDbContext db)
        {
                // ---- Форма 83: Карта рабочих режимов вторичных источников питания
                var form = new Form
                {
                        Number = "83",
                        Title = "Карта рабочих режимов вторичных источников питания",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secInput = new FormSection { Form = form, Title = "Входные параметры", Order = 1 };
                var secOutput = new FormSection { Form = form, Title = "Выходные параметры", Order = 2 };
                var secLoad = new FormSection { Form = form, Title = "Нагрузка", Order = 3 };
                var secProtection = new FormSection { Form = form, Title = "Защита", Order = 4 };
                var secRipple = new FormSection { Form = form, Title = "Пульсации", Order = 5 };
                var secPower = new FormSection { Form = form, Title = "Мощность", Order = 6 };
                var secTemp = new FormSection { Form = form, Title = "Температура", Order = 7 };
                var secCoeff = new FormSection { Form = form, Title = "Коэффициент нагрузки", Order = 8 };

                form.Sections.AddRange(new[] { secInput, secOutput, secLoad, secProtection, secRipple, secPower, secTemp, secCoeff });

                var parameters = new List<FormParameter>
    {
        // ---- Входные параметры
        new() { Form = form, Section = secInput, RowNumber = 1,
                Order = 1, Name = "Входное напряжение (минимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secInput, RowNumber = 2,
                Order = 2, Name = "Входное напряжение (максимальное)", Unit = "В", IsRequired = false },

        // ---- Выходные параметры
        new() { Form = form, Section = secOutput, RowNumber = 3,
                Order = 1, Name = "Выходное напряжение (минимальное)", Unit = "В", IsRequired = false },
        new() { Form = form, Section = secOutput, RowNumber = 4,
                Order = 2, Name = "Выходное напряжение (максимальное)", Unit = "В", IsRequired = false },

        // ---- Нагрузка
        new() { Form = form, Section = secLoad, RowNumber = 5,
                Order = 1, Name = "Ток нагрузки (минимальный)", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secLoad, RowNumber = 6,
                Order = 2, Name = "Ток нагрузки (максимальный)", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secLoad, RowNumber = 7,
                Order = 3, Name = "Род тока (постоянный, переменный)", Unit = "", IsRequired = false },
        new() { Form = form, Section = secLoad, RowNumber = 8,
                Order = 4, Name = "Параметры (характер) нагрузки", Unit = "", IsRequired = false },

        // ---- Защита
        new() { Form = form, Section = secProtection, RowNumber = 9,
                Order = 1, Name = "Ток срабатывания защиты от короткого замыкания", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secProtection, RowNumber = 10,
                Order = 2, Name = "Максимальный ток перегрузки", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secProtection, RowNumber = 11,
                Order = 3, Name = "Время срабатывания защиты от короткого замыкания", Unit = "с", IsRequired = false },

        // ---- Пульсации
        new() { Form = form, Section = secRipple, RowNumber = 12,
                Order = 1, Name = "Напряжение пульсации", Unit = "мВ", IsRequired = false },

        // ---- Мощность
        new() { Form = form, Section = secPower, RowNumber = 13,
                Order = 1, Name = "Рассеиваемая мощность", Unit = "Вт", IsRequired = false },

        // ---- Температура
        new() { Form = form, Section = secTemp, RowNumber = 14,
                Order = 1, Name = "Температура окружающей среды", Unit = "°С", IsRequired = false },
        new() { Form = form, Section = secTemp, RowNumber = 15,
                Order = 2, Name = "Температура корпуса", Unit = "°С", IsRequired = false },

        // ---- Коэффициент нагрузки
        new() { Form = form, Section = secCoeff, RowNumber = 16,
                Order = 1, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm84(CatalogDbContext db)
        {
                // ---- Форма 84: Карта рабочих режимов силовых трансформаторов
                var form = new Form
                {
                        Number = "84",
                        Title = "Карта рабочих режимов силовых трансформаторов",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secFrequency = new FormSection { Form = form, Title = "Частота", Order = 1 };
                var secVoltage = new FormSection { Form = form, Title = "Напряжение", Order = 2 };
                var secCurrent = new FormSection { Form = form, Title = "Ток", Order = 3 };
                var secTemp = new FormSection { Form = form, Title = "Температура", Order = 4 };
                var secLoad = new FormSection { Form = form, Title = "Коэффициент нагрузки", Order = 5 };

                form.Sections.AddRange(new[] { secFrequency, secVoltage, secCurrent, secTemp, secLoad });

                var parameters = new List<FormParameter>
    {
        // ---- Частота питающего напряжения
        new() { Form = form, Section = secFrequency, RowNumber = 1,
                Order = 1, Name = "Частота питающего напряжения", Unit = "Гц", IsRequired = false },

        // ---- Напряжение на первичной обмотке
        new() { Form = form, Section = secVoltage, RowNumber = 2,
                Order = 1, Name = "Напряжение на первичной обмотке", Unit = "В", IsRequired = false },

        // ---- Рабочий потенциал обмотки
        new() { Form = form, Section = secVoltage, RowNumber = 3,
                Order = 2, Name = "Рабочий потенциал обмотки", Unit = "В", IsRequired = false },

        // ---- Ток обмотки
        new() { Form = form, Section = secCurrent, RowNumber = 4,
                Order = 1, Name = "Ток обмотки", Unit = "А", IsRequired = false },

        // ---- Температура окружающей среды (обмоток)
        new() { Form = form, Section = secTemp, RowNumber = 5,
                Order = 1, Name = "Температура окружающей среды (обмоток)", Unit = "°С", IsRequired = false },

        // ---- Коэффициент нагрузки
        new() { Form = form, Section = secLoad, RowNumber = 6,
                Order = 1, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm85(CatalogDbContext db)
        {
                // ---- Форма 85: Карта рабочих режимов импульсных трансформаторов
                var form = new Form
                {
                        Number = "85",
                        Title = "Карта рабочих режимов импульсных трансформаторов",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secCurrent = new FormSection { Form = form, Title = "Импульсный ток", Order = 1 };
                var secPulseParams = new FormSection { Form = form, Title = "Параметры импульса на первичной обмотке", Order = 2 };
                var secVoltSec = new FormSection { Form = form, Title = "Произведение U·τ", Order = 3 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 4 };
                var secLoad = new FormSection { Form = form, Title = "Коэффициент нагрузки", Order = 5 };

                form.Sections.AddRange(new[] { secCurrent, secPulseParams, secVoltSec, secEnv, secLoad });

                var parameters = new List<FormParameter>
    {
        // ---- Импульсный ток
        new() { Form = form, Section = secCurrent, RowNumber = 1,
                Order = 1, Name = "Импульсный ток", Unit = "мА", IsRequired = false },

        // ---- Параметры импульса на первичной обмотке
        new() { Form = form, Section = secPulseParams, RowNumber = 2,
                Order = 1, Name = "Частота повторения импульсов", Unit = "Гц", IsRequired = false },
        new() { Form = form, Section = secPulseParams, RowNumber = 3,
                Order = 2, Name = "Длительность импульса", Unit = "мкс", IsRequired = false },
        new() { Form = form, Section = secPulseParams, RowNumber = 4,
                Order = 3, Name = "Амплитуда напряжения импульса", Unit = "В", IsRequired = false },

        // ---- Произведение U·τ (вольт-секундная площадь)
        new() { Form = form, Section = secVoltSec, RowNumber = 5,
                Order = 1, Name = "Uи × Ти (вольт-секундная площадь)", Unit = "В·мкс", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 6,
                Order = 1, Name = "Температура окружающей среды (обмоток)", Unit = "°С", IsRequired = false },

        // ---- Коэффициент нагрузки
        new() { Form = form, Section = secLoad, RowNumber = 7,
                Order = 1, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm86(CatalogDbContext db)
        {
                // ---- Форма 86: Карта рабочих режимов дросселей фильтров
                var form = new Form
                {
                        Number = "86",
                        Title = "Карта рабочих режимов дросселей фильтров",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secFreq = new FormSection { Form = form, Title = "Частота", Order = 1 };
                var secCurrent = new FormSection { Form = form, Title = "Ток", Order = 2 };
                var secVoltage = new FormSection { Form = form, Title = "Напряжение", Order = 3 };
                var secTemp = new FormSection { Form = form, Title = "Температура", Order = 4 };
                var secLoad = new FormSection { Form = form, Title = "Коэффициент нагрузки", Order = 5 };

                form.Sections.AddRange(new[] { secFreq, secCurrent, secVoltage, secTemp, secLoad });

                var parameters = new List<FormParameter>
    {
        // ---- Частота тока
        new() { Form = form, Section = secFreq, RowNumber = 1,
                Order = 1, Name = "Частота тока", Unit = "Гц", IsRequired = false },

        // ---- Ток подмагничивания
        new() { Form = form, Section = secCurrent, RowNumber = 2,
                Order = 1, Name = "Ток подмагничивания", Unit = "А", IsRequired = false },

        // ---- Максимальный ток
        new() { Form = form, Section = secCurrent, RowNumber = 3,
                Order = 2, Name = "Максимальный ток", Unit = "А", IsRequired = false },

        // ---- Рабочий потенциал обмотки
        new() { Form = form, Section = secVoltage, RowNumber = 4,
                Order = 1, Name = "Рабочий потенциал обмотки", Unit = "В", IsRequired = false },

        // ---- Переменная составляющая
        new() { Form = form, Section = secVoltage, RowNumber = 5,
                Order = 2, Name = "Переменная составляющая напряжения", Unit = "В", IsRequired = false },

        // ---- Температура окружающей среды (обмотки)
        new() { Form = form, Section = secTemp, RowNumber = 6,
                Order = 1, Name = "Температура окружающей среды (обмотки)", Unit = "°С", IsRequired = false },

        // ---- Коэффициент нагрузки
        new() { Form = form, Section = secLoad, RowNumber = 7,
                Order = 1, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }
        private static Form AddForm87(CatalogDbContext db)
        {
                // ---- Форма 87: Карта рабочих режимов предохранителей и держателей предохранителей
                var form = new Form
                {
                        Number = "87",
                        Title = "Карта рабочих режимов предохранителей и держателей предохранителей",
                        IsUniversal = false,
                        ColumnGrouping = ColumnGrouping.PerRegimeGroup
                };

                // Секции (логическая группировка параметров)
                var secGeneral = new FormSection { Form = form, Title = "Общие параметры", Order = 1 };
                var secCurrent = new FormSection { Form = form, Title = "Ток", Order = 2 };
                var secTime = new FormSection { Form = form, Title = "Временные параметры", Order = 3 };
                var secReliability = new FormSection { Form = form, Title = "Надёжность", Order = 4 };
                var secEnv = new FormSection { Form = form, Title = "Условия эксплуатации", Order = 5 };
                var secLoad = new FormSection { Form = form, Title = "Коэффициент нагрузки", Order = 6 };

                form.Sections.AddRange(new[] { secGeneral, secCurrent, secTime, secReliability, secEnv, secLoad });

                var parameters = new List<FormParameter>
    {
        // ---- Общие параметры
        new() { Form = form, Section = secGeneral, RowNumber = 1,
                Order = 1, Name = "Род тока (постоянный, переменный)", Unit = "", IsRequired = false },
        new() { Form = form, Section = secGeneral, RowNumber = 2,
                Order = 2, Name = "Максимальное рабочее напряжение", Unit = "В", IsRequired = false },

        // ---- Ток
        new() { Form = form, Section = secCurrent, RowNumber = 3,
                Order = 1, Name = "Номинальный ток", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secCurrent, RowNumber = 4,
                Order = 2, Name = "Ток перегрузки", Unit = "А", IsRequired = false },

        // ---- Временные параметры
        new() { Form = form, Section = secTime, RowNumber = 5,
                Order = 1, Name = "Время срабатывания при протекании тока перегрузки", Unit = "с", IsRequired = false },

        // ---- Надёжность
        new() { Form = form, Section = secReliability, RowNumber = 6,
                Order = 1, Name = "Наибольший ток перегрузки или короткого замыкания", Unit = "А", IsRequired = false },
        new() { Form = form, Section = secReliability, RowNumber = 7,
                Order = 2, Name = "Суммарное время нахождения под током", Unit = "ч", IsRequired = false },

        // ---- Условия эксплуатации
        new() { Form = form, Section = secEnv, RowNumber = 8,
                Order = 1, Name = "Температура окружающей среды (обмотки)", Unit = "°С", IsRequired = false },

        // ---- Коэффициент нагрузки
        new() { Form = form, Section = secLoad, RowNumber = 9,
                Order = 1, Name = "Коэффициент нагрузки", Unit = "", IsRequired = false },
    };

                form.Parameters.AddRange(parameters);

                // Добавляем два столбца: "в схеме" и "по НТД"
                form.ValueColumns.AddRange(new[]
                {
        new FormValueColumn { Form = form, Key = "scheme", Title = "в схеме", Source = ColumnSource.ManualScheme, Order = 1 },
        new FormValueColumn { Form = form, Key = "ntd",    Title = "по НТД",  Source = ColumnSource.DatasheetNdt, Order = 2 },
    });

                db.Forms.Add(form);
                return (form);
        }



}




