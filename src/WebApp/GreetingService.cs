using Microsoft.Extensions.Logging;

namespace MyFunctionApp;

public interface IGreetingService
{
    object Handle(string username);
}

public class GreetingService : IGreetingService
{
    private readonly ILogger<GreetingService> _logger;

    public GreetingService(ILogger<GreetingService> logger)
    {
        _logger = logger;
    }

    public object Handle(string username)
    {
        _logger.LogInformation("HTTP request processed, username: {username}", username);
        return new { message = $"hello {username}" };
    }
}
