// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore.Models;
using Squidex.EntityFramework.TestHelpers;
using Squidex.Infrastructure;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Squidex.EntityFramework.Domain.Users;

public abstract class EFOpenIddictTests<TContext>(ISqlFixture<TContext> fixture)
    where TContext : DbContext, IDbContextWithDialect
{
    [Fact]
    public async Task Should_allow_openiddict_tokens_without_application()
    {
        await using var dbContext = await fixture.DbContextFactory.CreateDbContextAsync();

        var authorization = new OpenIddictEntityFrameworkCoreAuthorization
        {
            Id = Guid.NewGuid().ToString(),
            ApplicationId = null,
            CreationDate = DateTime.UtcNow,
            Status = Statuses.Valid,
            Subject = "admin@squidex.io",
            Type = AuthorizationTypes.Permanent,
        };

        var token = new OpenIddictEntityFrameworkCoreToken
        {
            Id = Guid.NewGuid().ToString(),
            ApplicationId = null,
            Authorization = authorization,
            CreationDate = DateTime.UtcNow,
            ExpirationDate = DateTime.UtcNow.AddMinutes(5),
            Status = Statuses.Valid,
            Subject = "admin@squidex.io",
            Type = "authorization_code",
        };

        dbContext.Set<OpenIddictEntityFrameworkCoreAuthorization>().Add(authorization);
        dbContext.Set<OpenIddictEntityFrameworkCoreToken>().Add(token);

        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Should_roundtrip_openiddict_token_with_long_type_identifier()
    {
        // OpenIddict persists the full token type identifier, and the longest of them needs 57
        // characters, which is more than the 50 characters the token type column used to allow.
        var type = TokenTypeIdentifiers.Private.AuthorizationCode;
        var tokenId = Guid.NewGuid().ToString();

        await using (var dbContext = await fixture.DbContextFactory.CreateDbContextAsync())
        {
            var token = new OpenIddictEntityFrameworkCoreToken
            {
                Id = tokenId,
                ApplicationId = null,
                CreationDate = DateTime.UtcNow,
                ExpirationDate = DateTime.UtcNow.AddMinutes(5),
                Status = Statuses.Valid,
                Subject = "admin@squidex.io",
                Type = type,
            };

            dbContext.Set<OpenIddictEntityFrameworkCoreToken>().Add(token);

            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = await fixture.DbContextFactory.CreateDbContextAsync())
        {
            // Compare the stored value, because not every database fails when it truncates.
            var found = await dbContext.Set<OpenIddictEntityFrameworkCoreToken>().FindAsync(tokenId);

            Assert.Equal(type, found?.Type);
        }
    }
}
