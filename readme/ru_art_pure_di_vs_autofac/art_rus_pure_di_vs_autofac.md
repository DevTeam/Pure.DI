# Pure.DI и Autofac: генерация кода или классический DI-контейнер?

Если искать в интернете «мощный DI-контейнер для .NET», почти в каждом списке встретится Autofac. У него заслуженная репутация зрелого и функционального контейнера: области времени жизни, модули, декораторы, сервисы по ключу, фабрики, `Owned<T>`, перехват и расширяемый конвейер разрешения зависимостей.

На этом фоне Pure.DI иногда воспринимают как узкоспециализированный генератор, который выигрывает в скорости, но должен уступать классическому DI-контейнеру по возможностям. Такое сравнение требует уточнения: библиотеки решают многие одинаковые прикладные задачи, но делают это на разных этапах жизненного цикла приложения.

Pure.DI решает практически те же повседневные задачи, что и Autofac, но переносит основную работу контейнера на этап компиляции. По настройкам генератор строит граф объектов, то есть модель их зависимостей, и использует его для генерации кода. Сам граф представляет собой модель, а не создаваемые экземпляры. Когда приложение обращается к корню, сгенерированный C#-код создаёт соответствующую композицию объектов.

Autofac хранит регистрации в контейнере и создаёт объекты через универсальный метод `Resolve` во время выполнения. Если вызывать `Resolve` непосредственно из прикладного кода, легко прийти к антипаттерну «Service Locator».

Pure.DI работает иначе. Он генерирует обычный статически типизированный C#-код и ещё во время компиляции проверяет граф зависимостей. Поэтому структурные ошибки настройки анализируемых корней обнаруживаются до запуска приложения. Публичный API композиции состоит из явно объявленных и строго типизированных корней: свойств или методов этой композиции.

Ниже сравниваются Pure.DI 2.5.2 и Autofac 9.3.1. Примеры Autofac взяты из его официальной документации. Примеры Pure.DI взяты из его документации и тестовых сценариев.

## Короткий ответ

Если состав приложения известен во время компиляции, Pure.DI покрывает основной набор возможностей Autofac, обнаруживает структурные ошибки в статически анализируемых графах объявленных корней и устраняет накладные расходы на разрешение зависимостей через классический DI-контейнер.

Главное преимущество Autofac проявляется там, где граф зависимостей определяется во время выполнения. Например, когда плагины загружаются в виде сборок во время работы приложения, регистрации задаются внешней конфигурацией или реализации заранее неизвестны.

| Если важнее | Предпочтительный вариант |
|---|---|
| Статически известный граф, проверка при сборке, Native AOT и минимальная стоимость создания объектов | Pure.DI |
| Плагины, динамические регистрации и конвейеры разрешения во время выполнения | Autofac |
| Готовая мультитенантная инфраструктура и расширения экосистемы классического DI-контейнера | Autofac |
| Типизированные корни и доступный для чтения код создания объектов | Pure.DI |

## Одна задача, две архитектуры

Обычная регистрация Autofac выглядит так:

```csharp
var builder = new ContainerBuilder();

builder.RegisterType<ConsoleLogger>()
    .As<ILogger>()
    .SingleInstance();

builder.RegisterType<OrderService>()
    .As<IOrderService>();

using var container = builder.Build();
var service = container.Resolve<IOrderService>();
```

Во время выполнения `Build()` Autofac создаёт контейнер с описанием регистраций. При вызове `Resolve<IOrderService>()` контейнер выбирает конструктор, находит зависимости, применяет правила времени жизни и создаёт композицию объектов.

Источник: [концепции регистрации Autofac](https://docs.autofac.org/en/latest/register/registration.html).

Эквивалентная настройка Pure.DI:

```csharp
DI.Setup(nameof(Composition))
    .Bind<ILogger>().As(Lifetime.Singleton).To<ConsoleLogger>()
    .Bind<IOrderService>().To<OrderService>()
    .Root<IOrderService>("OrderService");

var composition = new Composition();
var service = composition.OrderService;
```

Источник: [внедрение абстракций в Pure.DI](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/injections-of-abstractions.md).

В обоих фрагментах используются одни и те же доменные типы: `ILogger`, `ConsoleLogger`, `IOrderService` и `OrderService`. Доменные типы не показаны, так как сравниваются только настройки DI.

Внешне настройки DI похожи, но разница в подходах существенная. Для корня композиции `OrderService` Pure.DI генерирует C#-код создания композиции объектов, концептуально близкий к следующему:

```csharp
public IOrderService OrderService
{
    get
    {
        _singletonLogger ??= new ConsoleLogger();
        return new OrderService(_singletonLogger);
    }
}
```

У Pure.DI нет поиска регистрации/привязок во время выполнения. Типы и конструкторы уже выбраны генератором, а ошибка в графе зависимостей становится ошибкой компиляции, а не ошибкой времени выполнения.

## Матрица возможностей

| Возможность | Autofac | Pure.DI |
|---|---|---|
| Внедрение через конструктор | Да | Да, с проверкой во время компиляции |
| Внедрение через свойства, поля и методы | Да для [свойств и методов](https://docs.autofac.org/en/latest/register/prop-method-injection.html) | Да для [свойств](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/property-injection.md), [полей](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/field-injection.md) и [методов](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/method-injection.md) |
| `Transient` и `Singleton` | Да | Да |
| Области времени жизни и `Scoped` | Да | Да |
| Именованные и сопоставляемые области | Да | Отдельные и зависимые композиции, `SetupScope`, теги |
| Делегаты и фабрики | `Func<T>`, `Func<X, Y, T>`, пользовательские фабрики делегатов, лямбда-регистрации | `Func<T>`, `Func<X, Y, T>`, фабрики с аргументами, строго типизированные методы-корни |
| Отложенное создание | `Lazy<T>` | `Lazy<T>` |
| Коллекции реализаций | [`IEnumerable<T>` и другие типы отношений](https://docs.autofac.org/en/latest/resolve/relationships.html#enumeration-ienumerable-b-ilist-b-icollection-b) | [Массивы](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/array.md), списки, [перечисления](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/enumerable.md), [словари](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/dictionary.md) и диапазоны |
| Именованные сервисы и сервисы по ключу | Имена, ключи, `IIndex<TKey,T>` | Теги, умные теги, `IReadOnlyDictionary<TKey, TValue>`, `IKeyedServiceProvider` |
| Обобщённые привязки | Регистрации открытых обобщённых типов | Точные обобщённые привязки с маркерными типами |
| Обобщённые корни с ограничениями типов | Нет отдельного API: контейнер разрешает уже закрытый обобщённый тип | [Да](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/generic-composition-roots-with-constraints.md), корень генерируется как обобщённый метод C# |
| Декораторы | Встроенная регистрация декораторов | Обычные привязки и теги, обрабатываемые во время компиляции |
| Перехват | Интеграция с Castle DynamicProxy | Генерируемые методы-перехватчики; при необходимости Castle DynamicProxy |
| `Owned<T>` | Отдельная область времени жизни | Отдельная модель владения для создаваемой композиции |
| Синхронное и асинхронное освобождение | [Да](https://docs.autofac.org/en/latest/lifetime/disposal.html) | [Да](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/tracking-async-disposable-instances-per-a-composition-root.md) |
| Достройка существующего объекта | `InjectProperties()` | Свойства, поля и методы через `BuildUp`/`TryBuildUp` |
| Модули | Модули времени выполнения | Зависимые, внутренние и глобальные композиции |
| [Сканирование сборок](https://docs.autofac.org/en/latest/register/scanning.html) | Да, во время выполнения | Нет: типы должны быть известны во время компиляции |
| [Источники регистраций](https://docs.autofac.org/en/latest/advanced/registration-sources.html) | Да, регистрации можно предоставлять динамически | Нет прямого аналога: граф фиксируется при компиляции |
| [Конвейеры разрешения](https://docs.autofac.org/en/latest/advanced/pipelines.html) | Да, промежуточные обработчики для сервисов и регистраций | Нет конвейера времени выполнения; доступны сгенерированные методы-перехватчики |
| [Конфигурация регистраций](https://docs.autofac.org/en/latest/configuration/index.html) | Модули и внешняя конфигурация во время выполнения | Настройки являются частью исходного кода и требуют перекомпиляции |
| [Метаданные `Meta<T>`](https://docs.autofac.org/en/latest/advanced/metadata.html) | Да, включая строго типизированные метаданные | Нет прямого аналога `Meta<T>`; доступны теги и атрибуты времени компиляции |
| [Multitenant containers](https://docs.autofac.org/en/latest/advanced/multitenant.html) | Готовая инфраструктура времени выполнения | Нет прямого аналога; можно создавать композиции для отдельных tenants |
| [Pooled instances](https://docs.autofac.org/en/latest/advanced/pooled-instances.html) | Готовый пакет для pooling | Нет встроенного lifetime; [пример пула объектов](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/object-pool.md) реализован прикладным кодом |
| Проверка графов объявленных корней во время компиляции | Нет | [Да](https://github.com/DevTeam/Pure.DI/blob/2.5.2/DIAGNOSTICS.md) |
| Явные типизированные корни | Не являются основной моделью | [Да](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/composition-roots.md) |
| Типизированные аргументы корней | Нет отдельного API корней; доступны параметры `Resolve` и фабрики делегатов | [Да](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/root-arguments.md), корни генерируются как обычные методы |
| Native AOT | [Поддерживается с ограничениями](https://docs.autofac.org/en/latest/advanced/native-aot-trimming.html) | Сгенерированный код создания объектов не требует .NET Reflection; [пример Native AOT](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/ConsoleNativeAOT.md) |
| `Span<T>` и `ref struct` в графе | Нет как универсальные типы отношений | [Да](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/span-and-readonlyspan.md) |
| `ref`-зависимости и stack-only аргументы корней | Универсальный объектный API контейнера не может передавать управляемые ссылки и неупаковываемые `ref struct`; требуется внешний типизированный код | [Да](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/ref-dependencies.md), включая сгенерированные методы-корни со [`scoped ReadOnlySpan<T>`](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/method-injection-for-a-hot-path.md) |
| Объединённые типы (union types) как DI-контракты | Нет | [Да](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/union-types.md) |
| Генерация интерфейсов | Нет | [Да](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/generate-an-interface-from-a-class.md) |
| Nullable annotations как часть DI-контракта | `T` и `T?` представлены одним `System.Type` и неразличимы для `Resolve(Type)` | [Да](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/nullable-reference-types.md), проверяются во всём графе |
| Просмотр и отладка кода создания объектов | Внутренний конвейер времени выполнения | [Обычный сгенерированный C#](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/composition-roots.md) |

В верхней части таблицы перечислены общие возможности, затем возможности Autofac без прямого аналога в Pure.DI и возможности Pure.DI без прямого аналога в Autofac. Некоторые строки описывают не прямые аналоги, а разные способы решить одну прикладную задачу. Например, область времени жизни Autofac и зависимая композиция Pure.DI имеют разную семантику, хотя обе помогают задать границы создания и владения объектами.

## Регистрации, времена жизни и выбор реализации

### Времена жизни

Autofac поддерживает следующие варианты времени жизни объектов: `Instance Per Dependency`, `Single Instance`, `Instance Per Lifetime Scope`, `Instance Per Matching Lifetime Scope`, `Instance Per Request`, `Instance Per Owned` и `Thread Scope`.

Источник: [области времени жизни экземпляров Autofac](https://docs.autofac.org/en/latest/lifetime/instance-scope.html).

Pure.DI поддерживает:

- `Transient`: новый экземпляр для каждого внедрения;
- `Singleton`: один экземпляр на композицию;
- `Scoped`: один экземпляр на область;
- `PerResolve`: один экземпляр на построение корня;
- `PerBlock`: повторное использование внутри блока сгенерированного кода.

Источники: [Transient](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/transient.md), [Singleton](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/singleton.md), [Scoped](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/scoped.md), [PerResolve](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/perresolve.md), [PerBlock](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/perblock.md).

Преимущество Pure.DI здесь в том, что генератор заранее знает, где хранится каждый объект, и создаёт для него поле или локальную переменную. Нарушения времени жизни (captive dependencies), отсутствующая привязка или цикл в зависимостях обнаруживаются до запуска программы. Если в анализируемом графе есть структурная ошибка, приложение не соберётся.

### Сервисы по ключу и теги

В Autofac сервис можно зарегистрировать по имени или произвольному ключу:

```csharp
builder.RegisterType<OnlineState>()
    .Keyed<IDeviceState>(DeviceState.Online);

builder.RegisterType<OfflineState>()
    .Keyed<IDeviceState>(DeviceState.Offline);
```

Для выбора во время выполнения Autofac предлагает `IIndex<TKey, TValue>`.

Источник: [именованные сервисы и сервисы по ключу в Autofac](https://docs.autofac.org/en/latest/advanced/keyed-services.html).

Pure.DI использует теги:

```csharp
DI.Setup(nameof(Composition))
    .Bind<IDeviceState>(DeviceState.Online).To<OnlineState>()
    .Bind<IDeviceState>(DeviceState.Offline).To<OfflineState>()
    .Root<IDeviceState>("Online", DeviceState.Online);
```

Источник: [теги в Pure.DI](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/tags.md).

Здесь используются те же `IDeviceState`, `OnlineState`, `OfflineState` и `DeviceState`, что и во фрагменте Autofac. Тегом может быть строка, перечисление, тип, `null`, `default` или [умный тег](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/smart-tags.md).

В показанном примере Pure.DI реализация для корня выбирается по тегу во время генерации кода. Если потребителю нужно выбирать одну из зарегистрированных реализаций по ключу во время выполнения, можно внедрить обычный `IReadOnlyDictionary<TKey, TValue>`:

```csharp
DI.Setup(nameof(Composition))
    .Bind(Tag.Unique).To((OnlineState state) =>
        new KeyValuePair<DeviceState, IDeviceState>(DeviceState.Online, state))
    .Bind(Tag.Unique).To((OfflineState state) =>
        new KeyValuePair<DeviceState, IDeviceState>(DeviceState.Offline, state))
    .Root<DeviceStateSelector>("DeviceStateSelector");

sealed class DeviceStateSelector(
    IReadOnlyDictionary<DeviceState, IDeviceState> states)
{
    public IDeviceState this[DeviceState state] => states[state];
}
```

Pure.DI заранее генерирует код формирования словаря и проверяет граф всех его значений, а конкретный элемент выбирается по ключу уже во время выполнения. Такой контракт не привязывает прикладной класс к API DI-инструмента и по назначению близок к `IIndex<TKey, TValue>` в Autofac.

Источник: [внедрение словаря в Pure.DI](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/dictionary.md).

Когда требуется стандартный API .NET, композиция Pure.DI также может реализовать [`IKeyedServiceProvider`](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/keyed-service-provider.md).

### Обобщённые привязки

Autofac регистрирует открытый обобщённый тип:

```csharp
builder.RegisterGeneric(typeof(Repository<>))
    .As(typeof(IRepository<>));
```

Источник: [открытые обобщённые компоненты Autofac](https://docs.autofac.org/en/latest/register/registration.html#open-generic-components).

Pure.DI использует маркерный тип:

```csharp
DI.Setup(nameof(Composition))
    .Bind<IRepository<TT>>().To<Repository<TT>>();
```

Источник: [обобщённые типы в Pure.DI](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/generics.md).

Регистрация привязки похожа, но Pure.DI создаёт конкретный код отдельно для `Repository<User>` и `Repository<Order>`. Это позволяет учитывать ограничения обобщённых типов, вложенные обобщённые типы и теги, то есть точно определять тип объекта на этапе компиляции. Успешная сборка подтверждает структурную корректность сгенерированного графа, но не исключает прикладные исключения в конструкторах, фабриках, перехватчиках или коде освобождения ресурсов.

Для Native AOT разница существенна. Официальная документация Autofac прямо указывает, что открытый обобщённый тип, закрываемый значимым типом, требует динамического кода во время выполнения и может привести к `DependencyResolutionException`. В Pure.DI конкретные закрытые типы известны во время компиляции, поэтому такой проблемы не возникает.

Источник: [Native AOT и trimming в Autofac](https://docs.autofac.org/en/latest/advanced/native-aot-trimming.html).

### Обобщённые корни

Классические DI-контейнеры поддерживают регистрации открытых обобщённых типов, но разрешают уже закрытый тип, например `IRepository<Order>`. Pure.DI может сделать обобщённым сам корень композиции и перенести ограничения его параметров типов в публичный API:

```csharp
DI.Setup(nameof(Composition))
    .Hint(Hint.Resolve, "Off")
    .Bind().To<StreamSource<TTDisposable>>()
    .Bind().To<DataProcessor<TTDisposable, TTS>>()
    .Root<IDataProcessor<TTDisposable, TTS>>("GetProcessor");

var composition = new Composition();
var processor = composition.GetProcessor<Stream, double>();

interface IStreamSource<T>
    where T : IDisposable;

sealed class StreamSource<T> : IStreamSource<T>
    where T : IDisposable;

interface IDataProcessor<T, TOptions>
    where T : IDisposable
    where TOptions : struct;

sealed class DataProcessor<T, TOptions>(IStreamSource<T> source)
    : IDataProcessor<T, TOptions>
    where T : IDisposable
    where TOptions : struct;
```

Для этого корня Pure.DI генерирует обычный обобщённый метод:

```csharp
public IDataProcessor<T, TOptions> GetProcessor<T, TOptions>()
    where T : IDisposable
    where TOptions : struct;
```

Типы `T` и `TOptions` выбираются вызывающим кодом и проверяются компилятором C#. Универсальный метод `Resolve` классического DI-контейнера не может выразить такой контракт: он принимает уже сформированный закрытый тип. Аналог потребовал бы отдельной вручную написанной обобщённой фабрики вне API контейнера.

Параметр типа может определять не только возвращаемый тип, но и тип аргумента корня. Например, сочетание `RootArg<TT>("model")` и `Root<IPresenter<TT>>("GetPresenter")` создаёт метод, в котором модель передаётся во время выполнения, а соответствие типов проверяется компилятором:

```csharp
public IPresenter<T> GetPresenter<T>(T model);
```

Корень также может быть асинхронным, принимать обычные runtime-аргументы и сохранять ограничения типов:

```csharp
public Task<IQuery<TConnection, TResult>>
    GetDataQueryAsync<TConnection, TResult>(
        CancellationToken cancellationToken)
    where TConnection : IDisposable
    where TResult : struct;
```

Это не новые варианты `Resolve`: Pure.DI генерирует типизированные методы, сигнатуры которых становятся частью API композиции.

Источники: [обобщённые корни Pure.DI с ограничениями типов](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/generic-composition-roots-with-constraints.md), [обобщённые аргументы корней](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/generic-root-arguments.md) и [асинхронные обобщённые корни с ограничениями](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/generic-async-composition-roots-with-constraints.md).

## Фабрики и типы отношений

Autofac называет `Lazy<T>`, `Owned<T>`, `Func<T>`, `IEnumerable<T>`, `Meta<T>` и `IIndex<TKey,TValue>` типами отношений (relationship types). Их можно комбинировать, например `IEnumerable<Func<Owned<T>>>`.

Источник: [неявные типы отношений Autofac](https://docs.autofac.org/en/latest/resolve/relationships.html).

Pure.DI поддерживает те же основные сценарии:

- [Lazy](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/lazy.md);
- [Func](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/func.md);
- [Func с аргументами](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/func-with-arguments.md);
- [перечисления](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/enumerable.md);
- [словари](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/dictionary.md);
- [аргументы композиции](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/composition-arguments.md);
- [аргументы корня](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/root-arguments.md).

Например, обе библиотеки автоматически внедряют `Func<IEnemy>` в один и тот же потребитель:

```csharp
sealed class GameLevel(Func<IEnemy> enemyFactory) : IGameLevel
{
    public IEnemy CreateEnemy() => enemyFactory();
}
```

Регистрация Autofac:

```csharp
builder.RegisterType<Enemy>().As<IEnemy>();
builder.RegisterType<GameLevel>().As<IGameLevel>();
```

Регистрация и корень Pure.DI:

```csharp
DI.Setup(nameof(Composition))
    .Bind().To<Enemy>()
    .Bind().To<GameLevel>()
    .Root<IGameLevel>("GameLevel");
```

Источник: [внедрение по требованию в Pure.DI](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/injection-on-demand.md).

В Autofac делегат является типом отношений. В Pure.DI он генерируется как строго типизированный вызов создания объекта. Прикладной `GameLevel` от выбора DI-инструмента не зависит.

Pure.DI также поддерживает `Func<...>` с аргументами, значения которых определяются динамически во время выполнения:

```csharp
DI.Setup(nameof(Composition))
    .Bind().As(Lifetime.Singleton).To<Clock>()
    .Bind().To<Person>()
    .Bind().To<Team>()
    .Root<Team>("Team");
```

Потребитель получает фабрику с двумя динамическими аргументами:

```csharp
sealed class Team(Func<int, string, IPerson> personFactory)
{
    public IPerson CreatePerson(int id, string name) =>
        personFactory(id, name);
}
```

Значения `id` и `name` передаются при вызове фабрики. Зависимость `IClock`, которая также нужна `Person`, Pure.DI получает из композиции. Таким образом, статически известные зависимости проверяются во время компиляции, а динамические данные передаются во время выполнения.

Источник: [`Func` с аргументами в Pure.DI](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/func-with-arguments.md).

Autofac также поддерживает `Func<X, Y, T>` с аргументами времени выполнения:

```csharp
builder.RegisterType<Clock>().As<IClock>().SingleInstance();
builder.RegisterType<Person>().As<IPerson>();

using var container = builder.Build();
var personFactory = container.Resolve<Func<int, string, IPerson>>();
var person = personFactory(10, "Nik");
```

Для стандартного `Func<X, Y, T>` Autofac сопоставляет аргументы с параметрами конструктора по типу, а остальные зависимости получает из текущего lifetime scope. Такой `Func` не поддерживает несколько аргументов одного типа. В этом случае Autofac предлагает пользовательские фабрики делегатов (`custom delegate factories`), в которых параметры сопоставляются по имени.

Источники: [`Func<X, Y, T>` в Autofac](https://docs.autofac.org/en/latest/resolve/relationships.html#parameterized-instantiation-func-x-y-b) и [delegate factories](https://docs.autofac.org/en/latest/advanced/delegate-factories.html).

## `Owned<T>`: одинаковое имя, разная реализация

Обе библиотеки позволяют потребителю явно владеть частью создаваемой композиции объектов и освободить её независимо от контейнера или основной композиции. Для этого потребитель может использовать, например, такую зависимость, как `Func<Owned<IMessageHandler>>`:

```csharp
sealed class MessagePump
{
    private readonly Func<Owned<IMessageHandler>> handlerFactory;

    public MessagePump(Func<Owned<IMessageHandler>> handlerFactory) =>
        this.handlerFactory = handlerFactory;

    public void Go()
    {
        while (true)
        {
            var message = NextMessage();

            using var handler = handlerFactory();
            handler.Value.Handle(message);
        }
    }
}
```

Это форма официального примера из раздела [об управляемых экземплярах Autofac](https://docs.autofac.org/en/latest/advanced/owned-instances.html).

### Как это настраивает Autofac

```csharp
builder.RegisterType<MessageHandler>().As<IMessageHandler>();
// MessagePump получает Func<Owned<IMessageHandler>>.
builder.RegisterType<MessagePump>();

using var container = builder.Build();
var pump = container.Resolve<MessagePump>();
```

Для каждого `Owned<T>` Autofac создаёт отдельный `ILifetimeScope`. Освобождение `Owned<T>` завершает область и освобождает созданные в ней зависимости, которые не являются общими.

В Autofac 9.3.1 `Owned<T>` является классом. Он заменяет ссылку на область времени жизни через `Interlocked.Exchange`, поддерживает `Dispose()` и `DisposeAsync()`, а затем делегирует освобождение области времени жизни.

Источник: [`Owned.cs` из Autofac 9.3.1](https://github.com/autofac/Autofac/blob/v9.3.1/src/Autofac/Features/OwnedInstances/Owned.cs).

### Как это настраивает Pure.DI

```csharp
DI.Setup(nameof(Composition))
    .Bind<IMessageHandler>().To<MessageHandler>()
    // MessagePump получает Func<Owned<IMessageHandler>>.
    .Bind().To<MessagePump>()
    .Root<MessagePump>("MessagePump");

var composition = new Composition();
var pump = composition.MessagePump;
```

В обеих настройках `Owned<T>` не нужно регистрировать отдельно. Autofac предоставляет его как тип отношений, а Pure.DI распознаёт `Func<Owned<IMessageHandler>>` в конструкторе `MessagePump` и генерирует фабрику вместе с кодом освобождения создаваемой композиции объектов.

Источники: [отслеживание освобождаемых экземпляров в делегатах Pure.DI](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/tracking-disposable-instances-in-delegates.md) и [OwnedTests.cs](https://github.com/DevTeam/Pure.DI/blob/2.5.2/tests/Pure.DI.IntegrationTests/OwnedTests.cs).

Но внутри нет специализированной области времени жизни как в Autofac:

- `Owned<T>` представляет собой `readonly struct`, содержащий значение и ссылку на механизм освобождения;
- генератор заранее знает все освобождаемые объекты конкретного графа зависимостей;
- для графа без ресурсов используется общий пустой владелец без отдельного аккумулятора ресурсов;
- для известного числа ресурсов аккумулятор создаётся с подходящей ёмкостью, что оптимизирует работу списка для хранения ресурсов;
- при [`ThreadSafe = Off`](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/threadsafe-off-for-single-thread-composition.md) генератор не добавляет ненужную синхронизацию;
- синхронное и асинхронное освобождение генерируются с учётом целевой платформы .NET.

Реализация учитывает синхронное и асинхронное освобождение, обратный порядок ресурсов, повторные и конкурентные вызовы, вложенных владельцев, общие времена жизни и откат при ошибке создания графа. Эти сценарии покрыты сравнительными интеграционными тестами с Autofac.

Autofac обеспечивает схожий результат в аналогичных сценариях. Преимущество Pure.DI здесь в том, что управление временем жизни реализуется в сгенерированном коде, создающем композицию объектов. Этот подход не требует специальной области времени жизни как в Autofac и допускает оптимизации во время компиляции, невозможные для классического DI-контейнера.

## Внедрение через свойства и методы, достройка объектов

Autofac поддерживает обязательные свойства, `PropertiesAutowired()`, ручную установку свойств и внедрение в существующий объект через `InjectProperties()`. Внедрение через метод обычно конфигурируется с помощью лямбда-регистрации.

Источник: [внедрение через свойства и методы в Autofac](https://docs.autofac.org/en/latest/register/prop-method-injection.html).

Pure.DI поддерживает внедрение через свойства, поля и методы непосредственно в сгенерированном коде:

```csharp
sealed class Navigator : INavigator
{
    public IMap? CurrentMap { get; private set; }

    [Dependency]
    public void LoadMap(IMap map) => CurrentMap = map;
}
```

Источник: [внедрение через методы в Pure.DI](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/method-injection.md).

Для существующего объекта доступна достройка (Build-Up):

```csharp
.Bind().To(ctx =>
{
    var person = new Person();
    ctx.BuildUp(person);
    return person;
})
```

`TryBuildUp` позволяет безопасно проверить возможность достройки объекта.

Источники: [достройка существующего объекта](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/build-up-of-an-existing-object.md) и [построители с TryBuildUp](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/builders.md).

Поскольку члены известны генератору на этапе компиляции, донастройка объектов выполняется напрямую обычным кодом C# и не требует использования .NET Reflection во время выполнения.

## Декораторы и перехват

Autofac имеет специальный API:

```csharp
builder.RegisterType<TextWidget>()
    .As<IWidget>();

builder.RegisterDecorator<BoxWidget, IWidget>();
```

Источник: [адаптеры и декораторы Autofac](https://docs.autofac.org/en/latest/advanced/adapters-decorators.html).

В Pure.DI конфигурация той же цепочки выглядит так:

```csharp
DI.Setup(nameof(Composition))
    .Bind("base").To<TextWidget>()
    .Bind().To<BoxWidget>()
    .Root<IWidget>("Widget");
```

В обоих случаях используются `TextWidget` и `BoxWidget`. Единственное отличие в настройке DI состоит в том, что Pure.DI явно отмечает тег декорируемого сервиса:

```csharp
sealed class BoxWidget([Tag("base")] IWidget inner) : IWidget
{
    public string Render() => $"[ {inner.Render()} ]";
}
```

Здесь тег `"base"` выбран произвольно, по смыслу, и не является неким «магическим» значением.

Источник: [декоратор в Pure.DI](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/decorator.md).

Такой подход не требует специальной логики декораторов во время выполнения: декораторы становятся частью обычной композиции объектов, а соответствующий граф зависимостей проверяется во время компиляции. Например, ничто не мешает декорировать сразу несколько объектов разных типов в одном классе-декораторе.

Для перехвата Autofac интегрируется с Castle DynamicProxy.

Источник: [перехват типов в Autofac](https://docs.autofac.org/en/latest/advanced/interceptors.html).

Pure.DI предлагает сгенерированные частичные методы `OnNewInstance` и `OnDependencyInjection`. Через них можно изменить или обернуть экземпляр в выбранной точке создаваемой композиции объектов без потери производительности. Когда действительно нужен динамический прокси-объект, Castle DynamicProxy можно встроить в сгенерированный код создания композиции объектов.

Источники: [перехват в Pure.DI](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/interception.md) и [расширенный перехват](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/advanced-interception.md).

Здесь Autofac удобнее, если перехват должен полностью конфигурироваться во время выполнения. Pure.DI сильнее, когда важны производительность, прозрачность, фильтрация типов на этапе генерации.

## Модули и большие композиции

Модули Autofac инкапсулируют набор регистраций внутри модулей:

```csharp
public sealed class RepositoryModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<SqlRepository>()
            .As<IRepository>();
    }
}
```

Источник: [модули Autofac](https://docs.autofac.org/en/latest/configuration/modules.html).

Pure.DI разделяет настройки больших композиций на несколько частей:

```csharp
DI.Setup("Infrastructure", CompositionKind.Internal)
    .Bind<IDatabase>().To<SqlDatabase>();

DI.Setup(nameof(Composition))
    .DependsOn("Infrastructure")
    .Bind<IUserService>().To<UserService>()
    .Root<IUserService>("UserService");
```

Источники: [зависимые композиции](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/dependent-compositions.md), [глобальные композиции](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/global-compositions.md), [наследование композиций](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/inheritance-of-compositions.md), [контекст настройки](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/dependent-compositions-with-setup-context.md) и [настройка области](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/scope-setup-method.md).

Доступны открытые `public`, внутренние `internal` и глобальные `global` композиции, наследование композиций, контекст настройки и отдельные настройки областей. Они позволяют повторно использовать конфигурацию. Всё это обрабатывается во время компиляции.

Динамические модули Autofac лучше подходят для сборки приложения из неизвестного заранее набора плагинов. Композиции Pure.DI лучше подходят для модулей, определённых во время компиляции: графы объявленных корней проверяются во время сборки.

## Где Pure.DI особенно хорош

### Ошибка в графе становится ошибкой сборки

`Build()` завершает регистрацию компонентов и подготавливает контейнер Autofac, но не выполняет полную проверку графа. Из-за динамических регистраций, параметров, модулей и фабрик многие ошибки обнаруживаются только при конкретном вызове `Resolve()`. Поэтому классический DI-контейнер не всегда может заранее подтвердить корректность всех композиций объектов, которые будут сформированы во время выполнения.

Источник: [почему анализ регистраций не встроен в Autofac](https://docs.autofac.org/en/latest/faq/container-analysis.html).

Pure.DI анализирует каждый объявленный корень и сообщает:

- отсутствующие привязки;
- неоднозначный выбор реализации;
- циклические зависимости;
- нарушение ограничений обобщённых типов;
- недоступный конструктор или другой член типа;
- несовместимый тег;
- проблемы с nullable annotations;
- некорректное использование стековых типов;
- и многое другое.

Разработчик получает структурно проверенный код создания композиции объектов до запуска приложения. Прикладной код конструкторов, фабрик и перехватчиков при этом сохраняет обычное поведение и может завершиться исключением во время выполнения.

### Корни композиции

В Autofac типичный сценарий создания композиции объектов основан на вызове метода `Resolve<T>()`. В Pure.DI:

```csharp
var report = composition.CreateReport(userId);
var worker = composition.Worker;
```

Корнем композиции может быть свойство или метод с типизированными аргументами и результатом. Анонимные корни при необходимости остаются доступны через методы `Resolve`, как и в Autofac.

Источники: [корни композиции](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/composition-roots.md) и [типизированные аргументы корней](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/root-arguments.md).

### Native AOT без Reflection в сгенерированном коде

Autofac 9 поддерживает trimming и Native AOT для базовых сценариев, но заявляет следующие ограничения:

- сканирование сборок требует сохранения типов, найденных с помощью .NET Reflection;
- открытый обобщённый тип, закрытый значимым типом, может привести к ошибке во время выполнения;
- строго типизированные представления метаданных требуют компиляции выражений во время выполнения;
- сгенерированные фабрики, фабрики делегатов и пользовательские селекторы свойств могут выдавать `RequiresDynamicCode` или `RequiresUnreferencedCode`;
- отсутствие предупреждения для некоторых типов отношений не гарантирует полной совместимости с AOT.

Источник: [Native AOT и trimming в Autofac](https://docs.autofac.org/en/latest/advanced/native-aot-trimming.html).

Pure.DI генерирует обычные вызовы `new`. Для trimmer это обычный статически связанный код на C#: генератору не требуется сохранять конструкторы, методы, свойства и поля «на всякий случай», потому что всё необходимое уже используется напрямую. Совместимость приложения с Native AOT и trimming по-прежнему зависит от пользовательских фабрик, перехватчиков и сторонних библиотек.

Пример: [консольное приложение Pure.DI с Native AOT](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/ConsoleNativeAOT.md).

### Современный C# становится частью композиции объектов

Pure.DI поддерживает сценарии, которые нельзя представить через универсальный объектный API большинства классических DI-контейнеров:

- `Span<T>` и `ReadOnlySpan<T>` как зависимости;
- `ref struct` и фабрики с `allows ref struct`;
- `ref`-зависимости, сохраняющие семантику управляемой ссылки;
- stack-only аргументы корней и сгенерированные параметры `scoped ReadOnlySpan<T>`;
- корни `ValueTask<T>`;
- nullable annotations как часть DI-контракта;
- union types как DI-контракты.

В частности, `T` и `T?` имеют одинаковый runtime-тип. Поэтому универсальный `Resolve(Type)` не может выбрать между ними как между двумя контрактами. Pure.DI анализирует nullable annotations в исходном коде и сохраняет это различие при проверке и генерации графа.

Объектный API также не может принять `ReadOnlySpan<T>` через `object` или `params object[]`: stack-only значение нельзя упаковать. Pure.DI вместо этого может сгенерировать типизированный корень:

```csharp
public IRouteMatcher CreateMatcher(
    scoped ReadOnlySpan<char> path);
```

Управляемые ссылки сохраняются и при внедрении зависимостей:

```csharp
[Ordinal]
public void Initialize(ref Data data);
```

Такой API можно написать вручную как внешнюю фабрику, но нельзя выразить универсальным объектным API классического DI-контейнера.

Источники: [Span и ReadOnlySpan](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/span-and-readonlyspan.md), [`ref`-зависимости](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/ref-dependencies.md), [фабрика с `allows ref struct`](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/allows-ref-struct-factory.md), [внедрение через метод на горячем участке](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/method-injection-for-a-hot-path.md), [фабрика с `ReadOnlySpan<T>`](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/default-func-with-readonlyspan.md), [корень `ValueTask<T>`](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/valuetask-root.md), [nullable reference types](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/nullable-reference-types.md), [объединённые типы](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/union-types.md) и [union-результат без упаковки](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/non-boxing-union-result.md).

Кроме того, Pure.DI умеет [генерировать интерфейсы из класса](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/generate-an-interface-from-a-class.md), сохраняя обобщённые типы, nullable annotations, события и XML-документацию.

## Производительность: накладные расходы классического DI-контейнера

Набор тестов BenchmarkDotNet в репозитории сравнивает создание одной и той же композиции объектов вручную, через Pure.DI и через Autofac — классический DI-контейнер.

Ниже приведены несколько строк из сохранённых результатов. Они получены с BenchmarkDotNet 0.15.8 и .NET SDK 10.0.102 под Windows 10 на AMD Ryzen 9 5900X. В таблице показано среднее время одной операции и объём выделенной управляемой памяти:

| Сценарий | Ручной код | Корень Pure.DI | Autofac | Память Pure.DI | Память Autofac |
|---|---:|---:|---:|---:|---:|
| Transient | 2,820 нс | 3,088 нс | 13 207,209 нс | 24 Б | 30 424 Б |
| Singleton | 2,745 нс | 2,805 нс | 8 143,869 нс | 24 Б | 22 048 Б |
| `Func<T>` | 3,089 нс | 2,774 нс | 4 800,897 нс | 24 Б | 13 128 Б |
| Array | 49,80 нс | 51,32 нс | 13 300,06 нс | 408 Б | 26 496 Б |

Источник: [тесты производительности Pure.DI](https://github.com/DevTeam/Pure.DI/tree/91f5c2ee57dbccf2efa21959d7d8743ac1e9937e/benchmarks/data/results). Снимок результатов сохранён в commit `91f5c2ee57dbccf2efa21959d7d8743ac1e9937e` от 14 февраля 2026 года.

В этом запуске создание конкретного transient-графа через сгенерированный корень Pure.DI заняло примерно в 4276 раз меньше времени и выделило примерно в 1268 раз меньше памяти, чем разрешение того же корня через Autofac. Эти результаты относятся к конкретному графу объектов и окружению теста. Соотношение может измениться при другой структуре графа, версии .NET, оборудовании или способе получения корня.

Разница обусловлена архитектурой инструментов. Pure.DI заранее генерирует специализированный код создания графа, близкий к ручному. Autofac разрешает зависимости через универсальный механизм классического DI-контейнера и применяет правила времени жизни при выполнении программы. Поэтому преимущество Pure.DI заключается не в конкретном результате одного теста, а в минимальных накладных расходах при создании статически известного графа.

## Где Autofac объективно сильнее

### Сканирование сборок

Autofac может загрузить сборку, найти подходящие реализации и зарегистрировать их на этапе выполнения.

Источник: [сканирование сборок в Autofac](https://docs.autofac.org/en/latest/register/scanning.html).

Это полезно для плагинов и расширений. Но приходится полагаться на .NET Reflection, менее явную конфигурацию и дополнительные требования к trimming.

### Источники регистраций и конвейеры разрешения зависимостей

Autofac позволяет динамически предоставить регистрацию для неизвестного типа и встроить промежуточные обработчики в конвейер разрешения зависимостей.

Источники: [источники регистраций](https://docs.autofac.org/en/latest/advanced/registration-sources.html) и [конвейеры разрешения](https://docs.autofac.org/en/latest/advanced/pipelines.html).

Такая гибкость нужна фреймворкам и инфраструктурным библиотекам. В Pure.DI подобное поведение обычно реализуется фабрикой, методом-перехватчиком или специализированным кодом.

### Готовая мультитенантная инфраструктура

Autofac имеет отдельный multitenant package и tenant-specific containers.

Источник: [мультитенантные приложения в Autofac](https://docs.autofac.org/en/latest/advanced/multitenant.html).

Pure.DI позволяет создавать отдельные композиции для каждого tenant с помощью тегов, фабрик, аргументов композиций или корней, но не предоставляет идентичные возможности времени выполнения.

Если приложение действительно использует эти возможности, Autofac может быть лучшим выбором. Но большинству приложений не требуется иметь динамический граф зависимостей для создания композиций объектов.

## Разные модели расширяемости

Autofac описывает возможности в терминах контейнера:

- источник регистраций;
- конвейер разрешения;
- промежуточные обработчики сервиса;
- область времени жизни;
- сканирование модулей.

Pure.DI часто решает те же прикладные задачи обычным кодом на C#, но это не всегда прямые аналоги возможностей Autofac:

- вместо источника регистраций времени выполнения используются обобщённые привязки или фабрики;
- вместо промежуточных обработчиков разрешения используются сгенерированные частичные методы;
- поиск декораторов заменяет явная типизированная цепочка декораторов в композиции объектов;
- область времени жизни для unit of work заменяет сгенерированный `Owned<T>`, оптимизированный для конкретной композиции объектов;
- вместо модуля времени выполнения используется зависимая композиция;
- вместо `Resolve<T>()` доступны корни композиции в виде обычных свойств или методов. [Методы `Resolve<T>()`](https://github.com/DevTeam/Pure.DI/blob/2.5.2/readme/resolve-methods.md) также поддерживаются и работают с высокой производительностью.

## Итог

Autofac действительно является мощным классическим DI-контейнером. Его зрелость, экосистема и возможности динамической настройки во время выполнения заслуживают высокой оценки.

Но из этого не следует, что Pure.DI менее развит по своим возможностям. Для .NET-приложения с заранее известным графом зависимостей картина иная:

- для основных DI-сценариев Autofac в Pure.DI есть аналоги;
- граф зависимостей для всех используемых корней проверяется во время сборки;
- сгенерированный код создания объектов не требует Reflection и динамической генерации кода;
- корни композиции являются строго типизированными;
- сгенерированный код можно читать и отлаживать как обычный код на C#;
- сгенерированный код не требует упаковки значений ради универсального API классического DI-контейнера;
- `Owned<T>` получает специализированную и тщательно протестированную модель владения ресурсами;
- производительность близка к ручному коду, написанному в парадигме чистого DI.

Поэтому точнее сформулировать различие так:

> Pure.DI реализует основные сценарии классического DI-контейнера через генерацию кода и проверяет структуру статически известных графов во время сборки.

Autofac оправдан, если состав композиций должен меняться во время выполнения. Если он известен заранее, Pure.DI строит и проверяет граф, а затем генерирует код. Эта работа выполняется один раз во время сборки приложения.
