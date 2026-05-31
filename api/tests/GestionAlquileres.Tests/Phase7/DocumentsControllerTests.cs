using System.Net;
using System.Net.Http.Json;
using System.Text;
using Xunit;

namespace GestionAlquileres.Tests.Phase7;

public class DocumentsControllerTests : IClassFixture<Phase7ApiFactory>
{
    private readonly Phase7ApiFactory _factory;
    public DocumentsControllerTests(Phase7ApiFactory factory) => _factory = factory;

    // T1 — list documents on empty contract returns 200 empty array
    [Fact]
    public async Task T1_List_EmptyContract_Returns200EmptyArray()
    {
        var (contractId, client) = await _factory.SetupContractAsync("doc-t1");
        var resp = await client.GetAsync($"/api/v1/contracts/{contractId}/documents");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var docs = await resp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(0, docs.GetArrayLength());
    }

    // T2 — upload requires auth (no token → 401)
    [Fact]
    public async Task T2_Upload_NoAuth_Returns401()
    {
        var (contractId, _) = await _factory.SetupContractAsync("doc-t2");
        var anonClient = _factory.CreateClient();
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("hello"), "file", "test.txt");
        var resp = await anonClient.PostAsync($"/api/v1/contracts/{contractId}/documents", content);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // T3 — upload a file, returns 201 with document data
    [Fact]
    public async Task T3_Upload_ValidFile_Returns201()
    {
        var (contractId, client) = await _factory.SetupContractAsync("doc-t3");

        using var content = new MultipartFormDataContent();
        var fileBytes = Encoding.UTF8.GetBytes("Este es el contrato firmado.");
        content.Add(new ByteArrayContent(fileBytes) { Headers = { { "Content-Type", "application/pdf" } } },
            "file", "contrato.pdf");

        var resp = await client.PostAsync($"/api/v1/contracts/{contractId}/documents", content);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var doc = await resp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.NotEqual(Guid.Empty, doc.GetProperty("id").GetGuid());
        Assert.Equal("contrato.pdf", doc.GetProperty("fileName").GetString());
        Assert.Equal("application/pdf", doc.GetProperty("mimeType").GetString());
    }

    // T4 — list after upload returns 1 document
    [Fact]
    public async Task T4_ListAfterUpload_Returns1Document()
    {
        var (contractId, client) = await _factory.SetupContractAsync("doc-t4");

        using var content = new MultipartFormDataContent();
        var fileBytes = Encoding.UTF8.GetBytes("recibo de pago");
        content.Add(new ByteArrayContent(fileBytes) { Headers = { { "Content-Type", "application/pdf" } } },
            "file", "recibo.pdf");
        (await client.PostAsync($"/api/v1/contracts/{contractId}/documents", content)).EnsureSuccessStatusCode();

        var listResp = await client.GetAsync($"/api/v1/contracts/{contractId}/documents");
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var docs = await listResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(1, docs.GetArrayLength());
    }

    // T5 — get download URL returns token + url
    [Fact]
    public async Task T5_GetDownloadUrl_Returns200WithToken()
    {
        var (contractId, client) = await _factory.SetupContractAsync("doc-t5");

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("doc content")) { Headers = { { "Content-Type", "text/plain" } } },
            "file", "nota.txt");
        var uploadResp = await client.PostAsync($"/api/v1/contracts/{contractId}/documents", content);
        uploadResp.EnsureSuccessStatusCode();
        var uploaded = await uploadResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var docId = uploaded.GetProperty("id").GetGuid();

        var urlResp = await client.GetAsync($"/api/v1/contracts/{contractId}/documents/{docId}/download-url");
        Assert.Equal(HttpStatusCode.OK, urlResp.StatusCode);

        var urlDto = await urlResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.False(string.IsNullOrEmpty(urlDto.GetProperty("token").GetString()));
        Assert.Contains("/api/v1/files/download", urlDto.GetProperty("url").GetString()!);
    }

    // T6 — download via presigned token streams the file
    [Fact]
    public async Task T6_Download_ValidToken_StreamsFile()
    {
        var (contractId, client) = await _factory.SetupContractAsync("doc-t6");
        var fileContent = "contenido del archivo de prueba";
        var fileBytes = Encoding.UTF8.GetBytes(fileContent);

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(fileBytes) { Headers = { { "Content-Type", "text/plain" } } },
            "file", "test.txt");
        var uploadResp = await client.PostAsync($"/api/v1/contracts/{contractId}/documents", content);
        uploadResp.EnsureSuccessStatusCode();
        var uploaded = await uploadResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var docId = uploaded.GetProperty("id").GetGuid();

        var urlResp = await client.GetAsync($"/api/v1/contracts/{contractId}/documents/{docId}/download-url");
        urlResp.EnsureSuccessStatusCode();
        var urlDto = await urlResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var token = urlDto.GetProperty("token").GetString()!;

        // Download without auth — token is the auth
        var anonClient = _factory.CreateClient();
        var downloadResp = await anonClient.GetAsync($"/api/v1/files/download?token={token}");
        Assert.Equal(HttpStatusCode.OK, downloadResp.StatusCode);

        var downloaded = await downloadResp.Content.ReadAsStringAsync();
        Assert.Equal(fileContent, downloaded);
    }

    // T7 — download with invalid token returns 401
    [Fact]
    public async Task T7_Download_InvalidToken_Returns401()
    {
        var anonClient = _factory.CreateClient();
        var resp = await anonClient.GetAsync("/api/v1/files/download?token=invalid.token");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // T8 — download with missing token returns 401
    [Fact]
    public async Task T8_Download_MissingToken_Returns401()
    {
        var anonClient = _factory.CreateClient();
        var resp = await anonClient.GetAsync("/api/v1/files/download?token=");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // T9 — delete document removes it from list
    [Fact]
    public async Task T9_Delete_RemovesFromList()
    {
        var (contractId, client) = await _factory.SetupContractAsync("doc-t9");

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("to delete")) { Headers = { { "Content-Type", "text/plain" } } },
            "file", "borrar.txt");
        var uploadResp = await client.PostAsync($"/api/v1/contracts/{contractId}/documents", content);
        uploadResp.EnsureSuccessStatusCode();
        var uploaded = await uploadResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var docId = uploaded.GetProperty("id").GetGuid();

        var deleteResp = await client.DeleteAsync($"/api/v1/contracts/{contractId}/documents/{docId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

        var listResp = await client.GetAsync($"/api/v1/contracts/{contractId}/documents");
        var docs = await listResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(0, docs.GetArrayLength());
    }

    // T10 — cross-org isolation: org B cannot access org A's document download token
    [Fact]
    public async Task T10_CrossOrgIsolation_CannotUseOtherOrgToken()
    {
        var (contractIdA, clientA) = await _factory.SetupContractAsync("doc-t10a");
        var (_, clientB) = await _factory.SetupContractAsync("doc-t10b");

        // Org A uploads a file
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("secret")) { Headers = { { "Content-Type", "text/plain" } } },
            "file", "secret.txt");
        var uploadResp = await clientA.PostAsync($"/api/v1/contracts/{contractIdA}/documents", content);
        uploadResp.EnsureSuccessStatusCode();
        var uploaded = await uploadResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var docId = uploaded.GetProperty("id").GetGuid();

        // Org A gets download URL
        var urlResp = await clientA.GetAsync($"/api/v1/contracts/{contractIdA}/documents/{docId}/download-url");
        urlResp.EnsureSuccessStatusCode();
        var urlDto = await urlResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var tokenA = urlDto.GetProperty("token").GetString()!;

        // Org B cannot list Org A's documents (tenant filter)
        var listAsB = await clientB.GetAsync($"/api/v1/contracts/{contractIdA}/documents");
        var docsB = await listAsB.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(0, docsB.GetArrayLength()); // filtered out

        // Token from Org A still works via /files/download (token contains orgId — so this is valid)
        // but Org B cannot generate a token for Org A's document
        var urlRespFromB = await clientB.GetAsync($"/api/v1/contracts/{contractIdA}/documents/{docId}/download-url");
        Assert.Equal(HttpStatusCode.NotFound, urlRespFromB.StatusCode);
    }
}
