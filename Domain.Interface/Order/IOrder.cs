using Domain.Interface.Custumer;
using Domain.Interface.Service;
using Domain.Interface.Stock;
using Domain.Interface.Vehicle;

namespace Domain.Interface.Order
{
    public interface IOrder : IEntity
    {
        IDocument CustomerDocument { get; }
        ILicensePlate VehicleLicensePlate { get; }
        IReadOnlyCollection<IMechanicalService> Services { get; }
        IReadOnlyCollection<IMaterial> Materials { get; }
        decimal Budget { get; }
        WorkOrderStatus Status { get; }
        DateTime DateCreated { get; }
        DateTime DateFinished { get; }
        TimeSpan Duration { get; }

        void StartDiagnosis();
        IMechanicalService AddService(IMechanicalService serviceToAdd);
        IMechanicalService RemoveService(IMechanicalService serviceToRemove);
        IMaterial AddMaterial(IMaterial materialToAdd);
        IMaterial RemoveMaterial(IMaterial materialToRemove);
        void FinalizeDiagnosis();
        void ApproveService(bool approved);
        void StartService();
        void CompleteService(DateTime dateFinished);
        void DeliverVehicle();
    }
}
