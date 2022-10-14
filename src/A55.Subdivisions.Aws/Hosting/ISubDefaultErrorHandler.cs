namespace A55.Subdivisions.Hosting;

public interface ISubErrorListener
{
    public Task OnError(Exception ex);
}

sealed class ErrorListener : ISubErrorListener
{
    readonly Func<Exception, Task> handler;

    public ErrorListener(Func<Exception, Task> handler) => this.handler = handler;
    public Task OnError(Exception ex) => handler?.Invoke(ex) ?? Task.CompletedTask;
}
