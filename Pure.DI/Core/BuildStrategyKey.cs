namespace Pure.DI.Core
{
    internal class BuildStrategyKey
    {
        private readonly int _id;
        private readonly Dependency _dependency;

        public BuildStrategyKey(int id, Dependency dependency)
        {
            _id = id;
            _dependency = dependency;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            BuildStrategyKey other = (BuildStrategyKey)obj;
            return obj.GetType() == GetType() && _id == other._id && _dependency.Equals(other._dependency);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_id * 397) ^ _dependency.GetHashCode();
            }
        }
    }
}