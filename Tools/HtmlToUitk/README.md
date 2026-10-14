# HtmlToUitk — HTML/CSS → Unity UI Toolkit

Канонический CLI NeoxiderTools: валидирует HTML/CSS и создаёт UXML/USS, localization keys, media/navigation/animation specs и проверяемые baked gradient PNG. Живые labels/кнопки остаются native UITK controls; пользовательские C# actions подключаются через runtime API.

Начать с [Production.md](docs/Production.md): реальные build/test/publish/convert команды, fixture, runtime contract и ограничения. Происхождение: [Provenance.md](docs/Provenance.md). Для root review: [source-final.json](docs/source-final.json), где byte-identical reuse отделён от изменений.

```powershell
dotnet build Tools/HtmlToUitk/HtmlToUitk.sln
dotnet test Tools/HtmlToUitk/HtmlToUitk.sln
dotnet run --project Tools/HtmlToUitk/src/HtmlToUitk.Cli -- run Tools/HtmlToUitk/fixtures/production-neutral/neutral.htujob.json
```

Текущая проверка:2559passed/32historicalskipped/0failed; новый actual CLI fixture19/19 безskip. Output поддерживает media width/height/aspect, nav/modal/tabs, keyframes/reduced motion, linear/radial/conic/repeating/alpha. Static gradient geometry имеет явные границы, dynamic unsupported cases отказаны с nonzero. C#preview/compare пока unsupported nonzero, проверенные Python oracle альтернативы документированы.

Unity runtime/editor расположен в Assets/Neoxider/Scripts/UI/HtmlToUitk. Native import/render/actions/reload/visual QA — отдельный gate, dotnet PASS его не заменяет. Full JS/browser CSS parity не обещается. Compile DLL/bin/obj/generated QA outputs не являются исходной поставкой.
