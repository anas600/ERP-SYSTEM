'use client';

// صفحة الأقسام (Departments) — جدول
// Sprint 31 (Playwright discovery): this page was missing. Created.

import { useEffect, useState } from 'react';
import { Users, ChevronRight, Building2 } from 'lucide-react';
import { PageHeader, Input, Table, Badge } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { hrApi, Department, getErrorMessage } from '@/lib/api';

export default function DepartmentsPage() {
  const { loading: authLoading } = useAuth();
  const [departments, setDepartments] = useState<Department[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState('');

  useEffect(() => {
    if (authLoading) return;
    load();
  }, [authLoading]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await hrApi.listDepartments();
      setDepartments(data);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'تعذّر تحميل الأقسام.'));
    } finally {
      setLoading(false);
    }
  };

  const filtered = departments.filter(
    (d) =>
      !filter ||
      d.name.toLowerCase().includes(filter.toLowerCase()) ||
      d.code.toLowerCase().includes(filter.toLowerCase()) ||
      (d.managerName || '').toLowerCase().includes(filter.toLowerCase())
  );

  // Top-level (no parent) vs sub-departments
  const topLevel = filtered.filter((d) => !d.parentId);
  const childrenOf = (parentId: string) => filtered.filter((d) => d.parentId === parentId);

  return (
    <div>
      <PageHeader
        title="🏢 الأقسام"
        description="الهيكل التنظيمي للشركة — الأقسام والمدراء والموظفين"
      />

      <div className="mb-4">
        <Input
          placeholder="🔍 بحث (اسم / كود / مدير)..."
          value={filter}
          onChange={(e) => setFilter(e.target.value)}
          containerClassName="max-w-md"
        />
      </div>

      {error && (
        <div className="bg-danger-50 border border-danger-200 text-danger-700 px-4 py-3 rounded-lg mb-4 text-sm">
          {error}
        </div>
      )}

      {loading ? (
        <div className="bg-white rounded-xl border border-gray-200 p-8 text-center text-gray-500">
          جاري التحميل…
        </div>
      ) : filtered.length === 0 ? (
        <div className="bg-white rounded-xl border border-gray-200 p-8 text-center text-gray-500">
          لا توجد أقسام تطابق البحث.
        </div>
      ) : (
        <div className="space-y-3">
          {topLevel.map((dept) => (
            <div key={dept.id} className="bg-white rounded-xl border border-gray-200 overflow-hidden">
              <DepartmentCard dept={dept} />
              {childrenOf(dept.id).map((sub) => (
                <div key={sub.id} className="border-t border-gray-100 pr-8 bg-gray-50/50">
                  <DepartmentCard dept={sub} isChild />
                </div>
              ))}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function DepartmentCard({ dept, isChild }: { dept: Department; isChild?: boolean }) {
  return (
    <div className="p-4 flex items-start gap-3">
      <div className={`mt-1 ${isChild ? 'text-gray-400' : 'text-blue-600'}`}>
        {isChild ? <ChevronRight className="h-5 w-5" /> : <Building2 className="h-5 w-5" />}
      </div>
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2 flex-wrap">
          <p className={`font-semibold ${isChild ? 'text-gray-700' : 'text-gray-800'}`}>{dept.name}</p>
          <span className="text-xs font-mono text-gray-500 bg-gray-100 px-1.5 py-0.5 rounded">
            {dept.code}
          </span>
          {!dept.isActive && <Badge variant="warning">غير نشط</Badge>}
        </div>
        <div className="mt-1 flex items-center gap-4 text-sm text-gray-600 flex-wrap">
          <div className="flex items-center gap-1">
            <Users className="h-3.5 w-3.5" />
            <span>
              <span className="font-semibold">{dept.employeeCount ?? 0}</span> موظف
            </span>
          </div>
          {dept.managerName && (
            <div className="flex items-center gap-1">
              <span className="text-gray-500">المدير:</span>
              <span className="font-medium">{dept.managerName}</span>
              {dept.managerCode && (
                <span className="text-xs font-mono text-gray-400">({dept.managerCode})</span>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
