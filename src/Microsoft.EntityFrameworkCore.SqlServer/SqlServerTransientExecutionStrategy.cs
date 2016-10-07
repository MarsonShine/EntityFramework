// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore
{
    public class SqlServerTransientExecutionStrategy : ExecutionStrategy
    {
        private readonly ICollection<int> _errorNumbersToAdd;

        /// <summary>
        ///     Creates a new instance of <see cref="SqlServerTransientExecutionStrategy" /> without a context.
        /// </summary>
        /// <remarks>
        ///     The default retry limit is 5, which means that the total amount of time spent before failing is 26 seconds plus the random factor.
        /// </remarks>
        protected SqlServerTransientExecutionStrategy()
            : base(null, DefaultMaxRetryCount, DefaultMaxDelay)
        {
        }

        /// <summary>
        ///     Creates a new instance of <see cref="SqlServerTransientExecutionStrategy" />.
        /// </summary>
        /// <param name="context"> The context on which the operations will be invoked. </param>
        /// <remarks>
        ///     The default retry limit is 5, which means that the total amount of time spent before failing is 26 seconds plus the random factor.
        /// </remarks>
        public SqlServerTransientExecutionStrategy(
            [NotNull] DbContext context)
            : this(context, DefaultMaxRetryCount)
        {
        }

        /// <summary>
        ///     Creates a new instance of <see cref="SqlServerTransientExecutionStrategy" />.
        /// </summary>
        /// <param name="context"> The required dependencies. </param>
        public SqlServerTransientExecutionStrategy(
            [NotNull] ExecutionStrategyContext context)
            : this(context, DefaultMaxRetryCount)
        {
        }

        /// <summary>
        ///     Creates a new instance of <see cref="SqlServerTransientExecutionStrategy" /> without a context.
        /// </summary>
        /// <param name="maxRetryCount"> The maximum number of retry attempts. </param>
        protected SqlServerTransientExecutionStrategy(
            int maxRetryCount)
            : base(null, maxRetryCount, DefaultMaxDelay)
        {
        }

        /// <summary>
        ///     Creates a new instance of <see cref="SqlServerTransientExecutionStrategy" />.
        /// </summary>
        /// <param name="context"> The context on which the operations will be invoked. </param>
        /// <param name="maxRetryCount"> The maximum number of retry attempts. </param>
        public SqlServerTransientExecutionStrategy(
            [NotNull] DbContext context,
            int maxRetryCount)
            : this(context, maxRetryCount, DefaultMaxDelay, null)
        {
        }

        /// <summary>
        ///     Creates a new instance of <see cref="SqlServerTransientExecutionStrategy" />.
        /// </summary>
        /// <param name="context"> The required dependencies. </param>
        /// <param name="maxRetryCount"> The maximum number of retry attempts. </param>
        public SqlServerTransientExecutionStrategy(
            [NotNull] ExecutionStrategyContext context,
            int maxRetryCount)
            : this(context, maxRetryCount, DefaultMaxDelay, null)
        {
        }

        /// <summary>
        ///     Creates a new instance of <see cref="SqlServerTransientExecutionStrategy" /> without a context.
        /// </summary>
        /// <param name="maxRetryCount"> The maximum number of retry attempts. </param>
        /// <param name="maxRetryDelay"> The maximum delay in milliseconds between retries. </param>
        /// <param name="errorNumbersToAdd"> Additional SQL error numbers that should be considered transient. </param>
        protected SqlServerTransientExecutionStrategy(
            int maxRetryCount,
            TimeSpan maxRetryDelay,
            [CanBeNull] ICollection<int> errorNumbersToAdd)
            : base(null, maxRetryCount, maxRetryDelay)
        {
            _errorNumbersToAdd = errorNumbersToAdd;
        }

        /// <summary>
        ///     Creates a new instance of <see cref="SqlServerTransientExecutionStrategy" />.
        /// </summary>
        /// <param name="context"> The context on which the operations will be invoked. </param>
        /// <param name="maxRetryCount"> The maximum number of retry attempts. </param>
        /// <param name="maxRetryDelay"> The maximum delay in milliseconds between retries. </param>
        /// <param name="errorNumbersToAdd"> Additional SQL error numbers that should be considered transient. </param>
        public SqlServerTransientExecutionStrategy(
            [NotNull] DbContext context,
            int maxRetryCount,
            TimeSpan maxRetryDelay,
            [CanBeNull] ICollection<int> errorNumbersToAdd)
            : this(new ExecutionStrategyContext(
                    context, context.GetService<IDbContextServices>().LoggerFactory.CreateLogger<IExecutionStrategy>()),
                maxRetryCount, maxRetryDelay, errorNumbersToAdd)
        {
        }

        /// <summary>
        ///     Creates a new instance of <see cref="SqlServerTransientExecutionStrategy" />.
        /// </summary>
        /// <param name="context"> The required dependencies. </param>
        /// <param name="maxRetryCount"> The maximum number of retry attempts. </param>
        /// <param name="maxRetryDelay"> The maximum delay in milliseconds between retries. </param>
        /// <param name="errorNumbersToAdd"> Additional SQL error numbers that should be considered transient. </param>
        public SqlServerTransientExecutionStrategy(
            [NotNull] ExecutionStrategyContext context,
            int maxRetryCount,
            TimeSpan maxRetryDelay,
            [CanBeNull] ICollection<int> errorNumbersToAdd)
            : base(context, maxRetryCount, maxRetryDelay)
        {
            _errorNumbersToAdd = errorNumbersToAdd;
        }

        protected override bool ShouldRetryOn(Exception exception)
        {
            if (_errorNumbersToAdd != null)
            {
                var sqlException = exception as SqlException;
                if (sqlException != null)
                {
                    foreach (SqlError err in sqlException.Errors)
                    {
                        if (_errorNumbersToAdd.Contains(err.Number))
                        {
                            return true;
                        }
                    }
                }
            }

            return TransientExceptionDetector.ShouldRetryOn(exception);
        }
    }
}
