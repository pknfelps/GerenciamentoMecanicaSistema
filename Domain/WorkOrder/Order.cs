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

        public Order(
            string customerDocument,
            string vehicleLicensePlate,
            IEnumerable<IMechanicalService> services,
            IEnumerable<IMaterial> materials,
            DateTime dateCreated) : this(
                Guid.NewGuid(),
                customerDocument,
                vehicleLicensePlate,
                ValidateServices(services),
                ValidateMaterials(materials),
                0.0m,
                WorkOrderStatus.Received,
                dateCreated,
                DateTime.MinValue)
        { }

        public Order(Guid id, string customerDocument, string vehicleLicensePlate, List<IMechanicalService> services, List<IMaterial> materials, decimal budget, WorkOrderStatus status, DateTime dateCreated, DateTime dateFinished)
        {
            if (id == Guid.Empty)
                throw new DomainValidationException("O ID da ordem não pode ser vazio");

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
            EnsureDiagnosisInProgress("adicionar serviços");

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
            EnsureDiagnosisInProgress("remover serviços");

            var service = services.First(x => x.Id == serviceToRemove.Id);

            service.RemoveServiceAmount(serviceToRemove.Amount);

            if (service.Amount == 0)
                services.Remove(service);

            return service;
        }

        public IMaterial AddMaterial(IMaterial materialToAdd)
        {
            EnsureDiagnosisInProgress("adicionar peças ou insumos");

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
            EnsureDiagnosisInProgress("remover peças ou insumos");

            var material = materials.First(x => x.Id == materialToRemove.Id);

            material.RemoveAmount(materialToRemove.Amount);

            if (material.Amount == 0)
                materials.Remove(material);

            return material;
        }

        public void FinalizeDiagnosis()
        {
            if (Status is not WorkOrderStatus.InDiagnosis)
                throw new InvalidDomainStateException("Só é possível finalizar o diagnóstico enquanto a ordem estiver em diagnóstico");

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

        private static List<IMechanicalService> ValidateServices(IEnumerable<IMechanicalService> services)
        {
            if (services == null)
                throw new DomainValidationException("A lista de serviços deve ser informada");

            var serviceList = services.ToList();

            if (serviceList.Any(service => service.Amount <= 0))
                throw new DomainValidationException("As quantidades dos serviços devem ser maiores que zero");

            if (serviceList.Select(service => service.Id).Distinct().Count() != serviceList.Count)
                throw new DomainValidationException("A ordem não pode conter serviços duplicados");

            return serviceList;
        }

        private static List<IMaterial> ValidateMaterials(IEnumerable<IMaterial> materials)
        {
            if (materials == null)
                throw new DomainValidationException("A lista de materiais deve ser informada");

            var materialList = materials.ToList();

            if (materialList.Any(material => material.Amount <= 0))
                throw new DomainValidationException("As quantidades dos materiais devem ser maiores que zero");

            if (materialList.Select(material => material.Id).Distinct().Count() != materialList.Count)
                throw new DomainValidationException("A ordem não pode conter materiais duplicados");

            return materialList;
        }

        private void EnsureDiagnosisInProgress(string operation)
        {
            if (Status is not WorkOrderStatus.InDiagnosis)
                throw new InvalidDomainStateException($"Só é possível {operation} durante o diagnóstico");
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
