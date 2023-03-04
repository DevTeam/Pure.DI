// ReSharper disable UnusedMember.Global
namespace Pure.DI.UsageTests.Models;

internal interface IClock
{
    DateTimeOffset Now { get;  }
}