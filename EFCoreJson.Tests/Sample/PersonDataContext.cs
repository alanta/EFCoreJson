using Microsoft.EntityFrameworkCore;

namespace EFCoreJson.Tests;

public class PersonDataContext : DbContext
{
    public PersonDataContext(DbContextOptions<PersonDataContext> options) : base(options)
    {

    }

    public DbSet<Person> Persons { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Person>().Property(e => e.Addresses).HasJsonConversion<List<Address>>();
    }
}