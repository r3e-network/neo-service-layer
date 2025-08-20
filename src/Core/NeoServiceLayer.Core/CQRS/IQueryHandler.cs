using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System;


namespace NeoServiceLayer.Core.CQRS
{
    /// <summary>
    /// Interface for handling queries
    /// </summary>
    /// <typeparam name="TQuery">Type of query to handle</typeparam>
    /// <typeparam name="TResult">Type of result returned</typeparam>
    public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
    {
        /// <summary>
        /// Handles the specified query and returns a result
        /// </summary>
        /// <param name="query">The query to handle</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task containing the result</returns>
        Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
    }
}
