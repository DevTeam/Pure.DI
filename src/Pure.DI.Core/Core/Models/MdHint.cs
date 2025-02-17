namespace Pure.DI.Core.Models;

readonly record struct MdHint(Hint Key, LinkedList<string> Values);