using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Core.CQRS
{
    /// <summary>
    /// Interface for handling queries
    /// </summary>
    /// <typeparam name="TQuery">Type of query to handle</typeparam>
    /// <typeparam name="TResult">Type of result returned</typeparam>
    public interface IQueryHandler&lt;in TQuery, TResult&gt; where TQuery : IQuery&lt;TResult&gt;
    {
        /// <summary>
        /// Handles the specified query and returns a result
        /// </summary>
        /// <param name="query">The query to handle</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task containing the result</returns>
        Task&lt;TResult&gt; HandleAsync(TQuery query, CancellationToken cancellationToken = default);
    }
}