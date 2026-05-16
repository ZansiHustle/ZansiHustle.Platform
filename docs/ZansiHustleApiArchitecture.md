# ZansiHustle API Architecture Guide

> **Purpose of this document.** A reusable, opinionated reference for the
> ZansiHustle backend architecture (.NET 8 + EF Core + Identity + JWT).
> Hand this file to a new Claude/IDE session and say:
> "Use this architecture for my new API." It captures conventions,
> patterns, and the decisions behind them — not just the shape.
>
> **Status**: Audit-only document. No business logic was changed while
> writing it. Real class names, method names, and file paths from the
> ZansiHustle codebase are quoted throughout.
>
> **Stack baseline**
> - .NET 8 (`net8.0`), nullable enabled, implicit usings
> - EF Core 8 + SQL Server (`Microsoft.EntityFrameworkCore.SqlServer`)
> - ASP.NET Core Identity (`IdentityDbContext<User, IdentityRole<Guid>, Guid>`)
> - JWT Bearer auth (`Microsoft.AspNetCore.Authentication.JwtBearer`)
> - Cloudflare R2 via `AWSSDK.S3` (S3-compatible)
> - Swashbuckle for Swagger
> - Twilio (SMS/WhatsApp), SMTP email, Paystack/Ozow/Yoco payments

---

## Table of contents

1. [High-level architecture](#1-high-level-architecture)
2. [Folder structure](#2-folder-structure)
3. [Request flow](#3-request-flow-end-to-end)
4. [Result / error handling pattern](#4-result--error-handling-pattern)
5. [DTO pattern](#5-dto-pattern)
6. [Entity / model pattern](#6-entity--model-pattern)
7. [EF Core / Infrastructure pattern](#7-ef-core--infrastructure-pattern)
8. [Repository / service pattern](#8-repository--service-pattern)
9. [Authentication / authorization pattern](#9-authentication--authorization-pattern)
10. [Controller pattern](#10-controller-pattern)
11. [External integrations](#11-external-integrations)
12. [Validation rules](#12-validation-rules)
13. [New module checklist](#13-new-module-checklist)
14. [Naming conventions](#14-naming-conventions)
15. [What to copy into a new API (starter template)](#15-what-to-copy-into-a-new-api-starter-template)
16. [What to simplify for smaller projects](#16-what-to-simplify-for-smaller-projects)
17. [Improvements recommended for next project](#17-improvements-recommended-for-next-project)
18. [Final summary](#18-final-summary)

---

## 1. High-level architecture

Five projects, clean-architecture style, dependency arrows pointing
inward toward Domain. The actual `.csproj` references are the ground
truth:

```
ZansiHustle.API ─────► ZansiHustle.Application ─────► ZansiHustle.Domain ─────► ZansiHustle.Shared
       │                       │                              ▲
       └─► ZansiHustle.Infrastructure ────────────────────────┤
                               │                              │
                               └──────────────────────────────┘
```

### 1.1 What each project is for

- **ZansiHustle.Shared** — Cross-cutting, dependency-free types. Holds
  the `Result` / `Result<T>` envelope, `ErrorCodes` string constants,
  every status/kind/type enum (e.g. `ListingStatus`, `MerchantType`,
  `ShopProfileStatus`), and base query DTOs (`PagedListQueryBase`).
  References **nothing**. Safe to share with any project (including
  potentially a frontend codegen, admin portal, etc.).

- **ZansiHustle.Domain** — Pure entities. POCOs with no EF attributes,
  no DTOs, no MediatR, no services. References only Shared (for enums).
  Adds `Microsoft.AspNetCore.Identity.EntityFrameworkCore` only because
  `User : IdentityUser<Guid>` lives here. Folders mirror Application
  modules: `Listings/`, `Shops/`, `Merchants/`, `Reviews/`, `Identity/`,
  `Orders/`, `Payments/`, etc.

- **ZansiHustle.Application** — Business logic + persistence
  **interfaces** + DTOs. Contains:
  - service interfaces & implementations (`IListingService` +
    `ListingService`)
  - DTOs per module (`ListingDto`, `CreateListingRequestDto`, ...)
  - repository **interfaces** under `Persistence/` (e.g.
    `IListingRepository`)
  - cross-cutting interfaces (`ICurrentUserService`,
    `IUserLookupService`, `IStorageUrlResolver`)
  - `Common/Paging/PagedResult<T>`

  References Domain only. **Does not** reference EF Core, ASP.NET,
  Identity stack — that all lives behind interfaces.

- **ZansiHustle.Infrastructure** — The concrete adapters. Holds
  `AppDbContext`, every `IEntityTypeConfiguration<T>`, every repository
  implementation, JWT generator, SMTP/Twilio providers, Paystack/Ozow/
  Yoco clients, all `Migrations/`, configuration option classes
  (`JwtSettings`, `OzowSettings`, `YocoSettings`, `PaystackSettings`,
  `EmailSenderSettings`). References Application + Domain + Shared.

- **ZansiHustle.API** — HTTP host. Controllers (one per resource family),
  `BaseController` (the result-to-HTTP mapper), `Program.cs`, the
  `Extensions/` DI registration suite, `Middleware/`, and the storage
  service implementations (`R2MediaStorageService`,
  `LocalFilesystemMediaStorageService`). References Application +
  Infrastructure. **Composition root only** — no business logic.

### 1.2 Dependency rules (enforced by csproj refs)

| From ↓ \ To → | Shared | Domain | Application | Infrastructure | API |
|---|---|---|---|---|---|
| Shared | — | ❌ | ❌ | ❌ | ❌ |
| Domain | ✅ | — | ❌ | ❌ | ❌ |
| Application | (via Domain) | ✅ | — | ❌ | ❌ |
| Infrastructure | ✅ | ✅ | ✅ | — | ❌ |
| API | (via Infra) | (via Infra) | ✅ | ✅ | — |

### 1.3 What should never happen

- ❌ Controllers doing business logic (validation beyond model binding,
  DB access, ownership checks).
- ❌ Application referencing `Microsoft.EntityFrameworkCore` directly.
  Use repository interfaces from `Application/Persistence/`.
- ❌ Domain referencing Infrastructure, EF Core attributes (`[Key]`,
  `[Required]`), or DTOs.
- ❌ Shared referencing anything. Keep it dependency-free.
- ❌ Repositories returning DTOs. Repos return **entities**; the service
  maps to DTOs.
- ❌ Services throwing for expected business failures (validation,
  ownership, conflict). Return `Result.Failure(code, message)` instead
  and let `BaseController` map the code to an HTTP status.

---

## 2. Folder structure

### 2.1 `ZansiHustle.API`

```
ZansiHustle.API/
├── Program.cs                       # Composition root (Build/Run)
├── Controllers/
│   ├── BaseController.cs            # ToActionResult<T>, MapFailure
│   ├── AuthController.cs
│   ├── ListingsController.cs
│   ├── MerchantsController.cs
│   ├── ShopsController.cs
│   ├── ReviewsController.cs
│   └── ...                          # one per resource family
├── Extensions/
│   └── ServiceExtensions.cs         # AddCoreServices, AddDatabaseServices, etc.
├── Middleware/
│   └── ExceptionHandlingMiddleware.cs
├── Services/
│   ├── CurrentUserService.cs        # ICurrentUserService impl (reads JWT)
│   ├── UserLookupService.cs         # IUserLookupService impl
│   ├── EmailSenderConfigReporter.cs # boot-time config validators
│   ├── OzowConfigReporter.cs
│   └── YocoConfigReporter.cs
├── Storage/
│   ├── R2MediaStorageService.cs     # IMediaStorageService for R2
│   ├── LocalFilesystemMediaStorageService.cs  # dev fallback
│   └── AzureBlobMediaStorageService.cs        # stub
├── appsettings.json
└── appsettings.Development.json
```

### 2.2 `ZansiHustle.Application`

Per-module folder layout. Each module owns its services + DTOs:

```
ZansiHustle.Application/
├── Common/
│   ├── Paging/PagedResult.cs
│   └── Interfaces/Shared/
│       ├── ICurrentUserService.cs
│       └── IUserLookupService.cs
├── Persistence/                     # Repository interfaces
│   ├── Identity/IJwtTokenGenerator.cs
│   ├── Listings/IListingRepository.cs
│   ├── Merchants/IMerchantRepository.cs
│   ├── Shops/IShopProfileRepository.cs
│   ├── Reviews/IReviewRepository.cs
│   └── ...
├── Auth/
│   ├── IAuthService.cs / AuthService.cs
│   └── Dtos/...                     # LoginDto, RegisterDto, AuthTokenDto
├── Listings/
│   ├── IListingService.cs / ListingService.cs
│   └── Dtos/                        # ListingDto, ListingVariantDto, ...
├── Merchants/
├── Shops/
├── Reviews/
├── Media/
│   ├── IMediaService.cs
│   └── Storage/IStorageUrlResolver.cs
└── ...                              # one folder per bounded subdomain
```

### 2.3 `ZansiHustle.Domain`

```
ZansiHustle.Domain/
├── Identity/
│   ├── User.cs                      # : IdentityUser<Guid>
│   ├── UserProfile.cs
│   ├── UserSettings.cs
│   └── RefreshToken.cs
├── Listings/Listing.cs + ListingVariant.cs
├── Merchants/Merchant.cs
├── Shops/ShopProfile.cs
├── Reviews/Review.cs
├── Orders/Order.cs + OrderItem.cs
├── Payments/Payment.cs + PaymentEvent.cs
├── SellerCategories/SellerCategory.cs + SellerSubcategory.cs
├── Marketplace/MarketplaceListing.cs (+ images)
├── Media/MediaAsset.cs
└── ... (Campaigns, Events, Fundraising, Influencers, etc.)
```

### 2.4 `ZansiHustle.Infrastructure`

```
ZansiHustle.Infrastructure/
├── Data/
│   ├── AppDbContext.cs              # IdentityDbContext<User, IdentityRole<Guid>, Guid>
│   └── Configurations/              # IEntityTypeConfiguration<T> per entity
│       ├── Listings/ListingConfiguration.cs
│       ├── Listings/ListingVariantConfiguration.cs
│       └── ...
├── Persistence/                     # Repository implementations + some configs
│   ├── Listings/ListingRepository.cs
│   ├── Shops/ShopProfileRepository.cs + ShopProfileConfiguration.cs
│   ├── Reviews/ReviewRepository.cs + ReviewConfiguration.cs
│   └── ...
├── Identity/
│   └── JwtTokenGenerator.cs
├── Configuration/                   # Options classes bound from appsettings
│   ├── JwtSettings.cs
│   ├── PaystackSettings.cs
│   ├── OzowSettings.cs
│   ├── YocoSettings.cs
│   └── EmailSenderSettings.cs
├── Communications/
│   ├── SmtpEmailProvider.cs
│   ├── TwilioSmsProvider.cs
│   ├── TwilioWhatsAppProvider.cs
│   ├── TwilioClientProvider.cs
│   └── InMemoryOtpStore.cs
├── Payments/
│   ├── Paystack/PaystackClient.cs
│   ├── Ozow/OzowClient.cs + OzowHashService.cs
│   └── Yoco/YocoClient.cs + YocoSignatureService.cs
└── Migrations/                      # 43+ files, YYYYMMDDhhmmss_Name.cs
```

### 2.5 `ZansiHustle.Shared`

```
ZansiHustle.Shared/
├── Results/
│   ├── Result.cs                    # non-generic envelope
│   └── ResultOfT.cs                 # Result<T> generic
├── Errors/
│   └── ErrorCodes.cs                # static class of code constants
├── Enums/
│   ├── Listings/ (ListingStatus, ListingType, ListingSource, AvailabilityMode, ...)
│   ├── Merchants/ (MerchantStatus, MerchantType, MerchantKycStatus)
│   ├── Shops/ (ShopProfileStatus, ShopSubscriptionStatus)
│   ├── Reviews/ (ReviewStatus, ReviewTargetType)
│   ├── Orders/ (OrderStatus, PaymentStatus)
│   ├── Media/ (MediaStatus, MediaKind, MediaPurpose, MediaVisibility)
│   └── ...
└── Queries/
    └── PagedListQueryBase.cs        # Page, PageSize, Status, Search, FromUtc, ToUtc
```

---

## 3. Request flow (end-to-end)

A real example — `PATCH /api/listings/{id}` with variants — exercises
every layer:

```
HTTP request
  │
  ▼
ListingsController.UpdateListing(id, UpdateListingRequestDto dto)
  │  [Authorize]
  │  ownerUserId = _currentUser.UserId
  ▼
IListingService.UpdateAsync(ownerUserId, id, dto)            ◄── Application
  │  • load listing via IMerchantRepository / IListingRepository
  │  • ownership: merchant.OwnerUserId == ownerUserId  (else 403)
  │  • apply scalar field changes (Title/Price/Status/etc.)
  │  • if dto.Variants != null:
  │      • assign fresh GUIDs to variants
  │      • call repo.SaveListingAndReplaceVariantsAsync(...)
  │  • else: repo.Update(listing); repo.SaveChangesAsync()
  ▼
IListingRepository.SaveListingAndReplaceVariantsAsync(...)   ◄── Application interface
  │
  ▼
ListingRepository (Infrastructure)
  │  await using var tx = await _db.Database.BeginTransactionAsync();
  │  await _db.ListingVariants
  │      .Where(v => v.ListingId == listing.Id)
  │      .ExecuteDeleteAsync();
  │  _db.ListingVariants.AddRange(newVariants);
  │  _db.Listings.Update(listing);
  │  await _db.SaveChangesAsync();
  │  await tx.CommitAsync();
  ▼
SQL Server
  │
  ▼  (back up the stack)
Service maps entity → ListingDto, wraps in Result<ListingDto>.Success(dto)
  │
  ▼
Controller: return ToActionResult(result);   ── BaseController.MapFailure on failure
  │
  ▼
HTTP 200  { "isSuccess": true, "message": "...", "data": { ... } }
```

Other clean modules to read first when learning the codebase:
**Listings** (rich, has variants), **Shops** (clean owner/public/admin
split), **Reviews** (polymorphic target, batched display lookup),
**Merchants** (largest surface, payout-eligibility nuances).

---

## 4. Result / error handling pattern

### 4.1 The envelope (`ZansiHustle.Shared.Results`)

```csharp
public class Result
{
    public bool   IsSuccess { get; }
    public string? Code     { get; }   // e.g. "FORBIDDEN", null on success
    public string  Message  { get; }

    public static Result Success(string message = "OK");
    public static Result Failure(string code, string message);
    public static Result Failure(string message);  // defaults code to "500"
}

public class Result<T> : Result
{
    public T? Data { get; }
    public static Result<T> Success(T data, string message = "OK");
    public static new Result<T> Failure(string code, string message);
}
```

**Every service method returns `Task<Result<T>>` or `Task<Result>`.**
No exceptions for expected failures.

### 4.2 Wire shape

```jsonc
// Success
{ "isSuccess": true,  "code": null,        "message": "OK", "data": { ... } }

// Failure
{ "isSuccess": false, "code": "FORBIDDEN", "message": "You do not have permission...", "data": null }
```

### 4.3 Error codes (`ZansiHustle.Shared.Errors.ErrorCodes`)

A static class of `const string` values. Selected examples:

| Code                          | Maps to | Used for                                     |
|-------------------------------|---------|----------------------------------------------|
| `BAD_REQUEST`                 | 400     | Generic input validation                     |
| `WEAK_PASSWORD`               | 400     | Password policy failure                      |
| `INVALID_PHONE_NUMBER`        | 400     | Phone format/validation                      |
| `OTP_INVALID`                 | 400     | OTP wrong                                    |
| `WEBHOOK_SIGNATURE_INVALID`   | 400     | Payment webhook HMAC mismatch                |
| `UNAUTHORIZED`                | 401     | Generic auth failure                         |
| `INVALID_CREDENTIALS`         | 401     | Login wrong                                  |
| `INVALID_REFRESH_TOKEN`       | 401     | Refresh missing/revoked                      |
| `REFRESH_TOKEN_EXPIRED`       | 401     | Refresh past TTL                             |
| `FORBIDDEN`                   | 403     | Ownership / permission                       |
| `INACTIVE_ACCOUNT`            | 403     | User account disabled                        |
| `EMAIL_NOT_CONFIRMED`         | 403     | Email verification required                  |
| `NOT_FOUND`                   | 404     | Missing resource                             |
| `CONFLICT`                    | 409     | Generic conflict                             |
| `EMAIL_TAKEN`                 | 409     | Duplicate email on register                  |
| `PAYMENT_ALREADY_PAID`        | 409     | Idempotency / replay                         |
| `PAYMENT_AMOUNT_MISMATCH`     | 409     | Tampering / drift                            |
| `TOO_MANY_REQUESTS`           | 429     | Generic rate limit                           |
| `OTP_RESEND_COOLDOWN`         | 429     | OTP resend too soon                          |
| `EMAIL_SEND_FAILED`           | 502     | Upstream SMTP failure                        |
| `SMS_SEND_FAILED`             | 502     | Upstream Twilio failure                      |
| `WHATSAPP_SEND_FAILED`        | 502     | Upstream Twilio WhatsApp failure             |
| `PAYMENT_INIT_FAILED`         | 502     | Gateway init failed                          |
| `PROVIDER_NOT_CONFIGURED`     | 503     | Missing creds (graceful soft-fail at runtime)|
| (anything else)               | 500     | `EXCEPTION` / unmapped                       |

### 4.4 `BaseController.MapFailure`

A single switch in `BaseController.cs` maps codes → HTTP status codes.
Adding a new code is one extra line. Controllers never set status codes
themselves — they call `ToActionResult(result)` and trust the mapping.

### 4.5 How exceptions are handled

- **Service layer** wraps work in `try { ... } catch (Exception ex)`
  and returns `Result.Failure(ErrorCodes.EXCEPTION, ex.Message)`. The
  service logs (`_logger.LogError(ex, "...")`) so the stack is in the
  app log, but the wire shape stays uniform.
- **Unhandled exceptions** are caught by `ExceptionHandlingMiddleware`,
  which returns HTTP 500 with a generic body. Exception details are
  exposed only when the environment is non-Production **or** when the
  config flag `Diagnostics:ExposeExceptionDetails` is true. A trace ID
  is always returned.

### 4.6 How to avoid fake success

- **Never** `return Result.Success()` from inside a `catch`.
- **Never** swallow a repository return value — if
  `SaveChangesAsync()` returns false, return a failure.
- **Always** propagate `Result.Failure` from inner helpers; don't
  re-wrap them as success.

---

## 5. DTO pattern

### 5.1 Naming convention

- `*Dto` — full read projection (admin/owner view)
- `*PublicDto` — narrow buyer-safe projection (no bank, KYC, owner)
- `*RequestDto` — write payload (create or update)
- `*ResponseDto` — response wrapper when not a plain Dto (e.g.
  `IssueUploadResponseDto`)
- `*FilterRequestDto` — query params bound via `[FromQuery]`
- `Create*RequestDto` / `Update*RequestDto` — distinct types; never
  reuse the create DTO for update.

### 5.2 Folder convention

Each module has a **flat** `Dtos/` folder. No `Requests/Responses/`
sub-nesting; keeps the import path short.

### 5.3 Update DTOs are nullable

`Update*RequestDto` properties are nullable. **Omitted = no change.**
Example:

```csharp
public class UpdateListingRequestDto
{
    public string?            Title           { get; set; }
    public decimal?           Price           { get; set; }
    public ListingStatus?     Status          { get; set; }
    public AvailabilityMode?  AvailabilityMode{ get; set; }
    // ...
    public List<ListingVariantRequestDto>? Variants { get; set; }
    // null   → leave variants untouched
    // []     → clear all variants
    // [...]  → REPLACE all variants atomically
}
```

This is the deliberate convention: **patch semantics**, not partial-
merge. Easy to reason about, no diff bugs.

### 5.4 Public DTO projections

Public DTOs are emphatically **narrower**. The `ShopProfileDto` exposes
`SubscriptionStatus`, `SuspensionReason`, `BillingProvider`,
`MerchantId`; the `ShopProfilePublicDto` exposes only what a buyer
should see — name, slug, description, logo/banner, rating, review
count, created time. Same for `MerchantDto` vs `MerchantPublicDto`.

**Build public DTOs from day one for any externally-visible entity.**
Retrofitting them later is one of the bigger pains we hit.

### 5.5 Mapping

Mapping is done by hand in services (no AutoMapper). Pros: explicit,
greppable, no startup magic, no runtime surprises. Cons: verbose for
big entities. The trade is worth it.

---

## 6. Entity / model pattern

### 6.1 Where entities live

`ZansiHustle.Domain/<Module>/<Entity>.cs`. POCOs only — no EF
attributes (no `[Required]`, `[MaxLength]`, `[Key]`). All metadata
moves into `IEntityTypeConfiguration<T>` in Infrastructure.

### 6.2 Conventions observed

- **Primary keys**: `Guid Id` everywhere. No int PKs in domain
  entities. Rationale: distributed-system safe; no PK leakage in URLs.
- **Timestamps**: `DateTime CreatedAtUtc` (required, set on creation),
  `DateTime? UpdatedAtUtc` (nullable). UTC-only — no timezone juggling.
- **Status fields**: Enums in `Shared/Enums/...`, stored as `int`
  through `.HasConversion<int>()`. Sorting + indexes stay efficient.
- **Soft-delete**: Not universal. Used where audit matters
  (`Review.DeletedAtUtc`, `ShopProfile.SuspendedAtUtc`,
  `Order.CancelledAtUtc`). Combined with a `Status` enum that flips to
  `Deleted`/`Suspended`/`Cancelled`. Hard delete is fine for child
  rows (`ListingVariant` cascades).
- **Navigation properties**: **Not `virtual`** — lazy-loading proxies
  are not used. Eager loading is explicit per repository
  (`.Include(x => x.Merchant)` etc.).
- **No base class**: There's no `BaseEntity` or `AggregateRoot`.
  Timestamp fields are repeated by hand on each entity. This is a
  conscious trade — readability over DRY. (A `BaseEntity { Guid Id,
  CreatedAtUtc, UpdatedAtUtc }` is a fine simplification for a new
  project; see §17.)
- **Denormalised aggregates**: `Rating`, `ReviewCount`,
  `FollowersCount`, `TotalOrders`, `TotalRevenue` live on the parent
  entity (Merchant, ShopProfile, Listing) and are refreshed by service
  methods. Trade: read speed vs eventual consistency. Easy to recompute.

### 6.3 Sample entities (selected fields)

**Listing** (`Domain/Listings/Listing.cs`)
- Identity: `Id`, `Code`, `Slug`
- Type & state: `Type` (Product/Service), `Status`, `AvailabilityMode`,
  `ListingSource` (SellerAccount/ShopProfile/PhysicalStore)
- Ownership: `MerchantId` (+ nav `Merchant`), `ShopProfileId?` (+ nav)
- Content: `Title`, `Description`, `Price` (precision 18,2), `Currency`
- Taxonomy: `SellerCategoryId?`, `SellerSubcategoryId?`
- Location: `Province?`, `City?`
- Media: `List<string> Images` (JSON column)
- Engagement: `IsFeatured`, `IsBoosted`, `Rating?`, `ReviewCount`
- Product-only: `Stock?`, `Condition?`, `DeliveryOptions?` (JSON)
- Service-only: `PricingModel?`, `ServiceArea?`, `Turnaround?`,
  `Availability?` (JSON), `BookingMethods?` (JSON)
- Children: `List<ListingVariant> Variants`
- Audit: `CreatedAtUtc`, `UpdatedAtUtc?`

**ListingVariant** (`Domain/Listings/ListingVariant.cs`)
- `Id`, `ListingId` (+ nav `Listing`)
- `Name`, `Description?`
- `Price?` + `UsesCustomPrice` (variant price wins only when flag true)
- `Stock?` (variant-level inventory; falls back to listing.Stock when null)
- `Sku?`, `SortOrder`, `IsActive`
- `CreatedAtUtc`, `UpdatedAtUtc?`

**ShopProfile** (`Domain/Shops/ShopProfile.cs`)
- `Id`, `MerchantId` (FK, restrict delete), `Slug` (globally unique)
- `Name`, `Description?`, `LogoUrl?`, `BannerUrl?`
- Shop-specific contact (independent of merchant):
  `ContactEmail?`, `ContactPhoneNumber?`, `WhatsAppNumber?`
- Location: `Province?`, `City?`, `AddressLine1?`
- Lifecycle: `Status` (Draft/Active/Suspended/PendingReview),
  `SubscriptionStatus` (None/EarlyAccess/Trial/Active/PastDue/
  Cancelled/Expired), `EarlyAccessGrantedAtUtc?`, `EarlyAccessUntilUtc?`,
  `SubscriptionStartedAtUtc?`, `SubscriptionEndsAtUtc?`,
  `ActivatedAtUtc?`, `SuspendedAtUtc?`, `SuspensionReason?`
- Billing (reserved): `BillingProvider?`, `BillingReference?`
- Aggregates: `Rating?`, `ReviewCount`
- Audit: `CreatedAtUtc`, `UpdatedAtUtc?`

**Merchant** (`Domain/Merchants/Merchant.cs`)
- Identity: `Id`, `Code`, `Slug`, `Name`, `Description?`, `Type`,
  `Status`, `KycStatus`, `OwnerUserId?`, `IsPayoutEligible`
- Classification: `SellerCategoryId?`, `SellerSubcategoryId?`
- Contact: `ContactEmail?`, `ContactPhoneNumber?`, `WhatsAppNumber?`,
  `SocialHandle?`
- Address (full Google Places set): `Province?`, `City?`, `AddressLine1?`,
  `Suburb?`, `PostalCode?`, `Country?`, `CountryCode?`,
  `Latitude?`/`Longitude?` (precision 9,6), `GooglePlaceId?`,
  `FormattedAddress?`
- Brand: `WebsiteUrl?`, `LogoUrl?`, `BannerUrl?`
- Bank (sensitive — TODO encryption): `BankName?`, `BankAccountHolder?`,
  `BankAccountNumber?`, `BankAccountType?`, `BankBranchCode?`,
  `IsBankVerified`, `BankUpdatedAtUtc?`
- KYC (sensitive): `IdNumber?`, `ReferralCode?`, `ReferrerUserId?`
- Aggregates: `FollowersCount`, `Rating?`, `ReviewCount`, `TotalOrders`,
  `TotalRevenue` (precision 18,2)
- Audit: `CreatedAtUtc`, `UpdatedAtUtc?`

**Review** (`Domain/Reviews/Review.cs`)
- `Id`, `TargetType` (polymorphic), `TargetId`, `ReviewerUserId`
- `Rating` (1–5, CHECK constraint), `Comment?`, `Status`
- `CreatedAtUtc`, `UpdatedAtUtc?`, `DeletedAtUtc?` (soft delete)

**User** (`Domain/Identity/User.cs`) — extends `IdentityUser<Guid>`
- `FirstName`, `LastName`, `IsActive`, `AccountStatus`,
  `CreatedOnUtc`, `UpdatedOnUtc?`

---

## 7. EF Core / Infrastructure pattern

### 7.1 `AppDbContext`

- Class: `AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>`
- Location: `Infrastructure/Data/AppDbContext.cs`
- DbSets: one per aggregate root (Merchants, Listings, ListingVariants,
  ShopProfiles, Orders, OrderItems, Payments, PaymentEvents, Reviews,
  MediaAssets, MarketplaceListings, RefreshTokens, UserProfiles,
  UserSettings, SellerCategories, ...).
- `OnModelCreating`:
  - `base.OnModelCreating(builder);` — Identity tables
  - `builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());`
    — autoloads every `IEntityTypeConfiguration<T>`.
  - A few inline configurations remain for legacy Identity-adjacent
    tables (`UserProfile`, `UserSettings`, `RefreshToken`).

### 7.2 Entity configurations

One file per entity, implementing `IEntityTypeConfiguration<T>`.
Located under either `Data/Configurations/<Module>/` or
`Persistence/<Module>/` (the codebase has both — collapse to one for a
new project).

Examples of patterns used:

- **Indexes**: define them on hot query paths. `ListingConfiguration`
  indexes Code, Slug (unique), Type, Status, AvailabilityMode,
  MerchantId, ListingSource, ShopProfileId, Category IDs, Price, City,
  Province, CreatedAtUtc, IsFeatured, IsBoosted.
- **Enum conversion**: `builder.Property(x => x.Status).HasConversion<int>();`
- **Decimal precision**: `Price` → `HasPrecision(18, 2)`; `Rating` →
  `HasPrecision(5, 2)`.
- **JSON columns**: `List<string>` fields stored as JSON via a custom
  `ValueConverter<List<string>, string>` + matching `ValueComparer`.
  Used for Images, DeliveryOptions, Availability, BookingMethods.
- **Foreign-key delete behaviors**:
  - `Listing.Merchant` — `OnDelete(DeleteBehavior.Cascade)`
  - `Listing.ShopProfile` — `OnDelete(DeleteBehavior.Restrict)`
  - `Listing.SellerCategory` — `OnDelete(DeleteBehavior.SetNull)`
  - `Listing.SellerSubcategory` — `OnDelete(DeleteBehavior.NoAction)`
    (avoids "multiple cascade paths" on SQL Server)
  - `Review.User` — `NoAction` (preserve audit on user deletion)
- **Filtered unique indexes**: `ShopProfileConfiguration` enforces one
  active shop per merchant via a filtered index `[Status] <> 3`.
- **CHECK constraints**: `Review.Rating BETWEEN 1 AND 5`.

### 7.3 Migrations

- Folder: `Infrastructure/Migrations/`
- Naming: `YYYYMMDDhhmmss_<DescriptiveName>.cs`
- Count: 43+ at time of writing
- Commands (run from solution root, with `Infrastructure` as the
  project and `API` as the startup project):
  ```
  dotnet ef migrations add <Name> -p ZansiHustle.Infrastructure -s ZansiHustle.API
  dotnet ef database update            -p ZansiHustle.Infrastructure -s ZansiHustle.API
  dotnet ef migrations remove          -p ZansiHustle.Infrastructure -s ZansiHustle.API
  ```
- Migrations are applied at app startup via `app.SeedApplicationAsync()`
  which calls `db.Database.MigrateAsync()` before seeding roles and
  event templates.

### 7.4 Query patterns in repositories

- **AsNoTracking**: default for read queries. Mutating queries pull
  tracked entities deliberately.
- **Explicit Includes**: every read decides what it needs. No global
  navigation strategies.
- **Projection**: complex search reads project to anonymous types or
  use entity reads followed by service-side mapping (no `.Select(x =>
  new SomeDto(...))` inside repositories — keep DTOs in Application).
- **Transactions**: explicit `await using var tx = await
  _db.Database.BeginTransactionAsync();` for multi-statement atomic
  operations (e.g. `SaveListingAndReplaceVariantsAsync`).
- **Bulk delete**: EF 7+ `ExecuteDeleteAsync()` used for wholesale
  variant clears — far cheaper than tracking + per-row delete.

### 7.5 Soft-delete practices

- No `IsDeleted` global query filter. Soft-delete is explicit per
  domain: queries against Reviews/Shops/Orders filter on `Status`
  explicitly. This is deliberate — global filters are easy to forget
  to bypass for admin endpoints.

### 7.6 Indexes and constraints — rules of thumb

- Any column you filter on in a "/public" search → index it.
- Any column with a `Status`/`Type` enum used in lists → index it
  (composite with `CreatedAtUtc` for keyset paging).
- Slug → unique index.
- "One active X per owner" → filtered unique index excluding the
  inactive state.
- Numeric ranges (rating, price) → `HasPrecision`.

---

## 8. Repository / service pattern

### 8.1 Repository interface convention

In `Application/Persistence/<Module>/I<Entity>Repository.cs`:

```csharp
public interface IListingRepository
{
    Task<(List<Listing> Items, int Total)> SearchAsync(ListingFilterRequestDto filter);
    Task<Listing?>     GetByIdAsync(Guid id);
    Task<Listing?>     GetBySlugAsync(string slug);
    Task<List<Listing>>GetByMerchantAsync(Guid merchantId);
    Task<List<Listing>>GetByShopProfileAsync(Guid shopProfileId);
    Task<List<Listing>>GetByOwnerAsync(Guid ownerUserId);
    Task<bool>         ExistsBySlugAsync(string slug);
    Task AddAsync(Listing listing);
    void Update(Listing listing);
    void Delete(Listing listing);
    Task<bool> SaveChangesAsync();

    // Atomic variant-replace operation (see §3 + §17)
    Task<bool> SaveListingAndReplaceVariantsAsync(
        Listing listing,
        IReadOnlyList<ListingVariant>? newVariantsOrNull);
}
```

Rules:
- `Task<T?> GetXxxAsync(...)` returns null for "not found".
- `Add` is async (EF may resolve PK), `Update`/`Delete` are sync (just
  mark state).
- Every repo owns its `SaveChangesAsync()`. **No `IUnitOfWork`** —
  services that need to combine multiple repos coordinate themselves.
  (For a serious new project, see §17 — adding a tiny `IUnitOfWork`
  with `SaveChangesAsync()` on it is a worthwhile simplification.)

### 8.2 Service interface convention

In `Application/<Module>/I<Module>Service.cs`. Every method returns
`Task<Result<T>>` or `Task<Result>`. Selected example:

```csharp
public interface IListingService
{
    Task<Result<PagedResult<ListingListItemDto>>> SearchAsync(ListingFilterRequestDto filter);
    Task<Result<ListingDto>>                       GetByIdAsync(Guid id);
    Task<Result<List<ListingListItemDto>>>         GetByMerchantAsync(Guid merchantId);
    Task<Result<List<ListingListItemDto>>>         GetByShopProfileAsync(Guid shopProfileId);
    Task<Result<List<ListingListItemDto>>>         GetMineAsync(Guid ownerUserId);
    Task<Result<ListingDto>>                       CreateAsync(Guid ownerUserId, CreateListingRequestDto request);
    Task<Result<ListingDto>>                       UpdateAsync(Guid ownerUserId, Guid listingId, UpdateListingRequestDto request);
    Task<Result>                                    DeleteAsync(Guid ownerUserId, Guid listingId);
}
```

Notes:
- Mutating methods take `ownerUserId` as the first param. This forces
  the controller to pass the JWT user — keeping ownership checks
  unmissable.
- Public read methods (no `ownerUserId`) imply public/anonymous
  surface and apply narrow projections.
- Admin methods are separate (`SearchAdminAsync(...)`,
  `SuspendAsync(...)`).

### 8.3 DI registration

All wiring lives in `ZansiHustle.API/Extensions/ServiceExtensions.cs`,
grouped by concern:

- `AddCoreServices()` — controllers + Swagger + user/profile/settings
- `AddDatabaseServices(config)` — `AppDbContext` + connection switching
  (UAT vs Live via `Database:UseLive`)
- `AddIdentityServices(config)` — Identity + JWT bearer (strict password
  policy, fail-startup if `JwtSettings:Key` missing)
- `AddInfrastructureServices()` — repositories + `IJwtTokenGenerator`
- `AddAuthServices()` — `IAuthService`
- `AddApiServices()` — `ICurrentUserService`, `IUserLookupService`
- `AddEmailServices()`, `AddTwilioCommunications()`, `AddOtpServices()`
- `AddPaystackPayments()`, `AddOzowPayments()`, `AddYocoPayments()`
- `AddMarketingAndOperationsServices()` — the big DI block for
  feature services (27+ repos/services).
- `AddCustomCors()` — named `FrontendCors` policy
- `ConfigureMiddleware()` — pipeline assembly
- `SeedApplicationAsync()` — migrate + seed roles + seed templates

### 8.4 Where business validation belongs

- **DTO-shape validation** (required, max length) — in services, not
  via `[Required]` data annotations. The codebase keeps DTOs as plain
  POCOs and validates explicitly in the service entry. Trade: more
  code; gain: error messages match `Result.Failure(BAD_REQUEST, ...)`
  shape consistently.
- **Business validation** (status transitions, "must be Active",
  "must own this merchant") — in services.
- **Constraints** (unique slug, FK existence) — defence-in-depth at
  both service and DB level.

### 8.5 Where ownership / permission checks belong

**Service layer, always.** Pattern:

```csharp
public async Task<Result<ListingDto>> CreateAsync(Guid ownerUserId, CreateListingRequestDto request)
{
    var merchant = await _merchantRepository.GetByIdAsync(request.MerchantId);
    if (merchant is null)
        return Result<ListingDto>.Failure(ErrorCodes.NOT_FOUND, "Merchant not found.");

    if (merchant.OwnerUserId != ownerUserId)
        return Result<ListingDto>.Failure(ErrorCodes.FORBIDDEN,
            "You do not have permission to add listings to this shop.");

    if (merchant.Status != MerchantStatus.Active)
        return Result<ListingDto>.Failure(ErrorCodes.FORBIDDEN,
            "Your shop must be approved before you can publish listings.");

    // ... rest of create
}
```

**Never** trust the controller to enforce ownership.

### 8.6 Three-tier visibility (a hard convention)

For every public-facing resource, services expose three flavours:

| Tier   | Method shape                                    | Returns                 | Auth          |
|--------|-------------------------------------------------|-------------------------|---------------|
| Public | `GetPublicByIdAsync(id)`, `SearchPublicAsync(q)` | `*PublicDto`            | AllowAnonymous|
| Mine   | `GetMineAsync(ownerUserId)`, `UpdateMineAsync`  | `*Dto` (full)           | [Authorize]   |
| Admin  | `SearchAdminAsync(filter)`, `SuspendAsync`      | `*Dto` (full, any state)| [Authorize] + role |

Public surfaces filter to `Status == Active` and omit sensitive fields
(bank, KYC, owner, billing). Mine surfaces are scoped by JWT
`OwnerUserId`. Admin surfaces see everything.

---

## 9. Authentication / authorization pattern

### 9.1 Setup (`AddIdentityServices` + `AddAuthServices`)

- ASP.NET Identity with `User : IdentityUser<Guid>` and
  `IdentityRole<Guid>`.
- Password policy: 8+ chars, upper + lower + digit + non-alphanumeric.
  Unique email required. Email confirmation **not** required at login
  — sensitive operations gate on `EmailConfirmed` per feature.
- JWT bearer with strict validation: issuer, audience, lifetime,
  signing key. **ClockSkew = TimeSpan.Zero** (no token grace).
- `JwtSettings:Key` is validated at startup — missing throws
  `InvalidOperationException`. Same for connection strings.

### 9.2 Current-user access (`ICurrentUserService`)

Defined in `Application/Common/Interfaces/Shared`. Implemented in
`API/Services/CurrentUserService.cs`:

```csharp
public interface ICurrentUserService
{
    Guid?  UserId          { get; }
    string? Email           { get; }
    bool    IsAuthenticated { get; }
}
```

Reads `ClaimTypes.NameIdentifier` (or custom `"user_id"`) for the Guid;
`ClaimTypes.Email` for email. Used by controllers to pass `ownerUserId`
into services. **Application services never see `HttpContext`.**

### 9.3 JWT token generation (`IJwtTokenGenerator`)

In `Infrastructure/Identity/JwtTokenGenerator.cs`:

```csharp
public interface IJwtTokenGenerator
{
    Task<AuthTokenDto>     GenerateTokenAsync(User user);
    Task<AuthTokenDto?>    RefreshTokenAsync(string refreshToken);
    Task<bool>             RevokeRefreshTokenAsync(string refreshToken);
    Task                    RevokeAllRefreshTokensForUserAsync(Guid userId);
}
```

Claims baked into the JWT: `Sub`, `Jti`, `Email`, `GivenName`,
`FamilyName`, `NameIdentifier`, `user_id` (Guid string),
`account_status`, `platform_role` (one per role).

Refresh tokens are stored in the `RefreshTokens` table; refresh
rotates the token in a transaction.

### 9.4 Authorize patterns

- **Controller default**: `[Authorize]` on the class.
- **Public endpoints**: `[AllowAnonymous]` per action (or per
  controller for purely public ones).
- **Admin role**: `[Authorize(Roles = "Admin")]` per action where
  admin-only operations live (suspend/reactivate, approve/reject KYC).
- **No policy-based auth** in use today; roles are sufficient.

### 9.5 Designing secure endpoints

For every endpoint, decide which of the three tiers it is:

- Public → `[AllowAnonymous]` + `*PublicDto` + status filter `Active`
- Mine → `[Authorize]` + pass `_currentUser.UserId!.Value` into the
  service + service does the ownership check
- Admin → `[Authorize(Roles="Admin")]` + admin-flavoured service
  methods

---

## 10. Controller pattern

### 10.1 Thin controllers

Controllers:
1. Bind input
2. Read `_currentUser.UserId` if needed
3. Call exactly one service method
4. Return `ToActionResult(result)`

That's it. No DB access, no business validation, no try/catch.

### 10.2 Routes

- `[Route("api/[controller]")]` — controller name without the
  `Controller` suffix becomes the segment (e.g. `ShopsController` →
  `/api/shops`).
- Plural resource names.
- Three URL conventions inside each controller:
  - `GET  /api/{res}/public`, `GET /api/{res}/public/{id}` — anonymous
  - `GET  /api/{res}/mine`, `POST /api/{res}/mine`,
    `PATCH /api/{res}/mine/{id}` — authenticated, scoped to caller
  - `GET  /api/{res}`, `POST /api/{res}/{id}/{verb}` — admin

### 10.3 Real example: `ShopsController`

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShopsController : BaseController
{
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine()
        => ToActionResult(await _shops.GetMineAsync(_currentUser.UserId!.Value));

    [HttpPost("mine")]
    public async Task<IActionResult> CreateMine(CreateShopRequestDto dto)
        => ToActionResult(await _shops.CreateMineAsync(_currentUser.UserId!.Value, dto));

    [HttpPatch("mine/{shopId:guid}")]
    public async Task<IActionResult> UpdateMine(Guid shopId, UpdateShopRequestDto dto)
        => ToActionResult(await _shops.UpdateMineAsync(_currentUser.UserId!.Value, shopId, dto));

    [AllowAnonymous]
    [HttpGet("public")]
    public async Task<IActionResult> SearchPublic([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? q = null)
        => ToActionResult(await _shops.SearchPublicAsync(page, pageSize, q));

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPublic(Guid id)
        => ToActionResult(await _shops.GetPublicByIdAsync(id));

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SearchAdmin([FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] ShopProfileStatus? status = null, [FromQuery] string? q = null)
        => ToActionResult(await _shops.SearchAdminAsync(page, pageSize, status, q));

    [HttpPost("{id:guid}/suspend")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Suspend(Guid id, [FromBody] SuspendShopDto dto)
        => ToActionResult(await _shops.SuspendAsync(id, dto.Reason));

    [HttpPost("{id:guid}/reactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reactivate(Guid id)
        => ToActionResult(await _shops.ReactivateAsync(id));
}
```

### 10.4 BaseController

```csharp
public abstract class BaseController : ControllerBase
{
    protected IActionResult ToActionResult(Result result)        => ...; // 200 or MapFailure
    protected IActionResult ToActionResult<T>(Result<T> result)  => ...; // 200 or MapFailure
    private   IActionResult MapFailure(string? code, string message);   // switch → 400/401/403/404/409/429/500/502/503
}
```

---

## 11. External integrations

### 11.1 Cloudflare R2 storage (`AWSSDK.S3`)

The S3 SDK is used because R2 is S3-compatible. Config under
`Storage:R2`:

```jsonc
"Storage": {
  "R2": {
    "AccountId":     "<set at runtime>",
    "Endpoint":      "https://<accountId>.r2.cloudflarestorage.com",
    "AccessKey":     "<set at runtime>",
    "SecretKey":     "<set at runtime>",
    "PublicBaseUrl": "https://pub-<hash>.r2.dev",
    "PrivateBucket": "<bucket>",
    "PublicBucket":  "<bucket>"
  }
}
```

- If `R2:AccountId` is present, DI registers `R2MediaStorageService`;
  otherwise it falls back to `LocalFilesystemMediaStorageService`
  (uses `Media:LocalBasePath` or `{ContentRoot}/App_Data/_media`,
  HMAC-signed URLs).
- `IMediaStorageService` is the contract (Application). Implementations
  live in `API/Storage/`.
- **Public assets**: signed PUT bakes
  `Cache-Control: public, max-age=31536000, immutable` into the
  signature; clients must echo the header on PUT or R2 returns 403.
  Read URLs are constructed as `{PublicBaseUrl}/{storageKey}` —
  permanent, CDN-cacheable.
- **Private assets** (KYC, IDs): short-TTL signed GET URLs.

### 11.2 `IStorageUrlResolver` (URL refresh on read)

```csharp
public interface IStorageUrlResolver
{
    Task<string?> RefreshAsync(string? storedUrl);
}
```

Called per-asset on read paths. If `storedUrl` points at our R2 bucket
and is a signed URL nearing expiry, it re-issues a fresh signed URL.
Permanent CDN URLs (i.e. anything matching `PublicBaseUrl`) pass
through unchanged. **Always** route DTO URL fields through this on the
way out.

### 11.3 URL normalisation rules — DO and DO NOT

- ✅ Store the **R2 storage key** OR a permanent CDN URL (`{PublicBaseUrl}/{key}`).
- ✅ Resolve to a fresh URL at read time when needed.
- ❌ **Never** persist `file://`, `blob:`, `content://`, `data:`,
  Expo cache URIs, or any other client-local URI in the database.
  Frontend must finalise upload → backend stores the storage key →
  responses always return resolved URLs.

### 11.4 Email, SMS, WhatsApp

- **Email**: SMTP via `SmtpEmailProvider` (custom). Multiple senders
  configured under `EmailProviders:Senders` (`Default`, `Accounts`,
  `Support`, `NoReply`, `Security`, `Payments`). `EmailSenderConfigReporter`
  is a hosted service that validates each sender at boot and logs
  **field names only** (never secrets).
- **SMS**: `TwilioSmsProvider` (REST v2010). `TwilioClientProvider` is
  singleton.
- **WhatsApp**: `TwilioWhatsAppProvider`.
- **Phone verification**: Twilio Verify service.
- **OTP**: `InMemoryOtpStore` (replace with Redis/SQL for scale).
- Failure codes: `EMAIL_SEND_FAILED`, `SMS_SEND_FAILED`,
  `WHATSAPP_SEND_FAILED`, `PROVIDER_NOT_CONFIGURED`.

### 11.5 Payments (Paystack, Ozow, Yoco)

Each gateway has its own folder and typed HTTP client:

- `Infrastructure/Payments/Paystack/PaystackClient.cs`
- `Infrastructure/Payments/Ozow/OzowClient.cs` +
  `OzowHashService.cs` (SHA-512 webhook signature)
- `Infrastructure/Payments/Yoco/YocoClient.cs` +
  `YocoSignatureService.cs` (HMAC-SHA256 Standard Webhooks)

At boot, `OzowConfigReporter` and `YocoConfigReporter` validate
credentials and warn on:
- missing fields (lists exact env-var names to set)
- URLs containing `localhost`/`127.0.0.1`/`0.0.0.0` (unreachable from
  the gateway's network)
- non-HTTPS webhook URLs
- `UatTestMode = true` (caps transactions at a tiny test amount).

Webhook signature failures return `WEBHOOK_SIGNATURE_INVALID` (400);
duplicate completions return `PAYMENT_ALREADY_PAID` (409); amount
drift returns `PAYMENT_AMOUNT_MISMATCH` (409).

---

## 12. Validation rules

### 12.1 Where validation lives

- **Model binding** (type coercion, JSON shape) — ASP.NET model
  binder.
- **Required / shape** validation — explicit checks in the service
  entry, returning `Result.Failure(BAD_REQUEST, ...)`. No
  `[Required]` data annotations on DTOs.
- **Business validation** (status transitions, "must be Active",
  duplicates) — service layer.
- **DB constraints** (unique slug, FK, CHECK) — defence-in-depth.

### 12.2 Examples

**Listing variants — replace semantics, capped at 20**
```csharp
if (request.Variants is { Count: > 20 })
    return Result<ListingDto>.Failure(ErrorCodes.BAD_REQUEST,
        "A listing can have at most 20 variants.");
```

The full update path uses
`IListingRepository.SaveListingAndReplaceVariantsAsync(listing, variants)`:
- `null` → leave variants untouched
- `[]` → delete all variants
- non-empty → atomic delete + insert in a single transaction

This replaces a previous diff-based approach that produced
`DbUpdateConcurrencyException` false 409s when tracked navigation
state tangled. **Prefer replace over diff for child collections** when
the child rows aren't referenced elsewhere.

**Shop creation — caller must own an approved OnlineStore merchant**
```csharp
var merchant = (await _merchantRepository.GetByOwnerAsync(ownerUserId))
    .FirstOrDefault(m => m.Type == MerchantType.OnlineStore);

if (merchant is null)
    return Result<ShopProfileDto>.Failure(ErrorCodes.FORBIDDEN,
        "You need an approved online-seller account before opening a shop.");

if (merchant.Status != MerchantStatus.Active)
    return Result<ShopProfileDto>.Failure(ErrorCodes.FORBIDDEN,
        "Your shop must be approved before you can publish listings.");
```

**Duplicate slug — 409 not 400**
```csharp
if (await _shopRepository.SlugExistsAsync(slug))
    return Result<ShopProfileDto>.Failure(ErrorCodes.CONFLICT,
        "A shop with this slug already exists.");
```

### 12.3 Picking the right code

| Situation                              | Code             | HTTP |
|----------------------------------------|------------------|------|
| Missing/empty/badly-shaped field       | `BAD_REQUEST`    | 400  |
| Login wrong                            | `INVALID_CREDENTIALS` | 401 |
| Caller authenticated but not allowed   | `FORBIDDEN`      | 403  |
| Resource doesn't exist                 | `NOT_FOUND`      | 404  |
| Unique-constraint duplicate            | `CONFLICT` / `EMAIL_TAKEN` | 409 |
| Webhook idempotency replay             | `PAYMENT_ALREADY_PAID` | 409 |
| External provider failure              | `EMAIL_SEND_FAILED` / `PAYMENT_INIT_FAILED` | 502 |
| Provider not configured                | `PROVIDER_NOT_CONFIGURED` | 503 |
| Unexpected exception                   | `EXCEPTION`      | 500  |

---

## 13. New module checklist

Adding a new bounded subdomain (e.g. `Bookings`) — copy this list:

- [ ] **Domain entity** in `ZansiHustle.Domain/Bookings/Booking.cs`
      (POCO, no EF attributes, `Guid Id`, `CreatedAtUtc`,
      `UpdatedAtUtc?`).
- [ ] **Enums** in `ZansiHustle.Shared/Enums/Bookings/`
      (`BookingStatus`, `BookingType`, etc.).
- [ ] **Error codes** in `ErrorCodes.cs` if new ones are needed
      (e.g. `BOOKING_SLOT_TAKEN`).
- [ ] **DTOs** in `ZansiHustle.Application/Bookings/Dtos/`:
      `BookingDto`, `BookingPublicDto`, `CreateBookingRequestDto`,
      `UpdateBookingRequestDto`, `BookingFilterRequestDto`.
- [ ] **Repository interface** in
      `Application/Persistence/Bookings/IBookingRepository.cs`.
- [ ] **Service interface + impl** in
      `Application/Bookings/IBookingService.cs` + `BookingService.cs`.
- [ ] **EF configuration** in
      `Infrastructure/Persistence/Bookings/BookingConfiguration.cs`
      (indexes, conversions, FK behaviour).
- [ ] **Repository impl** in
      `Infrastructure/Persistence/Bookings/BookingRepository.cs`.
- [ ] **DbSet** added to `AppDbContext`.
- [ ] **DI registration** in `ServiceExtensions.cs`
      (`AddMarketingAndOperationsServices` or a new bucket method).
- [ ] **Migration**: `dotnet ef migrations add AddBookings -p ZansiHustle.Infrastructure -s ZansiHustle.API`.
- [ ] **Controller** in `API/Controllers/BookingsController.cs`
      with the three-tier endpoints (public / mine / admin).
- [ ] **Map `BaseController.MapFailure`** if any new error codes need
      a non-default status.
- [ ] **BookingsController** smoke-tested through Swagger (Public,
      Mine, Admin paths).
- [ ] **Frontend API types** (e.g. `ZansiHustleApp/src/features/
      bookings/api/bookings.types.ts`) updated to mirror DTOs.
- [ ] **React Query keys** namespaced uniquely (e.g.
      `['bookings','mine']`, `['bookings','public', { page }]`) — do
      not reuse another module's key shape.

---

## 14. Naming conventions

| Concept                       | Convention                                  |
|-------------------------------|---------------------------------------------|
| Domain entity                 | `Listing`, `ShopProfile`, `MediaAsset` (singular noun) |
| Primary key                   | `Id : Guid`                                 |
| Foreign key                   | `<Entity>Id` + nav `<Entity>` (`MerchantId`, `Merchant`) |
| Created timestamp             | `CreatedAtUtc` (Identity tables use `CreatedOnUtc`) |
| Updated timestamp             | `UpdatedAtUtc` (nullable)                   |
| Soft-delete timestamp         | `DeletedAtUtc`, `SuspendedAtUtc`, etc.      |
| Status field                  | `Status : <Module>Status` enum              |
| Service interface             | `I<Module>Service`                          |
| Service impl                  | `<Module>Service`                           |
| Repository interface          | `I<Entity>Repository`                       |
| Repository impl               | `<Entity>Repository`                        |
| Controller                    | `<Resources>Controller` (plural)            |
| Endpoint conventions          | `/api/<resources>/public`, `/api/<resources>/mine`, `/api/<resources>` |
| Full DTO                      | `<Entity>Dto`                               |
| Public DTO                    | `<Entity>PublicDto`                         |
| Create / Update DTO           | `Create<Entity>RequestDto`, `Update<Entity>RequestDto` |
| List item DTO                 | `<Entity>ListItemDto` (narrower than full)  |
| Filter / query DTO            | `<Entity>FilterRequestDto`                  |
| Enum file                     | `<Concept>.cs` under `Shared/Enums/<Module>/` |
| Error code                    | `UPPER_SNAKE_CASE` in `ErrorCodes`          |
| Migration                     | `YYYYMMDDhhmmss_<Description>.cs`           |
| EF configuration              | `<Entity>Configuration : IEntityTypeConfiguration<Entity>` |
| DI extension                  | `Add<Concern>Services` (`AddCoreServices`, `AddDatabaseServices`, ...) |

---

## 15. What to copy into a new API (starter template)

### 15.1 Recommended project layout

Start with the same five projects (.API / .Application / .Domain /
.Infrastructure / .Shared). They are cheap to create and the
separation pays for itself by week two.

### 15.2 Recommended baseline classes & interfaces

**Shared**
- `Result`, `Result<T>`
- `ErrorCodes` static class (start with the 20 generic codes; add
  per-domain codes as you need them)
- `PagedListQueryBase`
- (optional) a `BaseEntity` POCO with `Id`, `CreatedAtUtc`,
  `UpdatedAtUtc?` — this is one improvement I'd carry into the new
  project (see §17)

**Application**
- `Common/Paging/PagedResult<T>`
- `Common/Interfaces/Shared/ICurrentUserService`
- `Persistence/Identity/IJwtTokenGenerator`
- `Auth/IAuthService` + DTOs (Login, Register, Refresh, ChangePassword,
  Forgot/Reset)

**Infrastructure**
- `Data/AppDbContext`
- `Identity/JwtTokenGenerator`
- `Configuration/JwtSettings`

**API**
- `Controllers/BaseController` with `ToActionResult` and the
  `MapFailure` switch
- `Extensions/ServiceExtensions` skeleton (`AddCoreServices`,
  `AddDatabaseServices`, `AddIdentityServices`,
  `AddInfrastructureServices`, `AddAuthServices`, `AddApiServices`,
  `AddCustomCors`, `ConfigureMiddleware`, `SeedApplicationAsync`)
- `Middleware/ExceptionHandlingMiddleware`
- `Services/CurrentUserService` (JWT claim reader)

### 15.3 Recommended DI extension shape

Keep `Program.cs` declarative — every call is a single `AddXxx`
extension method that owns its concern. Group related options
(`builder.Services.Configure<JwtSettings>(...)`) inside its method, not
in `Program.cs`. Validate critical config at startup (throw on
missing JWT key / connection string) — fail loud at boot.

### 15.4 Recommended EF configuration pattern

- One configuration class per entity.
- Auto-apply via `ApplyConfigurationsFromAssembly`.
- Always set `HasPrecision` on decimals.
- Always set `HasConversion<int>()` on enums.
- Define indexes for every column used as a query filter or sort key.
- Use filtered unique indexes for "one active X per owner" rules.

### 15.5 Recommended BaseController pattern

Keep it tiny. Two methods: `ToActionResult(Result)` and
`ToActionResult<T>(Result<T>)`. One private `MapFailure` switch.
Don't add per-controller "helper" methods — push them into services.

---

## 16. What to simplify for smaller projects

### 16.1 Always keep

- The five-project layout (Shared / Domain / Application /
  Infrastructure / API). It's cheap and pays back fast.
- `Result<T>` + `ErrorCodes` + `BaseController.MapFailure`. The
  uniform envelope is the single best thing about this codebase.
- The three-tier visibility convention (public / mine / admin) with
  matching DTOs.
- `ICurrentUserService` to keep Application out of `HttpContext`.
- Explicit ownership checks in services.

### 16.2 Worth simplifying for small/medium APIs

- **Drop the repository pattern; use `AppDbContext` directly in
  services** for trivial CRUD. The repos here are thin pass-throughs
  for many entities. Keep repos for the few aggregates with complex
  reads (search, denormalised joins). This is the single biggest
  cleanup win.
- **Drop "atomic replace via repository helper"** in favour of putting
  the transaction directly in the service (with `IAppDbContext`
  injected). The variant-replace pattern works, but the indirection
  doesn't earn its keep at small scale.
- **Skip AutoMapper.** Hand-mapping in services is fine and explicit.
  ZansiHustle already does this — keep doing it.
- **Skip MediatR** unless you have a real cross-cutting reason
  (decorators, pipeline behaviours). At three controllers and ten
  services it adds friction.
- **One configuration folder, not two.** ZansiHustle has
  `Infrastructure/Data/Configurations` and
  `Infrastructure/Persistence/<Module>/*Configuration.cs`. Pick one.
- **One repository folder, not two.** Same issue. Either
  `Infrastructure/Persistence/<Module>/` or
  `Infrastructure/Repositories/<Module>/`.

### 16.3 Recommended minimum for a serious new API

- 5 projects.
- `Result<T>` + `ErrorCodes` + `BaseController`.
- Services as the only place business logic lives.
- `AppDbContext` injected into services directly (skip repositories
  until they prove themselves needed for a given aggregate).
- One `Add<Concern>Services` extension per concern.
- Migrations applied at startup, in dev only — production migrations
  are gated by deploy pipeline.

---

## 17. Improvements recommended for next project

### 17.1 What worked well

- **`Result<T>` envelope.** Single biggest unifying force. Every layer
  speaks the same language. Errors are debuggable from the JSON alone.
- **Three-tier visibility (public/mine/admin).** Makes secure-by-default
  obvious in both service and controller. Public DTOs prevent
  accidental data leakage.
- **Replace-not-diff for child collections.** Eliminated a class of
  `DbUpdateConcurrencyException` false 409s on listing variants.
- **DI extensions per concern.** `Program.cs` reads like a table of
  contents.
- **Boot-time config reporters** (Email/Ozow/Yoco). Logs the exact
  env-var names that are missing — no secret values — and warns about
  localhost webhooks. Has saved hours.
- **Filtered unique indexes** for "one active X per owner". Lets you
  re-open a suspended row without a unique-constraint collision.

### 17.2 What caused friction

- **Two folders for configurations and repositories**
  (`Data/Configurations` vs `Persistence`). Pick one location per
  concept on day one.
- **Generic-feeling React Query keys.** `['shops','mine']` was used
  for two different shapes (merchant accounts vs shop profile),
  causing a real bug. Namespace by *data shape*, not just by URL.
  Use `['merchants','mine']` and `['shopProfile','mine']` from day one.
- **Ambiguous frontend hook names** (`useSellerShops` that returned
  merchant accounts, not shops). Name hooks after the entity they
  return, not the screen they live on.
- **No `BaseEntity`.** `Id`, `CreatedAtUtc`, `UpdatedAtUtc?` are
  copy-pasted on every entity. A 3-property base class is worth the
  tiny coupling.
- **No `IUnitOfWork`.** Every repo owns its `SaveChangesAsync()`.
  Cross-repo writes need extra ceremony. Either inject `AppDbContext`
  directly into services (recommended) or add a minimal
  `IUnitOfWork { Task<int> SaveChangesAsync(); }` so cross-repo
  operations commit in one place.
- **No public-vs-owner DTO projection from day one.** Public DTOs got
  retrofitted module-by-module. Painful. Do this at module-creation
  time, not later.
- **Sensitive fields in plain text** (`Merchant.BankAccountNumber`,
  `Merchant.IdNumber`). TODO comments mark them but they're still
  unencrypted. Add a column-level encryption strategy upfront.
- **No concurrency strategy** (`[Timestamp]` / `RowVersion`). For
  edit-heavy entities (listing, shop profile) this would prevent
  silent overwrite when two tabs save the same record. Worth adding
  on day one — it's nearly free if planned in.
- **No background job runner.** `IHostedService` works for boot-time
  validators, but webhooks-with-retry, scheduled aggregate refresh,
  and email retries need something like Hangfire or Quartz.NET. Pick
  one upfront.
- **Tests came late.** Add at least integration tests for ownership
  checks and the result→HTTP mapping at module bootstrap time.
- **In-memory OTP store.** Fine for dev; replace with Redis or a SQL
  table from day one if you have any expectation of scale or
  multi-instance deploys.

### 17.3 Concrete improvements to bake into the next API

1. `BaseEntity { Guid Id; DateTime CreatedAtUtc; DateTime? UpdatedAtUtc; }`
2. `IAppDbContext` interface so services can inject the DbContext
   without referencing Infrastructure; skip per-entity repositories
   except for genuinely complex aggregates.
3. `IUnitOfWork.SaveChangesAsync()` if you keep repos.
4. One folder for EF configurations; one for repositories.
5. Public/owner/admin DTO trio defined per module from day one.
6. `RowVersion` column on every editable aggregate.
7. Column-level encryption on `BankAccountNumber`, national IDs.
8. Redis-backed OTP store (or SQL table with TTL cleanup).
9. Hangfire (or equivalent) registered upfront for retries.
10. Integration test project (`<Solution>.IntegrationTests`)
    wired to `WebApplicationFactory<Program>`.
11. Consistent React Query key prefix policy on the frontend:
    `['<entity>','<scope>', ...params]` with `<scope>` one of
    `'mine' | 'public' | 'admin' | 'detail' | 'list'`.

---

## 18. Final summary

### 18.1 Recommended architecture for the next API

Same five-project layout. Same `Result<T>` envelope. Same `BaseController`
+ `MapFailure` switch. Same three-tier visibility convention. Same
per-module folder shape in Application. Same DI-extension-per-concern
pattern in `Program.cs`. Same conditional cloud-vs-local storage
fallback. Same boot-time config reporters.

**Simplify**: drop most repositories (inject `AppDbContext` directly
into services); collapse the two configuration/persistence folder
locations into one; add `BaseEntity` and `RowVersion` upfront.

**Add**: integration tests, Hangfire (or equivalent), Redis OTP store,
column-level encryption for sensitive PII, RowVersion concurrency
tokens.

### 18.2 Folder structure tree

```
MyNewApi/
├── MyNewApi.sln
├── MyNewApi.Shared/
│   ├── Results/         (Result, Result<T>)
│   ├── Errors/          (ErrorCodes)
│   ├── Enums/           (<Module>/<Enum>.cs)
│   └── Queries/         (PagedListQueryBase)
├── MyNewApi.Domain/
│   ├── _Common/         (BaseEntity)
│   ├── Identity/        (User : IdentityUser<Guid>, RefreshToken)
│   └── <Module>/        (entities)
├── MyNewApi.Application/
│   ├── Common/          (Paging, Interfaces/Shared)
│   ├── Persistence/     (interfaces — IAppDbContext, IUnitOfWork, optional repos)
│   ├── Auth/            (IAuthService + DTOs)
│   └── <Module>/        (services + DTOs)
├── MyNewApi.Infrastructure/
│   ├── Data/            (AppDbContext + Configurations/<Module>/*Configuration.cs)
│   ├── Identity/        (JwtTokenGenerator)
│   ├── Configuration/   (JwtSettings, StorageSettings, ...)
│   ├── Communications/  (Email/SMS providers)
│   ├── Payments/        (gateway clients)
│   ├── Repositories/    (optional, only where needed)
│   └── Migrations/
└── MyNewApi.API/
    ├── Program.cs
    ├── appsettings.json
    ├── Controllers/     (BaseController + <Resources>Controller.cs)
    ├── Extensions/      (ServiceExtensions.cs)
    ├── Middleware/      (ExceptionHandlingMiddleware)
    ├── Services/        (CurrentUserService, UserLookupService, config reporters)
    └── Storage/         (R2 / local / cloud storage adapters)
```

### 18.3 Step-by-step bootstrapping guide

1. `dotnet new sln -n MyNewApi`
2. `dotnet new classlib -n MyNewApi.Shared -f net8.0`
3. `dotnet new classlib -n MyNewApi.Domain -f net8.0` + add reference to Shared
4. `dotnet new classlib -n MyNewApi.Application -f net8.0` + reference to Domain
5. `dotnet new classlib -n MyNewApi.Infrastructure -f net8.0` + reference to Application, Domain, Shared
6. `dotnet new webapi -n MyNewApi.API -f net8.0` + reference to Application + Infrastructure
7. `dotnet sln add **/*.csproj`
8. Add NuGet packages:
   - **API**: `Microsoft.AspNetCore.Authentication.JwtBearer`,
     `Microsoft.EntityFrameworkCore`,
     `Microsoft.EntityFrameworkCore.SqlServer`,
     `Microsoft.EntityFrameworkCore.Design`,
     `Microsoft.EntityFrameworkCore.Tools`, `Swashbuckle.AspNetCore`,
     (optional) `AWSSDK.S3`
   - **Infrastructure**: same EF + `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
   - **Domain**: `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
9. **Shared**: copy `Result`, `Result<T>`, `ErrorCodes`,
   `PagedListQueryBase` from this guide.
10. **Domain**: add `_Common/BaseEntity.cs` and
    `Identity/User : IdentityUser<Guid>`.
11. **Application**: add `Common/Paging/PagedResult<T>`,
    `Common/Interfaces/Shared/ICurrentUserService`,
    `Persistence/IAppDbContext`, `Auth/IAuthService` + DTOs.
12. **Infrastructure**: add `Data/AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>`,
    `Configuration/JwtSettings`, `Identity/JwtTokenGenerator`.
13. **API**: add `Controllers/BaseController` with `ToActionResult` /
    `MapFailure`, `Middleware/ExceptionHandlingMiddleware`,
    `Services/CurrentUserService`, `Extensions/ServiceExtensions` with
    the standard `AddXxx` suite.
14. Rewrite `Program.cs` to call the `AddXxx` extensions in order
    (Core → Database → Identity → Infra → Auth → Api → Communications
    → Payments → Cors → Build → Migrate → Middleware → Run).
15. `dotnet ef migrations add Initial -p MyNewApi.Infrastructure -s MyNewApi.API`
16. `dotnet run --project MyNewApi.API` — Swagger should come up.
17. Begin module #1 by following the [§13 checklist](#13-new-module-checklist).

---

## Appendix A — Why each piece exists (one-liners)

- **Five projects**: enforce dependency direction at the compiler level.
- **`Result<T>`**: uniform success/failure shape that the controller
  layer can mechanically translate to HTTP.
- **`ErrorCodes` + `MapFailure`**: codes are language-stable
  (`FORBIDDEN`) while HTTP status is a presentation detail.
- **DTO suffixes (`Dto`/`PublicDto`/`RequestDto`)**: visibility and
  intent at-a-glance.
- **Three-tier visibility**: secure-by-default; public surface can't
  leak sensitive fields.
- **`ICurrentUserService`**: keeps Application out of `HttpContext`.
- **Replace-not-diff for child collections**: avoids fragile
  EF-tracked diffs; simpler mental model.
- **Boot-time config reporters**: fail loud and obvious for missing
  third-party credentials.
- **Conditional R2 vs local storage**: dev works without cloud
  credentials; production demands them.
- **Filtered unique indexes**: enforce "one active per owner" without
  blocking re-opens.
- **Explicit transactions on multi-statement writes**: predictable
  rollback semantics; no implicit save surprises.

---

## Appendix B — Hand-off prompt for a new project

When starting a new API in a fresh Claude session, paste:

> "Use the architecture described in `docs/ZansiHustleApiArchitecture.md`.
> Apply the §15 starter template, the §16 simplifications, and the
> §17 improvements. Bootstrap per §18.3. Stop after each module so I
> can review the controller's three-tier endpoints and the
> `Result<T>` shape before generating the next module."
