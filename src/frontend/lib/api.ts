// API client للـ ERP-SYSTEM
// يستخدم localStorage لحفظ الـ token

import axios, { AxiosInstance } from 'axios';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'https://a59fdc3d90a895af-47-253-4-207.serveousercontent.com';

export const api: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
  timeout: 30000,
});

// Request interceptor: أضف JWT token تلقائياً
api.interceptors.request.use((config) => {
  if (typeof window !== 'undefined') {
    const token = localStorage.getItem('accessToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
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
        localStorage.removeItem('user');
        window.location.href = '/login';
      }
    }
    return Promise.reject(err);
  }
);

// ============ Types ============
// ملاحظة: الـ contracts تطابق AuthDtos.cs في الـ backend (C#).
//   - Register: TenantName يُنشئ tenant جديد + Subdomain يُحسب عبر Slugify
//   - Login:    TenantId (Guid) اختياري للبحث داخل tenant محدد

export interface RegisterRequest {
  email: string;
  password: string;
  fullName: string;
  tenantName: string;
  baseCurrency?: string;     // optional, default "LYD"
}

export interface LoginRequest {
  email: string;
  password: string;
  tenantId?: string;         // optional (Guid) — إن لم يُرسل، بحث شامل
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
  user: UserInfo;
  holdingCompanyId?: string;
}

export interface UserInfo {
  id: string;
  tenantId: string;
  email: string;
  fullName: string;
  roles: string[];
}

// ============ Finance ============
export interface Account {
  id: string;
  tenantId: string;
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
export interface Item {
  id: string;
  tenantId: string;
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
  tenantId: string;
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
  tenantId: string;
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
  tenantId: string;
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
  tenantId: string;
  grNumber: string;
  purchaseOrderId: string;
  poNumber?: string;
  vendorName?: string;
  vendorId?: string;
  status: number;
  receivedDate: string;
  warehouseId: string;
  warehouseName?: string;
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
  tenantId: string;
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
  tenantId: string;
  name: string;
  code: string;
  parentId?: string;
  managerId?: string;
  isActive: boolean;
}

export interface Employee {
  id: string;
  tenantId: string;
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
  tenantId: string;
  employeeId: string;
  employeeName?: string;
  type: number; // 1=CheckIn, 2=CheckOut
  timestamp: string;
  notes?: string;
}

export interface LeaveRequest {
  id: string;
  tenantId: string;
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
  tenantId: string;
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
  tenantId: string;
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

// ============ Error extraction helper ============
// للحصول على رسالة خطأ أنيقة من Axios errors
export interface ApiError {
  detail?: string;
  message?: string;
}

export function getErrorMessage(e: unknown, fallback = 'حدث خطأ غير متوقع'): string {
  const err = e as { response?: { data?: ApiError }; message?: string };
  return err?.response?.data?.detail || err?.response?.data?.message || err?.message || fallback;
}

// ============ API helpers ============

export const authApi = {
  register: async (data: RegisterRequest): Promise<AuthResponse> => {
    const r = await api.post<AuthResponse>('/api/auth/register', data);
    if (typeof window !== 'undefined') {
      localStorage.setItem('accessToken', r.data.accessToken);
      localStorage.setItem('refreshToken', r.data.refreshToken);
      localStorage.setItem('user', JSON.stringify(r.data.user));
    }
    return r.data;
  },
  login: async (data: LoginRequest): Promise<AuthResponse> => {
    const r = await api.post<AuthResponse>('/api/auth/login', data);
    if (typeof window !== 'undefined') {
      localStorage.setItem('accessToken', r.data.accessToken);
      localStorage.setItem('refreshToken', r.data.refreshToken);
      localStorage.setItem('user', JSON.stringify(r.data.user));
    }
    return r.data;
  },
  logout: () => {
    if (typeof window !== 'undefined') {
      localStorage.removeItem('accessToken');
      localStorage.removeItem('refreshToken');
      localStorage.removeItem('user');
    }
  },
  me: async (): Promise<UserInfo> => {
    const r = await api.get<UserInfo>('/api/auth/me');
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

export const inventoryApi = {
  listItems: async (): Promise<Item[]> => {
    const r = await api.get<Item[]>('/api/inventory/items');
    return r.data;
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
  createVendor: async (data: Partial<Vendor>): Promise<Vendor> => {
    const r = await api.post<Vendor>('/api/procurement/vendors', data);
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
  createEmployee: async (data: Partial<Employee>): Promise<Employee> => {
    const r = await api.post<Employee>('/api/hr/employees', data);
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
