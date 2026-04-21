using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PGLLMS.Admin.Application.Interfaces;

namespace PGLLMS.Admin.Infrastructure.Storage;

/// <summary>
/// Implements <see cref="IOneDriveService"/> using the Microsoft Graph REST API with
/// client-credentials OAuth2. Supports files of any size via upload sessions (≤250 MB).
/// </summary>
public sealed class OneDriveService : IOneDriveService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly OneDriveSettings _settings;
    private readonly ILogger<OneDriveService> _logger;

    // Simple in-memory token cache — fine for a single server process.
    private string? _cachedToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    // Cached drive id for the configured user.
    private string? _driveId;
    private readonly SemaphoreSlim _driveLock = new(1, 1);

    private const int ChunkSize = 5 * 1024 * 1024; // 5 MB upload chunks

    public OneDriveService(
        IHttpClientFactory httpFactory,
        IOptions<OneDriveSettings> settings,
        ILogger<OneDriveService> logger)
    {
        _httpFactory = httpFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public async Task<string> UploadFileAsync(
        string remotePath,
        Stream content,
        string contentType = "application/pdf",
        CancellationToken ct = default)
    {
        var driveId = await GetDriveIdAsync(ct);
        var fullPath = BuildFullPath(remotePath);

        _logger.LogInformation("Uploading to OneDrive: {Path}", fullPath);

        var token = await GetAccessTokenAsync(ct);
        using var client = CreateGraphClient(token);

        // Ensure all intermediate folders exist (required for OneDrive for Business).
        await EnsureFolderHierarchyAsync(client, driveId, fullPath, ct);

        // Always use an upload session — works for all file sizes.
        var escapedPath = EscapeGraphPath(fullPath);
        var sessionUrl = $"https://graph.microsoft.com/v1.0/drives/{driveId}/root:{escapedPath}:/createUploadSession";

        var sessionBody = new
        {
            item = new
            {
                name = Path.GetFileName(fullPath),
                conflictBehavior = "replace"
            }
        };

        using var sessionReq = new HttpRequestMessage(HttpMethod.Post, sessionUrl);
        sessionReq.Content = JsonContent(sessionBody);
        using var sessionResp = await client.SendAsync(sessionReq, ct);

        if (!sessionResp.IsSuccessStatusCode)
        {
            var err = await sessionResp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Failed to create OneDrive upload session: {sessionResp.StatusCode} — {err}");
        }

        var sessionJson = await sessionResp.Content.ReadFromJsonAsync<UploadSessionResponse>(cancellationToken: ct);
        var uploadUrl = sessionJson?.UploadUrl
            ?? throw new InvalidOperationException("OneDrive upload session did not return an uploadUrl.");

        // Upload in chunks
        long totalLength = content.Length;
        long offset = 0;
        var buffer = new byte[ChunkSize];

        while (offset < totalLength)
        {
            int bytesRead = await content.ReadAtLeastAsync(buffer, 1, throwOnEndOfStream: false, ct);
            if (bytesRead == 0) break;

            using var chunkReq = new HttpRequestMessage(HttpMethod.Put, uploadUrl);
            chunkReq.Content = new ByteArrayContent(buffer, 0, bytesRead);
            chunkReq.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            chunkReq.Content.Headers.ContentRange =
                new ContentRangeHeaderValue(offset, offset + bytesRead - 1, totalLength);

            using var chunkResp = await client.SendAsync(chunkReq, ct);

            // 200/201 = done, 202 = continue
            if (!chunkResp.IsSuccessStatusCode)
            {
                var err = await chunkResp.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException($"OneDrive chunk upload failed at offset {offset}: {chunkResp.StatusCode} — {err}");
            }

            offset += bytesRead;
        }

        _logger.LogInformation("OneDrive upload complete: {Path}", fullPath);
        return remotePath;
    }

    public async Task<string?> GetDownloadUrlAsync(string remotePath, CancellationToken ct = default)
    {
        var driveId = await GetDriveIdAsync(ct);
        var fullPath = BuildFullPath(remotePath);
        var token = await GetAccessTokenAsync(ct);
        using var client = CreateGraphClient(token);

        var url = $"https://graph.microsoft.com/v1.0/drives/{driveId}/root:{EscapeGraphPath(fullPath)}";
        using var resp = await client.GetAsync(url, ct);

        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        resp.EnsureSuccessStatusCode();

        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        if (doc.RootElement.TryGetProperty("@microsoft.graph.downloadUrl", out var urlProp))
            return urlProp.GetString();

        return null;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Encodes a Graph API item path, escaping each segment individually
    /// so that '/' folder separators are preserved (not encoded as %2F).
    /// </summary>
    private static string EscapeGraphPath(string path)
        => string.Join("/", path.Split('/').Select(Uri.EscapeDataString));

    /// <summary>
    /// Ensures every folder in the path hierarchy exists in OneDrive for Business.
    /// OneDrive for Business does NOT auto-create parent folders during upload,
    /// so we must create them top-down before uploading a file.
    /// </summary>
    private async Task EnsureFolderHierarchyAsync(
        HttpClient client,
        string driveId,
        string fullPath,
        CancellationToken ct)
    {
        // fullPath is like /PGLLMS/F1/F1-1/filename.pdf
        // We need to create /PGLLMS, /PGLLMS/F1, /PGLLMS/F1/F1-1 in order.
        var segments = fullPath.Trim('/').Split('/');

        // Skip the last segment (the filename)
        for (int depth = 1; depth < segments.Length; depth++)
        {
            var folderPath = "/" + string.Join("/", segments.Take(depth));

            string url;
            if (depth == 1)
            {
                // Top-level: create under root
                url = $"https://graph.microsoft.com/v1.0/drives/{driveId}/root/children";
            }
            else
            {
                // Nested: create under the parent folder
                var parentPath = "/" + string.Join("/", segments.Take(depth - 1));
                url = $"https://graph.microsoft.com/v1.0/drives/{driveId}/root:{EscapeGraphPath(parentPath)}:/children";
            }

            var body = new
            {
                name = segments[depth - 1],
                folder = new { },
                // fail = return 409 if already exists (we'll ignore it)
                conflictBehavior = "fail"
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Content = JsonContent(body);

            using var resp = await client.SendAsync(req, ct);

            if (resp.IsSuccessStatusCode)
            {
                _logger.LogInformation("Created OneDrive folder: {Path}", folderPath);
            }
            else if (resp.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                // 409 means the folder already exists — that's fine
                _logger.LogDebug("OneDrive folder already exists: {Path}", folderPath);
            }
            else
            {
                var err = await resp.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException(
                    $"Failed to create OneDrive folder '{folderPath}': {resp.StatusCode} — {err}");
            }
        }
    }

    private string BuildFullPath(string remotePath)
    {
        var root = _settings.RootFolder.Trim('/');
        var rel = remotePath.TrimStart('/');
        return root.Length > 0 ? $"/{root}/{rel}" : $"/{rel}";
    }

    private HttpClient CreateGraphClient(string token)
    {
        var client = _httpFactory.CreateClient("MsGraph");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        await _tokenLock.WaitAsync(ct);
        try
        {
            if (_cachedToken is not null && DateTimeOffset.UtcNow < _tokenExpiry)
                return _cachedToken;

            using var client = _httpFactory.CreateClient();
            var body = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"]    = "client_credentials",
                ["client_id"]     = _settings.ClientId,
                ["client_secret"] = _settings.ClientSecret,
                ["scope"]         = "https://graph.microsoft.com/.default",
            });

            using var resp = await client.PostAsync(
                $"https://login.microsoftonline.com/{_settings.TenantId}/oauth2/v2.0/token",
                body, ct);

            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct);
            _cachedToken = json!.AccessToken;
            _tokenExpiry = DateTimeOffset.UtcNow.AddSeconds(json.ExpiresIn - 60); // 1-min buffer
            return _cachedToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task<string> GetDriveIdAsync(CancellationToken ct)
    {
        await _driveLock.WaitAsync(ct);
        try
        {
            if (_driveId is not null) return _driveId;

            var token = await GetAccessTokenAsync(ct);
            using var client = CreateGraphClient(token);

            using var resp = await client.GetAsync(
                $"https://graph.microsoft.com/v1.0/users/{Uri.EscapeDataString(_settings.UserEmail)}/drive", ct);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogError("Graph API failed to get user drive for {Email}: {Status} — {Body}",
                    _settings.UserEmail, resp.StatusCode, body);
                resp.EnsureSuccessStatusCode(); // throws
            }

            var json = await resp.Content.ReadFromJsonAsync<DriveResponse>(cancellationToken: ct);
            _driveId = json!.Id;
            return _driveId;
        }
        finally
        {
            _driveLock.Release();
        }
    }

    private static HttpContent JsonContent<T>(T value) =>
        new StringContent(
            JsonSerializer.Serialize(value),
            System.Text.Encoding.UTF8,
            "application/json");

    // ── Private response DTOs ─────────────────────────────────────────────────

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; set; } = default!;
        [JsonPropertyName("expires_in")]   public int ExpiresIn { get; set; }
    }

    private sealed class DriveResponse
    {
        [JsonPropertyName("id")] public string Id { get; set; } = default!;
    }

    private sealed class UploadSessionResponse
    {
        [JsonPropertyName("uploadUrl")] public string? UploadUrl { get; set; }
    }
}
