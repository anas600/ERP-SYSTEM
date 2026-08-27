// Sprint 64 / DEC-225 + DEC-226 — Subcontractor API client (frontend).
//
// Typed wrappers for the Subcontractor, Sub-Contract, Sub-ProgressBilling,
// Sub-Payment, and Sub-Statement endpoints. Mirrors the axios-based
// `lib/api.ts` pattern: every request automatically carries the JWT
// (Authorization) and the active company id (X-Company-Id) via the
// axios interceptor in `lib/api.ts`.
//
// L19 / DEC-095: the JWT context supplies CompanyId — the FE never sends
// CompanyId in the request body.

import { api, getErrorMessage } from '../api';
import type {
  SubStatement,
  SubStatementSummary,
} from '../api-types';

// ============================================================================
// Domain types (re-declared here so the FE pages only need to import from this
// module — keeps the surface tidy. They MUST match the BE response records in
// src/backend/Modules/Projects/Application/Dtos/SubcontractorDtos.cs,
// SubProgressBillingDtos.cs, SubPaymentDtos.cs, and SubStatementDtos.cs.
// ============================================================================

export interface Subcontractor {
  id: string;
  companyId: string;
  code: string;
  name: string;
  nameAr?: string | null;
  contactPerson?: string | null;
  phone?: string | null;
  email?: string | null;
  tradeSpecialty?: string | null;
  taxId?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface SubContract {
  id: string;
  companyId: string;
  projectId: string;
  subcontractorId: string;
  contractNumber: string;
  scopeOfWork: string;
  contractValue: number;
  retentionPercent: number;
  retentionReleaseBilling: number;
  startDate?: string | null;
  endDate?: string | null;
  status: number;          // 1=Active, 2=Completed, 3=Cancelled
  statusName: string;      // "نشط" | "مكتمل" | "ملغى"
  notes?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface SubProgressBilling {
  id: string;
  companyId: string;
  subContractId: string;
  billingNumber: string;
  billingDate: string;
  periodFrom?: string | null;
  periodTo?: string | null;
  workCompletedPercent: number;
  grossAmount: number;
  retentionDeducted: number;
  previousBillingsAmount: number;
  netPayable: number;
  status: number;          // 1=Draft, 2=Approved, 3=Paid, 4=Cancelled
  statusName: string;      // "مسودة" | "معتمد" | "مدفوع" | "ملغى"
  notes?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface SubPayment {
  id: string;
  companyId: string;
  subContractId: string;
  subProgressBillingId: string;
  paymentNumber: string;
  paymentDate: string;
  amount: number;
  retentionReleased: number;
  paymentMethod?: string | null;
  referenceNumber?: string | null;
  notes?: string | null;
  createdAt: string;
}

// ============================================================================
// Requests
// ============================================================================

export interface CreateSubcontractorRequest {
  code: string;
  name: string;
  nameAr?: string | null;
  contactPerson?: string | null;
  phone?: string | null;
  email?: string | null;
  tradeSpecialty?: string | null;
  taxId?: string | null;
}

export interface UpdateSubcontractorRequest {
  name: string;
  nameAr?: string | null;
  contactPerson?: string | null;
  phone?: string | null;
  email?: string | null;
  tradeSpecialty?: string | null;
  taxId?: string | null;
  isActive: boolean;
}

export interface CreateSubContractRequest {
  subcontractorId: string;
  contractNumber: string;
  scopeOfWork: string;
  contractValue: number;
  retentionPercent: number;
  retentionReleaseBilling: number;
  startDate?: string | null;
  endDate?: string | null;
  notes?: string | null;
}

export interface CreateSubProgressBillingRequest {
  billingNumber: string;
  billingDate: string;
  periodFrom?: string | null;
  periodTo?: string | null;
  workCompletedPercent: number;
  notes?: string | null;
}

export interface CreateSubPaymentRequest {
  paymentNumber: string;
  paymentDate: string;
  amount: number;
  paymentMethod?: string | null;
  referenceNumber?: string | null;
  notes?: string | null;
}

export interface ReleaseRetentionRequest {
  releaseDate: string;
  amount: number;
  notes?: string | null;
}

// ============================================================================
// Helpers — tolerate { statement: ... } OR raw ... response shapes
// (Sprint 64 / DEC-225 contract: BE returns the raw DTO directly, but we keep
// a defensive unwrap in case the controller is later wrapped in an envelope.)
// ============================================================================

function unwrap<T>(data: T | { statement: T; summary: T } | any, key: 'statement' | 'summary'): T {
  if (data && typeof data === 'object' && key in data) return data[key] as T;
  return data as T;
}

// ============================================================================
// API client
// ============================================================================

export const subcontractorsApi = {
  // -------- Subcontractor master data --------

  listSubcontractors: async (): Promise<Subcontractor[]> => {
    const r = await api.get<Subcontractor[]>('/api/subcontractors');
    return Array.isArray(r.data) ? r.data : [];
  },

  getSubcontractor: async (id: string): Promise<Subcontractor> => {
    const r = await api.get<Subcontractor>(`/api/subcontractors/${id}`);
    return r.data;
  },

  createSubcontractor: async (data: CreateSubcontractorRequest): Promise<Subcontractor> => {
    const r = await api.post<Subcontractor>('/api/subcontractors', data);
    return r.data;
  },

  updateSubcontractor: async (
    id: string,
    data: UpdateSubcontractorRequest
  ): Promise<Subcontractor> => {
    const r = await api.put<Subcontractor>(`/api/subcontractors/${id}`, data);
    return r.data;
  },

  // -------- Sub-Contract (project + subcontractor link) --------

  listSubContractsByProject: async (projectId: string): Promise<SubContract[]> => {
    const r = await api.get<SubContract[]>(`/api/projects/${projectId}/sub-contracts`);
    return Array.isArray(r.data) ? r.data : [];
  },

  getSubContract: async (id: string): Promise<SubContract> => {
    const r = await api.get<SubContract>(`/api/sub-contracts/${id}`);
    return r.data;
  },

  createSubContract: async (
    projectId: string,
    data: CreateSubContractRequest
  ): Promise<SubContract> => {
    const r = await api.post<SubContract>(`/api/projects/${projectId}/sub-contracts`, data);
    return r.data;
  },

  // -------- Sub-ProgressBilling --------

  listBillingsBySubContract: async (subContractId: string): Promise<SubProgressBilling[]> => {
    const r = await api.get<SubProgressBilling[]>(`/api/sub-contracts/${subContractId}/billings`);
    return Array.isArray(r.data) ? r.data : [];
  },

  createBilling: async (
    subContractId: string,
    data: CreateSubProgressBillingRequest
  ): Promise<SubProgressBilling> => {
    const r = await api.post<SubProgressBilling>(
      `/api/sub-contracts/${subContractId}/billings`,
      data
    );
    return r.data;
  },

  // -------- Sub-Payment --------

  listPaymentsBySubContract: async (subContractId: string): Promise<SubPayment[]> => {
    const r = await api.get<SubPayment[]>(`/api/sub-contracts/${subContractId}/payments`);
    return Array.isArray(r.data) ? r.data : [];
  },

  createPayment: async (
    subContractId: string,
    billingId: string,
    data: CreateSubPaymentRequest
  ): Promise<SubPayment> => {
    const r = await api.post<SubPayment>(
      `/api/sub-contracts/${subContractId}/billings/${billingId}/payments`,
      data
    );
    return r.data;
  },

  releaseRetention: async (
    subContractId: string,
    data: ReleaseRetentionRequest
  ): Promise<SubPayment> => {
    const r = await api.post<SubPayment>(
      `/api/sub-contracts/${subContractId}/release-retention`,
      data
    );
    return r.data;
  },

  // -------- Sub-Statement (DEC-225) --------

  /**
   * GET /api/sub-contracts/{subContractId}/statement
   * Returns the full P&L for one sub-contract.
   */
  fetchSubStatement: async (subContractId: string): Promise<SubStatement> => {
    try {
      const { data } = await api.get<SubStatement | { statement: SubStatement }>(
        `/api/sub-contracts/${subContractId}/statement`
      );
      return unwrap<SubStatement>(data, 'statement');
    } catch (e) {
      throw new Error(getErrorMessage(e, 'فشل تحميل كشف الحساب'));
    }
  },

  /**
   * GET /api/subcontractors/{subcontractorId}/projects/{projectId}/summary
   * Aggregated summary across all sub-contracts for a (subcontractor, project) pair.
   */
  fetchSubcontractorProjectSummary: async (
    subcontractorId: string,
    projectId: string
  ): Promise<SubStatementSummary> => {
    try {
      const { data } = await api.get<
        SubStatementSummary | { summary: SubStatementSummary }
      >(`/api/subcontractors/${subcontractorId}/projects/${projectId}/summary`);
      return unwrap<SubStatementSummary>(data, 'summary');
    } catch (e) {
      throw new Error(getErrorMessage(e, 'فشل تحميل ملخص المشروع'));
    }
  },
};

// ============================================================================
// Standalone helpers (named exports for direct import in pages that want to
// keep tree-shaking tight). Both functions tolerate { statement | summary }
// envelope OR raw DTO shapes from the BE.
// ============================================================================

export async function fetchSubStatement(subContractId: string): Promise<SubStatement> {
  return subcontractorsApi.fetchSubStatement(subContractId);
}

export async function fetchSubcontractorProjectSummary(
  subcontractorId: string,
  projectId: string
): Promise<SubStatementSummary> {
  return subcontractorsApi.fetchSubcontractorProjectSummary(subcontractorId, projectId);
}
