/* ============================================================================
   UAT-ONLY dispatch cleanup — removes fake/stale rows that still drive the
   ZansiDispatch command-centre overview (Quoted Fees, Pending, Pending Recon,
   counts) plus their dependent events / actions / ledger rows, and any orphans.

   ⚠️  RUN IN UAT ONLY. Manual, preview-first. NOT an EF migration. Never run
       automatically and never in production.

   The command-centre overview is computed PURELY from real data:
     • financials + status counts  ← dbo.ZansiDispatchShipments (last 30 days)
     • provider failure/fallback   ← dbo.ZansiDispatchProviderRequestLogs
   So removing the fake PendingDispatch shipment rows makes the overview read
   zero. We ONLY delete shipments that were never booked with a courier:
     Status = PendingDispatch(1) AND ProviderShipmentId/TrackingNumber/
     ShortTrackingReference/LabelUrl/DeliveredAt all NULL,
   and we ALWAYS preserve the known-good shipment:
     edbcc5ae-2699-4481-b5e1-f8f944236ed2 (provider 117442505, ref 7D67MD).

   HOW TO USE
   ----------
   1. Run STEP 0 (preview) — eyeball what will be deleted + current totals.
   2. Run STEP 1 with @Commit = 0 (default) → dry run, rolls back, prints counts.
   3. Only when satisfied, set @Commit = 1 and re-run STEP 1 to apply.
   4. Run STEP 2 (sanity) — confirm everything reads zero (or only real rows).
   ============================================================================ */

------------------------------------------------------------------------------
-- STEP 0 — PREVIEW (read-only): the fake pending shipments + current totals.
------------------------------------------------------------------------------
SELECT 'fake_pending_shipments_to_delete' AS preview;
SELECT s.Id, s.OrderId, s.Status, s.QuotedDeliveryFee, s.ReconciliationStatus,
       s.ProviderShipmentId, s.TrackingNumber, s.ShortTrackingReference, s.LabelUrl, s.DeliveredAt, s.CreatedAt
FROM dbo.ZansiDispatchShipments AS s
WHERE s.Status = 1
  AND s.ProviderShipmentId     IS NULL
  AND s.TrackingNumber         IS NULL
  AND s.ShortTrackingReference IS NULL
  AND s.LabelUrl               IS NULL
  AND s.DeliveredAt            IS NULL
  AND s.Id <> 'EDBCC5AE-2699-4481-B5E1-F8F944236ED2'
ORDER BY s.CreatedAt;

SELECT 'orphan_dependent_rows' AS preview,
  (SELECT COUNT(*) FROM dbo.ZansiDispatchShipmentEvents  e WHERE NOT EXISTS (SELECT 1 FROM dbo.ZansiDispatchShipments s WHERE s.Id = e.ShipmentId)) AS orphan_events,
  (SELECT COUNT(*) FROM dbo.ZansiDispatchShipmentActions a WHERE NOT EXISTS (SELECT 1 FROM dbo.ZansiDispatchShipments s WHERE s.Id = a.ShipmentId)) AS orphan_actions,
  (SELECT COUNT(*) FROM dbo.ZansiDispatchLedgerEntries   l WHERE NOT EXISTS (SELECT 1 FROM dbo.ZansiDispatchShipments s WHERE s.Id = l.ShipmentId)) AS orphan_ledger;


------------------------------------------------------------------------------
-- STEP 1 — DELETE (transactional). @Commit = 0 → dry run; 1 → apply.
------------------------------------------------------------------------------
DECLARE @Commit bit = 0;   -- <<< set to 1 to actually delete

BEGIN TRAN;

    DECLARE @Doomed TABLE (Id uniqueidentifier PRIMARY KEY);

    INSERT INTO @Doomed (Id)
    SELECT s.Id
    FROM dbo.ZansiDispatchShipments AS s
    WHERE s.Status = 1
      AND s.ProviderShipmentId     IS NULL
      AND s.TrackingNumber         IS NULL
      AND s.ShortTrackingReference IS NULL
      AND s.LabelUrl               IS NULL
      AND s.DeliveredAt            IS NULL
      AND s.Id <> 'EDBCC5AE-2699-4481-B5E1-F8F944236ED2';

    /* 1a. Dependents of the fake shipments (loose, un-FK'd ShipmentId refs). */
    DELETE e FROM dbo.ZansiDispatchShipmentEvents  e INNER JOIN @Doomed d ON d.Id = e.ShipmentId;
    DELETE a FROM dbo.ZansiDispatchShipmentActions a INNER JOIN @Doomed d ON d.Id = a.ShipmentId;
    DELETE l FROM dbo.ZansiDispatchLedgerEntries   l INNER JOIN @Doomed d ON d.Id = l.ShipmentId;

    /* 1b. The fake shipments themselves. */
    DELETE s FROM dbo.ZansiDispatchShipments s INNER JOIN @Doomed d ON d.Id = s.Id;

    /* 1c. TRUE ORPHANS — dependent rows whose shipment no longer exists (left
           over from any earlier partial cleanup / source). Safe: never touches
           a row that still belongs to a surviving shipment. */
    DELETE e FROM dbo.ZansiDispatchShipmentEvents  e WHERE NOT EXISTS (SELECT 1 FROM dbo.ZansiDispatchShipments s WHERE s.Id = e.ShipmentId);
    DELETE a FROM dbo.ZansiDispatchShipmentActions a WHERE NOT EXISTS (SELECT 1 FROM dbo.ZansiDispatchShipments s WHERE s.Id = a.ShipmentId);
    DELETE l FROM dbo.ZansiDispatchLedgerEntries   l WHERE NOT EXISTS (SELECT 1 FROM dbo.ZansiDispatchShipments s WHERE s.Id = l.ShipmentId);

    /* 1d. Returns / Refunds / Disputes — these modules have NO backend tables
           yet (mock-only in the dispatch app). Guarded so the script is a no-op
           if the tables don't exist; if they're added later, fake UAT rows for
           deleted shipments get cleaned here. */
    IF OBJECT_ID('dbo.ZansiDispatchReturns', 'U') IS NOT NULL
        EXEC('DELETE r FROM dbo.ZansiDispatchReturns r WHERE NOT EXISTS (SELECT 1 FROM dbo.ZansiDispatchShipments s WHERE s.Id = r.ShipmentId);');
    IF OBJECT_ID('dbo.ZansiDispatchRefunds', 'U') IS NOT NULL
        EXEC('DELETE r FROM dbo.ZansiDispatchRefunds r WHERE NOT EXISTS (SELECT 1 FROM dbo.ZansiDispatchShipments s WHERE s.Id = r.ShipmentId);');
    IF OBJECT_ID('dbo.ZansiDispatchDisputes', 'U') IS NOT NULL
        EXEC('DELETE r FROM dbo.ZansiDispatchDisputes r WHERE NOT EXISTS (SELECT 1 FROM dbo.ZansiDispatchShipments s WHERE s.Id = r.ShipmentId);');

    PRINT CONCAT('Fake pending shipments matched: ', (SELECT COUNT(*) FROM @Doomed));

IF @Commit = 1
BEGIN
    COMMIT TRAN;
    PRINT 'Committed: fake dispatch rows + orphans removed.';
END
ELSE
BEGIN
    ROLLBACK TRAN;
    PRINT 'Dry run only — nothing was deleted (set @Commit = 1 to apply).';
END


------------------------------------------------------------------------------
-- STEP 2 — SANITY CHECK (run after applying). Everything should read zero
-- when no real shipments remain (only the known-good shipment, if any).
------------------------------------------------------------------------------
SELECT 'shipments_by_status' AS metric, s.Status, COUNT(*) AS count
FROM dbo.ZansiDispatchShipments s GROUP BY s.Status ORDER BY s.Status;

SELECT 'totals' AS metric,
  (SELECT COUNT(*) FROM dbo.ZansiDispatchShipments)                                  AS shipments_total,
  (SELECT ISNULL(SUM(QuotedDeliveryFee), 0) FROM dbo.ZansiDispatchShipments)         AS total_quoted_fees,
  (SELECT ISNULL(SUM(ISNULL(ActualCourierCost,0)), 0) FROM dbo.ZansiDispatchShipments) AS total_actual_costs,
  (SELECT COUNT(*) FROM dbo.ZansiDispatchShipments WHERE ReconciliationStatus = 1)   AS pending_reconciliation,
  (SELECT COUNT(*) FROM dbo.ZansiDispatchShipments WHERE Status = 13)                AS needs_attention,
  (SELECT COUNT(*) FROM dbo.ZansiDispatchShipmentEvents)                             AS events_count,
  (SELECT COUNT(*) FROM dbo.ZansiDispatchShipmentActions)                            AS actions_count,
  (SELECT COUNT(*) FROM dbo.ZansiDispatchLedgerEntries)                              AS ledger_count,
  (SELECT ISNULL(SUM(Amount), 0) FROM dbo.ZansiDispatchLedgerEntries)                AS ledger_total;

-- After this, re-fetch GET /api/zansidispatch/command-centre/overview — all
-- financials/counts should be 0 (or reflect only the preserved real shipment).
