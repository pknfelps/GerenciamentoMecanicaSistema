using ControllerTests.Logging;
using DependencyInjection;
using Domain.Interface.Custumer;
using Domain.Interface.Order;
using Domain.Interface.Vehicle;
using Infrastructure.Email;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Service.Events;
using Service.Interface;
using Service.Interface.Email;
using Service.Interface.Events.Order;

namespace ControllerTests;

public sealed class EmailRegistrationTests
{
    [TestCase(null, "Development")]
    [TestCase(null, "Production")]
    [TestCase("hom", "Development")]
    [TestCase("hom", "Staging")]
    [TestCase("prd", "Development")]
    [TestCase("prd", "Production")]
    public async Task SelectsEmailAdapterFromValidatedRuntimeMode(string? environment, string hostEnvironment)
    {
        var values = new Dictionary<string, string?>
        {
            ["ASPNETCORE_ENVIRONMENT"] = hostEnvironment,
            ["EmailSettings:Host"] = "smtp-local.example.invalid",
            ["EmailSettings:Port"] = "2525",
            ["EmailSettings:Username"] = "local-user",
            ["EmailSettings:Password"] = "local-password",
            ["EmailSettings:SenderName"] = "Local Sender",
            ["EmailSettings:SenderEmail"] = "sender@example.invalid",
            ["EmailSettings:UseTls"] = "true"
        };
        if (environment is not null) values["Runtime:Environment"] = environment;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var usesAwsConfiguration = environment is not null;
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddInfrastructure(configuration, usesAwsConfiguration);
        await using var provider = services.BuildServiceProvider();

        var sender = provider.GetRequiredService<IEmailSender>();

        Assert.That(services.Count(service => service.ServiceType == typeof(IEmailSender)), Is.EqualTo(1));
        if (usesAwsConfiguration)
        {
            Assert.That(sender, Is.TypeOf<LoggingEmailSender>());
            AssertNoSmtp(services, provider);
        }
        else
        {
            Assert.That(sender, Is.TypeOf<MailKitEmailSender>());
            Assert.That(services.Any(service => service.ServiceType == typeof(ISmtpClient)), Is.True);
            Assert.That(services.Any(service => service.ServiceType == typeof(SmtpConnection)), Is.True);
            var settings = provider.GetRequiredService<IOptions<EmailSettings>>().Value;
            Assert.Multiple(() =>
            {
                Assert.That(settings.Host, Is.EqualTo("smtp-local.example.invalid"));
                Assert.That(settings.Port, Is.EqualTo(2525));
                Assert.That(settings.Username, Is.EqualTo("local-user"));
                Assert.That(settings.Password, Is.EqualTo("local-password"));
                Assert.That(settings.SenderName, Is.EqualTo("Local Sender"));
                Assert.That(settings.SenderEmail, Is.EqualTo("sender@example.invalid"));
                Assert.That(settings.UseTls, Is.True);
            });
        }
    }

    [Test]
    public async Task AwsRegistrationDoesNotReadSmtpConfiguration()
    {
        var configuration = Substitute.For<IConfiguration>();
        configuration.GetSection("EmailSettings").Returns(_ => throw new InvalidOperationException("SMTP configuration must not be read"));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration, usesAwsConfiguration: true);
        await using var provider = services.BuildServiceProvider();

        Assert.That(provider.GetRequiredService<IEmailSender>(), Is.TypeOf<LoggingEmailSender>());
        AssertNoSmtp(services, provider);
        configuration.DidNotReceive().GetSection("EmailSettings");
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task RealNotificationFlowUsesSimulatedSenderWithoutExternalServices(bool budget)
    {
        var logs = new CapturingLoggerProvider();
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddProvider(logs));
        services.AddInfrastructure(configuration, usesAwsConfiguration: true);
        services.AddTransient<IEmailService, Service.EmailService>();
        await using var provider = services.BuildServiceProvider();
        var customer = Substitute.For<ICustomer>();
        customer.Name.Returns("Nome Privado");
        customer.Email.Address.Returns("cliente@example.invalid");
        var vehicle = Substitute.For<IVehicle>();
        vehicle.Model.Returns("Modelo Privado");
        vehicle.LicensePlate.License.Returns("CVC2026");
        var order = new OrderNotificationSnapshot(Guid.NewGuid(), "41738422011", "CVC2026", 123456.78m, WorkOrderStatus.WaitingForApproval);
        var dependencies = Substitute.For<IOrderDependenciesGateway>();
        dependencies.GetCustomerByDocument(order.CustomerDocument).Returns(customer);
        dependencies.GetVehicleByLicensePlate(order.VehicleLicensePlate).Returns(vehicle);
        var handler = new OrderNotificationEventHandler(dependencies, provider.GetRequiredService<IEmailService>(),
            provider.GetRequiredService<ILogger<OrderNotificationEventHandler>>());

        await handler.Handle(budget ? new BudgetAvailableEvent(order) : new OrderStatusChangedEvent(order));

        var entry = logs.Entries.Single();
        Assert.Multiple(() =>
        {
            Assert.That(entry.Level, Is.EqualTo(LogLevel.Information));
            Assert.That(entry.Message, Is.EqualTo("Envio de e-mail simulado."));
            Assert.That(entry.Exception, Is.Null);
            Assert.That(entry.Properties.Keys, Is.EquivalentTo(new[] { "{OriginalFormat}" }));
        });
        AssertNoSmtp(services, provider);
        await dependencies.Received(1).GetCustomerByDocument(order.CustomerDocument);
        await dependencies.Received(1).GetVehicleByLicensePlate(order.VehicleLicensePlate);
    }

    private static void AssertNoSmtp(IServiceCollection services, IServiceProvider provider)
    {
        Assert.Multiple(() =>
        {
            Assert.That(provider.GetService<ISmtpClient>(), Is.Null);
            Assert.That(provider.GetService<SmtpConnection>(), Is.Null);
            Assert.That(provider.GetService<MailKitEmailSender>(), Is.Null);
            Assert.That(services.Any(service => service.ServiceType == typeof(IConfigureOptions<EmailSettings>)), Is.False);
        });
    }
}
