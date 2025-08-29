using EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var composition = new Composition
{
    ServiceProvider = new ServiceCollection()
        .AddEntityFrameworkInMemoryDatabase()
        .AddDbContext<PersonsDbContext>(options => options.UseInMemoryDatabase("Database of persons"))
        .BuildServiceProvider()
};

var root = composition.Root;
await root.RunAsync();

partial class Program(
    PersonsDbContext db,
    Func<Person> newPerson,
    Func<Contact> newContact)
{
    private async Task RunAsync()
    {
        var nik = newPerson() with
        {
            Name = "Nik",
            Contacts =
            [
                newContact() with { PhoneNumber = "+123456789" }
            ]
        };

        db.Persons.Add(nik);

        var john = newPerson() with
        {
            Name = "John",
            Contacts =
            [
                newContact() with { PhoneNumber = "+777333444" },
                newContact() with { PhoneNumber = "+999888666" }
            ]
        };

        db.Persons.Add(john);

        await db.SaveChangesAsync();
        await db.Persons.ForEachAsync(Console.WriteLine);
    }
}