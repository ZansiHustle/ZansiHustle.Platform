/* ============================================================================
   UAT-ONLY cleanup: remove old fake / incomplete PendingDispatch shipments.

   ⚠️  RUN IN UAT ONLY. This is a manual, preview-first script — NOT an EF
       migration. It must never run automatically against production.

   It deletes ONLY internal PendingDispatch rows that were never booked with a
   courier (no provider shipment id, no tracking number, no short ref, no label,
   not delivered). Anything with a real courier booking, tracking, label, or a
   non-PendingDispatch status is PRESERVED — including the known-good shipment:

       ShipmentId            = edbcc5ae-2699-4481-b5e1-f8f944236ed2
       ProviderShipmentId    = 117442505
       ShortTrackingReference= 7D67MD

   HOW TO USE
   ----------
   1. Run STEP 1 (preview) and eyeball the rows. Confirm the known-good shipment
      is NOT listed and the count looks right.
   2. Only then run STEP 2 with @Commit = 1 to actually delete. Leave @Commit = 0
      to dry-run inside a rolled-back transaction (prints counts, changes nothing).
   ============================================================================ */

-- Status ints (ZansiDispatchShipmentStatus): PendingDispatch = 1.
-- The known-good shipment id is excluded explicitly as a belt-and-braces guard.
-- AGE CUTOFF: a FRESH PendingDispatch shipment from a real seller acceptance has
-- the same NULL signature as an old fake, so we only delete rows OLDER than
-- @OlderThanUtc (default 24h ago). Never deletes a just-accepted shipment.
DECLARE @OlderThanUtc datetime2 = DATEADD(HOUR, -24, SYSUTCDATETIME());

/* ---------------------------------------------------------------------------
   STEP 1 — PREVIEW: exactly what STEP 2 would delete. Read-only.
   --------------------------------------------------------------------------- */
SELECT
    s.Id,
    s.OrderId,
    s.Status,
    s.ProviderShipmentId,
    s.TrackingNumber,
    s.ShortTrackingReference,
    s.LabelUrl,
    s.DeliveredAt,
    s.CreatedAt
FROM dbo.ZansiDispatchShipments AS s
WHERE s.Status = 1                                   -- PendingDispatch only
  AND s.ProviderShipmentId      IS NULL              -- never booked with a courier
  AND s.TrackingNumber          IS NULL
  AND s.ShortTrackingReference  IS NULL
  AND s.LabelUrl                IS NULL
  AND s.DeliveredAt             IS NULL
  AND s.CreatedAt               < @OlderThanUtc       -- protect fresh accepted-order shipments
  AND s.Id <> 'EDBCC5AE-2699-4481-B5E1-F8F944236ED2' -- preserve the known-good shipment
ORDER BY s.CreatedAt;


/* ---------------------------------------------------------------------------
   STEP 2 — DELETE (transactional). Set @Commit = 1 to apply; 0 = dry run.
   Also removes the matched shipments' orphan events, ACTIONS, and ledger
   entries (those tables hold loose, un-FK'd ShipmentId references).
   --------------------------------------------------------------------------- */
DECLARE @Commit bit = 0;   -- <<< set to 1 to actually delete

BEGIN TRAN;

    DECLARE @Doomed TABLE (Id uniqueidentifier PRIMARY KEY);

    INSERT INTO @Doomed (Id)
    SELECT s.Id
    FROM dbo.ZansiDispatchShipments AS s
    WHERE s.Status = 1
      AND s.ProviderShipmentId      IS NULL
      AND s.TrackingNumber          IS NULL
      AND s.ShortTrackingReference  IS NULL
      AND s.LabelUrl                IS NULL
      AND s.DeliveredAt             IS NULL
      AND s.CreatedAt               < @OlderThanUtc       -- protect fresh accepted-order shipments
      AND s.Id <> 'EDBCC5AE-2699-4481-B5E1-F8F944236ED2';

    DELETE e
    FROM dbo.ZansiDispatchShipmentEvents AS e
    INNER JOIN @Doomed d ON d.Id = e.ShipmentId;

    DELETE a
    FROM dbo.ZansiDispatchShipmentActions AS a
    INNER JOIN @Doomed d ON d.Id = a.ShipmentId;

    DELETE l
    FROM dbo.ZansiDispatchLedgerEntries AS l
    INNER JOIN @Doomed d ON d.Id = l.ShipmentId;

    DELETE s
    FROM dbo.ZansiDispatchShipments AS s
    INNER JOIN @Doomed d ON d.Id = s.Id;

    PRINT CONCAT('Matched shipments: ', (SELECT COUNT(*) FROM @Doomed));

IF @Commit = 1
BEGIN
    COMMIT TRAN;
    PRINT 'Committed: fake PendingDispatch rows removed.';
END
ELSE
BEGIN
    ROLLBACK TRAN;
    PRINT 'Dry run only — nothing was deleted (set @Commit = 1 to apply).';
END
