namespace OperationMaps.Domain.Enums;

public enum UserRole { User = 0, Admin = 1 }

public enum ColumnGrouping { PerComponent = 0, PerRegimeGroup = 1 }

public enum ColumnSource
{
  ManualScheme = 0, // "в схеме" - ввод вручную
  DatasheetNdt = 1, // "по НТД" - из *NtdValue
  ManualPins = 2 // "№ выводов" - ввод вручную, дефолт из ComponentPinValue
}

public enum MatchStatus { Unresolved = 0, Matched = 1, Manual = 2 }
