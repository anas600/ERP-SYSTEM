'use client';

// صفحة قائمة الموظفين (Employees) — جدول

import { useEffect, useState } from 'react';
import { formatDate, formatTime } from '@/lib/utils';
import Link from 'next/link';
import { Plus, Mail, Phone } from 'lucide-react';
import { Button, Input, Table, Badge, PageHeader } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { hrApi, Employee, getErrorMessage } from '@/lib/api';

export default function EmployeesPage() {
  const { loading: authLoading } = useAuth();
  const [employees, setEmployees] = useState<Employee[]>([]);
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
      const data = await hrApi.listEmployees();
      setEmployees(data);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'تعذّر تحميل الموظفين.'));
    } finally {
      setLoading(false);
    }
  };

  const filtered = employees.filter(
    (e) =>
      !filter ||
      e.fullName.toLowerCase().includes(filter.toLowerCase()) ||
      e.email.toLowerCase().includes(filter.toLowerCase()) ||
      (e.employeeNumber || '').includes(filter)
  );

  return (
    <div>
      <PageHeader
        title="👥 الموظفين"
        description="قائمة الموظفين النشطين في الشركة"
        actions={
          <Link href="/hr/employees/new">
            <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />}>
              موظف جديد
            </Button>
          </Link>
        }
      />

      <div className="mb-4">
        <Input
          placeholder="🔍 بحث (اسم / بريد / رقم)..."
          value={filter}
          onChange={(e) => setFilter(e.target.value)}
          containerClassName="max-w-md"
        />
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4 text-sm">
          {error}
        </div>
      )}

      <Table
        columns={[
          {
            key: 'employee',
            header: 'الموظف',
            render: (e) => (
              <div>
                <p className="font-semibold text-gray-800">{e.fullName}</p>
                <p className="text-xs text-gray-500 font-mono mt-0.5">{e.employeeNumber}</p>
              </div>
            ),
          },
          {
            key: 'contact',
            header: 'التواصل',
            render: (e) => (
              <div className="space-y-1 text-xs text-gray-600">
                <div className="flex items-center gap-1">
                  <Mail className="h-3 w-3" />
                  <span className="truncate max-w-[180px]">{e.email}</span>
                </div>
                {e.phone && (
                  <div className="flex items-center gap-1">
                    <Phone className="h-3 w-3" />
                    <span dir="ltr">{e.phone}</span>
                  </div>
                )}
              </div>
            ),
          },
          {
            key: 'jobTitle',
            header: 'المسمى الوظيفي',
            render: (e) => e.jobTitle || <span className="text-gray-400 text-xs">—</span>,
          },
          {
            key: 'department',
            header: 'القسم',
            render: (e) =>
              e.departmentName ? <Badge variant="info">{e.departmentName}</Badge> : <span className="text-gray-400 text-xs">—</span>,
          },
          {
            key: 'hireDate',
            header: 'تاريخ التعيين',
            render: (e) => (
              <span className="text-sm text-gray-700">{formatDate(e.hireDate)}</span>
            ),
          },
          {
            key: 'baseSalary',
            header: 'الراتب الأساسي',
            align: 'end',
            render: (e) => (
              <span className="font-mono text-sm">
                {e.baseSalary?.toLocaleString(undefined, { minimumFractionDigits: 2 }) || '0.00'}
              </span>
            ),
          },
          {
            key: 'isActive',
            header: 'الحالة',
            align: 'center',
            render: (e) => (e.isActive ? <Badge variant="success">نشط</Badge> : <Badge variant="neutral">غير نشط</Badge>),
          },
        ]}
        data={filtered}
        loading={loading}
        rowKey={(e) => e.id}
        emptyMessage="لا يوجد موظفين. أضف أول موظف."
      />

      {!loading && filtered.length > 0 && (
        <p className="mt-3 text-xs text-gray-500 text-start">{filtered.length} موظف</p>
      )}
    </div>
  );
}


