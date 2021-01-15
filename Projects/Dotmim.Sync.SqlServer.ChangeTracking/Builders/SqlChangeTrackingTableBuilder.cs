﻿using Dotmim.Sync.Builders;
using Dotmim.Sync.SqlServer.Builders;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;

namespace Dotmim.Sync.SqlServer.ChangeTracking.Builders
{
    public class SqlChangeTrackingTableBuilder : SqlTableBuilder
    {
        private SqlChangeTrackingBuilderTrackingTable sqlChangeTrackingBuilderTrackingTable;
        private SqlChangeTrackingBuilderProcedure sqlChangeTrackingBuilderProcedure;
        private SqlChangeTrackingBuilderTrigger sqlChangeTrackingBuilderTrigger;

      
        public SqlChangeTrackingTableBuilder(SyncTable tableDescription, ParserName tableName, ParserName trackingTableName, SyncSetup setup)
            : base(tableDescription, tableName, trackingTableName, setup) 
        {
            this.sqlChangeTrackingBuilderTrackingTable = new SqlChangeTrackingBuilderTrackingTable(TableDescription, this.TableName, this.TrackingTableName, Setup);
            this.sqlChangeTrackingBuilderProcedure = new SqlChangeTrackingBuilderProcedure(TableDescription, this.TableName, this.TrackingTableName, Setup);
            this.sqlChangeTrackingBuilderTrigger = new SqlChangeTrackingBuilderTrigger(TableDescription, this.TableName, this.TrackingTableName, Setup);
        }



        public override Task<DbCommand> GetExistsStoredProcedureCommandAsync(DbStoredProcedureType storedProcedureType, SyncFilter filter, DbConnection connection, DbTransaction transaction)
            => this.sqlChangeTrackingBuilderProcedure.GetExistsStoredProcedureCommandAsync(storedProcedureType, filter, connection, transaction);
        public override Task<DbCommand> GetCreateStoredProcedureCommandAsync(DbStoredProcedureType storedProcedureType, SyncFilter filter, DbConnection connection, DbTransaction transaction)
            => this.sqlChangeTrackingBuilderProcedure.GetCreateStoredProcedureCommandAsync(storedProcedureType, filter, connection, transaction);
        public override Task<DbCommand> GetDropStoredProcedureCommandAsync(DbStoredProcedureType storedProcedureType, SyncFilter filter, DbConnection connection, DbTransaction transaction)
            => this.sqlChangeTrackingBuilderProcedure.GetDropStoredProcedureCommandAsync(storedProcedureType, filter, connection, transaction);

        public override Task<DbCommand> GetCreateTrackingTableCommandAsync(DbConnection connection, DbTransaction transaction)
            => this.sqlChangeTrackingBuilderTrackingTable.GetCreateTrackingTableCommandAsync(connection, transaction);
        public override Task<DbCommand> GetDropTrackingTableCommandAsync(DbConnection connection, DbTransaction transaction)
            => this.sqlChangeTrackingBuilderTrackingTable.GetDropTrackingTableCommandAsync(connection, transaction);
        public override Task<DbCommand> GetRenameTrackingTableCommandAsync(ParserName oldTableName, DbConnection connection, DbTransaction transaction)
            => this.sqlChangeTrackingBuilderTrackingTable.GetRenameTrackingTableCommandAsync(oldTableName, connection, transaction);
        public override Task<DbCommand> GetExistsTrackingTableCommandAsync(DbConnection connection, DbTransaction transaction)
            => this.sqlChangeTrackingBuilderTrackingTable.GetExistsTrackingTableCommandAsync(connection, transaction);

        public override Task<DbCommand> GetExistsTriggerCommandAsync(DbTriggerType triggerType, DbConnection connection, DbTransaction transaction)
            => this.sqlChangeTrackingBuilderTrigger.GetExistsTriggerCommandAsync(triggerType, connection, transaction);
        public override Task<DbCommand> GetCreateTriggerCommandAsync(DbTriggerType triggerType, DbConnection connection, DbTransaction transaction)
            => this.sqlChangeTrackingBuilderTrigger.GetCreateTriggerCommandAsync(triggerType, connection, transaction);
        public override Task<DbCommand> GetDropTriggerCommandAsync(DbTriggerType triggerType, DbConnection connection, DbTransaction transaction)
            => this.sqlChangeTrackingBuilderTrigger.GetDropTriggerCommandAsync(triggerType, connection, transaction);

    }
}
