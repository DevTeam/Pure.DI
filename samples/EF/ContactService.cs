namespace EF;

class ContactService: IContactService
{
    public string ConvertToString(Contact contact) =>
        contact.PhoneNumber;
}