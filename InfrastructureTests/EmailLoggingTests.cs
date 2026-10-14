using Infrastructure.Email;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using NSubstitute;

namespace InfrastructureTests;

public sealed class EmailLoggingTests
{
    private static readonly string[] SensitiveValues =
    [
        "smtp-private.example.invalid", "private-user", "private-password", "private-sender", "sender@example.invalid",
        "Nome Privado", "cliente@example.invalid", "Assunto Privado", "private-body", "private-html",
        "417.384.220-11", "11222333000181", "CVC2026", "private-message", "private-inner", "private-data",
        "eyJfake.eyJfake.fake", "Host=private;Password=private", "jwt-key-private"
    ];

    [TestCase(false)]
    [TestCase(true)]
    public async Task SmtpFailuresRethrowWithoutLoggingStackOrSettings(bool authentication)
    {
        var client = Substitute.For<ISmtpClient>();
        var logger = Substitute.For<ILogger<SmtpConnection>>();
        client.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<SecureSocketOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ => authentication ? Task.CompletedTask : ThrowSmtpFailure());
        client.AuthenticateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ => ThrowSmtpFailure());
        await using var connection = new SmtpConnection(client, Settings(), logger);

        var error = Assert.ThrowsAsync<InvalidOperationException>(() => connection.GetClientAsync());

        Assert.That(error!.Message, Does.Contain("private-message"));
        AssertSafeLogs(logger, nameof(ThrowSmtpFailure));
        Assert.That(logger.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(ILogger.Log)), Is.EqualTo(2));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task SendingFailuresNeverLogMessageOrRecipient(bool connectionFailure)
    {
        var client = Substitute.For<ISmtpClient>();
        var connectionLogger = Substitute.For<ILogger<SmtpConnection>>();
        var senderLogger = Substitute.For<ILogger<MailKitEmailSender>>();
        client.IsConnected.Returns(!connectionFailure);
        client.ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<SecureSocketOptions>(), Arg.Any<CancellationToken>()).Returns(_ => ThrowSmtpFailure());
        client.SendAsync(Arg.Any<MimeMessage>(), Arg.Any<CancellationToken>(), Arg.Any<ITransferProgress>()).Returns(_ => ThrowSendFailure());
        await using var connection = new SmtpConnection(client, Settings(), connectionLogger);
        var sender = new MailKitEmailSender(Settings(), connection, senderLogger);

        Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendAsync("Nome Privado", "cliente@example.invalid", "Assunto Privado", "private-body", "private-html"));

        AssertSafeLogs(senderLogger, connectionFailure ? nameof(ThrowSmtpFailure) : nameof(ThrowSendFailure));
        if (connectionFailure) AssertSafeLogs(connectionLogger, nameof(ThrowSmtpFailure));
    }

    [Test]
    public async Task SanitizationDoesNotAlterTheEmailBeingSent()
    {
        var client = Substitute.For<ISmtpClient>();
        var logger = Substitute.For<ILogger<MailKitEmailSender>>();
        client.IsConnected.Returns(true);
        client.SendAsync(Arg.Any<MimeMessage>(), Arg.Any<CancellationToken>(), Arg.Any<ITransferProgress>()).Returns(Task.FromResult("sent"));
        await using var connection = new SmtpConnection(client, Settings(), Substitute.For<ILogger<SmtpConnection>>());
        var sender = new MailKitEmailSender(Settings(), connection, logger);

        await sender.SendAsync("Nome Privado", "cliente@example.invalid", "Assunto Privado", "private-body", "private-html");

        await client.Received().SendAsync(Arg.Is<MimeMessage>(message => message.To.Mailboxes.Single().Address == "cliente@example.invalid"
            && message.Subject == "Assunto Privado" && message.TextBody == "private-body" && message.HtmlBody == "private-html"),
            Arg.Any<CancellationToken>(), Arg.Any<ITransferProgress>());
        Assert.That(logger.ReceivedCalls().Where(call => call.GetMethodInfo().Name == nameof(ILogger.Log)), Is.Empty);
    }

    private static IOptions<EmailSettings> Settings() => Options.Create(new EmailSettings
    {
        Host = "smtp-private.example.invalid", Port = 2525, Username = "private-user", Password = "private-password",
        SenderName = "private-sender", SenderEmail = "sender@example.invalid", UseTls = false
    });

    private static async Task ThrowSmtpFailure()
    {
        await Task.Yield();
        throw CreateFailure();
    }

    private static async Task<string> ThrowSendFailure()
    {
        await Task.Yield();
        throw CreateFailure();
    }

    private static InvalidOperationException CreateFailure()
    {
        var error = new InvalidOperationException("private-message " + string.Join(" ", SensitiveValues), new Exception("private-inner"));
        error.Data["detail"] = "private-data";
        return error;
    }

    private static void AssertSafeLogs<T>(ILogger<T> logger, string originalMethod)
    {
        var calls = logger.ReceivedCalls().Where(call => call.GetMethodInfo().Name == nameof(ILogger.Log)).ToArray();
        Assert.That(calls, Is.Not.Empty);
        foreach (var call in calls)
        {
            var arguments = call.GetArguments();
            var properties = ((IEnumerable<KeyValuePair<string, object?>>)arguments[2]!).ToDictionary(pair => pair.Key, pair => pair.Value);
            Assert.That(arguments[3], Is.Null);
            if ((LogLevel)arguments[0]! == LogLevel.Error)
            {
                Assert.That(properties["ExceptionType"], Is.EqualTo(typeof(InvalidOperationException).FullName));
                Assert.That(properties.Keys, Is.EquivalentTo(new[] { "ExceptionType", "{OriginalFormat}" }));
                Assert.That(arguments[2]!.ToString(), Does.Not.Contain(originalMethod));
            }
            foreach (var value in SensitiveValues)
            {
                Assert.That(arguments[2]!.ToString(), Does.Not.Contain(value));
                Assert.That(string.Join(" ", properties.Values), Does.Not.Contain(value));
            }
        }
    }
}
