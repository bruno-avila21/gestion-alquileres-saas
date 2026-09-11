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

    // T11 — REGRESSION (audit 2026-07-31, critical): visibility may only be changed through the
    // document's OWN contract route. The controller used to check ownership *after* the command
    // had already saved, so this call returned 404 while the flag was persisted — and the tenant
    // of the other contract could then download the file via /me/documents.
    [Fact]
    public async Task T11_SetVisibility_DocumentFromAnotherContract_Returns404AndDoesNotWrite()
    {
        var (contractA, client) = await _factory.SetupContractAsync("doc-t11");
        var contractB = await CreateSecondContractAsync(client, "doc-t11");

        // Contract B gets a private document.
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("informe privado"))
            { Headers = { { "Content-Type", "application/pdf" } } }, "file", "privado.pdf");
        content.Add(new StringContent("false"), "isVisibleToTenant");
        var uploadResp = await client.PostAsync($"/api/v1/contracts/{contractB}/documents", content);
        uploadResp.EnsureSuccessStatusCode();
        var uploaded = await uploadResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var docId = uploaded.GetProperty("id").GetGuid();
        Assert.False(uploaded.GetProperty("isVisibleToTenant").GetBoolean());

        // Same org, same staff user — but the document is addressed through contract A.
        var patchResp = await client.PatchAsJsonAsync(
            $"/api/v1/contracts/{contractA}/documents/{docId}/visibility",
            new { isVisibleToTenant = true });
        Assert.Equal(HttpStatusCode.NotFound, patchResp.StatusCode);

        // The 404 must mean "nothing happened": the document is still private.
        var listResp = await client.GetAsync($"/api/v1/contracts/{contractB}/documents");
        listResp.EnsureSuccessStatusCode();
        var docs = await listResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(1, docs.GetArrayLength());
        Assert.False(docs[0].GetProperty("isVisibleToTenant").GetBoolean());
    }

    // T12 — the matching pair still works, so T11 is not passing because visibility is broken.
    [Fact]
    public async Task T12_SetVisibility_OwnContract_Returns200AndFlips()
    {
        var (contractId, client) = await _factory.SetupContractAsync("doc-t12");

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("contrato firmado"))
            { Headers = { { "Content-Type", "application/pdf" } } }, "file", "contrato.pdf");
        content.Add(new StringContent("false"), "isVisibleToTenant");
        var uploadResp = await client.PostAsync($"/api/v1/contracts/{contractId}/documents", content);
        uploadResp.EnsureSuccessStatusCode();
        var uploaded = await uploadResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var docId = uploaded.GetProperty("id").GetGuid();

        var patchResp = await client.PatchAsJsonAsync(
            $"/api/v1/contracts/{contractId}/documents/{docId}/visibility",
            new { isVisibleToTenant = true });
        Assert.Equal(HttpStatusCode.OK, patchResp.StatusCode);

        var patched = await patchResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.True(patched.GetProperty("isVisibleToTenant").GetBoolean());
        Assert.Equal(contractId, patched.GetProperty("contractId").GetGuid());
    }

    // T13 — an unknown document id now yields 404 instead of the previous unhandled
    // KeyNotFoundException, which the exception middleware turned into a 500.
    [Fact]
    public async Task T13_SetVisibility_UnknownDocument_Returns404()
    {
        var (contractId, client) = await _factory.SetupContractAsync("doc-t13");

        var patchResp = await client.PatchAsJsonAsync(
            $"/api/v1/contracts/{contractId}/documents/{Guid.NewGuid()}/visibility",
            new { isVisibleToTenant = true });

        Assert.Equal(HttpStatusCode.NotFound, patchResp.StatusCode);
    }

    /// <summary>Creates a second property/tenant/contract inside the SAME organization as `client`.</summary>
    private static async Task<Guid> CreateSecondContractAsync(HttpClient client, string slug)
    {
        var propResp = await client.PostAsJsonAsync("/api/v1/properties", new
        {
            address = "Corrientes 2000", city = "CABA", province = "CABA",
            propertyType = "Apartment", areaM2 = (decimal?)null, notes = (string?)null,
        });
        propResp.EnsureSuccessStatusCode();
        var propId = (await propResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("id").GetGuid();

        var tenantResp = await client.PostAsJsonAsync("/api/v1/tenants", new
        {
            firstName = "Beto", lastName = "Pérez", dni = "29999222",
            email = $"beto@{slug}.com", phone = (string?)null,
        });
        tenantResp.EnsureSuccessStatusCode();
        var tenantId = (await tenantResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("id").GetGuid();

        var contractResp = await client.PostAsJsonAsync("/api/v1/contracts", new
        {
            propertyId = propId,
            appTenantId = tenantId,
            startDate = "2026-01-01",
            endDate = "2028-01-01",
            monthlyRent = 300_000m,
            currency = "ARS",
            adjustmentType = "Manual",
            adjustmentFrequency = "Quarterly",
            dayOfMonth = 1,
            depositAmount = (decimal?)null,
            notes = (string?)null,
        });
        contractResp.EnsureSuccessStatusCode();
        return (await contractResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("id").GetGuid();
    }
}
