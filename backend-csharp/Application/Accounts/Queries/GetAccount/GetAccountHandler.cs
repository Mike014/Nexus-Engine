// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Application.Accounts.Queries.GetAccount
//
// FILE DESCRIPTION:
// Implements the MediatR handler for GetAccountQuery.
//
// CLASS DOCUMENTATION:
// - GetAccountHandler: Executes the read-side query against the Account projection table.
//   Reads directly from the NexusDbContext read model (accounts table).
//   Never touches the Event Store (domain_events) -- projections exist precisely
//   to avoid replaying events on every read.
//
// MEMBER DOCUMENTATION:
// - _db: Injected NexusDbContext. Used in read-only mode via AsNoTracking().
// - Handle: Queries the accounts projection by ID. Returns null if not found,
//   which the Controller translates to HTTP 404 NotFound.
//   AsNoTracking disables EF Core change tracking -- no state modifications
//   are expected, so tracking overhead is unnecessary.
// ============================================================================

namespace NexusEngine.Api.Application.Accounts.Queries.GetAccount;

using MediatR;
using Microsoft.EntityFrameworkCore;
using NexusEngine.Api.Infrastructure.Persistence;

public class GetAccountHandler : IRequestHandler<GetAccountQuery, AccountDto?>
{
    private readonly NexusDbContext _db;

    public GetAccountHandler(NexusDbContext db)
    {
        _db = db;
    }

    public async Task<AccountDto?> Handle(
        GetAccountQuery query,
        CancellationToken cancellationToken)
    {
        var account = await _db.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.Id == query.AccountId,
                cancellationToken
            );

        if (account is null)
            return null;

        return new AccountDto(
            account.Id,
            account.OwnerName,
            account.Balance,
            account.ReservedBalance,
            account.Currency,
            account.Status,
            account.CreatedAt
        );
    }
}