# Чек-лист: добавление map.json для новой формы

Проверено на Form68 (нашли и исправили 6 багов координат) и Form83/Form86
(написаны с нуля). Главное правило: **никогда не угадывать координаты
ячеек по внешнему виду документа в Word** — из-за объединённых ячеек и
переменного числа подписей в разных строках визуальный номер колонки
почти никогда не совпадает с физическим индексом ячейки, который использует
`WordTableHelper`/`map.json`. Вместо этого — вскрывать реальную структуру
через OpenXML.

## 1. Собрать вводные

- В `DatabaseSeeder.cs` найти `AddFormNN(...)` — список `FormParameter`
  (RowNumber, Name, Unit), секции, `FormValueColumn` (обычно "в схеме" +
  "по НТД"; если форма поддерживает "номера выводов" по параметру — см.
  Form64 отдельно).
- Убедиться, что `template.docx` уже лежит в
  `src/OperationMaps.Infrastructure/Word/Resources/FormNN/`.

## 2. Вскрыть реальную структуру таблицы

Временно добавить диагностический тест (потом удалить!) в
`tests/OperationMaps.Tests/Word/`:

```csharp
[Fact]
public void Diag()
{
  var path = Path.Combine(AppContext.BaseDirectory, "Word", "Resources", "FormNN", "template.docx");
  using var doc = WordprocessingDocument.Open(path, false);
  var table = doc.MainDocumentPart!.Document.Body!.Elements<Table>().First();
  var rows = table.Elements<TableRow>().ToList();
  var msgs = rows.Select((r, i) =>
  {
    var cells = r.Elements<TableCell>().ToList();
    return $"row{i}(1-based {i + 1}) cellCount={cells.Count} texts=[{string.Join(" | ", cells.Select(c => c.InnerText))}]";
  });
  Assert.Fail(string.Join("\n", msgs));
}
```

`dotnet test --filter "FullyQualifiedName~Diag"` — `Assert.Fail` выведет
всё в сообщении об ошибке. Для каждой строки получаем: 1-based номер
строки в документе, физическое количество ячеек, их текст.

## 3. Понять раскладку данных по слотам

Для строки с параметром: `prefix = cellCount − componentsPerPage × N`
(N=2 для scheme+ntd, N=3 если есть ещё колонка pins). Данные начинаются
с индекса `prefix` (0-based) и идут блоками по N на слот, в порядке
(scheme, ntd[, pins]) — так же, как в заголовке "в схеме | по НТД".

**`prefix` меняется от строки к строке**, если у параметра составная
подпись (несколько ячеек-label перед номером строки) — не считать, что
он одинаков для всех строк формы. Так и рождаются баги: если скопировать
координаты соседней строки без пересчёта `prefix`.

Строка `noteCell` физически **не обязана** быть той же строкой, что
последний параметр — в Form68 это была отдельная следующая строка. Всегда
искать текст "Примечание" в дампе и проверять его собственный `prefix`.

Если в шаблоне есть строка вида "Номера выводов" (метаданные компонента,
не привязанные к конкретному параметру) — под неё, скорее всего, нет
поля в `WordComponentData`/`ComponentSlotMap.MetaCells`. Не выдумывать
новое поле по ходу дела — либо оставить ячейку незамапленной, либо
завести это отдельной задачей.

## 4. Написать map.json

- `componentsPerPage` = сколько слотов в реальной таблице (обычно 3).
- `metaCells` — только те ключи (`componentName`, `componentType`,
  `quantity`, `positionsNumber`), для которых в шаблоне реально есть
  отдельная строка. Не добавлять "на всякий случай".
- `parameterCells["RowNumber"]` — `row` (1-based), `ntdCol`/`schemeCol`
  (1-based) по формуле из шага 3. Старый формат `col` (без ntd/scheme
  разделения) — только если у формы нет колонки "в схеме" вообще.
- `noteCell` — если в шаблоне есть строка "Примечание".
- `headerReplacements` — обычно одинаковый для всех форм:
  `{{designation}}`/`{{sheet}}`/`{{totalSheets}}`.

## 5. Проверить round-trip тестом

Добавить номер формы в `[InlineData("NN")]` общего теста
`Export_then_import_round_trips_simple_forms` в
`tests/OperationMaps.Tests/Word/WordServiceTests.cs`. Он сам берёт
координаты из загруженного `map.json` (не из твоей головы), пишет разные
значения в каждый слот и проверяет, что при чтении назад ничего не
потерялось и не попало в чужой слот — именно так были найдены баги
Form4 и Form68.

`dotnet test` — если тест падает на конкретном номере строки/слота,
вернуться к дампу из шага 2 и пересчитать `prefix` для этой строки.

## 6. Убрать временное, закоммитить

- Удалить диагностический тест (`Diag`).
- Один коммит: `map.json` + добавление в `InlineData` (плюс фикс, если
  форма уже существовала и была сломана).
