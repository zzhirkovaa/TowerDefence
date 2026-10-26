# Как работает ECS в моём проекте Tower Defense

Этот файл описывает **текущую версию первой практической**, а не план итоговой игры. Кодовые листинги ниже скопированы целиком из проекта. Читай разделы по порядку: сначала смысл ECS, затем подготовка объектов, затем выполнение систем во время игры.

## 1. Зачем здесь ECS

Идея итогового проекта — tower defense с большим количеством похожих врагов. У каждого врага есть однотипные данные: позиция, скорость, стамина. Ко всем врагам применяются одни и те же правила движения и восстановления стамины. ECS позволяет хранить эти данные отдельно от правил и обрабатывать подходящие сущности общими системами.

В обычном подходе Unity на каждом враге мог бы висеть `MonoBehaviour` со своим `Update()`. В ECS система движения одна: она перебирает всех врагов с нужными компонентами. Это удобная основа для большого числа однотипных объектов и даёт возможности для оптимизации данных и выполнения кода. **Само наличие ECS не доказывает, что игра уже выдерживает тысячи врагов:** в текущей демонстрации создаётся один враг, теста производительности нет.

Главная практическая причина выбора ECS здесь — соответствие идее будущих массовых волн. Главная учебная цель первой практической — показать, что движение и характеристика врага реализованы именно через компоненты и системы.

## 2. Три слова ECS

| Слово | Простой смысл | Пример в проекте |
|---|---|---|
| **Entity** | Отдельная сущность с собственным идентификатором | Конкретный враг, башня, спавнер |
| **Component** | Данные или метка, прикреплённые к entity | `MoveSpeed`, `Stamina`, `CanMove`, `TowerTarget` |
| **System** | Код, который находит entity по компонентам и выполняет действие | `MoveToTowerSystem`, `StaminaSystem` |

Entity **не является скриптом** и сама по себе не содержит метода «идти». У неё есть набор компонентов. Например, у врага есть скорость и стамина, а у башни — метка цели. Система движения выбирает сущности с подходящим набором данных и меняет их позицию.

Аналогия: entity — строка таблицы, компоненты — её поля, система — правило, которое обрабатывает строки с нужными полями. Аналогия помогает понять идею, но внутри Unity есть свои структуры хранения и правила доступа.

## 3. GameObject, SubScene, Authoring и Baker: что происходит до запуска

В редакторе Unity стоят привычные **GameObject**. Башня и спавнер расположены в `Gameplay` SubScene. В главной сцене `SampleScene` находится объект SubScene, который подгружает `Gameplay`. Враг настроен как префаб `Enemy.prefab`, ссылка на него указана у спавнера.

`...Authoring` — обычный `MonoBehaviour`, который висит на GameObject и задаёт настройки в Inspector. `...Baker` — класс, чей метод `Bake` Unity вызывает при подготовке ECS-данных. Этот метод говорит Unity, какие ECS-компоненты должны получить соответствующие сущности. Процесс называется **baking**, или «запекание».

Важно не путать этапы: **Authoring хранит настройки, Baker переносит их в ECS, System работает во время игры.** `GetEntity(...)` возвращает идентификатор ECS-сущности, связанной с данным GameObject при baking. `AddComponent(entity, ...)` добавляет данные именно этой сущности. Это не отдельный «второй враг».

`TransformUsageFlags.Dynamic` сообщает baking-системе, что сущности нужен трансформ для положения и возможного движения. Положение ECS-сущности затем хранится в `LocalTransform`. У башни в этой работе меняется положение в редакторе до Play; система движения читает её ECS-позицию при работе.

Визуальные `MeshRenderer`/`MeshFilter` и ECS-логика — разные стороны объекта: одни отвечают за отображение, другие — за данные и поведение. Для текущей схемы башня не атакует, а служит целью.

### Какие библиотеки используются

Проект создан в Unity `6000.6.0f1`, сцена трёхмерная, рендеринг настроен через URP. Пакет `Unity.Entities` даёт `Entity`, `IComponentData`, `Baker`, `ISystem` и `SystemAPI`; `Unity.Transforms` даёт `LocalTransform`; `Unity.Mathematics` — `float3`, `math`, `quaternion`; Burst используется для компиляции помеченных систем. В проекте также установлены `Entities Graphics` и другие пакеты Unity. Наличие пакетов физики и навигации **не означает**, что в этой практической уже реализованы столкновения или поиск пути.

## 4. Подготовка трёх типов объектов

### 4.1. Башня: метка цели

`TowerAuthoring` пустой, потому что на башне не нужно настраивать числа. Его наличие сообщает Baker, что эту башню следует пометить как цель. `TowerBaker` получает связанную с башней entity и добавляет `TowerTarget`. Это метка без полей: она отвечает на вопрос «какая сущность является башней?».

**Полный код `Assets/Scripts/Authoring/TowerAuthoring.cs`:**

```csharp
using Unity.Entities;
using UnityEngine;

/// <summary>Помечает башню как цель врага.</summary>
public class TowerAuthoring : MonoBehaviour
{
}

class TowerBaker : Baker<TowerAuthoring>
{
    public override void Bake(TowerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent<TowerTarget>(entity);
    }
}
```

Здесь `GetEntity(...)` относится **к тому GameObject, на котором висит `TowerAuthoring`**. `AddComponent<TowerTarget>(entity)` прикрепляет метку к полученной ECS-сущности. Позднее система найдёт башню по этой метке и прочитает её положение.

### 4.2. Враг: числовые настройки и начальное состояние

Поля `EnemyAuthoring` можно менять в Inspector префаба. `EnemyBaker` переносит их в `MoveSpeed` и `Stamina`. Он также добавляет включённый `CanMove`, поэтому новый враг изначально может идти.

**Полный код `Assets/Scripts/Authoring/EnemyAuthoring.cs`:**

```csharp
using Unity.Entities;
using UnityEngine;

public class EnemyAuthoring : MonoBehaviour
{
    public float Speed = 3f;
    public float ReachDistance = 0.35f;
    public float MaxStamina = 2f;
    public float StaminaDrainPerSecond = 2.5f;
    public float StaminaRegenPerSecond = 2f;
}

class EnemyBaker : Baker<EnemyAuthoring>
{
    public override void Bake(EnemyAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);

        AddComponent(entity, new MoveSpeed
        {
            Value = authoring.Speed,
            ReachDistance = authoring.ReachDistance
        });
        AddComponent(entity, new Stamina
        {
            Current = authoring.MaxStamina,
            Max = authoring.MaxStamina,
            DrainPerSecond = authoring.StaminaDrainPerSecond,
            RegenPerSecond = authoring.StaminaRegenPerSecond,
            MinToMove = authoring.MaxStamina * 0.5f
        });
        AddComponent<CanMove>(entity);
        SetComponentEnabled<CanMove>(entity, true);
    }
}
```

Что означают важные строки:

- `GetEntity(...)` — получить ECS-сущность, соответствующую префабу врага при baking.
- `Value = authoring.Speed` — скопировать скорость из Inspector в компонент движения.
- `Current = authoring.MaxStamina` — начать с полным запасом стамины.
- `MinToMove = MaxStamina * 0.5f` — после отдыха разрешить движение, когда восстановилась половина запаса.
- `SetComponentEnabled<CanMove>(entity, true)` — включить возможность двигаться в начале.

**Значения `3`, `2` и так далее в C# — значения по умолчанию.** В сохранённом `Enemy.prefab` выставлены другие значения: скорость `2`, расстояние остановки `0.35`, максимум стамины `4`, расход `1.6` в секунду, восстановление `1.1` в секунду. При baking берутся значения из префаба, то есть фактические настройки Inspector.

### 4.3. Спавнер: ссылка на префаб врага

В Inspector объекта `Spawner` указано поле `Prefab`, ссылающееся на `Enemy.prefab`. Baker добавляет сущности спавнера компонент `EnemySpawner`, внутри которого хранится ECS-ссылка на этот префаб.

**Полный код `Assets/Scripts/Authoring/EnemySpawnerAuthoring.cs`:**

```csharp
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Создаёт одного врага из префаба в SubScene.
/// </summary>
public class EnemySpawnerAuthoring : MonoBehaviour
{
    public GameObject Prefab;
}

class EnemySpawnerBaker : Baker<EnemySpawnerAuthoring>
{
    public override void Bake(EnemySpawnerAuthoring authoring)
    {
        if (authoring.Prefab == null)
            return;

        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new EnemySpawner
        {
            Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic)
        });
    }
}
```

Проверка `if (authoring.Prefab == null) return;` означает: без назначенного префаба не добавлять компонент спавнера. `GetEntity(authoring.Prefab, ...)` относится уже **к префабу врага**, а не к самому спавнеру. Так Unity подготавливает ссылку на ECS-префаб, который можно создать во время игры.

Получается три разные сущности/роли: башня — цель, спавнер — место появления и ссылка на префаб, созданный враг — тот, кто движется. Сам префаб — шаблон, из которого создаётся экземпляр врага.

## 5. Какие компоненты существуют

Компонент с `IComponentData` — структура данных ECS. Поля компонента можно читать и изменять в системах. Метка тоже компонент, но без полей. `IEnableableComponent` позволяет включать и выключать компонент без удаления его из сущности.

### `TowerTarget`: кто является целью

**Полный код `Assets/Scripts/Components/TowerTarget.cs`:**

```csharp
using Unity.Entities;

/// <summary>Метка цели, к которой движется враг.</summary>
public struct TowerTarget : IComponentData
{
}
```

У компонента нет полей: факт его наличия обозначает башню. В текущей сцене предполагается **одна** такая башня.

### `MoveSpeed`: как идти и где остановиться

**Полный код `Assets/Scripts/Components/MoveSpeed.cs`:**

```csharp
using Unity.Entities;

/// <summary>
/// Скорость врага и расстояние, на котором он останавливается у цели.
/// </summary>
public struct MoveSpeed : IComponentData
{
    public float Value;
    public float ReachDistance;
}
```

`Value` измеряется в единицах Unity за секунду. `ReachDistance` — дистанция до центра башни, на которой движение прекращается. Например, при `0.35` враг не обязан попасть точно в координату центра.

### `Stamina`: запас сил

**Полный код `Assets/Scripts/Components/Stamina.cs`:**

```csharp
using Unity.Entities;

/// <summary>
/// Стамина. Тратится на бег, плавно восстанавливается.
/// Если Current = 0, враг не может двигаться, пока не отдохнёт.
/// </summary>
public struct Stamina : IComponentData
{
    public float Current;
    public float Max;
    public float DrainPerSecond;
    public float RegenPerSecond;
    public float MinToMove;
}
```

`Current` — текущий запас; `Max` — максимальный; `DrainPerSecond` — расход за секунду движения; `RegenPerSecond` — восстановление за секунду отдыха; `MinToMove` — порог возобновления движения.

### `CanMove`: разрешение двигаться

**Полный код `Assets/Scripts/Components/CanMove.cs`:**

```csharp
using Unity.Entities;

/// <summary>
/// Выключаемый компонент: враг может идти к башне, только пока хватает стамины.
/// </summary>
public struct CanMove : IComponentData, IEnableableComponent
{
}
```

Полей нет. Если компонент **включён**, запрос системы движения выбирает этого врага. Если он выключен, враг остаётся существовать, но система движения его пропускает. Выключение — не смерть, не удаление entity и не удаление компонента.

### `EnemySpawner`: какой префаб создать

**Полный код `Assets/Scripts/Components/EnemySpawner.cs`:**

```csharp
using Unity.Entities;

/// <summary>
/// Префаб врага, которого нужно создать для демонстрации движения.
/// </summary>
public struct EnemySpawner : IComponentData
{
    public Entity Prefab;
}
```

Поле `Prefab` имеет тип `Entity`: это ECS-ссылка на шаблон врага. Это не строка с именем объекта. Из этого шаблона система создаст отдельный экземпляр.

Кроме собственных компонентов, проект использует `LocalTransform` из `Unity.Transforms`. Он хранит положение, поворот и масштаб ECS-сущности. Его код написан Unity, поэтому полного листинга этого библиотечного типа в проекте нет.

## 6. Что происходит при запуске игры — по порядку

1. Unity загружает главную сцену `SampleScene` и её `Gameplay` SubScene.
2. Baking подготавливает ECS-данные: башня получает `TowerTarget`, спавнер — `EnemySpawner`, префаб врага — `MoveSpeed`, `Stamina` и включённый `CanMove`.
3. `EnemySpawnerSystem` находит спавнер, создаёт **одного** врага из ECS-префаба и помещает его в позицию спавнера.
4. `StaminaSystem` следит за стаминой врага и при необходимости переключает `CanMove`.
5. `MoveToTowerSystem` получает текущую позицию башни, выбирает врага, которому разрешено идти, двигает его к башне и уменьшает стамину.
6. Пункты 4–5 повторяются по кадрам. Враг то идёт, то отдыхает, пока не окажется достаточно близко к башне.

Точный кадр, в который после создания врага впервые сработает движение, зависит от порядка систем, но логика всегда использует уже созданную entity. Для `StaminaSystem` явно задан порядок **перед** `MoveToTowerSystem`.

### 6.1. Система создания одного врага

**Полный код `Assets/Scripts/Systems/EnemySpawnerSystem.cs`:**

```csharp
using Unity.Entities;
using Unity.Transforms;

/// <summary>
/// Один раз создаёт врага в позиции спавнера.
/// </summary>
public partial struct EnemySpawnerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EnemySpawner>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var spawner = SystemAPI.GetSingleton<EnemySpawner>();
        var enemy = state.EntityManager.Instantiate(spawner.Prefab);

        var spawnEntity = SystemAPI.GetSingletonEntity<EnemySpawner>();
        var spawnPosition = SystemAPI.GetComponent<LocalTransform>(spawnEntity).Position;
        var transform = state.EntityManager.GetComponentData<LocalTransform>(enemy);
        transform.Position = spawnPosition;
        state.EntityManager.SetComponentData(enemy, transform);
        state.Enabled = false;
    }
}
```

Разбор по действиям:

- `OnCreate` вызывается при создании системы. `RequireForUpdate<EnemySpawner>()` не даёт выполнять `OnUpdate`, пока в мире нет компонента спавнера.
- `GetSingleton<EnemySpawner>()` берёт данные **единственного** спавнера. Здесь предполагается ровно один спавнер.
- `Instantiate(spawner.Prefab)` создаёт отдельную entity врага из шаблона.
- `GetSingletonEntity<EnemySpawner>()` находит entity самого спавнера; её `LocalTransform.Position` используется как место появления.
- `GetComponentData` читает трансформ созданного врага, `SetComponentData` записывает его с новой позицией.
- `state.Enabled = false` выключает систему после первого создания. Поэтому это не генератор волн.

**Важно:** ECS-сущность спавнера и созданная им ECS-сущность врага — разные объекты.

### 6.2. Система стамины

**Полный код `Assets/Scripts/Systems/StaminaSystem.cs`:**

```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Плавно восстанавливает стамину. Если она кончилась — выключает CanMove,
/// пока Current снова не дорастёт до MinToMove.
/// </summary>
[UpdateBefore(typeof(MoveToTowerSystem))]
public partial struct StaminaSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Stamina>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (stamina, entity) in SystemAPI.Query<RefRW<Stamina>>().WithEntityAccess())
        {
            var canMove = SystemAPI.IsComponentEnabled<CanMove>(entity);

            if (canMove && stamina.ValueRO.Current <= 0f)
            {
                SystemAPI.SetComponentEnabled<CanMove>(entity, false);
                canMove = false;
            }

            // Восстанавливается, только пока моб стоит. Иначе паузу не видно.
            if (!canMove)
            {
                stamina.ValueRW.Current = math.min(
                    stamina.ValueRO.Max,
                    stamina.ValueRO.Current + stamina.ValueRO.RegenPerSecond * deltaTime);

                if (stamina.ValueRO.Current >= stamina.ValueRO.MinToMove)
                    SystemAPI.SetComponentEnabled<CanMove>(entity, true);
            }
        }
    }
}
```

`OnUpdate` — метод, который система выполняет каждый кадр, когда есть подходящие данные. `SystemAPI.Time.DeltaTime` — продолжительность текущего кадра в секундах. Умножение на неё превращает величину «за секунду» в изменение за этот кадр.

`SystemAPI.Query<RefRW<Stamina>>()` перебирает сущности со стаминой. `RefRW` означает, что систему интересует **чтение и запись** значения. `.WithEntityAccess()` добавляет в цикл идентификатор самой entity, чтобы система могла проверить и переключить её `CanMove`.

Если `CanMove` включён, но `Current <= 0`, система выключает `CanMove`. Пока он выключен, система увеличивает `Current` на `RegenPerSecond * DeltaTime`, но не выше `Max`. При достижении `MinToMove` снова включает `CanMove`. Атрибут `[UpdateBefore(typeof(MoveToTowerSystem))]` заставляет эту проверку выполняться перед системой движения.

### 6.3. Система движения к башне

**Полный код `Assets/Scripts/Systems/MoveToTowerSystem.cs`:**

```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>Двигает врага к текущей позиции башни, пока хватает стамины.</summary>
public partial struct MoveToTowerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<TowerTarget>();
        state.RequireForUpdate<MoveSpeed>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var towerEntity = SystemAPI.GetSingletonEntity<TowerTarget>();
        var towerPosition = SystemAPI.GetComponent<LocalTransform>(towerEntity).Position;
        var deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (transform, speed, stamina) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRO<MoveSpeed>, RefRW<Stamina>>()
                     .WithAll<CanMove>())
        {
            var position = transform.ValueRO.Position;
            var target = new float3(towerPosition.x, position.y, towerPosition.z);
            var offset = target - position;
            var distance = math.length(offset);
            if (distance <= speed.ValueRO.ReachDistance)
                continue;

            var direction = offset / distance;
            var step = math.min(speed.ValueRO.Value * deltaTime, distance - speed.ValueRO.ReachDistance);
            transform.ValueRW.Position = position + direction * step;
            transform.ValueRW.Rotation = quaternion.LookRotationSafe(direction, math.up());
            stamina.ValueRW.Current = math.max(
                0f,
                stamina.ValueRO.Current - stamina.ValueRO.DrainPerSecond * deltaTime);
        }
    }
}
```

Разбор по действиям:

1. `RequireForUpdate<TowerTarget>()` и `RequireForUpdate<MoveSpeed>()` требуют, чтобы в ECS-мире были цель и движущийся враг.
2. `GetSingletonEntity<TowerTarget>()` находит **единственную** башню по метке. `GetComponent<LocalTransform>(towerEntity).Position` получает её позицию.
3. `SystemAPI.Query<RefRW<LocalTransform>, RefRO<MoveSpeed>, RefRW<Stamina>>().WithAll<CanMove>()` выбирает сущности с позицией, скоростью, стаминой и **включённым** `CanMove`. `RefRO` означает только чтение скорости, `RefRW` — чтение и изменение позиции и стамины.
4. `target = new float3(towerPosition.x, position.y, towerPosition.z)` берёт X/Z башни, а высоту Y оставляет равной высоте врага. Поэтому враг движется по горизонтальной плоскости, а не «взлетает» к центру высокой башни.
5. `offset = target - position` — вектор от врага к цели; `distance = math.length(offset)` — расстояние до цели.
6. Если расстояние уже не больше `ReachDistance`, движения нет. Иначе `direction = offset / distance` получает направление единичной длины.
7. `step = math.min(speed * DeltaTime, distance - ReachDistance)` ограничивает шаг: враг идёт с заданной скоростью и не проскальзывает за выбранную границу остановки.
8. Позиция изменяется, поворот направляет врага в сторону движения, стамина уменьшается на расход за этот кадр. `math.max(0f, ...)` не позволяет стамине стать отрицательной.

`[BurstCompile]` просит Unity компилировать метод средствами Burst для более эффективного исполнения, когда это возможно. `Unity.Mathematics.float3`, `math` и `quaternion` — типы и функции математики, используемые здесь вместо обычных `Vector3` и `Mathf` в логике ECS.

### Пример на числах из префаба

При скорости `2` и кадре длительностью `0.02` секунды нормальный шаг равен `2 × 0.02 = 0.04` единицы, пока враг не приблизился к границе остановки. Стамина при расходе `1.6` уменьшится за такой кадр на `1.6 × 0.02 = 0.032`. Враг начинает с `4`; после исчерпания запаса останавливается. `MinToMove = 4 × 0.5 = 2`, поэтому снова пойдёт, когда восстановит стамину до `2`. Это упрощённый расчёт: фактическое число кадров зависит от длительности каждого кадра и расстояния до цели.

## 7. Почему перенос башни теперь меняет цель

Система движения **не хранит заранее записанную точку маршрута**. Она каждый кадр читает `LocalTransform` entity с меткой `TowerTarget`. Поэтому при переносе башни в редакторе **до Play** Unity запечёт её новое положение, и враг пойдёт туда. Если во время игры код изменит ECS-позицию башни, система тоже будет читать новую позицию.

Это прямое движение к цели. Здесь нет поиска обходного маршрута, `NavMesh`-построения пути, проверки столкновения с башней или её атаки. Требование первой практической — движение к цели через компоненты и системы — выполняется; полноценный поиск пути относится к дальнейшей работе.

## 8. Частые вопросы на защите

**Зачем ECS?** Для большого числа объектов с одинаковыми данными и логикой: данные врагов разделены на компоненты, общие действия вынесены в системы. Это основа под будущие массовые волны; производительность тысяч врагов ещё не измерялась.

**Что такое entity?** Отдельная сущность в ECS с идентификатором и набором компонентов. Созданный враг, башня и спавнер — разные entity.

**Что такое baking?** Подготовка ECS-данных из настроек обычных Unity-объектов. `Authoring` показывает поля в Inspector, `Baker` переносит их в компоненты. Системы потом работают с этими компонентами.

**Почему нельзя просто читать `EnemyAuthoring.Speed` в системе?** Потому что это поле `MonoBehaviour` у обычного GameObject, а ECS-система выбирает и обрабатывает ECS-компоненты. Baker переносит число в `MoveSpeed.Value`.

**Почему башне нужен пустой компонент?** `TowerTarget` — метка. По наличию этой метки система находит нужную entity. Позиция хранится отдельно в `LocalTransform`.

**Что делает `CanMove`?** Включённый компонент пропускает врага в систему движения; выключенный временно исключает его из движения. Стамина управляет этим переключением.

**Почему `RefRO` и `RefRW`?** `RefRO` — читать компонент, `RefRW` — читать и менять. Скорость система движения только читает, позицию и стамину меняет.

**Почему `DeltaTime`?** Чтобы скорость и расход стамины измерялись в единицах за секунду и не зависели напрямую от числа кадров в секунду.

**Где здоровье и смерть врага?** Их нет в текущей версии. В пункте 4 практической здоровье указано как пример; здесь выбран другой пример из задания — стамина, влияющая на движение и плавно восстанавливающаяся.

**Сколько врагов создаётся?** Один. Текущая практика демонстрирует движение и характеристику entity. Волны, тысячи врагов, анимации, коллизии, боёвка и поиск пути — задачи для следующих этапов итогового проекта.

**Что нельзя утверждать?** Что здесь уже есть обход препятствий, волны, урон башне, смерть врагов или доказанная работа с тысячами объектов.

## 9. Короткий рассказ на 40–60 секунд

> Тема проекта — tower defense. Я использую ECS, потому что в итоговой игре будет много однотипных врагов. Entity — это отдельная сущность; компоненты хранят её данные; системы обрабатывают сущности с нужным набором компонентов. В редакторе я настраиваю башню, спавнер и префаб врага как GameObject. Baker переносит их настройки в ECS-компоненты. Башня получает метку цели, враг — скорость, стамину и разрешение двигаться. Спавнер создаёт одного врага для демонстрации. Система движения каждый кадр берёт позицию башни и ведёт врага к ней. Другая система останавливает врага при нулевой стамине и постепенно восстанавливает её. Так выполнены движение entity к цели и характеристика entity, влияющая на поведение.

## 10. Что проверить в Unity перед защитой

1. В `Enemy.prefab` посмотри значения `EnemyAuthoring` в Inspector.
2. В `Gameplay` SubScene убедись, что на `Tower` есть `TowerAuthoring`, а на `Spawner` — `EnemySpawnerAuthoring` со ссылкой на префаб.
3. Запусти `SampleScene`: враг должен появиться у `Spawner`, идти к башне, делать паузу при исчерпании стамины и остановиться возле башни.
4. Останови Play, перенеси башню в другое место в редакторе и запусти снова: враг должен пойти к новой позиции башни.
5. Если в Inspector появятся значения по умолчанию вместо настроек префаба или Console покажет ошибки, сначала проверь сохранение сцены и префаба.

