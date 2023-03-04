namespace Pure.DI;

public interface IGenerator
{
    void Generate(IEnumerable<SyntaxUpdate> updates, CancellationToken cancellationToken);
}