namespace Clock;

public delegate void Tick();

public interface ITicks
{
    public event Tick Tick;
}