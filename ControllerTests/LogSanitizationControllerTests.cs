using ControllerTests.Logging;
using Domain.Interface.Exceptions;
using Domain.Interface.Order;
using GerenciamentoMecanicaSistema.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using NSubstitute;
using Service.Interface.Exceptions;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace ControllerTests;

public sealed class LogSanitizationControllerTests : BaseControllerTests
{
    private CapturingLoggerProvider logs = null!;
    private const string Sensitive = "417.384.220-11 11222333000181 CVC2026 cliente@example.invalid Nome Privado senha-privada hash-privado eyJfake.eyJfake.fake Host=private;Password=private jwt-key-private aws-config-private 123456.78";

    protected override TestWebApplicationFactory CreateFactory()
    {
        logs = new();
        return new() { LogProvider = logs };
    }

    protected override void MockService() { }

    [TestCase("application", 400, LogLevel.Warning)]
    [TestCase("domain", 400, LogLevel.Warning)]
    [TestCase("business", 422, LogLevel.Warning)]
    [TestCase("unexpected", 500, LogLevel.Error)]
    public async Task LogsOnlyTechnicalErrorDetailsAndRouteTemplate(string scenario, int status, LogLevel level)
    {
        var id = Guid.NewGuid();
        TestWebAppFactory.OrderServiceMock.GetOrderStatus(id).Returns(_ => ThrowRequestFailure(scenario));

        using var response = await TestClient.GetAsync($"/Orders/{id}/status?document=417.384.220-11&password=senha-privada");
        var body = await response.Content.ReadAsStringAsync();
        var entry = logs.Entries.Single(entry => entry.Properties.ContainsKey("ExceptionType"));

        Assert.Multiple(() =>
        {
            Assert.That((int)response.StatusCode, Is.EqualTo(status));
            Assert.That(response.Content.Headers.ContentType!.MediaType, Is.EqualTo("application/json"));
            Assert.That(entry.Level, Is.EqualTo(level));
            Assert.That(entry.Exception, Is.Null);
            Assert.That(entry.Properties["StatusCode"], Is.EqualTo(status));
            Assert.That(entry.Properties["ExceptionType"], Is.Not.Null.And.Not.Empty);
            Assert.That(entry.Properties.ContainsKey("StackTrace"), Is.False);
            Assert.That(entry.Message, Does.Not.Contain(nameof(ThrowRequestFailure)));
            Assert.That(entry.Properties["Route"], Is.EqualTo("Orders/{id}/status"));
            Assert.That(entry.Properties["HttpMethod"], Is.EqualTo("GET"));
            Assert.That(entry.Properties["RequestId"], Is.Not.Null.And.Not.Empty);
            Assert.That(entry.Properties["TraceId"], Is.Not.Null.And.Not.Empty);
        });
        AssertSafe(entry, id.ToString());
        using var problem = JsonDocument.Parse(body);
        Assert.That(problem.RootElement.GetProperty("detail").GetString(), Is.EqualTo(status == 500
            ? "Ocorreu um erro inesperado ao processar a requisição." : Sensitive));

        var formatter = TestWebAppFactory.Services.GetServices<ConsoleFormatter>().Single(formatter => formatter.Name == ConsoleFormatterNames.Simple);
        var scopeProvider = new LoggerExternalScopeProvider();
        using var pushed = scopeProvider.Push("RequestPath: /cliente@example.invalid/417.384.220-11");
        using var text = new StringWriter();
        var logEntry = new LogEntry<string>(entry.Level, "tests", default, entry.Message, null, (state, _) => state);
        formatter.Write(in logEntry, scopeProvider, text);
        Assert.That(text.ToString(), Does.Contain("RequestId:").And.Not.Contain(nameof(ThrowRequestFailure)));
        foreach (var value in Sensitive.Split(' ')) Assert.That(text.ToString(), Does.Not.Contain(value));
    }

    [Test]
    public async Task UnknownRouteAndMethodNeverLogTheirReceivedValues()
    {
        using var factory = LoggerFactory.Create(builder => builder.AddProvider(logs));
        using var activity = new Activity("logging-test").SetIdFormat(ActivityIdFormat.W3C).Start();
        var context = new DefaultHttpContext { RequestServices = TestWebAppFactory.Services, TraceIdentifier = "server-request-id" };
        context.Response.Body = new MemoryStream();
        context.Request.Method = "PRIVATE-METHOD";
        context.Request.Path = "/cliente@example.invalid/417.384.220-11";
        context.Request.QueryString = new("?password=senha-privada");
        var middleware = new ExceptionHandlingMiddleware(async _ => { await ThrowRequestFailure("unexpected"); }, factory.CreateLogger<ExceptionHandlingMiddleware>());

        await middleware.InvokeAsync(context);

        var entry = logs.Entries.Single(entry => entry.Properties.ContainsKey("ExceptionType"));
        Assert.Multiple(() =>
        {
            Assert.That(entry.Properties["Route"], Is.EqualTo("unmatched"));
            Assert.That(entry.Properties["HttpMethod"], Is.EqualTo("OTHER"));
            Assert.That(entry.Properties["RequestId"], Is.EqualTo("server-request-id"));
            Assert.That(entry.Properties["TraceId"], Is.EqualTo(activity.TraceId.ToString()));
            Assert.That(context.Response.StatusCode, Is.EqualTo(500));
        });
        AssertSafe(entry, "PRIVATE-METHOD");
        await context.Response.Body.DisposeAsync();
    }

    [Test]
    public async Task ConcurrentRequestsKeepSeparateTechnicalIdentifiers()
    {
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var arrived = 0;
        TestWebAppFactory.OrderServiceMock.GetOrderStatus(Arg.Any<Guid>()).Returns(async _ =>
        {
            if (Interlocked.Increment(ref arrived) == 2) ready.SetResult();
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(10));
            return await ThrowRequestFailure("unexpected");
        });

        var responses = await Task.WhenAll(TestClient.GetAsync($"/Orders/{Guid.NewGuid()}/status"), TestClient.GetAsync($"/Orders/{Guid.NewGuid()}/status"));
        foreach (var response in responses) { Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError)); response.Dispose(); }

        var entries = logs.Entries.Where(entry => entry.Properties.ContainsKey("ExceptionType")).ToArray();
        Assert.That(entries, Has.Length.EqualTo(2));
        Assert.That(entries.Select(entry => entry.Properties["RequestId"]).Distinct().Count(), Is.EqualTo(2));
        foreach (var entry in entries) AssertSafe(entry);
    }

    [Test]
    public void TextConsoleKeepsLoggingLevelsAndDoesNotCaptureRequestScopes()
    {
        var options = TestWebAppFactory.Services.GetRequiredService<IOptionsMonitor<SimpleConsoleFormatterOptions>>().CurrentValue;
        Assert.That(options.IncludeScopes, Is.False);
        var factory = TestWebAppFactory.Services.GetRequiredService<ILoggerFactory>();
        Assert.That(TestWebAppFactory.Services.GetRequiredService<IOptions<LoggerFilterOptions>>().Value.CaptureScopes, Is.False);
        Assert.That(factory.CreateLogger("Microsoft.AspNetCore.Hosting.Diagnostics").IsEnabled(LogLevel.Critical), Is.True);
        Assert.That(factory.CreateLogger<ExceptionHandlingMiddleware>().IsEnabled(LogLevel.Warning), Is.True);
    }

    private static async Task<WorkOrderStatus> ThrowRequestFailure(string scenario)
    {
        await Task.Yield();
        Exception error = scenario switch
        {
            "application" => new InvalidRequestException(Sensitive),
            "domain" => new DomainValidationException(Sensitive),
            "business" => new BusinessRuleException(Sensitive),
            _ => new InvalidOperationException(Sensitive, new Exception("private-inner"))
        };
        error.Data["private-key"] = "private-data";
        throw error;
    }

    private static void AssertSafe(CapturedLogEntry entry, string additional = "not-a-real-sensitive-value")
    {
        var rendered = entry.Message + string.Join(" ", entry.Properties.Values)
            + string.Join(" ", entry.Scopes.Select(scope => scope.Key + string.Join(" ", scope.Value.Values)));
        Assert.That(entry.Exception, Is.Null);
        foreach (var value in Sensitive.Split(' ').Concat(["private-inner", "private-data", additional, TestWebApplicationFactory.JwtKey]))
            Assert.That(rendered, Does.Not.Contain(value));
    }
}
