# PhysicsEvents3D

**Что это:** 3D-версия PhysicsEvents2D: перехватывает OnTriggerEnter, OnCollisionEnter и др., пробрасывает в UnityEvent. Фильтры: LayerMask, requiredTag. Пространство имён `Neo.Tools`, файл `Scripts/Tools/InteractableObject/PhysicsEvents3D.cs`.

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
- `layers` (`LayerMask`): Фильтр по слоям. События будут срабатывать только для объектов на выбранных слоях.
- `requiredTag`: Фильтр по тегу. Если поле не пустое, события будут срабатывать только для объектов с указанным тегом.

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
4.  (Опционально) Настройте фильтры `layers` и `requiredTag`, чтобы реагировать только на определенные объекты.
5.  В инспекторе выберите нужное событие (например, `onCollisionEnter`) и нажмите `+`, чтобы добавить действие. Перетащите объект, метод которого вы хотите вызвать, и выберите сам метод.
