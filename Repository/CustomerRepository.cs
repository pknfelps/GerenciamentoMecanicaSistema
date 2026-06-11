using Dapper;
using Domain.Customer;
using Domain.Interface.Custumer;
using Repository.Dto;
using Repository.Interface;
using System.Data;

namespace Repository
{
    public class CustomerRepository(IDbConnection connection) : BaseRepository(connection), ICustomerRepository
    {
        public static string RegisterCustomerSql { get; private set; } = """
                INSERT INTO customers(id, name, document, phone, email)
                VALUES (@Id, @Name, @Document, @Phone, @Email);
                """;

        public static string GetCustomersSql { get; private set; } = """
                SELECT id, name, document, phone, email
                FROM customers
                LIMIT 50;
                """;

        public static string GetCustomersByDocumentoSql { get; private set; } = """
                SELECT id, name, document, phone, email
                FROM customers
                WHERE document = @document;
                """;

        public static string UpdateCustomersql { get; private set; } = $"""
                UPDATE customers
                SET name = @Name,
                    phone = @Phone,
                    email = @Email
                WHERE document = @Document;
                """;

        public static string DeleteCustomersql { get; private set; } = """
                DELETE FROM customers
                WHERE document = @document;
                """;

        public async Task<int> RegisterCustomer(ICustomer customer)
        {
            return await Connection.ExecuteAsync(RegisterCustomerSql, CustomerDb.Create(customer));
        }

        public async Task<IEnumerable<ICustomer>> GetCustomers()
        {
            var customers = await Connection.QueryAsync<CustomerDb>(GetCustomersSql);

            return customers.Select(customer => customer.ToDomain());
        }

        public async Task<ICustomer?> GetCustomer(string document)
        {
            var customer = await Connection.QuerySingleOrDefaultAsync<CustomerDb?>(GetCustomersByDocumentoSql, new { document });

            if (customer == null)
                return null;

            return customer.ToDomain();
        }

        public async Task<int> UpdateCustomer(ICustomer customer)
        {
            return await Connection.ExecuteAsync(UpdateCustomersql, CustomerDb.Create(customer));
        }

        public async Task<int> DeleteCustomer(string document)
        {
            return await Connection.ExecuteAsync(DeleteCustomersql, new { document });
        }
    }
}
