using Microsoft.AspNetCore.Mvc;

namespace Taogar.WebApi.EndPointsBuilder
{
    /// <summary>
    /// interface of unparametrized handler
    /// </summary>
    /// <typeparam name="O">Type of return object</typeparam>
    public interface IEndPoint<O>
    {
        /// <summary>
        /// Method for invoke on end point
        /// </summary>
        /// <returns>Returned result</returns>
        public Task<O> Handle();
    }

    /// <summary>
    /// interface of parametrized handler
    /// </summary>
    /// <typeparam name="I">Type of input object</typeparam>
    /// <typeparam name="O">Type of return object</typeparam>
    public interface IEndPoint<I, O>
    {
        /// <summary>
        /// Method for invoke on end point
        /// </summary>
        /// <param name="model">Input model</param>
        /// <returns>Returned result</returns>
        public Task<O> Handle([FromBody] I model);
    }
}
