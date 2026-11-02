namespace Infrastructure.Persistence.PostgreSql.Models
{
    internal class OrderStatusHistoryDb
    {
        public int Sequence { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
    }
}
