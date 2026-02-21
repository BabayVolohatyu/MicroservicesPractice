using Microsoft.Data.Sqlite;

namespace FinancialTracker.Transactions.API.Persistence;

/// <summary>
/// Holds a single shared SQLite connection for the in-memory Transactions database
/// so that EnsureCreated and all requests use the same connection and see the same schema/data.
/// </summary>
public sealed class TransactionsDbConnectionHolder : IDisposable
{
    private readonly object _lock = new();
    private SqliteConnection? _connection;

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
                _connection = new SqliteConnection("Data Source=TransactionsDb;Mode=Memory;Cache=Shared");
                _connection.Open();
                return _connection;
            }
        }
    }

    public void Dispose() => _connection?.Dispose();
}
