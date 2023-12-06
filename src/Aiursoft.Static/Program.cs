using Aiursoft.CommandFramework;

var command = new StaticHandler().BuildAsCommand();

return await new AiursoftCommandApp(command)
    .RunAsync(args);