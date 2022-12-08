using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EFCoreJson.Tests;

[Collection(DatabaseCollection.Name)]
public class When_a_field_is_mapped_as_JSON_data
{
    private readonly TestDatabase _database;

    public When_a_field_is_mapped_as_JSON_data(TestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public void It_should_be_possible_to_insert_and_read_data()
    {
        // Arrange
        var context = CreateDataContext();

        context.Persons.Add(new Person
        {
            FirstName = "Frank",
            LastName = "Sharp",
            DateOfBirth = new DateTime(2001, 12, 11),
            Addresses =
            {
                new Address
                {
                    City = "Tilburg",
                    Street = "Schouwburgring",
                    Number = "123b",
                    Type = "Home"
                }
            }
        });

        // Act
        context.SaveChanges();

        // Assert
        var context2 = CreateDataContext();
        context2.Persons.Single(p => p.FirstName == "Frank").Addresses.Single().Should().BeEquivalentTo(new Address
        {
            City = "Tilburg",
            Street = "Schouwburgring",
            Number = "123b",
            Type = "Home"
        });
    }

    [Fact]
    public void It_should_be_possible_to_add_data()
    {
        // Arrange
        var context = CreateDataContext();

        context.Persons.Add(new Person
        {
            FirstName = "Chris",
            LastName = "Sharp",
            DateOfBirth = new DateTime(2001, 12, 11),
            Addresses =
            {
                new Address
                {
                    City = "Tilburg",
                    Street = "Schouwburgring",
                    Number = "123b",
                    Type = "Home"
                }
            }
        });

        context.SaveChanges();

        // Act
        var context2 = CreateDataContext();
        var person = context2.Persons.Single(p => p.FirstName == "Chris");
        person.Addresses.Add(new Address
        {
            City = "Nieuwegein",
            Company = "4DotNet",
            Street = "Nevelgaarde",
            Number = "20f",
            Type = "Work"
        });

        context2.SaveChanges();

        // Assert
        var context3 = CreateDataContext();
        context3.Persons.Single(p => p.FirstName == "Chris").Addresses.Should().BeEquivalentTo(new[]{
            new Address
            {
                City = "Tilburg",
                Street = "Schouwburgring",
                Number = "123b",
                Type = "Home"
            },new Address
            {
                City = "Nieuwegein",
                Company = "4DotNet",
                Street = "Nevelgaarde",
                Number = "20f",
                Type = "Work"
            }});
    }

    [Fact]
    public void It_should_be_possible_to_remove_data()
    {
        // Arrange
        var context = CreateDataContext();

        context.Persons.Add(new Person
        {
            FirstName = "Chris",
            LastName = "Sharp",
            DateOfBirth = new DateTime(2001, 12, 11),
            Addresses =
            {
                new Address
                {
                    City = "Tilburg",
                    Street = "Schouwburgring",
                    Number = "123b",
                    Type = "Home"
                },
                new Address
                {
                    City = "Nieuwegein",
                    Company = "4DotNet",
                    Street = "Nevelgaarde",
                    Number = "20f",
                    Type = "Work"
                }
            }
        });

        context.SaveChanges();

        // Act
        var context2 = CreateDataContext();
        var person = context2.Persons.Single(p => p.FirstName == "Chris");
        person.Addresses = person.Addresses.Where(x => x.Type == "Work").ToList();

        context2.SaveChanges();

        // Assert
        var context3 = CreateDataContext();
        context3.Persons.Single(p => p.FirstName == "Chris").Addresses.Should().BeEquivalentTo(new[]{
            new Address
            {
                City = "Nieuwegein",
                Company = "4DotNet",
                Street = "Nevelgaarde",
                Number = "20f",
                Type = "Work"
            }});
    }


    [Fact]
    public void It_should_be_able_to_query_the_json_data()
    {
        // Arrange
        var context = CreateDataContext();

        context.Persons.Add(new Person
        {
            FirstName = "Billy",
            LastName = "Jean",
            DateOfBirth = new DateTime(2001, 12, 11),
            Addresses =
            {
                new Address
                {
                    City = "Tilburg",
                    Street = "Schouwburgring",
                    Number = "123b",
                    Type = "Home"
                },
                new Address
                {
                    City = "Nieuwegein",
                    Company = "4DotNet",
                    Street = "Nevelgaarde",
                    Number = "20f",
                    Type = "Work"
                }
            }
        });

        context.Persons.Add(new Person
        {
            FirstName = "Alice",
            LastName = "Cooper",
            DateOfBirth = new DateTime(2001, 12, 11),
            Addresses =
            {
                new Address
                {
                    City = "Los Angeles",
                    Street = "Mulholland Drive",
                    Number = "12a",
                    Type = "Home"
                }
            }
        });

        context.SaveChanges();


        // Act
        var context2 = CreateDataContext();
        var result = context2.Persons.FromSqlRaw(@"  WITH addressMatches (PersonId) AS (

	SELECT DISTINCT Id FROM Persons p 
  CROSS APPLY OPENJSON(p.Addresses,N'$') WITH ( AddressType NVARCHAR(50) N'$.Type' )
  WHERE AddressType='Work'
  Group BY Id
  ) 
  SELECT persons.* from persons inner join addressMatches on Id=addressMatches.PersonId").ToArray();


        // Assert
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(x => x.Addresses.Any(a => a.Type == "Work").Should().BeTrue(), because: "Only people with a Work address should be returned");

    }

    private PersonDataContext CreateDataContext()
    {
        var options = new DbContextOptionsBuilder<PersonDataContext>()
            .UseSqlServer(_database.DatabaseConnectionString)
            .Options;
        return new PersonDataContext(options);
    }
}

