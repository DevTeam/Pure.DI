# Новое в Pure.DI: union types, генерация интерфейсов и DI без лишних аллокаций

Pure.DI — это генератор кода для внедрения зависимостей (Dependency Injection), который работает на этапе компиляции. Pure.DI развивает идею «чистого DI»: вместо контейнера и рефлексии вы получаете обычный C#‑код, который создаёт композиции объектов. В этой статье — новые возможности из релизов 2.3.5–2.5.1: от union types в роли DI‑контрактов до сценариев внедрения зависимостей без лишних аллокаций.

Ключевые преимущества Pure.DI:

- **Zero-Overhead**: генератор создаёт обычный статически типизированный C#‑код без runtime‑поиска зависимостей и рефлексии
- **Проверка на этапе компиляции**: ошибки конфигурации DI‑графа, циклические зависимости и недостающие привязки обнаруживаются на этапе компиляции
- **Широкая совместимость**: от .NET Framework 2.0 до современных версий .NET, Unity, Native AOT и других платформ
- **Прозрачность**: вы всегда можете посмотреть сгенерированный код, отладить его и понять, как он работает

Со времени предыдущего поста вышло шесть релизов, и статья делится на две главные части.

**Часть 1. Новые возможности:**

- union types как DI‑контракты
- генерация интерфейсов из классов (`[GenerateInterface]`)
- привязки атрибутами в реализациях (`[Bind]`, `[Type]`, `[Tag]`, `[Lifetime]`)
- `[Export]`: члены классов как источники зависимостей
- кастомные binding‑атрибуты
- скоупы: `SetupScope`, скоуп на сцену Unity, `CreateScope()` для Microsoft DI
- полная поддержка nullable reference types
- `TryBuildUp` — безопасная «достройка» объектов
- поддержка новых возможностей C# 13/14: `OverloadResolutionPriority` и partial‑конструкторы

**Часть 2. Производительность — DI на hot paths:**

- `Span<T>`, `ReadOnlySpan<T>` и ref struct‑зависимости
- фабрики для stack-only значений (`allows ref struct`)
- zero-copy парсинг и method injection на hot path
- диагностики для stack-only
- non-boxing union results
- каталог high-performance примеров: ArrayPool, пулы объектов, фабрики без замыканий, `ValueTask<T>`‑корни, `ThreadSafe = Off`
- экономия в сгенерированном коде

Другие статьи на тему Pure.DI:

- [Pure.DI помогает сделать DI чистым](https://habr.com/ru/articles/765112/) - рекомендуется для понимания идеи
- [Pure.DI v2.1](https://habr.com/ru/articles/795809/)
- [Новое в Pure.DI к началу 2024 года](https://habr.com/ru/articles/808297/)
- [Новое в Pure.DI к концу 2024 года](https://habr.com/ru/articles/868744/)
- [Pure.DI в Unity](https://habr.com/ru/articles/876712/)

---

## Часть 1. Новые возможности

### Union types как DI-контракты

В preview‑версиях C# появились union types — тип, значением которого может быть один из нескольких заранее известных case‑типов. Pure.DI научился использовать union как **DI‑контракт**: case‑реализация неявно конвертируется в union, и генератор строит весь граф зависимостей выбранного case за union‑контрактом. Главное отличие от интерфейса — case‑типам не нужна общая абстракция: это могут быть даже классы из разных сторонних SDK, которые вы не можете менять.

```csharp
DI.Setup(nameof(Composition))
    .Bind<IReceiptService>().To<ReceiptService>()
    // Композиция решает, какой платёжный шлюз
    // стоит за union-контрактом
    .Bind<PaymentGateway>().To<StripeGateway>()
    .Root<CheckoutService>("Checkout");

// Case-типы — независимые классы без общего интерфейса,
// у каждого свои зависимости
class StripeGateway(StripeApiClient api)
{
    public string Charge(int amountInCents) =>
        api.Post($"charge {amountInCents}");
}

class BankGateway(BankAccount account)
{
    public string Transfer(int amountInCents) =>
        $"bank transfer {amountInCents} from {account.Iban}";
}

// Union-тип: case-типы неявно конвертируются в union,
// и Pure.DI использует эту конверсию
union PaymentGateway(StripeGateway, BankGateway);

// Сервис зависит от union и обрабатывает каждый case явно
class CheckoutService(PaymentGateway gateway, IReceiptService receipts)
{
    public string Pay(int amountInCents)
    {
        var confirmation = gateway switch
        {
            StripeGateway stripe => stripe.Charge(amountInCents),
            BankGateway bank => bank.Transfer(amountInCents),
            _ => throw new InvalidOperationException("Unknown payment gateway.")
        };

        return receipts.Print(confirmation);
    }
}
```

Смена провайдера — однострочное изменение: `.Bind<PaymentGateway>().To<BankGateway>()`. Что важно знать:

- если явной union‑привязки нет, но ровно одна зарегистрированная привязка конвертируется в union — Pure.DI разрешит union через этот единственный case автоматически
- если применимых case‑привязок несколько — вы получите ошибку `DIE050` со списком кандидатов и указанием мест привязок
- union‑типы **не участвуют** в автопривязках — случайно создать «пустой» union не получится
- case‑привязка остаётся владельцем своего lifetime и disposal: `Singleton`, `Scoped`, `PerResolve` работают как обычно
- коллекции case‑ов собираются через `Tag.Unique`: можно запросить `IEnumerable<PaymentGateway>`, массив, `ReadOnlySpan<>` и т. д.

[Union types: case-реализации за union-контрактом](https://github.com/DevTeam/Pure.DI/blob/master/readme/union-types.md)

Поддержка union types требует preview‑версии языка и .NET 11 Preview 5 или новее, но попробовать её можно уже сейчас.

---

### Генерация интерфейсов из классов

Классическая пара «класс + зеркальный интерфейс» — самый скучный boilerplate в DI‑проектах. Теперь интерфейс можно сгенерировать из реализации: достаточно объявить пустой `partial interface` и пометить класс атрибутом `[GenerateInterface]`. Отдельное спасибо [Adam Hathcock](https://github.com/adamhathcock) за дизайн и реализацию этой возможности!

```csharp
DI.Setup(nameof(Composition))
    .Bind().To<EmailSender>()
    .Root<App>(nameof(App));

// Пустое объявление — тело интерфейса сгенерирует Pure.DI
public partial interface IEmailSender;

[GenerateInterface]
public class EmailSender : IEmailSender
{
    public string Provider => "smtp";

    public string Send(string address) => $"sent:{address}";
}

// Потребитель зависит от абстракции, которую никто не писал руками
public class App(IEmailSender sender);
```

Возможности не ограничиваются простым зеркалированием:

- [имя, namespace и уровень доступа интерфейса настраиваются](https://github.com/DevTeam/Pure.DI/blob/master/readme/customize-the-generated-interface.md) через именованные аргументы атрибута
- [из одного класса можно сгенерировать несколько интерфейсов](https://github.com/DevTeam/Pure.DI/blob/master/readme/generate-several-interfaces-from-one-class.md), распределяя члены по контрактам атрибутами
- [атрибут `[IgnoreInterface]`](https://github.com/DevTeam/Pure.DI/blob/master/readme/ignore-members-in-the-generated-interface.md) исключает члены из контракта
- [generic-члены, nullable-аннотации и события сохраняются](https://github.com/DevTeam/Pure.DI/blob/master/readme/generate-interfaces-with-generics.md), как и XML‑комментарии документации

[Генерация интерфейса из класса](https://github.com/DevTeam/Pure.DI/blob/master/readme/generate-an-interface-from-a-class.md)

---

### Привязки атрибутами в реализациях

В версии 2.5.0 атрибутная модель привязок была переработана (это breaking change, о миграции — ниже). Теперь `[Bind]` можно объявлять прямо на типе реализации и указывать контракт, время жизни и тег. Реализации могут сами описывать свои привязки, и настройка композиции становится компактнее:

```csharp
DI.Setup(nameof(Composition))
    // В setup — только корень, привязка задана атрибутом
    .Root<IMessageWriter>("Writer", "console");

[Bind(typeof(IMessageWriter), Lifetime.Singleton, "console")]
class ConsoleMessageWriter : IMessageWriter
{
    public void Write(string message) => Console.WriteLine(message);
}
```

Атрибуты внутри одной группы `[...]` сливаются в одну привязку, а отдельные группы создают независимые привязки. Это удобно, когда одна реализация играет несколько ролей с разными контрактами, lifetime и тегами:

```csharp
// Группа привязки #1: участник пайплайна оплаты
[Bind(typeof(IPaymentProcessor), Lifetime.Singleton, "audit")]
// Группа привязки #2: тот же адаптер как sink аудита со своим lifetime
[Bind(typeof(IPaymentAuditSink), Lifetime.Transient, "operations")]
class PaymentAuditAdapter(IEventStore eventStore) :
    IPaymentProcessor,
    IPaymentAuditSink;
```

Кроме `[Bind]`, работают и «точечные» атрибуты `[Type]`, `[Tag]` и новый `[Lifetime]` — их можно комбинировать. Для generic‑реализаций поддерживаются маркерные контракты вида `typeof(IBox<TT>)`.

[BindAttribute на реализации](https://github.com/DevTeam/Pure.DI/blob/master/readme/bind-attribute.md)

[Группы Bind-атрибутов: несколько ролей одного адаптера](https://github.com/DevTeam/Pure.DI/blob/master/readme/bind-attribute-groups.md)

---

### `[Export]`: члены классов как источники зависимостей

Раньше источник зависимости задавал member-level атрибут `[Bind]`. Теперь эта роль у нового атрибута `[Export]` — семантика стала однозначной: `[Bind]` — про привязки, `[Export]` — про источники зависимостей:

```csharp
DI.Setup(nameof(Composition))
    .Bind().As(Lifetime.Singleton).To<DeviceFeatureProvider>()
    .Bind().To<PhotoService>()
    .Root<IPhotoService>("PhotoService");

class DeviceFeatureProvider
{
    // Свойство — источник зависимости
    [Export] public IGps Gps { get; } = new Gps();

    [Export] public ICamera Camera { get; } = new Camera();
}

class PhotoService(IGps gps, Func<ICamera> cameraFactory) : IPhotoService;
```

`[Export]` принимает необязательные `lifetime` и `tags`, так что экспортированный член ведёт себя как источник привязки:

```csharp
class GraphicsAdapter
{
    [Export(lifetime: Lifetime.Singleton, tags: ["HighPerformance"])]
    public IGpu HighPerfGpu { get; } = new DiscreteGpu();
}

class RayTracer([Tag("HighPerformance")] IGpu gpu) : IRenderer;
```

Миграция с 2.4.x проста: member-level `[Bind]` заменяется на `[Export]`, а `RootKinds.Exposed` — на `RootKinds.Exported`.

[ExportAttribute: члены как источники зависимостей](https://github.com/DevTeam/Pure.DI/blob/master/readme/export-attribute.md)

[Export с lifetime и тегом](https://github.com/DevTeam/Pure.DI/blob/master/readme/export-attribute-with-lifetime-and-tag.md)

---

### Кастомные binding-атрибуты

Если не хочется размечать реализации встроенными атрибутами Pure.DI — объявите **собственный** атрибут и зарегистрируйте его в setup. Методы `TypeAttribute<T>()`, `TagAttribute<T>()` и новый `LifetimeAttribute<T>()` объясняют генератору, из каких аргументов конструктора атрибута читать контракт, тег и время жизни. В приведённом варианте атрибут использует тип `Pure.DI.Lifetime`, поэтому сборке с реализациями всё ещё нужна ссылка на API Pure.DI:

```csharp
DI.Setup(nameof(Composition))
    // Регистрируем свой атрибут как источник binding-метаданных
    .TypeAttribute<ServiceAttribute<TT>>()
    .LifetimeAttribute<ServiceAttribute<TT>>()
    .TagAttribute<ServiceAttribute<TT>>(1)
    .Root<IMessageWriter>("Writer", "console");

// Атрибут может жить в сборке с реализациями и не зависит
// от конкретного встроенного binding-атрибута Pure.DI
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
class ServiceAttribute<T> : Attribute
{
    public ServiceAttribute(
        Lifetime lifetime = Lifetime.Transient,
        object? tag = null)
    {
    }
}

[Service<IMessageWriter>(Lifetime.Singleton, "console")]
class ConsoleMessageWriter : IMessageWriter;
```

Кастомные атрибуты участвуют в тех же правилах слияния групп, что и встроенные `Bind`, `Type`, `Tag`, `Lifetime`. Дублирование lifetime внутри одной группы атрибутов диагностируется как ошибка.

[Кастомный binding-атрибут](https://github.com/DevTeam/Pure.DI/blob/master/readme/custom-bind-attribute.md)

---

### Скоупы: `SetupScope`

Классическая задача — время жизни «на запрос/операцию»: контекст запроса, unit of work, телеметрия. Новый генерируемый метод (имя задаётся хинтом `ScopeMethodName`) привязывает новый экземпляр композиции к родительскому скоупу: `Scoped`‑экземпляры уникальны внутри скоупа и освобождаются вместе с ним (обычный `Singleton` же остаётся общим для всех).

```csharp
var composition = new Composition();

// Запрос #1: свой scope, свой RequestContext
using (var request = Composition.SetupScope(composition, new Composition()))
{
    var checkout = request.RequestRoot;
    // В пределах scope RequestContext один и тот же,
    // по окончании — будет вызван Dispose
}

partial class Composition
{
    static void Setup() => DI.Setup()
        .Hint(Hint.ScopeMethodName, "SetupScope")
        // Время жизни «на запрос»
        .Bind().As(Scoped).To<RequestContext>()
        .Bind().As(Singleton).To<IdGenerator>()
        .Bind().To<CheckoutService>()
        .Root<ICheckoutService>("RequestRoot");
}
```

Имя метода подбирается под домен: `CreateScope`, `BeginRequest`, `OpenSession` — что улучшает читаемость кода. Скоупы могут создаваться и фабричными методами, а родительский и дочерний скоупы валидируются на различие — случайное «вложение в себя» исключено.

[Scope setup method: скоуп на запрос без класса-обёртки](https://github.com/DevTeam/Pure.DI/blob/master/readme/scope-setup-method.md)

---

### Скоупы на платформах: сцены Unity и Microsoft DI

Та же модель скоупов естественно ложится на платформенные сценарии. В Unity каждая загруженная сцена получает собственный скоуп: `Scoped` сервисы разделяются внутри сцены и изолированы между сценами, `Singleton` — общие на всё приложение. При этом `MonoBehaviour`-объекты создаёт сам Unity, а Pure.DI лишь достраивает их через builders:

```csharp
public partial class Scope : MonoBehaviour
{
    [SerializeField] ClockConfig clockConfig;

    void Setup() => DI.Setup()
        .Hint(Hint.ScopeMethodName, "SetupScope")
        .Bind().To(() => clockConfig)
        .Bind<IClockService>().As(Singleton).To<ClockService>()
        .Bind<IClockSession>().As(Scoped).To<ClockSession>()
        // Unity создаёт MonoBehaviour, Pure.DI достраивает
        .Builders<MonoBehaviour>();
}
```

[Unity scene scopes: скоуп на каждую сцену](https://github.com/DevTeam/Pure.DI/blob/master/readme/unity-scene-scopes.md)

Для интеграции с `Microsoft.Extensions.DependencyInjection` композиция может реализовать `IServiceScopeFactory`/`IServiceScope`: каждый вызов `composition.CreateScope()` создаёт новый scope, а теги можно сопоставить keyed services. Эта модель совместима со scoped‑семантикой MS DI для явно объявленных composition roots; через `IServiceProvider` разрешаются только корни, зарегистрированные вызовами `Root(...)` или `RootBind()`.

[Service provider со скоупами в семантике MS DI](https://github.com/DevTeam/Pure.DI/blob/master/readme/service-provider-with-scope.md)

---

### Nullable reference types на всём пути

Версия 2.4.0 принесла полную поддержку nullable reference types — аннотации сохраняются при чтении контрактов, построении графа и генерации кода. Это breaking change: код, полагавшийся на автоматические null‑проверки для nullable‑аргументов, может потребовать корректировки.

```csharp
DI.Setup(nameof(Composition))
    .Bind<IDatabase>().To<Database>()
    .Bind<IReportService>().To<ReportService>()
    // Nullable-аргументы больше не получают null-проверок:
    // null проходит внутрь, как и задумано
    .Arg<string?>("defaultTitle", "title")
    .RootArg<string?>("connectionString", "connection")
    .Root<IReportService>("CreateReportService");

var composition = new Composition(defaultTitle: null);
var reportService = composition.CreateReportService(connectionString: null);

class ReportService(
    [Tag("title")] string? defaultTitle,
    [Tag("connection")] string? connectionString,
    IDatabase? optionalDatabase) : IReportService;
```

На что это влияет:

- non-null привязка может удовлетворить nullable‑зависимость — удобно для опциональных параметров конструктора, nullable‑результатов фабрик и элементов коллекций
- `T?` означает «потребитель умеет обрабатывать null», но не отменяет ошибку графа при отсутствующей привязке — вся строгость проверок сохраняется
- для generic‑контрактов с nullable‑аргументами предпочитайте `where T : class?` вместо `where T : class`, чтобы не получать предупреждений от компилятора
- новые предупреждения подсвечивают неоднозначные nullable‑корни в методах `Resolve`

[Nullable reference types: примеры и рекомендации](https://github.com/DevTeam/Pure.DI/blob/master/readme/nullable-reference-types.md)

---

### `TryBuildUp`: безопасная достройка объектов

Builders «достраивают» объекты, созданные внешней системой — UI‑фреймворком, сериализатором, Unity. Строгий `BuildUp` бросает `ArgumentException`, если runtime‑подтип неизвестен композиции. Сейчас вместе с `BuildUp` генерируется безопасный `TryBuildUp`, который не бросает исключения:

```csharp
DI.Setup(nameof(Composition))
    .Bind().To(Guid.NewGuid)
    .Bind().To<PlutoniumBattery>()
    // Builder для каждого типа, унаследованного от IRobot
    .Builders<IRobot>("BuildUp", filter: "*Bot");

var composition = new Composition();

// Известный тип — достройка выполняется
var cleaner = composition.BuildUp(new CleanerBot());

// Неизвестный подтип — graceful fallback вместо исключения
var externalRobot = new ExternalRobot();
if (!composition.TryBuildUp(externalRobot))
{
    // объект остался нетронутым, обрабатываем ситуацию сами
}
```

Это особенно удобно, когда объекты приходят извне и их фактический тип известен только в рантайме: десериализация, плагины, объекты сцены Unity.

[Builders и TryBuildUp](https://github.com/DevTeam/Pure.DI/blob/master/readme/builders.md)

---

### Новые возможности C# 13/14: partial-конструкторы и `OverloadResolutionPriority`

C# 14 позволяет разделить конструктор на объявление (контракт) и тело — например, чтобы контракт с DI‑атрибутами жил в одной части partial‑класса, а логика в другой (в том числе сгенерированной). Pure.DI видит объединённый конструктор и работает с ним как с обычным:

```csharp
partial class AuditSink : AuditSinkBase
{
    // Определяющее объявление участвует в выборе конструктора,
    // атрибуты параметров объединяются с телом конструктора
    public partial AuditSink(
        [Tag("durable")] IAuditTransport transport,
        AuditSinkOptions options);
}

partial class AuditSink
{
    // Инициализатор base(...)/this(...)
    public partial AuditSink(
        IAuditTransport transport,
        AuditSinkOptions options)
        : base(transport)
    {
        BatchSize = options.BatchSize;
    }
}
```

[Partial constructor injection](https://github.com/DevTeam/Pure.DI/blob/master/readme/partial-constructor-injection.md)

Кроме того, Pure.DI поддерживает появившийся в C# 13 `OverloadResolutionPriorityAttribute` — атрибут, которым можно повысить или понизить приоритет перегрузки конструктора:

```csharp
[method: OverloadResolutionPriority(1)]
class BillingApiClient(ResilientHttpOptions options)
{
    // Legacy-конструктор сохранён для старых вызывающих,
    // но новый код (включая сгенерированный) предпочтёт primary
    public BillingApiClient(LegacyHttpOptions options)
        : this(new ResilientHttpOptions()) { }
}
```

Приоритеты работают как в C#: больше — предпочтительнее, неаннотированные конструкторы имеют приоритет 0, отрицательные значения понижают приоритет. `[Ordinal]` остаётся явным и более сильным механизмом Pure.DI. Предупреждение `DIW014` относится к другому случаю — method injection: оно сообщает, что обычный сгенерированный вызов метода может попасть в другую перегрузку с более высоким `OverloadResolutionPriority`.

[OverloadResolutionPriority при выборе конструктора](https://github.com/DevTeam/Pure.DI/blob/master/readme/overload-resolution-priority.md)

---

## Часть 2. Производительность: DI на hot paths

«Zero-overhead» в Pure.DI означает обычный статически типизированный код без runtime‑контейнера. Релиз 2.5.1 идёт дальше: внедрение зависимостей теперь работает в стек‑ориентированном коде с `Span<T>` и `ref struct`, не добавляя обязательных обёрток или runtime‑поиска. Потенциально опасные способы хранения stack-only значений диагностируются при построении DI‑графа.

### `Span<T>` и `ReadOnlySpan<T>` как зависимости

`Span` внедряется так же, как массивы `T[]` — для немедленного использования в конструкторе или методе. Для value‑типов Pure.DI генерирует `stackalloc`: коллекция зависимостей собирается вообще без heap‑аллокации.

```csharp
DI.Setup(nameof(Composition))
    .Bind<Point>('a').To(() => new Point(1, 1))
    .Bind<Point>('b').To(() => new Point(2, 2))
    .Bind<Point>('c').To(() => new Point(3, 3))
    .Bind<IPath>().To<Path>()
    .Root<IPath>("Path");

readonly struct Point(int x, int y);

class Path(ReadOnlySpan<Point> points) : IPath
{
    // Спан размещён на стеке — очень дёшево.
    // Сохранить его в поле нельзя (ref struct),
    // но обработать в конструкторе — пожалуйста
    public int PointCount { get; } = points.Length;
}
```

Внедрение `ref struct` в конструктор heap‑типа доступно для совместимости, но сопровождается предупреждением `DIW012` — предпочтительный путь для stack-only значений — это method injection, о нём ниже.

[Span и ReadOnlySpan](https://github.com/DevTeam/Pure.DI/blob/master/readme/span-and-readonlyspan.md)

---

### Фабрики для stack-only значений

Начиная с C# 13 стандартный `Func<TArg, TResult>` может принимать `ref struct`, если его generic‑параметры объявлены с `allows ref struct`; для этого также нужны совместимые reference assemblies современного BCL. Pure.DI предоставляет безопасный default‑биндинг `Func<ReadOnlySpan<char>, T>`: span остаётся локальным в рамках синхронного вызова фабрики и сразу передаётся в создаваемый граф зависимостей:

```csharp
DI.Setup(nameof(Composition))
    .Root<Func<ReadOnlySpan<char>, Parser>>("ParserFactory");

var composition = new Composition();
var parser = composition.ParserFactory("Hello".AsSpan());

class Parser
{
    [Ordinal]
    public void Initialize(ReadOnlySpan<char> text) { /* ... */ }
}
```

[Default Func с ReadOnlySpan](https://github.com/DevTeam/Pure.DI/blob/master/readme/default-func-with-readonlyspan.md)

Во‑вторых, поддерживаются собственные generic‑делегаты с `where T : allows ref struct` — stack-only значение прокидывается через `ctx.Override(...)` и потребляется немедленно, а в потокобезопасном контексте override и внедрение держатся под `lock (ctx.Lock)`:

```csharp
delegate bool ParserFactory<in T>(T text)
    where T : allows ref struct;

DI.Setup(nameof(Composition))
    .Bind<ParserFactory<ReadOnlySpan<char>>>().To(ctx => new ParserFactory<ReadOnlySpan<char>>(text =>
    {
        lock (ctx.Lock)
        {
            ctx.Override<ReadOnlySpan<char>>(text);
            ctx.Inject<Parser<ReadOnlySpan<char>>>(out var parser);
            return parser.Initialized;
        }
    }))
    .Root<ParserFactory<ReadOnlySpan<char>>>("ParserFactory");
```

`allows ref struct` работает также в generic‑корнях и root‑аргументах. `Bind<T>()`, `RootBind<T>()`, `Root<T>()` и `DefaultLifetime<T>()` принимают ref-like контракты; `Transient<T>()`, `PerResolve<T>()` и `PerBlock<T>()` допускают ref-like результаты, которые остаются локальными при построении корня. `Singleton<T>()` и `Scoped<T>()` могут принимать ref-like зависимости параметризованной фабрики, но её сохраняемый результат должен быть heap-safe — иначе Pure.DI сообщает `DIE046`.

[Allows ref struct factory](https://github.com/DevTeam/Pure.DI/blob/master/readme/allows-ref-struct-factory.md)

---

### Zero-copy парсинг и method injection на hot path

Когда стабильные зависимости сервиса живут долго, но есть данные, которые меняются на каждый вызов, per-call значение передаётся root‑аргументом и потребляется сразу в методе внедрения. Бонус C# 14: root‑аргумент `byte[]` может удовлетворить контракту внедрения типа `ReadOnlySpan<byte>` — Pure.DI применит span‑конверсию типа, предоставляемую компилятором, и парсер прочитает caller-owned массив без единого копирования:

```csharp
DI.Setup(nameof(Composition))
    .Bind<IMessageRegistry>().To<MessageRegistry>()
    .Bind<IPacketHandler>().To<PacketHandler>()
    .RootArg<byte[]>("frame")
    .Root<IPacketHandler>("Handle");

var composition = new Composition();
// Массив остаётся во владении вызывающего:
// после вызова его можно вернуть в пул
var packet = composition.Handle(frame);

sealed class PacketHandler(IMessageRegistry registry) : IPacketHandler
{
    [Ordinal(0)]
    public void Decode(ReadOnlySpan<byte> frame)
    {
        var messageId = BinaryPrimitives.ReadUInt16BigEndian(frame);
        // zero-copy разбор пакета...
    }
}
```

[Zero-copy парсинг сетевых пакетов](https://github.com/DevTeam/Pure.DI/blob/master/readme/zero-copy-network-packet-parsing.md)

Если аргумент сам stack-only (`.RootArg<ReadOnlySpan<char>>("path")`), сгенерированный корень (у нас метод) получает сигнатуру с модификатором `scoped` — компилятор гарантирует, что значение не переживёт текущий stack frame. Типичные сценарии: парсеры, роутеры, декодеры протоколов, конвейеры валидации.

[Method injection для hot path](https://github.com/DevTeam/Pure.DI/blob/master/readme/method-injection-for-a-hot-path.md)

---

### Диагностики для stack-only

Работа со стеком — место, где легко получить хитрую ошибку. Pure.DI переносит эти ошибки на этап компиляции — добавлен целый пакет диагностик:

- `DIE046` — stack-only зависимость нельзя использовать с «хранимыми» lifetime (Singleton, Scoped и т. п.)
- `DIE047` — stack-only зависимость нельзя внедрить в поле или свойство
- `DIE048` — stack-only реализацию нельзя внедрить через конверсию в интерфейс
- `DIE049` — stack-only зависимость нельзя захватить в сгенерированных делегатах и отложенных фабриках
- `DIW012` — предупреждение о внедрении stack-only зависимости в конструктор heap‑типа
- `DIW013` — предупреждение о несинхронизированном override stack-only значения в потокобезопасном контексте

Заметьте: там, где раньше вы получили бы малопонятную ошибку компилятора C# (вроде `CS9244`), Pure.DI выдаёт свою диагностику с объяснением и ссылкой на справку.

[Справочник диагностик Pure.DI](https://github.com/DevTeam/Pure.DI/blob/master/DIAGNOSTICS.md)

---

### Non-boxing union results

Возвращаемся к union types — теперь со стороны производительности. Компактный `union` хранит значение как `object`, то есть боксит value‑типы. Для горячих путей поддерживаются **кастомные** union‑результаты, которые хранят value-type case в отдельном поле — без боксинга, рефлексии и обёрток:

```csharp
DI.Setup(nameof(Composition))
    // CacheHit — value-type case внутри CacheLookupResult
    .Bind<CacheLookupResult>().To<CacheHit>()
    .Root<CacheLookupResult>("CachedProduct");

var result = new Composition().CachedProduct;
// Allocation-free доступ к результату
if (result.TryGetValue(out CacheHit hit)) { /* ... */ }

readonly record struct CacheHit(int ProductId, decimal Price);

readonly record struct CacheMiss(int ProductId);

// Кастомный union хранит value-type case напрямую
[System.Runtime.CompilerServices.Union]
readonly struct CacheLookupResult : System.Runtime.CompilerServices.IUnion
{
    private readonly byte _kind;
    private readonly CacheHit _hit;
    private readonly CacheMiss _miss;

    public CacheLookupResult(CacheHit hit) =>
        (_kind, _hit, _miss) = (1, hit, default);

    public CacheLookupResult(CacheMiss miss) =>
        (_kind, _hit, _miss) = (2, default, miss);

    public bool TryGetValue(out CacheHit value)
    {
        value = _hit;
        return _kind == 1;
    }

    // Value — обязательный общий fallback (боксит value-type case),
    // используйте его только для диагностики
    public object? Value => _kind switch
    {
        1 => _hit,
        2 => _miss,
        _ => null
    };
}
```

[Non-boxing union result](https://github.com/DevTeam/Pure.DI/blob/master/readme/non-boxing-union-result.md)

---

### Каталог high-performance примеров

Вместе с новыми возможностями появилась целая серия примеров «DI на hot path»:

- **[ArrayPool-буферы](https://github.com/DevTeam/Pure.DI/blob/master/readme/arraypool-buffer.md)** — `ArrayPool<T>` поддерживается из коробки: запрашивайте `ArrayPool<byte>` как обычную зависимость, а возврат буфера в пул привязывайте к dispose владельца через `Owned<T>`
- **[Пул объектов](https://github.com/DevTeam/Pure.DI/blob/master/readme/object-pool.md)** — Singleton‑пул «тёплых» объектов плюс короткоживущий lease на каждый вызов корня: переиспользование явное, утечек за пределы скоупа нет
- **[Фабрики без захвата замыканий](https://github.com/DevTeam/Pure.DI/blob/master/readme/factory-without-closure-capture.md)** — зависимости объявляются параметрами лямбды (`.To((CurrencyFormatter currency, TaxPolicy tax) => ...)`), а не захватываются из окружения — код фабрики детерминирован и allocation-friendly
- **[Struct-зависимости](https://github.com/DevTeam/Pure.DI/blob/master/readme/struct-dependency.md)** — небольшая value-type зависимость создаётся напрямую и не требует отдельной heap‑аллокации
- **[ValueTask-корни](https://github.com/DevTeam/Pure.DI/blob/master/readme/valuetask-root.md)** — корень вида `Root<ValueTask<IFeatureSnapshot>>` без аллокации `Task<T>` для синхронного случая
- **[ThreadSafe = Off](https://github.com/DevTeam/Pure.DI/blob/master/readme/threadsafe-off-for-single-thread-composition.md)** — если композиция создаётся и используется на одном потоке (CLI‑утилита, фаза инициализации game loop), хинт `.Hint(Hint.ThreadSafe, "Off")` убирает сгенерированную синхронизацию полностью

---

### Оптимизация генерации кода

- поле `_lock` теперь создаётся только тогда, когда на него действительно ссылается хотя бы один `lock (...)` — раньше оно создавалось «на всякий случай» для любой композиции, расходуя память в каждом экземпляре
- весь сгенерированный код помечается `[GeneratedCode]` — анализаторы, coverage и code-style инструменты могут распознавать его и, в зависимости от настроек, исключать из анализа или покрытия; в сгенерированный класс встраивается фактическая версия пакета Pure.DI
- при построении графа и генерации добавлены кэширование, предпочтение более дешёвого синтаксического анализа там, где он достаточен, и устранение части повторных попыток построения графа — это снижает накладные расходы генератора во время компиляции

---

## Полезные мелочи

Короткой строкой о том, что не тянет на отдельный раздел, но пригодится:

**Новые свойства контекста фабрик.** К `ctx.Tag`, `ctx.ConsumerType` и `ctx.Lock` добавились [`ctx.RootName`](https://github.com/DevTeam/Pure.DI/blob/master/readme/root-name.md), [`ctx.RootType`](https://github.com/DevTeam/Pure.DI/blob/master/readme/root-type.md) и [`ctx.IsLockRequired`](https://github.com/DevTeam/Pure.DI/blob/master/readme/islockrequired.md):

```csharp
// Логгер знает, через какой корень его строят
.Bind().To(ctx => new Logger(ctx.RootName))

// Блокировка — только когда она действительно нужна
.Bind().To(ctx =>
{
    if (ctx.IsLockRequired)
    {
        lock (ctx.Lock) { /* ... */ }
    }
    // ...
})
```

**Внедрение словарей.** Зависимость `IReadOnlyDictionary<TKey, TValue>` собирается автоматически из привязок `KeyValuePair<TKey, TValue>` с `Tag.Unique` — удобно для выбора реализации по ключу в рантайме:

```csharp
.Bind(Tag.Unique).To((EmailChannel c) => new KeyValuePair<Channel, INotificationChannel>(Channel.Email, c))
.Bind(Tag.Unique).To((SmsChannel c) => new KeyValuePair<Channel, INotificationChannel>(Channel.Sms, c))

class NotificationService(IReadOnlyDictionary<Channel, INotificationChannel> channels);
```

[Dictionary: выбор зависимости по ключу](https://github.com/DevTeam/Pure.DI/blob/master/readme/dictionary.md)

**Больше BCL-типов из коробки.** `TimeProvider`, `TaskCompletionSource<T>`, `CultureInfo`, `IFormatProvider`, `StringComparer`, `IReadOnlySet<T>`, `RandomNumberGenerator` и другие внедряются без дополнительных усилий — а при необходимости [default‑привязку можно переопределить](https://github.com/DevTeam/Pure.DI/blob/master/readme/default-bcl-bindings.md).

**Фабрики до 16 параметров**, обновление Roslyn до 5.6.

**Пример на Uno Platform.** Кроссплатформенное приложение на single-project Uno SDK: композиция как XAML‑ресурс, virtual‑корни для view model, `DesignTimeComposition` с override‑корнями для дизайнера — и всё это без runtime‑контейнера. [Подробное описание примера](https://github.com/DevTeam/Pure.DI/blob/master/readme/UnoApp.md).

---

## Вместо заключения

Если есть желание попробовать новые возможности, можно начать [с любого примера](https://github.com/DevTeam/Pure.DI?tab=readme-ov-file#examples) — они независимы и запускаются легко через `dotnet run`. А если чего-то не хватает — не стесняйтесь создать тикет в [репозитории Pure.DI на GitHub](https://github.com/DevTeam/Pure.DI).

Спасибо за интерес и что дочитали до конца!
