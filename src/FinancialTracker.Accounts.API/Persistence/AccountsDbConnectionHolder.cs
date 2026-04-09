using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace FinancialTracker.Accounts.API.Persistence;

/// <summary>
/// Holds a single shared SQLite connection for the in-memory Accounts database
/// so that EnsureCreated and all requests use the same connection and see the same schema/data.
/// </summary>
public sealed class AccountsDbConnectionHolder : IDisposable
{
    private readonly object _lock = new();
    private readonly string _connectionString;
    private SqliteConnection? _connection;

    public AccountsDbConnectionHolder(IConfiguration configuration)
    {
        _connectionString = configuration["ACCOUNTS_DB_CONNECTION_STRING"]
            ?? throw new InvalidOperationException("ACCOUNTS_DB_CONNECTION_STRING must be set in environment variables");
    }

    public SqliteConnection Connection
    {
        get
        {
            if (_connection != null)
                return _connection;
            lock (_lock)
            {
                if (_connection != null)
                    return _connection;
                _connection = new SqliteConnection(_connectionString);
                _connection.Open();
                return _connection;
            }
        }
    }

    public void Dispose() => _connection?.Dispose();
}
