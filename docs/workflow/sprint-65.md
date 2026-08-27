# 📋 Sprint 65 — Finance ↔ Projects Integration (VALUE SPRINT) (2026-08-27)

> **Sprint Hand-off Document** — Contract for Workers (Jimis) and Mavis Local
>
> **Status:** 🟡 In Progress (Autonomous mode — Anas sleeping)
> **Branch:** `feature/sprint-65-finance-projects` (off `develop` after Sprint 62+63+64 merge)
> **Mode:** LOCAL-ONLY (M1-Local — no push until Anas's "ادفع")
> **Duration:** 4-6 days (planned 15-22 Sep, executing in autonomous run)

---

## 🎯 Sprint Goal

Wire up the **Finance ↔ Projects integration** so that:
1. Posting rules automatically create journal entries when a Progress Billing is INVOICED
2. Posting rules create journal entries when a Sub-Payment is recorded
3. Project P&L (Sprint 57 / DEC-161) reads subcontractor costs from `sub_payments` (Sprint 64)
4. Dashboard shows cross-module KPIs (e.g. "Outstanding Receivables" + "Outstanding Payables" side-by-side)
5. Bank reconciliation: when a Receipt is recorded, automatically suggest matching Sub-Payment

**Why now:** This is the **VALUE SPRINT** — after 6 sprints of building modules, this is where the data flows. Construction companies need to see: when we bill a client (AR) AND when we pay a subcontractor (AP), the books reflect both.

---

## 📦 DECs in Scope (7 DECs)

| DEC | الوصف | Wave | الحجم |
|-----|-------|------|-------|
| **DEC-231** | ProgressBilling → AR Invoice (auto on INVOICED) | 1 | كبير |
| **DEC-232** | Sub-Payment → AP Vendor Bill (auto on payment) | 1 | كبير |
| **DEC-233** | Project P&L includes Subcontractor costs (from sub_payments) | 2 | متوسط |
| **DEC-234** | Dashboard cross-module KPIs endpoint | 2 | متوسط |
| **DEC-235** | Receipt ↔ Sub-Payment matching suggestion | 3 | كبير |
| **DEC-236** | FE Dashboard widgets (Outstanding AR + AP) | 2 | متوسط |
| **DEC-237** | FE Bank Reconciliation page | 3 | متوسط |

---

## 🌊 Wave Structure (3 waves)

### Wave 1 — Finance Trigger Wiring (Auto-Journal)
**Target:** 60-90 min. **Worker:** 1.

#### Worker 1A — ProgressBilling → AR Invoice (DEC-231) + SubPayment → Vendor Bill (DEC-232)

**Scope (files):**

**New files:**
1. `src/backend/Modules/Projects/Application/Services/FinanceIntegrationService.cs` — orchestrator that calls AR + AP services
2. `src/backend/Modules/Projects/Application/Events/BillingApprovedEvent.cs`
3. `src/backend/Modules/Projects/Application/Events/SubPaymentCreatedEvent.cs`
4. `src/backend/Modules/Projects/Application/Handlers/BillingApprovedHandler.cs` — creates AR Invoice + Journal Entry
5. `src/backend/Projects/Application/Handlers/SubPaymentCreatedHandler.cs` — creates AP Vendor Bill + Journal Entry
6. `src/backend/Tests/ERPSystem.Tests/Projects/Sprint65FinanceIntegrationTests.cs` (8+ tests)

**Modified files:**
- `src/backend/Modules/Projects/Application/Services/BillingService.cs` — fire `BillingApprovedEvent` on ApproveAsync
- `src/backend/Modules/Projects/Application/Services/SubPaymentService.cs` — fire `SubPaymentCreatedEvent` on CreateAsync
- `src/backend/Host/Program.cs` — DI for the 2 handlers + FinanceIntegrationService
- `src/backend/Modules/Projects/AGENTS.md` — Wave 1A section
- `CHANGELOG.md` — Wave 1A entry

**Event pattern (in-process):**
```csharp
public sealed class BillingApprovedEvent
{
    public Guid BillingId { get; }
    public Guid ProjectId { get; }
    public Guid CompanyId { get; }
    public decimal NetAmount { get; }
    public Guid UserId { get; }
    public DateTime OccurredAt { get; }
}
```

**Handler (DEC-231 — ProgressBilling → AR Invoice):**
```csharp
public class BillingApprovedHandler
{
    public async Task Handle(BillingApprovedEvent evt)
    {
        // 1. Load ProgressBilling + Project + Contract
        // 2. Find or create Customer (from Project.CustomerId)
        // 3. Create AR SalesInvoice:
        //    - invoiceNumber = "AR-{billingNumber}"
        //    - amount = billing.NetAmount
        //    - dueDate = today + 30 days
        // 4. Create JournalEntry (via PostingRules):
        //    - DR Accounts Receivable
        //    - CR Revenue
        // 5. Update ProgressBilling.invoiceId + journalEntryId
    }
}
```

**Handler (DEC-232 — SubPayment → Vendor Bill):**
```csharp
public class SubPaymentCreatedHandler
{
    public async Task Handle(SubPaymentCreatedEvent evt)
    {
        // 1. Load SubPayment + SubContract + Subcontractor
        // 2. Find or create Vendor (from Subcontractor)
        // 3. Create AP VendorBill:
        //    - billNumber = "AP-{paymentNumber}"
        //    - amount = payment.Amount
        //    - dueDate = payment.PaymentDate (already paid)
        // 4. Create JournalEntry (via PostingRules):
        //    - DR Subcontractor Cost
        //    - CR Cash / Bank
        // 5. Update SubPayment.vendorBillId + journalEntryId
    }
}
```

**L19 / DEC-095:** CompanyId from ICompanyContext. UserId from event payload (which came from JWT).

**Tests (8+):**
- `BillingApprovedEvent_FiresOnApprove_CreatesArInvoice`
- `BillingApprovedHandler_PostsJournalEntry_DrArCrRevenue`
- `BillingApprovedHandler_DuplicateApprove_DoesNotCreateDuplicateInvoice`
- `SubPaymentCreatedEvent_FiresOnPayment_CreatesVendorBill`
- `SubPaymentCreatedHandler_PostsJournalEntry_DrCostCrCash`
- `SubPaymentCreatedHandler_RetentionRelease_CreatesSeparateBill`
- `FinanceIntegrationService_HandlesMultipleEventsInSequence`
- `FinanceIntegrationService_RollsBack_OnFailure`

### Wave 2 — Project P&L + Dashboard
**Target:** 60-90 min. **Worker:** 1.

#### Worker 2A — Project P&L + Subcontractor costs (DEC-233) + Dashboard KPIs (DEC-234+236)

**Scope (files):**

**New files:**
1. `src/backend/Modules/Projects/Application/Services/ProjectCostService.cs` — aggregates subcontractor costs
2. `src/backend/Host/Controllers/DashboardCrossModuleController.cs` — 2 endpoints
3. `src/backend/Tests/ERPSystem.Tests/Projects/Sprint65ProjectCostServiceTests.cs` (5+ tests)
4. `src/backend/Tests/ERPSystem.Tests/Projects/Sprint65DashboardCrossModuleControllerTests.cs` (4+ tests)
5. `src/frontend/app/(authenticated)/dashboard/cross-module/page.tsx` — new page
6. `src/frontend/components/dashboard/OutstandingArCard.tsx`
7. `src/frontend/components/dashboard/OutstandingApCard.tsx`
8. `src/frontend/components/dashboard/ProjectProfitabilityCard.tsx`
9. `src/frontend/tests/components/dashboard/OutstandingArCard.test.tsx` (3+ tests)

**Modified files:**
- `src/backend/Modules/Projects/Application/Services/ProjectPnLService.cs` — add `SubcontractorCost` from `sub_payments`
- `src/backend/Modules/Projects/Application/ProjectsDtos.cs` — add `SubcontractorCost` field to `ProjectPnLResponse`
- `src/frontend/lib/api-types.ts` — add `DashboardCrossModuleResponse` type
- `src/frontend/components/layout/AppShell.tsx` — add Dashboard sub-route
- `src/frontend/lib/api/dashboard.ts` — new file
- `src/backend/Modules/Projects/AGENTS.md` — Wave 2A section
- `CHANGELOG.md` — Wave 2A entry

**ProjectCostService algorithm:**
```
For each project:
  subcontractorCost = SUM(sub_payments.amount) for this project (status != 4)
  projectPnL.subcontractorCost = subcontractorCost
  projectPnL.totalCosts = projectPnL.totalCosts + subcontractorCost
  projectPnL.grossProfit = projectPnL.totalRevenue - projectPnL.totalCosts
```

**Dashboard cross-module endpoint:**
```http
GET /api/dashboard/cross-module
→ 200 {
  outstandingAR: decimal,    // total unpaid sales_invoices
  outstandingAP: decimal,    // total unpaid sub_payments
  netPosition: decimal,      // outstandingAR - outstandingAP
  projectCount: int,
  totalContractValue: decimal,
  totalRevenue: decimal,      // from sales_invoices
  totalSubcontractorCost: decimal,  // from sub_payments
  unprofitableProjects: int   // projects where costs > revenue
}
```

**2 BE endpoints:**
- `GET /api/dashboard/cross-module` — cross-module KPIs
- `GET /api/dashboard/project-profitability` — list of all projects ranked by profitability

**L19 / DEC-095:** userId from JWT. CompanyId from ICompanyContext.

**Tests (12+):**
- 5 service tests for ProjectCostService
- 4 controller tests for DashboardCrossModuleController
- 3 component tests for OutstandingArCard

### Wave 3 — Bank Reconciliation
**Target:** 60-90 min. **Worker:** 1.

#### Worker 3A — Receipt ↔ Sub-Payment Matching (DEC-235+237)

**Scope (files):**

**New files:**
1. `src/backend/Modules/Finance/Application/Services/BankReconciliationService.cs` — auto-match Receipts to Sub-Payments
2. `src/backend/Host/Controllers/BankReconciliationsController.cs` — 3 endpoints
3. `src/backend/Tests/ERPSystem.Tests/Finance/Sprint65BankReconciliationServiceTests.cs` (6+ tests)
4. `src/backend/Tests/ERPSystem.Tests/Finance/Sprint65BankReconciliationsControllerTests.cs` (3+ tests)
5. `src/frontend/app/(authenticated)/finance/reconciliation/page.tsx`
6. `src/frontend/components/finance/ReceiptMatchCard.tsx`
7. `src/frontend/components/finance/ReconciliationQueue.tsx`
8. `src/frontend/lib/api/reconciliation.ts`
9. `src/frontend/tests/components/finance/ReceiptMatchCard.test.tsx` (3+ tests)

**Modified files:**
- `src/backend/Modules/Finance/Application/Services/ReceiptService.cs` — add `SuggestMatchesAsync` method
- `src/backend/Host/Program.cs` — DI for BankReconciliationService
- `src/frontend/lib/api-types.ts` — add `ReceiptMatch` type
- `src/frontend/components/layout/AppShell.tsx` — add Reconciliation sub-route
- `CHANGELOG.md` — Wave 3A entry
- `src/frontend/AGENTS.md` — Wave 3A section

**Matching algorithm:**
```
For each Receipt (incoming bank credit):
  1. Find Sub-Payments with:
     - amount within ±5% of receipt.amount
     - payment_date within ±30 days of receipt.date
     - status = "expected" (vendor bill exists, payment not yet received)
  2. Score by:
     - amount exact match: +50
     - amount ±1%: +30
     - amount ±5%: +10
     - date exact: +20
     - date ±7 days: +10
  3. Return top 5 matches sorted by score
```

**3 endpoints:**
- `GET /api/receipts/{id}/suggest-matches` — return top 5 matches
- `POST /api/receipts/{id}/confirm-match/{subPaymentId}` — link receipt to sub-payment
- `GET /api/reconciliation/queue` — all unmatched receipts

**L19 / DEC-095:** userId from JWT for `confirm-match`.

**Tests (12+):**
- 6 service tests for BankReconciliationService
- 3 controller tests for BankReconciliationsController
- 3 component tests for ReceiptMatchCard

---

## 🛡️ Quality Gates

```
[ ] dotnet build → 0 errors
[ ] dotnet test → 0 regressions
[ ] npm run typecheck → 0 errors
[ ] npm run build → production build succeeds
[ ] Sprint65 tests pass (32+ new)
[ ] No tenant_id, no secrets, no EF Core
[ ] L19 / DEC-095 throughout
[ ] CHANGELOG.md entry per wave
[ ] AGENTS.md updated per wave
[ ] Conventional Commits
```

---

## 🎯 Success Criteria

- 7 DECs delivered (231-237)
- 32+ new tests pass (BE + FE)
- 7 BE endpoints (2 cross-module dashboard + 3 reconciliation + 2 BE→finance)
- 5+ FE components + 2 FE pages
- Trust Mode E2E: create project → create contract → post billing → INVOICE → see AR invoice auto-created → record sub-payment → see vendor bill auto-created → see P&L update → see dashboard show "Outstanding AR" + "Outstanding AP"

---

## ⚠️ Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Cross-module transactions can fail mid-way (e.g. AR created but JE failed) | Wrap each handler in try/catch; log failure; roll back DB transaction |
| Posting rules for AR/AP may not exist | DEC-231/232 may need to seed posting rules (DEC-237a) |
| Sprint 63 RBAC + Sprint 64 Subcontractor both have new permission codes | DEC-238 (out of scope) defers RBAC cleanup to Sprint 66 |
| Sprint 64 L198 marker + Sprint 63 marker conflict | When Sprint 63 merges, both attributes collapse to one (real one) |
| Large scope (3 waves × ~5 files each) | Each wave is well-scoped; defer any "nice to have" to Sprint 66 |

---

## 📋 Out of Scope (deferred to Sprint 66+)

- Multi-currency for AR/AP matching
- Auto-bank-feed integration (need real bank API access)
- Subcontractor self-service portal
- Real-time dashboard updates (SSE/WebSocket)
- Mobile app for site engineers

---

**Written by:** محمد (Mavis — M1-Exec, autonomous mode) | 2026-08-27
**Awaiting:** Worker 1A spawn → 3 waves → Sprint 65 closure
