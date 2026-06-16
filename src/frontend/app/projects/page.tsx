'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { authApi, projectsApi, Project, PROJECT_STATUSES } from '@/lib/api';

export default function ProjectsPage() {
  const router = useRouter();
  const [projects, setProjects] = useState<Project[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!authApi.isLoggedIn()) { router.push('/login'); return; }
    load();
  }, [router]);

  const load = async () => {
    setLoading(true); setError(null);
    try {
      const data = await projectsApi.listProjects();
      setProjects(data);
    } catch (e: any) {
      setError(e?.response?.data?.detail || 'فشل التحميل');
    } finally { setLoading(false); }
  };

  return (
    <div className="min-h-screen" dir="rtl">
      <header className="bg-white shadow-sm border-b">
        <div className="max-w-7xl mx-auto px-6 py-4 flex items-center gap-3">
          <Link href="/dashboard" className="text-gray-500 hover:text-gray-700">← رجوع</Link>
          <h1 className="text-2xl font-bold text-gray-800">📊 المشاريع (Projects)</h1>
        </div>
      </header>

      <main className="max-w-7xl mx-auto px-6 py-6">
        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">
            {error}
          </div>
        )}

        {loading ? (
          <div className="text-center py-12 text-gray-500">جاري التحميل...</div>
        ) : projects.length === 0 ? (
          <div className="bg-white rounded-xl shadow-sm p-12 text-center text-gray-500">
            لا توجد مشاريع في هذا الـ tenant.
          </div>
        ) : (
          <div className="space-y-3">
            {projects.map((p) => (
              <div key={p.id} className="bg-white rounded-xl shadow-sm p-5 border-r-4 border-purple-500">
                <div className="flex items-start justify-between">
                  <div>
                    <p className="text-xs text-gray-500 font-mono">{p.code}</p>
                    <h3 className="font-bold text-gray-800 mt-1 text-lg">{p.name}</h3>
                    {p.description && (
                      <p className="text-sm text-gray-500 mt-1">{p.description}</p>
                    )}
                  </div>
                  <span className="bg-purple-50 text-purple-700 px-3 py-1 rounded text-sm">
                    {PROJECT_STATUSES[p.status] || p.status}
                  </span>
                </div>
                <div className="mt-3 pt-3 border-t grid grid-cols-2 gap-4 text-sm">
                  <div>
                    <p className="text-gray-500">الميزانية</p>
                    <p className="font-bold text-gray-800">{p.budget?.toLocaleString()}</p>
                  </div>
                  <div>
                    <p className="text-gray-500">تاريخ البدء</p>
                    <p className="font-mono text-gray-800">
                      {new Date(p.startDate).toLocaleDateString('ar-EG')}
                    </p>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </main>
    </div>
  );
}
