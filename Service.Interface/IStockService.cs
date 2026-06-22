using Service.Interface.Dto.Stock;

namespace Service.Interface
{
    public interface IStockService
    {
        Task RegisterNewPart(CreatePartDto partDto);
        Task<IEnumerable<PartDto>> GetParts(Guid? id = null, string name = "", string brand = "");
        Task<PartDto?> GetPart(Guid? id = null, string name = "", string brand = "");
        Task AddPartAmount(Guid id, int value);
        Task RemovePartAmount(Guid id, int value);
        Task ReservePartAmount(Guid id, int value);
        Task RestorePartAmount(Guid id, int value);
        Task ConsumeReservedAmount(Guid id, int value);
        Task UpdatePartPrice(Guid id, double value);
        Task DeletePart(Guid id);
    }
}
