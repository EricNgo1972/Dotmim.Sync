﻿using Dotmim.Sync.Batch;
using Dotmim.Sync.Builders;
using Dotmim.Sync.Enumerations;
using Dotmim.Sync.Serialization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Dotmim.Sync
{
    public partial class LocalOrchestrator : BaseOrchestrator
    {
        /// <summary>
        /// Update all untracked rows from the client database
        /// </summary>
        public virtual Task<bool> UpdateUntrackedRowsAsync(SyncSet schema, DbConnection connection = default, DbTransaction transaction = default, CancellationToken cancellationToken = default, IProgress<ProgressArgs> progress = null)
        => RunInTransactionAsync(SyncStage.ChangesApplying, async (ctx, connection, transaction) =>
        {
            // If schema does not have any table, just return
            if (schema == null || schema.Tables == null || !schema.HasTables)
                throw new MissingTablesException();

            // Update untracked rows
            foreach (var table in schema.Tables)
            {
                var syncAdapter = this.GetSyncAdapter(table, this.Setup);
                await this.InternalUpdateUntrackedRowsAsync(ctx, syncAdapter, connection, transaction).ConfigureAwait(false);
            }

            return true;

        }, connection, transaction, cancellationToken);


        /// <summary>
        /// Internal update untracked rows routine
        /// </summary>
        internal async Task<int> InternalUpdateUntrackedRowsAsync(SyncContext ctx, DbSyncAdapter syncAdapter, DbConnection connection, DbTransaction transaction)
        {
            // Get correct Select incremental changes command 
            var command = await syncAdapter.GetCommandAsync(DbCommandType.UpdateUntrackedRows, connection, transaction);

            // Execute
            var rowAffected = await command.ExecuteNonQueryAsync().ConfigureAwait(false);

            // Check if we have a return value instead
            var syncRowCountParam = DbSyncAdapter.GetParameter(command, "sync_row_count");

            if (syncRowCountParam != null)
                rowAffected = (int)syncRowCountParam.Value;

            return rowAffected;
        }

    }
}
