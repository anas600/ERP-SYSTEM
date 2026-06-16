'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { authApi, UserInfo, financeApi, inventoryApi, projectsApi, Account, Item, Project } from '@/lib/api';

export default function Dashboard() {
  const router = useRouter();
  const [user, setUser] = useState<UserInfo | null>(null);
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [items, setItems] = useState<Item[]>([]);
  const [projects, setProjects] = useState<Project[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!authApi.isLoggedIn()) {
      router.push('/login');
      return;
    }
    setUser(authApi.getUser());
    loadData();
  }, [router]);

  const loadData = async () => {
    try {
      const [a, i, p] = await Promise.allSettled([
        financeApi.listAccounts(),
        inventoryApi.listItems(),
        projectsApi.listProjects(),
      ]);
      if (a.status === 'fulfilled') setAccounts(a.value);
      if (i.status === 'fulfilled') setItems(i.value);
      if (p.status === 'fulfilled') setProjects(p.value);
    } finally {
      setLoading(false);
    }
  };

  const onLogout = () => {
    authApi.logout();
    router.push('/login');
  };

  return (
    <div className="min-h-screen" dir="rtl">
      {/* Header */}
      <header className="bg-white shadow-sm border-b">
        <div className="max-w-7xl mx-auto px-6 py-4 flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-gray-800">🏢 ERP-SYSTEM</h1>
            <p className="text-sm text-gray-500">نظام ERP متكامل</p>
          </div>
          <div className="flex items-center gap-4">
            {user && (
              <div className="text-right">
                <p className="text-sm font-semibold text-gray-800">{user.fullName}</p>
                <p className="text-xs text-gray-500">{user.email}</p>
              </div>
            )}
            <button
              onClick={onLogout}
              className="bg-red-50 text-red-600 px-4 py-2 rounded-lg text-sm font-semibold hover:bg-red-100"
            >
              خروج
            </button>
          </div>
        </div>
      </header>

      <main className="max-w-7xl mx-auto px-6 py-8">
        {/* Stats Cards */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
          <div className="bg-white p-6 rounded-xl shadow-sm border-r-4 border-blue-500">
            <p className="text-sm text-gray-500">💰 الحسابات المحاسبية</p>
            <p className="text-3xl font-bold text-gray-800 mt-1">{accounts.length}</p>
            <Link href="/finance/accounts" className="text-blue-600 text-sm mt-2 inline-block hover:underline">
              عرض الكل →
            </Link>
          </div>
          <div className="bg-white p-6 rounded-xl shadow-sm border-r-4 border-green-500">
            <p className="text-sm text-gray-500">📦 المنتجات</p>
            <p className="text-3xl font-bold text-gray-800 mt-1">{items.length}</p>
            <Link href="/inventory/items" className="text-green-600 text-sm mt-2 inline-block hover:underline">
              عرض الكل →
            </Link>
          </div>
          <div className="bg-white p-6 rounded-xl shadow-sm border-r-4 border-purple-500">
            <p className="text-sm text-gray-500">📊 المشاريع</p>
            <p className="text-3xl font-bold text-gray-800 mt-1">{projects.length}</p>
            <Link href="/projects" className="text-purple-600 text-sm mt-2 inline-block hover:underline">
              عرض الكل →
            </Link>
          </div>
        </div>

        {/* Quick Links */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          <Link href="/finance/accounts" className="bg-white p-6 rounded-xl shadow-sm hover:shadow-md transition-shadow">
            <h3 className="font-bold text-gray-800 mb-1">💰 Finance</h3>
            <p className="text-sm text-gray-500">Chart of Accounts، القيود، الفواتير، التقارير</p>
          </Link>
          <Link href="/projects" className="bg-white p-6 rounded-xl shadow-sm hover:shadow-md transition-shadow">
            <h3 className="font-bold text-gray-800 mb-1">📊 Projects</h3>
            <p className="text-sm text-gray-500">المشاريع، المهام، الميزانيات، الموارد</p>
          </Link>
          <Link href="/inventory/items" className="bg-white p-6 rounded-xl shadow-sm hover:shadow-md transition-shadow">
            <h3 className="font-bold text-gray-800 mb-1">📦 Inventory</h3>
            <p className="text-sm text-gray-500">المنتجات، المخازن، الحركات</p>
          </Link>
          <a href="https://6a7b8321c6aab108-47-253-4-207.serveousercontent.com/swagger/index.html"
             target="_blank"
             rel="noopener noreferrer"
             className="bg-white p-6 rounded-xl shadow-sm hover:shadow-md transition-shadow">
            <h3 className="font-bold text-gray-800 mb-1">📘 API Docs (Swagger)</h3>
            <p className="text-sm text-gray-500">81 endpoint - اختبار APIs</p>
          </a>
        </div>

        {loading && (
          <div className="text-center py-8 text-gray-500">جاري تحميل البيانات...</div>
        )}
      </main>
    </div>
  );
}
