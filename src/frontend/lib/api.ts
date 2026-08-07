// API client للـ ERP-SYSTEM
// يستخدم localStorage لحفظ الـ token
//
// Phase 6.3: Multi-Company model.
//   - The active company is tracked in localStorage as `currentCompanyId`.
//   - The X-Company-Id header is sent on every authenticated request.
//   - The Holding Company is auto-seeded at startup. Register creates the
//     first user under the Holding (no tenant wizard).

import axios, { AxiosInstance } from 'axios';

// في الإنتاج (HF Spaces): نستخدم same-origin (Caddy reverse proxy)
// في dev: NEXT_PUBLIC_API_URL=http://localhost:5000
const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || '';

// DEC-093: Bumped to 60s to align with backend CommandTimeout=60s
// and OutboxProcessor exponential backoff window. HF Space proxy default
// is still ~30s, so heavy register flows (CoA + UoMs + Categories) might
// still hit a proxy timeout on cold start — see RUNBOOK.md for retry.
export const API_TIMEOUT_MS = 60_000;

export const api: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
  timeout: API_TIMEOUT_MS,
});

// Request interceptor: JWT token + X-Company-Id header تلقائياً
api.interceptors.request.use((config) => {
  if (typeof window !== 'undefined') {
    const token = localStorage.getItem('accessToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    // Phase 6.3: send the active company id on every request so the backend's
    // CompanyContextMiddleware can resolve ICompanyContext. The user may switch
    // the active company via the <CompanySwitcher /> — see lib/useCompany.ts.
    const currentCompanyId = localStorage.getItem('currentCompanyId');
    if (currentCompanyId) {
      config.headers['X-Company-Id'] = currentCompanyId;
    }
  }
  return config;
});

// Response interceptor: اعرض errors بشكل أنيق
api.interceptors.response.use(
  (r) => r,
  (err) => {
    if (err.response?.status === 401) {
      if (typeof window !== 'undefined') {
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('user');
        localStorage.removeItem('currentCompanyId');
        localStorage.removeItem('defaultCompanyId');
        window.location.href = '/login';
      }
    }
    return Promise.reject(err);
  }
);

// ============ Types ============
// ملاحظة: الـ contracts تطابق AuthDtos.cs في الـ backend (C#).
//   - Register: creates the first user under the default Holding Company
//     (no tenant wizard, no subdomain)
//   - Login:    no tenant field (the user is global; companies are user→company joins)

export interface RegisterRequest {
  email: string;
  password: string;
  fullName: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface UserCompanyInfo {
  companyId: string;
  code: string;
  name: string;
  isDefault: boolean;
  isHolding: boolean;
}

export interface GetUserCompaniesResponse {
  userId: string;
  defaultCompanyId: string;
  companies: UserCompanyInfo[];
}

export interface UserInfo {
  id: string;
  email: string;
  fullName: string;
  roles: string[];
  /** Default company (set from user_companies.is_default = true). */
  defaultCompanyId: string;
  /** All companies the user has access to. Drives <CompanySwitcher />. */
  companies: UserCompanyInfo[];
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
  user: UserInfo;
  /** The Holding Company this deployment is rooted at. */
  holdingCompanyId: string;
}

// ============ Finance ============
export interface Account {
  id: string;
    companyId?: string;
  code: string;
  name: string;
  description?: string;
  type: number;  // 1=Asset, 2=Liability, 3=Equity, 4=Revenue, 5=Expense
  normalBalance: number;  // 1=Debit, 2=Credit
  parentAccountId?: string;
  isPostable: boolean;
  isActive: boolean;
  isIntercompany: boolean;
  // Sprint 52a: 1=L1 Class, 2=L2 Sub-class, 3=L3 Control, 4=L4 Detail. Null = not yet backfilled.
  level?: number;
  createdAt: string;
  updatedAt: string;
}

// Sprint 52a: tree view of the CoA. L1 roots contain nested L2 → L3 → L4 children.
// `type` and `normalBalance` are strings (BE EnumStringTypeHandler) for readability.
export interface AccountTreeNode {
  id: string;
  code: string;
  name: string;
  type: string;  // 'Asset' | 'Liability' | 'Equity' | 'Revenue' | 'Expense'
  normalBalance: string;  // 'Debit' | 'Credit'
  level: number;  // 1..4 (or 99 = orphan)
  isPostable: boolean;
  children: AccountTreeNode[];
}

export const ACCOUNT_TYPES: Record<number, string> = {
  1: 'أصول',
  2: 'خصوم',
  3: 'حقوق ملكية',
  4: 'إيرادات',
  5: 'مصروفات',
};

// ============ Inventory ============
// Phase 6.3: in-app notification type (Cycle 8 / DEC-073)
// Source: src/backend/Modules/Notifications/Entities/Notification.cs
export interface Notification {
  id: string;
  companyId: string;
  userId: string;
  type: string;        // "LowStock" حالياً، مستقبلياً "JournalPosted", ...
  title: string;
  message: string;
  referenceType?: string;
  referenceId?: string;
  isRead: boolean;
  createdAt: string;
  readAt?: string;
}

export interface Item {
  id: string;
    companyId: string;
  sku: string;
  barcode?: string;
  name: string;
  description?: string;
  categoryId?: string;
  unitOfMeasureId: string;
  itemType: string;
  costingMethod: string;
  averageCost: number;
  reorderLevel: number;
  reorderQuantity: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

// ============ Projects ============
export interface Project {
  id: string;
    companyId: string;
  costCenterId: string;
  code: string;
  name: string;
  description?: string;
  status: number;  // 1=Planning, 2=Active, 3=OnHold, 4=Completed, 5=Cancelled
  budget: number;
  startDate: string;
  endDate?: string;
  isActive: boolean;
  createdAt: string;
}

export const PROJECT_STATUSES: Record<number, string> = {
  1: 'تخطيط',
  2: 'نشط',
  3: 'معلق',
  4: 'مكتمل',
  5: 'ملغي',
};

// ============ Resources (Sprint 32 / DEC-112) ============
export interface Resource {
  id: string;
  companyId: string;
  code: string;
  name: string;
  type: number;  // 1=Labor, 2=Equipment, 3=Material, 4=Service
  hourlyRate: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export const RESOURCE_TYPES: Record<number, string> = {
  1: 'عمالة',
  2: 'معدات',
  3: 'مواد',
  4: 'خدمات',
};

// ============ Reports ============
// Sprint 36 (DEC-122): Trial Balance + Customer/Vendor Statements
// Trial Balance — matches AccountBalanceResponse in FinanceDtos.cs
// AccountType enum: Asset=1, Liability=2, Equity=3, Revenue=4, Expense=5
// BE returns type/normalBalance as STRING (Dapper EnumStringTypeHandler) for
// human-readable JSON. We accept string here and look up via a map.
export type AccountTypeName = 'Asset' | 'Liability' | 'Equity' | 'Revenue' | 'Expense';
export type NormalBalanceName = 'Debit' | 'Credit';

export const ACCOUNT_TYPE_LABELS: Record<AccountTypeName, string> = {
  Asset: 'أصول',
  Liability: 'خصوم',
  Equity: 'حقوق ملكية',
  Revenue: 'إيرادات',
  Expense: 'مصروفات',
};

// Display order for grouped tables
export const ACCOUNT_TYPE_ORDER: AccountTypeName[] = [
  'Asset',
  'Liability',
  'Equity',
  'Revenue',
  'Expense',
];

export interface TrialBalanceRow {
  accountId: string;
  accountCode: string;
  accountName: string;
  type: AccountTypeName;
  normalBalance: NormalBalanceName;
  totalDebit: number;
  totalCredit: number;
  balance: number;
}

export interface TrialBalanceReport {
  asOfDate: string;
  rows: TrialBalanceRow[];
}

// ============== Sprint 48 — Financial Reports DTOs ==============

export interface BalanceSheetRow {
  accountId: string;
  accountCode: string;
  accountName: string;
  balance: number;
}

export interface BalanceSheetSection {
  title: string;
  rows: BalanceSheetRow[];
  subtotal: number;
}

export interface BalanceSheetReport {
  asOfDate: string;
  assets: BalanceSheetSection;
  liabilities: BalanceSheetSection;
  equity: BalanceSheetSection;
  totalAssets: number;
  totalLiabilities: number;
  totalEquity: number;
  totalLiabilitiesAndEquity: number;
  isBalanced: boolean;
  variance: number;
}

export interface IncomeStatementRow {
  accountId: string;
  accountCode: string;
  accountName: string;
  amount: number;
}

export interface IncomeStatementSection {
  title: string;
  rows: IncomeStatementRow[];
  subtotal: number;
}

export interface IncomeStatementReport {
  from: string;
  to: string;
  revenue: IncomeStatementSection;
  expenses: IncomeStatementSection;
  totalRevenue: number;
  totalExpenses: number;
  netIncome: number;
  isProfitable: boolean;
}

export interface CashFlowLine {
  description: string;
  amount: number;
}

export interface CashFlowSection {
  title: string;
  lines: CashFlowLine[];
  subtotal: number;
}

export interface CashFlowReport {
  from: string;
  to: string;
  operating: CashFlowSection;
  investing: CashFlowSection;
  financing: CashFlowSection;
  netOperatingCash: number;
  netInvestingCash: number;
  netFinancingCash: number;
  netChangeInCash: number;
}

export interface APAgingBucket {
  vendorId: string;
  vendorCode: string;
  vendorName: string;
  current: number;
  days31To60: number;
  days61To90: number;
  days91Plus: number;
  total: number;
}

export interface APAgingReport {
  asOfDate: string;
  vendors: APAgingBucket[];
  totalCurrent: number;
  total31To60: number;
  total61To90: number;
  total91Plus: number;
  grandTotal: number;
}

// ============ Statements (Customer / Vendor) ============
// Sprint 36 (DEC-122) — chronological ledger per party
export interface StatementLine {
  date: string;
  type: 'Opening' | 'فاتورة' | 'سند قبض' | 'فاتورة مورّد' | 'دفعة';
  reference: string;
  description: string;
  debit: number;
  credit: number;
  runningBalance: number;
}

export interface CustomerStatement {
  customerId: string;
  customerCode: string;
  customerName: string;
  from: string | null;
  to: string | null;
  openingBalance: number;
  totalInvoiced: number;
  totalReceived: number;
  closingBalance: number;
  lines: StatementLine[];
}

export interface VendorStatement {
  vendorId: string;
  vendorCode: string;
  vendorName: string;
  from: string | null;
  to: string | null;
  openingBalance: number;
  totalBilled: number;
  totalPaid: number;
  closingBalance: number;
  lines: StatementLine[];
}

// ============ Procurement ============
// الـ DTOs تطابق Contracts في `src/backend/Modules/Procurement/Application/Dtos.cs`
// (Backend مبني في فرع منفصل — هذا الـ contract المتوقع بناءً على gap-analysis.md §3)

export interface Vendor {
  id: string;
    name: string;
  email?: string;
  phone?: string;
  address?: string;
  taxNumber?: string;
  currency: string;
  paymentTerms: string; // Net30, Net60, Cash
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export const PAYMENT_TERMS: Record<string, string> = {
  Cash: 'نقدي',
  Net15: 'صافي 15 يوم',
  Net30: 'صافي 30 يوم',
  Net60: 'صافي 60 يوم',
  Net90: 'صافي 90 يوم',
};

// PO Status: Draft=1, Pending=2, Approved=3, Sent=4, Received=5, Cancelled=6
export const PO_STATUSES: Record<number, string> = {
  1: 'مسودة',
  2: 'بانتظار الموافقة',
  3: 'معتمد',
  4: 'مُرسل للمورّد',
  5: 'مُستلَم',
  6: 'ملغي',
};

export const PO_STATUS_VARIANTS: Record<number, 'neutral' | 'warning' | 'info' | 'success' | 'danger'> = {
  1: 'neutral',
  2: 'warning',
  3: 'info',
  4: 'info',
  5: 'success',
  6: 'danger',
};

export interface PurchaseOrderLine {
  id: string;
  itemId: string;
  itemName?: string;
  quantity: number;
  unitPrice: number;
  taxRate: number;
  subTotal: number;
}

export interface PurchaseOrder {
  id: string;
    poNumber: string;
  vendorId: string;
  vendorName?: string;
  status: number;
  orderDate: string;
  expectedDate?: string;
  currency: string;
  totalAmount: number;
  notes?: string;
  lines: PurchaseOrderLine[];
  createdAt: string;
}

// GR Status: Draft=1, Received=2, Cancelled=3
export const GR_STATUSES: Record<number, string> = {
  1: 'مسودة',
  2: 'مُستلَم',
  3: 'ملغي',
};

export const GR_STATUS_VARIANTS: Record<number, 'neutral' | 'success' | 'danger'> = {
  1: 'neutral',
  2: 'success',
  3: 'danger',
};

export interface GoodsReceiptLine {
  id: string;
  itemId: string;
  itemName?: string;
  quantity: number;
  notes?: string;
}

export interface GoodsReceipt {
  id: string;
    grNumber: string;
  purchaseOrderId: string;
  poNumber?: string;
  poStatus?: string;        // DEC-031: enriched
  vendorName?: string;
  vendorId?: string;
  vendorCode?: string;      // DEC-031: enriched
  status: number;
  receivedDate: string;
  warehouseId: string;
  warehouseName?: string;
  warehouseCode?: string;   // DEC-031: enriched
  notes?: string;           // DEC-031: enriched
  currency?: string;
  lines: GoodsReceiptLine[];
  createdAt: string;
}

// Bill Status: Draft=1, Posted=2, Paid=3, Cancelled=4
export const BILL_STATUSES: Record<number, string> = {
  1: 'مسودة',
  2: 'مُرحَّل',
  3: 'مُدفوع',
  4: 'ملغي',
};

export const BILL_STATUS_VARIANTS: Record<number, 'neutral' | 'info' | 'success' | 'danger'> = {
  1: 'neutral',
  2: 'info',
  3: 'success',
  4: 'danger',
};

export interface VendorBillLine {
  id: string;
  itemId: string;
  itemName?: string;
  quantity: number;
  unitPrice: number;
  taxRate: number;
  subTotal: number;
}

export interface VendorBill {
  id: string;
    billNumber: string;
  goodsReceiptId: string;
  grNumber?: string;
  vendorId: string;
  vendorName?: string;
  status: number;
  billDate: string;
  dueDate?: string;
  currency: string;
  subTotal: number;
  taxAmount: number;
  totalAmount: number;
  notes?: string;
  lines: VendorBillLine[];
  createdAt: string;
}

// ============ HR ============
// الـ DTOs تطابق Contracts في `src/backend/Modules/HR/Application/Dtos.cs`

// Leave Type: Annual=1, Sick=2, Emergency=3, Unpaid=4
export const LEAVE_TYPES: Record<number, string> = {
  1: 'سنوية',
  2: 'مرضية',
  3: 'طارئة',
  4: 'بدون راتب',
};

// Leave Status: Pending=1, Approved=2, Rejected=3
export const LEAVE_STATUSES: Record<number, string> = {
  1: 'بانتظار الموافقة',
  2: 'معتمدة',
  3: 'مرفوضة',
};

export const LEAVE_STATUS_VARIANTS: Record<number, 'warning' | 'success' | 'danger'> = {
  1: 'warning',
  2: 'success',
  3: 'danger',
};

// Attendance Type: CheckIn=1, CheckOut=2
export const ATTENDANCE_TYPES: Record<number, string> = {
  1: 'حضور',
  2: 'انصراف',
};

export interface Department {
  id: string;
    name: string;
  code: string;
  parentId?: string;
  managerId?: string;
  // Sprint 31 (DEC-107): manager name + code (L40) so the FE doesn't show raw GUIDs.
  managerName?: string;
  managerCode?: string;
  // Sprint 31 (DEC-107): employee count per department.
  employeeCount?: number;
  isActive: boolean;
}

export interface Employee {
  id: string;
    employeeNumber: string;
  fullName: string;
  email: string;
  phone?: string;
  nationalId?: string;
  departmentId?: string;
  departmentName?: string;
  jobTitle?: string;
  hireDate: string;
  terminationDate?: string;
  baseSalary: number;
  isActive: boolean;
  createdAt: string;
}

export interface AttendanceRecord {
  id: string;
    employeeId: string;
  employeeName?: string;
  type: number; // 1=CheckIn, 2=CheckOut
  timestamp: string;
  notes?: string;
}

export interface LeaveRequest {
  id: string;
    employeeId: string;
  employeeName?: string;
  leaveType: number;
  startDate: string;
  endDate: string;
  totalDays: number;
  status: number;
  reason?: string;
  approverId?: string;
  approverName?: string;
  approvedAt?: string;
  notes?: string;
  createdAt: string;
}

// ============ Payroll ============
// الـ DTOs تطابق Contracts في `src/backend/Modules/Payroll/Application/Dtos.cs`
// الـ state machine: Draft=1, Processing=2, Posted=3, Cancelled=4

export const PAYROLL_RUN_STATUSES: Record<number, string> = {
  1: 'مسودة',
  2: 'قيد المعالجة',
  3: 'مُرحَّل',
  4: 'ملغي',
};

export const PAYROLL_RUN_STATUS_VARIANTS: Record<number, 'neutral' | 'warning' | 'info' | 'success' | 'danger'> = {
  1: 'neutral',
  2: 'warning',
  3: 'success',
  4: 'danger',
};

// PayrollItem status: Draft=1, Processed=2, Posted=3, Cancelled=4
export const PAYROLL_ITEM_STATUSES: Record<number, string> = {
  1: 'مسودة',
  2: 'مُعالَج',
  3: 'مُرحَّل',
  4: 'ملغي',
};

// SalaryComponentType: Earning=1, Deduction=2
export const COMPONENT_TYPES: Record<number, 'earning' | 'deduction'> = {
  1: 'earning',
  2: 'deduction',
};

export const COMPONENT_TYPE_LABELS: Record<number, string> = {
  1: 'مستحق',
  2: 'مستقطع',
};

export interface PayrollRun {
  id: string;
    periodStart: string;
  periodEnd: string;
  status: number;
  totalGross: number;
  totalNet: number;
  processedAt?: string;
  postedAt?: string;
  notes?: string;
  createdAt: string;
  itemsCount?: number;
}

export interface PayslipComponent {
  id: string;
  componentType: number;
  name: string;
  amount: number;
  sortOrder: number;
}

export interface PayrollItem {
  id: string;
    payrollRunId: string;
  employeeId: string;
  employeeNumber?: string;
  employeeName?: string;
  baseSalary: number;
  grossSalary: number;
  taxAmount: number;
  socialInsuranceEmployee: number;
  netSalary: number;
  status: number;
  paymentDays: number;
  notes?: string;
  components: PayslipComponent[];
}

export interface Payslip extends PayrollItem {}

export interface EosResponse {
  employeeId: string;
  employeeNumber?: string;
  employeeName?: string;
  hireDate: string;
  terminationDate: string;
  yearsOfService: number;
  monthlySalary: number;
  eosAmount: number;
  formula: string;
}

export interface CreatePayrollRunRequest {
  periodStart: string;
  periodEnd: string;
  notes?: string;
}

// ============ Accounts Receivable (AR) ============
// الـ DTOs تطابق Contracts في `src/backend/Modules/AccountsReceivable/Application/Dtos.cs`

export interface Customer {
  id: string;
    companyId: string;
  code: string;
  name: string;
  nameEn?: string;
  taxId?: string;
  email?: string;
  phone?: string;
  address?: string;
  creditLimit?: number;
  paymentTermsDays: number;
  isActive: boolean;
}

export interface SalesInvoiceLine {
  id: string;
  lineNumber: number;
  description: string;
  itemId?: string;
  quantity: number;
  unitPrice: number;
  taxRate: number;
  lineTotal: number;
}

// SalesInvoice status: Draft=1, Sent=2, PartiallyPaid=3, Paid=4, Overdue=5, Cancelled=6
export const SALES_INVOICE_STATUSES: Record<number, string> = {
  1: 'مسودة',
  2: 'مُرسل',
  3: 'مدفوع جزئياً',
  4: 'مدفوع',
  5: 'متأخر',
  6: 'ملغي',
};

export const SALES_INVOICE_STATUS_VARIANTS: Record<number, 'neutral' | 'info' | 'warning' | 'success' | 'danger'> = {
  1: 'neutral',
  2: 'info',
  3: 'warning',
  4: 'success',
  5: 'danger',
  6: 'danger',
};

export interface SalesInvoice {
  id: string;
    customerId: string;
  customerName?: string;
  invoiceNumber: string;
  invoiceDate: string;
  dueDate?: string;
  currencyCode: string;
  exchangeRate: number;
  subtotal: number;
  taxAmount: number;
  totalAmount: number;
  paidAmount: number;
  outstanding: number;
  status: number;
  notes?: string;
  projectId?: string;
  // Sprint 35 (DEC-118) + Sprint 39 (DEC-125): VAT 5% opt-in flag
  useVat5?: boolean;
  postedAt?: string;
  journalEntryId?: string;
  createdAt: string;
  lines: SalesInvoiceLine[];
  allocations: ReceiptAllocation[];
}

export const PAYMENT_METHODS: Record<string, string> = {
  Cash: 'نقدي',
  Bank: 'بنك',
  Transfer: 'تحويل',
  Check: 'شيك',
};

export interface ReceiptAllocation {
  id: string;
  salesInvoiceId: string;
  salesInvoiceNumber?: string;
  amountApplied: number;
}

export interface Receipt {
  id: string;
    customerId: string;
  customerName?: string;
  receiptNumber: string;
  receiptDate: string;
  amount: number;
  currencyCode: string;
  paymentMethod?: string;
  notes?: string;
  postedAt?: string;
  journalEntryId?: string;
  createdAt: string;
  allocations: ReceiptAllocation[];
}

export interface ArAgingBucket {
  bucket0To30: number;
  bucket31To60: number;
  bucket61To90: number;
  bucket91To120: number;
  bucket120Plus: number;
  total: number;
}

export interface ArAgingRow {
  customerId: string;
  customerCode: string;
  customerName: string;
  buckets: ArAgingBucket;
}

export interface ArAgingReport {
  asOfDate: string;
  rows: ArAgingRow[];
  grandTotal: ArAgingBucket;
}

// ============ AR API ============
// endpoints: /api/ar/{customers|sales-invoices|receipts|aging}

export const arApi = {
  // ----- Customers -----
  listCustomers: async (): Promise<Customer[]> => {
    const r = await api.get<Customer[]>('/api/ar/customers');
    return r.data;
  },
  getCustomer: async (id: string): Promise<Customer> => {
    const r = await api.get<Customer>(`/api/ar/customers/${id}`);
    return r.data;
  },
  createCustomer: async (data: Omit<Customer, 'id' | 'companyId' | 'isActive'>): Promise<Customer> => {
    const r = await api.post<Customer>('/api/ar/customers', data);
    return r.data;
  },
  updateCustomer: async (id: string, data: Partial<Omit<Customer, 'id' | 'companyId'>>): Promise<Customer> => {
    const r = await api.put<Customer>(`/api/ar/customers/${id}`, data);
    return r.data;
  },
  deactivateCustomer: async (id: string): Promise<void> => {
    await api.delete(`/api/ar/customers/${id}`);
  },

  // ----- Customer Statement (Sprint 36, DEC-122) -----
  // كشـف حساب العميل: رصيد افتتاحي + فواتير + مقبوضات + رصيد ختامي
  getCustomerStatement: async (
    id: string,
    from?: string,
    to?: string
  ): Promise<CustomerStatement> => {
    const r = await api.get<CustomerStatement>(
      `/api/ar/customers/${id}/statement`,
      { params: { from, to } }
    );
    return r.data;
  },

  // ----- Sales Invoices -----
  listInvoices: async (): Promise<SalesInvoice[]> => {
    const r = await api.get<SalesInvoice[]>('/api/ar/sales-invoices');
    return r.data;
  },
  getInvoice: async (id: string): Promise<SalesInvoice> => {
    const r = await api.get<SalesInvoice>(`/api/ar/sales-invoices/${id}`);
    return r.data;
  },
  createInvoice: async (data: {
    customerId: string;
    invoiceDate: string;
    dueDate?: string;
    currencyCode: string;
    exchangeRate: number;
    notes?: string;
    projectId?: string;
    // Sprint 39 (DEC-125): useVat5 is opt-in, OFF by default. Per Libyan rule
    // ("لا نطبق الضريبة بشكل افتراضي"). When true, the BE applies the VAT 5%
    // posting rule and emits Cr 1411 (VAT Output). When false, no VAT.
    useVat5?: boolean;
    lines: { description: string; quantity: number; unitPrice: number; taxRate: number; itemId?: string }[];
    postImmediately?: boolean;
  }): Promise<SalesInvoice> => {
    const r = await api.post<SalesInvoice>('/api/ar/sales-invoices', data);
    return r.data;
  },
  updateInvoice: async (
    id: string,
    data: {
      customerId: string;
      invoiceDate: string;
      dueDate?: string;
      currencyCode: string;
      exchangeRate: number;
      notes?: string;
      projectId?: string;
      lines: { description: string; quantity: number; unitPrice: number; taxRate: number; itemId?: string }[];
    }
  ): Promise<SalesInvoice> => {
    const r = await api.put<SalesInvoice>(`/api/ar/sales-invoices/${id}`, data);
    return r.data;
  },
  postInvoice: async (id: string): Promise<SalesInvoice> => {
    const r = await api.put<SalesInvoice>(`/api/ar/sales-invoices/${id}/post`);
    return r.data;
  },
  cancelInvoice: async (id: string): Promise<SalesInvoice> => {
    const r = await api.put<SalesInvoice>(`/api/ar/sales-invoices/${id}/cancel`);
    return r.data;
  },

  // ----- Receipts -----
  listReceipts: async (): Promise<Receipt[]> => {
    const r = await api.get<Receipt[]>('/api/ar/receipts');
    return r.data;
  },
  getReceipt: async (id: string): Promise<Receipt> => {
    const r = await api.get<Receipt>(`/api/ar/receipts/${id}`);
    return r.data;
  },
  createReceipt: async (data: {
    customerId: string;
    receiptDate: string;
    amount: number;
    currencyCode: string;
    paymentMethod?: string;
    notes?: string;
    allocations: { salesInvoiceId: string; amountApplied: number }[];
    postImmediately?: boolean;
  }): Promise<Receipt> => {
    const r = await api.post<Receipt>('/api/ar/receipts', data);
    return r.data;
  },
  postReceipt: async (id: string): Promise<Receipt> => {
    const r = await api.put<Receipt>(`/api/ar/receipts/${id}/post`);
    return r.data;
  },
  reverseReceipt: async (id: string): Promise<Receipt> => {
    const r = await api.put<Receipt>(`/api/ar/receipts/${id}/reverse`);
    return r.data;
  },

  // ----- Aging Report -----
  aging: async (asOfDate?: string): Promise<ArAgingReport> => {
    const r = await api.get<ArAgingReport>('/api/ar/aging', { params: asOfDate ? { asOfDate } : undefined });
    return r.data;
  },
};

// ============ Error extraction helper ============
// للحصول على رسالة خطأ أنيقة من Axios errors
export interface ApiError {
  detail?: string;
  message?: string;
  error?: string;
}

export function getErrorMessage(e: unknown, fallback = 'حدث خطأ غير متوقع'): string {
  const err = e as { response?: { data?: ApiError }; message?: string };
  return (
    err?.response?.data?.detail ||
    err?.response?.data?.message ||
    err?.response?.data?.error ||
    err?.message ||
    fallback
  );
}

// ============ API helpers ============

export const authApi = {
  register: async (data: RegisterRequest): Promise<AuthResponse> => {
    const r = await api.post<AuthResponse>('/api/auth/register', data);
    if (typeof window !== 'undefined') {
      localStorage.setItem('accessToken', r.data.accessToken);
      localStorage.setItem('refreshToken', r.data.refreshToken);
      localStorage.setItem('user', JSON.stringify(r.data.user));
      // Phase 6.3: persist the user's default + current company so the
      // <CompanySwitcher /> + X-Company-Id header work across reloads.
      localStorage.setItem('defaultCompanyId', r.data.user.defaultCompanyId);
      localStorage.setItem('currentCompanyId', r.data.user.defaultCompanyId);
    }
    return r.data;
  },
  login: async (data: LoginRequest): Promise<AuthResponse> => {
    const r = await api.post<AuthResponse>('/api/auth/login', data);
    if (typeof window !== 'undefined') {
      localStorage.setItem('accessToken', r.data.accessToken);
      localStorage.setItem('refreshToken', r.data.refreshToken);
      localStorage.setItem('user', JSON.stringify(r.data.user));
      localStorage.setItem('defaultCompanyId', r.data.user.defaultCompanyId);
      localStorage.setItem('currentCompanyId', r.data.user.defaultCompanyId);
    }
    return r.data;
  },
  logout: () => {
    if (typeof window !== 'undefined') {
      localStorage.removeItem('accessToken');
      localStorage.removeItem('refreshToken');
      localStorage.removeItem('user');
      localStorage.removeItem('currentCompanyId');
      localStorage.removeItem('defaultCompanyId');
    }
  },
  me: async (): Promise<UserInfo> => {
    const r = await api.get<UserInfo>('/api/auth/me');
    if (typeof window !== 'undefined') {
      // Refresh cached user so defaultCompanyId + companies stay in sync.
      localStorage.setItem('user', JSON.stringify(r.data));
      localStorage.setItem('defaultCompanyId', r.data.defaultCompanyId);
    }
    return r.data;
  },
  /** Phase 6.3: returns the full list of companies the user has access to. */
  getUserCompanies: async (): Promise<GetUserCompaniesResponse> => {
    const r = await api.get<GetUserCompaniesResponse>('/api/auth/me/companies');
    if (typeof window !== 'undefined') {
      localStorage.setItem('defaultCompanyId', r.data.defaultCompanyId);
    }
    return r.data;
  },
  getUser: (): UserInfo | null => {
    if (typeof window === 'undefined') return null;
    const u = localStorage.getItem('user');
    return u ? JSON.parse(u) : null;
  },
  isLoggedIn: (): boolean => {
    if (typeof window === 'undefined') return false;
    return !!localStorage.getItem('accessToken');
  },
  // Phase 6.3: helpers for the <CompanySwitcher /> component. The current
  // company is the one whose id is sent as `X-Company-Id` on every API call.
  getCurrentCompanyId: (): string | null => {
    if (typeof window === 'undefined') return null;
    return localStorage.getItem('currentCompanyId');
  },
  setCurrentCompanyId: (id: string) => {
    if (typeof window !== 'undefined') {
      localStorage.setItem('currentCompanyId', id);
    }
  },
  getDefaultCompanyId: (): string | null => {
    if (typeof window === 'undefined') return null;
    return localStorage.getItem('defaultCompanyId');
  },
};

// ============ Journal Entry types (Sprint 39, DEC-125) ============
// الـ API client methods (L60): يجب استخدام api client method بدلاً من raw fetch()
// حتى يتم إرفاق JWT تلقائياً. الـ types موحدة هنا لتجنب duplicate interfaces.

export interface JournalEntryLine {
  lineNumber: number;
  accountId: string;
  accountCode: string;
  accountName: string;
  debit: number;
  credit: number;
  description?: string;
}

export interface JournalEntry {
  id: string;
  entryNumber: string;
  entryDate: string;
  description: string;
  reference?: string;
  status: number; // 1=Draft, 2=Posted, 3=Reversed
  postedAt?: string;
  totalDebit: number;
  totalCredit: number;
  lines?: JournalEntryLine[];
}

export interface JournalEntryDetail extends JournalEntry {
  lines: JournalEntryLine[];
  createdAt?: string;
  createdBy?: string;
  postingRuleId?: string;
  sourceType?: string;
  sourceId?: string;
}

export interface CreateJournalEntryRequest {
  entryDate: string;
  description: string;
  reference?: string;
  lines: { accountId: string; debit: number; credit: number; description?: string }[];
  postImmediately?: boolean;
}

export const financeApi = {
  listAccounts: async (): Promise<Account[]> => {
    const r = await api.get<Account[]>('/api/finance/accounts');
    return r.data;
  },
  // Sprint 52a: tree view of the CoA. Used by /finance/accounts-tree.
  getAccountsTree: async (): Promise<AccountTreeNode[]> => {
    const r = await api.get<AccountTreeNode[]>('/api/finance/accounts/tree');
    return r.data;
  },
  createAccount: async (data: Partial<Account>): Promise<Account> => {
    const r = await api.post<Account>('/api/finance/accounts', data);
    return r.data;
  },
  // ----- Trial Balance (Sprint 36, DEC-122) -----
  // ميزان المراجعة: كل الحسابات وأرصدتها في تاريخ معين
  getTrialBalance: async (asOf?: string): Promise<TrialBalanceRow[]> => {
    const r = await api.get<TrialBalanceRow[]>('/api/finance/ledger/trial-balance', {
      params: asOf ? { asOf } : undefined,
    });
    return r.data;
  },
  // ----- General Ledger per account (Sprint 38, DEC-124) -----
  // دفتر الأستاذ: كل الحركات على حساب معين بترتيب زمني
  getAccountLedger: async (accountId: string, from?: string, to?: string): Promise<unknown> => {
    const r = await api.get<unknown>(`/api/finance/ledger/accounts/${accountId}`, {
      params: { from, to },
    });
    return r.data;
  },
  // ----- Sprint 48 (DEC-130..132) — Financial Reports -----
  /** الميزانية العمومية — Balance Sheet */
  getBalanceSheet: async (asOf?: string): Promise<BalanceSheetReport> => {
    const r = await api.get<BalanceSheetReport>('/api/finance/ledger/balance-sheet', {
      params: asOf ? { asOf } : undefined,
    });
    return r.data;
  },
  /** قائمة الدخل — Income Statement (P&L) */
  getIncomeStatement: async (from?: string, to?: string): Promise<IncomeStatementReport> => {
    const r = await api.get<IncomeStatementReport>('/api/finance/ledger/income-statement', {
      params: { from, to },
    });
    return r.data;
  },
  /** التدفقات النقدية — Cash Flow (Indirect Method) */
  getCashFlow: async (from?: string, to?: string): Promise<CashFlowReport> => {
    const r = await api.get<CashFlowReport>('/api/finance/ledger/cash-flow', {
      params: { from, to },
    });
    return r.data;
  },
  /** أعمار الذمم الدائنة — AP Aging */
  getAPAging: async (asOf?: string): Promise<APAgingReport> => {
    const r = await api.get<APAgingReport>('/api/procurement/ap-aging', {
      params: asOf ? { asOf } : undefined,
    });
    return r.data;
  },
  // Sprint 39 (DEC-125): Journal entries list (L60 — API client method so JWT is attached)
  listJournalEntries: async (): Promise<JournalEntry[]> => {
    const r = await api.get<JournalEntry[]>('/api/finance/journal-entries');
    return r.data;
  },
  getJournalEntry: async (id: string): Promise<JournalEntryDetail> => {
    const r = await api.get<JournalEntryDetail>(`/api/finance/journal-entries/${id}`);
    return r.data;
  },
  createJournalEntry: async (data: CreateJournalEntryRequest): Promise<JournalEntry> => {
    const r = await api.post<JournalEntry>('/api/finance/journal-entries', data);
    return r.data;
  },
  postJournalEntry: async (id: string): Promise<{ journalEntryId: string; entryNumber: string; status: string }> => {
    const r = await api.post<{ journalEntryId: string; entryNumber: string; status: string }>(`/api/finance/journal-entries/${id}/post`, {});
    return r.data;
  },
  reverseJournalEntry: async (id: string, reason: string): Promise<{ reversedEntryId: string; reversalEntryId: string; entryNumber: string }> => {
    const r = await api.post<{ reversedEntryId: string; reversalEntryId: string; entryNumber: string }>(`/api/finance/journal-entries/${id}/reverse`, { reason });
    return r.data;
  },
  // Sprint 40 (L67): Posting rules CRUD
  listPostingRules: async (): Promise<unknown[]> => {
    const r = await api.get<unknown[]>('/api/finance/posting-rules');
    return r.data;
  },
  getPostingRule: async (id: string): Promise<unknown> => {
    const r = await api.get<unknown>(`/api/finance/posting-rules/${id}`);
    return r.data;
  },
  createPostingRule: async (data: unknown): Promise<unknown> => {
    const r = await api.post<unknown>('/api/finance/posting-rules', data);
    return r.data;
  },
  updatePostingRule: async (id: string, data: unknown): Promise<unknown> => {
    const r = await api.put<unknown>(`/api/finance/posting-rules/${id}`, data);
    return r.data;
  },
  deletePostingRule: async (id: string): Promise<void> => {
    await api.delete(`/api/finance/posting-rules/${id}`);
  },
  // Sprint 40 (L67): Cost Centers CRUD
  listCostCenters: async (): Promise<unknown[]> => {
    const r = await api.get<unknown[]>('/api/cost-centers');
    return r.data;
  },
  getCostCenter: async (id: string): Promise<unknown> => {
    const r = await api.get<unknown>(`/api/cost-centers/${id}`);
    return r.data;
  },
  createCostCenter: async (data: unknown): Promise<unknown> => {
    const r = await api.post<unknown>('/api/cost-centers', data);
    return r.data;
  },
  updateCostCenter: async (id: string, data: unknown): Promise<unknown> => {
    const r = await api.put<unknown>(`/api/cost-centers/${id}`, data);
    return r.data;
  },
};

// Sprint 22: Reports module deleted. Complex report APIs (Trial Balance, P&L,
// Balance Sheet, Cash Flow, Top Customers/Vendors, Budget vs Actual, etc.) removed.
// Simple reports live in their parent module — e.g., Trial Balance at /api/finance/ledger/trial-balance.

// ============ Identity (User Management) API ============
// Phase 6.2: Admin user CRUD

export interface AdminUser { id: string; email: string; fullName: string; isActive: boolean; twoFactorEnabled: boolean; createdAt: string; updatedAt: string; lastLoginAt?: string; }
export interface AdminUserWithRoles { user: AdminUser; roleIds: string[]; companies: { userId: string; companyId: string; companyCode: string; companyName: string; isDefault: boolean; isHolding: boolean; assignedAt: string }[]; }
/** User → company mapping record. Matches backend UserCompanyLink. */
export interface UserCompany {
  userId: string;
  companyId: string;
  companyCode: string;
  companyName: string;
  isDefault: boolean;
  isHolding: boolean;
  assignedAt: string;
}
export interface RoleItem { id: string; name: string; description?: string; }
export interface CreateUserRequest { email: string; fullName: string; password: string; roleIds?: string[]; defaultCompanyId?: string; }
export interface UpdateUserRequest { fullName?: string; email?: string; isActive?: boolean; roleIds?: string[]; defaultCompanyId?: string; }
export interface ChangePasswordRequest { currentPassword: string; newPassword: string; }
export interface AssignUserToCompanyRequest { companyId: string; isDefault: boolean; }

export const identityApi = {
  listUsers: async (skip = 0, take = 50): Promise<{ count: number; items: AdminUser[] }> => {
    const r = await api.get<{ count: number; items: AdminUser[] }>('/api/identity/users', { params: { skip, take } });
    return r.data;
  },
  getUser: async (id: string): Promise<AdminUserWithRoles> => {
    const r = await api.get<AdminUserWithRoles>(`/api/identity/users/${id}`);
    return r.data;
  },
  createUser: async (data: CreateUserRequest): Promise<AdminUser> => {
    const r = await api.post<AdminUser>('/api/identity/users', data);
    return r.data;
  },
  updateUser: async (id: string, data: UpdateUserRequest): Promise<{ message: string }> => {
    const r = await api.put<{ message: string }>(`/api/identity/users/${id}`, data);
    return r.data;
  },
  resetPassword: async (id: string, newPassword: string): Promise<{ message: string }> => {
    const r = await api.put<{ message: string }>(`/api/identity/users/${id}/password`, { newPassword });
    return r.data;
  },
  deactivateUser: async (id: string): Promise<void> => {
    await api.delete(`/api/identity/users/${id}`);
  },
  listRoles: async (): Promise<RoleItem[]> => {
    const r = await api.get<RoleItem[]>('/api/identity/roles');
    return r.data;
  },
  // Per-user companies (Multi-Company mapping). Mirrors backend IUserRepository.GetUserCompaniesAsync.
  // Note: as of Phase 6.2 the RolesController does not expose a dedicated GET endpoint for this —
  // callers should fall back to `getUser(id).companies`. The endpoint is wired here for forward
  // compatibility and the user-management UI uses it when present.
  listUserCompanies: async (userId: string): Promise<UserCompany[]> => {
    const r = await api.get<UserCompany[]>(`/api/identity/users/${userId}/companies`);
    return r.data;
  },
  // POST /api/identity/users/{userId}/companies — assign user to a company.
  // Backend may 404 if endpoint is not yet wired (Phase 6.2). UI should handle gracefully.
  assignUserToCompany: async (userId: string, companyId: string, isDefault = false): Promise<{ message: string }> => {
    const r = await api.post<{ message: string }>(`/api/identity/users/${userId}/companies`, {
      companyId,
      isDefault,
    } as AssignUserToCompanyRequest);
    return r.data;
  },
  // DELETE /api/identity/users/{userId}/companies/{companyId} — remove user from a company.
  // Backend may 404 if endpoint is not yet wired (Phase 6.2). UI should handle gracefully.
  removeUserFromCompany: async (userId: string, companyId: string): Promise<void> => {
    await api.delete(`/api/identity/users/${userId}/companies/${companyId}`);
  },
  changePassword: async (data: ChangePasswordRequest): Promise<{ message: string }> => {
    const r = await api.post<{ message: string }>('/api/auth/change-password', data);
    return r.data;
  },
  // Forgot password (Phase 6)
  forgotPassword: async (email: string): Promise<{ message: string; devToken?: string; resetUrl?: string; expiresAt?: string }> => {
    const r = await api.post<{ message: string; devToken?: string; resetUrl?: string; expiresAt?: string }>('/api/auth/forgot-password', { email });
    return r.data;
  },
  resetPasswordWithToken: async (token: string, newPassword: string): Promise<{ message: string }> => {
    const r = await api.post<{ message: string }>('/api/auth/reset-password', { token, newPassword });
    return r.data;
  },
};

export const inventoryApi = {
  listItems: async (): Promise<Item[]> => {
    const r = await api.get<Item[]>('/api/inventory/items');
    return r.data;
  },
  // GET /api/inventory/items/{id}
  getItem: async (id: string): Promise<Item> => {
    const r = await api.get<Item>(`/api/inventory/items/${id}`);
    return r.data;
  },
  // POST /api/inventory/items
  createItem: async (data: Omit<Item, 'id' | 'companyId' | 'createdAt' | 'updatedAt'>): Promise<Item> => {
    const r = await api.post<Item>('/api/inventory/items', data);
    return r.data;
  },
  // PUT /api/inventory/items/{id}
  updateItem: async (id: string, data: Partial<Omit<Item, 'id' | 'companyId' | 'createdAt' | 'updatedAt'>>): Promise<Item> => {
    const r = await api.put<Item>(`/api/inventory/items/${id}`, data);
    return r.data;
  },
  // GET /api/inventory/categories — قائمة فئات الأصناف (لـ select في form)
  listCategories: async (): Promise<{ id: string; code: string; name: string; parentId?: string; isActive: boolean }[]> => {
    const r = await api.get<{ id: string; code: string; name: string; parentId?: string; isActive: boolean }[]>(`/api/inventory/categories`);
    return r.data;
  },
  // POST /api/inventory/categories — Sprint 40 (L67)
  createCategory: async (data: { code: string; name: string; parentId?: string; isActive?: boolean }): Promise<{ id: string }> => {
    const r = await api.post<{ id: string }>('/api/inventory/categories', data);
    return r.data;
  },
  // PUT /api/inventory/categories/{id} — Sprint 40 (L67)
  updateCategory: async (id: string, data: { code: string; name: string; parentId?: string; isActive?: boolean }): Promise<{ id: string }> => {
    const r = await api.put<{ id: string }>(`/api/inventory/categories/${id}`, data);
    return r.data;
  },
  // DELETE /api/inventory/categories/{id} — Sprint 40 (L67)
  deleteCategory: async (id: string): Promise<void> => {
    await api.delete(`/api/inventory/categories/${id}`);
  },
  // GET /api/inventory/units — قائمة وحدات القياس (UoM)
  listUnitsOfMeasure: async (): Promise<{ id: string; code: string; name: string; isActive: boolean }[]> => {
    const r = await api.get<{ id: string; code: string; name: string; isActive: boolean }[]>(`/api/inventory/units`);
    return r.data;
  },
  // GET /api/inventory/warehouses — المستودعات (لـ select في form)
  listWarehouses: async (): Promise<{ id: string; code: string; name: string; isActive: boolean }[]> => {
    const r = await api.get<{ id: string; code: string; name: string; isActive: boolean }[]>(`/api/inventory/warehouses`);
    return r.data;
  },
  // GET /api/inventory/reservations
  listReservations: async (): Promise<unknown[]> => {
    const r = await api.get<unknown[]>('/api/inventory/reservations');
    return r.data;
  },
  // GET /api/inventory/reservations/{id}
  getReservation: async (id: string): Promise<unknown> => {
    const r = await api.get<unknown>(`/api/inventory/reservations/${id}`);
    return r.data;
  },
  // POST /api/inventory/reservations
  createReservation: async (data: unknown): Promise<unknown> => {
    const r = await api.post<unknown>('/api/inventory/reservations', data);
    return r.data;
  },
  // PUT /api/inventory/reservations/{id}
  updateReservation: async (id: string, data: unknown): Promise<unknown> => {
    const r = await api.put<unknown>(`/api/inventory/reservations/${id}`, data);
    return r.data;
  },
  // DELETE /api/inventory/reservations/{id} — Sprint 40 (L67)
  deleteReservation: async (id: string): Promise<void> => {
    await api.delete(`/api/inventory/reservations/${id}`);
  },
  // GET /api/inventory/movements
  listMovements: async (): Promise<unknown[]> => {
    const r = await api.get<unknown[]>('/api/inventory/movements');
    return r.data;
  },
  // POST /api/inventory/movements
  createMovement: async (data: unknown): Promise<unknown> => {
    const r = await api.post<unknown>('/api/inventory/movements', data);
    return r.data;
  },
  // GET /api/inventory/items/{id}/stock — get stock level for an item
  getItemStock: async (itemId: string): Promise<unknown> => {
    const r = await api.get<unknown>(`/api/inventory/items/${itemId}/stock`);
    return r.data;
  },
  // Sprint 22: Notifications module deleted. listNotifications + getUnreadNotifications + markNotificationRead removed.
};

export const projectsApi = {
  listProjects: async (): Promise<Project[]> => {
    const r = await api.get<Project[]>('/api/projects');
    return r.data;
  },
  // Sprint 40 (L67): Project CRUD
  createProject: async (data: Partial<Project>): Promise<Project> => {
    const r = await api.post<Project>('/api/projects', data);
    return r.data;
  },
};

// Sprint 32 (DEC-112): Resources API
export const resourcesApi = {
  listResources: async (): Promise<Resource[]> => {
    const r = await api.get<Resource[]>('/api/resources');
    return r.data;
  },
  createResource: async (data: Partial<Resource>): Promise<Resource> => {
    const r = await api.post<Resource>('/api/resources', data);
    return r.data;
  },
};

// ============ Procurement API ============
// endpoints: /api/procurement/{vendors|pos|grs|bills}

export const procurementApi = {
  // ----- Vendors -----
  listVendors: async (): Promise<Vendor[]> => {
    const r = await api.get<Vendor[]>('/api/procurement/vendors');
    return r.data;
  },
  // GET /api/procurement/vendors/{id}
  getVendor: async (id: string): Promise<Vendor> => {
    const r = await api.get<Vendor>(`/api/procurement/vendors/${id}`);
    return r.data;
  },
  createVendor: async (data: Partial<Vendor>): Promise<Vendor> => {
    const r = await api.post<Vendor>('/api/procurement/vendors', data);
    return r.data;
  },

  // ----- Vendor Statement (Sprint 36, DEC-122) -----
  // كشـف حساب المورّد: رصيد افتتاحي + فواتير + مدفوعات + رصيد ختامي
  getVendorStatement: async (
    id: string,
    from?: string,
    to?: string
  ): Promise<VendorStatement> => {
    const r = await api.get<VendorStatement>(
      `/api/procurement/vendors/${id}/statement`,
      { params: { from, to } }
    );
    return r.data;
  },
  // PUT /api/procurement/vendors/{id}
  updateVendor: async (id: string, data: Partial<Omit<Vendor, 'id' | 'createdAt' | 'updatedAt'>>): Promise<Vendor> => {
    const r = await api.put<Vendor>(`/api/procurement/vendors/${id}`, data);
    return r.data;
  },

  // ----- Purchase Orders -----
  listPOs: async (): Promise<PurchaseOrder[]> => {
    const r = await api.get<PurchaseOrder[]>('/api/procurement/pos');
    return r.data;
  },
  getPO: async (id: string): Promise<PurchaseOrder> => {
    const r = await api.get<PurchaseOrder>(`/api/procurement/pos/${id}`);
    return r.data;
  },
  createPO: async (data: Partial<PurchaseOrder>): Promise<PurchaseOrder> => {
    const r = await api.post<PurchaseOrder>('/api/procurement/pos', data);
    return r.data;
  },

  // ----- Goods Receipts -----
  listGRs: async (): Promise<GoodsReceipt[]> => {
    const r = await api.get<GoodsReceipt[]>('/api/procurement/grs');
    return r.data;
  },
  getGR: async (id: string): Promise<GoodsReceipt> => { // DEC-031
    const r = await api.get<GoodsReceipt>(`/api/procurement/grs/${id}`);
    return r.data;
  },
  createGR: async (data: Partial<GoodsReceipt>): Promise<GoodsReceipt> => {
    const r = await api.post<GoodsReceipt>('/api/procurement/grs', data);
    return r.data;
  },

  // ----- Vendor Bills -----
  listBills: async (): Promise<VendorBill[]> => {
    const r = await api.get<VendorBill[]>('/api/procurement/bills');
    return r.data;
  },
  createBill: async (data: Partial<VendorBill>): Promise<VendorBill> => {
    const r = await api.post<VendorBill>('/api/procurement/bills', data);
    return r.data;
  },
};

// ============ HR API ============
// endpoints: /api/hr/{employees|attendance|departments|leaves}

export const hrApi = {
  // ----- Departments -----
  listDepartments: async (): Promise<Department[]> => {
    const r = await api.get<Department[]>('/api/hr/departments');
    return r.data;
  },

  // ----- Employees -----
  listEmployees: async (): Promise<Employee[]> => {
    const r = await api.get<Employee[]>('/api/hr/employees');
    return r.data;
  },
  // GET /api/hr/employees/{id}
  getEmployee: async (id: string): Promise<Employee> => {
    const r = await api.get<Employee>(`/api/hr/employees/${id}`);
    return r.data;
  },
  createEmployee: async (data: Partial<Employee>): Promise<Employee> => {
    const r = await api.post<Employee>('/api/hr/employees', data);
    return r.data;
  },
  // PUT /api/hr/employees/{id}
  updateEmployee: async (id: string, data: Partial<Omit<Employee, 'id' | 'createdAt' | 'departmentName'>>): Promise<Employee> => {
    const r = await api.put<Employee>(`/api/hr/employees/${id}`, data);
    return r.data;
  },

  // ----- Attendance -----
  listAttendance: async (params?: { employeeId?: string; from?: string; to?: string }): Promise<AttendanceRecord[]> => {
    const r = await api.get<AttendanceRecord[]>('/api/hr/attendance', { params });
    return r.data;
  },
  // CheckIn/CheckOut — body: { employeeId, type: 1|2 }
  recordAttendance: async (data: { employeeId: string; type: number; notes?: string }): Promise<AttendanceRecord> => {
    const r = await api.post<AttendanceRecord>('/api/hr/attendance', data);
    return r.data;
  },

  // ----- Leaves -----
  listLeaves: async (): Promise<LeaveRequest[]> => {
    const r = await api.get<LeaveRequest[]>('/api/hr/leaves');
    return r.data;
  },
  createLeave: async (data: Partial<LeaveRequest>): Promise<LeaveRequest> => {
    const r = await api.post<LeaveRequest>('/api/hr/leaves', data);
    return r.data;
  },
  approveLeave: async (id: string): Promise<LeaveRequest> => {
    const r = await api.put<LeaveRequest>(`/api/hr/leaves/${id}/approve`);
    return r.data;
  },
  rejectLeave: async (id: string): Promise<LeaveRequest> => {
    const r = await api.put<LeaveRequest>(`/api/hr/leaves/${id}/reject`);
    return r.data;
  },

  // ----- Payroll (Phase 4) -----
  // endpoints: /api/hr/payroll/{runs|runs/{id}|runs/{id}/{process|post|items}|eos/{empId}}
  payroll: {
    // قائمة دورات الرواتب للـ tenant (مع filter اختياري على الحالة).
    listPayrollRuns: async (params?: { status?: number }): Promise<PayrollRun[]> => {
      const r = await api.get<PayrollRun[]>('/api/hr/payroll/runs', { params });
      return r.data;
    },
    // تفاصيل دورة رواتب واحدة (Run header).
    getPayrollRun: async (id: string): Promise<PayrollRun> => {
      const r = await api.get<PayrollRun>(`/api/hr/payroll/runs/${id}`);
      return r.data;
    },
    // إنشاء دورة رواتب جديدة (Draft).
    createPayrollRun: async (data: CreatePayrollRunRequest): Promise<PayrollRun> => {
      const r = await api.post<PayrollRun>('/api/hr/payroll/runs', data);
      return r.data;
    },
    // معالجة الدورة: يحسب payslip لكل موظف نشط.
    processPayrollRun: async (id: string): Promise<PayrollRun> => {
      const r = await api.post<PayrollRun>(`/api/hr/payroll/runs/${id}/process`);
      return r.data;
    },
    // ترحيل الدورة: ينشئ JournalEntry ويحدّث الحالة إلى Posted.
    postPayrollRun: async (id: string): Promise<PayrollRun> => {
      const r = await api.post<PayrollRun>(`/api/hr/payroll/runs/${id}/post`);
      return r.data;
    },
    // قائمة payslips الدورة.
    getPayrollRunItems: async (runId: string): Promise<PayrollItem[]> => {
      const r = await api.get<PayrollItem[]>(`/api/hr/payroll/runs/${runId}/items`);
      return r.data;
    },
    // تفاصيل payslip موظف واحد ضمن الدورة.
    getPayslip: async (runId: string, employeeId: string): Promise<Payslip> => {
      const r = await api.get<Payslip>(`/api/hr/payroll/runs/${runId}/items/${employeeId}/payslip`);
      return r.data;
    },
    // حساب مستحقات نهاية الخدمة (EOS) لموظف.
    getEos: async (employeeId: string, terminationDate?: string): Promise<EosResponse> => {
      const r = await api.get<EosResponse>(`/api/hr/payroll/eos/${employeeId}`, {
        params: terminationDate ? { terminationDate } : undefined,
      });
      return r.data;
    },
  },
};

// ============ Dashboard (Holding-level KPIs) ============
// Sprint 1: replaces the per-company dashboard with a Holding-level summary.
// Contract: GET /api/dashboard/summary — returns 4 counts for the active company
// (or, when the active company is the Holding, aggregated across all sub-companies).
// Backend is being built in parallel on the same branch; 404 in dev is expected
// until the endpoint is wired. The page handles the 404 with an error state.

export interface DashboardSummary {
  /** عدد الشركات ضمن القابضة (Holding) أو 1 لو الطلب ليس holding-scoped. */
  companies: number;
  /** عدد المستخدمين النشطين المرتبطين بالشركة الحالية (أو القابضة). */
  users: number;
  /** عدد النشاطات (journal posted, invoices, POs...) اليوم. */
  activities_today: number;
  /** إجمالي المعاملات المالية (sales + purchases + journals) في آخر 30 يوم. */
  transactions: number;
  /** معرّف الشركة/الـ scope اللي تم حساب الـ summary عليه — للتوضيح في الـ UI. */
  scopeCompanyId?: string;
  /** ISO timestamp للـ snapshot. */
  asOf?: string;
  // Sprint 5: optional trend fields (vs previous month). The BE summary
  // endpoint may or may not include them; the FE tolerates their absence.
  /** Percent change in `companies` vs the previous period. */
  companiesTrendPct?: number;
  /** Percent change in `users` vs the previous period. */
  usersTrendPct?: number;
  /** Percent change in `activities_today` vs the previous period. */
  activitiesTrendPct?: number;
  /** Percent change in `transactions` vs the previous period. */
  transactionsTrendPct?: number;
}

// ============ Sprint 5: Dashboard Chart DTOs (Phase 4.1) ============
// Mirror the C# classes in
// src/backend/Modules/Dashboard/Application/DTOs/ChartDtos.cs exactly. The FE
// drops the responses straight into Recharts without any further transform.

/** One month-bucket for the revenue-vs-expense line chart. */
export interface RevenueVsExpensePoint {
  /** ISO yyyy-MM string, UTC (e.g. "2026-02"). Sortable + locale-independent. */
  month: string;
  /** Absolute LYD revenue for the month (≥ 0). */
  revenue: number;
  /** Absolute LYD expense for the month (≥ 0). */
  expense: number;
  /** revenue - expense (positive = profit, negative = loss). */
  net: number;
}

/** One expense-category slice for the pie / donut chart. */
export interface ExpenseCategorySlice {
  /** Account name (e.g. "Rent Expense"). */
  category: string;
  /** Absolute LYD amount for the slice. */
  amount: number;
  /** Stable palette index color from BE (CSS hex). */
  color: string;
}

/** One row for the top-customers bar chart. */
export interface TopCustomerChartRow {
  customerId: string;
  customerName: string;
  totalSpent: number;
  invoiceCount: number;
}

export const dashboardApi = {
  // GET /api/dashboard/summary — Holding-level KPIs (4 counts).
  // Active company comes from X-Company-Id header (set by the axios interceptor).
  getSummary: async (): Promise<DashboardSummary> => {
    const r = await api.get<DashboardSummary>('/api/dashboard/summary');
    return r.data;
  },
  // Sprint 5 (T1): GET /api/dashboard/charts/revenue?months=6 — line chart.
  getRevenueChart: async (months = 6): Promise<RevenueVsExpensePoint[]> => {
    const r = await api.get<RevenueVsExpensePoint[]>('/api/dashboard/charts/revenue', {
      params: { months },
    });
    return r.data;
  },
  // Sprint 5 (T2): GET /api/dashboard/charts/expenses-by-category?months=3 — pie.
  getExpenseByCategory: async (months = 3): Promise<ExpenseCategorySlice[]> => {
    const r = await api.get<ExpenseCategorySlice[]>('/api/dashboard/charts/expenses-by-category', {
      params: { months },
    });
    return r.data;
  },
  // Sprint 5 (T3): GET /api/dashboard/charts/top-customers?limit=5 — bar.
  getTopCustomers: async (limit = 5): Promise<TopCustomerChartRow[]> => {
    const r = await api.get<TopCustomerChartRow[]>('/api/dashboard/charts/top-customers', {
      params: { limit },
    });
    return r.data;
  },
};

// ============ Activity Feed API (Sprint 3 — T1) ============
// Recent user actions (LOGIN, LOGOUT, REGISTER, COMPANY_SWITCH, PASSWORD_CHANGE, ...).
// The backend reads from the `activity_log` table (created in Cycle 6 / DEC-073).
// Contract: GET /api/activity/recent?limit=20 — returns ActivityItem[] DESC by timestamp.

export interface ActivityItem {
  /** bigint id as string (Postgres bigint can overflow JS number). */
  id: string;
  userId?: string | null;
  userName?: string | null;
  /** Action constant, e.g. "LOGIN_SUCCESS", "REGISTER", "COMPANY_SWITCH". */
  action: string;
  /** Optional entity type the action refers to (e.g. "company", "user"). */
  entityType?: string | null;
  entityId?: string | null;
  /** ISO 8601 timestamp. */
  timestamp: string;
  /** Free-form metadata (e.g. login failure reason). */
  metadata?: Record<string, unknown> | null;
  ipAddress?: string | null;
}

// Sprint 22: Activity module deleted. activityApi removed.

// ============ Holdings API ============
// Sprint 1: Holding detail view. Lists a Holding + its sub-companies.
// Contract: GET /api/holdings/{slug} — returns the holding by URL slug.
// The holding's `companies` array is the source of truth for the sub-companies
// dropdown on the Holding page (a separate, simpler view than /admin/companies).

export interface HoldingCompany {
  id: string;
  code: string;
  name: string;
  /** هل هي الشركة القابضة نفسها (false في العادة للـ sub-companies) */
  isHolding: boolean;
  isActive: boolean;
  currency: string;
  country?: string;
  createdAt: string;
}

export interface HoldingDetail {
  id: string;
  /** الـ URL-friendly slug — مثال "mfa-holding" */
  slug: string;
  name: string;
  legalName?: string;
  taxNumber?: string;
  baseCurrency: string;
  country?: string;
  /** قائمة الشركات الفرعية (لا تشمل القابضة نفسها) */
  companies: HoldingCompany[];
  createdAt: string;
}

export const holdingsApi = {
  // GET /api/holdings/{slug} — Holding detail + sub-companies.
  getBySlug: async (slug: string): Promise<HoldingDetail> => {
    const r = await api.get<HoldingDetail>(`/api/holdings/${encodeURIComponent(slug)}`);
    return r.data;
  },
};

// ============ Companies API (Sprint 2 — T1, T3, T7, T8) ============
// Phase 6 Multi-Company: companies are the top-level scope. The /api/companies
// endpoint is being upgraded to support pagination (T1) and a richer DTO
// (parentCompanyId, baseCurrency, legalName). This block is the canonical
// frontend client for company CRUD — used by /admin/companies (T7) and
// /admin/companies/[id] (T8).
//
// T1: GET /api/companies?page=N&pageSize=20  — paginated list
// T3: POST /api/companies                     — create
// PUT: PUT /api/companies/{id}                — update (T8 edit form)
// PUT: PUT /api/companies/{id}/activate        — activate/deactivate

export interface Company {
  id: string;
  code: string;
  name: string;
  /** الاسم القانوني — قد يختلف عن الاسم التجاري */
  legalName?: string;
  /** الرقم الضريبي — اختياري */
  taxNumber?: string;
  /** العملة الأساسية (ISO 4217 — e.g. "LYD", "USD") */
  baseCurrency: string;
  /** البلد — اختياري (ISO 3166-1 alpha-2 أو اسم حر) */
  country?: string;
  isHolding: boolean;
  isActive: boolean;
  /** معرّف الشركة الأم (للشركات الفرعية في الـ holding tree) */
  parentCompanyId?: string | null;
  /** اسم الشركة الأم — للعرض في الجدول بدون استعلام إضافي */
  parentCompanyName?: string | null;
  createdAt: string;
  updatedAt?: string;
}

/** شكل الرد للـ GET /api/companies?page=N&pageSize=20 (T1).
 *  الـ backend قد يطابق `PaginationResponse<T>` من Shared layer أو يُرجع شكلاً
 *  أبسط. كلا الشكلين مدعومان هنا لتجنّب كسر الـ build قبل جاهزية T1. */
export interface PagedCompanies {
  items: Company[];
  total: number;
  page: number;
  pageSize: number;
}

export interface CreateCompanyRequest {
  code: string;
  name: string;
  legalName?: string;
  taxNumber?: string;
  baseCurrency: string;
  country?: string;
  isHolding?: boolean;
  isActive?: boolean;
  parentCompanyId?: string | null;
}

export interface UpdateCompanyRequest {
  code?: string;
  name?: string;
  legalName?: string;
  taxNumber?: string;
  baseCurrency?: string;
  country?: string;
  isHolding?: boolean;
  isActive?: boolean;
  parentCompanyId?: string | null;
}

export const companiesApi = {
  // T1: GET /api/companies?page=N&pageSize=20
  // يقبل أيضاً `includeInactive=true` للـ admin view (يعرض المعطّلة).
  // يلتقط شكل الرد المتوقَّع `{ items, total, page, pageSize }` — لو الـ backend
  // ما يزال يُرجع مصفوفة بسيطة (Phase 6.1b) نلفّها في شكل موحّد.
  list: async (params?: { page?: number; pageSize?: number; includeInactive?: boolean; search?: string }): Promise<PagedCompanies> => {
    const r = await api.get<Company[] | PagedCompanies>('/api/companies', {
      params: {
        page: params?.page,
        pageSize: params?.pageSize,
        includeInactive: params?.includeInactive,
        search: params?.search,
      },
    });
    if (Array.isArray(r.data)) {
      // الـ backend لم يُحدَّث بعد — رجّع كل العناصر في صفحة واحدة.
      return {
        items: r.data,
        total: r.data.length,
        page: 1,
        pageSize: r.data.length,
      };
    }
    return r.data;
  },
  // GET /api/companies/{id} — T8 details
  get: async (id: string): Promise<Company> => {
    const r = await api.get<Company>(`/api/companies/${id}`);
    return r.data;
  },
  // T3: POST /api/companies — create
  create: async (data: CreateCompanyRequest): Promise<Company> => {
    const r = await api.post<Company>('/api/companies', data);
    return r.data;
  },
  // T8 edit: PUT /api/companies/{id}
  // الـ backend لم يُضِف PUT بعد (T3 يستخدم POST لكل من create/update في
  // بعض الـ designs). نُحاول PUT أولاً، ونُسقط إلى POST إن فشل.
  update: async (id: string, data: UpdateCompanyRequest): Promise<Company> => {
    try {
      const r = await api.put<Company>(`/api/companies/${id}`, data);
      return r.data;
    } catch (e: unknown) {
      const err = e as { response?: { status?: number } };
      if (err?.response?.status === 405 || err?.response?.status === 404) {
        // Fallback: الـ backend يستخدم POST بدون id = create-only. نُعيد رمي
        // الخطأ الأصلي لأن الـ edit غير مدعوم في تلك الحالة.
      }
      throw e;
    }
  },
  // PUT /api/companies/{id}/activate — تفعيل/تعطيل (يُسهِّل workflow T8)
  setActive: async (id: string, isActive: boolean): Promise<{ message: string }> => {
    const r = await api.put<{ message: string }>(`/api/companies/${id}/activate`, { isActive });
    return r.data;
  },
};

// ============ Users API (Sprint 2 — T4, T5, T9, T10) ============
// T4: GET /api/users?company_id={id}&skip=N&take=M  — list with company filter
// T5: GET /api/users/{id}/companies                  — user's assigned companies
//
// هذا الـ namespace الجديد مُكمِّل لـ `identityApi` الموجود (الـ auth flow +
// user CRUD). هنا نُركِّز على الـ read paths اللازمة لـ admin screens.
// ملاحظة: `identityApi.listUserCompanies(userId)` و `getUser(userId)` لا تزال
// تعمل — الـ frontend يُفضِّل `usersApi` للـ admin views.

export interface PagedUsers {
  items: AdminUser[];
  total: number;
  skip: number;
  take: number;
}

export const usersApi = {
  // T4: GET /api/users?company_id=&skip=&take=
  list: async (params?: { companyId?: string; skip?: number; take?: number }): Promise<PagedUsers> => {
    const r = await api.get<AdminUser[] | PagedUsers>('/api/users', {
      params: {
        company_id: params?.companyId,
        skip: params?.skip ?? 0,
        take: params?.take ?? 50,
      },
    });
    if (Array.isArray(r.data)) {
      return { items: r.data, total: r.data.length, skip: params?.skip ?? 0, take: params?.take ?? 50 };
    }
    return r.data;
  },
  // T5: GET /api/users/{id}/companies
  getUserCompanies: async (userId: string): Promise<UserCompany[]> => {
    const r = await api.get<UserCompany[]>(`/api/users/${userId}/companies`);
    return r.data;
  },
};

// Sprint 22: Search module deleted. searchApi + types + helper removed.
// (Was: Sprint 5 Phase 5.1 global search across customers/suppliers/invoices/accounts)

// ============ Sprint 5: Dashboard Charts (Phase 4.1) ============
// Chart DTOs + dashboardApi methods are defined in the "Dashboard
// (Holding-level KPIs)" section above (the original Sprint 1 section was
// extended in place). See:
//   - interface RevenueVsExpensePoint
//   - interface ExpenseCategorySlice
//   - interface TopCustomerChartRow
//   - dashboardApi.getRevenueChart / getExpenseByCategory / getTopCustomers
//
// This trailing block intentionally left blank — kept as a section divider
// so future readers can grep for "Sprint 5: Dashboard Charts" and jump here.

// ============ Sprint 11: Full Demo Coverage (T1, FE Jimi) ============
//
// These typed wrappers cover the new demo pages added in Sprint 11:
//   - /holding       → getHoldingDashboard (consolidated KPIs)
//   - /admin/companies → getCompanyTree (hierarchical view)
//   - /accounts      → getAccounts (CoA in the new DTO shape)
//   - /transactions  → getRecentTransactions (journal feed)
//   - /reports       → getReports (saved reports list)
//
// The DTOs are defined in `api-types.ts` (FE is the source of truth per
// Sprint 11 hand-off). The endpoints are wired by the BE worker on the
// parallel branch. If the BE endpoint is not ready yet, the wrappers
// gracefully fall through to the next attempt.
//
// Backed by: `src/frontend/lib/api-types.ts` (Sprint 11 T1).

import type {
  CompanyTreeNode,
  HoldingDashboard,
  AccountDto,
  TransactionDto,
  ReportDto,
  SubsidiaryListDto,
  ActivityFeedItemDto,
  NotificationDto,
} from './api-types';

/** GET /api/holdings/dashboard — consolidated KPIs across the holding.
 *  Falls back to /api/dashboard/holding if the BE uses the alternative route. */
export async function getHoldingDashboard(): Promise<HoldingDashboard> {
  try {
    const r = await api.get<HoldingDashboard>('/api/holdings/dashboard');
    return r.data;
  } catch (e: unknown) {
    const err = e as { response?: { status?: number } };
    if (err?.response?.status === 404) {
      // Try the alternative route the BE might use.
      const r2 = await api.get<HoldingDashboard>('/api/dashboard/holding');
      return r2.data;
    }
    throw e;
  }
}

/** GET /api/companies/tree — hierarchical company tree.
 *  Returns the root nodes (children of the holding). */
export async function getCompanyTree(): Promise<CompanyTreeNode[]> {
  const r = await api.get<CompanyTreeNode[] | { children: CompanyTreeNode[] }>(
    '/api/companies/tree'
  );
  if (Array.isArray(r.data)) return r.data;
  // Defensive: the BE may wrap the list in { children: [...] }.
  return r.data?.children ?? [];
}

/** GET /api/accounts — flat Chart of Accounts (new Sprint 11 DTO).
 *  Note: this is distinct from `financeApi.listAccounts()` which returns
 *  the legacy `Account` shape (numeric enums for type/normalBalance). */
export async function getAccounts(): Promise<AccountDto[]> {
  const r = await api.get<AccountDto[]>('/api/accounts');
  return r.data;
}

// Sprint 22: getRecentTransactions + getReports + getActivityFeed + getUnreadNotifications removed (Reports/Activity/Notifications modules deleted).

/** GET /api/companies/{id}/subsidiaries — children of a specific company. */
export async function getSubsidiaries(companyId: string): Promise<SubsidiaryListDto> {
  const r = await api.get<SubsidiaryListDto>(
    `/api/companies/${encodeURIComponent(companyId)}/subsidiaries`
  );
  return r.data;
}


