using System.Text.Json.Serialization;

namespace Common.Messages;

public record ProductMessageFromStorage
{
    [JsonInclude]
    public Guid ProductId { get; set; }
    
    [JsonInclude]
    public int? QuantityInStock { get; set; }

    [JsonInclude]
    public string? Article { get; set; }

    [JsonInclude]
    public string? Name { get; set; }

    [JsonInclude]
    public decimal? Price { get; set; }

    [JsonInclude]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProductEventType EventType { get; set; }
}