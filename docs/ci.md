# Проверка сборки на Windows

[Windows CI](../.github/workflows/windows-ci.yml) запускается при push,
pull request и вручную через GitHub Actions. Проверяется вся OperationMaps.slnx
в конфигурации Release на windows-latest с .NET SDK 10.0.x.

Шаги: восстановление NuGet → сборка → тесты без повторной сборки.
Ошибка любого шага делает проверку неуспешной. Новый запуск той же ветки
отменяет предыдущий. Тайм-аут задания — 20 минут.

Артефакт windows-release-checks хранит build.log и результаты TRX 14 дней,
включая случаи неуспешной сборки или тестов. Отменённые запуски его не
загружают. Проверка не собирает релизный архив приложения.

Тесты требуют ресурсы Word, XML-примеры из xml/ и документ .741,
указанный в OperationMaps.Tests.csproj. Они должны присутствовать в checkout;
файлы только из локальной рабочей папки на runner не попадут.
В артефакт проверки входят только перечисленные log/TRX, без исходных
документов и пользовательских баз.

Локально на Windows выполнить те же шаги:

```powershell
dotnet restore OperationMaps.slnx
dotnet build OperationMaps.slnx --configuration Release --no-restore --verbosity minimal -p:WarningsAsErrors=nullable "-flp:logfile=artifacts/ci/build.log;verbosity=normal"
dotnet test OperationMaps.slnx --configuration Release --no-build --no-restore --verbosity minimal --logger "trx;LogFileName=tests.trx" --results-directory artifacts/ci/test-results
```

Наличие workflow в рабочей папке ещё не означает успешный прогон в GitHub.
Первая проверка runner выполняется после отправки изменений в репозиторий.
Nullable-предупреждения компилятора в CI считаются ошибками сборки.
После исправлений R0 локальная полная Release-пересборка проходит без
предупреждений; правило не позволяет вернуть те же проблемы незаметно.

Используемые действия и их параметры:
[checkout v4](https://github.com/actions/checkout/tree/v4),
[setup-dotnet v4](https://github.com/actions/setup-dotnet/tree/v4),
[upload-artifact v4](https://github.com/actions/upload-artifact/tree/v4).
