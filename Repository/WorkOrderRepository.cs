using Dapper;
using Domain.Interface.Order;
using Domain.Interface.Service;
using Domain.Interface.Stock;
using Repository.Dto;
using Repository.Interface;
using System.Data;

namespace Repository
{
    public class WorkOrderRepository(IDbConnection connection) : BaseRepository(connection), IWorkOrderRepository
    {
        public static string CreateServiceSql { get; private set; } = """
            INSERT INTO orders(id, customer_document, vehicle_license_plate, budget, status, date_created, date_finished)
            VALUES (@Id, @Customer_Document, @Vehicle_License_Plate, @Budget, @Status, @Date_Created, @Date_Finished);
            """;

        public static string GetOrdersSql { get; private set; } = """
            SELECT id, customer_document, vehicle_license_plate, budget, status, date_created, date_finished 
            FROM orders
            LIMIT 50;
            """;

        public static string GetOrderSql { get; private set; } = """
            SELECT 
                os.id, 
                os.customer_document, 
                os.vehicle_license_plate, 

                CASE
                    WHEN COUNT(s.id) = 0 THEN '[]'::json
                    ELSE json_agg(jsonb_build_object(
                        'id', s.id,
                        'description', s.description,
                        'hours', s.hours,
                        'price_per_hour', s.price_per_hour,
                        'amount', s.amount))
                END AS services,

                CASE
                    WHEN COUNT(i.id) = 0 THEN '[]'::json
                    ELSE json_agg(DISTINCT jsonb_build_object(
                        'id', i.id,
                        'name', i.name,
                        'brand', i.brand,
                        'price', i.price,
                        'amount', i.amount))
                    END AS parts,

                os.budget, 
                os.status, 
                os.date_created, 
                os.date_finished 

            FROM orders os
            LEFT JOIN order_services s ON s.order_id = os.id
            LEFT JOIN order_items i ON i.order_id = os.id

            WHERE os.id = @orderId

            GROUP BY
                os.id,
                os.customer_document,
                os.vehicle_license_plate,
                os.budget,
                os.status,
                os.date_created,
                os.date_finished;
            """;

        public static string GetCustomerOrdersSql { get; private set; } = """
            SELECT 
                os.id, 
                os.customer_document, 
                os.vehicle_license_plate, 

                CASE
                    WHEN COUNT(s.id) = 0 THEN '[]'::json
                    ELSE json_agg(jsonb_build_object(
                        'id', s.id,
                        'description', s.description,
                        'hours', s.hours,
                        'price_per_hour', s.price_per_hour,
                        'amount', s.amount))
                END AS services,

                CASE
                    WHEN COUNT(i.id) = 0 THEN '[]'::json
                    ELSE json_agg(DISTINCT jsonb_build_object(
                        'id', i.id,
                        'name', i.name,
                        'brand', i.brand,
                        'price', i.price,
                        'amount', i.amount))
                    END AS parts,

                os.budget, 
                os.status, 
                os.date_created, 
                os.date_finished 

            FROM orders os
            LEFT JOIN order_services s ON s.order_id = os.id
            LEFT JOIN order_items i ON i.order_id = os.id

            WHERE os.customer_document = @customerDocument

            GROUP BY
                os.id,
                os.customer_document,
                os.vehicle_license_plate,
                os.budget,
                os.status,
                os.date_created,
                os.date_finished;
            """;

        public static string GetServiceFromOrderSql { get; private set; } = """
            SELECT id, order_id, description, hours, price_per_hour, amount
            FROM order_services
            WHERE id = @service_Id AND order_id = @order_Id;
            """;

        public static string UpdateOrderStatusSql { get; private set; } = """
            UPDATE orders
            SET status = @Status
            WHERE id = @Id;
            """;

        public static string AddServiceToOrderSql { get; private set; } = """
            INSERT INTO order_services(id, order_id, description, hours, price_per_hour, amount)
            VALUES (@Id, @orderId, @Description, @Hours, @Price_Per_Hour, @Amount);
            """;

        public static string UpdateServiceAmountOfOrderSql { get; private set; } = """
            UPDATE order_services
            SET amount = @Amount
            WHERE id = @Id AND order_id = @orderId;
            """;

        public static string AddPartToOrderSql { get; private set; } = """
            INSERT INTO order_items(id, order_id, name, brand, price, amount)
            VALUES (@Id, @orderId, @Name, @Brand, @Price, @Amount);
            """;

        public static string UpdatePartFromOrderSql { get; private set; } = """
            UPDATE order_items
            SET amount = @Amount
            WHERE id = @Id AND order_id = @order_id;
            """;

        public static string DeleteServiceFromOrderSql { get; private set; } = """
            DELETE FROM order_services
            WHERE id = @serviceId AND order_id = @orderId;
            """;

        public static string RemovePartFromOrderSql { get; private set; } = """
            DELETE FROM order_items
            WHERE id = @partId AND order_id = @orderId;
            """;

        public static string UpdateOrderBudgetSql { get; private set; } = """
            UPDATE orders
            SET budget = @budget
            WHERE id = @id;
            """;

        public static string DeleteServicesFromOrderSql { get; private set; } = """
            DELETE FROM order_services
            WHERE order_id = @orderId;
            """;

        public static string RemovePartsFromOrderSql { get; private set; } = """
            DELETE FROM order_items
            WHERE order_id = @orderId;
            """;

        public static string DeleteOrderSql { get; private set; } = """
            DELETE FROM orders
            WHERE id = @orderId;
            """;

        public async Task<int> CreateOrder(IWorkOrder serviceOrder)
        {
            return await Connection.ExecuteAsync(CreateServiceSql, WorkOrderDb.Create(serviceOrder));
        }

        public async Task<IEnumerable<IWorkOrder?>> GetOrders()
        {
            var orders = await Connection.QueryAsync<WorkOrderDb>(GetOrdersSql);

            return orders.Select(order => order.ToDomain());
        }

        public async Task<IWorkOrder?> GetOrder(Guid orderId)
        {
            var order = await Connection.QuerySingleOrDefaultAsync<WorkOrderDb>(GetOrderSql, new { orderId });

            if (order == null)
                return null;

            return order.ToDomain();
        }

        public async Task<IEnumerable<IWorkOrder?>> GetCustomerOrders(string customerDocument)
        {
            var orders = await Connection.QueryAsync<WorkOrderDb>(GetCustomerOrdersSql, new { customerDocument });

            return orders.Select(order => order.ToDomain());
        }

        public async Task<int> UpdateOrderStatus(Guid orderId, WorkOrderStatus status)
        {
            return await Connection.ExecuteAsync(UpdateOrderStatusSql, new { Id = orderId, Status = status.ToString() });
        }

        public async Task<int> AddServiceToOrder(Guid orderId, IMechanicalService service)
        {
            return await Connection.ExecuteAsync(AddServiceToOrderSql, new { Id = service.Id, orderId = orderId, Description = service.Description, Hours = service.Hours, Price_Per_Hour = service.PricePerHour, Amount = service.Amount });
        }

        public async Task<int> UpdateServiceOfOrder(Guid orderId, IMechanicalService service)
        {
            return await Connection.ExecuteAsync(UpdateServiceAmountOfOrderSql, new { Id = service.Id, orderId = orderId, Amount = service.Amount });
        }

        public async Task<int> DeleteServiceFromOrder(Guid orderId, Guid serviceId)
        {
            return await Connection.ExecuteAsync(DeleteServiceFromOrderSql, new { orderId, serviceId });
        }

        public async Task<int> AddPartToOrder(Guid orderId, IPart part)
        {
            var partDb = PartDb.Create(part);

            return await Connection.ExecuteAsync(AddPartToOrderSql, new { Id = partDb.Id, orderId = orderId, Name = partDb.Name, Brand = partDb.Brand, Price = partDb.Price, Amount = partDb.Amount });
        }

        public async Task<int> RemovePartFromOrder(Guid orderId, Guid partId)
        {
            return await Connection.ExecuteAsync(RemovePartFromOrderSql, new { orderId, partId });
        }

        public async Task<int> UpdatePartFromOrder(Guid orderId, IPart part)
        {
            var partDb = PartDb.Create(part);

            return await Connection.ExecuteAsync(UpdatePartFromOrderSql, new { order_id = orderId, Id = partDb.Id, Amount = partDb.Amount });
        }

        public async Task<int> UpdateOrderBudget(Guid id, double budget)
        {
            return await Connection.ExecuteAsync(UpdateOrderBudgetSql, new { id, budget });
        }

        public async Task<int> DeleteOrder(Guid orderId)
        {
            await Connection.ExecuteAsync(DeleteServicesFromOrderSql, new { orderId });

            await Connection.ExecuteAsync(RemovePartsFromOrderSql, new { orderId });

            return await Connection.ExecuteAsync(DeleteOrderSql, new { orderId });
        }
    }
}
