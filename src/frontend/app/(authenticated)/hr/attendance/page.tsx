'use client';

// صفحة الحضور (Attendance) — CheckIn / CheckOut + history

import { useEffect, useState } from 'react';
import { formatDate, formatTime } from '@/lib/utils';
import { LogIn, LogOut as LogOutIcon, Clock, ClipboardList } from 'lucide-react';
import { Button, Select, Table, Badge, Card, PageHeader, EmptyState, SkeletonTable } from '@/components/ui';
import { useToast } from '@/lib/useToast';
import { useAuth } from '@/lib/useAuth';
import {
  hrApi,
  Employee,
  AttendanceRecord,
  ATTENDANCE_TYPES,
  getErrorMessage,
} from '@/lib/api';

export default function AttendancePage() {
  const { user } = useAuth();
  const toast = useToast();
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [selectedEmployee, setSelectedEmployee] = useState('');
  const [history, setHistory] = useState<AttendanceRecord[]>([]);
  const [loadingHistory, setLoadingHistory] = useState(false);
  const [actionLoading, setActionLoading] = useState<1 | 2 | null>(null);

  useEffect(() => {
    loadEmployees();
  }, []);

  useEffect(() => {
    if (selectedEmployee) loadHistory();
  }, [selectedEmployee]);

  const loadEmployees = async () => {
    try {
      const data = await hrApi.listEmployees();
      setEmployees(data);
      // حاول اختيار المستخدم الحالي إن وُجد (نطابق fullName أو email)
      if (user?.email) {
        const me = data.find((e) => e.email === user.email);
        if (me) setSelectedEmployee(me.id);
      }
    } catch {
      // ignore
    }
  };

  const loadHistory = async () => {
    setLoadingHistory(true);
    try {
      const data = await hrApi.listAttendance({ employeeId: selectedEmployee });
      // آخر 20 سجل
      setHistory(data.slice(-20).reverse());
    } catch (e: unknown) {
      toast.error(getErrorMessage(e, 'تعذّر تحميل السجل.'));
    } finally {
      setLoadingHistory(false);
    }
  };

  const handleAction = async (type: 1 | 2) => {
    if (!selectedEmployee) {
      toast.error('يجب اختيار الموظف أولاً.');
      return;
    }
    setActionLoading(type);
    try {
      await hrApi.recordAttendance({ employeeId: selectedEmployee, type });
      toast.success(type === 1 ? 'تم تسجيل الحضور بنجاح.' : 'تم تسجيل الانصراف بنجاح.');
      loadHistory();
    } catch (e: unknown) {
      toast.error(getErrorMessage(e, 'فشل تسجيل الحركة.'));
    } finally {
      setActionLoading(null);
    }
  };

  return (
    <div>
      <PageHeader
        title="🕐 الحضور والانصراف"
        description="سجل حضور الموظفين — CheckIn / CheckOut"
      />

      {/* Action Card */}
      <Card title="تسجيل حركة" description="اختر الموظف ثم اضغط CheckIn / CheckOut" className="mb-4">
        <div className="flex flex-wrap items-end gap-3">
          <div className="flex-1 min-w-[200px]">
            <Select
              label="الموظف"
              value={selectedEmployee}
              onChange={(e) => setSelectedEmployee(e.target.value)}
              options={employees.map((e) => ({ label: `${e.fullName} (${e.employeeNumber})`, value: e.id }))}
              placeholder="اختر الموظف"
            />
          </div>
          <Button
            variant="primary"
            onClick={() => handleAction(1)}
            loading={actionLoading === 1}
            disabled={!selectedEmployee}
            iconLeft={<LogIn className="h-4 w-4" />}
          >
            CheckIn
          </Button>
          <Button
            variant="danger"
            onClick={() => handleAction(2)}
            loading={actionLoading === 2}
            disabled={!selectedEmployee}
            iconLeft={<LogOutIcon className="h-4 w-4" />}
          >
            CheckOut
          </Button>
        </div>
      </Card>

      {/* History */}
      <Card title="السجل الأخير" description={`آخر 20 حركة للموظف المحدد`}>
        {loadingHistory ? (
          <SkeletonTable rows={5} cols={3} />
        ) : !selectedEmployee ? (
          <EmptyState
            icon={<ClipboardList className="h-12 w-12" />}
            title="اختر موظفاً لعرض السجل"
            description="اختر موظفاً من القائمة أعلاه لعرض آخر 20 حركة حضور/انصراف."
          />
        ) : history.length === 0 ? (
          <EmptyState
            icon={<Clock className="h-12 w-12" />}
            title="لا توجد حركات مسجلة"
            description="لم يتم تسجيل أي حركة حضور أو انصراف لهذا الموظف."
          />
        ) : (
          <Table
            columns={[
              {
                key: 'timestamp',
                header: 'الوقت',
                render: (r) => (
                  <div>
                    <p className="text-sm text-gray-800">{formatDate(r.timestamp)}</p>
                    <p className="text-xs text-gray-500 font-mono">
                      {formatTime(r.timestamp)}
                    </p>
                  </div>
                ),
              },
              {
                key: 'type',
                header: 'النوع',
                render: (r) => (
                  <Badge variant={r.type === 1 ? 'success' : 'danger'}>
                    {r.type === 1 ? (
                      <>
                        <LogIn className="h-3 w-3 ml-1" /> {ATTENDANCE_TYPES[r.type]}
                      </>
                    ) : (
                      <>
                        <LogOutIcon className="h-3 w-3 ml-1" /> {ATTENDANCE_TYPES[r.type]}
                      </>
                    )}
                  </Badge>
                ),
              },
              {
                key: 'notes',
                header: 'ملاحظات',
                render: (r) => r.notes || <span className="text-gray-400 text-xs">—</span>,
              },
            ]}
            data={history}
            rowKey={(r) => r.id}
            emptyMessage="لا توجد حركات مسجلة لهذا الموظف."
          />
        )}
      </Card>
    </div>
  );
}


