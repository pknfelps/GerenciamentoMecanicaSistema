using Infrastructure.Observability;
using Domain.Interface.Order;
using Domain.Interface.Service;
using Domain.Interface.Stock;
using Infrastructure.Persistence.PostgreSql.Models;
using Infrastructure.Persistence.PostgreSql.Querying;
using Infrastructure.Persistence.PostgreSql.Transactions;
using Service.Interface.Persistence;
using System.Data;

namespace Infrastructure.Persistence.PostgreSql.Repositories
{
    public class OrdersRepository(IDbConnection connection, DbTransactionContext? transactionContext = null, IntegrationMetrics? metrics = null) : BaseRepository(connection, transactionContext, metrics), IOrdersRepository
    {
        public static string CreateServiceSql { get; private set; } = """
            INSERT INTO orders(id, customer_document, vehicle_license_plate, budget, status)
            VALUES (@Id, @CustomerDocument, @VehicleLicensePlate, @Budget, @Status);
            """;

        public static string GetOrdersSql { get; private set; } = """
            SELECT 
                os.id, 
                os.customer_document AS CustomerDocument, 
                os.vehicle_license_plate AS VehicleLicensePlate, 

                CASE
                    WHEN COUNT(s.id) = 0 THEN '[]'::json
                    ELSE json_agg(DISTINCT jsonb_build_object(
                        'id', s.id,
                        'description', s.description,
                        'hours', s.hours,
                        'price_per_hour', s.price_per_hour,
                        'amount', s.amount))
                END AS services,

                CASE
                    WHEN COUNT(m.id) = 0 THEN '[]'::json
                    ELSE json_agg(DISTINCT jsonb_build_object(
                        'id', m.id,
                        'name', m.name,
                        'brand', m.brand,
                        'price', m.price,
                        'amount', m.amount))
                    END AS materials,

                os.budget, 
                os.status, 
                history.date_created AS DateCreated,
                history.date_finished AS DateFinished

            FROM orders os
            LEFT JOIN order_services s ON s.order_id = os.id
            LEFT JOIN order_materials m ON m.order_id = os.id
            LEFT JOIN LATERAL (
                SELECT
                    MIN(occurred_at) FILTER (WHERE sequence = 1) AS date_created,
                    MIN(occurred_at) FILTER (WHERE new_status = 'Finished' AND reason = 'ServiceCompleted') AS date_finished
                FROM order_status_history
                WHERE order_id = os.id
            ) history ON TRUE

            {0}

            GROUP BY
                os.id,
                os.customer_document,
                os.vehicle_license_plate,
                os.budget,
                os.status,
                history.date_created,
                history.date_finished
            ORDER BY history.date_created ASC, os.id ASC;
            """;

        public static string GetOperationalOrdersSql { get; private set; } = """
            SELECT
                os.id,
                os.customer_document AS CustomerDocument,
                os.vehicle_license_plate AS VehicleLicensePlate,
                '[]'::json AS services,
                '[]'::json AS materials,
                os.budget,
                os.status,
                history.date_created AS DateCreated,
                history.date_finished AS DateFinished
            FROM orders os
            LEFT JOIN LATERAL (
                SELECT
                    MIN(occurred_at) FILTER (WHERE sequence = 1) AS date_created,
                    MIN(occurred_at) FILTER (WHERE new_status = 'Finished' AND reason = 'ServiceCompleted') AS date_finished
                FROM order_status_history
                WHERE order_id = os.id
            ) history ON TRUE
            WHERE os.status NOT IN (@Finished, @Delivered)
            ORDER BY
                CASE os.status
                    WHEN @InExecution THEN 0
                    WHEN @WaitingForExecution THEN 1
                    WHEN @WaitingForApproval THEN 2
                    WHEN @InDiagnosis THEN 3
                    WHEN @Received THEN 4
                    ELSE 5
                END,
                history.date_created ASC, os.id ASC;
            """;

        public static string UpdateOrderSql { get; private set; } = """
            UPDATE orders
            SET budget = @Budget,
                status = @Status
            WHERE id = @Id;
            """;

        private const string InsertHistorySql = """
            INSERT INTO order_status_history(id, order_id, sequence, previous_status, new_status, occurred_at, reason)
            VALUES (@EventId, @OrderId, @Sequence, @PreviousStatus, @NewStatus, @OccurredAt, @Reason);
            """;

        public static string AddServiceToOrderSql { get; private set; } = """
            INSERT INTO order_services(id, order_id, description, hours, price_per_hour, amount)
            VALUES (@Id, @orderId, @Description, @Hours, @PricePerHour, @Amount);
            """;

        public static string UpdateServiceAmountOfOrderSql { get; private set; } = """
            UPDATE order_services
            SET amount = @Amount
            WHERE id = @Id AND order_id = @orderId;
            """;

        public static string AddMaterialToOrderSql { get; private set; } = """
            INSERT INTO order_materials(id, order_id, name, brand, price, amount)
            VALUES (@Id, @orderId, @Name, @Brand, @Price, @Amount);
            """;

        public static string UpdateMaterialFromOrderSql { get; private set; } = """
            UPDATE order_materials
            SET amount = @Amount
            WHERE id = @Id AND order_id = @order_id;
            """;

        public static string DeleteServiceFromOrderSql { get; private set; } = """
            DELETE FROM order_services
            WHERE id = @serviceId AND order_id = @orderId;
            """;

        public static string RemoveMaterialFromOrderSql { get; private set; } = """
            DELETE FROM order_materials
            WHERE id = @materialId AND order_id = @orderId;
            """;

        public static string DeleteServicesFromOrderSql { get; private set; } = """
            DELETE FROM order_services
            WHERE order_id = @orderId;
            """;

        public static string RemoveMaterialsFromOrderSql { get; private set; } = """
            DELETE FROM order_materials
            WHERE order_id = @orderId;
            """;

        public static string DeleteOrderSql { get; private set; } = """
            DELETE FROM orders
            WHERE id = @orderId;
            """;

        public async Task LockOrder(Guid orderId)
        {
            if (Transaction is null)
                throw new InvalidOperationException("O bloqueio da ordem exige uma transação ativa.");

            await Database.QuerySingleOrDefaultAsync<Guid?>(
                "SELECT id FROM orders WHERE id = @Id FOR UPDATE;", new { Id = orderId }, Transaction);
        }

        public async Task<int> CreateOrder(IOrder serviceOrder)
        {
            if (serviceOrder.Status != WorkOrderStatus.Received || serviceOrder.DateCreated == DateTime.MinValue)
                throw new ArgumentException("A criação da ordem exige estado Received e instante de abertura.", nameof(serviceOrder));

            return await ExecuteInTransaction(async transaction =>
            {
                var affected = await Database.ExecuteAsync(CreateServiceSql, OrderDb.Create(serviceOrder), transaction);
                await Database.ExecuteAsync(InsertHistorySql, new
                {
                    EventId = Guid.NewGuid(),
                    OrderId = serviceOrder.Id,
                    Sequence = 1,
                    PreviousStatus = (string?)null,
                    NewStatus = serviceOrder.Status.ToString(),
                    OccurredAt = serviceOrder.DateCreated.ToUniversalTime(),
                    Reason = (string?)null
                }, transaction);
                return affected;
            });
        }

        public async Task<IEnumerable<IOrder>> GetOrders(Guid? id = null, string customer_document = "", string vehicle_license_plate = "")
        {
            var query = GetOrdersSql.BuildQuery(BuildQueryParameters(id, customer_document, vehicle_license_plate));
            var orders = await Database.QueryAsync<OrderDb>(query.Sql, query.Parameters, Transaction);

            return orders.Select(order => order.ToDomain());
        }

        public async Task<IEnumerable<IOrder>> GetOperationalOrders()
        {
            var orders = await Database.QueryAsync<OrderDb>(
                GetOperationalOrdersSql,
                new
                {
                    Finished = WorkOrderStatus.Finished.ToString(),
                    Delivered = WorkOrderStatus.Delivered.ToString(),
                    InExecution = WorkOrderStatus.InExecution.ToString(),
                    WaitingForExecution = WorkOrderStatus.WaitingForExecution.ToString(),
                    WaitingForApproval = WorkOrderStatus.WaitingForApproval.ToString(),
                    InDiagnosis = WorkOrderStatus.InDiagnosis.ToString(),
                    Received = WorkOrderStatus.Received.ToString()
                },
                Transaction);

            return orders.Select(order => order.ToDomain());
        }

        public async Task<IOrder?> GetOrder(Guid? id = null, string customer_document = "", string vehicle_license_plate = "")
        {
            var query = GetOrdersSql.BuildQuery(BuildQueryParameters(id, customer_document, vehicle_license_plate));
            var order = await Database.QuerySingleOrDefaultAsync<OrderDb>(query.Sql, query.Parameters, Transaction);

            if (order == null)
                return null;

            return order.ToDomain();
        }

        public async Task<int> UpdateOrder(IOrder order)
        {
            return await ExecuteInTransaction(async transaction =>
            {
                var previousStatus = await Database.QuerySingleOrDefaultAsync<string>(
                    "SELECT status FROM orders WHERE id = @Id FOR UPDATE;", new { order.Id }, transaction);
                if (previousStatus is null)
                    return 0;

                var newStatus = order.Status.ToString();
                if (previousStatus != newStatus)
                {
                    var latest = await Database.QuerySingleOrDefaultAsync<OrderStatusHistoryDb>(
                        """
                        SELECT sequence, new_status AS NewStatus, occurred_at AS OccurredAt
                        FROM order_status_history WHERE order_id = @Id
                        ORDER BY sequence DESC LIMIT 1;
                        """, new { order.Id }, transaction);
                    if (latest is null || latest.NewStatus != previousStatus)
                        throw new InvalidOperationException("O histórico da ordem não corresponde ao estado atual.");

                    var completed = order.Status == WorkOrderStatus.Finished && previousStatus == WorkOrderStatus.InExecution.ToString();
                    var occurredAt = completed
                        ? (order.DateFinished ?? throw new InvalidOperationException("A conclusão exige um instante válido.")).ToUniversalTime()
                        : DateTime.UtcNow;
                    if (occurredAt == DateTime.MinValue || occurredAt < latest.OccurredAt)
                        throw new InvalidOperationException("O instante da transição da ordem é inválido.");

                    string? reason = null;
                    if (order.Status == WorkOrderStatus.Finished)
                        reason = completed ? "ServiceCompleted" : "BudgetRejected";

                    await Database.ExecuteAsync(InsertHistorySql, new
                    {
                        EventId = Guid.NewGuid(),
                        OrderId = order.Id,
                        Sequence = checked(latest.Sequence + 1),
                        PreviousStatus = previousStatus,
                        NewStatus = newStatus,
                        OccurredAt = occurredAt,
                        Reason = reason
                    }, transaction);
                }

                return await Database.ExecuteAsync(UpdateOrderSql, new { order.Id, order.Budget, Status = newStatus }, transaction);
            });
        }

        private async Task<int> ExecuteInTransaction(Func<IDbTransaction, Task<int>> operation)
        {
            if (Transaction is not null)
                return await operation(Transaction);

            using var transaction = Database.BeginTransaction();
            try
            {
                var affected = await operation(transaction);
                Database.Commit(transaction);
                return affected;
            }
            catch
            {
                Database.Rollback(transaction);
                throw;
            }
        }

        public async Task<int> AddServiceToOrder(Guid orderId, IMechanicalService service)
        {
            return await Database.ExecuteAsync(AddServiceToOrderSql, new { Id = service.Id, orderId = orderId, Description = service.Description, Hours = service.Hours, PricePerHour = service.PricePerHour, Amount = service.Amount }, Transaction);
        }

        public async Task<int> UpdateServiceOfOrder(Guid orderId, IMechanicalService service)
        {
            return await Database.ExecuteAsync(UpdateServiceAmountOfOrderSql, new { Id = service.Id, orderId = orderId, Amount = service.Amount }, Transaction);
        }

        public async Task<int> RemoveServiceFromOrder(Guid orderId, Guid serviceId)
        {
            return await Database.ExecuteAsync(DeleteServiceFromOrderSql, new { orderId, serviceId }, Transaction);
        }

        public async Task<int> AddMaterialToOrder(Guid orderId, IMaterial material)
        {
            var materialDb = MaterialDb.Create(material);

            return await Database.ExecuteAsync(AddMaterialToOrderSql, new { Id = materialDb.Id, orderId = orderId, Name = materialDb.Name, Brand = materialDb.Brand, Price = materialDb.Price, Amount = materialDb.Amount }, Transaction);
        }

        public async Task<int> RemoveMaterialFromOrder(Guid orderId, Guid materialId)
        {
            return await Database.ExecuteAsync(RemoveMaterialFromOrderSql, new { orderId, materialId }, Transaction);
        }

        public async Task<int> UpdateMaterialFromOrder(Guid orderId, IMaterial material)
        {
            var materialDb = MaterialDb.Create(material);

            return await Database.ExecuteAsync(UpdateMaterialFromOrderSql, new { order_id = orderId, Id = materialDb.Id, Amount = materialDb.Amount }, Transaction);
        }

        public async Task<int> DeleteOrder(Guid orderId)
        {
            return await ExecuteInTransaction(async transaction =>
            {
                await Database.ExecuteAsync(DeleteServicesFromOrderSql, new { orderId }, transaction);
                await Database.ExecuteAsync(RemoveMaterialsFromOrderSql, new { orderId }, transaction);
                return await Database.ExecuteAsync(DeleteOrderSql, new { orderId }, transaction);
            });
        }

        private static Dictionary<string, object?> BuildQueryParameters(Guid? id = null, string customer_document = "", string vehicle_license_plate = "")
        {
            return new() { { "os." + nameof(id), id }, { "os." + nameof(customer_document), customer_document }, { "os." + nameof(vehicle_license_plate), vehicle_license_plate } };
        }
    }
}
