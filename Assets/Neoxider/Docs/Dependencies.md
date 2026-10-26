# Dependencies and editor tooling / Зависимости и инструменты

NeoxiderTools targets Unity `6000.0+`. Runtime package requirements are declared in `package.json`;
the repository's development project has additional editor tools in `Packages/manifest.json`.
MCP is an optional development tool and is not a dependency of the published runtime library.

## Declared dependency baseline

These declared pins were selected from official stable releases/registry metadata on 26 October 2026.
Windows validation recorded on 26–27 October 2026 and its scope/limits are listed in [Verification 11](../Verification11.md).
Manifest requests and native resolved versions are distinct: editor-bundled packages follow the chosen SDK.
Future dependency changes require native resolution and affected regression/scene checks; editing a
manifest alone does not establish compatibility.

For Mirror components assembled from C#, configure the GameObject while inactive, add its identity and
all NetworkBehaviours, then activate it. Mirror initializes behaviour identity caches through native
`Awake`; adding behaviours after an active identity has already initialized is not equivalent. Test
dispatchers must use Mirror's public `GetStableHashCode16()` for woven function signatures rather than
manually truncating the32-bit hash.

| Dependency | Version | Scope |
|---|---|---|
| AI Navigation | `2.0.16` | Runtime package; Unity minimum `6000.0` |
| Input System | `1.20.1` | Runtime package; Unity minimum `6000.0` |
| uGUI / TextMeshPro | `2.0.0` minimum | Runtime package; newer editor-bundled versions follow the Unity SDK |
| UniTask | `2.5.11` | Bundled development-project vendor; install separately in a consumer |
| Mirror | `96.11.3` | Optional networking vendor in the development project |
| MCP for Unity | `10.3.0` | Optional Unity editor package and matching Python server |
| Burst | `1.8.31` | Development project; the editor may resolve its bundled compatible version |
| Asset Manager | `1.12.0` | Development editor tooling |
| Version Control integration | `2.13.6` | Development editor tooling |
| Timeline | `1.8.13` | Development project; editor-bundled compatibility applies |
| Visual Scripting | `1.9.12` | Optional development editor tooling |

Use Unity Package Manager to resolve and write `packages-lock.json`. Keep URP, Shader Graph, VFX Graph,
Test Framework and other editor-bundled packages compatible with the chosen Unity SDK. Registry
`dist-tags.latest` can be older than an editor-bundled version; it is not a reason to downgrade it.

Official sources: [Unity package registry](https://packages.unity.com),
[UniTask release](https://github.com/Cysharp/UniTask/releases/tag/2.5.11),
[Mirror release](https://github.com/MirrorNetworking/Mirror/releases/tag/v96.11.3),
[MCP release](https://github.com/CoplayDev/unity-mcp/releases/tag/v10.3.0).

## DOTween Core and Pro must be upgraded together

The current development checkout contains DOTween Core `1.2.765` with Pro `1.0.381`.
The official updater requires Pro `1.0.405` or newer before installing the newer free Core.
Keep this working pair until the licensed Pro update is available, then use the official setup procedure
and recheck the tween lifecycle/animation regressions. Do not fetch a commercial Pro DLL from an
unofficial mirror. See [Demigiant's upgrade instructions](https://dotween.demigiant.com/download.php).

Pro is optional for NeoxiderTools consumers. Core package prefabs must not contain missing Pro scripts;
the loading spinner uses the package's ordinary `ConstantRotator` with a three-second rotation period.

## MCP for Unity

Pin the editor package to a stable release:

```text
https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#v10.3.0
```

Install the matching server using `uv`:

```text
uv tool install mcpforunityserver==10.3.0
mcp-for-unity --transport http --http-url http://127.0.0.1:8040
```

Configure the MCP client for `http://127.0.0.1:8040/mcp`, connect the intended Unity editor instance and
check the instance/editor-state resources before issuing changes. A running HTTP server does not prove
an editor is connected. Select the exact instance when more than one project is open. Restart/reconnect
the client connection if it retained an earlier startup failure. See the
[official MCP installation guide](https://coplaydev.github.io/unity-mcp/getting-started/install).

The developer project omits the retired `com.unity.modules.vr` dependency so it also opens in Unity 6.6.
The supported `com.unity.modules.xr` dependency remains enabled.

## По-русски

Из проекта разработки убрана устаревшая зависимость `com.unity.modules.vr`, которая мешала открытию
в Unity 6.6. Поддерживаемый модуль `com.unity.modules.xr` остаётся включённым.

Обновляйте через официальные источники и фиксируйте `packages-lock.json` после разрешения зависимостей
в Unity. Версии URP/Shader Graph/VFX/Test Framework должны соответствовать редактору, а не просто
числу в registry `latest`. После обновления ввода, Mirror, UniTask и анимаций повторяйте затронутые
проверки и игровые сценарии. Записанные результаты Windows-проверок 26–27 октября 2026 и их ограничения —
в [Verification 11](../Verification11.md); они не являются сертификацией всех Unity 6 и платформ.

MCP состоит из Unity-пакета и Python-сервера; здесь обе части закреплены на `10.3.0`. Успешное
HTTP-рукопожатие проверяет сервер, но для работы со сценами нужно отдельное подключение редактора.
DOTween Core и старую Pro нельзя обновлять независимо: сначала нужна лицензированная новая Pro.
Подробнее об установке сэмплов и их зависимостях — в [Samples](Samples.md).
