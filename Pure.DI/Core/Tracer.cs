namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class Tracer : ITracer, IDisposable
    {
        private readonly ILog<Tracer> _log;
        private readonly Stack<Dependency> _path = new();
        private readonly List<Dependency[]> _paths = new();

        public Tracer(ILog<Tracer> log) => _log = log;

        public IEnumerable<Dependency[]> Paths => _paths;

        public IDisposable RegisterResolving(Dependency dependency)
        {
            if (!_path.Contains(dependency))
            {
                _path.Push(dependency);
                _log.Trace(GetMessages);
                return this;
            }

            var path = string.Join(" -> ", _path.Reverse().Select(i => i.Equals(dependency) ? $"[{i}]": i.ToString()));
            throw new BuildException(Diagnostics.Error.CircularDependency, $"Circular dependency detected for {path} ---> [{dependency}].", dependency.Binding.Location);
        }

        public void Save()
        {
            _paths
                .Where(path => Eq(_path, path))
                .Where(path => path.Length < _path.Count)
                .ToList()
                .ForEach(i => _paths.Remove(i));

            _paths.Add(_path.ToArray());
        }
        
        private static bool Eq(IEnumerable<Dependency> seq1, IEnumerable<Dependency> seq2)
        {
            using var enumerator1 = seq1.GetEnumerator();
            using var enumerator2 = seq2.GetEnumerator();
            while (enumerator1.MoveNext() && enumerator2.MoveNext())
            {
                if (!Equals(enumerator1.Current.Binding.Id, enumerator2.Current.Binding.Id))
                {
                    return false;
                }
            }

            return true;
        }
        
        public void Reset() => _paths.Clear();

        void IDisposable.Dispose()
        {
            if (_path.Count == 0)
            {
                _log.Warning("The path is empty.");
                return;
            }

            _path.Pop();
        }

        public override string ToString() => 
            string.Join(" -> ", _path.Reverse().Select(i => i.ToString()));

        private string[] GetMessages()
        {
            List<string> messages = new();
            var offset = 0;
            foreach (var item in _path)
            {
                messages.Add($"{new string(' ', offset)}{item}");
                offset += 2;
            }

            return messages.ToArray();
        }
    }
}