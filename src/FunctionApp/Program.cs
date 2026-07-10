using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyFunctionApp;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<IGreetingService, GreetingService>();
    })
    .Build();

host.Run();

public class SATDemoFunc
{
    private readonly IGreetingService _greetingService;

    public SATDemoFunc(IGreetingService greetingService)
    {
        _greetingService = greetingService;
    }

    [Function("SATDemoFunc")]
    public HttpResponseData Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "satdemofunc/{username}")] HttpRequestData req,
        string username)
    {
        var result = _greetingService.Handle(username);

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
        response.WriteString(JsonSerializer.Serialize(result));
        return response;
    }
}
