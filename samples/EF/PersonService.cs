namespace EF;

class PersonService : IPersonService
{
    public string ConvertToString(Person person) =>
        $"Id: {person.Id:0000}, Name: \"{person.Name}\", Contacts: {string.Join(", ", person.Contacts.Select(i => i))}";
}