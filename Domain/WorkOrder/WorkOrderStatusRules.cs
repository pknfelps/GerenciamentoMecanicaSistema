using Domain.Interface.Exceptions;
using Domain.Interface.Order;

namespace Domain.WorkOrder
{
    public static class WorkOrderStatusRules
    {
        public static WorkOrderStatus Parse(string value)
        {
            if (!Enum.TryParse<WorkOrderStatus>(value, out var status)
                || !Enum.IsDefined(status) || value != status.ToString())
                throw new DomainValidationException("O status da ordem é inválido");

            return status;
        }
    }
}
