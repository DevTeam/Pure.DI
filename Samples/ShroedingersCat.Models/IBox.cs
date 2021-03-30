namespace ShroedingersCat.Models
{
    public interface IBox<out T>
    {
        T Content { get; }
    }
}
