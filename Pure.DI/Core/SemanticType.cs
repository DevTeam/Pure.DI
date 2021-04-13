// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
namespace Pure.DI.Core
{
    using System;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class SemanticType
    {
        public readonly ITypeSymbol Type;
        public readonly SemanticModel SemanticModel;

        public SemanticType(ITypeSymbol type, SemanticModel semanticModel)
        {
            Type = type;
            SemanticModel = semanticModel;
        }

        public static implicit operator SemanticModel(SemanticType semanticType) => semanticType.SemanticModel;

        public static implicit operator TypeSyntax(SemanticType semanticType) => SyntaxFactory.ParseTypeName(semanticType.Type.ToMinimalDisplayString(semanticType.SemanticModel, 0));

        public bool Equals(SemanticType other) => SemanticTypeEqualityComparer.Default.Equals(this, other);

        public override string ToString() => Type.ToString();

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetType() == GetType() && Equals((SemanticType) obj);
        }

        public override int GetHashCode() => SemanticTypeEqualityComparer.Default.GetHashCode();

        public SemanticType Construct(params SemanticType[] typeArg)
        {
            if (
                Type is not INamedTypeSymbol namedTypeSymbol
                || !namedTypeSymbol.IsGenericType)
            {
                throw new ArgumentException($"{Type} is nt generic type.");
            }

            var unboundGeneric = namedTypeSymbol.ConstructUnboundGenericType();
            var typeName = string.Join("", unboundGeneric.ToDisplayParts().TakeWhile(i => i.ToString() != "<")) + "`" + namedTypeSymbol.TypeArguments.Length;
            unboundGeneric = (
                from ns in new[] { string.Empty }.Concat(SemanticModel.LookupNamespacesAndTypes(0).Select(i => $"{i}."))
                let generic = SemanticModel.Compilation.GetTypeByMetadataName($"{ns}{typeName}")
                where generic != null
                select generic).FirstOrDefault();

            if (unboundGeneric == null)
            {
                throw new InvalidOperationException($"Cannot construct {Type}.");
            }

            return new SemanticType(unboundGeneric.Construct(typeArg.Select(i => i.Type).ToArray()), SemanticModel);
        }

        public bool IsGenericTypeMarker
        {
            get
            {
                foreach (var attr in Type.GetAttributes())
                {
                    if (
                        attr.AttributeClass != null
                        && new SemanticType(attr.AttributeClass, SemanticModel).Equals(typeof(GenericTypeArgumentAttribute)))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool IsComposedGenericTypeMarker
        {
            get
            {
                if (IsGenericTypeMarker)
                {
                    return true;
                }

                return Type switch
                {
                    INamedTypeSymbol {IsGenericType: false} => false,
                    INamedTypeSymbol namedTypeSymbol => namedTypeSymbol.TypeArguments.OfType<INamedTypeSymbol>().Any(type => new SemanticType(type, SemanticModel).IsComposedGenericTypeMarker),
                    IArrayTypeSymbol arrayTypeSymbol => new SemanticType(arrayTypeSymbol.ElementType, SemanticModel).IsComposedGenericTypeMarker,
                    _ => false
                };
            }
        }

        public bool IsValidTypeToResolve =>
            Type switch
            {
                INamedTypeSymbol namedTypeSymbol => !namedTypeSymbol.IsUnboundGenericType && !IsComposedGenericTypeMarker,
                IArrayTypeSymbol arrayTypeSymbol => new SemanticType(arrayTypeSymbol, SemanticModel).IsValidTypeToResolve,
                _ => false
            };

        public bool Equals(Type type) =>
            SymbolEqualityComparer.Default.Equals(ToTypeSymbol(type), Type);

        public TypeSyntax TypeSyntax => SyntaxFactory.ParseTypeName(Type.ToMinimalDisplayString(SemanticModel, 0));

        private INamedTypeSymbol ToTypeSymbol(Type type)
        {
            if (type.FullName == null)
            {
                throw new ArgumentException(nameof(type));
            }

            if (!type.IsConstructedGenericType)
            {
                return SemanticModel.Compilation.GetTypeByMetadataName(type.FullName) ?? throw new InvalidOperationException("Cannot convert type.");
            }

            var typeArgumentsTypeInfos = type.GenericTypeArguments.Select(ToTypeSymbol);
            var openType = type.GetGenericTypeDefinition();
            var typeSymbol = SemanticModel.Compilation.GetTypeByMetadataName(openType.FullName ?? throw new InvalidOperationException("Cannot get generic type name."));
            return typeSymbol?.Construct(typeArgumentsTypeInfos.ToArray<ITypeSymbol>()) ?? throw new InvalidOperationException("Cannot convert generic type.");
        }
    }
}
