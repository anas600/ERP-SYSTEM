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
  createdAt: string;
  updatedAt: string;
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

// ============ Reports ============
export interface TrialBalanceRow {
  accountId: string;
  accountCode: string;
  accountName: string;
  accountType: number;
  debit: number;
  credit: number;
}

export interface TrialBalanceReport {
  asOfDate: string;
  rows: TrialBalanceRow[];
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
//
// Sprint 4 (T4): نوفر دالتين:
//   - getErrorMessage: تستخرج string واحد (AR عادةً) — موجودة للتوافق.
//   - getBilingualErrorMessage: تُرجع { ar, en } من lib/errors.ts dictionary.
//     الـ pages الجديدة تستعملها لعرض رسالة ثنائية اللغة.
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

// Re-export the bilingual helpers from `lib/errors.ts` for convenience.
export { mapApiError, getBilingualError, formatBilingual } from './errors';
export type { BilingualError } from './errors';

/**
 * variant جاهز للاستخدام في الـ pages: يرجّع BilingualError مع fallback محدد.
 *
 *   const msg = getBilingualErrorMessage(err, 'فشل تحميل القائمة', 'Failed to load list');
 *   toast.error(formatBilingual(msg));
 *   <ErrorState message={msg} />
 */
export function getBilingualErrorMessage(
  e: unknown,
  fallbackAr?: string,
  fallbackEn?: string
): import('./errors').BilingualError {
  // Lazy-import to keep api.ts surface clean and avoid circular references.
  const { getBilingualError } = require('./errors') as typeof import('./errors');
  return getBilingualError(e, fallbackAr || fallbackEn ? { ar: fallbackAr ?? '', en: fallbackEn ?? '' } : undefined);
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

export const financeApi = {
  listAccounts: async (): Promise<Account[]> => {
    const r = await api.get<Account[]>('/api/finance/accounts');
    return r.data;
  },
  createAccount: async (data: Partial<Account>): Promise<Account> => {
    const r = await api.post<Account>('/api/finance/accounts', data);
    return r.data;
  },
  trialBalance: async (asOfDate: string): Promise<TrialBalanceReport> => {
    const r = await api.get<TrialBalanceReport>('/api/reports/finance/trial-balance', {
      params: { asOfDate },
    });
    return r.data;
  },
};

// ============ 20 Mandatory Accounting Reports API ============
// Phase 6.2: Added on top of the new Multi-Company architecture

// DTOs matching the backend (C#)
export interface TrialBalanceRow { accountId: string; accountCode: string; accountName: string; accountType: number; debit: number; credit: number; }
export interface TrialBalanceReport { asOfDate: string; rows: TrialBalanceRow[]; totalDebit: number; totalCredit: number; isBalanced: boolean; }
export interface IncomeStatement { from: string; to: string; revenue: number; cogs: number; operatingExpenses: number; otherIncome: number; otherExpenses: number; netIncome: number; }
export interface BalanceSheet { asOfDate: string; totalAssets: number; totalLiabilities: number; totalEquity: number; isBalanced: boolean; }
export interface CashFlowReport { from: string; to: string; operatingActivities: number; investingActivities: number; financingActivities: number; netCashFlow: number; }
export interface JournalEntryLineDto { journalEntryId: string; entryNumber: string; entryDate: string; description: string; reference: string; totalDebit: number; totalCredit: number; status: number; postedAt?: string; }
export interface JournalEntryReport { from?: string; to?: string; status?: number; totalEntries: number; totalDebit: number; totalCredit: number; lines: JournalEntryLineDto[]; }
export interface AccountActivityTransaction { journalLineId: string; entryDate: string; entryNumber: string; reference: string; description: string; debit: number; credit: number; }
export interface AccountActivityResponse { accountId: string; accountCode: string; accountName: string; normalBalance: number; from?: string; to?: string; openingBalance: number; periodDebit: number; periodCredit: number; closingBalance: number; transactions: AccountActivityTransaction[]; }
export interface CollectionsRow { receiptId: string; receiptNumber: string; receiptDate: string; customerCode: string; customerName: string; paymentMethod: string; amount: number; currency: string; notes: string; }
export interface CollectionsReport { from?: string; to?: string; totalAmount: number; count: number; rows: CollectionsRow[]; }
export interface CostCenterPerformanceRow { costCenterId: string; costCenterCode: string; costCenterName: string; revenue: number; expense: number; net: number; margin: number; }
export interface CostCenterPerformanceReport { from?: string; to?: string; totalRevenue: number; totalExpense: number; totalNet: number; rows: CostCenterPerformanceRow[]; }
export interface VatReport { from: string; to: string; vatRate: number; totalSales: number; outputVat: number; totalPurchases: number; inputVat: number; netVatPayable: number; details: Record<string, unknown>; }
export interface SalesByCustomerRow { customerId: string; customerCode: string; customerName: string; invoiceCount: number; subtotal: number; taxAmount: number; totalAmount: number; paidAmount: number; outstanding: number; }
export interface SalesByCustomerReport { from: string; to: string; grandTotal: number; grandOutstanding: number; rows: SalesByCustomerRow[]; }
export interface SalesByItemRow { itemId: string; sku: string; itemName: string; quantity: number; subtotal: number; taxAmount: number; totalAmount: number; }
export interface SalesByItemReport { from: string; to: string; grandTotal: number; rows: SalesByItemRow[]; }
export interface PurchasesByVendorRow { vendorId: string; vendorCode: string; vendorName: string; billCount: number; subtotal: number; taxAmount: number; totalAmount: number; paidAmount: number; outstanding: number; }
export interface PurchasesByVendorReport { from: string; to: string; grandTotal: number; grandOutstanding: number; rows: PurchasesByVendorRow[]; }
export interface TopCustomerRow { rank: number; customerId: string; customerCode: string; customerName: string; totalAmount: number; invoiceCount: number; }
export interface TopCustomersReport { from: string; to: string; limit: number; rows: TopCustomerRow[]; }
export interface TopVendorRow { rank: number; vendorId: string; vendorCode: string; vendorName: string; totalAmount: number; billCount: number; }
export interface TopVendorsReport { from: string; to: string; limit: number; rows: TopVendorRow[]; }
export interface BudgetVsActualRow { projectId: string; projectName: string; budget: number; actual: number; variance: number; variancePercent: number; }
export interface BudgetVsActualReport { projectId?: string; from: string; to: string; totalBudget: number; totalActual: number; totalVariance: number; totalVariancePercent: number; rows: BudgetVsActualRow[]; }

export const reportsApi = {
  // Report 1: Trial Balance
  trialBalance: async (asOf: string): Promise<TrialBalanceReport> => {
    const r = await api.get<TrialBalanceReport>('/api/finance/reports/trial-balance', { params: { asOf } });
    return r.data;
  },
  // Report 2: Income Statement
  incomeStatement: async (from: string, to: string): Promise<IncomeStatement> => {
    const r = await api.get<IncomeStatement>('/api/finance/reports/income-statement', { params: { from, to } });
    return r.data;
  },
  // Report 3: Balance Sheet
  balanceSheet: async (asOf: string): Promise<BalanceSheet> => {
    const r = await api.get<BalanceSheet>('/api/finance/reports/balance-sheet', { params: { asOf } });
    return r.data;
  },
  // Report 4: Cash Flow
  cashFlow: async (from: string, to: string): Promise<CashFlowReport> => {
    const r = await api.get<CashFlowReport>('/api/finance/reports/cash-flow', { params: { from, to } });
    return r.data;
  },
  // Report 5: General Ledger
  generalLedger: async (accountId: string, from?: string, to?: string): Promise<AccountActivityResponse> => {
    const r = await api.get<AccountActivityResponse>('/api/finance/reports/general-ledger', { params: { accountId, from, to } });
    return r.data;
  },
  // Report 6: Journal Entries
  journalEntries: async (from?: string, to?: string, status?: number, skip = 0, take = 100): Promise<JournalEntryReport> => {
    const r = await api.get<JournalEntryReport>('/api/finance/reports/journal-entries', { params: { from, to, status, skip, take } });
    return r.data;
  },
  // Report 7: Account Activity
  accountActivity: async (accountId: string, from?: string, to?: string): Promise<AccountActivityResponse> => {
    const r = await api.get<AccountActivityResponse>('/api/finance/reports/account-activity', { params: { accountId, from, to } });
    return r.data;
  },
  // Report 10: AP Aging
  apAging: async (asOf: string): Promise<{ asOfDate: string; vendors: { vendorCode: string; vendorName: string; current: number; days31To60: number; days61To90: number; days91Plus: number; total: number }[]; totalCurrent: number; total31To60: number; total61To90: number; total91Plus: number; grandTotal: number; }> => {
    const r = await api.get<{ asOfDate: string; vendors: { vendorCode: string; vendorName: string; current: number; days31To60: number; days61To90: number; days91Plus: number; total: number }[]; totalCurrent: number; total31To60: number; total61To90: number; total91Plus: number; grandTotal: number; }>('/api/finance/reports/ap-aging', { params: { asOf } });
    return r.data;
  },
  // Report 11: Collections
  collections: async (from?: string, to?: string): Promise<CollectionsReport> => {
    const r = await api.get<CollectionsReport>('/api/finance/reports/collections', { params: { from, to } });
    return r.data;
  },
  // Report 12: Sales by Customer
  salesByCustomer: async (from: string, to: string): Promise<SalesByCustomerReport> => {
    const r = await api.get<SalesByCustomerReport>('/api/ar/reports/sales-by-customer', { params: { from, to } });
    return r.data;
  },
  // Report 13: Sales by Item
  salesByItem: async (from: string, to: string): Promise<SalesByItemReport> => {
    const r = await api.get<SalesByItemReport>('/api/ar/reports/sales-by-item', { params: { from, to } });
    return r.data;
  },
  // Report 14: Purchases by Vendor
  purchasesByVendor: async (from: string, to: string): Promise<PurchasesByVendorReport> => {
    const r = await api.get<PurchasesByVendorReport>('/api/procurement/reports/purchases-by-vendor', { params: { from, to } });
    return r.data;
  },
  // Report 15: Top Customers
  topCustomers: async (from: string, to: string, limit = 10): Promise<TopCustomersReport> => {
    const r = await api.get<TopCustomersReport>('/api/ar/reports/top-customers', { params: { from, to, limit } });
    return r.data;
  },
  // Report 15: Top Vendors
  topVendors: async (from: string, to: string, limit = 10): Promise<TopVendorsReport> => {
    const r = await api.get<TopVendorsReport>('/api/procurement/reports/top-vendors', { params: { from, to, limit } });
    return r.data;
  },
  // Report 16: Cost Center Performance
  costCenterPerformance: async (from?: string, to?: string): Promise<CostCenterPerformanceReport> => {
    const r = await api.get<CostCenterPerformanceReport>('/api/finance/reports/cost-center-performance', { params: { from, to } });
    return r.data;
  },
  // Report 17/18: Project P&L + Budget vs Actual (all projects)
  projectBudgetVsActual: async (projectId?: string, from?: string, to?: string): Promise<BudgetVsActualReport> => {
    const r = await api.get<BudgetVsActualReport>('/api/reports/projects/budget-vs-actual', { params: { projectId, from, to } });
    return r.data;
  },
  // Report 19: VAT
  vat: async (from: string, to: string): Promise<VatReport> => {
    const r = await api.get<VatReport>('/api/finance/reports/vat', { params: { from, to } });
    return r.data;
  },
  // Report 20: Inventory Valuation
  inventoryValuation: async (): Promise<{ count: number; totalValue: number; items: { itemId: string; itemSku: string; itemName: string; warehouseId: string; warehouseName: string; quantityOnHand: number; averageCost: number; totalValue: number }[] }> => {
    const r = await api.get<{ count: number; totalValue: number; items: { itemId: string; itemSku: string; itemName: string; warehouseId: string; warehouseName: string; quantityOnHand: number; averageCost: number; totalValue: number }[] }>('/api/reports/inventory/valuation');
    return r.data;
  },
};

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
  // GET /api/inventory/units — قائمة وحدات القياس (UoM)
  listUnitsOfMeasure: async (): Promise<{ id: string; code: string; name: string; isActive: boolean }[]> => {
    const r = await api.get<{ id: string; code: string; name: string; isActive: boolean }[]>(`/api/inventory/units`);
    return r.data;
  },
  // ----- Notifications (Cycle 8 / DEC-073) -----
  // GET /api/inventory/notifications — user notifications (paginated)
  listNotifications: async (params?: { unreadOnly?: boolean; skip?: number; take?: number }): Promise<Notification[]> => {
    const qs = new URLSearchParams();
    if (params?.unreadOnly) qs.set('unreadOnly', 'true');
    if (params?.skip != null) qs.set('skip', String(params.skip));
    if (params?.take != null) qs.set('take', String(params.take));
    const url = `/api/inventory/notifications${qs.toString() ? '?' + qs.toString() : ''}`;
    const r = await api.get<Notification[]>(url);
    return r.data;
  },
  // GET /api/inventory/notifications/unread — unread + count
  getUnreadNotifications: async (): Promise<{ count: number; items: Notification[] }> => {
    const r = await api.get<{ count: number; items: Notification[] }>(`/api/inventory/notifications/unread`);
    return r.data;
  },
  // POST /api/inventory/notifications/{id}/mark-read — mark as read
  markNotificationRead: async (id: string): Promise<void> => {
    await api.post(`/api/inventory/notifications/${id}/mark-read`);
  },
};

export const projectsApi = {
  listProjects: async (): Promise<Project[]> => {
    const r = await api.get<Project[]>('/api/projects');
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
}

export const dashboardApi = {
  // GET /api/dashboard/summary — Holding-level KPIs (4 counts).
  // Active company comes from X-Company-Id header (set by the axios interceptor).
  getSummary: async (): Promise<DashboardSummary> => {
    const r = await api.get<DashboardSummary>('/api/dashboard/summary');
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

export const activityApi = {
  // GET /api/activity/recent?limit=20 — recent activity feed
  // The backend may return either an array (Sprint 3 spec) or { items: [...] }
  // (defensive — mirrors the pattern in companiesApi.list).
  recent: async (limit = 20): Promise<ActivityItem[]> => {
    const r = await api.get<ActivityItem[] | { items?: ActivityItem[] }>(
      '/api/activity/recent',
      { params: { limit } }
    );
    if (Array.isArray(r.data)) return r.data;
    return r.data?.items ?? [];
  },
};

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
