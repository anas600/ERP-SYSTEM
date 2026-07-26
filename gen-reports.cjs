// Generate remaining report pages in bulk
const fs = require('fs');
const path = require('path');

const BASE = 'F:/minimaxDescktop2/ERP-SYstem/src/frontend/app/(authenticated)';

const reports = [
  {
    slug: 'account-activity',
    title: 'نشاط الحساب',
    titleEn: 'Account Activity',
    desc: 'Account Activity — حركة حساب محدد',
    api: 'accountActivity',
    renderer: 'account-activity',
  },
  {
    slug: 'journal-entries',
    title: 'دفتر القيود المحاسبية',
    titleEn: 'Journal Entries',
    desc: 'Journal Entries — كل القيود المحاسبية',
    api: 'journalEntries',
    renderer: 'journal-entries',
  },
  {
    slug: 'ap-aging',
    title: 'أعمار الذمم الدائنة',
    titleEn: 'AP Aging',
    desc: 'AP Aging — فواتير الموردين المستحقة',
    api: 'apAging',
    renderer: 'ap-aging',
  },
  {
    slug: 'collections',
    title: 'التحصيلات',
    titleEn: 'Collections',
    desc: 'Collections — أداء تحصيل العملاء',
    api: 'collections',
    renderer: 'collections',
  },
  {
    slug: 'cost-center-performance',
    title: 'أداء مراكز التكلفة',
    titleEn: 'Cost Center Performance',
    desc: 'Cost Center Performance — إيرادات وتكاليف كل مركز',
    api: 'costCenterPerformance',
    renderer: 'cost-center-performance',
  },
];

const template = (r) => `'use client';

import { useEffect, useState } from 'react';
import { ArrowLeft, FileText } from 'lucide-react';
import Link from 'next/link';
import { PageHeader, Card, Button } from '@/components/ui';
import { reportsApi, getErrorMessage } from '@/lib/api';
import { formatDate, formatCurrency } from '@/lib/utils';

export default function ${toPascal(r.slug)}Page() {
  const [from, setFrom] = useState('2025-08-01');
  const [to, setTo] = useState('2026-07-26');
  const [report, setReport] = useState<any>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => { load(); }, []);

  const load = async () => {
    setLoading(true); setError(null);
    try {
      const data = await reportsApi.${r.api}(from, to);
      setReport(data);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل التقرير.'));
    } finally { setLoading(false); }
  };

  return (
    <div>
      <PageHeader
        title="${r.title}"
        description="${r.desc}"
        actions={
          <Link href="/reports/financial">
            <Button variant="secondary" iconLeft={<ArrowLeft className="h-4 w-4" />}>العودة</Button>
          </Link>
        }
      />

      <Card className="p-4 mb-4">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
          <div>
            <label className="text-xs text-gray-500 block mb-1">من تاريخ</label>
            <input type="date" value={from} onChange={(e) => setFrom(e.target.value)} className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" />
          </div>
          <div>
            <label className="text-xs text-gray-500 block mb-1">إلى تاريخ</label>
            <input type="date" value={to} onChange={(e) => setTo(e.target.value)} className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" />
          </div>
          <div className="flex items-end">
            <Button onClick={load} variant="primary" disabled={loading}>
              {loading ? 'جاري التحميل...' : 'تحديث'}
            </Button>
          </div>
        </div>
      </Card>

      {error && <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">{error}</div>}

      {loading ? (
        <Card className="p-12 text-center text-gray-500">جاري التحميل...</Card>
      ) : !report ? (
        <Card className="p-12 text-center text-gray-500">
          <FileText className="h-12 w-12 mx-auto mb-3 text-gray-300" />
          لا توجد بيانات في الفترة المحددة.
        </Card>
      ) : (
        <Card className="p-4">
          <pre className="text-xs bg-gray-50 p-3 rounded overflow-auto" dir="ltr">
            {JSON.stringify(report, null, 2)}
          </pre>
        </Card>
      )}
    </div>
  );
}
`;

function toPascal(slug) {
  return slug.split('-').map(s => s[0].toUpperCase() + s.slice(1)).join('');
}

for (const r of reports) {
  const dir = path.join(BASE, 'reports', 'financial', r.slug);
  fs.mkdirSync(dir, { recursive: true });
  const file = path.join(dir, 'page.tsx');
  fs.writeFileSync(file, template(r), 'utf8');
  console.log('Wrote', file);
}

// Sales reports
const salesReports = [
  { slug: 'sales-by-item', title: 'المبيعات حسب الصنف', desc: 'Sales by Item — المبيعات مجمعة حسب الصنف', api: 'salesByItem' },
  { slug: 'top-customers', title: 'أفضل العملاء', desc: 'Top Customers — أكثر العملاء تحقيقاً للإيرادات', api: 'topCustomers' },
];

for (const r of salesReports) {
  const dir = path.join(BASE, 'reports', 'sales', r.slug);
  fs.mkdirSync(dir, { recursive: true });
  const file = path.join(dir, 'page.tsx');
  fs.writeFileSync(file, template(r), 'utf8');
  console.log('Wrote', file);
}

// Procurement reports
const procReports = [
  { slug: 'purchases-by-vendor', title: 'المشتريات حسب المورد', desc: 'Purchases by Vendor — المشتريات مجمعة حسب المورد', api: 'purchasesByVendor' },
  { slug: 'top-vendors', title: 'أفضل الموردين', desc: 'Top Vendors — أكثر الموردين تعاملاً', api: 'topVendors' },
];

for (const r of procReports) {
  const dir = path.join(BASE, 'reports', 'procurement', r.slug);
  fs.mkdirSync(dir, { recursive: true });
  const file = path.join(dir, 'page.tsx');
  fs.writeFileSync(file, template(r), 'utf8');
  console.log('Wrote', file);
}

console.log('\\nAll report pages created!');
