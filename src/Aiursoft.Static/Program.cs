using Aiursoft.CommandFramework;
using Aiursoft.Static;
using Aiursoft.Static.Handlers;

return await new SingleCommandApp<StaticHandler>().RunAsync(args);