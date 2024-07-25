namespace Pure.DI.Integration;

public interface IMyGenericService<in T>
{
    public void DoSomething(T value);
}