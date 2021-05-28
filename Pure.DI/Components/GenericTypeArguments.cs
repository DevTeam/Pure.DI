
// ReSharper disable InconsistentNaming
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InvalidXmlDocComment
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedType.Global
#pragma warning disable 0436
#pragma warning disable 8714
namespace Pure.DI
{    
    /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS { }

/// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IDisposable</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTDisposable: System.IDisposable { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IComparable</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTComparable: System.IComparable { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IComparable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTComparable<in T>: System.IComparable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IEquatable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEquatable<T>: System.IEquatable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IEnumerable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEnumerable<out T>: System.Collections.Generic.IEnumerable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IEnumerator[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEnumerator<out T>: System.Collections.Generic.IEnumerator<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.ICollection[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTCollection<T>: System.Collections.Generic.ICollection<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IList[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTList<T>: System.Collections.Generic.IList<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.ISet[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTSet<T>: System.Collections.Generic.ISet<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IComparer[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTComparer<in T>: System.Collections.Generic.IComparer<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IEqualityComparer[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEqualityComparer<in T>: System.Collections.Generic.IEqualityComparer<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IDictionary[TKey, TValue]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTDictionary<TKey, TValue>: System.Collections.Generic.IDictionary<TKey, TValue> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IObservable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTObservable<out T>: System.IObservable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IObserver[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTObserver<in T>: System.IObserver<T> { }
#endif
        /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT1 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC1 { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI1 { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS1 { }

/// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IDisposable</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTDisposable1: System.IDisposable { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IComparable</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTComparable1: System.IComparable { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IComparable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTComparable1<in T>: System.IComparable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IEquatable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEquatable1<T>: System.IEquatable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IEnumerable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEnumerable1<out T>: System.Collections.Generic.IEnumerable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IEnumerator[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEnumerator1<out T>: System.Collections.Generic.IEnumerator<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.ICollection[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTCollection1<T>: System.Collections.Generic.ICollection<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IList[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTList1<T>: System.Collections.Generic.IList<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.ISet[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTSet1<T>: System.Collections.Generic.ISet<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IComparer[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTComparer1<in T>: System.Collections.Generic.IComparer<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IEqualityComparer[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEqualityComparer1<in T>: System.Collections.Generic.IEqualityComparer<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IDictionary[TKey, TValue]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTDictionary1<TKey, TValue>: System.Collections.Generic.IDictionary<TKey, TValue> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IObservable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTObservable1<out T>: System.IObservable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IObserver[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTObserver1<in T>: System.IObserver<T> { }
#endif
        /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT2 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC2 { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI2 { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS2 { }

/// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IDisposable</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTDisposable2: System.IDisposable { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IComparable</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTComparable2: System.IComparable { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IComparable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTComparable2<in T>: System.IComparable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IEquatable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEquatable2<T>: System.IEquatable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IEnumerable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEnumerable2<out T>: System.Collections.Generic.IEnumerable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IEnumerator[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEnumerator2<out T>: System.Collections.Generic.IEnumerator<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.ICollection[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTCollection2<T>: System.Collections.Generic.ICollection<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IList[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTList2<T>: System.Collections.Generic.IList<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.ISet[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTSet2<T>: System.Collections.Generic.ISet<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IComparer[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTComparer2<in T>: System.Collections.Generic.IComparer<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IEqualityComparer[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEqualityComparer2<in T>: System.Collections.Generic.IEqualityComparer<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IDictionary[TKey, TValue]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTDictionary2<TKey, TValue>: System.Collections.Generic.IDictionary<TKey, TValue> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IObservable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTObservable2<out T>: System.IObservable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IObserver[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTObserver2<in T>: System.IObserver<T> { }
#endif
        /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT3 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC3 { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI3 { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS3 { }

/// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IDisposable</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTDisposable3: System.IDisposable { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IComparable</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTComparable3: System.IComparable { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IComparable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTComparable3<in T>: System.IComparable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IEquatable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEquatable3<T>: System.IEquatable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IEnumerable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEnumerable3<out T>: System.Collections.Generic.IEnumerable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IEnumerator[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEnumerator3<out T>: System.Collections.Generic.IEnumerator<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.ICollection[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTCollection3<T>: System.Collections.Generic.ICollection<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IList[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTList3<T>: System.Collections.Generic.IList<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.ISet[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTSet3<T>: System.Collections.Generic.ISet<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IComparer[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTComparer3<in T>: System.Collections.Generic.IComparer<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IEqualityComparer[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEqualityComparer3<in T>: System.Collections.Generic.IEqualityComparer<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IDictionary[TKey, TValue]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTDictionary3<TKey, TValue>: System.Collections.Generic.IDictionary<TKey, TValue> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IObservable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTObservable3<out T>: System.IObservable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IObserver[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTObserver3<in T>: System.IObserver<T> { }
#endif
        /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT4 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC4 { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI4 { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS4 { }

/// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IDisposable</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTDisposable4: System.IDisposable { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IComparable</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTComparable4: System.IComparable { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IComparable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTComparable4<in T>: System.IComparable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IEquatable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEquatable4<T>: System.IEquatable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IEnumerable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEnumerable4<out T>: System.Collections.Generic.IEnumerable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IEnumerator[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEnumerator4<out T>: System.Collections.Generic.IEnumerator<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.ICollection[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTCollection4<T>: System.Collections.Generic.ICollection<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IList[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTList4<T>: System.Collections.Generic.IList<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.ISet[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTSet4<T>: System.Collections.Generic.ISet<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IComparer[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTComparer4<in T>: System.Collections.Generic.IComparer<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IEqualityComparer[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEqualityComparer4<in T>: System.Collections.Generic.IEqualityComparer<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IDictionary[TKey, TValue]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTDictionary4<TKey, TValue>: System.Collections.Generic.IDictionary<TKey, TValue> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IObservable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTObservable4<out T>: System.IObservable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IObserver[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTObserver4<in T>: System.IObserver<T> { }
#endif
        /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT5 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC5 { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI5 { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS5 { }

/// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IDisposable</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTDisposable5: System.IDisposable { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IComparable</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTComparable5: System.IComparable { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IComparable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTComparable5<in T>: System.IComparable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IEquatable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEquatable5<T>: System.IEquatable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IEnumerable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEnumerable5<out T>: System.Collections.Generic.IEnumerable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IEnumerator[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEnumerator5<out T>: System.Collections.Generic.IEnumerator<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.ICollection[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTCollection5<T>: System.Collections.Generic.ICollection<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IList[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTList5<T>: System.Collections.Generic.IList<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.ISet[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTSet5<T>: System.Collections.Generic.ISet<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IComparer[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTComparer5<in T>: System.Collections.Generic.IComparer<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IEqualityComparer[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEqualityComparer5<in T>: System.Collections.Generic.IEqualityComparer<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IDictionary[TKey, TValue]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTDictionary5<TKey, TValue>: System.Collections.Generic.IDictionary<TKey, TValue> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IObservable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTObservable5<out T>: System.IObservable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IObserver[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTObserver5<in T>: System.IObserver<T> { }
#endif
        /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT6 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC6 { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI6 { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS6 { }

/// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IDisposable</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTDisposable6: System.IDisposable { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IComparable</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTComparable6: System.IComparable { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IComparable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTComparable6<in T>: System.IComparable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IEquatable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEquatable6<T>: System.IEquatable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IEnumerable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEnumerable6<out T>: System.Collections.Generic.IEnumerable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IEnumerator[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEnumerator6<out T>: System.Collections.Generic.IEnumerator<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.ICollection[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTCollection6<T>: System.Collections.Generic.ICollection<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IList[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTList6<T>: System.Collections.Generic.IList<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.ISet[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTSet6<T>: System.Collections.Generic.ISet<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IComparer[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTComparer6<in T>: System.Collections.Generic.IComparer<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IEqualityComparer[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEqualityComparer6<in T>: System.Collections.Generic.IEqualityComparer<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IDictionary[TKey, TValue]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTDictionary6<TKey, TValue>: System.Collections.Generic.IDictionary<TKey, TValue> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IObservable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTObservable6<out T>: System.IObservable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IObserver[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTObserver6<in T>: System.IObserver<T> { }
#endif
        /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT7 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC7 { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI7 { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS7 { }

/// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IDisposable</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTDisposable7: System.IDisposable { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IComparable</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTComparable7: System.IComparable { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IComparable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTComparable7<in T>: System.IComparable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IEquatable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEquatable7<T>: System.IEquatable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IEnumerable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEnumerable7<out T>: System.Collections.Generic.IEnumerable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IEnumerator[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEnumerator7<out T>: System.Collections.Generic.IEnumerator<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.ICollection[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTCollection7<T>: System.Collections.Generic.ICollection<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IList[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTList7<T>: System.Collections.Generic.IList<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.ISet[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTSet7<T>: System.Collections.Generic.ISet<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IComparer[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTComparer7<in T>: System.Collections.Generic.IComparer<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IEqualityComparer[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEqualityComparer7<in T>: System.Collections.Generic.IEqualityComparer<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IDictionary[TKey, TValue]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTDictionary7<TKey, TValue>: System.Collections.Generic.IDictionary<TKey, TValue> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IObservable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTObservable7<out T>: System.IObservable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IObserver[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTObserver7<in T>: System.IObserver<T> { }
#endif
        /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT8 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC8 { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI8 { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS8 { }

/// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IDisposable</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTDisposable8: System.IDisposable { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IComparable</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTComparable8: System.IComparable { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IComparable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTComparable8<in T>: System.IComparable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IEquatable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEquatable8<T>: System.IEquatable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IEnumerable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEnumerable8<out T>: System.Collections.Generic.IEnumerable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IEnumerator[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEnumerator8<out T>: System.Collections.Generic.IEnumerator<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.ICollection[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTCollection8<T>: System.Collections.Generic.ICollection<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IList[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTList8<T>: System.Collections.Generic.IList<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.ISet[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTSet8<T>: System.Collections.Generic.ISet<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IComparer[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTComparer8<in T>: System.Collections.Generic.IComparer<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IEqualityComparer[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTEqualityComparer8<in T>: System.Collections.Generic.IEqualityComparer<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.Collections.Generic.IDictionary[TKey, TValue]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTDictionary8<TKey, TValue>: System.Collections.Generic.IDictionary<TKey, TValue> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IObservable[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTObservable8<out T>: System.IObservable<T> { }
#endif
    /// <summary>
#if !NET35
    /// Represents the generic type arguments marker for <c>System.IObserver[T]</c>.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTObserver8<in T>: System.IObserver<T> { }
#endif
        /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT9 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC9 { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI9 { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS9 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT10 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC10 { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI10 { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS10 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT11 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC11 { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI11 { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS11 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT12 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC12 { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI12 { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS12 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT13 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC13 { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI13 { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS13 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT14 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC14 { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI14 { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS14 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT15 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC15 { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI15 { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS15 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT16 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC16 { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI16 { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS16 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT17 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC17 { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI17 { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS17 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT18 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC18 { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI18 { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS18 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT19 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC19 { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI19 { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS19 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT20 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC20 { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI20 { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS20 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT21 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC21 { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI21 { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS21 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT22 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC22 { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI22 { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS22 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT23 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC23 { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI23 { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS23 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT24 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC24 { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI24 { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS24 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT25 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC25 { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI25 { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS25 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT26 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC26 { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI26 { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS26 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT27 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC27 { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI27 { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS27 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT28 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC28 { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI28 { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS28 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT29 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC29 { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI29 { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS29 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT30 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC30 { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI30 { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS30 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT31 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC31 { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI31 { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS31 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type.
    /// </summary>
    [GenericTypeArgument]
    internal abstract class TT32 { }

    /// <summary>
    /// Represents the generic type arguments marker for a reference type with default constructor.
    /// </summary>
    [GenericTypeArgument]
    internal class TTC32 { }

    /// <summary>
    /// Represents the generic type arguments marker for an interface.
    /// </summary>
    [GenericTypeArgument]
    internal interface TTI32 { }

    /// <summary>
    /// Represents the generic type arguments marker for a value type.
    /// </summary>
    [GenericTypeArgument]
    internal struct TTS32 { }

}
