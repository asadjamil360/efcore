// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.EntityFrameworkCore
{
    public abstract class InterceptionTestBase
    {
        protected InterceptionTestBase(InterceptionFixtureBase fixture)
        {
            Fixture = fixture;
        }

        protected InterceptionFixtureBase Fixture { get; }

        protected class Singularity
        {
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int Id { get; set; }

            public string Type { get; set; }
        }

        protected class Brane
        {
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int Id { get; set; }

            public string Type { get; set; }
        }

        public class UniverseContext : PoolableDbContext
        {
            public UniverseContext(DbContextOptions options)
                : base(options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder
                    .Entity<Singularity>()
                    .HasData(
                        new Singularity
                        {
                            Id = 77, Type = "Black Hole"
                        },
                        new Singularity
                        {
                            Id = 88, Type = "Bing Bang"
                        });

                modelBuilder
                    .Entity<Brane>()
                    .HasData(
                        new Brane
                        {
                            Id = 77, Type = "Black Hole?"
                        },
                        new Brane
                        {
                            Id = 88, Type = "Bing Bang?"
                        });
            }
        }

        protected void AssertSql(string expected, string actual)
            => Assert.Equal(
                expected,
                actual.Replace("\r", string.Empty).Replace("\n", " "));

        protected (DbContext, TInterceptor) CreateContext<TInterceptor>(bool inject = false)
            where TInterceptor : class, IInterceptor, new()
        {
            var interceptor = new TInterceptor();

            var context = inject ? CreateContext(null, interceptor) : CreateContext(interceptor);

            return (context, interceptor);
        }

        public UniverseContext CreateContext(IInterceptor appInterceptor, string connectionString)
            => new UniverseContext(
                Fixture.CreateOptions(
                    new[]
                    {
                        appInterceptor
                    }, Enumerable.Empty<IInterceptor>()));

        public UniverseContext CreateContext(IInterceptor appInterceptor, params IInterceptor[] injectedInterceptors)
            => new UniverseContext(
                Fixture.CreateOptions(
                    new[]
                    {
                        appInterceptor
                    }, injectedInterceptors));

        public UniverseContext CreateContext(
            IEnumerable<IInterceptor> appInterceptors,
            IEnumerable<IInterceptor> injectedInterceptors = null)
            => new UniverseContext(Fixture.CreateOptions(appInterceptors, injectedInterceptors ?? Enumerable.Empty<IInterceptor>()));

        public abstract class InterceptionFixtureBase : SharedStoreFixtureBase<UniverseContext>
        {
            public virtual DbContextOptions CreateOptions(
                IEnumerable<IInterceptor> appInterceptors,
                IEnumerable<IInterceptor> injectedInterceptors)
                => base.AddOptions(
                        TestStore
                            .AddProviderOptions(
                                new DbContextOptionsBuilder()
                                    .AddInterceptors(appInterceptors)
                                    .UseInternalServiceProvider(
                                        InjectInterceptors(new ServiceCollection(), injectedInterceptors)
                                            .BuildServiceProvider())))
                    .EnableDetailedErrors()
                    .Options;

            protected virtual IServiceCollection InjectInterceptors(
                IServiceCollection serviceCollection,
                IEnumerable<IInterceptor> injectedInterceptors)
            {
                foreach (var interceptor in injectedInterceptors)
                {
                    serviceCollection.AddSingleton(interceptor);
                }

                return serviceCollection;
            }
        }
    }
}