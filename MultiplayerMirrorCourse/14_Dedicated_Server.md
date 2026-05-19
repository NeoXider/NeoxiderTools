# Урок 14: dedicated server и headless запуск

**Навигация:** [Оглавление](README.md) · [Старт](00_START_HERE.md) · [Оформление](LESSON_STYLE.md) · базовый трек · урок 14/15 · Mirror `96.x`

| Ключевые слова | dedicated, headless, `UNITY_SERVER`, port, command line |
|----------------|---------------------------------------------------------|

---

## Карта урока

| Что | Ответ |
|-----|-------|
| Объект работы | Server build, CLI bootstrap, `SERVER_RUN.md`. |
| Кто владеет state | Dedicated server владеет матчем без локального player/UI. |
| Как проверить | Запустить server из CLI и подключить отдельный client build. |
| Артефакт | Команда запуска, READY log, port/transport contract. |

---

## Что должно получиться

Вы можете запустить server build без UI, передать порт из командной строки и подключиться отдельным client build.

---

## Проблема

Если сервер стартует только по кнопке в `NetworkManagerHUD`, это не dedicated server. Хостинг и CI не будут нажимать кнопки.

---

## Теория коротко

Dedicated server:

- не имеет локального игрока;
- не нуждается в камерах, меню, постобработке;
- должен стартовать из CLI;
- пишет понятные логи;
- корректно завершает матч при stop/signal.

В Unity server build используйте актуальный dedicated/server target для вашей версии Unity. `UNITY_SERVER` помогает отключать клиентские части.

В актуальной Unity Manual Dedicated Server build создаётся через Build Profiles, scripting (`StandaloneBuildSubtarget.Server`) или command line (`-standaloneBuildSubtarget Server`). Dedicated Server target оптимизирует build под CPU, memory и disk, поэтому он лучше обычного headless client build для хостинга.

---

## Практика

Bootstrap:

```csharp
using System;
using Mirror;
using UnityEngine;

public sealed class ServerBootstrap : MonoBehaviour
{
    [SerializeField] NetworkManager manager;

    void Start()
    {
#if UNITY_SERVER
        int port = ReadPort(7777);
        ConfigureTransportPort(port);
        manager.StartServer();
        Debug.Log($"[SERVER] READY port={port}");
#endif
    }

    static int ReadPort(int fallback)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "-port" && int.TryParse(args[i + 1], out int port))
                return port;

        return fallback;
    }

    static void ConfigureTransportPort(int port)
    {
        // Настройте конкретный transport вашего проекта.
    }
}
```

Отключение UI:

```csharp
#if UNITY_SERVER
Destroy(clientOnlyUiRoot);
#endif
```

---

## Проверка себя

- Server build стартует из командной строки.
- В логе есть `[SERVER] READY port=...`.
- Client подключается по IP/port.
- UI/camera/audio не расходуют ресурсы на сервере.
- Занятый порт даёт понятную ошибку.

---

## Минимальная диагностика

| Симптом | Что проверить |
|---------|---------------|
| Server ждёт кнопку | `NetworkManagerHUD` остался частью запуска, нет bootstrap. |
| Client не подключается | IP, port, protocol UDP/TCP, firewall, transport port. |
| В CI код падает | `UNITY_SERVER` ветка не компилировалась локально. |
| Server расходует лишние ресурсы | Камеры/audio/UI/client-only systems не отключены. |

---

## Частые ошибки

- Server build всё ещё ждёт кнопку UI.
- Порт захардкожен.
- Открыт неправильный протокол firewall.
- В `#if UNITY_SERVER` ветках код ни разу не компилировался в CI.
- Нет server logs.

---

## Лайфхаки

- Документируйте точную команду запуска в `SERVER_RUN.md`.
- Сначала проверьте server на локальной машине, потом на VPS/облаке.
- Для health/ready хостингу нужна простая строка в логе или health endpoint.
- Версию server build связывайте с клиентской версией.

---

## Профессиональный минимум

- Server запускается одной командой без Editor и UI.
- Port задаётся параметром окружения/CLI, а не только inspector.
- READY/STOP/DISCONNECT логи пригодны для CI и хостинга.
- Client/server version mismatch обрабатывается до матча.

---

## Домашнее задание

Соберите dedicated server и заполните `SERVER_RUN.md`:

- команда запуска;
- порт и transport;
- пример READY-лога;
- как подключается client;
- что происходит при занятом порту.
