// ReSharper disable UnusedAutoPropertyAccessor.Local
namespace EF;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

record Person
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; private set; }

    [StringLength(16)]
    public string Name { get; init; } = "";

    public ICollection<Contact> Contacts { get; init; } = [];

    [NotMapped]
    public required IPersonService Service { get; init; }

    public override string ToString() => Service.ConvertToString(this);
}