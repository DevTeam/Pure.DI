#pragma warning disable CS8618
namespace EF;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

record Contact
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; private set; }

    public int PersonId { get; private set; }

    public Person Person { get; set; }

    [StringLength(10)]
    public string PhoneNumber { get; init; } = "";

    [NotMapped]
    public required IContactService Service { get; init; }

    public override string ToString() => Service.ConvertToString(this);
}