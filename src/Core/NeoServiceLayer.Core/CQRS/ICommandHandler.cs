using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System;


namespace NeoServiceLayer.Core.CQRS
{
    /// <summary>
    /// Interface for handling commands without return values
    /// </summary>
    /// <typeparam name="TCommand">Type of command to handle</typeparam>
    public interface ICommandHandler<in TCommand> where TCommand : ICommand
    {
        /// <summary>
        /// Handles the specified command
        /// </summary>
        /// <param name="command">The command to handle</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Interface for handling commands with return values
    /// </summary>
    /// <typeparam name="TCommand">Type of command to handle</typeparam>
    /// <typeparam name="TResult">Type of result returned</typeparam>
    public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
    {
        /// <summary>
        /// Handles the specified command and returns a result
        /// </summary>
        /// <param name="command">The command to handle</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task containing the result</returns>
        Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
    }
}
