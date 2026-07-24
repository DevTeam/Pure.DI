# Pure.DI: новые возможности

Pure.DI — это генератор кода для внедрения зависимостей (Dependency Injection), который работает на этапе компиляции. Pure.DI развивает идею «чистого DI»: вместо контейнера и рефлексии вы получаете обычный C#‑код, который создаёт композиции объектов. В этой статье — новые возможности, которые упрощают настройку композиций, делают корни гибче, а диагностику — понятнее.

Ключевые преимущества Pure.DI:

- **Zero-Overhead**: генерируемый код создания композиций объектов не отличается от ручного
- **Проверка на этапе компиляции**: ошибки внедрения, циклические зависимости, недостающие привязки и все другие ошибки обнаруживаются на этапе компиляции
- **Работает везде**: от .NET Framework 2.0 до последних версий .NET, Unity, Native AOT и других платформ
- **Прозрачность**: вы всегда можете посмотреть сгенерированный код, отладить его и понять, как он работает

Со времени выхода предыдущего поста Pure.DI получил улучшения, которые делают настройку композиций ещё более гибкой и выразительной.

О чём статья:

- контекст настройки для зависимых композиций (setup context)
- управление глубиной переопределения зависимостей в фабриках
- BuildUp/Builders для «достройки» существующих объектов
- подсказки (hints) для контроля auto-binding и конструкторов по умолчанию
- облегчённые корни (light roots) и оптимизированные анонимные корни
- lifetime по умолчанию (включая auto-bindings), упрощённые lifetime-методы
- SpecialType<T> для упрощённых привязок (в т. ч. для Unity)
- Tag.Any для гибких тегированных зависимостей
- `ref`/`out`‑зависимости и сценарии с ref‑struct
- контекстно‑зависимые фабрики (`ctx.ConsumerType`) и потокобезопасные фабрики (`ctx.Lock`)
- новые варианты корней (`virtual`/`override`) для наследования композиций
- более выразительная диагностика: ID ошибок/предупреждений, ссылки на справку, локализация

Другие статьи на тему Pure.DI:

- [Pure.DI помогает сделать DI чистым](https://habr.com/ru/articles/765112/) - рекомендуется для понимани идеи
- [Pure.DI v2.1](https://habr.com/ru/articles/795809/)
- [Новое в Pure.DI к началу 2024 года](https://habr.com/ru/articles/808297/)
- [Новое в Pure.DI к концу 2024 года](https://habr.com/ru/articles/868744/)
- [Pure.DI в Unity](https://habr.com/ru/articles/876712/)

---

## Setup context для зависимых композиций

Когда одна настройка DI зависит от другой и ей могут быть нужны **элементы** базовой настройки (например, поля, инициализированные Unity или внешней инфраструктурой), можно передать *контекст настройки* явно. Это делает зависимость прозрачной и убирает «магические» ссылки на состояние.

```csharp
// Создаём базовую композицию с настройками для продакшена
var baseContext = new BaseComposition { Settings = new AppSettings("prod") };

// Передаём контекст базовой композиции в зависимую
var composition = new Composition(baseContext);

// Получаем сервис, который использует настройки из базовой композиции
var service = composition.Service;

internal partial class BaseComposition
{
	internal AppSettings Settings { get; set; } = new("dev");

	private void Setup() => DI.Setup(nameof(BaseComposition))
        .Bind<IAppSettings>().To(_ => Settings);
}

internal partial class Composition
{
	private void Setup() => DI.Setup(nameof(Composition))
        // Указываем зависимость от базовой композиции с передачей аргумента
		.DependsOn(nameof(BaseComposition), SetupContextKind.Argument, "baseContext")
		.Bind<IService>().To<Service>()
		.Root<IService>("Service");
}

record AppSettings(string Environment) : IAppSettings;

interface IAppSettings { string Environment { get; } }

interface IService { }

class Service(IAppSettings settings) : IService;
```

[Зависимые композиции с передачей контекста](https://github.com/DevTeam/Pure.DI/blob/master/readme/dependent-compositions-with-setup-context.md)

Кроме передачи с помощью аргумента, контекст можно «подмешивать» через [members](https://github.com/DevTeam/Pure.DI/blob/master/readme/dependent-compositions-with-setup-context-members.md) / [аксессоры](https://github.com/DevTeam/Pure.DI/blob/master/readme/dependent-compositions-with-setup-context-members-and-property-accessors.md) и использовать [root‑argument](https://github.com/DevTeam/Pure.DI/blob/master/readme/dependent-compositions-with-setup-context-root-argument.md) сценарии — это удобно, когда базовая настройка живёт как объект, а зависимая композиция должна оставаться чистой и тестируемой.

---

## Наследование композиций: переиспользование инфраструктуры без дублирования

Если у вас есть общий слой инфраструктуры (БД, логирование, кэш), его удобно вынести в базовую композицию и наследовать в конкретных композициях. Так вы переиспользуете привязки и при этом оставляете «верхнюю» композицию маленькой и предметной.

```csharp
// Базовый класс с общей инфраструктурой
class Infrastructure
{
	private static void Setup() => DI.Setup(kind: CompositionKind.Internal)
        .Bind<IDatabase>().To<SqlDatabase>();
}

// Конкретная композиция наследует инфраструктуру
partial class Composition : Infrastructure
{
	private void Setup() => DI.Setup()
        .Bind<IUserManager>().To<UserManager>()
		.Root<App>(nameof(App));
}
```

[Наследование композиций: общий Setup и привязки в базовом классе](https://github.com/DevTeam/Pure.DI/blob/master/readme/inheritance-of-compositions.md)

В паре с virtual/override корнями композиции это даёт удобную модель «продакшен‑композиция + тестовая надстройка» без внедрения runtime‑контейнеров.

---

## Управление глубиной переопределения зависимостей

В фабриках часто нужно временно переопределить зависимость (например, `requestId`/`tenantId`) — но не всегда хочется, чтобы override «протёк» во все вложенные зависимости. Для этого есть два режима:

- `ctx.Override(...)` — переопределение распространяется максимально глубоко по графу зависимостей
- `ctx.Let(...)` — переопределение действует только на текущий уровень внедрения

```csharp
.Bind().To<Service>(ctx =>
{
	ctx.Let(42); // только для Service(...)
	ctx.Inject(out Service service);
	return service;
})
```

[Глубина override (Override vs Let) на реальном примере](https://github.com/DevTeam/Pure.DI/blob/master/readme/override-depth.md)

Это особенно полезно в сервисах, где один и тот же примитив (например, `int id`) встречается и в корневом конструкторе, и внутри нескольких зависимостей: можно управлять тем, что именно вы хотите подменить.

---

## BuildUp/Builders для достройки объектов

Иногда объект создаёт внешняя система (UI‑фреймворк, сериализатор, Unity, ORM, фабрика ...), а вам нужно лишь достроить его - **довнедрить зависимости** в поля/свойства/методы, помеченные атрибутами внедрения. Для этого есть BuildUp‑сценарии и генерация builders.

```csharp
// Настраиваем генерацию builder для интерфейса IRobot
DI.Setup(nameof(Composition))
	.Bind().To(Guid.NewGuid)
	.Bind().To<PlutoniumBattery>()
	.Builders<IRobot>("BuildUp");

var composition = new Composition();
// Достраиваем уже созданный объект
var bot = composition.BuildUp(new CleanerBot());
```

[Builders: BuildUp для типов, известных на этапе компиляции](https://github.com/DevTeam/Pure.DI/blob/master/readme/builders.md)

Builders удобны для сценариев, где DI дополняет/инициализирует уже имеющийся экземпляр, например, компоненты Unity, десериализованные объекты и др.

---

## Контроль автопривязок (auto-binding)

Автопривязки часто ускоряют разработку, но в больших решениях иногда нужно **запретить** их полностью или точечно, чтобы все зависимости были под полным контролем - имели явные привязки и ревью изменений.

```csharp
// Полностью отключаем автопривязки
DI.Setup(nameof(Composition))
	.Hint(Hint.DisableAutoBinding, "On")
	.Bind<IService>().To<Service>()
	.Root<IService>("Root");
```

`DisableAutoBinding` поддерживает фильтры по имени типа и по lifetime — удобно, если вы хотите оставить auto-binding только для «простых DTO», но запретить для сервисов/репозиториев:

- `DisableAutoBindingImplementationTypeNameRegularExpression`
- `DisableAutoBindingImplementationTypeNameWildcard`
- `DisableAutoBindingLifetimeRegularExpression`
- `DisableAutoBindingLifetimeWildcard`

---

## Пропуск конструктора по умолчанию

Иногда наличие параметрless‑конструктора мешает: объект формально можно создать, но реальная инициализация объекта должна идти через другой конструктор/фабрику. Подсказка `SkipDefaultConstructor` помогает направить генерацию в нужную сторону.

```csharp
// Пропускаем конструктор по умолчанию для всех типов
DI.Setup(nameof(Composition))
	.Hint(Hint.SkipDefaultConstructor, "On")
	// Или только для конкретных типов по маске
	.Hint(Hint.SkipDefaultConstructorImplementationTypeNameWildcard, "*SchroedingersCat")
	.Bind<ICat>().To<SchroedingersCat>()
	.Root<Zoo>("Root");
```

Это полезно в интеграциях, где часть типов имеет «пустой» конструктор для фреймворка, но в реальной композиции должна создаваться с использованием рекомендуемого конструктора. Другие полезные подсказки:

- `SkipDefaultConstructorImplementationTypeNameRegularExpression`
- `SkipDefaultConstructorImplementationTypeNameWildcard`
- `SkipDefaultConstructorLifetimeRegularExpression`
- `SkipDefaultConstructorLifetimeWildcard`

---

## Light roots и оптимизированные анонимные корни

Для небольших корней (утилиты, «простые» сервисы) выгодно генерировать их как **легковесные**: корни используют общий lightweight‑композит и делегаты, значительно уменьшая объём сгенерированного кода и накладные расходы. Анонимные корни легковесны по умолчанию.

```csharp
using static Pure.DI.RootKinds;

DI.Setup(nameof(Composition))
	.Bind().To<ConsoleLogger>()
	
	// Именованный легковесный корень
	.Root<ILogger>("Logger", kind: Light);

var composition = new Composition();
var logger = composition.Logger;
```

[Light roots и лёгкие анонимные корни](https://github.com/DevTeam/Pure.DI/blob/master/readme/light-roots.md)

Если вам важно «дебажить» анонимные корни как полноценные графы, поведение можно контролировать подсказкой `LightweightAnonymousRoot`.

---

## Lifetime по умолчанию — и для auto-bindings тоже

`DefaultLifetime(...)` позволяет убрать повторяющиеся `.As(...)` и сделать конфигурацию компактнее.

[DefaultLifetime: как задать lifetime «по умолчанию»](https://github.com/DevTeam/Pure.DI/blob/master/readme/default-lifetime.md)

При необходимости можно задавать lifetime «по умолчанию» точечно — для конкретного контракта и даже с учётом тега.

[DefaultLifetime для конкретного контракта](https://github.com/DevTeam/Pure.DI/blob/master/readme/default-lifetime-for-a-type.md)

Теперь время жизни можно определить и для зависимостей, которые Pure.DI создаёт автоматически (auto-bindings), в примере ниже это `Cache`:

```csharp
// Задаём singleton для auto-binding типа Cache
DI.Setup(nameof(Composition))
	.DefaultLifetime<Cache>(Lifetime.Singleton)
	.Bind<IService>().To<Service>()
	.Root<IService>("Root");

class Cache;

class Service(Cache cache) : IService;

interface IService;
```

---

## Упрощённые lifetime-методы: меньше шума в конфигурации

Когда конфигурация состоит в основном из однотипных привязок, удобнее писать не `.Bind().As(...).To<...>()`, а сразу lifetime-метод: `Singleton<>()`, `PerResolve<>()` и т.д. Это делает настройку композиции короче и заметно читабельнее.

```csharp
// Краткая запись для однотипных привязок
DI.Setup(nameof(Composition))
	.PerBlock<OrderManager>()
	.Transient<Shop, OrderNameFormatter>()
	.Root<IShop>("MyShop");
```

[Упрощённые lifetime-specific привязки](https://github.com/DevTeam/Pure.DI/blob/master/readme/simplified-lifetime-specific-bindings.md)

Этот же подход работает и для фабрик: когда нужно вручную создать объект, но при этом зафиксировать lifetime одним вызовом.

[Упрощённые lifetime-specific фабрики](https://github.com/DevTeam/Pure.DI/blob/master/readme/simplified-lifetime-specific-factory.md)

---

## `SpecialType<T>` «упрощённая привязка» для платформенных базовых типов

Упрощённая привязка, т.е. привязка без указания контрактов (`.Bind().To<Implementation>()`), умеет автоматически подхватывать «разумный» набор контрактов (интерфейсы/абстракции), но в платформах вроде Unity есть базовые типы (например, `MonoBehaviour`), которые вы не хотите превращать в контракты автоматически. Для этого можно объявить тип «специальным» и исключить его из автопривязок.

```csharp
DI.Setup(nameof(Composition))
    // Исключаем MonoBehaviour из автопривязок
	.SpecialType<MonoBehaviour>()
	.Bind().To<GameController>()
	.Root<GameController>("Controller");
```

[Упрощённая привязка и список специальных типов](https://github.com/DevTeam/Pure.DI/blob/master/readme/simplified-binding.md)

Это снижает риск неожиданных конфликтов в графе, когда базовый платформенный тип «вдруг» начинает участвовать как контракт привязки.

---

## Маркеры generic type argument: удобнее для сложных generic-привязок

В сложных generic‑сценариях полезно иметь «маркерный» тип, который означает «любой T» в привязке (например, `IBox<TT>`). Pure.DI поддерживает такие маркеры через `GenericTypeArgument<...>()` и через вариант в виде атрибута.

```csharp
// Объявляем TT как маркер для любого типа
DI.Setup(nameof(Composition))
	.GenericTypeArgument<MyTT>()
	.Bind<ISequence<MyTT>>().To<Sequence<MyTT>>()
	.Root<IProgram>("Root");

interface MyTT;

interface ISequence<T>;

class Sequence<T> : ISequence<T>;

interface IProgram;
```

[Custom generic argument: marker через GenericTypeArgument<T>()](https://github.com/DevTeam/Pure.DI/blob/master/readme/custom-generic-argument.md)

[Custom generic argument attribute: marker через атрибут](https://github.com/DevTeam/Pure.DI/blob/master/readme/custom-generic-argument-attribute.md)

В роли маркера может выступать не только интерфейс, но и ссылочный тип с публичным конструктором без параметров — это упрощает интеграцию с фреймворками.

---

## `Tag.Any`: одна привязка для любых тегов

Когда нужно поддержать произвольные значения тега (включая `null`) и при этом «видеть» сам тег внутри фабрики, помогает `Tag.Any`.

```csharp
// Одна привязка для любого значения тега
DI.Setup(nameof(Composition))
	.Bind<IQueue>(Tag.Any).To(ctx => new Queue(ctx.Tag))
	.Root<IQueue>("AuditQueue", "Audit");
```

[Tag.Any: привязка, которая "матчится" на любой тег](https://github.com/DevTeam/Pure.DI/blob/master/readme/tag-any.md)

---

## `ref/out`‑зависимости и ref‑struct: низкоуровневые сценарии без лишних аллокаций

Pure.DI поддерживает внедрения, где нужны `ref`/`out`, что открывает дверь в high‑perf сценарии (буферы, `Span<T>`, ref‑struct). Например: инициализировать сервис методом, который принимает ref‑структуру.

```csharp
// Метод для внедрения с ref-параметром
class Service
{
	[Ordinal] public void Initialize(ref Data data) { /* ... */ }
}
```

[Ref dependencies: пример с ref‑struct и методом для внедрения](https://github.com/DevTeam/Pure.DI/blob/master/readme/ref-dependencies.md)

---

## Контекстно‑зависимые фабрики: `ctx.ConsumerType`

Если фабрика должна вести себя по‑разному в зависимости от того, *кому* внедряют зависимость, используйте `ctx.ConsumerType`. Типичный пример — логирование: "обогащать" логгер типом класса‑потребителя.

```csharp
// Фабрика, которая знает тип потребителя
.Bind().To(ctx =>
{
	ctx.Inject<Serilog.ILogger>(out var logger);
	return logger.ForContext(ctx.ConsumerType);
})
```

[ConsumerType: контекстный логгер на примере Serilog](https://github.com/DevTeam/Pure.DI/blob/master/readme/consumer-type.md)

---

## Потокобезопасные фабрики: `ctx.Lock`

Для фабрик, которые выполняют инициализацию «под Lock», и для параллельных override‑сценариев есть общий объект синхронизации — `ctx.Lock`.

```csharp
// Потокобезопасная фабрика с синхронизацией
.Bind<IMessageBus>().To(ctx =>
{
	lock (ctx.Lock)
	{
		ctx.Inject(out MessageBus bus);
		bus.Connect();
		return bus;
	}
})
```

[Фабрика с синхронизацией через ctx.Lock](https://github.com/DevTeam/Pure.DI/blob/master/readme/factory-with-thread-synchronization.md)

[Thread-safe overrides: безопасные overrides при параллельной сборке](https://github.com/DevTeam/Pure.DI/blob/master/readme/thread-safe-overrides.md)

---

## Virtual/Override корний: расширяемость через наследование

Если вы строите «базовую» композицию для продакшена и хотите переопределять корни в наследниках (например, в тестах или для разных окружений), корни можно делать `virtual`, а затем переопределять.

```csharp
using static Pure.DI.RootKinds;

// Продакшен-композиция с virtual корнем
partial class ProdComposition
{
	private void Setup() => DI.Setup(nameof(ProdComposition))
        .Bind<ITime>().To<SystemTime>()
		.Root<ITime>("Time", kind: Public | Property | Virtual);
}

// Тестовая композиция переопределяет корень
partial class TestComposition : ProdComposition
{
	// Переопределяем root в наследнике
	public override ITime Time => new FakeTime();
}
```

Флаг `Override` полезен, когда вы хотите, чтобы *сам* генератор создал override‑реализацию корня в производном классе (например, с другим набором привязок), оставаясь в compile‑time модели.

---

## Множественные `[Bind]` атрибуты

`BindAttribute` позволяет объявлять «источники зависимостей» прямо на свойствах/полях/методах провайдера. Теперь можно задавать несколько привязок — удобно, когда один метод/свойство должно "обслуживать" несколько контрактов/тегов.

```csharp
class GatewayProvider
{
    // Один метод обслуживает несколько контрактов/тегов
	[Bind(typeof(IApiClient), Lifetime.Singleton, "Public")]
	[Bind(typeof(IApiClient), Lifetime.Singleton, "Internal")]
	public ApiClient Create() => new ApiClient();
}
```

[BindAttribute: привязка зависимостей](https://github.com/DevTeam/Pure.DI/blob/master/readme/bind-attribute.md)

---

## Type/Tag атрибуты на members‑внедрении (методы/поля/свойства)

Когда вы делаете внедрение не через конструктор, а через свойства/поля/методы, иногда нужно явно указать **тег** или **тип**, чтобы точно указать что должно быть внедрено. Атрибуты `Tag` и `Type` теперь поддерживаются и для таких внедрений.

```csharp
class JobRunner
{
	[Tag("fast")]
	public IExecutor? Executor { get; set; }

	public void Init([Type(typeof(SystemClock))] IClock clock) { /* ... */ }
}
```

[Type attribute: явное указание внедряемого типа](https://github.com/DevTeam/Pure.DI/blob/master/readme/type-attribute.md)

[Tag attribute: выбор реализации по тегу](https://github.com/DevTeam/Pure.DI/blob/master/readme/tag-attribute.md)

Это хорошо сочетается с BuildUp/Builders, где объект уже существует, а вы хотите аккуратно «настроить» его зависимости через members‑внедрение.

---

## Генерация конструкторов: только когда это нужно

Поведение генерации конструкторов для `Composition` стало более прагматичным: конструкторы появляются, когда в графе есть **аргументы композиции** и/или **scoped‑lifetime** привязки.

```csharp
DI.Setup(nameof(Composition))
	.Arg<string>("connectionString")
	.Bind<IDb>().To<Db>()
	.Root<IService>("Service");

// Конструктор генерируется с аргументом connectionString
var composition = new Composition(connectionString: "Server=.;Database=App;");
```

[Composition arguments: передача значений снаружи в композицию](https://github.com/DevTeam/Pure.DI/blob/master/readme/composition-arguments.md)

[Scope: lifetime Scoped и работа со scope‑иерархией](https://github.com/DevTeam/Pure.DI/blob/master/readme/scope.md)

Смысл простой: если у композиции нет параметров и нет scoped‑зависимостей, лишние конструкторы не генерируются. А если они нужны — сигнатура строится по реально используемым аргументам. Это позволяет использовать Pure.DI, например, в таких сценариях как Unity, когда не всегда можно использовать конструкторы для инициализации объектов.

---

## Циклические зависимости

Pure.DI улучшил работу с циклическими зависимостями: диагностика точнее указывает места в коде, построение графа старается показывать минимально возможное число ошибок, а в сложных случаях анализ старается аккуратнее «собирать» циклические участки, чтобы результат был предсказуемее. В сценариях, где цикл допустим по смыслу, его по‑прежнему можно разорвать через `Func<T>`, `Lazy<T>` и т. п.

```csharp
interface IA;
interface IB;

// Разрыв цикла через фабрику Func<IB>
class A(Func<IB> b) : IA;

class B(IA a) : IB;
```

[Lazy/Func: инъекции по требованию и разрыв циклов](https://github.com/DevTeam/Pure.DI/blob/master/readme/lazy.md)

Если же цикл недопустим — вы получите диагностику на этапе компиляции с ID и ссылкой на справку.

---

## Диагностика стала проще: ID, справка, локализация

Pure.DI всё так же обнаруживает проблемы на этапе компиляции, но с ними стало проще работать: у ошибок/предупреждений появились **стабильные ID**, описания и ссылки на документацию, локализация сообщений. Плюс к этому улучшена привязка диагностик к месту в коде.

```csharp
DI.Setup(nameof(Composition))
	.Bind<IService>().To<Service>()
	.Root<IService>("Root");

interface IService;

class Service(IDependency dep) : IService;

interface IDependency;
```

[Справочник диагностик Pure.DI](https://github.com/DevTeam/Pure.DI/blob/master/DIAGNOSTICS.md)

Сейчас диагностические сообщения доступны для следующих языков:

- Английский
- Арабский
- Бенгальский
- Вьетнамский
- Индонезийский
- Испанский
- Итальянский
- Китайский
- Корейский
- Немецкий
- Португальский
- Русский
- Тайский
- Французский
- Хинди
- Японский

Если же кому не хватает например Санскрита, то не стесняйтесь создать тикет в [репозитории Pure.DI на GitHub](https://github.com/DevTeam/Pure.DI).

---

Если есть желание попробовать новые возможности, можно начать [с любого примера](https://github.com/DevTeam/Pure.DI?tab=readme-ov-file#examples) — они независимы и запускаются легко через `dotnet run`.

Спасибо за интерес и что дочитали до конца!
