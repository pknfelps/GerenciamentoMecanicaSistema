using Infrastructure.Email;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace InfrastructureTests;

public sealed class LoggingEmailSenderTests
{
    [TestCase("cliente@example.invalid")]
    [TestCase("not an email address")]
    public async Task EachSendCompletesAndLogsOnlyOneFixedInformationEvent(string recipient)
    {
        var logger = Substitute.For<ILogger<LoggingEmailSender>>();
        var sender = new LoggingEmailSender(logger);

        for (var index = 0; index < 2; index++)
        {
            var sending = sender.SendAsync("Nome Privado", recipient, "Assunto Privado", "417.384.220-11 CVC2026 private-body", "<p>private-html</p>");
            Assert.That(sending.IsCompletedSuccessfully, Is.True);
            await sending;
        }

        var calls = logger.ReceivedCalls().Where(call => call.GetMethodInfo().Name == nameof(ILogger.Log)).ToArray();
        Assert.That(calls, Has.Length.EqualTo(2));
        foreach (var call in calls)
        {
            var arguments = call.GetArguments();
            var properties = ((IEnumerable<KeyValuePair<string, object?>>)arguments[2]!).ToDictionary(pair => pair.Key, pair => pair.Value);
            Assert.Multiple(() =>
            {
                Assert.That(arguments[0], Is.EqualTo(LogLevel.Information));
                Assert.That(arguments[2]!.ToString(), Is.EqualTo("Envio de e-mail simulado."));
                Assert.That(arguments[3], Is.Null);
                Assert.That(properties.Keys, Is.EquivalentTo(new[] { "{OriginalFormat}" }));
                Assert.That(properties["{OriginalFormat}"], Is.EqualTo("Envio de e-mail simulado."));
            });
        }
    }
}
