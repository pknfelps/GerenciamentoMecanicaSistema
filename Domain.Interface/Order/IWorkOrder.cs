using Domain.Interface.Custumer;
using Domain.Interface.Service;
using Domain.Interface.Stock;
using Domain.Interface.Vehicle;

namespace Domain.Interface.Order
{
    public interface IWorkOrder : IEntity
    {
        IDocument CustomerDocument { get; }
        ILicensePlate VehicleLicensePlate { get; }
        List<IMechanicalService> Services { get; }
        List<IPart> Parts { get; }
        double Budget { get; }
        WorkOrderStatus Status { get; }
        DateTime DateCreated { get; }
        DateTime DateFinished { get; }

        void StartDiagnosis();
        IMechanicalService AddService(IMechanicalService serviceToAdd);
        IMechanicalService RemoveService(IMechanicalService serviceToRemove);
        IPart AddPartOrSupplie(IPart itemToAdd);
        IPart RemovePartOrSupplie(IPart itemToRemove);
        void FinalizeDiagnosis();
        void ApproveService(bool approved);
        void StartService();
        void CompleteService();
        void VehicleDelivered();
    }
}
