using Microsoft.Data.Sqlite;

namespace CageLogic.Infrastructure.Progression;

/// <summary>Creates connections to the app-private progression database.</summary>
public sealed class SqliteConnectionFactory
{
	private readonly string _connectionString;

	public SqliteConnectionFactory(string databasePath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

		_connectionString = new SqliteConnectionStringBuilder
		{
			DataSource = Path.GetFullPath(databasePath),
			Mode = SqliteOpenMode.ReadWriteCreate,
			Cache = SqliteCacheMode.Shared
		}.ToString();
	}

	public SqliteConnection CreateConnection() => new(_connectionString);
}
