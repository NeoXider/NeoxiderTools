# AnimatorParameterDriver

Компонент для **удобного изменения параметров Animator** из кода и через UnityEvent: триггер, bool, float, int. Имя параметра можно задать в переменной в инспекторе — тогда метод принимает только значение (удобно для UnityEvent).

## Поля

| Поле | Описание |
|------|----------|
| **Animator** | Целевой Animator. Если не задан — берётся с этого объекта. |
| **Trigger Parameter Name** | Имя триггера для метода **SetTrigger()** без аргумента. |
| **Bool Parameter Name** | Имя bool-параметра для **SetBool(bool)**. |
| **Float Parameter Name** | Имя float-параметра для **SetFloat(float)**. |
| **Int Parameter Name** | Имя int-параметра для **SetInt(int)**. |

## API

**Методы с именем в переменной** (имя задаётся в инспекторе, метод — только значение):

| Метод | Описание |
|-------|----------|
| **SetTrigger()** | Вызвать триггер из поля Trigger Parameter Name. |
| **SetBool(bool value)** | Установить bool по полю Bool Parameter Name. |
| **SetFloat(float value)** | Установить float по полю Float Parameter Name. |
| **SetInt(int value)** | Установить int по полю Int Parameter Name. |

**Методы с именем в аргументе**:

| Метод | Описание |
|-------|----------|
| **SetTrigger(string triggerName)** | Вызвать триггер по имени. |
| **SetBool(string parameterName, bool value)** | Установить bool-параметр. |
| **SetBoolTrue(string parameterName)** / **SetBoolFalse(string parameterName)** | Установить bool = true/false. |
| **SetFloat(string parameterName, float value)** | Установить float-параметр. |
| **SetInt(string parameterName, int value)** | Установить int-параметр. |

## Пример: имя в переменной

В инспекторе задайте **Trigger Parameter Name** = `"Attack"`. Тогда из кнопки или кода можно вызвать **SetTrigger()** без аргументов — сработает триггер "Attack". Аналогично: **Bool Parameter Name** = `"IsRunning"` и вызов **SetBool(bool)** — в UnityEvent передаёте только значение (true/false).

```csharp
driver.SetTrigger();           // триггер из triggerParameterName
driver.SetBool(true);          // bool из boolParameterName
driver.SetFloat(1.5f);         // float из floatParameterName
driver.SetInt(2);              // int из intParameterName
```

## Пример: имя в аргументе

```csharp
driver.SetTrigger("Attack");
driver.SetBool("IsRunning", true);
driver.SetFloat("Speed", 1.5f);
driver.SetInt("State", 2);
```

Имена параметров должны совпадать с именами в окне Animator (Parameters).
