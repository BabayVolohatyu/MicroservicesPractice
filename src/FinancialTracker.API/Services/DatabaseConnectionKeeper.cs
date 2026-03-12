using FinancialTracker.Accounts.Infrastructure.Persistence;
using FinancialTracker.Auth.Infrastructure.Persistence;
using FinancialTracker.Transactions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FinancialTracker.API.Services;

public sealed class DatabaseConnectionKeeper : IDisposable
{
    private readonly AuthDbContext _authDb;
    private readonly AccountsDbContext _accountsDb;
    private readonly TransactionsDbContext _transactionsDb;
    private readonly System.Data.Common.DbConnection _authConnection;
    private readonly System.Data.Common.DbConnection _accountsConnection;
    private readonly System.Data.Common.DbConnection _transactionsConnection;

    public DatabaseConnectionKeeper(
        AuthDbContext authDb,
        AccountsDbContext accountsDb,
        TransactionsDbContext transactionsDb)
    {
        _authDb = authDb;
        _accountsDb = accountsDb;
        _transactionsDb = transactionsDb;

        // Create tables
        _authDb.Database.EnsureCreated();
        _accountsDb.Database.EnsureCreated();
        _transactionsDb.Database.EnsureCreated();

        _authConnection = _authDb.Database.GetDbConnection();
        _accountsConnection = _accountsDb.Database.GetDbConnection();
        _transactionsConnection = _transactionsDb.Database.GetDbConnection();
        
        if (_authConnection.State != System.Data.ConnectionState.Open)
            _authConnection.Open();
        if (_accountsConnection.State != System.Data.ConnectionState.Open)
            _accountsConnection.Open();
        if (_transactionsConnection.State != System.Data.ConnectionState.Open)
            _transactionsConnection.Open();
    }

    public void Dispose()
    {
        _authConnection?.Close();
        _accountsConnection?.Close();
        _transactionsConnection?.Close();
        _authDb?.Dispose();
        _accountsDb?.Dispose();
        _transactionsDb?.Dispose();
    }
}
