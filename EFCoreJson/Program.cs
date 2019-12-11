using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace EFCoreJson
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new DbContextOptionsBuilder<PersonDataContext>().UseSqlServer("Data Source=.;Initial Catalog=JsonData;Persist Security Info=True;User ID=me;Password=topsecret;Connection Timeout=30;").Options;
            var context = new PersonDataContext(options);

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


            var context2 = new PersonDataContext(options);
            var person = context2.Persons.FirstOrDefault(p => p.FirstName == "Chris");
            person.Addresses.Add(new Address
            {
                City = "Nieuwegein",
                Company = "4DotNet",
                Street = "Nevelgaarde",
                Number = "20f",
                Type = "Work"
            });

            context2.SaveChanges();

            person.Addresses = person.Addresses.Where(x => x.Type == "Work").ToList();

            context2.SaveChanges();


            //var work = context2.Persons.FirstOrDefault(p => p.Addresses.Any(a => a.Type == "Work"));

            var result = context2.Persons.FromSqlRaw(@"  WITH addressMatches (PersonId) AS (

	SELECT DISTINCT Id FROM Persons p 
  CROSS APPLY OPENJSON(p.Addresses,N'$') WITH ( AddressType NVARCHAR(50) N'$.Type' )
  WHERE AddressType='Work'
  Group BY Id
  ) 
  SELECT persons.* from persons inner join addressMatches on Id=addressMatches.PersonId").ToArray();

            Console.WriteLine($"Found {result.Count()} matches");
        }
    }

    public class PersonDataContext : DbContext
    {
        public PersonDataContext(DbContextOptions<PersonDataContext> options) : base(options)
        {
            
        }
        public DbSet<Person> Persons { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Person>().Property(e => e.Addresses).HasJsonConversion<List<Address>>();
        }
    }

    public class Person
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        public List<Address> Addresses { get; set; } = new List<Address>();
    }

    public class Address
    {
        public string Type { get; set; }
        public string Company { get; set; }
        public string Number { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
    }

    public static class ValueConversionExtensions
    {
        public static PropertyBuilder<T> HasJsonConversion<T>(this PropertyBuilder<T> propertyBuilder) where T : class, new()
        {
            ValueConverter<T, string> converter = new ValueConverter<T, string>
            (
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<T>(v) ?? new T()
            );

            ValueComparer<T> comparer = new ValueComparer<T>
            (
                (l, r) => JsonConvert.SerializeObject(l) == JsonConvert.SerializeObject(r),
                v => v == null ? 0 : JsonConvert.SerializeObject(v).GetHashCode(),
                v => JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(v))
            );

            propertyBuilder.HasConversion(converter);
            propertyBuilder.Metadata.SetValueConverter(converter);
            propertyBuilder.Metadata.SetValueComparer(comparer);
            propertyBuilder.HasColumnType("jsonb");

            return propertyBuilder;
        }
    }
}
