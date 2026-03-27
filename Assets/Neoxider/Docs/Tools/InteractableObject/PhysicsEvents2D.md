# PhysicsEvents2D

**Что это:** компонент, перехватывающий сообщения 2D-физики (OnTriggerEnter2D, OnCollisionEnter2D и др.) и пробрасывающий их в UnityEvent. Фильтры: опционально по тегу (`filterByTag` + `requiredTag`) и по слою (`filterByLayer` + `layers`); при включённых обоих проверяются **и тег, и слой**. Пространство имён `Neo.Tools`, файл `Scripts/Tools/InteractableObject/PhysicsEvents2D.cs`.

**Как использовать:** добавить на объект с Collider2D (триггер или Rigidbody2D); подписаться на onTriggerEnter/onCollisionEnter и др. в инспекторе. При необходимости задать layers и requiredTag.

---

## 2. Описание класса

### PhysicsEvents2D
- **Пространство имен**: `Neo.Tools`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Tools/InteractableObject/PhysicsEvents2D.cs`

**Описание**
Компонент-прослушиватель, который перехватывает стандартные сообщения 2D-физики от Unity (такие как `OnTriggerEnter2D`) и преобразует их в публичные события `UnityEvent`, которые можно настроить в инспекторе.

**Ключевые поля**
- `interactable`: Позволяет временно включить или отключить обработку всех событий.
- `filterByTag` (`bool`): Включить проверку тега. Если включено и `requiredTag` непустой, другой объект должен иметь этот тег.
- `filterByLayer` (`bool`, по умолчанию включено): Включить проверку слоя по маске `layers`.
- `layers` (`LayerMask`): Маска слоёв; учитывается только при `filterByLayer = true`.
- `requiredTag` (`string`): Тег; учитывается только при `filterByTag = true` и непустой строке.

Если обе галочки выключены, фильтрации нет. Если обе включены, проходят только объекты, подходящие **и** по тегу, **и** по слою.

**Unity Events**

- **События триггера (Trigger events)**: Срабатывают, когда другой коллайдер входит в триггерную зону этого объекта (`Is Trigger` на коллайдере должен быть включен).
  - `onTriggerEnter` (`UnityEvent<Collider2D>`)
  - `onTriggerStay` (`UnityEvent<Collider2D>`)
  - `onTriggerExit` (`UnityEvent<Collider2D>`)

- **События столкновения (Collision events)**: Срабатывают, когда другой коллайдер физически сталкивается с этим объектом (требует наличия `Rigidbody2D`).
  - `onCollisionEnter` (`UnityEvent<Collision2D>`)
  - `onCollisionStay` (`UnityEvent<Collision2D>`)
  - `onCollisionExit` (`UnityEvent<Collision2D>`)

---

## 3. Как использовать

1.  Добавьте компонент `PhysicsEvents2D` на `GameObject`, у которого уже есть `Collider2D`.
2.  **Для событий триггера**: Убедитесь, что у вашего `Collider2D` включена опция `Is Trigger`.
3.  **Для событий столкновения**: Убедитесь, что на вашем объекте также есть компонент `Rigidbody2D`.
4.  (Опционально) Включите `filterByTag` и/или `filterByLayer` и задайте `requiredTag` и/или маску `layers`.
5.  В инспекторе выберите нужное событие (например, `onTriggerEnter`) и нажмите `+`, чтобы добавить действие. Перетащите объект, метод которого вы хотите вызвать, и выберите сам метод.
