using Npgsql;

namespace Teste
{
    public class Program
    {
        public static async void Main(string[] args)
        {
            var connString = "host=127.0.0.1;port=5432;database=postgres;User Id=postgres;password=adm123;";
            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            Console.WriteLine("Conectado com sucesso!");
        }
    }
}