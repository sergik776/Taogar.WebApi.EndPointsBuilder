using Microsoft.AspNetCore.Mvc;
using Taogar.WebApi.EndPointsBuilder;
using WebApiTest.DataTransferObjects;

namespace WebApiTest.Handlers
{
    public class WeatherForecastHandler : IEndPoint<IEnumerable<WeatherForecast>>
    {
        ILogger<WeatherForecastHandler> logger;
        public WeatherForecastHandler(ILogger<WeatherForecastHandler> _logger)
        {
            logger = _logger;
        }

        [HttpPost]
        public async Task<IEnumerable<WeatherForecast>> Handle()
        {
            logger.LogInformation("Invoke WeatherForecastHandler");
            string[] summaries = new string[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };
            var forecast = Enumerable.Range(1, 5).Select(index =>
                    new WeatherForecast
                    {
                        Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                        TemperatureC = Random.Shared.Next(-20, 55),
                        Summary = summaries[Random.Shared.Next(summaries.Length)]
                    })
                    .ToArray();
            return forecast.ToList();
        }
    }
}
