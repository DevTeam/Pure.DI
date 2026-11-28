// ReSharper disable HeapView.BoxingAllocation

namespace Pure.DI.IntegrationTests;

using System.Text;
using Core.Models;

static class GraphExtensions
{
    public static string ConvertToString(this DependencyGraph graph)
    {
        var sb = new StringBuilder();
        // The ordering is culture-invariant to make the rendering independent of the machine, but not ordinal:
        // the expected output of the scenarios lists the edges in a word-like order, where 'int(99)' comes
        // before 'Sample.IDependency'.
        var edgeOrder = StringComparer.InvariantCulture;
        foreach (var node in graph.Graph.Vertices.OrderBy(i => i.Binding.Id).ThenBy(i => i.ToString(), edgeOrder))
        {
            if (sb.Length > 0)
            {
                sb.AppendLine();
            }

            sb.Append(node);
            if (!graph.Graph.TryGetInEdges(node, out var dependencies))
            {
                continue;
            }

            foreach (var dependency in dependencies.OrderBy(i => i.Injection.ToString(), edgeOrder))
            {
                sb.AppendLine();
                sb.Append($"  {(dependency.IsResolved ? '+' : '-')}{dependency}");
            }
        }

        return sb.Replace("\r", "").ToString();
    }
}