#### Member ordinal attribute

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Attributes/MemberOrdinalAttributeScenario.cs)

When applied to a property or field, these type members will also participate in dependency injection in the appropriate order from smallest value to largest.

```c#
interface IPerson
{
    string Name { get; }
}

class Person : IPerson
{
    private readonly StringBuilder _name = new();

    public string Name => _name.ToString();

    [Ordinal(0)]
    public int Id;

    [Ordinal(1)]
    public string FirstName
    {
        set
        {
            _name.Append(Id);
            _name.Append(' ');
            _name.Append(value);
        }
    }

    [Ordinal(2)]
    public DateTime Birthday
    {
        set
        {
            _name.Append(' ');
            _name.Append($"{value:yyyy-MM-dd}");
        }
    }
}

DI.Setup(nameof(PersonComposition))
    .Arg<int>("personId")
    .Arg<string>("personName")
    .Arg<DateTime>("personBirthday")
    .Bind<IPerson>().To<Person>()

    // Composition root
    .Root<IPerson>("Person");

var composition = new PersonComposition(
    personId: 123,
    personName: "Nik",
    personBirthday: new DateTime(1977, 11, 16));

var person = composition.Person;
person.Name.ShouldBe("123 Nik 1977-11-16");
```

The attribute `Ordinal` is part of the API, but you can use your own attribute at any time, and this allows you to define them in the assembly and namespace you want.

<details open>
<summary>Class Diagram</summary>

```mermaid
classDiagram
	class PersonComposition {
		+IPerson Person
		+ T ResolveᐸTᐳ()
		+ T ResolveᐸTᐳ(object? tag)
		+ object Resolve(Type type)
		+ object Resolve(Type type, object? tag)
	}
	class Int32
	class String
	class DateTime
	Person --|> IPerson : 
	class Person {
		+Person()
		+Int32 Id
		+String FirstName
		+DateTime Birthday
	}
	class IPerson {
		<<abstract>>
	}
	Person o-- Int32 : Argument "personId"
	Person o-- String : Argument "personName"
	Person o-- DateTime : Argument "personBirthday"
	PersonComposition ..> Person : IPerson Person
```

</details>

<details>
<summary>Pure.DI-generated partial class PersonComposition</summary><blockquote>

```c#
partial class PersonComposition
{
  private readonly PersonComposition _rootM04D27di;

  private readonly int _argM04D27di_personId;
  private readonly string _argM04D27di_personName;
  private readonly System.DateTime _argM04D27di_personBirthday;

  public PersonComposition(int personId, string personName, System.DateTime personBirthday)
  {
    _rootM04D27di = this;
    if (ReferenceEquals(personName, null))
    {
      throw new ArgumentNullException("personName");
    }

    _argM04D27di_personId = personId;
    _argM04D27di_personName = personName;
    _argM04D27di_personBirthday = personBirthday;
  }

  internal PersonComposition(PersonComposition baseComposition)
  {
    _rootM04D27di = baseComposition._rootM04D27di;
    _argM04D27di_personId = baseComposition._argM04D27di_personId;
    _argM04D27di_personName = baseComposition._argM04D27di_personName;
    _argM04D27di_personBirthday = baseComposition._argM04D27di_personBirthday;
  }

  public IPerson Person
  {
    [MethodImpl((MethodImplOptions)0x100)]
    get
    {
      Person transientM04D27di0_Person = new Person();
      transientM04D27di0_Person.Id = _argM04D27di_personId;
      transientM04D27di0_Person.FirstName = _argM04D27di_personName;
      transientM04D27di0_Person.Birthday = _argM04D27di_personBirthday;
      return transientM04D27di0_Person;
    }
  }

  [MethodImpl((MethodImplOptions)0x100)]
  public T Resolve<T>()
  {
    return ResolverM04D27di<T>.Value.Resolve(this);
  }

  [MethodImpl((MethodImplOptions)0x100)]
  public T Resolve<T>(object? tag)
  {
    return ResolverM04D27di<T>.Value.ResolveByTag(this, tag);
  }

  [MethodImpl((MethodImplOptions)0x100)]
  public object Resolve(Type type)
  {
    var index = (int)(_bucketSizeM04D27di * ((uint)RuntimeHelpers.GetHashCode(type) % 1));
    ref var pair = ref _bucketsM04D27di[index];
    return pair.Key == type ? pair.Value.Resolve(this) : ResolveM04D27di(type, index);
  }

  [MethodImpl((MethodImplOptions)0x8)]
  private object ResolveM04D27di(Type type, int index)
  {
    var finish = index + _bucketSizeM04D27di;
    while (++index < finish)
    {
      ref var pair = ref _bucketsM04D27di[index];
      if (pair.Key == type)
      {
        return pair.Value.Resolve(this);
      }
    }

    throw new InvalidOperationException($"Cannot resolve composition root of type {type}.");
  }

  [MethodImpl((MethodImplOptions)0x100)]
  public object Resolve(Type type, object? tag)
  {
    var index = (int)(_bucketSizeM04D27di * ((uint)RuntimeHelpers.GetHashCode(type) % 1));
    ref var pair = ref _bucketsM04D27di[index];
    return pair.Key == type ? pair.Value.ResolveByTag(this, tag) : ResolveM04D27di(type, tag, index);
  }

  [MethodImpl((MethodImplOptions)0x8)]
  private object ResolveM04D27di(Type type, object? tag, int index)
  {
    var finish = index + _bucketSizeM04D27di;
    while (++index < finish)
    {
      ref var pair = ref _bucketsM04D27di[index];
      if (pair.Key == type)
      {
        return pair.Value.ResolveByTag(this, tag);
      }
    }

    throw new InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {type}.");
  }

  public override string ToString()
  {
    return
      "classDiagram\n" +
        "  class PersonComposition {\n" +
          "    +IPerson Person\n" +
          "    + T ResolveᐸTᐳ()\n" +
          "    + T ResolveᐸTᐳ(object? tag)\n" +
          "    + object Resolve(Type type)\n" +
          "    + object Resolve(Type type, object? tag)\n" +
        "  }\n" +
        "  class Int32\n" +
        "  class String\n" +
        "  class DateTime\n" +
        "  Person --|> IPerson : \n" +
        "  class Person {\n" +
          "    +Person()\n" +
          "    +Int32 Id\n" +
          "    +String FirstName\n" +
          "    +DateTime Birthday\n" +
        "  }\n" +
        "  class IPerson {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  Person o-- Int32 : Argument \"personId\"\n" +
        "  Person o-- String : Argument \"personName\"\n" +
        "  Person o-- DateTime : Argument \"personBirthday\"\n" +
        "  PersonComposition ..> Person : IPerson Person";
  }

  private readonly static int _bucketSizeM04D27di;
  private readonly static Pair<Type, IResolver<PersonComposition, object>>[] _bucketsM04D27di;

  static PersonComposition()
  {
    var valResolverM04D27di_0000 = new ResolverM04D27di_0000();
    ResolverM04D27di<IPerson>.Value = valResolverM04D27di_0000;
    _bucketsM04D27di = Buckets<Type, IResolver<PersonComposition, object>>.Create(
      1,
      out _bucketSizeM04D27di,
      new Pair<Type, IResolver<PersonComposition, object>>[1]
      {
         new Pair<Type, IResolver<PersonComposition, object>>(typeof(IPerson), valResolverM04D27di_0000)
      });
  }

  private sealed class ResolverM04D27di<T>: IResolver<PersonComposition, T>
  {
    public static IResolver<PersonComposition, T> Value = new ResolverM04D27di<T>();

    public T Resolve(PersonComposition composite)
    {
      throw new InvalidOperationException($"Cannot resolve composition root of type {typeof(T)}.");
    }

    public T ResolveByTag(PersonComposition composite, object tag)
    {
      throw new InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type {typeof(T)}.");
    }
  }

  private sealed class ResolverM04D27di_0000: IResolver<PersonComposition, IPerson>
  {
    public IPerson Resolve(PersonComposition composition)
    {
      return composition.Person;
    }

    public IPerson ResolveByTag(PersonComposition composition, object tag)
    {
      switch (tag)
      {
        case null:
          return composition.Person;
        default:
          throw new InvalidOperationException($"Cannot resolve composition root \"{tag}\" of type IPerson.");
      }
    }
  }
}
```

</blockquote></details>

