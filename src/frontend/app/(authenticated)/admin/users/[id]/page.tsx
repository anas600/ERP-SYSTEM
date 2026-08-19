'use client';

// Sprint 2 — T10: User detail + assigned companies.
//   Route: /admin/users/[id]
//   Show:  email, full_name, is_active, two_factor_enabled, created_at, last_login_at
//   Roles list + "Assigned Companies" section.
//   Edit:  full name, email, is_active, roleIds (PUT /api/identity/users/{id})
//   Pwd:   reset (PUT /api/identity/users/{id}/password)
//   Cos:   assign/remove (POST/DELETE /api/identity/users/{id}/companies)
//
// T5 enhancement: بدل الاعتماد على `identityApi.getUser(id).companies` فقط
// (الـ eager-load في user detail)، نستخدم `usersApi.getUserCompanies(id)`
// (T5: GET /api/users/{id}/companies) كمصدر منفصل — يتطابق مع T5 contract
// في الـ hand-off. لو الـ endpoint غير متاح بعد، نسقط إلى getUser.detail
// companies مع تحذير صامت.

import { useEffect, useState, useCallback } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { ArrowRight, Save, KeyRound, Building2, Trash2, Plus, Shield, Mail, CheckCircle2, XCircle, Calendar } from 'lucide-react';
import { Badge, Button, Card, Modal, PageHeader, SkeletonTable } from '@/components/ui';
import { companiesApi, identityApi, usersApi, getErrorMessage, type Company, type UserCompany } from '@/lib/api';
import { formatDate, formatTime } from '@/lib/utils';
import { useToast } from '@/lib/useToast';

interface UserDetail {
  id: string;
  email: string;
  fullName: string;
  isActive: boolean;
  twoFactorEnabled?: boolean;
  createdAt: string;
  updatedAt: string;
  lastLoginAt?: string;
  roleIds: string[];
  /** User → company assignments. يتم تحميلها من T5 endpoint أو كـ fallback من getUser. */
  companies: UserCompany[];
}

interface RoleItem { id: string; name: string; description?: string; }

const ROLE_COLORS: Record<string, 'success' | 'info' | 'warning' | 'danger' | 'neutral'> = {
  Admin: 'danger',
  Accountant: 'info',
  ProjectManager: 'warning',
  Viewer: 'neutral',
};

export default function UserDetailPage() {
  const params = useParams();
  const id = params?.id as string;
  const router = useRouter();
  const toast = useToast();

  const [user, setUser] = useState<UserDetail | null>(null);
  const [roles, setRoles] = useState<RoleItem[]>([]);
  const [companies, setCompanies] = useState<Company[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [editing, setEditing] = useState(false);
  const [editName, setEditName] = useState('');
  const [editEmail, setEditEmail] = useState('');
  const [editIsActive, setEditIsActive] = useState(true);
  const [editRoleIds, setEditRoleIds] = useState<string[]>([]);

  // Modals
  const [showPwd, setShowPwd] = useState(false);
  const [newPwd, setNewPwd] = useState('');
  const [showAddCompany, setShowAddCompany] = useState(false);
  const [addCompanyId, setAddCompanyId] = useState('');
  const [addAsDefault, setAddAsDefault] = useState(false);

  const load = useCallback(async () => {
    if (!id) return;
    setLoading(true);
    try {
      const [detail, rolesRes, companiesRes] = await Promise.all([
        identityApi.getUser(id),
        identityApi.listRoles(),
        // T8: companies list now comes from companiesApi (paginated).
        // Fall back to empty array on failure.
        companiesApi.list({ pageSize: 100, includeInactive: true }).catch(() => null),
      ]);
      // T5: get user→company assignments from the dedicated endpoint.
      // Fall back to the eager-loaded `detail.companies` if T5 isn't wired yet.
      let userCompanies: UserCompany[] = detail.companies;
      try {
        userCompanies = await usersApi.getUserCompanies(id);
      } catch {
        // T5 endpoint not yet available — keep the eager-loaded assignments
        // from identityApi.getUser. No toast: this is a graceful degradation.
      }
      const u: UserDetail = {
        ...detail.user,
        roleIds: detail.roleIds,
        companies: userCompanies,
      };
      setUser(u);
      setRoles(rolesRes);
      setCompanies(companiesRes ? companiesRes.items : []);
      setEditName(u.fullName);
      setEditEmail(u.email);
      setEditIsActive(u.isActive);
      setEditRoleIds(u.roleIds);
    } catch (e: unknown) {
      toast.error(getErrorMessage(e, 'فشل تحميل المستخدم.'));
    } finally {
      setLoading(false);
    }
  }, [id, toast]);

  useEffect(() => { load(); }, [load]);

  const startEdit = () => {
    if (!user) return;
    setEditName(user.fullName);
    setEditEmail(user.email);
    setEditIsActive(user.isActive);
    setEditRoleIds(user.roleIds);
    setEditing(true);
  };

  const save = async () => {
    if (!user) return;
    setSaving(true);
    try {
      await identityApi.updateUser(user.id, {
        fullName: editName,
        email: editEmail,
        isActive: editIsActive,
        roleIds: editRoleIds,
      });
      toast.success('تم حفظ التغييرات');
      setEditing(false);
      await load();
    } catch (e: unknown) {
      toast.error(getErrorMessage(e, 'فشل الحفظ.'));
    } finally {
      setSaving(false);
    }
  };

  const submitResetPwd = async () => {
    if (!user) return;
    if (newPwd.length < 8) { toast.error('كلمة المرور يجب أن تكون 8 أحرف على الأقل.'); return; }
    setSaving(true);
    try {
      await identityApi.resetPassword(user.id, newPwd);
      toast.success('تم تغيير كلمة المرور');
      setShowPwd(false);
      setNewPwd('');
    } catch (e: unknown) {
      toast.error(getErrorMessage(e, 'فشل تغيير كلمة المرور.'));
    } finally {
      setSaving(false);
    }
  };

  const assignCompany = async () => {
    if (!user || !addCompanyId) return;
    setSaving(true);
    try {
      await identityApi.assignUserToCompany(user.id, addCompanyId, addAsDefault);
      toast.success('تم إضافة الشركة للمستخدم');
      setShowAddCompany(false);
      setAddCompanyId('');
      setAddAsDefault(false);
      await load();
    } catch (e: unknown) {
      toast.error(getErrorMessage(e, 'فشل إضافة الشركة.'));
    } finally {
      setSaving(false);
    }
  };

  const removeCompany = async (companyId: string, companyName: string) => {
    if (!user) return;
    if (!confirm(`إزالة المستخدم من شركة ${companyName}؟`)) return;
    setSaving(true);
    try {
      await identityApi.removeUserFromCompany(user.id, companyId);
      toast.success(`تم إزالة ${companyName}`);
      await load();
    } catch (e: unknown) {
      toast.error(getErrorMessage(e, 'فشل الإزالة.'));
    } finally {
      setSaving(false);
    }
  };

  if (loading || !user) {
    return (
      <div>
        <PageHeader title="تحميل..." />
        <div className="bg-white rounded-xl shadow-sm p-4">
          <SkeletonTable rows={3} cols={3} />
        </div>
      </div>
    );
  }

  const userRoles = user.roleIds.map(rid => roles.find(r => r.id === rid)?.name).filter(Boolean) as string[];
  const availableCompanies = companies.filter(c => !user.companies.some(uc => uc.companyId === c.id));

  return (
    <div>
      <PageHeader
        title={user.fullName || user.email}
        description="تفاصيل المستخدم"
        actions={
          <div className="flex gap-2">
            <Button variant="secondary" onClick={() => router.push('/admin/users')}>
              <ArrowRight className="h-4 w-4 inline-block ml-1" />
              العودة
            </Button>
            {!editing ? (
              <Button variant="primary" onClick={startEdit}>
                تعديل
              </Button>
            ) : (
              <>
                <Button variant="secondary" onClick={() => setEditing(false)} disabled={saving}>
                  إلغاء
                </Button>
                <Button variant="primary" onClick={save} disabled={saving}>
                  <Save className="h-4 w-4 inline-block ml-1" />
                  {saving ? 'جاري...' : 'حفظ'}
                </Button>
              </>
            )}
          </div>
        }
      />

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        <Card>
          <h3 className="text-lg font-bold text-gray-800 mb-3">المعلومات الأساسية</h3>
          <div className="space-y-3">
            <div>
              <label className="block text-sm text-gray-500 mb-1">الاسم الكامل</label>
              {editing ? (
                <input
                  value={editName}
                  onChange={(e) => setEditName(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm"
                />
              ) : (
                <div className="text-gray-800 font-medium">{user.fullName || '—'}</div>
              )}
            </div>
            <div>
              <label className="block text-sm text-gray-500 mb-1">الإيميل</label>
              {editing ? (
                <input
                  type="email"
                  value={editEmail}
                  onChange={(e) => setEditEmail(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm"
                  dir="ltr"
                />
              ) : (
                <div className="text-gray-800 flex items-center gap-1" dir="ltr">
                  <Mail className="h-3 w-3 text-gray-400" />
                  {user.email}
                </div>
              )}
            </div>
            <div>
              <label className="block text-sm text-gray-500 mb-1">الحالة</label>
              {editing ? (
                <label className="flex items-center gap-2 text-sm">
                  <input
                    type="checkbox"
                    checked={editIsActive}
                    onChange={(e) => setEditIsActive(e.target.checked)}
                    className="rounded"
                  />
                  <span>فعّال</span>
                </label>
              ) : user.isActive ? (
                <Badge variant="success">
                  <CheckCircle2 className="h-3 w-3 inline-block ml-1" />
                  فعّال
                </Badge>
              ) : (
                <Badge variant="neutral">
                  <XCircle className="h-3 w-3 inline-block ml-1" />
                  معطّل
                </Badge>
              )}
            </div>
            <div>
              <label className="block text-sm text-gray-500 mb-1">الأدوار</label>
              {editing ? (
                <div className="flex flex-wrap gap-2">
                  {roles.map((r) => {
                    const sel = editRoleIds.includes(r.id);
                    return (
                      <button
                        type="button"
                        key={r.id}
                        onClick={() => setEditRoleIds(prev => prev.includes(r.id) ? prev.filter(x => x !== r.id) : [...prev, r.id])}
                        className={`px-3 py-1.5 rounded-lg text-sm border transition-colors ${
                          sel ? 'bg-blue-600 text-white border-blue-600' : 'bg-white text-gray-700 border-gray-300 hover:border-blue-400'
                        }`}
                      >
                        {r.name}
                      </button>
                    );
                  })}
                </div>
              ) : (
                <div className="flex flex-wrap gap-2">
                  {userRoles.length === 0 ? (
                    <span className="text-sm text-gray-400">لا يوجد أدوار</span>
                  ) : (
                    userRoles.map((r) => (
                      <Badge key={r} variant={ROLE_COLORS[r] ?? 'neutral'}>
                        <Shield className="h-3 w-3 inline-block ml-1" />
                        {r}
                      </Badge>
                    ))
                  )}
                </div>
              )}
            </div>
            <div className="pt-3 border-t text-xs text-gray-500 space-y-1">
              <div className="flex items-center gap-1">
                <Calendar className="h-3 w-3" />
                تاريخ الإنشاء: {formatDate(user.createdAt)}
              </div>
              {user.lastLoginAt && (
                <div>آخر دخول: {formatDate(user.lastLoginAt)} {formatTime(user.lastLoginAt)}</div>
              )}
            </div>
          </div>
        </Card>

        <Card>
          <div className="flex items-center justify-between mb-3">
            <h3 className="text-lg font-bold text-gray-800">الشركات المرتبطة</h3>
            <Button variant="ghost" size="sm" onClick={() => setShowAddCompany(true)}>
              <Plus className="h-3 w-3 inline-block ml-1" />
              إضافة شركة
            </Button>
          </div>
          {user.companies.length === 0 ? (
            <p className="text-sm text-gray-400 py-6 text-center">لا توجد شركات مرتبطة</p>
          ) : (
            <div className="space-y-2">
              {user.companies.map((c) => (
                <div key={c.companyId} className="flex items-center justify-between p-2 border border-gray-200 rounded-lg">
                  <div className="flex items-center gap-2">
                    <Building2 className="h-4 w-4 text-gray-500" />
                    <div>
                      <div className="text-sm font-medium">
                        {c.companyCode} — {c.companyName}
                        {c.isDefault && <Badge variant="success" className="mr-2">افتراضية</Badge>}
                        {c.isHolding && <Badge variant="info" className="mr-2">Holding</Badge>}
                      </div>
                      <div className="text-xs text-gray-400">منذ {formatDate(c.assignedAt)}</div>
                    </div>
                  </div>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => removeCompany(c.companyId, c.companyName)}
                    disabled={saving}
                  >
                    <Trash2 className="h-3 w-3 text-danger-500" />
                  </Button>
                </div>
              ))}
            </div>
          )}
          <div className="pt-3 border-t mt-3">
            <Button variant="secondary" size="sm" onClick={() => setShowPwd(true)}>
              <KeyRound className="h-3 w-3 inline-block ml-1" />
              إعادة تعيين كلمة المرور
            </Button>
          </div>
        </Card>
      </div>

      {/* Reset Password Modal */}
      <Modal open={showPwd} onClose={() => { if (!saving) { setShowPwd(false); setNewPwd(''); } }} title="إعادة تعيين كلمة المرور">
        <div className="space-y-3" dir="rtl">
          <p className="text-sm text-gray-600">أدخل كلمة مرور جديدة (8 أحرف على الأقل).</p>
          <input
            type="password"
            value={newPwd}
            onChange={(e) => setNewPwd(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm"
            placeholder="••••••••"
            disabled={saving}
            autoFocus
            dir="ltr"
          />
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="secondary" onClick={() => { setShowPwd(false); setNewPwd(''); }} disabled={saving}>إلغاء</Button>
            <Button variant="primary" onClick={submitResetPwd} disabled={saving || newPwd.length < 8}>
              {saving ? 'جاري...' : 'حفظ'}
            </Button>
          </div>
        </div>
      </Modal>

      {/* Add Company Modal */}
      <Modal open={showAddCompany} onClose={() => { if (!saving) { setShowAddCompany(false); setAddCompanyId(''); } }} title="إضافة شركة للمستخدم">
        <div className="space-y-3" dir="rtl">
          {availableCompanies.length === 0 ? (
            <p className="text-sm text-gray-500">كل الشركات مربوطة بهذا المستخدم بالفعل.</p>
          ) : (
            <>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">الشركة</label>
                <select
                  value={addCompanyId}
                  onChange={(e) => setAddCompanyId(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm"
                >
                  <option value="">— اختر شركة —</option>
                  {availableCompanies.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.code} — {c.name} {c.isHolding ? '(Holding)' : ''}
                    </option>
                  ))}
                </select>
              </div>
              <label className="flex items-center gap-2 text-sm text-gray-700">
                <input
                  type="checkbox"
                  checked={addAsDefault}
                  onChange={(e) => setAddAsDefault(e.target.checked)}
                  className="rounded"
                />
                <span>اجعلها الشركة الافتراضية</span>
              </label>
              <div className="flex justify-end gap-2 pt-2">
                <Button variant="secondary" onClick={() => { setShowAddCompany(false); setAddCompanyId(''); }} disabled={saving}>إلغاء</Button>
                <Button variant="primary" onClick={assignCompany} disabled={saving || !addCompanyId}>
                  {saving ? 'جاري...' : 'إضافة'}
                </Button>
              </div>
            </>
          )}
        </div>
      </Modal>
    </div>
  );
}
