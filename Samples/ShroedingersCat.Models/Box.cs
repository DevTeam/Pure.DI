namespace ShroedingersCat.Models
{
    public class Box<T> : IBox<T>
    {
        public Box(T content) => Content = content;

        public T Content { get; }

        public override string ToString() => $"[{Content}]";
    }
}