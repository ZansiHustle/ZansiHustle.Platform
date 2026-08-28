using System;
using System.Collections.Generic;

namespace ZansiHustle.Application.Catalog.External
{
    /// <summary>
    /// Registration record for one approved external catalog source (e.g.
    /// "zansitech"). Mirrors <c>ExternalShopOptions</c> (the payments
    /// feature's equivalent) but deliberately carries a DIFFERENT
    /// <see cref="SharedSecret"/> namespace — catalog sync and payment
    /// authorization must never share a credential.
    /// </summary>
    public sealed class ExternalCatalogSourceOptions
    {
        public bool Enabled { get; set; }

        /// <summary>Display name for this source, e.g. "ZansiTech".</summary>
        public string SourceName { get; set; } = string.Empty;

        /// <summary>
        /// The ZansiHustle ShopProfile all of this source's mirrored
        /// Listings are published under. The owning MerchantId is resolved
        /// from this at sync time (never configured separately) — a single
        /// authoritative link, so a stale/mismatched second id can't drift.
        /// UAT and Production have different values; never hardcoded.
        /// </summary>
        public Guid ShopProfileId { get; set; }

        /// <summary>Server-to-server secret for X-ZansiHustle-Catalog-Secret. Never committed — set via env var, e.g. ExternalCatalogs__zansitech__SharedSecret.</summary>
        public string SharedSecret { get; set; } = string.Empty;
    }

    /// <summary>
    /// Bound from the "ExternalCatalogs" configuration section — a map of
    /// lowercase sourceCode → <see cref="ExternalCatalogSourceOptions"/>.
    /// </summary>
    public sealed class ExternalCatalogSourcesOptions : Dictionary<string, ExternalCatalogSourceOptions>
    {
        public const string SectionName = "ExternalCatalogs";
    }
}
