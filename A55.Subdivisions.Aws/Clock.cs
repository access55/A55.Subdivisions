namespace A55.Subdivisions.Aws;

public interface ISubClock
{
    DateTime Now();
}

sealed class UtcClock : ISubClock
{
    public DateTime Now() => DateTime.UtcNow;
}
