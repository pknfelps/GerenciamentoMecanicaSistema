using GerenciamentoMecanicaSistema;

namespace ControllerTests;

[NonParallelizable]
public sealed class ProgramStartupTests
{
    [TestCase("Runtime:Environment", "private-runtime-value")]
    [TestCase("Telemetry:Enabled", "private-telemetry-value")]
    public async Task StartupFailureReturnsAnErrorAndPrintsOnlyTheFixedMessageAndExceptionType(string setting, string value)
    {
        var originalError = Console.Error;
        using var output = new StringWriter();
        try
        {
            Console.SetError(output);

            var exitCode = await Program.Main(["Telemetry:Enabled=false", $"{setting}={value}"]);

            Assert.Multiple(() =>
            {
                Assert.That(exitCode, Is.EqualTo(1));
                Assert.That(output.ToString().Trim(), Is.EqualTo(
                    "Falha na inicialização ou execução da API. ExceptionType: System.InvalidOperationException"));
                Assert.That(output.ToString(), Does.Not.Contain(value));
            });
        }
        finally
        {
            Console.SetError(originalError);
        }
    }
}
