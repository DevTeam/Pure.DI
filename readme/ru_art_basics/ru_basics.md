# Pure.DI: DI без контейнера, без отражения типов .NET и с проверкой на этапе компиляции

Pure.DI — это **генератор кода C# для внедрения зависимостей**, который строит граф зависимостей **на этапе компиляции** и генерирует обычный C#‑код создания объектов. В результате вы получаете «чистый DI»: без сервис-локатора, без рефлексии и с выявлением проблем на этапе компиляции.

Эта статья — про базовые возможности Pure.DI. Она рассчитана на разработчиков, которые пишут на C#, уже сталкивались с DI‑контейнерами и хотят:

- повысить предсказуемость и производительность DI;
- видеть реальный объектный граф, а не «магический» runtime‑контейнер;
- выявлять ошибки конфигурации DI на этапе компиляции;
- использовать DI в средах, где .NET Reflection нежелателен (AOT, Unity, старые фреймворки, библиотечные проекты).

Подробности: [Pure.DI README](https://github.com/DevTeam/Pure.DI)

---

## DI: что мы на самом деле пытаемся решить

Представьте типичный прикладной сценарий: сервис оформляет заказ, записывает события в лог и взаимодействует с внешним платежным шлюзом.

Если зависимости создаются «внутри» сервисов, код быстро превращается в клубок:

```c#
// Плохо: сервис сам определяет, что и как создавать
sealed class CheckoutService
{
    private readonly HttpClient _http = new();
    private readonly PaymentGatewayClient _gateway;
    private readonly ILogger _logger = new ConsoleLogger();

    public CheckoutService()
    {
        _gateway = new PaymentGatewayClient(_http, apiKey: "hardcoded");
    }

    public Task CheckoutAsync(Order order) => _gateway.PayAsync(order);
}
```

Проблемы здесь стандартные:

- **Тестирование**: сложно подменить `PaymentGatewayClient` и `HttpClient`.
- **Конфигурация**: API‑ключи и настройки оказываются внутри бизнес‑кода.
- **Время жизни**: кто и когда должен освобождать ресурсы?
- **Сильное связывание**: доменная логика начинает зависеть от инфраструктуры.

DI — это всего лишь инструмент: *объект не должен создавать свои зависимости; ему их передают извне*. Чаще всего — через конструктор.

```c#
sealed class CheckoutService(IPaymentGateway gateway, ILogger logger)
{
    public Task CheckoutAsync(Order order) => gateway.PayAsync(order);
}
```

Возникает вопрос: **кто же будет создавать `IPaymentGateway`, `ILogger` и сам `CheckoutService`?**

Ответ — *композиция* приложения.

---

## Pure DI: «контейнера нет», а внедрение зависимостей есть

В классическом подходе вы настраиваете DI‑контейнер, а затем во время выполнения контейнер строит граф зависимостей и предоставляет объекты по запросу.

**Pure DI** — подход, где контейнера как runtime‑сущности нет. Есть только:

- **композиция объектов** (как собрать граф в конкретный объект),
- **корни композиции** (*composition roots*) — входные точки, из которых строится нужная композиция.

В идеальном мире вы хотите, чтобы:

- создание объектов осуществлялось **обычным кодом** (без рефлексии и динамических вызовов),
- композиция объектов была **прозрачной** и отлаживаемой,
- ошибки «не хватает зависимости» или «цикл в графе» выявлялись **до выхода в продакшен**.

Именно это и делает Pure.DI.

---

## Что такое Pure.DI — простыми словами

Pure.DI — это **compile-time DI code generator**: вы определяете граф зависимостей (привязки, теги, времена жизни, корни), а генератор:

1. анализирует этот граф зависимостей на этапе компиляции;
2. проверяет, что граф корректен (нет «дыр», циклов, недоступных конструкторов и т.п.);
3. **генерирует** partial‑класс композиции с обычными свойствами/методами, которые создают композиции объектов, с корнями, как "начальными" объектами композиции.

Очевидные ключевые преймущества данного подхода:

- **Zero Overhead**: в рантайме нет контейнера, нет сканирования сборок, нет отражения типов; создается ровно то, что вы бы написали руками — цепочка `new`.
- **Compile‑Time Validation**: ошибки настройки DI становятся ошибками/предупреждениями компиляции.
- **Works everywhere**: никаких runtime‑зависимостей — можно использовать хоть в .NET Framework 2.0+, хоть в AOT/Unity‑сценариях.
- **Transparency**: можно посмотреть и отладить сгенерированный код так же как и обычный ваш код.
- **Built‑in BCL Support**: множество типов из .NET BCL (`Func<>`, `Lazy<>`, `IEnumerable<>`, `Task`, `ValueTask`, `Span`, `Tuple` и др.) поддерживаются «из коробки».

---

## Чем это лучше «обычного» DI‑контейнера в реальной работе

Ниже приведены только практические преймущества.

### Предсказуемая производительность

Если DI сводится к сгенерированному коду `new A(new B(new C()))`, то:

- нет затрат на рефлексию/динамику;
- нет скрытых аллокаций на построение графа;
- нет иной «магии», что упрощает профилирование и оптимизацию.

### Ошибки — в компиляции, а не в рантайме

В классическом контейнере вы можете долгое время не замечать ошибку регистрации, пока путь выполнения (может даже очень редкий) не приведёт к проблемному графу объектов. Pure.DI строит граф заранее — и сразу сообщает о проблемах. Проблемный код просто не скомпилируется, как бы вы не пытались.

### Понятные корни композиции вместо бесконечного `Resolve<T>()`

В Pure.DI корни композиции — это **явные** свойства или методы. Это дисциплинирует архитектуру: вы точно знаете, какие объекты ваш «контейнер» предоставляет извне. Ни какого Sevice Locator.

### Удобно для библиотек и ограниченных окружений

Если вы пишете библиотеку, модуль, плагин, Unity‑код или AOT‑приложение — отсутствие runtime‑зависимостей и рефлексии часто становится решающим фактором.

---

## Быстрый старт

Перед началом полезно знать два технических требования (и важно понимать, что они относятся к генератору, а не к вашему приложению):

- для компиляции нужен **.NET SDK 6.0.4+** (при этом проекты могут таргетить и более старые платформы, вплоть до .NET Framework 2.0+);
- **C# 8+** требуется только тем проектам, где включён source generator Pure.DI (остальные проекты решения могут быть на любой версии C#).

Какие пакеты бывают (чаще всего вам нужен только первый):

- [Pure.DI](https://www.nuget.org/packages/Pure.DI) — генератор кода DI;
- [Pure.DI.Abstractions](https://www.nuget.org/packages/Pure.DI.Abstractions) — общие абстракции/атрибуты;
- [Pure.DI.MS](https://www.nuget.org/packages/Pure.DI.MS) — дополнения для интеграции с Microsoft DI;
- [Pure.DI.Templates](https://www.nuget.org/packages/Pure.DI.Templates) — шаблоны для создания проектов из командной строки.

Минимальный пример: есть сервис, отправляющий письма, и мы хотим создать его через DI.

1) Добавьте пакет:

- [Pure.DI на NuGet](https://www.nuget.org/packages/Pure.DI)

2) Опишите привязки и корень композиции:

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IClock>().To<SystemClock>()
    .Bind<IEmailSender>().To<SmtpEmailSender>()
    .Bind<INotificationService>().To<NotificationService>()
    // Корень композиции: публичная точка входа в граф
    .Root<INotificationService>("Notifications");

var composition = new Composition();
composition.Notifications.SendWelcome("dev@company.com");

interface IClock
{
    DateTimeOffset Now { get; }
}

sealed class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.UtcNow;
}

interface IEmailSender
{
    void Send(string to, string subject, string body);
}

sealed class SmtpEmailSender(IClock clock) : IEmailSender
{
    public void Send(string to, string subject, string body)
    {
        // Здесь могла бы быть реальная отправка
        Console.WriteLine($"[{clock.Now:u}] -> {to}: {subject}");
    }
}

interface INotificationService
{
    void SendWelcome(string email);
}

sealed class NotificationService(IEmailSender sender) : INotificationService
{
    public void SendWelcome(string email) =>
        sender.Send(email, "Добро пожаловать!", "Рады видеть вас в системе.");
}
```

На этом шаге генератор создаст partial‑класс `Composition` и свойство `Notifications`, которое вернёт собранный граф. Все очень просто.

См. также реальные примеры в репозитории:

- [Пример с котом Щредингера](https://github.com/DevTeam/Pure.DI?tab=readme-ov-file#schr%C3%B6dingers-cat-demonstrates-how-it-all-works-)
- [Пример привязки абстракций к реализациям](https://github.com/DevTeam/Pure.DI/blob/master/readme/injections-of-abstractions.md)
- [Пример автоматического связывания (auto‑bindings)](https://github.com/DevTeam/Pure.DI/blob/master/readme/auto-bindings.md)
- [Как устроены корни композиции](https://github.com/DevTeam/Pure.DI/blob/master/readme/composition-roots.md)

---

## Класс композиции

С точки зрения архитектуры, `Composition` — это место, где:

- определены **привязки** (bindings): «вместо `IEmailSender` используем `SmtpEmailSender`»;
- определены **корни** (roots): «наружу выдаем только `INotificationService Notifications`»;
- настроены **времена жизни** зависимостей;
- (опционально) задаются **подсказки генератору** (hints).

Важный момент: Pure.DI не «скрывается» где-то в рантайме. В проекте появляется **обычный класс**, который можно открыть, отладить и подробно изучить.

См. также:

- [Пример корней композиции и Resolve‑методов](https://github.com/DevTeam/Pure.DI/blob/master/readme/resolve-methods.md)
- [Как отключить генерацию Resolve‑методов](https://github.com/DevTeam/Pure.DI/blob/master/readme/resolve-hint.md)

---

## Привязки (bindings): как связать интерфейсы с реализациями

Базовая форма привязки:

```c#
.Bind<IContract>().As(Lifetime).To<Implementation>()
```

Это ровно то, что вы привыкли видеть в DI‑контейнерах, но результатом будет сгенерированный код.

Реальный сценарий: два способа доставки — курьер и постамат. Бизнес‑код зависит от `IDeliveryService`, а конкретная реализация выбирается на уровне композиции - инфрастуктуры, специализированной для создания объектов конкретных типов.

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IDeliveryService>().To<CourierDelivery>()
    .Bind<IOrderService>().To<OrderService>()
    .Root<IOrderService>("Orders");

var composition = new Composition();
composition.Orders.PlaceOrder();

interface IDeliveryService
{
    void Ship();
}

sealed class CourierDelivery : IDeliveryService
{
    public void Ship() => Console.WriteLine("Едет курьер");
}

interface IOrderService
{
    void PlaceOrder();
}

sealed class OrderService(IDeliveryService delivery) : IOrderService
{
    public void PlaceOrder()
    {
        // бизнес‑логика...
        delivery.Ship();
    }
}
```

См. также:

- [Пример привязок абстракций к реализациям](https://github.com/DevTeam/Pure.DI/blob/master/readme/injections-of-abstractions.md)

---

## Автоматические привязки: удобно, но будте бдительны

Pure.DI умеет создавать **неабстрактные** типы без явных привязок. Это удобно для небольших приложений, утилит и демонстраций: вы объявляете корень, а зависимости «подтягиваются» автоматически.

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    .Root<ReportService>("Reports");

var composition = new Composition();
var service = composition.Reports;

sealed class FileSystem;

sealed class ReportService(FileSystem fs);
```

Однако в реальном приложении автоматическое связывание быстро упирается в архитектурные ограничения:

- сложнее соблюдать принцип инверсии зависимостей (вы начинаете зависеть от конкретных классов);
- сложнее управлять раличными реализациями одних и тех же абстракций и декораторами.

Поэтому типичная рекомендация — **зависеть от абстракций** и явно связывать их с реализациями.

См. также:

- [Пример автоматического связывания](https://github.com/DevTeam/Pure.DI/blob/master/readme/auto-bindings.md)

---

## Фабрики: когда нужно больше, чем просто вызвать конструктор

Иногда объект невозможно создать только через конструктор:

- нужна ручная инициализация (подключиться к БД, прогреть кэш, собрать конфиг);
- объект создается через сторонний API;
- нужно выполнить доп. проверку/настройку.

В Pure.DI для этого есть привязка к фабрике:

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IDbConnection>().To<DbConnection>(ctx =>
    {
        // Inject() создаёт DbConnection со всеми зависимостями (если они есть)
        ctx.Inject(out DbConnection conn);
        conn.Open();
        return conn;
    })
    .Bind<IRepository>().To<Repository>()
    .Root<IRepository>("Repo");

var composition = new Composition();
var repo = composition.Repo;

interface IDbConnection;

sealed class DbConnection : IDbConnection
{
    public void Open() { /* ... */ }
}

interface IRepository;

sealed class Repository(IDbConnection connection) : IRepository;
```

А если фабрика простая, можно описать её коротко — параметры лямбды будут внедрены автоматически:

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind().To(() => DateTimeOffset.UtcNow)
    .Bind<ILogFileName>().To((DateTimeOffset now) =>
        new LogFileName($"app-{now:yyyy-MM-dd}.log"))
    .Root<ILogFileName>("LogName");

var composition = new Composition();
Console.WriteLine(composition.LogName.Value);

interface ILogFileName
{
    string Value { get; }
}

sealed record LogFileName(string Value) : ILogFileName;
```

См. также:

- [Фабрика с ручной инициализацией](https://github.com/DevTeam/Pure.DI/blob/master/readme/factory.md)
- [Упрощённая фабрика (зависимости — параметрами лямбды)](https://github.com/DevTeam/Pure.DI/blob/master/readme/simplified-factory.md)

---

## Упрощённые привязки: когда контракт можно определить автоматически

В больших проектах рутина — это не DI как концепция, а количество строк его настройки. Pure.DI позволяет упростить записи.

`Bind().To<Implementation>()` связывает **сам тип** и его **не-специальные** абстракции, которые он реализует напрямую.

```
DI.Setup(nameof(Composition))
    .Bind().To<SmtpEmailSender>()
    .Root<IEmailSender>("Sender");

var composition = new Composition();
composition.EmailSender.Send("...");

interface IEmailSender { ... }

class SmtpEmailSender : IEmailSender { ... }
```

Это полезно, когда класс реализует один или несколько «обычных» контрактов, и вы не хотите перечислять их вручную.
 
Есть варианты «привязки по времени жизни» через методы `Singleton<>()`, `PerResolve<>()` и т.д.

```
DI.Setup(nameof(Composition))
    .Singleton<SystemClock>()
    .Transient<EmailSender>()
    .Root<IEmailSender>("Sender");

var composition = new Composition();
composition.EmailSender.Send("...");

interface ISystemClock;

sealed class SystemClock: ISystemClock;

interface IEmailSender { ... }

class EmailSender(ISystemClock clock): IEmailSender { ... }
```

См. также:

- [Упрощённые привязки без указания контракта](https://github.com/DevTeam/Pure.DI/blob/master/readme/simplified-binding.md)
- [Короткие методы привязки с временем жизни в названии](https://github.com/DevTeam/Pure.DI/blob/master/readme/simplified-lifetime-specific-bindings.md)

---

## Теги (tags): несколько реализаций одного контракта

В реальном приложении часто бывает несколько реализаций одного интерфейса:

- разные способы логирования (файл, консоль, telemetry);
- разные клиенты API (public/internal);
- разные провайдеры платежей (банковская карта/банковский перевод/подарочные сертификаты).

Теги позволяют выбрать реализацию **явно**, не создавая дополнительные интерфейсы:

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IPaymentClient>("Sandbox").To<SandboxPaymentClient>()
    .Bind<IPaymentClient>("Prod").To<ProdPaymentClient>()
    .Bind<CheckoutService>().To<CheckoutService>()
    .Root<CheckoutService>("Checkout");

var composition = new Composition();
var root = composition.Checkout;

interface IPaymentClient;

sealed class SandboxPaymentClient : IPaymentClient;

sealed class ProdPaymentClient : IPaymentClient;

sealed class CheckoutService(
    [Tag("Prod")] IPaymentClient client)
{
    // ...
}
```

См. также:

- [Пример использования тегов](https://github.com/DevTeam/Pure.DI/blob/master/readme/tags.md)
- [Умные теги (без строк и опечаток)](https://github.com/DevTeam/Pure.DI/blob/master/readme/smart-tags.md)

---

## Время жизни (lifetimes)

Pure.DI поддерживает привычные времена жизни. Важно: это не «особенность DI», а инструмент управления ресурсами и изоляцией состояния.

Ниже — упрощённая шпаргалка (в терминах поведения):

| Lifetime | Когда создается | Когда один и тот же экземпляр переиспользуется |
|---|---|---|
| `Transient` | каждый раз заново | никогда |
| `Singleton` | один раз на экземпляр `Composition` | везде в рамках одной композиции |
| `PerResolve` | при каждом обращении к корню | внутри одного корня |
| `PerBlock` | внутри блока построения | позволяет сократить число инстансов (детали зависят от графа) |
| `Scoped` | на scope‑композицию | внутри одного scope |

Практический ориентир:

- `Transient` — безопасное значение по умолчанию для большинства stateless‑сервисов.
- `Singleton` — для кэшей/пулов/метаданных, но требует **потокобезопасности** и аккуратности.
- `Scoped` — для «ресурсов запроса»: DbContext/UnitOfWork/RequestTelemetry.

См. также:

- [Transient: новый объект каждый раз](https://github.com/DevTeam/Pure.DI/blob/master/readme/transient.md)
- [Singleton: один объект на Composition](https://github.com/DevTeam/Pure.DI/blob/master/readme/singleton.md)
- [PerResolve: один объект на корень](https://github.com/DevTeam/Pure.DI/blob/master/readme/perresolve.md)
- [PerBlock: снижает число инстансов](https://github.com/DevTeam/Pure.DI/blob/master/readme/perblock.md)
- [Scope/Scoped: «на запрос»](https://github.com/DevTeam/Pure.DI/blob/master/readme/scope.md)

---

## Аргументы композиции: как передавать внешнее состояние без глобальных статических переменных

DI плохо сочетается с передачей дополнителого внешенего состояния (каких то данных) в создаваемый объект. Но в Pure.DI **Composition arguments** превращают внешние состояние в зависимости, доступные в графе как любые другие:

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IApiClient>().To<ApiClient>()
    .Root<IApiClient>("Api")
    .Arg<string>("baseUrl")
    .Arg<string>("token", "api token");

var composition = new Composition(
    baseUrl: "https://api.company.com",
    token: "secret");

var api = composition.Api;

interface IApiClient;

sealed class ApiClient(
    string baseUrl,
    [Tag("api token")] string token) : IApiClient;
```

См. также:

- [Пример аргументов композиции](https://github.com/DevTeam/Pure.DI/blob/master/readme/composition-arguments.md)

---

## Аргументы корня: когда параметры нужны только в одной точке входа

Иногда значения должны передаваться **не в Composition**, а в конкретный корень: например, обработчик команды получает `userId`, а остальная часть композиции от этого не зависит.

Для этого есть **Root arguments**. Такой корень становится методом:

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    // RootArg несовместим с Resolve-методами (лучше отключить)
    .Hint(Hint.Resolve, "Off")
    .RootArg<Guid>("userId")
    .Bind<IUserService>().To<UserService>()
    .Root<IUserService>("CreateUserService");

var composition = new Composition();
var service = composition.CreateUserService(userId: Guid.NewGuid());

interface IUserService;

sealed class UserService(Guid userId) : IUserService;
```

См. также:

- [Пример root arguments](https://github.com/DevTeam/Pure.DI/blob/master/readme/root-arguments.md)

---

## Генерация и использование: корни как свойства (и почему это удобно)

Ключевая особенность Pure.DI — корни композиции становятся **обычными** членами класса, свойствами или методами.

Это меняет стиль использования:

- корень легко подставить в UI‑binding (WPF/MAUI/Avalonia), потому что это свойство/метод;
- IDE и компилятор помогают с навигацией;
- легко документировать;
- зависимость превартилась в композицию только если вы явно объявили для неё корень.

При желании можно разрешать зависимости через `Resolve`, но лучше воспринимать это как «привычный, но не всегда рациональный подход».

См. также:

- [Resolve‑методы и их ограничения](https://github.com/DevTeam/Pure.DI/blob/master/readme/resolve-methods.md)

---

## Resolve‑методы: удобно, но это Sevice Locator

Pure.DI может генерировать методы `Resolve<T>()` и `Resolve(Type)` — это иногда полезно, например, для интеграции с кодом, который предпочитает работать с классическими DI контейнерами. Но по сути это **Service Locator**, со всеми классическими недостатками:

- API позволяет «получить что угодно» - это потеря контроля;
- появляется риск runtime‑исключений, например, когда нет соответсвующей привязки к конкретному (не абстрактному) типу;
- код становится сложнее анализировать и тестировать.

Если вы хотите строгую и чистую архитектуру, `Resolve` обычно отключают:

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    .Hint(Hint.Resolve, "Off")
    // ...
    .Root<App>("App");
```

См. также:

- [Подсказка Resolve (включение/выключение)](https://github.com/DevTeam/Pure.DI/blob/master/readme/resolve-hint.md)

---

## Способы внедрения зависимостей

В большинстве проектов хватает **constructor injection** — это и проще, и безопаснее, и отлично поддерживается анализаторами/IDE.

Но иногда удобны и другие варианты (например, при «достройке» объекта, созданного извне).

Pure.DI поддерживает:

- внедрение через конструктор;
- внедрение через свойства;
- внедрение через поля;
- внедрение через методы.

Свойства, поля и методы всего лишь нужно пометить атрибутом `[Dependency]` или дургими такими как `[Ordinal]`, `[Tag]`, `[Type]`, `[Inject]`. Но вы всегда легко можете расширить их своими атрибутами, тем самым сделав ваш код полностью независимым от DI. 

См. также:

- [Внедрение через свойства](https://github.com/DevTeam/Pure.DI/blob/master/readme/property-injection.md)
- [Внедрение через поля](https://github.com/DevTeam/Pure.DI/blob/master/readme/field-injection.md)
- [Внедрение через методы](https://github.com/DevTeam/Pure.DI/blob/master/readme/method-injection.md)

---

## Builders (BuildUp): когда нет возможности контролировать создание объекта

Иногда объект появляется «снаружи»:

- десериализация (JSON → объект);
- плагины/скрипты;
- игровые сущности, создаваемые движком;
- UI‑элементы, которые создает фреймворк.

В таких случаях полезен паттерн **BuildUp**: у вас уже есть экземпляр, и вы хотите «добавить» в него зависимости через поля/свойства/методы, отмеченные атрибутами внедрения, уже упомянутыми выше.

Pure.DI умеет генерировать **builders** для типов, производных от базового `T`, известных на этапе компиляции.

См. также:

- [Builders: достройка существующих объектов](https://github.com/DevTeam/Pure.DI/blob/master/readme/builders.md)
- [Builder с аргументами](https://github.com/DevTeam/Pure.DI/blob/master/readme/builder-with-arguments.md)

---

## Обобщения: почему Pure.DI предлагает маркерные типы вместо «open generics»

Классические DI‑контейнеры часто регистрируют «open generics» вроде `IRepository<> → Repository<>`. Это удобно, но в сложных графах появляется неоднозначность: как именно сопоставить аргументы типов, особенно когда интерфейсы и реализации используют разные порядки или имена параметров.

В Pure.DI вместо «open generics» используется подход с **маркерными типами** (например, `TT`, `TT1`, `TT2`). Это делает сопоставление абсолютно **точным**.

На практике это выглядит так:

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<IRepository<TT>>().To<Repository<TT>>()
    .Bind<IDataService>().To<DataService>()
    .Root<IDataService>("Data");

var composition = new Composition();
var data = composition.Data;

interface IRepository<T>;

sealed class Repository<T> : IRepository<T>;

sealed record User;

sealed record Order;

interface IDataService;

sealed class DataService(
    IRepository<User> users,
    IRepository<Order> orders) : IDataService;
```

См. также:

- [Обобщения и маркерные типы](https://github.com/DevTeam/Pure.DI/blob/master/readme/generics.md)
- [Сложные обобщения](https://github.com/DevTeam/Pure.DI/blob/master/readme/complex-generics.md)

---

## Внедрение по требованию: Func, Lazy и фабрики

Иногда сервису нужно создавать зависимости **не сразу**, а по мере необходимости:

- «тяжёлая» зависимость (инициализация драйвера, прогрев данных);
- много экземпляров одного типа (элементы списка, игровые сущности);
- зависимости с параметрами времени выполнения.

Pure.DI поддерживает фабричные делегаты (`Func<T>`, `Func<TArg, T>`, `Func<TArg1, TArg2, ..., T>`) и `Lazy<T>` из коробки.

Пример: сервис формирует несколько одноразовых токенов, каждый раз создавая новый объект:

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    .Bind<ITokenGenerator>().To<TokenGenerator>()
    .Bind<ITokenService>().To<TokenService>()
    .Root<ITokenService>("Tokens");

var composition = new Composition();
composition.Tokens.IssueBatch(3);

interface ITokenGenerator
{
    string Next();
}

sealed class TokenGenerator : ITokenGenerator
{
    public string Next() => Guid.NewGuid().ToString("N");
}

interface ITokenService
{
    void IssueBatch(int count);
}

sealed class TokenService(Func<ITokenGenerator> generatorFactory) : ITokenService
{
    public void IssueBatch(int count)
    {
        for (var i = 0; i < count; i++)
        {
            var gen = generatorFactory(); // новый экземпляр по требованию
            Console.WriteLine(gen.Next());
        }
    }
}
```

См. также:

- [Injection on demand (Func<T>)](https://github.com/DevTeam/Pure.DI/blob/master/readme/injection-on-demand.md)
- [Injection on demand с аргументами (Func<TArg, T>)](https://github.com/DevTeam/Pure.DI/blob/master/readme/injections-on-demand-with-arguments.md)
- [BCL: Func<T>](https://github.com/DevTeam/Pure.DI/blob/master/readme/func.md)
- [BCL: Lazy<T>](https://github.com/DevTeam/Pure.DI/blob/master/readme/lazy.md)

---

## Поддержка BCL: когда стандартные типы «просто работают»

В традиционных контейнерах многие задачи решаются специальными расширениями. В Pure.DI значительная часть полезных типов из стандартной библиотеки классов платформы .NET (BCL) поддержана "из коробки".

Из часто используемого:

- `Func<T>`, `Func<TArg1, T>`, `Func<TArg1, TArg2, ..., T>` — фабрики;
- `Lazy<T>` — ленивое создание;
- `IEnumerable<T>`, массивы — коллекции зависимостей;
- `Task<T>`, `ValueTask<T>` — для async‑корней;
- `IServiceProvider`/Service collection сценарии (когда нужно интегрироваться с экосистемой Microsoft DI).

См. также:

- [BCL: Enumerable](https://github.com/DevTeam/Pure.DI/blob/master/readme/enumerable.md)
- [BCL: Enumerable generics](https://github.com/DevTeam/Pure.DI/blob/master/readme/enumerable-generics.md)
- [BCL: Task](https://github.com/DevTeam/Pure.DI/blob/master/readme/task.md)
- [BCL: ValueTask](https://github.com/DevTeam/Pure.DI/blob/master/readme/valuetask.md)
- [BCL: Service provider](https://github.com/DevTeam/Pure.DI/blob/master/readme/service-provider.md)
- [BCL: Service collection](https://github.com/DevTeam/Pure.DI/blob/master/readme/service-collection.md)

---

## Декораторы и перехват: добавляем логирование и метрики без переписывания кода

Перехват (interception) в Pure.DI в базовом варианте хорошо сочетается с паттерном **Decorator**: мы «упаковываем» реализацию в другую реализацию того же интерфейса.

Реальный пример: оборачиваем `IOrderService` в логирующий декоратор.

```c#
using Pure.DI;

DI.Setup(nameof(Composition))
    // "base" — базовая реализация
    .Bind("base").To<OrderService>()
    // Декоратор использует "base"
    .Bind<IOrderService>().To<LoggingOrderService>()
    .Bind<ILogger>().To<ConsoleLogger>()
    .Root<IOrderService>("Orders");

var composition = new Composition();
composition.Orders.PlaceOrder("ORD-42");

interface ILogger
{
    void Info(string message);
}

sealed class ConsoleLogger : ILogger
{
    public void Info(string message) => Console.WriteLine(message);
}

interface IOrderService
{
    void PlaceOrder(string id);
}

sealed class OrderService : IOrderService
{
    public void PlaceOrder(string id) =>
        Console.WriteLine($"Заказ {id} оформлен");
}

sealed class LoggingOrderService(
    ILogger log,
    [Tag("base")] IOrderService inner) : IOrderService
{
    public void PlaceOrder(string id)
    {
        log.Info($"Начинаем оформление {id}");
        inner.PlaceOrder(id);
        log.Info($"Завершили оформление {id}");
    }
}
```

См. также:

- [Декоратор: базовый сценарий перехвата](https://github.com/DevTeam/Pure.DI/blob/master/readme/decorator.md)
- [Interception: дополнительные возможности](https://github.com/DevTeam/Pure.DI/blob/master/readme/interception.md)

---

## Hints: дополнительные настройки генерации, которые помогают при разработке

Hints — это тонкие настройки генератора «как именно генерировать код». Для базового старта достаточно знать три:

### ToString: увидеть граф зависимостей

Можно включить генерацию `ToString()`, который возвращает диаграмму в формате mermaid — удобно для ревью и обсуждения архитектуры.

См. также:

- [Hint ToString: диаграмма графа](https://github.com/DevTeam/Pure.DI/blob/master/readme/tostring-hint.md)

### ThreadSafe: отключить потокобезопасность, если вы уверены

По умолчанию генерация учитывает многопоточность. Но иногда композиция объектов строится строго в одном потоке (например, при запуске приложения), и можно получить чуть большую производительность.

См. также:

- [Hint ThreadSafe](https://github.com/DevTeam/Pure.DI/blob/master/readme/threadsafe-hint.md)

### OnDependencyInjection: точка для «динамического перехвата»

Если требуется централизованно «отслеживать» или модифицировать процесс внедрения (логирование, метрики, контроль), можно включить генерацию partial‑метода `OnDependencyInjection`.

См. также:

- [OnDependencyInjection (wildcard)](https://github.com/DevTeam/Pure.DI/blob/master/readme/ondependencyinjection-wildcard-hint.md)
- [OnDependencyInjection (regexp)](https://github.com/DevTeam/Pure.DI/blob/master/readme/ondependencyinjection-regular-expression-hint.md)

---

## Практические рекомендации по внедрению Pure.DI в проект

Для успешного внедрения рекомендуется следующая последовательность:

1) **Определите корни композиции**. Обычно это «точки входа» приложения: `App`, `MainController`, `MessageHandler`, `BackgroundWorker`. Минимизируйте их количество, в идеале до одного корня.
2) **Сведите создания объектов к композиции**. В остальных модулях пусть остаётся только бизнес‑код и контракты. Чем меньше объектов создаётся вручную, тем лучше.
3) **Начните с constructor injection**. Другие виды внедрения используйте как инструмент для build-up и интеграции.
4) **Используйте автосвязывание с осторожностью**: для демонстраций — отлично, для хорошей архитектуры — часто лишнее.
5) **Не злоупотребляйте Singleton**. И если он необходим — убедитесь в потокобезопасности и корректном освобождении ресурсов. Помните про захват зависимостей - если Singleton используется какую-то зависимость с другим временем жизни, то эта зависимость будет так же Singleton.
6) **Не делайте использование методов `Resolve()` основной моделью**. Корни композиции — это ваш путь к чистой архитектуре и спокойному обновлению в прод окружении.
7) **Держите конструкторы простыми**: без тяжёлой логики и операций ввода-вывода. Это позволит вам не задумываться о размерах композиции и не заставит использовать различные уловки, такие как отложенное создание объектов. Если необходимо — используйте фабрики или отдельную логику инициализации. 
8) **Не увлекайтесь фабриками**: фабрики это всегда дополнительная логика, которая требует поддержки. Используйте обычные привязки к реализациям.

---

## Заключение

Pure.DI делает DI предсказуемым и прозрачным:

- компилятор гарантирует корректность композиции объектов;
- создание объектов превращается в читаемый код;
- времена жизни и теги описываются декларативно;
- приложение остается таким быстрым, на солько это возможно и предсказуемым.

Если вам близка идея «Чистого DI», а не как «runtime DI контейнера», Pure.DI стоит попробовать хотя бы на одном сервисе или модуле — обычно после этого трудно вернуться к «чёрному DI ящику».