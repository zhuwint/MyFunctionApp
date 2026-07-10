using MyFunctionApp;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IGreetingService, GreetingService>();

var app = builder.Build();
app.MapMethods("/api/satdemofunc/{username}", ["GET", "POST"],
    (string username, IGreetingService greetingService) =>
        Results.Json(greetingService.Handle(username)));
app.Run();
