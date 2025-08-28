namespace EF;

using Microsoft.EntityFrameworkCore;

class PersonsDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Person> Persons { get; private set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Person>()
            .HasMany(i => i.Contacts)
            .WithOne(i => i.Person)
            .HasForeignKey(i => i.PersonId)
            .HasPrincipalKey(i => i.Id);
    }
}