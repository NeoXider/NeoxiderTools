# PhysicsEvents3D

**Что это:** 3D-версия PhysicsEvents2D: перехватывает OnTriggerEnter, OnCollisionEnter и др., пробрасывает в UnityEvent. Фильтры: опционально по тегу (`filterByTag` + `requiredTag`) и по слою (`filterByLayer` + `layers`); при включённых обоих проверяются **и тег, и слой**. Пространство имён `Neo.Tools`, файл `Scripts/Tools/InteractableObject/PhysicsEvents3D.cs`.

**Как использовать:** добавить на объект с Collider (триггер или Rigidbody); подписаться на onTriggerEnter/onCollisionEnter и др. в инспекторе.

---

## 2. Описание класса

### PhysicsEvents3D
- **Пространство имен**: `Neo.Tools`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Tools/InteractableObject/PhysicsEvents3D.cs`

**Описание**
Компонент-прослушиватель, который перехватывает стандартные сообщения 3D-физики от Unity (такие как `OnTriggerEnter`) и преобразует их в публичные события `UnityEvent`, которые можно настроить в инспекторе.

**Ключевые поля**
- `interactable`: Позволяет временно включить или отключить обработку всех событий.
- `filterByTag` (`bool`): Включить проверку тега. Если включено и `requiredTag` непустой, другой объект должен иметь этот тег.
- `filterByLayer` (`bool`, по умолчанию включено): Включить проверку слоя по маске `layers`.
- `layers` (`LayerMask`): Маска слоёв; учитывается только при `filterByLayer = true`.
- `requiredTag` (`string`): Тег; учитывается только при `filterByTag = true` и непустой строке.

Если обе галочки выключены, фильтрации нет (события для любых контактов). Если обе включены, проходят только объекты, подходящие **и** по тегу, **и** по слою.

**Unity Events**

- **События триггера (Trigger events)**: Срабатывают, когда другой коллайдер входит в триггерную зону этого объекта (`Is Trigger` на коллайдере должен быть включен).
  - `onTriggerEnter` (`UnityEvent<Collider>`)
  - `onTriggerStay` (`UnityEvent<Collider>`)
  - `onTriggerExit` (`UnityEvent<Collider>`)

- **События столкновения (Collision events)**: Срабатывают, когда другой коллайдер физически сталкивается с этим объектом (требует наличия `Rigidbody`).
  - `onCollisionEnter` (`UnityEvent<Collision>`)
  - `onCollisionStay` (`UnityEvent<Collision>`)
  - `onCollisionExit` (`UnityEvent<Collision>`)

---

## 3. Как использовать

1.  Добавьте компонент `PhysicsEvents3D` на `GameObject`, у которого уже есть `Collider` (например, `BoxCollider`, `SphereCollider`).
2.  **Для событий триггера**: Убедитесь, что у вашего `Collider` включена опция `Is Trigger`.
3.  **Для событий столкновения**: Убедитесь, что на вашем объекте также есть компонент `Rigidbody`.
4.  (Опционально) Включите `filterByTag` и/или `filterByLayer` и задайте `requiredTag` и/или маску `layers`.
5.  В инспекторе выберите нужное событие (например, `onCollisionEnter`) и нажмите `+`, чтобы добавить действие. Перетащите объект, метод которого вы хотите вызвать, и выберите сам метод.
