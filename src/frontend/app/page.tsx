'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { authApi } from '@/lib/api';

export default function Home() {
  const [loggedIn, setLoggedIn] = useState(false);
  useEffect(() => {
    setLoggedIn(authApi.isLoggedIn());
  }, []);

  return (
    <main className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100" dir="rtl">
      <div className="max-w-5xl mx-auto px-6 py-16">
        <div className="text-center mb-12">
          <h1 className="text-5xl font-bold text-gray-800 mb-3">🏢 ERP-SYSTEM</h1>
          <p className="text-xl text-gray-600">نظام ERP متكامل - Finance + Projects + Inventory</p>
          <p className="text-sm text-gray-500 mt-2">✅ Multi-tenant | 🔐 JWT Auth | 📊 Reports | 🔄 Event Sourcing</p>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-12">
          <div className="bg-white p-6 rounded-xl shadow-sm border-r-4 border-blue-500">
            <h2 className="text-2xl font-semibold mb-2">💰 Finance</h2>
            <p className="text-gray-600 text-sm">القيود المحاسبية، الفواتير، المدفوعات، التقارير</p>
          </div>
          <div className="bg-white p-6 rounded-xl shadow-sm border-r-4 border-green-500">
            <h2 className="text-2xl font-semibold mb-2">📊 Projects</h2>
            <p className="text-gray-600 text-sm">المشاريع، المهام، الميزانيات، الموارد</p>
          </div>
          <div className="bg-white p-6 rounded-xl shadow-sm border-r-4 border-purple-500">
            <h2 className="text-2xl font-semibold mb-2">📦 Inventory</h2>
            <p className="text-gray-600 text-sm">المنتجات، المخازن، الحركات، التقييم</p>
          </div>
        </div>

        <div className="bg-white rounded-2xl shadow-lg p-8 text-center">
          <h3 className="text-2xl font-bold text-gray-800 mb-4">جاهز للتجربة؟</h3>
          <p className="text-gray-600 mb-6">سجّل شركتك مجاناً واستكشف كل الـ Modules</p>
          <div className="flex gap-3 justify-center">
            {loggedIn ? (
              <Link href="/dashboard" className="bg-blue-600 text-white px-6 py-3 rounded-lg font-semibold hover:bg-blue-700">
                🚀 اذهب للوحة التحكم
              </Link>
            ) : (
              <>
                <Link href="/register" className="bg-green-600 text-white px-6 py-3 rounded-lg font-semibold hover:bg-green-700">
                  📝 إنشاء حساب
                </Link>
                <Link href="/login" className="bg-blue-600 text-white px-6 py-3 rounded-lg font-semibold hover:bg-blue-700">
                  🔐 تسجيل دخول
                </Link>
              </>
            )}
          </div>
          <div className="mt-6 pt-6 border-t">
            <a href="https://6a7b8321c6aab108-47-253-4-207.serveousercontent.com/swagger/index.html"
               target="_blank" rel="noopener noreferrer"
               className="text-blue-600 hover:underline text-sm">
              📘 API Documentation (Swagger) →
            </a>
          </div>
        </div>

        <p className="mt-8 text-center text-xs text-gray-500">
          🚧 Phase 2.5+ | Mavis Mavis | 2026
        </p>
      </div>
    </main>
  );
}
