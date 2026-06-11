using Domain.Customer;
using Domain.Interface.Custumer;
using Domain.Interface.Order;
using Domain.Interface.Service;
using Domain.Interface.Stock;
using Domain.Interface.Vehicle;
using Domain.Vehicle;

namespace Domain.Order
{
    public class WorkOrder : IWorkOrder
    {
        public Guid Id { get; private set; }
        public IDocument CustomerDocument { get; private set; }
        public ILicensePlate VehicleLicensePlate { get; private set; }
        public List<IMechanicalService> Services { get; private set; }
        public List<IPart> Parts { get; private set; }
        public double Budget { get; private set; } = 0.0;
        public ServiceOrderStatus Status { get; private set; }
        public DateTime DateCreated { get; private set; }
        public DateTime DateFinished { get; private set; }

        public WorkOrder(string customerDocument, string vehicleLicensePlate) : this(Guid.NewGuid(), customerDocument, vehicleLicensePlate, [], [], 0.0, ServiceOrderStatus.Received, DateTime.Now, DateTime.MinValue) { }

        public WorkOrder(Guid id, string customerDocument, string vehicleLicensePlate, List<IMechanicalService> services, List<IPart> partsAndSupplies, double budget, ServiceOrderStatus status, DateTime dateCreated, DateTime dateFinished)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Id não pode ser vazio");

            ArgumentException.ThrowIfNullOrEmpty(customerDocument);
            ArgumentException.ThrowIfNullOrEmpty(vehicleLicensePlate);

            if (budget < 0.0)
                throw new ArgumentException("Orçamento não pode ser negativo");

            Id = id;
            CustomerDocument = DocumentWrapper.CreateDocument(customerDocument);
            VehicleLicensePlate = LicensePlateWrapper.CreateLicensePlate(vehicleLicensePlate);
            Services = services;
            Parts = partsAndSupplies;
            Budget = budget;
            Status = status;
            DateCreated = dateCreated;
            DateFinished = dateFinished;
        }

        public void StartDiagnosis()
        {
            if (Status is not ServiceOrderStatus.Received)
                throw new InvalidOperationException("Só é possível iniciar o diagnóstico após o recebimento da ordem");

            Status = ServiceOrderStatus.InDiagnosis;
        }

        public void AddService(IMechanicalService serviceToAdd)
        {
            if (Status is ServiceOrderStatus.Received)
                throw new InvalidOperationException("Não é possível adicionar serviços antes de iniciar o diagnóstico");

            if (Status is ServiceOrderStatus.InExecution or ServiceOrderStatus.Finished or ServiceOrderStatus.Delivered)
                throw new InvalidOperationException("Não é possível adicionar serviços após o inicio do serviço");

            Services.Add(serviceToAdd);
        }

        public void RemoveService(IMechanicalService serviceToRemove)
        {
            if (Status is ServiceOrderStatus.Received)
                throw new InvalidOperationException("Não é possível adicionar serviços antes de iniciar o diagnóstico");

            if (Status is ServiceOrderStatus.InExecution or ServiceOrderStatus.Finished or ServiceOrderStatus.Delivered)
                throw new InvalidOperationException("Não é possível adicionar serviços após o inicio do serviço");

            _ = Services.FirstOrDefault(x => x.Id == serviceToRemove.Id) ?? throw new InvalidOperationException("Serviço não encontrado na ordem");

            Services.Remove(serviceToRemove);
        }

        public IPart AddPartOrSupplie(IPart itemToAdd)
        {
            if (Status is ServiceOrderStatus.Received or ServiceOrderStatus.InExecution or ServiceOrderStatus.Finished or ServiceOrderStatus.Delivered)
                throw new InvalidOperationException("Não é possível adicionar peças ou insumos após o inicio do serviço");

            var item = Parts.FirstOrDefault(x => x.Name == itemToAdd.Name && x.Brand == itemToAdd.Brand);

            if (item == null)
            {
                Parts.Add(itemToAdd);

                item = itemToAdd;
            }
            else
                item.AddAmount(itemToAdd.Amount);

            return item;
        }

        public IPart RemovePartOrSupplie(IPart itemToRemove)
        {
            if (Status is ServiceOrderStatus.Received or ServiceOrderStatus.InExecution or ServiceOrderStatus.Finished or ServiceOrderStatus.Delivered)
                throw new InvalidOperationException("Não é possível adicionar peças ou insumos após o inicio do serviço");

            var item = Parts.FirstOrDefault(x => x.Name == itemToRemove.Name && x.Brand == itemToRemove.Brand) ?? throw new InvalidOperationException("Item não encontrtado na ordem");

            item.RemoveAmount(itemToRemove.Amount);

            if (item.Amount == 0)
            {
                Parts.Remove(item);
            }

            return item;
        }

        public void FinalizeDiagnosis()
        {
            if (Status is not ServiceOrderStatus.InDiagnosis)
                throw new InvalidOperationException("Só é possível finalizar o diagnóstico enquanto a ordem Em Diagnósotico");

            if (Services.Count <= 0)
                throw new InvalidOperationException("Não é possível finalizar o diagnóstico sem serviços");

            CalculateBudget();
            // TODO: Envia para o cliente
            Status = ServiceOrderStatus.WaitingForApproval;
        }

        public void ApproveService(bool approved)
        {
            if (Status is not ServiceOrderStatus.WaitingForApproval)
                throw new InvalidOperationException("Não é possível aprovar ou recusar o serviço enquanto não estiver em estado de aprovação");

            Status = approved ? ServiceOrderStatus.WaitingForExecution : ServiceOrderStatus.Finished;
        }

        public void StartService()
        {
            if (Status is not ServiceOrderStatus.WaitingForExecution)
                throw new InvalidOperationException("Não é possível iniciar o serviço enquanto não estiver aguardando execução");

            Status = ServiceOrderStatus.InExecution;
        }

        public void CompleteService()
        {
            if (Status is not ServiceOrderStatus.InExecution)
                throw new InvalidOperationException("Não é possível finalizar o serviço enquanto não estiver em execução");

            // TODO: Atualiza estoque ("consome" itens que estavam reservados)
            DateFinished = DateTime.Now;
            Status = ServiceOrderStatus.Finished;
        }

        public void VehicleDelivered()
        {
            if (Status is not ServiceOrderStatus.Finished)
                throw new InvalidOperationException("Não é possível entregar o veículo enquanto não estiver finalizado");

            Status = ServiceOrderStatus.Delivered;
        }

        private void CalculateBudget()
        {
            double value = 0.0;

            foreach (var service in Services)
                value += service.Price;

            foreach (var item in Parts)
                value += item.Price * item.Amount;

            Budget = value;
        }
    }
}
