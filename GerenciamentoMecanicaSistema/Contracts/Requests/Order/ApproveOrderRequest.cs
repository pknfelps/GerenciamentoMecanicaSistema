using Service.Interface.Commands.Order;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GerenciamentoMecanicaSistema.Contracts.Requests.Order;

[JsonConverter(typeof(ApproveOrderRequestConverter))]
public sealed class ApproveOrderRequest(bool approved)
{
    [Description("Decisão explícita do cliente sobre o orçamento")]
    [Required, JsonRequired]
    public bool Approved { get; } = approved;
    public ApproveOrderCommand ToCommand() => new(Approved);
}
