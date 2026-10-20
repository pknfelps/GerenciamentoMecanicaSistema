using Infrastructure.Observability;
using Domain.Interface.Custumer;
using Infrastructure.Persistence.PostgreSql.Models;
using Infrastructure.Persistence.PostgreSql.Querying;
using Infrastructure.Persistence.PostgreSql.Transactions;
using Service.Interface.Persistence;
using System.Data;

namespace Infrastructure.Persistence.PostgreSql.Repositories
{
    public class CustomerRepository(IDbConnection connection, DbTransactionContext? transactionContext = null, IntegrationMetrics? metrics = null) : BaseRepository(connection, transactionContext, metrics), ICustomerRepository
    {
        public static string RegisterCustomerSql { get; private set; } = """
                INSERT INTO customers(id, name, document, phone, email, status)
                VALUES (@Id, @Name, @Document, @Phone, @Email, @Status);
                """;

        public static string GetCustomersSql { get; private set; } = """
                SELECT id, name, document, phone, email, status
                FROM customers
                {0}
                LIMIT 50;
                """;

        public static string UpdateCustomersql { get; private set; } = $"""
                UPDATE customers
                SET name = @Name,
                    phone = @Phone,
                    email = @Email
                WHERE id = @id;
                """;

        public static string DeleteCustomersql { get; private set; } = """
                DELETE FROM customers
                WHERE id = @id;
                """;

        public async Task<int> RegisterCustomer(ICustomer customer)
        {
            return await Database.ExecuteAsync(RegisterCustomerSql, CustomerDb.Create(customer), Transaction);
        }

        public async Task<IEnumerable<ICustomer>> GetCustomers(Guid? id = null, string name = "", string document = "")
        {
            var query = GetCustomersSql.BuildQuery(BuildQueryParameters(id, name, document));
            var customers = await Database.QueryAsync<CustomerDb>(query.Sql, query.Parameters, Transaction);

            return customers.Select(customer => customer.ToDomain());
        }

        public async Task<ICustomer?> GetCustomer(Guid? id = null, string name = "", string document = "")
        {
            var query = GetCustomersSql.BuildQuery(BuildQueryParameters(id, name, document));
            var customer = await Database.QuerySingleOrDefaultAsync<CustomerDb>(query.Sql, query.Parameters, Transaction);

            if (customer == null)
                return null;

            return customer.ToDomain();
        }

        public async Task<int> UpdateCustomer(ICustomer customer)
        {
            return await Database.ExecuteAsync(UpdateCustomersql, CustomerDb.Create(customer), Transaction);
        }

        public async Task<int> DeleteCustomer(Guid id)
        {
            return await Database.ExecuteAsync(DeleteCustomersql, new { id }, Transaction);
        }

        private static Dictionary<string, object?> BuildQueryParameters(Guid? id = null, string name = "", string document = "")
        {
            return new() { { nameof(id), id }, { nameof(name), name }, { nameof(document), document } };
        }
    }
}
