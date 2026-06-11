using Service.Interface.Dto.Stock;

namespace Service.Interface
{
    public interface IStockService
    {
        Task RegisterNewPart(CreatePartDto partDto);
        Task AddPartAmount(PartUpdateDto<int> partDto);
        Task RemovePartAmount(PartUpdateDto<int> partDto);
        Task ReservePartAmount(PartUpdateDto<int> partDto);
        Task RestorePartAmount(PartUpdateDto<int> partDto);
        Task ConsumeReservedAmount(PartUpdateDto<int> partDto);
        Task<IEnumerable<PartDto?>> GetParts();
        Task<PartDto?> GetPart(Guid partId);
        Task<PartDto?> GetPart(string name, string brand);
        Task UpdatePartPrice(PartUpdateDto<double> partDto);
        Task DeletePart(string name, string brand);
    }
}
