using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.ZansiDispatch;
using ZansiHustle.Domain.ZansiDispatch;
using ZansiHustle.Infrastructure.Data;

namespace ZansiHustle.Infrastructure.Data.Seed
{
    /// <summary>
    /// Seeds the default ZansiDispatch tuning knobs into
    /// <c>ZansiDispatchSettings</c> on startup. Idempotent: only inserts keys
    /// that are missing, so operator edits are never overwritten. Values mirror
    /// <see cref="ZansiDispatchDefaults"/> — the same numbers the service falls
    /// back to when a row is absent — so seeding only surfaces the knobs for
    /// tuning, never changes behaviour.
    /// </summary>
    public static class ZansiDispatchSettingsSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            var existing = await db.ZansiDispatchSettings.Select(s => s.Key).ToListAsync();
            var present = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);

            var now = DateTime.UtcNow;
            var added = false;

            foreach (var (key, value, description) in ZansiDispatchDefaults.Seed)
            {
                if (present.Contains(key)) continue;
                db.ZansiDispatchSettings.Add(new ZansiDispatchSetting
                {
                    Id = Guid.NewGuid(),
                    Key = key,
                    Value = value,
                    Description = description,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
                added = true;
            }

            if (added) await db.SaveChangesAsync();
        }
    }
}
