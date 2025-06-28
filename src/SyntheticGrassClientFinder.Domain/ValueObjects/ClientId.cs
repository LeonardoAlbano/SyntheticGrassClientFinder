namespace SyntheticGrassClientFinder.Domain.ValueObjects;

public record ClientId(Guid Value)
{
    public static ClientId New() => new(Guid.NewGuid());
    public static ClientId From(Guid value) => new(value);
    
    public override string ToString() => Value.ToString();
    
    public static implicit operator Guid(ClientId clientId) => clientId.Value;
    public static implicit operator ClientId(Guid value) => new(value);
}