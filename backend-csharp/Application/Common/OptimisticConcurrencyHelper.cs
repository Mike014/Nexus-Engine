// ============================================================================
// Copyright (c) NexusEngine Enterprise. All rights reserved.
// Product: NexusEngine.Api
// Layer: Application.Common
//
// FILE DESCRIPTION:
// Provides utility methods for optimistic concurrency conflict detection.
//
// CLASS DOCUMENTATION:
// - OptimisticConcurrencyHelper: Static helper used by command handlers
//   to detect unique constraint violations (PostgreSQL error code 23505)
//   thrown by the underlying DbUpdateException, enabling retry logic.
//
// MEMBER DOCUMENTATION:
// - IsUniqueConstraintViolation: Returns true when the DbUpdateException's
//   inner exception message contains "23505", indicating a concurrent write
//   to the same (aggregate_id, aggregate_version) pair was detected.
// ============================================================================

namespace NexusEngine.Api.Application.Common;

using Microsoft.EntityFrameworkCore;

public static class OptimisticConcurrencyHelper
{
    public static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("23505") == true;
    }
}
