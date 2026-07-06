using Dapper;
using Domain.Interface.Custumer;
using Repository.PersistenceModels;
using Repository.Interface;
using System.Data;

namespace Repository
{
    public class CustomerRepository(IDbConnection connection, DbTransactionContext? transactionContext = null) : BaseRepository(connection, transactionContext), ICustomerRepository
    {
        public static string RegisterCustomerSql { get; private set; } = """
                INSERT INTO customers(id, name, document, phone, email)
                VALUES (@Id, @Name, @Document, @Phone, @Email);
                """;

        public static string GetCustomersSql { get; private set; } = """
                SELECT id, name, document, phone, email
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
            return await Connection.ExecuteAsync(RegisterCustomerSql, CustomerDb.Create(customer), Transaction);
        }

        public async Task<IEnumerable<ICustomer>> GetCustomers(Guid? id = null, string name = "", string document = "")
        {
            var query = GetCustomersSql.BuildQuery(BuildQueryParameters(id, name, document));
            var customers = await Connection.QueryAsync<CustomerDb>(query.Sql, query.Parameters, Transaction);

            return customers.Select(customer => customer.ToDomain());
        }

        public async Task<ICustomer?> GetCustomer(Guid? id = null, string name = "", string document = "")
        {
            var query = GetCustomersSql.BuildQuery(BuildQueryParameters(id, name, document));
            var customer = await Connection.QuerySingleOrDefaultAsync<CustomerDb>(query.Sql, query.Parameters, Transaction);

            if (customer == null)
                return null;

            return customer.ToDomain();
        }

        public async Task<int> UpdateCustomer(ICustomer customer)
        {
            return await Connection.ExecuteAsync(UpdateCustomersql, CustomerDb.Create(customer), Transaction);
        }

        public async Task<int> DeleteCustomer(Guid id)
        {
            return await Connection.ExecuteAsync(DeleteCustomersql, new { id }, Transaction);
        }

        private static Dictionary<string, object?> BuildQueryParameters(Guid? id = null, string name = "", string document = "")
        {
            return new() { { nameof(id), id }, { nameof(name), name }, { nameof(document), document } };
        }
    }
}
