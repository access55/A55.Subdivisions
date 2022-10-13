namespace A55.Subdivisions.Aws.Hosting;

public interface ISubErrorListener
{
    public Task OnError(Exception ex);
}
