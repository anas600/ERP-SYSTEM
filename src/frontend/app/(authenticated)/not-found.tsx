'use client';

// 404 page للصفحات المحمية (authenticated)
// يظهر عندما يطلب المستخدم route غير موجود داخل منطقة (authenticated)

import Link from 'next/link';
import { ArrowRight, Search, Users, Package, FileText } from 'lucide-react';
import { Card, Button } from '@/components/ui';

export default function AuthenticatedNotFound() {
  return (
    <div dir="rtl" className="py-8">
      <Card className="max-w-2xl mx-auto text-center py-10">
        <p className="text-8xl font-extrabold text-blue-600 tracking-tight">404</p>
        <h1 className="mt-2 text-2xl font-bold text-gray-800">الصفحة غير موجودة</h1>
        <p className="mt-2 text-sm text-gray-500 max-w-md mx-auto">
          لم نتمكن من العثور على الصفحة التي تبحث عنها. تأكد من الرابط أو استخدم الروابط أدناه.
        </p>

        <div className="mt-6">
          <p className="text-xs text-gray-500 mb-3 flex items-center justify-center gap-1">
            <Search className="h-3 w-3" />
            هل تبحث عن...
          </p>
          <div className="flex items-center justify-center gap-2 flex-wrap">
            <Link href="/finance/customers">
              <Button variant="outline" size="sm" iconLeft={<Users className="h-4 w-4" />}>
                عميل
              </Button>
            </Link>
            <Link href="/inventory/items">
              <Button variant="outline" size="sm" iconLeft={<Package className="h-4 w-4" />}>
                منتج
              </Button>
            </Link>
            <Link href="/reports">
              <Button variant="outline" size="sm" iconLeft={<FileText className="h-4 w-4" />}>
                تقرير
              </Button>
            </Link>
          </div>
        </div>

        <div className="mt-8 pt-6 border-t border-gray-100">
          <Link href="/dashboard">
            <Button variant="primary" iconLeft={<ArrowRight className="h-4 w-4" />}>
              العودة للرئيسية
            </Button>
          </Link>
        </div>
      </Card>
    </div>
  );
}
