namespace Pure.DI.Core.Models;

internal readonly record struct MdHint(Hint Key, LinkedList<string> Values);