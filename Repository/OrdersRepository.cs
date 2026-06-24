using Dapper;
using Domain.Interface.Order;
using Domain.Interface.Service;
using Domain.Interface.Stock;
using Repository.Dto;
using Repository.Interface;
using System.Data;

namespace Repository
{
    public class OrdersRepository(IDbConnection connection) : BaseRepository(connection), IOrdersRepository
    {
        public static string CreateServiceSql { get; private set; } = """
            INSERT INTO orders(id, customer_document, vehicle_license_plate, budget, status, date_created, date_finished, duration)
            VALUES (@Id, @Customer_Document, @Vehicle_License_Plate, @Budget, @Status, @Date_Created, @Date_Finished, @Duration);
            """;

        public static string GetOrdersSql { get; private set; } = """
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
                os.date_created, 
                os.date_finished 

            FROM orders os
            LEFT JOIN order_services s ON s.order_id = os.id
            LEFT JOIN order_materials m ON m.order_id = os.id

            {0}

            GROUP BY
                os.id,
                os.customer_document,
                os.vehicle_license_plate,
                os.budget,
                os.status,
                os.date_created,
                os.date_finished;
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

        public static string UpdateOrderBudgetSql { get; private set; } = """
            UPDATE orders
            SET budget = @budget
            WHERE id = @id;
            """;

        public static string UpdateOrderDurationSql { get; private set; } = """
            UPDATE orders
            SET duration = @duration
            WHERE id = @id;
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

        public async Task<int> CreateOrder(IOrder serviceOrder)
        {
            return await Connection.ExecuteAsync(CreateServiceSql, OrderDb.Create(serviceOrder));
        }

        public async Task<IEnumerable<IOrder>> GetOrders(Guid? id = null, string customer_document = "", string vehicle_license_plate = "")
        {
            var orders = await Connection.QueryAsync<OrderDb>(GetOrdersSql.BuildQuery(BuildQueryParameters(id, customer_document, vehicle_license_plate)));

            return orders.Select(order => order.ToDomain());
        }

        public async Task<IOrder?> GetOrder(Guid? id = null, string customer_document = "", string vehicle_license_plate = "")
        {
            var order = await Connection.QuerySingleOrDefaultAsync<OrderDb>(GetOrdersSql.BuildQuery(BuildQueryParameters(id, customer_document, vehicle_license_plate)));

            if (order == null)
                return null;

            return order.ToDomain();
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

        public async Task<int> RemoveServiceFromOrder(Guid orderId, Guid serviceId)
        {
            return await Connection.ExecuteAsync(DeleteServiceFromOrderSql, new { orderId, serviceId });
        }

        public async Task<int> AddMaterialToOrder(Guid orderId, IMaterial material)
        {
            var materialDb = MaterialDb.Create(material);

            return await Connection.ExecuteAsync(AddMaterialToOrderSql, new { Id = materialDb.Id, orderId = orderId, Name = materialDb.Name, Brand = materialDb.Brand, Price = materialDb.Price, Amount = materialDb.Amount });
        }

        public async Task<int> RemoveMaterialFromOrder(Guid orderId, Guid materialId)
        {
            return await Connection.ExecuteAsync(RemoveMaterialFromOrderSql, new { orderId, materialId });
        }

        public async Task<int> UpdateMaterialFromOrder(Guid orderId, IMaterial material)
        {
            var materialDb = MaterialDb.Create(material);

            return await Connection.ExecuteAsync(UpdateMaterialFromOrderSql, new { order_id = orderId, Id = materialDb.Id, Amount = materialDb.Amount });
        }

        public async Task<int> UpdateOrderBudget(Guid id, double budget)
        {
            return await Connection.ExecuteAsync(UpdateOrderBudgetSql, new { id, budget });
        }

        public async Task<int> UpdateOrderDuration(Guid id, TimeSpan duration)
        {
            return await Connection.ExecuteAsync(UpdateOrderDurationSql, new { id, duration });
        }

        public async Task<int> DeleteOrder(Guid orderId)
        {
            await Connection.ExecuteAsync(DeleteServicesFromOrderSql, new { orderId });

            await Connection.ExecuteAsync(RemoveMaterialsFromOrderSql, new { orderId });

            return await Connection.ExecuteAsync(DeleteOrderSql, new { orderId });
        }

        private static Dictionary<string, object?> BuildQueryParameters(Guid? id = null, string customer_document = "", string vehicle_license_plate = "")
        {
            return new() { { "os." + nameof(id), id }, { "os." + nameof(customer_document), customer_document }, { "os." + nameof(vehicle_license_plate), vehicle_license_plate } };
        }
    }
}
