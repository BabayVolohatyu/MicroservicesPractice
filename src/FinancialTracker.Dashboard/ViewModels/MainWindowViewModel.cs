using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FinancialTracker.Dashboard.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly string _solutionRoot;
    private readonly string? _dockerCliDir;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private CancellationTokenSource? _healthCts;
    private CancellationTokenSource? _logsCts;
    private bool _authStarted;
    private bool _accountsStarted;
    private bool _transactionsStarted;
    private DateTime _lastLogFetch = DateTime.UtcNow;

    // ===================== Docker Desktop =====================

    [ObservableProperty] private string _dockerStatus = "Unknown";
    [ObservableProperty] private bool _isDockerRunning;

    // ===================== Infrastructure =====================

    [ObservableProperty] private string _kafkaStatus = "Stopped";
    [ObservableProperty] private string _authServiceStatus = "Stopped";
    [ObservableProperty] private string _accountsServiceStatus = "Stopped";
    [ObservableProperty] private string _transactionsServiceStatus = "Stopped";

    [ObservableProperty] private string _authHealth = "";
    [ObservableProperty] private string _accountsHealth = "";
    [ObservableProperty] private string _transactionsHealth = "";

    // ===================== Auth =====================

    [ObservableProperty] private string _regEmail = "user@test.com";
    [ObservableProperty] private string _regPassword = "Password123!";
    [ObservableProperty] private string _regName = "Test User";
    [ObservableProperty] private string _loginEmail = "user@test.com";
    [ObservableProperty] private string _loginPassword = "Password123!";
    [ObservableProperty] private string _jwtToken = "";
    [ObservableProperty] private string _authOutput = "";

    // ===================== Accounts =====================

    [ObservableProperty] private string _accName = "My Account";
    [ObservableProperty] private string _accCurrency = "USD";
    [ObservableProperty] private string _accountsOutput = "";

    // ===================== Transactions =====================

    [ObservableProperty] private string _txAccountId = "";
    [ObservableProperty] private string _txAmount = "500";
    [ObservableProperty] private string _txCategory = "Salary";
    [ObservableProperty] private string _txNote = "";
    [ObservableProperty] private string _txOutput = "";

    // ===================== Reliability =====================

    [ObservableProperty] private string _reliabilityLog = "";

    // ===================== Logs =====================

    [ObservableProperty] private string _infraLog = "";
    [ObservableProperty] private string _authLog = "";
    [ObservableProperty] private string _accountsLog = "";
    [ObservableProperty] private string _transactionsLog = "";

    public MainWindowViewModel()
    {
        _solutionRoot = FindSolutionRoot();
        _dockerCliDir = FindDockerCliDir();
        InfraLog += $"[{DateTime.Now:HH:mm:ss}] Solution root: {_solutionRoot}\n";
        if (_dockerCliDir != null)
            InfraLog += $"[{DateTime.Now:HH:mm:ss}] Docker CLI found: {_dockerCliDir}\n";
        else
            InfraLog += $"[{DateTime.Now:HH:mm:ss}] Docker CLI not found in known paths, relying on system PATH\n";
        if (!File.Exists(Path.Combine(_solutionRoot, "docker-compose.yml")))
            InfraLog += $"[{DateTime.Now:HH:mm:ss}] WARNING: docker-compose.yml not found at solution root\n";
        _ = CheckDockerStatusAsync();
        StartHealthCheckLoop();
        StartLogStreamLoop();
    }

    // ====================================================================
    //  DOCKER DESKTOP DETECTION
    // ====================================================================

    [RelayCommand]
    private async Task CheckDockerStatusAsync()
    {
        DockerStatus = "Checking...";
        LogInfra("Checking Docker status: docker info ...");
        var r = await RunShellAsync("docker", "info --format \"{{.ServerVersion}}\"");
        if (r.Ok)
        {
            var ver = r.Output.Trim();
            DockerStatus = $"Running (v{ver})";
            IsDockerRunning = true;
            LogInfra($"Docker is running, version: {ver}");
        }
        else
        {
            DockerStatus = "Not running — please start Docker Desktop manually";
            IsDockerRunning = false;
            LogInfra($"Docker is NOT running. Exit code: {r.ExitCode}");
            if (r.Output.Contains("500", StringComparison.Ordinal))
                LogInfra("  Docker engine returned 500. Ensure Docker Desktop is set to Linux containers.");
            else
                LogInfra($"  stdout+stderr: {r.Output}");
        }
    }

    private static string? FindDockerCliDir()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return null;

        var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        string[] candidates =
        [
            Path.Combine(pf, "Docker", "Docker", "resources", "bin"),
            @"C:\Program Files\Docker\Docker\resources\bin",
            Path.Combine(localApp, "Docker", "resources", "bin"),
            Path.Combine(localApp, "Programs", "Docker", "Docker", "resources", "bin"),
        ];

        return candidates.FirstOrDefault(d =>
            Directory.Exists(d) && File.Exists(Path.Combine(d, "docker.exe")));
    }

    // ====================================================================
    //  INFRASTRUCTURE COMMANDS
    // ====================================================================

    [RelayCommand]
    private async Task StartKafkaAsync()
    {
        if (!IsDockerRunning)
        {
            KafkaStatus = "Docker not running — start Docker Desktop first";
            LogInfra("Cannot start Kafka: Docker daemon is not running");
            return;
        }
        KafkaStatus = "Starting...";
        LogInfra("Starting Kafka: docker compose up -d kafka kafka-ui ...");
        var r = await RunShellAsync("docker", "compose up -d kafka kafka-ui");
        if (r.Ok)
        {
            KafkaStatus = "Running";
            LogInfra("Kafka started successfully");
            if (!string.IsNullOrWhiteSpace(r.Output))
                LogInfra($"  output: {r.Output}");
        }
        else
        {
            KafkaStatus = "Error (see Infra log)";
            LogInfra($"ERROR starting Kafka. Exit code: {r.ExitCode}");
            LogInfra($"  output: {r.Output}");
        }
    }

    [RelayCommand]
    private async Task StopKafkaAsync()
    {
        KafkaStatus = "Stopping...";
        LogInfra("Stopping Kafka: docker compose stop kafka kafka-ui ...");
        var r = await RunShellAsync("docker", "compose stop kafka kafka-ui");
        if (r.Ok)
        {
            KafkaStatus = "Stopped";
            LogInfra("Kafka stopped successfully");
        }
        else
        {
            KafkaStatus = "Error (see Infra log)";
            LogInfra($"ERROR stopping Kafka. Exit code: {r.ExitCode}");
            LogInfra($"  output: {r.Output}");
        }
    }

    [RelayCommand]
    private async Task StartAuthAsync()
    {
        if (_authStarted) return;
        AuthServiceStatus = "Starting...";
        LogInfra("Starting Auth service container (auth-api) ...");
        var r = await RunShellAsync("docker", "compose up -d --build auth-api");
        if (r.Ok)
        {
            _authStarted = true;
            LogInfra("Auth container launched — waiting for health check on http://localhost:5001");
        }
        else
        {
            AuthServiceStatus = "Error (see Infra log)";
            LogInfra($"ERROR starting Auth container. Exit code: {r.ExitCode}");
            LogInfra($"  output: {r.Output}");
        }
    }

    [RelayCommand]
    private async Task StopAuthAsync()
    {
        LogInfra("Stopping Auth container...");
        var r = await RunShellAsync("docker", "compose stop auth-api");
        _authStarted = false;
        AuthServiceStatus = "Stopped";
        AuthHealth = "";
        LogInfra(r.Ok ? "Auth container stopped" : $"ERROR stopping Auth: {r.Output}");
    }

    [RelayCommand]
    private async Task StartAccountsAsync()
    {
        if (_accountsStarted) return;
        AccountsServiceStatus = "Starting...";
        LogInfra("Starting Accounts service container (accounts-api) ...");
        var r = await RunShellAsync("docker", "compose up -d --build accounts-api");
        if (r.Ok)
        {
            _accountsStarted = true;
            LogInfra("Accounts container launched — waiting for health check on http://localhost:5002");
        }
        else
        {
            AccountsServiceStatus = "Error (see Infra log)";
            LogInfra($"ERROR starting Accounts container. Exit code: {r.ExitCode}");
            LogInfra($"  output: {r.Output}");
        }
    }

    [RelayCommand]
    private async Task StopAccountsAsync()
    {
        LogInfra("Stopping Accounts container...");
        var r = await RunShellAsync("docker", "compose stop accounts-api");
        _accountsStarted = false;
        AccountsServiceStatus = "Stopped";
        AccountsHealth = "";
        LogInfra(r.Ok ? "Accounts container stopped" : $"ERROR stopping Accounts: {r.Output}");
    }

    [RelayCommand]
    private async Task StartTransactionsAsync()
    {
        if (_transactionsStarted) return;
        TransactionsServiceStatus = "Starting...";
        LogInfra("Starting Transactions service container (transactions-api) ...");
        var r = await RunShellAsync("docker", "compose up -d --build transactions-api");
        if (r.Ok)
        {
            _transactionsStarted = true;
            LogInfra("Transactions container launched — waiting for health check on http://localhost:5003");
        }
        else
        {
            TransactionsServiceStatus = "Error (see Infra log)";
            LogInfra($"ERROR starting Transactions container. Exit code: {r.ExitCode}");
            LogInfra($"  output: {r.Output}");
        }
    }

    [RelayCommand]
    private async Task StopTransactionsAsync()
    {
        LogInfra("Stopping Transactions container...");
        var r = await RunShellAsync("docker", "compose stop transactions-api");
        _transactionsStarted = false;
        TransactionsServiceStatus = "Stopped";
        TransactionsHealth = "";
        LogInfra(r.Ok ? "Transactions container stopped" : $"ERROR stopping Transactions: {r.Output}");
    }

    [RelayCommand]
    private async Task StartAllAsync()
    {
        LogInfra("=== START ALL ===");
        if (!IsDockerRunning)
        {
            LogInfra("Docker is not running. Please start Docker Desktop first.");
            return;
        }

        KafkaStatus = "Starting...";
        AuthServiceStatus = "Starting...";
        AccountsServiceStatus = "Starting...";
        TransactionsServiceStatus = "Starting...";

        LogInfra("docker compose up -d --build ...");
        var r = await RunShellAsync("docker", "compose up -d --build");
        if (r.Ok)
        {
            _authStarted = _accountsStarted = _transactionsStarted = true;
            KafkaStatus = "Running";
            LogInfra("All containers launched — waiting for health checks...");
            if (!string.IsNullOrWhiteSpace(r.Output))
                LogInfra($"  output: {r.Output}");
        }
        else
        {
            KafkaStatus = "Error (see Infra log)";
            AuthServiceStatus = "Error (see Infra log)";
            AccountsServiceStatus = "Error (see Infra log)";
            TransactionsServiceStatus = "Error (see Infra log)";
            LogInfra($"ERROR starting containers. Exit code: {r.ExitCode}");
            LogInfra($"  output: {r.Output}");
        }
        LogInfra("=== START ALL complete ===");
    }

    [RelayCommand]
    private async Task StopAllAsync()
    {
        LogInfra("=== STOP ALL ===");
        LogInfra("docker compose down ...");
        var r = await RunShellAsync("docker", "compose down");
        _authStarted = _accountsStarted = _transactionsStarted = false;
        KafkaStatus = "Stopped";
        AuthServiceStatus = "Stopped";
        AccountsServiceStatus = "Stopped";
        TransactionsServiceStatus = "Stopped";
        AuthHealth = "";
        AccountsHealth = "";
        TransactionsHealth = "";
        LogInfra(r.Ok ? "All containers stopped and removed" : $"ERROR: {r.Output}");
        LogInfra("=== STOP ALL complete ===");
    }

    // ====================================================================
    //  HEALTH CHECKS
    // ====================================================================

    private void StartHealthCheckLoop()
    {
        _healthCts = new CancellationTokenSource();
        _ = HealthCheckLoopAsync(_healthCts.Token);
    }

    private async Task HealthCheckLoopAsync(CancellationToken ct)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        while (!ct.IsCancellationRequested)
        {
            try { await Task.Delay(4000, ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }

            if (!IsDockerRunning)
            {
                var r = await RunShellAsync("docker", "info --format \"{{.ServerVersion}}\"");
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (r.Ok)
                    {
                        var ver = r.Output.Trim();
                        DockerStatus = $"Running (v{ver})";
                        IsDockerRunning = true;
                        LogInfra($"Docker is now running, version: {ver}");
                    }
                });
                continue;
            }

            var authOk = await PingHealthAsync(client, "http://localhost:5001/health");
            var accOk = await PingHealthAsync(client, "http://localhost:5002/health");
            var txOk = await PingHealthAsync(client, "http://localhost:5003/health");

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_authStarted)
                {
                    AuthHealth = authOk ? "Healthy" : "Starting...";
                    if (authOk && AuthServiceStatus == "Starting...")
                        AuthServiceStatus = "Running";
                }
                if (_accountsStarted)
                {
                    AccountsHealth = accOk ? "Healthy" : "Starting...";
                    if (accOk && AccountsServiceStatus == "Starting...")
                        AccountsServiceStatus = "Running";
                }
                if (_transactionsStarted)
                {
                    TransactionsHealth = txOk ? "Healthy" : "Starting...";
                    if (txOk && TransactionsServiceStatus == "Starting...")
                        TransactionsServiceStatus = "Running";
                }
            });
        }
    }

    private static async Task<bool> PingHealthAsync(HttpClient client, string url)
    {
        try
        {
            using var resp = await client.GetAsync(url);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    [RelayCommand]
    private async Task RefreshHealthAsync()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        AuthHealth = _authStarted ? (await PingHealthAsync(client, "http://localhost:5001/health") ? "Healthy" : "Unhealthy") : "";
        AccountsHealth = _accountsStarted ? (await PingHealthAsync(client, "http://localhost:5002/health") ? "Healthy" : "Unhealthy") : "";
        TransactionsHealth = _transactionsStarted ? (await PingHealthAsync(client, "http://localhost:5003/health") ? "Healthy" : "Unhealthy") : "";
    }

    // ====================================================================
    //  OPEN IN BROWSER
    // ====================================================================

    [RelayCommand]
    private static void OpenKafkaUi() => OpenUrl("http://localhost:8080");

    [RelayCommand]
    private static void OpenAuthSwagger() => OpenUrl("http://localhost:5001/swagger");

    [RelayCommand]
    private static void OpenAccountsSwagger() => OpenUrl("http://localhost:5002/swagger");

    [RelayCommand]
    private static void OpenTransactionsSwagger() => OpenUrl("http://localhost:5003/swagger");

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch { }
    }

    // ====================================================================
    //  AUTH COMMANDS
    // ====================================================================

    [RelayCommand]
    private async Task RegisterAsync()
    {
        AuthOutput = "Registering...";
        LogInfra($"POST /api/v1/auth/register  email={RegEmail}");
        try
        {
            var resp = await _http.PostAsJsonAsync("http://localhost:5001/api/v1/auth/register",
                new { email = RegEmail, password = RegPassword, name = RegName });
            AuthOutput = await Pretty(resp);
            LogInfra($"  => {(int)resp.StatusCode} {resp.StatusCode}");
        }
        catch (Exception ex)
        {
            AuthOutput = $"Error: {ex.Message}";
            LogInfra($"  ERROR: {ex.Message}");
        }
    }

    /// <summary>
    /// Use the current <see cref="JwtToken"/> value (from login or pasted) as the Bearer token for Accounts/Transactions calls.
    /// </summary>
    [RelayCommand]
    private void ApplyJwtToken()
    {
        ApplyBearerToHttpClient(JwtToken);
        AuthOutput = string.IsNullOrWhiteSpace(NormalizeBearerToken(JwtToken))
            ? "Token cleared — API calls are unauthenticated until you login or paste a token."
            : "Bearer token applied to this session's HTTP client.";
        LogInfra(AuthOutput);
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        AuthOutput = "Logging in...";
        LogInfra($"POST /api/v1/auth/login  email={LoginEmail}");
        try
        {
            var resp = await _http.PostAsJsonAsync("http://localhost:5001/api/v1/auth/login",
                new { email = LoginEmail, password = LoginPassword });
            var body = await resp.Content.ReadAsStringAsync();
            AuthOutput = $"[{(int)resp.StatusCode} {resp.StatusCode}]\n{Fmt(body)}";
            LogInfra($"  => {(int)resp.StatusCode} {resp.StatusCode}");

            if (resp.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("token", out var t))
                {
                    JwtToken = t.GetString() ?? "";
                    ApplyBearerToHttpClient(JwtToken);
                    LogInfra("  JWT token saved for subsequent requests");
                }
            }
        }
        catch (Exception ex)
        {
            AuthOutput = $"Error: {ex.Message}";
            LogInfra($"  ERROR: {ex.Message}");
        }
    }

    // ====================================================================
    //  ACCOUNTS COMMANDS
    // ====================================================================

    [RelayCommand]
    private async Task CreateAccountAsync()
    {
        AccountsOutput = "Creating account...";
        LogInfra($"POST /api/v1/accounts  name={AccName} currency={AccCurrency}");
        try
        {
            var resp = await _http.PostAsJsonAsync("http://localhost:5002/api/v1/accounts",
                new { name = AccName, currency = AccCurrency });
            var body = await resp.Content.ReadAsStringAsync();
            AccountsOutput = $"[{(int)resp.StatusCode} {resp.StatusCode}]\n{Fmt(body)}";
            LogInfra($"  => {(int)resp.StatusCode} {resp.StatusCode}");

            if (resp.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("accountId", out var id))
                {
                    TxAccountId = id.GetString() ?? "";
                    LogInfra($"  AccountId auto-filled: {TxAccountId}");
                }
            }
        }
        catch (Exception ex)
        {
            AccountsOutput = $"Error: {ex.Message}";
            LogInfra($"  ERROR: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ListAccountsAsync()
    {
        AccountsOutput = "Loading accounts...";
        LogInfra("GET /api/v1/accounts");
        try
        {
            var resp = await _http.GetAsync("http://localhost:5002/api/v1/accounts");
            AccountsOutput = await Pretty(resp);
            LogInfra($"  => {(int)resp.StatusCode} {resp.StatusCode}");
        }
        catch (Exception ex)
        {
            AccountsOutput = $"Error: {ex.Message}";
            LogInfra($"  ERROR: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task GetBalanceAsync()
    {
        if (string.IsNullOrWhiteSpace(TxAccountId))
        {
            AccountsOutput = "Enter an Account ID first (create an account or use one from the list).";
            return;
        }
        AccountsOutput = "Fetching balance...";
        LogInfra($"GET /api/v1/accounts/{TxAccountId}");
        try
        {
            var resp = await _http.GetAsync($"http://localhost:5002/api/v1/accounts/{TxAccountId}");
            AccountsOutput = await Pretty(resp);
            LogInfra($"  => {(int)resp.StatusCode} {resp.StatusCode}");
        }
        catch (Exception ex)
        {
            AccountsOutput = $"Error: {ex.Message}";
            LogInfra($"  ERROR: {ex.Message}");
        }
    }

    // ====================================================================
    //  TRANSACTIONS COMMANDS
    // ====================================================================

    [RelayCommand]
    private async Task AddIncomeAsync()
    {
        TxOutput = "Adding income...";
        LogInfra($"POST /api/v1/transactions/income  accountId={TxAccountId} amount={TxAmount}");
        try
        {
            var resp = await _http.PostAsJsonAsync("http://localhost:5003/api/v1/transactions/income",
                new { accountId = TxAccountId, amount = decimal.Parse(TxAmount), category = NullIfEmpty(TxCategory), note = NullIfEmpty(TxNote) });
            TxOutput = await Pretty(resp);
            LogInfra($"  => {(int)resp.StatusCode} {resp.StatusCode}");
        }
        catch (Exception ex)
        {
            TxOutput = $"Error: {ex.Message}";
            LogInfra($"  ERROR: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task AddExpenseAsync()
    {
        TxOutput = "Adding expense...";
        LogInfra($"POST /api/v1/transactions/expense  accountId={TxAccountId} amount={TxAmount}");
        try
        {
            var resp = await _http.PostAsJsonAsync("http://localhost:5003/api/v1/transactions/expense",
                new { accountId = TxAccountId, amount = decimal.Parse(TxAmount), category = NullIfEmpty(TxCategory), note = NullIfEmpty(TxNote) });
            TxOutput = await Pretty(resp);
            LogInfra($"  => {(int)resp.StatusCode} {resp.StatusCode}");
        }
        catch (Exception ex)
        {
            TxOutput = $"Error: {ex.Message}";
            LogInfra($"  ERROR: {ex.Message}");
        }
    }

    // ====================================================================
    //  RELIABILITY TEST COMMANDS
    // ====================================================================

    [RelayCommand]
    private async Task RtStopKafkaAsync()
    {
        Log("=== STEP 1: Stopping Kafka broker ===");
        LogInfra("[Reliability] Stopping Kafka: docker compose stop kafka");
        var r = await RunShellAsync("docker", "compose stop kafka");
        Log(r.Ok ? "Kafka stopped successfully." : $"Error (exit {r.ExitCode}): {r.Output}");
        LogInfra(r.Ok ? "  Kafka stopped OK" : $"  ERROR exit {r.ExitCode}: {r.Output}");
        KafkaStatus = r.Ok ? "Stopped" : KafkaStatus;
        Log("Outbox Relay will fail to publish — events will accumulate in OutboxMessages table.\n");
    }

    [RelayCommand]
    private async Task RtCreateTransactionAsync()
    {
        Log($"=== STEP 2: Creating transaction (Kafka: {KafkaStatus}) ===");
        LogInfra("[Reliability] POST income for outbox / Kafka test...");
        if (string.IsNullOrWhiteSpace(TxAccountId))
        {
            Log("Set Account ID first — log in, use Create account (auto-fills ID) or List accounts.\n");
            return;
        }

        try
        {
            var resp = await _http.PostAsJsonAsync("http://localhost:5003/api/v1/transactions/income",
                new { accountId = TxAccountId, amount = decimal.Parse(TxAmount), category = "ReliabilityTest", note = "Created while Kafka is down" });
            var body = await resp.Content.ReadAsStringAsync();
            Log($"Response: [{(int)resp.StatusCode} {resp.StatusCode}]");
            Log(Fmt(body));
            LogInfra($"  => {(int)resp.StatusCode} {resp.StatusCode}");

            if (resp.StatusCode == HttpStatusCode.Created)
            {
                Log("Transaction saved to DB + OutboxMessage saved atomically.");
                if (KafkaStatus == "Stopped")
                    Log("Check Transactions service logs — Relay will report 'Failed to publish'.\n");
                else
                    Log("Kafka is up — Relay should publish shortly; check Transactions and Accounts logs.\n");
            }
            else
            {
                Log("Nothing was saved — the API rejected the request (no transaction, no outbox row).");
                if (resp.StatusCode == HttpStatusCode.NotFound)
                    Log("404 here means the account is missing or not yours: log in, create an account, and use that Account ID.\n");
                else if (resp.StatusCode == HttpStatusCode.Unauthorized)
                    Log("Log in first so the Transactions API receives a valid JWT.\n");
                else
                    Log("\n");
            }
        }
        catch (Exception ex)
        {
            Log($"Error: {ex.Message}\n");
            LogInfra($"  ERROR: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task RtStartKafkaAsync()
    {
        Log("=== STEP 3: Starting Kafka broker ===");
        LogInfra("[Reliability] Starting Kafka: docker compose start kafka");
        var r = await RunShellAsync("docker", "compose start kafka");
        Log(r.Ok ? "Kafka started successfully." : $"Error (exit {r.ExitCode}): {r.Output}");
        LogInfra(r.Ok ? "  Kafka started OK" : $"  ERROR exit {r.ExitCode}: {r.Output}");
        KafkaStatus = r.Ok ? "Running" : KafkaStatus;
        Log("Outbox Relay will pick up pending messages within ~5 seconds.");
        Log("Check Transactions logs: 'Published outbox message ... to Kafka'");
        Log("Check Accounts logs:     'Processing TransactionCreated event'\n");
    }

    [RelayCommand]
    private async Task RtCheckBalanceAsync()
    {
        Log("=== STEP 4: Verifying balance was updated via event ===");
        LogInfra("[Reliability] Checking balance...");
        try
        {
            var resp = await _http.GetAsync("http://localhost:5002/api/v1/accounts");
            var body = await resp.Content.ReadAsStringAsync();
            Log(Fmt(body));
            LogInfra($"  => {(int)resp.StatusCode} {resp.StatusCode}");
            Log("Balance should reflect the transaction that was created while Kafka was down.\n");
        }
        catch (Exception ex)
        {
            Log($"Error: {ex.Message}\n");
            LogInfra($"  ERROR: {ex.Message}");
        }
    }

    [RelayCommand]
    private void RtClear() => ReliabilityLog = "";

    [RelayCommand]
    private void ClearInfraLog() => InfraLog = "";

    // ====================================================================
    //  DOCKER LOG STREAMING
    // ====================================================================

    private void StartLogStreamLoop()
    {
        _logsCts = new CancellationTokenSource();
        _ = LogStreamLoopAsync(_logsCts.Token);
    }

    private async Task LogStreamLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { await Task.Delay(3000, ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }

            if (!IsDockerRunning) continue;

            var since = _lastLogFetch.ToString("yyyy-MM-ddTHH:mm:ssZ");
            _lastLogFetch = DateTime.UtcNow;

            await FetchServiceLogsAsync("auth-api", "Auth", _authStarted, since);
            await FetchServiceLogsAsync("accounts-api", "Accounts", _accountsStarted, since);
            await FetchServiceLogsAsync("transactions-api", "Transactions", _transactionsStarted, since);
        }
    }

    private async Task FetchServiceLogsAsync(string serviceName, string label, bool isStarted, string since)
    {
        if (!isStarted) return;
        var r = await RunShellAsync("docker", $"compose logs --no-log-prefix --since {since} {serviceName}");
        if (!r.Ok || string.IsNullOrWhiteSpace(r.Output)) return;
        foreach (var line in r.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            AppendSvcLog(label, line);
    }

    // ====================================================================
    //  SHELL HELPERS
    // ====================================================================

    private async Task<(bool Ok, int ExitCode, string Output)> RunShellAsync(string file, string args)
    {
        var psi = new ProcessStartInfo
        {
            WorkingDirectory = _solutionRoot,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            psi.FileName = "cmd.exe";
            psi.Arguments = $"/c {file} {args}";
        }
        else
        {
            psi.FileName = "/bin/sh";
            psi.Arguments = $"-c \"{file} {args}\"";
        }

        AugmentPathWithDocker(psi);

        var sb = new StringBuilder();
        try
        {
            var proc = new Process { StartInfo = psi };
            proc.OutputDataReceived += (_, e) => { if (e.Data != null) sb.AppendLine(e.Data); };
            proc.ErrorDataReceived += (_, e) => { if (e.Data != null) sb.AppendLine(e.Data); };
            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            await proc.WaitForExitAsync();
            return (proc.ExitCode == 0, proc.ExitCode, sb.ToString().Trim());
        }
        catch (Exception ex)
        {
            return (false, -1, $"Failed to run '{file} {args}': {ex.Message}");
        }
    }

    private void AugmentPathWithDocker(ProcessStartInfo psi)
    {
        if (_dockerCliDir == null) return;
        var curPath = psi.Environment.TryGetValue("PATH", out var p) ? p ?? "" : "";
        if (!curPath.Contains(_dockerCliDir, StringComparison.OrdinalIgnoreCase))
            psi.Environment["PATH"] = _dockerCliDir + Path.PathSeparator + curPath;
    }

    // ====================================================================
    //  UI HELPERS
    // ====================================================================

    private void LogInfra(string msg)
    {
        var entry = $"[{DateTime.Now:HH:mm:ss}] {msg}\n";
        if (Dispatcher.UIThread.CheckAccess())
            InfraLog = TrimLog(InfraLog + entry);
        else
            Dispatcher.UIThread.Post(() => InfraLog = TrimLog(InfraLog + entry));
    }

    private void AppendSvcLog(string label, string line)
    {
        var entry = $"[{DateTime.Now:HH:mm:ss}] {line}\n";
        Dispatcher.UIThread.Post(() =>
        {
            switch (label)
            {
                case "Auth": AuthLog = TrimLog(AuthLog + entry); break;
                case "Accounts": AccountsLog = TrimLog(AccountsLog + entry); break;
                case "Transactions": TransactionsLog = TrimLog(TransactionsLog + entry); break;
            }
        });
    }

    private static string TrimLog(string log) =>
        log.Length > 80_000 ? log[^60_000..] : log;

    private void Log(string msg) => ReliabilityLog += $"[{DateTime.Now:HH:mm:ss}] {msg}\n";

    private static async Task<string> Pretty(HttpResponseMessage resp)
    {
        var body = await resp.Content.ReadAsStringAsync();
        return $"[{(int)resp.StatusCode} {resp.StatusCode}]\n{Fmt(body)}";
    }

    private static string Fmt(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        }
        catch { return json; }
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    /// <summary>
    /// Strips a leading "Bearer " prefix if present so we never send "Bearer Bearer …".
    /// </summary>
    private static string NormalizeBearerToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return "";
        var t = token.Trim();
        if (t.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            t = t["Bearer ".Length..].Trim();
        return t;
    }

    private void ApplyBearerToHttpClient(string? token)
    {
        var raw = NormalizeBearerToken(token);
        _http.DefaultRequestHeaders.Remove("Authorization");
        if (string.IsNullOrEmpty(raw))
            return;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", raw);
    }

    private static string FindSolutionRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "FinancialTracker.sln")))
                return dir;
            dir = Path.GetDirectoryName(dir);
        }
        return Directory.GetCurrentDirectory();
    }

    public void Cleanup()
    {
        _healthCts?.Cancel();
        _healthCts?.Dispose();
        _logsCts?.Cancel();
        _logsCts?.Dispose();
        _http.Dispose();
    }
}
