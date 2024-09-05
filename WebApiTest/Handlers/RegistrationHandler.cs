using Microsoft.AspNetCore.Mvc;
using Taogar.WebApi.EndPointsBuilder;
using WebApiTest.DataTransferObjects;

namespace WebApiTest.Handlers
{
    public class RegistrationHandler : IEndPoint<Registration, string>
    {
        ILogger<RegistrationHandler> logger;

        public RegistrationHandler(ILogger<RegistrationHandler> _logger) 
        {
            logger = _logger;
        }

        [HttpPost]
        public async Task<string> Handle([FromBody] Registration model)
        {
            logger.LogInformation("Start registration");
            return $"User {model.Login} was registered";
        }
    }
}
