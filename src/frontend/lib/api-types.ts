// 📦 api-types.ts — Frontend contract types (Sprint 11 T1)
//
// This file is the **user-facing contract** for the FE. The BE implements
// against these shapes (per Constitution Article 3 + .mavis/AGENTS.md Rule 1).
//
// Why a dedicated file (was inlined in api.ts before):
//   - The in-file types in `api.ts` are still there for legacy callers and
//     for the implementation details (request bodies, response wrappers).
//   - This file is the **demo-grade** contract surfaced in the new demo pages
//     (`/holding`, `/accounts`, `/transactions`, `/reports`). It is the
//     "FE-wins" surface (per Sprint 11 hand-off — FE is the source of truth
//     for the DTOs).
//   - All types are flat, JSON-friendly, and ISO-8601 for dates. No nested
//     entity wrappers — the BE flattens before serialising.
//
// Rules followed:
//   - `company_id` only (Constitution Article 3, no `tenant_id`).
//   - Bilingual where relevant (type is EN, but labels are AR in pages).
//   - No secrets / credentials in any field.
//
// Sprint: 11 — Full Demo Coverage (T1, FE Jimi)
// Last updated: 2026-07-31

// Internal type re-use: pull the legacy `Company` shape from `api.ts` so
// the `SubsidiaryListDto` (below) and the re-export block at the bottom
// can both reference it. `api.ts` already has the full shape; this file
// only adds the NEW DTOs on top.
import type { Company } from './api';

// ============ Company tree (Holding dashboard) ============
//
// Flat node — the BE may return either a tree (children nested) or a flat
// list; the FE accepts both. The Holding dashboard uses this to render the
// company hierarchy. Mirrors C# DTO `CompanyTreeNodeDto` in
// `src/backend/Modules/Companies/Application/Services/CompanyService.cs`.
//
// Contract: GET /api/companies/tree

export interface CompanyTreeNode {
  id: string;
  code: string;
  name: string;
  /** null = top-level (holding) */
  parentCompanyId: string | null;
  /** Group companies roll up children but don't post transactions. */
  isGroup: boolean;
  isActive: boolean;
  /** Nested children (recursive). Optional — BE may return flat list. */
  children?: CompanyTreeNode[];
}

// ============ Holding dashboard (consolidated KPIs) ============
//
// Consolidated view across the entire Holding (all sub-companies). Returned
// by GET /api/holdings/dashboard (or /api/dashboard/holding — the BE chooses
// the canonical path; the FE wrapper handles either).
//
// The page renders: revenue, expenses, net profit, company count, employee
// count, treasury balance, and a feed of recent transactions.

export interface HoldingDashboard {
  /** Consolidated revenue (sum across all sub-companies, LYD). */
  totalRevenue: number;
  /** Consolidated expenses (sum across all sub-companies, LYD). */
  totalExpenses: number;
  /** Net profit (revenue - expenses). */
  netProfit: number;
  /** Number of sub-companies (excludes the Holding itself). */
  companyCount: number;
  /** Total active employees across the Holding. */
  employeeCount: number;
  /** Consolidated treasury balance (sum of cash + bank accounts). */
  treasuryBalance: number;
  /** Recent transactions feed (last 10 by default). */
  recentTransactions: TransactionDto[];
  /** ISO 8601 timestamp of the snapshot. */
  asOf?: string;
  /** Currency (default LYD). */
  currency?: string;
}

// ============ Account (Chart of Accounts) ============
//
// Flat account DTO — the new demo surface. The legacy `Account` type in
// `api.ts` is kept for `finance/accounts` (uses numeric enums for `type` and
// `normalBalance`). This new shape uses string unions for clarity.
//
// Contract: GET /api/accounts

export type AccountType = 'Asset' | 'Liability' | 'Equity' | 'Revenue' | 'Expense';
export type NormalBalance = 'Debit' | 'Credit';

export interface AccountDto {
  id: string;
  /** null = shared (holding-level) account. Otherwise scoped to a sub-company. */
  companyId: string | null;
  code: string;
  name: string;
  type: AccountType;
  /** null = root account. */
  parentAccountId: string | null;
  /** Postable accounts accept journal lines; non-postable are headers. */
  isPostable: boolean;
  isActive: boolean;
  normalBalance: NormalBalance;
  /** Optional description for display. */
  description?: string | null;
}

// ============ Transaction ============
//
// A single journal transaction line. The demo shows these on the
// `/transactions` page (last 50 by default).
//
// Contract: GET /api/transactions/recent?limit=50

export interface TransactionDto {
  id: string;
  companyId: string;
  /** The account being debited or credited. */
  accountId: string;
  /** Display-only convenience (joined on the BE). */
  accountCode?: string;
  accountName?: string;
  /** Debit amount (LYD). */
  debit: number;
  /** Credit amount (LYD). */
  credit: number;
  /** Free-form description. */
  description: string;
  /** ISO 8601 timestamp. */
  createdAt: string;
  /** Optional reference (e.g. invoice number, journal entry number). */
  reference?: string | null;
}

// ============ Report (saved / generated) ============
//
// Lightweight summary of a generated report. The BE keeps the heavy rows
// elsewhere; the demo page lists recent reports + lets the user open them.
//
// Contract: GET /api/reports

export interface ReportDto {
  id: string;
  companyId: string;
  /** Report type identifier, e.g. "trial-balance", "income-statement". */
  type: string;
  /** Display title. */
  title: string;
  /** ISO 8601 timestamp. */
  generatedAt: string;
  /** Parameters the report was generated with (e.g. date range). */
  parameters: Record<string, unknown>;
  /** Optional URL to download the report (PDF / CSV). */
  downloadUrl?: string | null;
}

// ============ Subsidiary list (children of a company) ============
//
// Returned by GET /api/companies/{id}/subsidiaries.

export interface SubsidiaryListDto {
  parentCompanyId: string;
  /** Re-uses the legacy `Company` shape from `api.ts` (re-exported below). */
  subsidiaries: Company[];
}

// ============ Activity feed item ============
//
// User-facing activity feed (login, logout, register, switch, etc.).
// Returned by GET /api/activity/recent?limit=20.
// Mirrors the BE's `ActivityFeedItem` (Modules/Activity).

export interface ActivityFeedItemDto {
  /** bigint id as string (Postgres bigint can overflow JS number). */
  id: string;
  userId: string | null;
  /** Action constant, e.g. "LOGIN_SUCCESS", "REGISTER". */
  action: string;
  /** Optional entity type the action refers to. */
  entityType: string | null;
  entityId: string | null;
  /** ISO 8601 timestamp. */
  timestamp: string;
  /** Raw metadata (JSON string from the BE; FE may parse lazily). */
  metadata: string | null;
  userName: string | null;
}

// ============ Notification ============
//
// Per-user notification (in-app). The polling hook
// `useNotifications()` already exists — this DTO is the canonical shape.
//
// Contract: GET /api/inventory/notifications/unread

export interface NotificationDto {
  id: string;
  userId: string;
  /** Optional company scope. */
  companyId?: string | null;
  /** Type identifier, e.g. "LowStock", "JournalPosted". */
  type: string;
  title: string;
  message: string;
  read: boolean;
  /** ISO 8601 timestamp. */
  createdAt: string;
  /** Optional link to navigate to. */
  linkUrl?: string | null;
}

// ============ Customer (AccountsReceivable) ============
//
// Flat customer DTO — used by the Customer list/new/view pages.
// Contract: GET /api/ar/customers
//
// Field set derived from `src/backend/Host/data-types/customers.json` + the
// demo seed in `DefaultHoldingBootstrapHostedService.TrySeedDemoDataAsync`.

export interface CustomerDto {
  id: string;
  companyId: string;
  code: string;
  name: string;
  nameEn?: string | null;
  taxId?: string | null;
  email?: string | null;
  phone?: string | null;
  address?: string | null;
  creditLimit?: number | null;
  paymentTermsDays: number;
  isActive: boolean;
  /** ISO 8601 timestamp. */
  createdAt: string;
  updatedAt: string;
}

export interface CreateCustomerRequest {
  code: string;
  name: string;
  nameEn?: string | null;
  taxId?: string | null;
  email?: string | null;
  phone?: string | null;
  address?: string | null;
  creditLimit?: number | null;
  paymentTermsDays: number;
  isActive?: boolean;
}

export interface CustomerStatement {
  customerId: string;
  customerName: string;
  openingBalance: number;
  totalInvoiced: number;
  totalPaid: number;
  closingBalance: number;
  currency: string;
  asOf: string;
  lines: CustomerStatementLine[];
}

export interface CustomerStatementLine {
  invoiceId: string;
  invoiceNumber: string;
  date: string;
  description: string;
  debit: number;
  credit: number;
  balance: number;
}

// ============ Vendor (Procurement) ============
//
// Contract: GET /api/procurement/vendors

export interface VendorDto {
  id: string;
  companyId: string;
  code: string;
  name: string;
  email?: string | null;
  phone?: string | null;
  address?: string | null;
  taxNumber?: string | null;
  website?: string | null;
  currency: string;
  paymentTerms: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateVendorRequest {
  code: string;
  name: string;
  email?: string | null;
  phone?: string | null;
  address?: string | null;
  taxNumber?: string | null;
  website?: string | null;
  currency?: string;
  paymentTerms?: string;
  isActive?: boolean;
}

export interface VendorStatement {
  vendorId: string;
  vendorName: string;
  openingBalance: number;
  totalBilled: number;
  totalPaid: number;
  closingBalance: number;
  currency: string;
  asOf: string;
  lines: VendorStatementLine[];
}

export interface VendorStatementLine {
  billId: string;
  billNumber: string;
  date: string;
  description: string;
  debit: number;
  credit: number;
  balance: number;
}

// ============ Item (Inventory) ============
//
// Contract: GET /api/inventory/items

export interface ItemDto {
  id: string;
  companyId: string;
  sku: string;
  barcode?: string | null;
  name: string;
  description?: string | null;
  categoryId?: string | null;
  unitOfMeasureId?: string | null;
  itemType: number;
  costingMethod: number;
  averageCost: number;
  standardCost: number;
  inventoryAccountId?: string | null;
  cogsAccountId?: string | null;
  salesAccountId?: string | null;
  reorderLevel: number;
  reorderQuantity: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateItemRequest {
  sku: string;
  barcode?: string | null;
  name: string;
  description?: string | null;
  categoryId?: string | null;
  unitOfMeasureId?: string | null;
  itemType?: number;
  costingMethod?: number;
  averageCost?: number;
  standardCost?: number;
  reorderLevel?: number;
  reorderQuantity?: number;
  isActive?: boolean;
}

// ============ Sales Invoice (AccountsReceivable) ============
//
// Contract: GET /api/ar/sales-invoices
// Includes line items + posting state.

export type SalesInvoiceStatus = 'Draft' | 'Posted' | 'Paid' | 'Cancelled';

export interface SalesInvoiceLineDto {
  id: string;
  invoiceId: string;
  itemId: string;
  description: string;
  quantity: number;
  unitPrice: number;
  /** Computed: quantity * unitPrice. */
  lineTotal: number;
}

export interface SalesInvoiceDto {
  id: string;
  companyId: string;
  customerId: string;
  invoiceNumber: string;
  invoiceDate: string;
  dueDate: string;
  status: SalesInvoiceStatus;
  currency: string;
  subtotal: number;
  taxAmount: number;
  total: number;
  notes?: string | null;
  lines: SalesInvoiceLineDto[];
  createdAt: string;
  updatedAt: string;
}

export interface CreateSalesInvoiceRequest {
  customerId: string;
  invoiceNumber: string;
  invoiceDate: string;
  dueDate: string;
  currency?: string;
  notes?: string | null;
  lines: Array<{
    itemId: string;
    description: string;
    quantity: number;
    unitPrice: number;
  }>;
}

// ============ Paged (generic) ============
//
// Generic paged response used by the new list pages.
// Mirrors C# DTO `PagedResult<T>` in `src/backend/Shared/Infrastructure/`.

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

// ============ Module Visibility (Sprint 63 — DEC-217) ============
//
// Contract: GET /api/me/visible-modules → { "modules": ["Projects", "Finance", ...] }
//
// `ModuleCode` is the canonical list of module names the BE knows about. The
// string values must match the BE's `module` column in `permissions` and
// `module_visibility` (case-sensitive).
//
// L19 / DEC-095: the FE never sends a userId — the BE reads it from the JWT.

export type ModuleCode =
  | 'Projects'
  | 'Finance'
  | 'HR'
  | 'Payroll'
  | 'Inventory'
  | 'Procurement'
  | 'AR'
  | 'Companies'
  | 'Identity'
  | 'Dashboard';

export interface VisibleModulesResponse {
  /** Sorted, deduped list of module names the user can see. */
  modules: ModuleCode[];
}

// ============ My Permissions (Sprint 63 — DEC-218) ============
//
// Contract: GET /api/me/permissions → { "permissions": ["projects.view", ...] }
//
// Permission codes follow the pattern `<resource>.<action>` (e.g.
// `projects.create`, `finance.accounts.post`). The wildcard `admin.all` is
// the Admin-bypass token — if present, every `hasPermission(...)` call
// returns true (see `usePermissions` hook).
//
// L19 / DEC-095: the FE never sends a userId — the BE reads it from the JWT.

export interface MyPermissionsResponse {
  /** Sorted, deduped list of permission codes the user holds. */
  permissions: string[];
}

// ============ Re-exports (legacy DTOs) ============
//
// To keep the new demo pages clean, this file re-exports the legacy shapes
// from `api.ts` so consumers can `import { HoldingDetail } from '@/lib/api-types'`
// without caring about the file split.
export type {
  HoldingDetail,
  HoldingCompany,
  Company,
  CreateCompanyRequest,
  PagedCompanies,
  Account as LegacyAccount,
  ActivityItem,
  DashboardSummary,
} from './api';

// ============ Sprint 64 / DEC-225 — Sub-Statement types ============
//
// Wire-format for the Sub-Statement API. Returned by:
//   - GET /api/sub-contracts/{subContractId}/statement                  → SubStatement
//   - GET /api/subcontractors/{subId}/projects/{projectId}/summary     → SubStatementSummary
//
// CompanyId is intentionally NOT in the responses (L19 / DEC-095) — the
// caller already knows the active company from the JWT context, so we
// don't echo it back.

export interface SubStatement {
  subContractId: string;
  subcontractorName: string;
  subcontractorCode: string;
  contractNumber: string;
  scopeOfWork: string;
  contractValue: number;
  totalBilledGross: number;
  totalRetentionWithheld: number;
  totalRetentionReleased: number;
  totalPaid: number;
  outstandingBalance: number;
  /** Cumulative % complete (0-100). Capped at 100. */
  workCompletedToDate: number;
  billingCount: number;
  firstBillingDate: string | null;
  lastBillingDate: string | null;
  lastPaymentDate: string | null;
  /** 1=Active, 2=Completed, 3=Cancelled. */
  status: number;
  statusName: string;
  /** 'OK' | 'OVERDUE' | 'SETTLED' */
  healthStatus: 'OK' | 'OVERDUE' | 'SETTLED';
  healthStatusName: string;
}

export interface SubStatementSummary {
  subcontractorId: string;
  subcontractorName: string;
  projectId: string;
  projectName: string;
  subContractCount: number;
  totalContractValue: number;
  totalBilled: number;
  totalPaid: number;
  totalOutstanding: number;
}

// ============ Sprint 65 / Wave 2A: Dashboard Cross-Module (DEC-234 + DEC-236) ============
//
// Flat cross-module KPI payload served by GET /api/dashboard/cross-module. All values
// are LYD (Libyan Dinar) unless otherwise specified; the FE renders them as-is
// (the `format.ts` lib wraps the display formatting). Field names match the
// C# DTO `DashboardCrossModuleResponse` in
// `src/backend/Host/Controllers/DashboardCrossModuleController.cs`.

export interface DashboardCrossModuleResponse {
  /** SUM(sales_invoices.total - amount_paid) for unpaid posted invoices. */
  outstandingAR: number;
  /** SUM(sub_payments.amount) for unmatched sub-payments. 0 before Sprint 64 merge. */
  outstandingAP: number;
  /** OutstandingAR - OutstandingAP. */
  netPosition: number;
  /** Active non-cancelled projects in the company. */
  projectCount: number;
  /** SUM(project_contracts.contract_value) for the company's active projects. */
  totalContractValue: number;
  /** SUM(sales_invoices.total_amount) for the company's posted invoices. */
  totalRevenue: number;
  /** SUM(sub_payments.amount) for the company. 0 before Sprint 64 merge. */
  totalSubcontractorCost: number;
  /** Count of active projects where sum(cost) > sum(revenue). */
  unprofitableProjects: number;
}

export type ProjectHealthStatus = 'OK' | 'AT_RISK' | 'OVER_BUDGET';

// Per-project profitability row served by GET /api/dashboard/project-profitability.
// Includes the subcontractor cost (Sprint 65 / DEC-233).
export interface ProjectProfitabilityResponse {
  projectId: string;
  projectCode: string;
  projectName: string;
  totalRevenue: number;
  /** Includes the subcontractor cost (DEC-233). */
  totalCosts: number;
  grossProfit: number;
  profitMarginPercent: number;
  healthStatus: ProjectHealthStatus;
}

// ============ Sprint 65 / Wave 3A: Bank Reconciliation (DEC-235 + DEC-237) ============
//
// Bank reconciliation matches incoming AR Receipts to expected AP Sub-Payments. The
// matching algorithm is a pure-function scorer (see BE `BankReconciliationService`)
// that produces a 0-100 score based on amount tolerance (±5%) and date tolerance
// (±30 days).
//
// The FE surfaces the suggested matches as cards and the accountant confirms the
// match with a single click. The queue endpoint returns all posted receipts that
// have not yet been matched to a sub-payment.

// One possible match between a Receipt and a Sub-Payment. Scored 0-100.
export type MatchQuality = 'EXCELLENT' | 'GOOD' | 'FAIR' | 'POOR';

export interface SubPaymentMatch {
  subPaymentId: string;
  subContractId: string;
  subcontractorName: string;
  paymentNumber: string;
  amount: number;
  paymentDate: string; // ISO-8601
  score: number; // 0-100
  matchQuality: MatchQuality;
  matchQualityName: string; // Arabic label
}

// A receipt that has not yet been matched to a sub-payment (queue row).
export interface UnmatchedReceipt {
  receiptId: string;
  receiptNumber: string;
  receiptDate: string; // ISO-8601
  amount: number;
  customerName: string | null;
  daysSinceReceipt: number;
}
