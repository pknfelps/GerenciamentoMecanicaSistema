using Service.Interface.Dto.CustomAttributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GerenciamentoMecanicaSistema.Contracts.Requests.Stock
{
    public class ValueUpdateRequest<T>(T value)
    {
        [Description("Quantidade a atualizar do item")]
        [Required, GenericValueValidation]
        public T Value { get; set; } = value;
    }
}
