'use client';

import { useEffect, useState } from 'react';
import { Activity, Database, HardDrive, Cpu, Clock, CheckCircle2, AlertTriangle, XCircle, RefreshCw } from 'lucide-react';
import { PageHeader, Card, Badge, Button } from '@/components/ui';
import { api, getErrorMessage } from '@/lib/api';

interface ComponentHealth {
  status: 'healthy' | 'degraded' | 'unhealthy' | 'unknown';
  [key: string]: any;
}

interface HealthReport {
  status: 'healthy' | 'degraded' | 'unhealthy';
  timestamp: string;
  components: Record<string, ComponentHealth>;
}

const STATUS_VARIANTS: Record<string, 'success' | 'warning' | 'danger' | 'neutral'> = {
  healthy: 'success',
  degraded: 'warning',
  unhealthy: 'danger',
  unknown: 'neutral',
};

const STATUS_ICONS: Record<string, any> = {
  healthy: CheckCircle2,
  degraded: AlertTriangle,
  unhealthy: XCircle,
  unknown: Activity,
};

const STATUS_LABELS: Record<string, string> = {
  healthy: 'سليم',
  degraded: 'متدهور',
  unhealthy: 'معطل',
  unknown: 'غير معروف',
};

const COMPONENT_LABELS: Record<string, string> = {
  database: 'قاعدة البيانات',
  memory: 'الذاكرة',
  disk: 'القرص',
  process: 'العملية',
  recent_activity: 'النشاط الأخير',
};

const COMPONENT_ICONS: Record<string, any> = {
  database: Database,
  memory: Cpu,
  disk: HardDrive,
  process: Activity,
  recent_activity: Clock,
};

export default function HealthPage() {
  const [health, setHealth] = useState<HealthReport | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [autoRefresh, setAutoRefresh] = useState(true);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await api.get<HealthReport>('/api/health/full');
      setHealth(data.data);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل حالة النظام.'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  useEffect(() => {
    if (!autoRefresh) return;
    const interval = setInterval(load, 30000);  // 30s auto-refresh
    return () => clearInterval(interval);
  }, [autoRefresh]);

  const overallIcon = health ? STATUS_ICONS[health.status] : Activity;
  const OverallIcon = overallIcon;

  return (
    <div>
      <PageHeader
        title="💚 حالة النظام"
        description="System Health — DB + Disk + Memory + Process + Activity"
        actions={
          <>
            <Button
              variant={autoRefresh ? 'primary' : 'secondary'}
              size="sm"
              onClick={() => setAutoRefresh(!autoRefresh)}
            >
              {autoRefresh ? 'إيقاف التحديث' : 'تفعيل التحديث'}
            </Button>
            <Button onClick={load} variant="secondary" size="sm" iconLeft={<RefreshCw className="h-3 w-3" />} disabled={loading}>
              تحديث
            </Button>
          </>
        }
      />

      {error && (
        <div className="bg-danger-50 border border-danger-200 text-danger-700 px-4 py-3 rounded-lg mb-4">
          {error}
        </div>
      )}

      {/* Overall Status */}
      {health && (
        <Card accent={health.status === 'healthy' ? 'green' : health.status === 'degraded' ? 'yellow' : 'red'}>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <OverallIcon className={`h-8 w-8 ${
                health.status === 'healthy' ? 'text-green-600' :
                health.status === 'degraded' ? 'text-yellow-600' : 'text-red-600'
              }`} />
              <div>
                <div className="text-2xl font-bold">
                  {STATUS_LABELS[health.status]}
                </div>
                <div className="text-sm text-gray-500">
                  آخر فحص: {new Date(health.timestamp).toLocaleString('ar-LY')}
                </div>
              </div>
            </div>
            <Badge variant={STATUS_VARIANTS[health.status]}>
              {health.status.toUpperCase()}
            </Badge>
          </div>
        </Card>
      )}

      {/* Component Status */}
      <h2 className="text-lg font-semibold text-gray-800 mt-6 mb-3">المكونات</h2>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        {health && Object.entries(health.components).map(([key, comp]) => {
          const Icon = COMPONENT_ICONS[key] || Activity;
          const StatusIcon = STATUS_ICONS[comp.status] || Activity;
          const SIcon = StatusIcon;
          return (
            <Card key={key} accent={(STATUS_VARIANTS[comp.status] === 'success' ? 'green' : STATUS_VARIANTS[comp.status] === 'warning' ? 'yellow' : 'red') as any}>
              <div className="flex items-start justify-between mb-3">
                <div className="flex items-center gap-2">
                  <Icon className="h-5 w-5 text-gray-600" />
                  <h3 className="font-bold text-gray-800">{COMPONENT_LABELS[key] || key}</h3>
                </div>
                <div className="flex items-center gap-1">
                  <SIcon className={`h-4 w-4 ${
                    comp.status === 'healthy' ? 'text-green-600' :
                    comp.status === 'degraded' ? 'text-yellow-600' :
                    comp.status === 'unhealthy' ? 'text-red-600' : 'text-gray-400'
                  }`} />
                  <Badge variant={STATUS_VARIANTS[comp.status]}>
                    {STATUS_LABELS[comp.status]}
                  </Badge>
                </div>
              </div>
              <div className="space-y-1 text-xs text-gray-600">
                {Object.entries(comp).filter(([k]) => k !== 'status').map(([k, v]) => (
                  <div key={k} className="flex justify-between">
                    <span className="text-gray-500">{k}:</span>
                    <span className="font-mono text-gray-800">{typeof v === 'number' ? v.toLocaleString() : String(v)}</span>
                  </div>
                ))}
              </div>
            </Card>
          );
        })}
      </div>

      {loading && !health && (
        <div className="bg-white rounded-xl shadow-sm p-12 text-center text-gray-500 mt-4">
          <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-blue-500 border-r-transparent" />
          <p className="mt-3 text-sm">جاري التحقق...</p>
        </div>
      )}
    </div>
  );
}
