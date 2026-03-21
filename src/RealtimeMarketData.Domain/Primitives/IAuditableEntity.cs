namespace RealtimeMarketData.Domain.Primitives;

public interface IAuditableEntity
{
    DateTime CreatedOn { get; set; }
    DateTime? UpdatedOn { get; set; }
}
