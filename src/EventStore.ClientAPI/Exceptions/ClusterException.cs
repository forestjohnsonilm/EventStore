﻿using System;


namespace EventStore.ClientAPI.Exceptions
{
    /// <summary>
    /// Exception thrown if cluster discovery fails.
    /// </summary>
    public class ClusterException : EventStoreConnectionException
    {
        /// <summary>
        /// Constructs a new <see cref="ClusterException" />.
        /// </summary>
        public ClusterException()
        {
        }

        /// <summary>
        /// Constructs a new <see cref="ClusterException" />.
        /// </summary>
        public ClusterException(string message)
                : base(message)
        {
        }

        /// <summary>
        /// Constructs a new <see cref="ClusterException" />.
        /// </summary>
        public ClusterException(string message, Exception innerException)
                : base(message, innerException)
        {
        }
    }
}