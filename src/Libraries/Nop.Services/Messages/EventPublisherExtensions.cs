﻿using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Messages;
using Nop.Core.Events;

namespace Nop.Services.Messages
{
    /// <summary>
    /// Event publisher extensions
    /// </summary>
    public static class EventPublisherExtensions
    {
        /// <summary>
        /// Entity tokens added
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <typeparam name="U">Type</typeparam>
        /// <param name="eventPublisher">Event publisher</param>
        /// <param name="entity">Entity</param>
        /// <param name="tokens">Tokens</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public static async Task EntityTokensAddedAsync<T, U>(this IEventPublisher eventPublisher, T entity, System.Collections.Generic.IList<U> tokens) where T : BaseEntity
        {
            await eventPublisher.PublishAsync(new EntityTokensAddedEvent<T, U>(entity, tokens));
        }

        /// <summary>
        /// Message token added
        /// </summary>
        /// <typeparam name="U">Type</typeparam>
        /// <param name="eventPublisher">Event publisher</param>
        /// <param name="message">Message</param>
        /// <param name="tokens">Tokens</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public static async Task MessageTokensAddedAsync<U>(this IEventPublisher eventPublisher, MessageTemplate message, System.Collections.Generic.IList<U> tokens)
        {
            await eventPublisher.PublishAsync(new MessageTokensAddedEvent<U>(message, tokens));
        }
    }
}