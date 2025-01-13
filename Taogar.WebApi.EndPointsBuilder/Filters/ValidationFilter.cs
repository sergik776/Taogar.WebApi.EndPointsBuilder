using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace WebApi.EndPointsBuilder.Filters
{
    public class ValidationFilter : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var model = context.Arguments.FirstOrDefault();
            if (model != null && model.GetType().IsClass)
            {
                var validationContext = new ValidationContext(model);
                var validationResults = new List<ValidationResult>();
                bool isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);
                if (!isValid)
                {
                    throw new ValidationException(string.Join(", ", validationResults.Select(vr => vr.ErrorMessage).ToList()));
                }
            }
            return await next(context);
        }
    }
}