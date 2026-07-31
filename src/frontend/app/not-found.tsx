'use client';

// Root not-found — 404 على مستوى الـ root layout
// (يظهر عند عدم تطابق أي route في كامل التطبيق)

import Link from 'next/link';
import { ArrowRight } from 'lucide-react';

export default function RootNotFound() {
  return (
    <div
      dir="rtl"
      className="min-h-screen flex items-center justify-center bg-gray-50 p-6"
    >
      <div className="max-w-md w-full bg-white rounded-2xl shadow-md border border-gray-100 p-8 text-center">
        <p className="text-7xl font-extrabold text-blue-600 tracking-tight">404</p>
        <h1 className="mt-2 text-xl font-bold text-gray-800">الصفحة غير موجودة</h1>
        <p className="mt-2 text-sm text-gray-500">
          لم نتمكن من العثور على الصفحة التي تبحث عنها.
        </p>
        <Link
          href="/"
          className="mt-6 inline-flex items-center gap-2 h-10 px-4 text-sm font-semibold rounded-lg bg-blue-600 text-white hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-1 focus:ring-blue-500"
        >
          <ArrowRight className="h-4 w-4" />
          العودة للرئيسية
        </Link>
      </div>
    </div>
  );
}
