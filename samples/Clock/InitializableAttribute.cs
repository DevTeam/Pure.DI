#pragma warning disable CS9113 // Parameter is unread.
namespace Clock;

[AttributeUsage(AttributeTargets.Method)]
public class InitializableAttribute(int ordinal = 0): Attribute;