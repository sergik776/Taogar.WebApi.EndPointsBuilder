using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;
using System.Reflection;

namespace Taogar.WebApi.EndPointsBuilder
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all implementations of the IEndPoint<O> and IEndPoint<I, O> interfaces in the DI container.
        /// </summary>
        /// <param name="services">IServiceCollection for registering dependencies.</param>
        /// <returns>IServiceCollection for later use.</returns>
        public static IServiceCollection AddBaseEndPointHandlers(this IServiceCollection services)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var handlerTypes = assembly.GetTypes()
                .Where(t => t.GetInterfaces().Any(i =>
                    (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEndPoint<,>)) ||
                    (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEndPoint<>))))
                .ToList();
                foreach (var handlerType in handlerTypes)
                {
                    services.AddScoped(handlerType);
                }
            }

            return services;
        }

        /// <summary>
        /// Registers endpoints in WebApplication for all IEndPoint<O> and IEndPoint<I, O> implementations.
        /// </summary>
        /// <param name="app">WebApplication to register endpoints for.</param>
        /// <exception cref="NotSupportedException"></exception>
        public static void RegisterEndPoints(this WebApplication app)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var handlerTypes = assembly.GetTypes()
                .Where(t => t.GetInterfaces().Any(i =>
                    (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEndPoint<,>)) ||
                    (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEndPoint<>))))
                .ToList();
                foreach (var handlerType in handlerTypes)
                {
                    using var scope = app.Services.CreateScope();
                    var handlerInstance = scope.ServiceProvider.GetService(handlerType);
                    var methods = handlerType.GetMethods();
                    foreach (var method in methods)
                    {
                        var httpMethodAttributes = method.GetCustomAttributes<HttpMethodAttribute>(inherit: false);
                        if (httpMethodAttributes.Any())
                        {
                            var methodParameters = method.GetParameters().Select(p => p.ParameterType).ToArray();
                            var returnType = method.ReturnType;
                            var delegateType = Expression.GetDelegateType(methodParameters.Concat(new[] { returnType }).ToArray());
                            var delegateInstance = Delegate.CreateDelegate(delegateType, handlerInstance, method);
                            foreach (var httpMethodAttr in httpMethodAttributes)
                            {
                                var httpMethods = httpMethodAttr.HttpMethods;
                                foreach (var method1 in httpMethods)
                                {
                                    var endpointRoute = "/" + handlerType.Name.ToLower();
                                    switch (method1)
                                    {
                                        case "POST":
                                            app.MapPost(endpointRoute, delegateInstance).WithOpenApi();
                                            break;
                                        case "GET":
                                            app.MapGet(endpointRoute, delegateInstance).WithOpenApi();
                                            break;
                                        case "PUT":
                                            app.MapPut(endpointRoute, delegateInstance).WithOpenApi();
                                            break;
                                        case "DELETE":
                                            app.MapDelete(endpointRoute, delegateInstance).WithOpenApi();
                                            break;
                                        case "PATCH":
                                            app.MapPatch(endpointRoute, delegateInstance).WithOpenApi();
                                            break;
                                        case "OPTIONS":
                                            app.MapMethods(endpointRoute, new[] { "OPTIONS" }, delegateInstance).WithOpenApi();
                                            break;
                                        default:
                                            throw new NotSupportedException($"HTTP метод '{method1}' не поддерживается.");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
