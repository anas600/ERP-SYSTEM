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
