using System.ComponentModel.DataAnnotations;

namespace EFCoreJson.Tests;

public class Person
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = "";

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = "";

    [Required]
    public DateTime DateOfBirth { get; set; }

    public List<Address> Addresses { get; set; } = new();
}