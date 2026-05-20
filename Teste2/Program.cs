using Npgsql;

namespace Teste2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Teste iniciado!");
            var connString = "host=127.0.0.1;port=5432;database=postgres;User Id=postgres;password=adm123;";
            using var conn = new NpgsqlConnection(connString);
            conn.Open();
            Console.WriteLine("Conectado com sucesso!");
        }
    }
}