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
        public WorkOrderStatus Status { get; private set; }
        public DateTime DateCreated { get; private set; }
        public DateTime DateFinished { get; private set; }
        public TimeSpan Duration => DateFinished != DateTime.MinValue ? DateFinished.Subtract(DateCreated) : TimeSpan.Zero;

        public WorkOrder(string customerDocument, string vehicleLicensePlate) : this(Guid.NewGuid(), customerDocument, vehicleLicensePlate, [], [], 0.0, WorkOrderStatus.Received, DateTime.Now, DateTime.MinValue) { }

        public WorkOrder(Guid id, string customerDocument, string vehicleLicensePlate, List<IMechanicalService> services, List<IPart> partsAndSupplies, double budget, WorkOrderStatus status, DateTime dateCreated, DateTime dateFinished)
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
            if (Status is not WorkOrderStatus.Received)
                throw new InvalidOperationException("Só é possível iniciar o diagnóstico após o recebimento da ordem");

            Status = WorkOrderStatus.InDiagnosis;
        }

        public IMechanicalService AddService(IMechanicalService serviceToAdd)
        {
            if (Status is WorkOrderStatus.Received)
                throw new InvalidOperationException("Não é possível adicionar serviços antes de iniciar o diagnóstico");

            if (Status is WorkOrderStatus.InExecution or WorkOrderStatus.Finished or WorkOrderStatus.Delivered)
                throw new InvalidOperationException("Não é possível adicionar serviços após o inicio do serviço");

            var service = Services.FirstOrDefault(s => s.Id == serviceToAdd.Id);

            if (service == null)
            {
                Services.Add(serviceToAdd);

                return serviceToAdd;
            }
            else
            {
                service.AddServiceAmount(serviceToAdd.Amount);

                return service;
            }
        }

        public IMechanicalService RemoveService(IMechanicalService serviceToRemove)
        {
            if (Status is WorkOrderStatus.Received)
                throw new InvalidOperationException("Não é possível adicionar serviços antes de iniciar o diagnóstico");

            if (Status is WorkOrderStatus.InExecution or WorkOrderStatus.Finished or WorkOrderStatus.Delivered)
                throw new InvalidOperationException("Não é possível adicionar serviços após o inicio do serviço");

            var service = Services.First(x => x.Id == serviceToRemove.Id);

            service.RemoveServiceAmount(serviceToRemove.Amount);

            if (service.Amount == 0)
                Services.Remove(serviceToRemove);

            return service;
        }

        public IPart AddPartOrSupplie(IPart itemToAdd)
        {
            if (Status is WorkOrderStatus.Received)
                throw new InvalidOperationException("Não é possível adicionar peças ou insumos antes de iniciar o diagnóstico");

            if (Status is WorkOrderStatus.InExecution or WorkOrderStatus.Finished or WorkOrderStatus.Delivered)
                throw new InvalidOperationException("Não é possível adicionar peças ou insumos após o inicio do serviço");

            var item = Parts.FirstOrDefault(x => x.Id == itemToAdd.Id);

            if (item == null)
            {
                Parts.Add(itemToAdd);

                return itemToAdd;
            }
            else
            {
                item.AddAmount(itemToAdd.Amount);

                return item;
            }
        }

        public IPart RemovePartOrSupplie(IPart itemToRemove)
        {
            if (Status is WorkOrderStatus.Received)
                throw new InvalidOperationException("Não é possível remover peças ou insumos antes de iniciar o diagnóstico");

            if (Status is WorkOrderStatus.InExecution or WorkOrderStatus.Finished or WorkOrderStatus.Delivered)
                throw new InvalidOperationException("Não é possível remover peças ou insumos após o inicio do serviço");

            var item = Parts.First(x => x.Id == itemToRemove.Id);

            item.RemoveAmount(itemToRemove.Amount);

            if (item.Amount == 0)
                Parts.Remove(item);

            return item;
        }

        public void FinalizeDiagnosis()
        {
            if (Status is not WorkOrderStatus.InDiagnosis)
                throw new InvalidOperationException("Só é possível finalizar o diagnóstico enquanto a ordem Em Diagnósotico");

            if (Services.Count <= 0)
                throw new InvalidOperationException("Não é possível finalizar o diagnóstico sem serviços");

            CalculateBudget();
            // TODO: Envia para o cliente
            Status = WorkOrderStatus.WaitingForApproval;
        }

        public void ApproveService(bool approved)
        {
            if (Status is not WorkOrderStatus.WaitingForApproval)
                throw new InvalidOperationException("Não é possível aprovar ou recusar o serviço enquanto não estiver em estado de aprovação");

            Status = approved ? WorkOrderStatus.WaitingForExecution : WorkOrderStatus.Finished;
        }

        public void StartService()
        {
            if (Status is not WorkOrderStatus.WaitingForExecution)
                throw new InvalidOperationException("Não é possível iniciar o serviço enquanto não estiver aguardando execução");

            Status = WorkOrderStatus.InExecution;
        }

        public void CompleteService()
        {
            if (Status is not WorkOrderStatus.InExecution)
                throw new InvalidOperationException("Não é possível finalizar o serviço enquanto não estiver em execução");

            DateFinished = DateTime.Now;
            Status = WorkOrderStatus.Finished;
        }

        public void VehicleDelivered()
        {
            if (Status is not WorkOrderStatus.Finished)
                throw new InvalidOperationException("Não é possível entregar o veículo enquanto não estiver finalizado");

            Status = WorkOrderStatus.Delivered;
        }

        private void CalculateBudget()
        {
            double value = 0.0;

            foreach (var service in Services)
                value += service.Price * service.Amount;

            foreach (var item in Parts)
                value += item.Price * item.Amount;

            Budget = value;
        }
    }
}
