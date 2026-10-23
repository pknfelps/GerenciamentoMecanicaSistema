using System.Text.Json;
using System.Text.Json.Serialization;

namespace GerenciamentoMecanicaSistema.Contracts.Requests.Order;

public sealed class ApproveOrderRequestConverter : JsonConverter<ApproveOrderRequest>
{
    public override ApproveOrderRequest Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var json = JsonDocument.ParseValue(ref reader);
        if (json.RootElement.ValueKind != JsonValueKind.Object) throw Invalid();
        var fields = json.RootElement.EnumerateObject().ToArray();
        if (fields.Length != 1 || !fields[0].Name.Equals("approved", StringComparison.OrdinalIgnoreCase)
            || fields[0].Value.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) throw Invalid();
        return new ApproveOrderRequest(fields[0].Value.GetBoolean());
    }
    public override void Write(Utf8JsonWriter writer, ApproveOrderRequest value, JsonSerializerOptions options)
    { writer.WriteStartObject(); writer.WriteBoolean("approved", value.Approved); writer.WriteEndObject(); }
    private static JsonException Invalid() => new("Informe somente approved como booleano explícito.");
}
