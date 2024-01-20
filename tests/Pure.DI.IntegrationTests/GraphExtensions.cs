// ReSharper disable HeapView.BoxingAllocation
namespace Pure.DI.IntegrationTests;

using System.Text;
using Core.Models;

internal static class GraphExtensions
{
     public static string ConvertToString(this DependencyGraph graph)
     {
          var sb = new StringBuilder();
          foreach (var node in graph.Graph.Vertices.OrderBy(i => i.Binding.Id).ThenBy(i => i.ToString()))
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

               foreach (var dependency in dependencies.OrderBy(i => i.Injection.ToString()))
               {
                    sb.AppendLine();
                    sb.Append($"  {(dependency.IsResolved ? '+' : '-')}{dependency}");
               }
          }

          return sb.Replace("\r", "").ToString();
     }
}