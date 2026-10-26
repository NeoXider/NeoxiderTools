# HtmlToUitk — HTML/CSS → Unity UI Toolkit

Канонический CLI NeoxiderTools: валидирует HTML/CSS и создаёт UXML/USS, localization keys, media/navigation/animation specs и проверяемые baked gradient PNG. Живые labels/кнопки остаются native UITK controls; пользовательские C# actions подключаются через runtime API.

Начать с [Production.md](docs/Production.md): build/test/publish/convert команды, fixture, runtime contract и ограничения. Канонический Unity-вход — [HTML/CSS → UI Toolkit](../../Assets/Neoxider/Docs/UI/HtmlToUitk.md), текущие результаты и native images — [Verification 11](../../Assets/Neoxider/Docs/UI/HtmlToUitkVerification11.md). [Provenance.md](docs/Provenance.md) и [source-final.json](docs/source-final.json) сохраняют свой исторический source/reuse scope.

Отдельная portable-поставка называется **HtmlToUitk.Cli.v11.0.0.zip**. Unity Core/Complete/UPM не содержат `Tools` и CLI. Для zip нужен **.NET 10 runtime**: приложение сохраняет target net9.0, а runtimeconfig задаёт **LatestMajor**; проверенный запуск вне репозитория на.NET 10.0.9 завершился exit0 без RollForward ENV. Runtime9 не включён и его запуск не заявляется. SDK 10.0.301 проверен для сборки net9 target; весь publish-каталог сохраняют рядом с DLL.

```powershell
dotnet build Tools/HtmlToUitk/HtmlToUitk.sln
dotnet test Tools/HtmlToUitk/HtmlToUitk.sln
dotnet run --project Tools/HtmlToUitk/src/HtmlToUitk.Cli -- run Tools/HtmlToUitk/fixtures/production-neutral/neutral.htujob.json
```

Свежий Round2 полного CLI solution: **2573 Passed / 30 NotExecuted / 0 Failed**;199 real capability cases прошли с SDK 6.6 managed/UnityEngine inputs. Отдельный Root pure logic: **291/291,0 skipped**. Прежние2559/32 относятся к предыдущему запуску, не к текущему итогу. Остаются7 validate-only,22 open-golden и1 S08 expected-diagnostic случая; они не считаются PASS. Для source net9 verification использовался process-local Major на.NET 10.0.9; net8 — runtime8.0.21.

Output поддерживает media width/height/aspect, nav/modal/tabs, keyframes/reduced motion и ограниченный static gradient bake. IR01 устраняет filename/CSS/canonical-asset collisions без тихой потери разных outputs. Неподдержанная dynamic geometry отказана с nonzero. C# `preview`/`compare` пока unsupported nonzero; Python oracle альтернативы описаны в Production.

Unity runtime/editor находится в Assets/Neoxider/Scripts/UI/HtmlToUitk. Round3 полной Unity assembly: **370/370,0 skipped на каждом6000.3.14f1/6000.6.0f1**, включая real three external CLI runs/native import/Play UIDocument. OS manual проверкаUnity 6.3 при1280×720 дала settingsClicks=1/saveClicks=1/no screen error; подробности по ссылке выше. Это не full JS/browser parity, не все разрешения и не mobile/player certification. Четыре старых диагностических csproj остаются machine-specific. Финальная проверка экспортированных release-архивов — отдельный gate Root; публикация здесь не заявляется.
