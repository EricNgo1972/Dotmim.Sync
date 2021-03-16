﻿using Dotmim.Sync.Builders;
using Dotmim.Sync.Enumerations;
using Dotmim.Sync.SqlServer;
using Dotmim.Sync.Tests.Core;
using Dotmim.Sync.Tests.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Dotmim.Sync.Tests.UnitTests
{
    public partial class InterceptorsTests
    {
        [Fact]
        public async Task LocalOrchestrator_GetChanges()
        {
            var dbNameSrv = HelperDatabase.GetRandomName("tcp_lo_srv");
            await HelperDatabase.CreateDatabaseAsync(ProviderType.Sql, dbNameSrv, true);

            var dbNameCli = HelperDatabase.GetRandomName("tcp_lo_cli");
            await HelperDatabase.CreateDatabaseAsync(ProviderType.Sql, dbNameCli, true);

            var csServer = HelperDatabase.GetConnectionString(ProviderType.Sql, dbNameSrv);
            var serverProvider = new SqlSyncProvider(csServer);

            var csClient = HelperDatabase.GetConnectionString(ProviderType.Sql, dbNameCli);
            var clientProvider = new SqlSyncProvider(csClient);

            await new AdventureWorksContext((dbNameSrv, ProviderType.Sql, serverProvider), true, false).Database.EnsureCreatedAsync();
            await new AdventureWorksContext((dbNameCli, ProviderType.Sql, clientProvider), true, false).Database.EnsureCreatedAsync();

            var scopeName = "scopesnap1";
            var syncOptions = new SyncOptions();
            var setup = new SyncSetup();

            // Make a first sync to be sure everything is in place
            var agent = new SyncAgent(clientProvider, serverProvider, this.Tables, scopeName);

            // Making a first sync, will initialize everything we need
            var s = await agent.SynchronizeAsync();

            // Get the orchestrators
            var localOrchestrator = agent.LocalOrchestrator;
            var remoteOrchestrator = agent.RemoteOrchestrator;

            // Client side : Create a product category and a product
            // Create a productcategory item
            // Create a new product on server
            var productId = Guid.NewGuid();
            var productName = HelperDatabase.GetRandomName();
            var productNumber = productName.ToUpperInvariant().Substring(0, 10);

            var productCategoryName = HelperDatabase.GetRandomName();
            var productCategoryId = productCategoryName.ToUpperInvariant().Substring(0, 6);

            using (var ctx = new AdventureWorksContext((dbNameCli, ProviderType.Sql, clientProvider)))
            {
                var pc = new ProductCategory { ProductCategoryId = productCategoryId, Name = productCategoryName };
                ctx.Add(pc);

                var product = new Product { ProductId = productId, Name = productName, ProductNumber = productNumber };
                ctx.Add(product);

                await ctx.SaveChangesAsync();
            }

            var onDatabaseSelecting = 0;
            var onDatabaseSelected = 0;
            var onSelecting = 0;
            var onSelected = 0;

            localOrchestrator.OnDatabaseChangesSelecting(dcs =>
            {
                onDatabaseSelecting++;
            });

            localOrchestrator.OnDatabaseChangesSelected(dcs =>
            {
                Assert.NotNull(dcs.BatchInfo);
                Assert.Equal(2, dcs.ChangesSelected.TableChangesSelected.Count);
                onDatabaseSelected++;
            });

            localOrchestrator.OnTableChangesSelecting(action =>
            {
                Assert.NotNull(action.Command);
                onSelecting++;
            });

            localOrchestrator.OnTableChangesSelected(action =>
            {
                Assert.NotNull(action.Changes);
                onSelected++;
            });

            // Get changes to be populated to the server
            var changes = await localOrchestrator.GetChangesAsync();

            Assert.Equal(this.Tables.Length, onSelecting);
            Assert.Equal(this.Tables.Length, onSelected);
            Assert.Equal(1, onDatabaseSelected);
            Assert.Equal(1, onDatabaseSelecting);

            HelperDatabase.DropDatabase(ProviderType.Sql, dbNameSrv);
            HelperDatabase.DropDatabase(ProviderType.Sql, dbNameCli);
        }

        [Fact]
        public async Task LocalOrchestrator_GetBacthChanges()
        {
            var dbNameSrv = HelperDatabase.GetRandomName("tcp_lo_srv");
            await HelperDatabase.CreateDatabaseAsync(ProviderType.Sql, dbNameSrv, true);

            var dbNameCli = HelperDatabase.GetRandomName("tcp_lo_cli");
            await HelperDatabase.CreateDatabaseAsync(ProviderType.Sql, dbNameCli, true);

            var csServer = HelperDatabase.GetConnectionString(ProviderType.Sql, dbNameSrv);
            var serverProvider = new SqlSyncProvider(csServer);

            var csClient = HelperDatabase.GetConnectionString(ProviderType.Sql, dbNameCli);
            var clientProvider = new SqlSyncProvider(csClient);

            await new AdventureWorksContext((dbNameSrv, ProviderType.Sql, serverProvider), true, true).Database.EnsureCreatedAsync();
            await new AdventureWorksContext((dbNameCli, ProviderType.Sql, clientProvider), true, false).Database.EnsureCreatedAsync();

            var scopeName = "scopesnap1";
            var syncOptions = new SyncOptions
            {
                BatchSize = 100
            };

            var setup = new SyncSetup();

            // Make a first sync to be sure everything is in place
            var agent = new SyncAgent(clientProvider, serverProvider, syncOptions, this.Tables, scopeName);

            // Making a first sync, will initialize everything we need
            var s = await agent.SynchronizeAsync();

            // Get the orchestrators
            var localOrchestrator = agent.LocalOrchestrator;
            var remoteOrchestrator = agent.RemoteOrchestrator;

            // Client side : Create a product category and a product
            // Create a productcategory item
            // Create a new product on server
            using (var ctx = new AdventureWorksContext((dbNameCli, ProviderType.Sql, clientProvider)))
            {
                for (int i = 0; i <= 10000; i++)
                {
                    var productId = Guid.NewGuid();
                    var productName = HelperDatabase.GetRandomName();
                    var productNumber = productName.ToUpperInvariant().Substring(0, 10);

                    var productCategoryName = HelperDatabase.GetRandomName();
                    var productCategoryId = productCategoryName.ToUpperInvariant().Substring(0, 6);

                    if (!ctx.ProductCategory.Any(p => p.ProductCategoryId == productCategoryId))
                    {
                        var pc = new ProductCategory { ProductCategoryId = productCategoryId, Name = productCategoryName };
                        ctx.Add(pc);
                    }

                    var product = new Product { ProductId = productId, Name = productName, ProductNumber = productNumber };
                    ctx.Add(product);

                }
                await ctx.SaveChangesAsync();

            }
            var onDatabaseSelecting = 0;
            var onDatabaseSelected = 0;
            var onSelecting = 0;
            var onSelected = 0;

            localOrchestrator.OnDatabaseChangesSelecting(dcs =>
            {
                onDatabaseSelecting++;
            });

            localOrchestrator.OnDatabaseChangesSelected(dcs =>
            {
                Assert.NotNull(dcs.BatchInfo);
                Assert.Equal(2, dcs.ChangesSelected.TableChangesSelected.Count);
                onDatabaseSelected++;
            });

            localOrchestrator.OnTableChangesSelecting(action =>
            {
                Assert.NotNull(action.Command);
                onSelecting++;
            });

            localOrchestrator.OnTableChangesSelected(action =>
            {
                Assert.NotNull(action.Changes);
                onSelected++;
            });

            // Get changes to be populated to the server
            var changes = await localOrchestrator.GetChangesAsync();

            Assert.Equal(this.Tables.Length, onSelecting);
            Assert.InRange(onSelected, 50, 60);
            Assert.Equal(1, onDatabaseSelected);
            Assert.Equal(1, onDatabaseSelecting);

            HelperDatabase.DropDatabase(ProviderType.Sql, dbNameSrv);
            HelperDatabase.DropDatabase(ProviderType.Sql, dbNameCli);
        }



        [Fact]
        public async Task RemoteOrchestrator_GetChanges()
        {
            var dbNameSrv = HelperDatabase.GetRandomName("tcp_lo_srv");
            await HelperDatabase.CreateDatabaseAsync(ProviderType.Sql, dbNameSrv, true);

            var dbNameCli = HelperDatabase.GetRandomName("tcp_lo_cli");
            await HelperDatabase.CreateDatabaseAsync(ProviderType.Sql, dbNameCli, true);

            var csServer = HelperDatabase.GetConnectionString(ProviderType.Sql, dbNameSrv);
            var serverProvider = new SqlSyncProvider(csServer);

            var csClient = HelperDatabase.GetConnectionString(ProviderType.Sql, dbNameCli);
            var clientProvider = new SqlSyncProvider(csClient);

            await new AdventureWorksContext((dbNameSrv, ProviderType.Sql, serverProvider), true, false).Database.EnsureCreatedAsync();
            await new AdventureWorksContext((dbNameCli, ProviderType.Sql, clientProvider), true, false).Database.EnsureCreatedAsync();

            var scopeName = "scopesnap1";
            var syncOptions = new SyncOptions();
            var setup = new SyncSetup();

            // Make a first sync to be sure everything is in place
            var agent = new SyncAgent(clientProvider, serverProvider, this.Tables, scopeName);

            // Making a first sync, will initialize everything we need
            var s = await agent.SynchronizeAsync();

            // Get the orchestrators
            var localOrchestrator = agent.LocalOrchestrator;
            var remoteOrchestrator = agent.RemoteOrchestrator;

            // Server side : Create a product category and a product
            // Create a productcategory item
            // Create a new product on server
            var productId = Guid.NewGuid();
            var productName = HelperDatabase.GetRandomName();
            var productNumber = productName.ToUpperInvariant().Substring(0, 10);

            var productCategoryName = HelperDatabase.GetRandomName();
            var productCategoryId = productCategoryName.ToUpperInvariant().Substring(0, 6);

            using (var ctx = new AdventureWorksContext((dbNameSrv, ProviderType.Sql, serverProvider)))
            {
                var pc = new ProductCategory { ProductCategoryId = productCategoryId, Name = productCategoryName };
                ctx.Add(pc);

                var product = new Product { ProductId = productId, Name = productName, ProductNumber = productNumber };
                ctx.Add(product);

                await ctx.SaveChangesAsync();
            }

            var onSelecting = 0;
            var onSelected = 0;
            var onDatabaseSelecting = 0;
            var onDatabaseSelected = 0;

            remoteOrchestrator.OnTableChangesSelecting(action =>
            {
                Assert.NotNull(action.Command);
                onSelecting++;
            });

            remoteOrchestrator.OnTableChangesSelected(action =>
            {
                Assert.NotNull(action.Changes);
                onSelected++;
            });
            remoteOrchestrator.OnDatabaseChangesSelecting(dcs =>
            {
                onDatabaseSelecting++;
            });

            remoteOrchestrator.OnDatabaseChangesSelected(dcs =>
            {
                Assert.NotNull(dcs.BatchInfo);
                Assert.Equal(2, dcs.ChangesSelected.TableChangesSelected.Count);
                onDatabaseSelected++;
            });

            var clientScope = await localOrchestrator.GetClientScopeAsync();

            // Get changes to be populated to be sent to the client
            var changes = await remoteOrchestrator.GetChangesAsync(clientScope);

            Assert.Equal(this.Tables.Length, onSelecting);
            Assert.Equal(this.Tables.Length, onSelected);
            Assert.Equal(1, onDatabaseSelected);
            Assert.Equal(1, onDatabaseSelecting);

            HelperDatabase.DropDatabase(ProviderType.Sql, dbNameSrv);
            HelperDatabase.DropDatabase(ProviderType.Sql, dbNameCli);
        }

    }
}
