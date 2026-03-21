namespace RealtimeMarketData.Domain.Primitives;

public abstract class AuditableEntity<TId> : Entity<TId>, IAuditableEntity where TId : notnull
{
    protected AuditableEntity(TId id) : base(id) { }
    protected AuditableEntity() { }

    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
}
