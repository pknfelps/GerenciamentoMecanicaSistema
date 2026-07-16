using Domain.Customer;
using Domain.Interface.Custumer;
using Domain.Interface.Exceptions;
using Domain.Interface.Order;
using Domain.Interface.Service;
using Domain.Interface.Stock;
using Domain.Interface.Vehicle;
using Domain.Vehicle;

namespace Domain.WorkOrder
{
    public class Order : IOrder
    {
        public Guid Id { get; private set; }
        public IDocument CustomerDocument { get; private set; }
        public ILicensePlate VehicleLicensePlate { get; private set; }
        public IReadOnlyCollection<IMechanicalService> Services => services.AsReadOnly();
        public IReadOnlyCollection<IMaterial> Materials => materials.AsReadOnly();
        public decimal Budget { get; private set; } = 0.0m;
        public WorkOrderStatus Status { get; private set; }
        public DateTime DateCreated { get; private set; }
        public DateTime DateFinished { get; private set; }
        public TimeSpan Duration => DateFinished != DateTime.MinValue ? DateFinished.Subtract(DateCreated) : TimeSpan.Zero;

        private readonly List<IMechanicalService> services;
        private readonly List<IMaterial> materials;

        public Order(string customerDocument, string vehicleLicensePlate, DateTime dateCreated) : this(Guid.NewGuid(), customerDocument, vehicleLicensePlate, [], [], 0.0m, WorkOrderStatus.Received, dateCreated, DateTime.MinValue) { }

        public Order(Guid id, string customerDocument, string vehicleLicensePlate, List<IMechanicalService> services, List<IMaterial> materials, decimal budget, WorkOrderStatus status, DateTime dateCreated, DateTime dateFinished)
        {
            if (id == Guid.Empty)
                throw new DomainValidationException("Id não pode ser vazio");

            if (string.IsNullOrEmpty(customerDocument))
                throw new DomainValidationException("Documento do cliente deve ser preenchido");

            if (string.IsNullOrEmpty(vehicleLicensePlate))
                throw new DomainValidationException("Placa do veículo deve ser preenchida");

            if (budget < 0.0m)
                throw new DomainValidationException("Orçamento não pode ser negativo");

            Id = id;
            CustomerDocument = DocumentWrapper.CreateDocument(customerDocument);
            VehicleLicensePlate = LicensePlateWrapper.CreateLicensePlate(vehicleLicensePlate);
            this.services = services;
            this.materials = materials;
            Budget = budget;
            Status = status;
            DateCreated = dateCreated;
            DateFinished = dateFinished;
        }

        public void StartDiagnosis()
        {
            if (Status is not WorkOrderStatus.Received)
                throw new InvalidDomainStateException("Só é possível iniciar o diagnóstico após o recebimento da ordem");

            Status = WorkOrderStatus.InDiagnosis;
        }

        public IMechanicalService AddService(IMechanicalService serviceToAdd)
        {
            if (Status is WorkOrderStatus.Received)
                throw new InvalidDomainStateException("Não é possível adicionar serviços antes de iniciar o diagnóstico");

            if (Status is WorkOrderStatus.InExecution or WorkOrderStatus.Finished or WorkOrderStatus.Delivered)
                throw new InvalidDomainStateException("Não é possível adicionar serviços após o inicio do serviço");

            var service = services.FirstOrDefault(s => s.Id == serviceToAdd.Id);

            if (service == null)
            {
                services.Add(serviceToAdd);

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
                throw new InvalidDomainStateException("Não é possível adicionar serviços antes de iniciar o diagnóstico");

            if (Status is WorkOrderStatus.InExecution or WorkOrderStatus.Finished or WorkOrderStatus.Delivered)
                throw new InvalidDomainStateException("Não é possível adicionar serviços após o inicio do serviço");

            var service = services.First(x => x.Id == serviceToRemove.Id);

            service.RemoveServiceAmount(serviceToRemove.Amount);

            if (service.Amount == 0)
                services.Remove(serviceToRemove);

            return service;
        }

        public IMaterial AddMaterial(IMaterial materialToAdd)
        {
            if (Status is WorkOrderStatus.Received)
                throw new InvalidDomainStateException("Não é possível adicionar peças ou insumos antes de iniciar o diagnóstico");

            if (Status is WorkOrderStatus.InExecution or WorkOrderStatus.Finished or WorkOrderStatus.Delivered)
                throw new InvalidDomainStateException("Não é possível adicionar peças ou insumos após o inicio do serviço");

            var material = materials.FirstOrDefault(x => x.Id == materialToAdd.Id);

            if (material == null)
            {
                materials.Add(materialToAdd);

                return materialToAdd;
            }
            else
            {
                material.AddAmount(materialToAdd.Amount);

                return material;
            }
        }

        public IMaterial RemoveMaterial(IMaterial materialToRemove)
        {
            if (Status is WorkOrderStatus.Received)
                throw new InvalidDomainStateException("Não é possível remover peças ou insumos antes de iniciar o diagnóstico");

            if (Status is WorkOrderStatus.InExecution or WorkOrderStatus.Finished or WorkOrderStatus.Delivered)
                throw new InvalidDomainStateException("Não é possível remover peças ou insumos após o inicio do serviço");

            var material = materials.First(x => x.Id == materialToRemove.Id);

            material.RemoveAmount(materialToRemove.Amount);

            if (material.Amount == 0)
                materials.Remove(material);

            return material;
        }

        public void FinalizeDiagnosis()
        {
            if (Status is not WorkOrderStatus.InDiagnosis)
                throw new InvalidDomainStateException("Só é possível finalizar o diagnóstico enquanto a ordem Em Diagnósotico");

            if (services.Count <= 0)
                throw new DomainBusinessRuleException("Não é possível finalizar o diagnóstico sem serviços");

            CalculateBudget();
            Status = WorkOrderStatus.WaitingForApproval;
        }

        public void ApproveService(bool approved)
        {
            if (Status is not WorkOrderStatus.WaitingForApproval)
                throw new InvalidDomainStateException("Não é possível aprovar ou recusar o serviço enquanto não estiver em estado de aprovação");

            Status = approved ? WorkOrderStatus.WaitingForExecution : WorkOrderStatus.Finished;
        }

        public void StartService()
        {
            if (Status is not WorkOrderStatus.WaitingForExecution)
                throw new InvalidDomainStateException("Não é possível iniciar o serviço enquanto não estiver aguardando execução");

            Status = WorkOrderStatus.InExecution;
        }

        public void CompleteService(DateTime dateFinished)
        {
            if (Status is not WorkOrderStatus.InExecution)
                throw new InvalidDomainStateException("Não é possível finalizar o serviço enquanto não estiver em execução");

            DateFinished = dateFinished;
            Status = WorkOrderStatus.Finished;
        }

        public void DeliverVehicle()
        {
            if (Status is not WorkOrderStatus.Finished)
                throw new InvalidDomainStateException("Não é possível entregar o veículo enquanto não estiver finalizado");

            Status = WorkOrderStatus.Delivered;
        }

        private void CalculateBudget()
        {
            decimal value = 0.0m;

            foreach (var service in services)
                value += service.Price * service.Amount;

            foreach (var material in materials)
                value += material.Price * material.Amount;

            Budget = value;
        }
    }
}