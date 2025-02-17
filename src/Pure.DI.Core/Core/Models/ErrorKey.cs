// ReSharper disable NotAccessedPositionalProperty.Global
namespace Pure.DI.Core.Models;

[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local")]
internal record ErrorKey(DependencyNode TargetNode, DependencyNode SourceNode);