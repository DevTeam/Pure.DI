namespace Pure.DI.IntegrationTests;

using Core;

public class SetupContextTests
{
    [Fact]
    public async Task ShouldFailWhenContextArgumentIsMissingForArgumentKind()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal partial class BaseComposition
                               {
                                   internal int Value { get; set; }

                                   private void Setup()
                                   {
                                       DI.Setup(nameof(BaseComposition), CompositionKind.Internal)
                                           .Bind<int>().To(_ => Value);
                                   }
                               }

                               internal partial class Composition
                               {
                                   private void Setup()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .DependsOn(nameof(BaseComposition), SetupContextKind.Argument, "baseContext")
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               interface IService
                               {
                                   int Value { get; }
                               }

                               class Service : IService
                               {
                                   public Service(int value)
                                   {
                                       Value = value;
                                   }

                                   public int Value { get; }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service.Value);
                                   }
                               }
                           }
                           """.RunAsync(new Options(CheckCompilationErrors: false));

        // Then
        result.Success.ShouldBeFalse(result);
    }

    [Fact]
    public async Task ShouldFailCompilationWhenInstanceMemberUsedWithoutSetupContext()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal partial class BaseComposition
                               {
                                   private int Value { get; } = 7;

                                   private void Setup()
                                   {
                                       DI.Setup(nameof(BaseComposition), CompositionKind.Internal)
                                           .Bind<int>().To(_ => Value);
                                   }
                               }

                               internal partial class Composition
                               {
                                   private void Setup()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .DependsOn(nameof(BaseComposition))
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               interface IService
                               {
                                   int Value { get; }
                               }

                               class Service : IService
                               {
                                   public Service(int value)
                                   {
                                       Value = value;
                                   }

                                   public int Value { get; }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service.Value);
                                   }
                               }
                           }
                           """.RunAsync(new Options(CheckCompilationErrors: false));

        // Then
        result.Success.ShouldBeFalse(result);
        result.Warnings.Count(i => i.Id == LogId.WarningInstanceMemberInDependsOnSetup).ShouldBe(1, result);
    }

    [Fact]
    public async Task ShouldSupportRootArgumentWithSimpleFactory()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal partial class BaseComposition
                               {
                                   internal int Value { get; set; }

                                   private void Setup()
                                   {
                                       DI.Setup(nameof(BaseComposition), CompositionKind.Internal)
                                           .Bind<int>().To(() => Value);
                                   }
                               }

                               internal partial class Composition
                               {
                                   private void Setup()
                                   {
                                       // Resolve = Off
                                       DI.Setup(nameof(Composition))
                                           .DependsOn(nameof(BaseComposition), SetupContextKind.RootArgument, "baseContext")
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               interface IService
                               {
                                   int Value { get; }
                               }

                               class Service : IService
                               {
                                   public Service(int value)
                                   {
                                       Value = value;
                                   }

                                   public int Value { get; }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var baseContext = new BaseComposition { Value = 41 };
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service(baseContext: baseContext).Value);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion: LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(0, result);
        result.StdOut.ShouldBe(["41"], result);
    }

    [Fact]
    public async Task ShouldAllowMissingNameForMembersContext()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal partial class BaseComposition
                               {
                                   // base value
                                   private int _value = 2;

                                   internal int Value => _value;

                                   internal int GetValue() => Value;

                                   private void Setup()
                                   {
                                       DI.Setup(nameof(BaseComposition), CompositionKind.Internal)
                                           .Bind<int>().To(_ => GetValue());
                                   }
                               }

                               internal partial class Composition
                               {
                                   private int _value = 2;

                                   private void Setup()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .DependsOn(nameof(BaseComposition), SetupContextKind.Members)
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }

                                   private partial int GetValue() => _value;
                                   internal void SetValue(int value) => _value = value;
                               }

                               interface IService
                               {
                                   int Value { get; }
                               }

                               class Service : IService
                               {
                                   public Service(int value)
                                   {
                                       Value = value;
                                   }

                                   public int Value { get; }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       composition.SetValue(5);
                                       Console.WriteLine(composition.Service.Value);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion: LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(0, result);
        result.StdOut.ShouldBe(["5"], result);
        result.GeneratedCode.Contains("partial int GetValue();").ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportMembersWhenCompositionInheritsBaseComposition()
    {
        // Given
        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal partial class BaseComposition
                               {
                                   // Public members
                                   public int PublicField = 1;
                                   public int PublicProperty { get; set; } = 2;
                                   public int PublicMethod() => 3;

                                   // Internal members
                                   internal int InternalField = 4;
                                   internal int InternalProperty { get; set; } = 5;
                                   internal int InternalMethod() => 6;

                                   // Private members
                                   private int PrivateField = 7;
                                   private int PrivateProperty { get; set; } = 8;
                                   private int PrivateMethod() => 9;
                                   
                                   // These fields are out of composition roots in Composition, so generator should skip them
                                   #pragma warning disable
                                   private int PrivateFieldOutOfBinding = 33;
                                   private int PrivatePropertyOutOfBinding { get; set; } = 34;
                                   private int PrivateMethodOutOfBinding() => 45;
                                   #pragma warning restore

                                   private void Setup()
                                   {
                                       DI.Setup(nameof(BaseComposition), CompositionKind.Internal)
                                           .Bind<int>("publicField").To(_ => PublicField)
                                           .Bind<int>("publicProperty").To(_ => PublicProperty)
                                           .Bind<int>("publicMethod").To(_ => PublicMethod())
                                           .Bind<int>("internalField").To(_ => InternalField)
                                           .Bind<int>("internalProperty").To(_ => InternalProperty)
                                           .Bind<int>("internalMethod").To(_ => InternalMethod())
                                           .Bind<int>("privateField").To(_ => PrivateField)
                                           .Bind<int>("privateProperty").To(_ => PrivateProperty)
                                           .Bind<int>("privateMethod").To(_ => PrivateMethod());
                                   }
                               }

                               internal partial class Composition : BaseComposition
                               {
                                   private void Setup()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .DependsOn(nameof(BaseComposition), SetupContextKind.Members)
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                                   
                                   private partial int PrivateMethod() => 9;
                               }

                               interface IService
                               {
                                   int Total { get; }
                               }

                               class Service : IService
                               {
                                   private readonly int _publicField;
                                   private readonly int _publicProperty;
                                   private readonly int _publicMethod;
                                   private readonly int _internalField;
                                   private readonly int _internalProperty;
                                   private readonly int _internalMethod;
                                   private readonly int _privateField;
                                   private readonly int _privateProperty;
                                   private readonly int _privateMethod;

                                   public Service(
                                       [Tag("publicField")] int publicField,
                                       [Tag("publicProperty")] int publicProperty,
                                       [Tag("publicMethod")] int publicMethod,
                                       [Tag("internalField")] int internalField,
                                       [Tag("internalProperty")] int internalProperty,
                                       [Tag("internalMethod")] int internalMethod,
                                       [Tag("privateField")] int privateField,
                                       [Tag("privateProperty")] int privateProperty,
                                       [Tag("privateMethod")] int privateMethod)
                                   {
                                       _publicField = publicField;
                                       _publicProperty = publicProperty;
                                       _publicMethod = publicMethod;
                                       _internalField = internalField;
                                       _internalProperty = internalProperty;
                                       _internalMethod = internalMethod;
                                       _privateField = privateField;
                                       _privateProperty = privateProperty;
                                       _privateMethod = privateMethod;
                                   }

                                   public int Total => _publicField + _publicProperty + _publicMethod
                                       + _internalField + _internalProperty + _internalMethod
                                       + _privateField + _privateProperty + _privateMethod;
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service.Total);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion: LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(0, result);
        result.StdOut.ShouldBe(["45"], result);
    }


    [Fact]
    public async Task ShouldSupportSetupContextMembers()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal sealed class Settings
                               {
                                   public Settings(int value)
                                   {
                                       Value = value;
                                   }

                                   public int Value { get; }
                               }

                               internal partial class BaseComposition
                               {
                                   internal int GetValue() => 3;

                                   private void Setup()
                                   {
                                       DI.Setup(nameof(BaseComposition), CompositionKind.Internal)
                                           .Bind<int>().To(_ => GetValue());
                                   }
                               }

                               internal partial class Composition
                               {
                                   private void Setup()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .DependsOn(nameof(BaseComposition), SetupContextKind.Members, "baseContext")
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }

                                   private partial int GetValue() => 5;
                               }

                               interface IService
                               {
                                   int Value { get; }
                               }

                               class Service : IService
                               {
                                   public Service(int value)
                                   {
                                       Value = value;
                                   }

                                   public int Value { get; }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service.Value);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion: LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(0, result);
        result.StdOut.ShouldBe(["5"], result);
        result.GeneratedCode.Contains("partial int GetValue();").ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldCopyMembersWithCommentsAttributesAndExternalTypes()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace External
                           {
                               internal sealed class ExternalSettings
                               {
                                   public ExternalSettings(int value)
                                   {
                                       Value = value;
                                   }

                                   public int Value { get; }
                               }

                               [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
                               internal sealed class ExternalMarkerAttribute : Attribute {}
                           }

                           namespace Sample
                           {
                               using External;

                               internal partial class BaseComposition
                               {
                                   // external settings field
                                   [ExternalMarker]
                                   internal ExternalSettings SettingsField = new ExternalSettings(5);

                                   // external settings property
                                   [ExternalMarker]
                                   internal ExternalSettings SettingsProperty { get; } = new ExternalSettings(7);

                                   // external settings method
                                   [ExternalMarker]
                                   internal int GetValue()
                                   {
                                       return SettingsField.Value + SettingsProperty.Value;
                                   }

                                   private void Setup()
                                   {
                                       DI.Setup(nameof(BaseComposition), CompositionKind.Internal)
                                           .Bind<int>().To(_ => GetValue());
                                   }
                               }

                               internal partial class Composition
                               {
                                   private int _value = 12;

                                   private void Setup()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .DependsOn(nameof(BaseComposition), SetupContextKind.Members, "baseContext")
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }

                                   private partial int GetValue() => _value;
                               }

                               interface IService
                               {
                                   int Value { get; }
                               }

                               class Service : IService
                               {
                                   public Service(int value)
                                   {
                                       Value = value;
                                   }

                                   public int Value { get; }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service.Value);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion: LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(0, result);
        result.StdOut.ShouldBe(["12"], result);
        result.GeneratedCode.Contains("global::External.ExternalMarkerAttribute").ShouldBeTrue(result);
        result.GeneratedCode.Contains("// external settings method").ShouldBeTrue(result);
        result.GeneratedCode.Contains("partial int GetValue();").ShouldBeTrue(result);
        result.GeneratedCode.Contains("return SettingsField.Value").ShouldBeFalse(result);
    }

    [Fact]
    public async Task ShouldCreatePartialAccessorMethodsForPropertyLogic()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace External
                           {
                               internal sealed class Counter
                               {
                                   public int Value { get; set; }
                               }

                               [AttributeUsage(AttributeTargets.Property)]
                               internal sealed class ExternalMarkerAttribute : Attribute {}
                           }

                           namespace Sample
                           {
                               using External;

                               internal partial class BaseComposition
                               {
                                   private readonly Counter _counter = new Counter();

                                   // external counter property
                                   [ExternalMarker]
                                   internal int Count
                                   {
                                       get => _counter.Value;
                                       set => _counter.Value = value;
                                   }

                                   private void Setup()
                                   {
                                       DI.Setup(nameof(BaseComposition), CompositionKind.Internal)
                                           .Bind<int>().To(_ => Count);
                                   }
                               }

                               internal partial class Composition
                               {
                                   private readonly Counter _counter = new Counter();

                                   private void Setup()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .DependsOn(nameof(BaseComposition), SetupContextKind.Members)
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }

                                   private partial int get__Count() => _counter.Value;
                                    internal void SetCount(int value) => _counter.Value = value;
                               }

                               interface IService
                               {
                                   int Value { get; }
                               }

                               class Service : IService
                               {
                                   public Service(int value)
                                   {
                                       Value = value;
                                   }

                                   public int Value { get; }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       composition.SetCount(9);
                                       Console.WriteLine(composition.Service.Value);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion: LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(0, result);
        result.StdOut.ShouldBe(["9"], result);
        result.GeneratedCode.Contains("global::External.ExternalMarkerAttribute").ShouldBeTrue(result);
        result.GeneratedCode.Contains("// external counter property").ShouldBeTrue(result);
        result.GeneratedCode.Contains("get => get__Count()").ShouldBeTrue(result);
        result.GeneratedCode.Contains("partial int get__Count();").ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportOverridesForMembers()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal abstract class BaseSettings
                               {
                                   protected int ValueField;

                                   public virtual int Value
                                   {
                                       get => ValueField;
                                       set => ValueField = value;
                                   }

                                   public virtual int GetValue() => Value;
                               }

                               internal partial class BaseComposition : BaseSettings
                               {
                                   public override int Value
                                   {
                                       get => base.Value;
                                       set => base.Value = value + 1;
                                   }

                                   public override int GetValue() => Value * 2;

                                   private void Setup()
                                   {
                                       DI.Setup(nameof(BaseComposition), CompositionKind.Internal)
                                           .Bind<int>().To(_ => GetValue());
                                   }
                               }

                               internal partial class Composition : BaseComposition
                               {
                                   private void Setup()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .DependsOn(nameof(BaseComposition), SetupContextKind.Members)
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               interface IService
                               {
                                   int Value { get; }
                               }

                               class Service : IService
                               {
                                   public Service(int value)
                                   {
                                       Value = value;
                                   }

                                   public int Value { get; }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       composition.Value = 3;
                                       Console.WriteLine(composition.Service.Value);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion: LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(0, result);
        result.StdOut.ShouldBe(["8"], result);
    }

    [Fact]
    public async Task ShouldCopyFieldsWithCommentsAndAttributes()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace External
                           {
                               [AttributeUsage(AttributeTargets.Field)]
                               internal sealed class FieldMarkerAttribute : Attribute
                               {
                                   public string Name { get; }

                                   public FieldMarkerAttribute(string name)
                                   {
                                       Name = name;
                                   }
                               }
                           }

                           namespace Sample
                           {
                               using External;

                               internal partial class BaseComposition
                               {
                                   // private field with marker
                                   [FieldMarker("important")]
                                   private int _importantField = 42;

                                   // internal field with marker
                                   [FieldMarker("internalField")]
                                   internal int _internalField = 100;

                                   private void Setup()
                                   {
                                       DI.Setup(nameof(BaseComposition), CompositionKind.Internal)
                                           .Bind<int>().To(_ => _importantField);
                                   }
                               }

                               internal partial class Composition
                               {
                                   private void Setup()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .DependsOn(nameof(BaseComposition), SetupContextKind.Members)
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }

                               }

                               interface IService
                               {
                                   int Value { get; }
                               }

                               class Service : IService
                               {
                                   public Service(int value)
                                   {
                                       Value = value;
                                   }

                                   public int Value { get; }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service.Value);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion: LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(0, result);
        result.StdOut.ShouldBe(["42"], result);
        result.GeneratedCode.Contains("// private field with marker").ShouldBeTrue(result);
        result.GeneratedCode.Contains("global::External.FieldMarkerAttribute").ShouldBeTrue(result);
        result.GeneratedCode.Contains("private int _importantField").ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldCopyPropertiesWithCommentsAndAttributes()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace External
                           {
                               [AttributeUsage(AttributeTargets.Property)]
                               internal sealed class PropertyMarkerAttribute : Attribute
                               {
                                   public string Description { get; }

                                   public PropertyMarkerAttribute(string description)
                                   {
                                       Description = description;
                                   }
                               }
                           }

                           namespace Sample
                           {
                               using External;

                               internal partial class BaseComposition
                               {
                                   // property with getter and setter
                                   [PropertyMarker("configuration property")]
                                   internal int ConfigValue
                                   {
                                       get => 42;
                                       set => Console.WriteLine(value);
                                   }

                                   private void Setup()
                                   {
                                       DI.Setup(nameof(BaseComposition), CompositionKind.Internal)
                                           .Bind<int>().To(_ => ConfigValue);
                                   }
                               }

                               internal partial class Composition
                               {
                                   private void Setup()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .DependsOn(nameof(BaseComposition), SetupContextKind.Members)
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }

                                   private partial int get__ConfigValue() => 88;
                               }

                               interface IService
                               {
                                   int Value { get; }
                               }

                               class Service : IService
                               {
                                   public Service(int value)
                                   {
                                       Value = value;
                                   }

                                   public int Value { get; }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service.Value);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion: LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(0, result);
        result.StdOut.ShouldBe(["88"], result);
        result.GeneratedCode.Contains("// property with getter and setter").ShouldBeTrue(result);
        result.GeneratedCode.Contains("global::External.PropertyMarkerAttribute").ShouldBeTrue(result);
        result.GeneratedCode.Contains("get => get__ConfigValue()").ShouldBeTrue(result);
        result.GeneratedCode.Contains("partial int get__ConfigValue();").ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldCopyMethodsWithCommentsAndAttributes()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace External
                           {
                               [AttributeUsage(AttributeTargets.Method)]
                               internal sealed class MethodMarkerAttribute : Attribute
                               {
                                   public string Category { get; }

                                   public MethodMarkerAttribute(string category)
                                   {
                                       Category = category;
                                   }
                               }
                           }

                           namespace Sample
                           {
                               using External;

                               internal partial class BaseComposition
                               {
                                   // utility method for calculations
                                   [MethodMarker("math")]
                                   internal int CalculateValue()
                                   {
                                       return 42;
                                   }

                                   private void Setup()
                                   {
                                       DI.Setup(nameof(BaseComposition), CompositionKind.Internal)
                                           .Bind<int>().To(_ => CalculateValue());
                                   }
                               }

                               internal partial class Composition
                               {
                                   private void Setup()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .DependsOn(nameof(BaseComposition), SetupContextKind.Members)
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }

                                   private partial int CalculateValue() => 66;
                               }

                               interface IService
                               {
                                   int Value { get; }
                               }

                               class Service : IService
                               {
                                   public Service(int value)
                                   {
                                       Value = value;
                                   }

                                   public int Value { get; }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service.Value);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion: LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(0, result);
        result.StdOut.ShouldBe(["66"], result);
        result.GeneratedCode.Contains("// utility method for calculations").ShouldBeTrue(result);
        result.GeneratedCode.Contains("global::External.MethodMarkerAttribute").ShouldBeTrue(result);
        result.GeneratedCode.Contains("partial int CalculateValue();").ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldCopyOnlyPrivateMembersWhenCompositionInheritsBase()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal partial class BaseComposition
                               {
                                   // These members are accessible through inheritance, should NOT be copied
                                   public int PublicMember = 1;
                                   internal int InternalMember = 2;
                                   protected int ProtectedMember = 3;

                                   // This member is NOT accessible through inheritance, should be copied
                                   private int _privateMember = 4;

                                   private void Setup()
                                   {
                                       DI.Setup(nameof(BaseComposition), CompositionKind.Internal)
                                           .Bind<int>().To(_ => _privateMember);
                                   }
                               }

                               internal partial class Composition : BaseComposition
                               {
                                   private void Setup()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .DependsOn(nameof(BaseComposition), SetupContextKind.Members)
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               interface IService
                               {
                                   int Value { get; }
                               }

                               class Service : IService
                               {
                                   public Service(int value)
                                   {
                                       Value = value;
                                   }

                                   public int Value { get; }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service.Value);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion: LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(0, result);
        result.StdOut.ShouldBe(["4"], result);
        result.GeneratedCode.Contains("public int PublicMember").ShouldBeFalse(result);
        result.GeneratedCode.Contains("internal int InternalMember").ShouldBeFalse(result);
        result.GeneratedCode.Contains("protected int ProtectedMember").ShouldBeFalse(result);
        result.GeneratedCode.Contains("// This member is NOT accessible through inheritance, should be copied").ShouldBeTrue(result);
        result.GeneratedCode.Contains("private int _privateMember").ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldCopyAllNonPrivateMembersWhenCompositionDoesNotInheritBase()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal partial class BaseComposition
                               {
                                   // These members are NOT accessible through inheritance, should be copied
                                   public int PublicMember = 1;
                                   internal int InternalMember = 2;

                                   // This member is NOT accessible through inheritance, should be copied
                                   private int _privateMember = 4;

                                   private void Setup()
                                   {
                                       DI.Setup(nameof(BaseComposition), CompositionKind.Internal)
                                           .Bind<int>("public").To(_ => PublicMember)
                                           .Bind<int>("internal").To(_ => InternalMember)
                                           .Bind<int>("private").To(_ => _privateMember);
                                   }
                               }

                               internal partial class Composition
                               {
                                   private void Setup()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .DependsOn(nameof(BaseComposition), SetupContextKind.Members)
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               interface IService
                               {
                                   int Total { get; }
                               }

                               class Service : IService
                               {
                                   private readonly int _public;
                                   private readonly int _internal;
                                   private readonly int _private;

                                   public Service(
                                       [Tag("public")] int @public,
                                       [Tag("internal")] int @internal,
                                       [Tag("private")] int @private)
                                   {
                                       _public = @public;
                                       _internal = @internal;
                                       _private = @private;
                                   }

                                   public int Total => _public + _internal + _private;
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service.Total);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion: LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(0, result);
        result.StdOut.ShouldBe(["7"], result);
        result.GeneratedCode.Contains("// These members are NOT accessible through inheritance, should be copied").ShouldBeTrue(result);
        result.GeneratedCode.Contains("public int PublicMember").ShouldBeTrue(result);
        result.GeneratedCode.Contains("internal int InternalMember").ShouldBeTrue(result);
        result.GeneratedCode.Contains("// This member is NOT accessible through inheritance, should be copied").ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldNotCopyStaticMembers()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal partial class BaseComposition
                               {
                                   // static member - should NOT be copied
                                   private static int StaticValue = 99;
                                   private static int GetStaticValue() => StaticValue;

                                   private int _instanceValue = 42;

                                   private void Setup()
                                   {
                                       DI.Setup(nameof(BaseComposition), CompositionKind.Internal)
                                           .Bind<int>().To(_ => _instanceValue);
                                   }
                               }

                               internal partial class Composition
                               {
                                   private void Setup()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .DependsOn(nameof(BaseComposition), SetupContextKind.Members)
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }
                               }

                               interface IService
                               {
                                   int Value { get; }
                               }

                               class Service : IService
                               {
                                   public Service(int value)
                                   {
                                       Value = value;
                                   }

                                   public int Value { get; }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service.Value);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion: LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(0, result);
        result.StdOut.ShouldBe(["42"], result);
        result.GeneratedCode.Contains("static int StaticValue").ShouldBeFalse(result);
        result.GeneratedCode.Contains("static int GetStaticValue()").ShouldBeFalse(result);
    }

    [Fact]
    public async Task ShouldCopyPropertiesWithExpressionBody()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using System.Collections.Generic;
                           using System.Linq;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal partial class BaseComposition
                               {
                                   private readonly List<string> _items = new List<string> { "a", "b", "c" };

                                   // count expression-bodied property
                                   internal int Count => _items.Count;

                                   // sum expression-bodied property
                                   internal int Sum => _items.Sum(x => x.Length);

                                   private void Setup()
                                   {
                                       DI.Setup(nameof(BaseComposition), CompositionKind.Internal)
                                           .Bind<int>("count").To(_ => Count)
                                           .Bind<int>("sum").To(_ => Sum);
                                   }
                               }

                               internal partial class Composition
                               {
                                   private int _count = 5;
                                   private int _sum = 10;

                                   private void Setup()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .DependsOn(nameof(BaseComposition), SetupContextKind.Members)
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }

                                   private partial int get__Count() => _count;
                                   private partial int get__Sum() => _sum;
                               }

                               interface IService
                               {
                                   int Total { get; }
                               }

                               class Service : IService
                               {
                                   private readonly int _count;
                                   private readonly int _sum;

                                   public Service(
                                       [Tag("count")] int count,
                                       [Tag("sum")] int sum)
                                   {
                                       _count = count;
                                       _sum = sum;
                                   }

                                   public int Total => _count + _sum;
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service.Total);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion: LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(0, result);
        result.StdOut.ShouldBe(["15"], result);
        result.GeneratedCode.Contains("// count expression-bodied property").ShouldBeTrue(result);
        result.GeneratedCode.Contains("internal int Count { get => get__Count(); }").ShouldBeTrue(result);
        result.GeneratedCode.Contains("partial int get__Count()").ShouldBeTrue(result);
    }

    [Fact]
    public async Task ShouldSupportSingleBindingWithFieldPropertyAndMethod()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace Sample
                           {
                               internal partial class BaseComposition
                               {
                                   // instance field
                                   private int _baseField = 1;

                                   // instance property
                                   private int BaseProperty { get { return 2; } }
                                   
                                   // instance property
                                   internal int BaseProperty2 { get; } = 4;

                                   // instance method
                                   private int GetBaseValue() => 3;

                                   private void Setup()
                                   {
                                       DI.Setup(nameof(BaseComposition), CompositionKind.Internal)
                                           .Bind<int>().To(_ => _baseField + BaseProperty + GetBaseValue() + BaseProperty2);
                                   }
                               }

                               internal partial class Composition
                               {
                                   private void Setup()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .DependsOn(nameof(BaseComposition), SetupContextKind.Members)
                                           .Bind<IService>().To<Service>()
                                           .Root<IService>("Service");
                                   }

                                   private partial int get__BaseProperty() => 20;
                                   private partial int GetBaseValue() => 30;
                               }

                               interface IService
                               {
                                   int Value { get; }
                               }

                               class Service : IService
                               {
                                   public Service(int value)
                                   {
                                       Value = value;
                                   }

                                   public int Value { get; }
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition();
                                       Console.WriteLine(composition.Service.Value);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion: LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(["55"], result);
    }

    [Fact]
    public async Task ShouldCopySetupContextMembersWhenCreatingScope()
    {
        // Given

        // When
        var result = await """
                           using System;
                           using Pure.DI;

                           namespace UnityEngine
                           {
                               // Lightweight test double to avoid Unity dependency
                               public class MonoBehaviour {}
                           }

                           namespace Sample
                           {
                               internal partial class BaseComposition
                               {
                                   // Host/runtime value that should flow to child scopes
                                   internal int SettingsValue { get; set; }

                                   private void Setup()
                                   {
                                       DI.Setup(nameof(BaseComposition), CompositionKind.Internal)
                                           .Bind<int>().To(_ => SettingsValue);
                                   }
                               }

                               interface IService : IDisposable
                               {
                                   int Value { get; }

                                   bool IsDisposed { get; }
                               }

                               sealed class Service : IService
                               {
                                   public Service(int value)
                                   {
                                       Value = value;
                                   }

                                   public int Value { get; }

                                   public bool IsDisposed { get; private set; }

                                   public void Dispose() => IsDisposed = true;
                               }

                               internal partial class Composition : UnityEngine.MonoBehaviour
                               {
                                   private void Setup()
                                   {
                                       DI.Setup(nameof(Composition))
                                           .DependsOn(nameof(BaseComposition), SetupContextKind.Members)
                                           .Bind<IService>().As(Lifetime.Scoped).To<Service>()
                                           .Root<IService>("Service");
                                   }

                                   public Composition CreateScope() => new(this);
                               }

                               public class Program
                               {
                                   public static void Main()
                                   {
                                       var composition = new Composition
                                       {
                                           SettingsValue = 41
                                       };

                                       var scope1 = composition.CreateScope();
                                       var service11 = scope1.Service;
                                       var service12 = scope1.Service;
                                       Console.WriteLine(service11.Value);
                                       Console.WriteLine(service11 == service12);

                                       var scope2 = composition.CreateScope();
                                       var service2 = scope2.Service;
                                       Console.WriteLine(service2.Value);
                                       Console.WriteLine(service11 == service2);

                                       scope1.Dispose();
                                       Console.WriteLine(service11.IsDisposed);

                                       scope2.Dispose();
                                       Console.WriteLine(service2.IsDisposed);
                                   }
                               }
                           }
                           """.RunAsync(new Options(LanguageVersion: LanguageVersion.CSharp9));

        // Then
        result.Success.ShouldBeTrue(result);
        result.Errors.Count.ShouldBe(0, result);
        result.Warnings.Count.ShouldBe(0, result);
        result.StdOut.ShouldBe(["41", "True", "41", "False", "True", "True"], result);
    }
}
