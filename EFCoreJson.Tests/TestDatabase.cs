using System.Data;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Microsoft.Data.SqlClient;

namespace EFCoreJson.Tests;

public class TestDatabase : IAsyncLifetime
{

    private readonly TestcontainerDatabaseConfiguration _configuration = new MsSqlTestcontainerConfiguration
    {
        Password = "yourStrong(!)Password"
    };

    public MsSqlTestcontainer MsSqlContainer { get; }

    public string DatabaseConnectionString { get; private set; } = "";

    public TestDatabase()
    {
        MsSqlContainer = new TestcontainersBuilder<MsSqlTestcontainer>()
            .WithImage("mcr.microsoft.com/mssql/server-2019-latest")
            .WithDatabase(_configuration)
            .Build();
    }

    public async Task InitializeAsync()
    {
        if (!string.IsNullOrWhiteSpace(DatabaseConnectionString))
        {
            return;
        }

        await MsSqlContainer.StartAsync().ConfigureAwait(false);

        DatabaseConnectionString = MsSqlContainer.ConnectionString;

        await InitializeDb();
    }

    public async Task DisposeAsync()
    {
        await MsSqlContainer.DisposeAsync();
    }

    private async Task InitializeDb()
    {
        var builder = new SqlConnectionStringBuilder(DatabaseConnectionString);
        builder.TrustServerCertificate = true;
        DatabaseConnectionString = builder.ToString();

        await ExecuteFiles(Path.GetFullPath("./Sample/Persons.sql"));
    }

    public async Task ExecuteFiles(params string[] sqlFileNames)
    {
        foreach (var file in sqlFileNames)
        {
            try
            {
                File.Exists(file).Should().BeTrue($"Missing file! : {file}");
                var batches = (await File.ReadAllTextAsync(file))
                    .Split("GO", StringSplitOptions.RemoveEmptyEntries);
                foreach (var batch in batches)
                {
                    await Execute(DatabaseConnectionString, batch);
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine(ex.Message);
                throw new DataException($"Error Executing '{file}'.", ex);
            }
        }
    }

    private static async Task Execute(string connectionString, string query)
    {
        await using var connection = new SqlConnection(connectionString);
        connection.Open();
        var cmd = new SqlCommand(query, connection)
        {
            CommandType = CommandType.Text
        };
        await cmd.ExecuteNonQueryAsync();
    }

}

[CollectionDefinition(DatabaseCollection.Name, DisableParallelization = true)]
public class DatabaseCollection : ICollectionFixture<TestDatabase>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
    public const string Name = "Database test";
}