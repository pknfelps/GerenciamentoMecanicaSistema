using Infrastructure.Persistence.PostgreSql.Transactions;
using Microsoft.Extensions.Logging;
using Npgsql;
using NSubstitute;
using System.Data;

namespace InfrastructureTests;

public sealed class TransactionLoggingTests
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task RollbackLogsOnlyTechnicalDetailsAndPreservesFailure(bool postgres)
    {
        var connection = Substitute.For<IDbConnection>();
        var transaction = Substitute.For<IDbTransaction>();
        connection.BeginTransaction().Returns(transaction);
        var context = new DbTransactionContext();
        var logger = Substitute.For<ILogger<TransactionManager>>();
        var manager = new TransactionManager(connection, context, logger);

        var error = Assert.CatchAsync(() => manager.ExecuteInTransaction(() => ThrowPersistenceFailure(postgres)));

        transaction.Received(1).Rollback();
        transaction.DidNotReceive().Commit();
        Assert.That(context.Current, Is.Null);
        Assert.That(error!.Message, Does.Contain("private-message"));
        var arguments = logger.ReceivedCalls().Single(call => call.GetMethodInfo().Name == nameof(ILogger.Log)).GetArguments();
        var properties = ((IEnumerable<KeyValuePair<string, object?>>)arguments[2]!).ToDictionary(pair => pair.Key, pair => pair.Value);
        Assert.That(arguments[0], Is.EqualTo(LogLevel.Error));
        Assert.That(arguments[3], Is.Null);
        Assert.That(properties["ExceptionType"], Is.EqualTo(error.GetType().FullName));
        Assert.That(properties.Keys, Is.EquivalentTo(new[] { "ExceptionType", "{OriginalFormat}" }));
        Assert.That(arguments[2]!.ToString(), Does.Not.Contain(nameof(ThrowPersistenceFailure)));
        foreach (var value in new[] { "private-message", "private-inner", "private-data", "private-detail", "23505", "417.384.220-11", "cliente@example.invalid", "Password=private" })
        {
            Assert.That(arguments[2]!.ToString(), Does.Not.Contain(value));
            Assert.That(string.Join(" ", properties.Values), Does.Not.Contain(value));
        }
    }

    private static async Task ThrowPersistenceFailure(bool postgres)
    {
        await Task.Yield();
        const string message = "private-message 417.384.220-11 cliente@example.invalid Password=private";
        Exception error = postgres
            ? new PostgresException(message, "ERROR", "ERROR", "23505", detail: "private-detail")
            : new InvalidOperationException(message, new Exception("private-inner"));
        error.Data["detail"] = "private-data";
        throw error;
    }
}
