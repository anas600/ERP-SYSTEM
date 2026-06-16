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

export interface RegisterRequest {
  email: string;
  password: string;
  fullName: string;
  tenantName: string;
  subdomain: string;
}

export interface LoginRequest {
  email: string;
  password: string;
  tenantSubdomain?: string;
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
