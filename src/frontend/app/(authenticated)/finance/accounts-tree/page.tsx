'use client';

// Sprint 52a (Phase 4) — 4-Level CoA Tree View.
//
// يعرض دليل الحسابات بشكل شجرة 4-مستويات (L1 Class → L2 Sub-class → L3
// Control → L4 Detail) مع badges ملوّنة لكل مستوى.
//
// الشجرة من endpoint الجديد /api/finance/accounts/tree (DEC-130/131/132
// of Sprint 52a). الافتراضي: كل L1 مفتوح، L2 مفتوح، L3/L4 مطوي —
// المستخدم يفتح/يغلق بزر.
//
// الإحصائيات أسفل الصفحة: كم حساب بكل مستوى.

import { useEffect, useMemo, useState } from 'react';
import Link from 'next/link';
import {
  ChevronRight,
  ChevronDown,
  Folder,
  FolderOpen,
  FileText,
  Layers,
  ArrowLeft,
  Search,
} from 'lucide-react';
import { PageHeader, Card, Input, useToast } from '@/components/ui';
import {
  financeApi,
  AccountTreeNode,
  getErrorMessage,
} from '@/lib/api';
import { cn } from '@/lib/utils';

const LEVEL_META: Record<number, { label: string; color: string; chip: string; bar: string; desc: string }> = {
  1: {
    label: 'L1',
    color: 'text-purple-800',
    chip: 'bg-purple-100 text-purple-800 border-purple-200',
    bar: 'border-r-4 border-purple-500',
    desc: 'الفئة الرئيسية (Class)',
  },
  2: {
    label: 'L2',
    color: 'text-blue-800',
    chip: 'bg-blue-100 text-blue-800 border-blue-200',
    bar: 'border-r-4 border-blue-500',
    desc: 'الفئة الفرعية (Sub-class)',
  },
  3: {
    label: 'L3',
    color: 'text-amber-800',
    chip: 'bg-amber-100 text-amber-800 border-amber-200',
    bar: 'border-r-4 border-amber-500',
    desc: 'حساب وسيط (Control)',
  },
  4: {
    label: 'L4',
    color: 'text-green-800',
    chip: 'bg-green-100 text-green-800 border-green-200',
    bar: 'border-r-4 border-green-500',
    desc: 'حساب تفصيلي (Detail — قابل للترحيل)',
  },
  99: {
    label: '??',
    color: 'text-red-800',
    chip: 'bg-red-100 text-red-800 border-red-200',
    bar: 'border-r-4 border-red-500',
    desc: 'حساب يتيم (parent chain مكسور)',
  },
};

const TYPE_LABEL: Record<string, string> = {
  Asset: 'أصول',
  Liability: 'خصوم',
  Equity: 'حقوق ملكية',
  Revenue: 'إيرادات',
  Expense: 'مصروفات',
};

const TYPE_COLOR: Record<string, string> = {
  Asset: 'bg-blue-50 text-blue-700',
  Liability: 'bg-red-50 text-red-700',
  Equity: 'bg-purple-50 text-purple-700',
  Revenue: 'bg-green-50 text-green-700',
  Expense: 'bg-orange-50 text-orange-700',
};

export default function CoaTreePage() {
  const { error } = useToast();
  const [tree, setTree] = useState<AccountTreeNode[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [expanded, setExpanded] = useState<Set<string>>(new Set());

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    financeApi.getAccountsTree()
      .then((nodes) => {
        if (cancelled) return;
        setTree(nodes);
        // Default: expand all L1 roots + their L2 children
        const initial = new Set<string>();
        for (const n of nodes) {
          initial.add(n.id);
          for (const c of n.children) initial.add(c.id);
        }
        setExpanded(initial);
      })
      .catch((err) => {
        if (cancelled) return;
        error('فشل تحميل شجرة الحسابات: ' + getErrorMessage(err));
      })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [error]);

  // Count accounts by level
  const counts = useMemo(() => {
    const c: Record<number, number> = { 1: 0, 2: 0, 3: 0, 4: 0, 99: 0 };
    const walk = (nodes: AccountTreeNode[]) => {
      for (const n of nodes) {
        c[n.level] = (c[n.level] || 0) + 1;
        walk(n.children);
      }
    };
    walk(tree);
    return c;
  }, [tree]);

  const totalAccounts = useMemo(() => {
    return Object.values(counts).reduce((s, n) => s + n, 0);
  }, [counts]);

  const toggle = (id: string) => {
    setExpanded((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const expandAll = () => {
    const all = new Set<string>();
    const walk = (nodes: AccountTreeNode[]) => {
      for (const n of nodes) { all.add(n.id); walk(n.children); }
    };
    walk(tree);
    setExpanded(all);
  };

  const collapseAll = () => {
    setExpanded(new Set());
  };

  // Filter helper
  const matches = (node: AccountTreeNode): boolean => {
    if (!search.trim()) return true;
    const q = search.toLowerCase();
    if (node.code.toLowerCase().includes(q)) return true;
    if (node.name.toLowerCase().includes(q)) return true;
    return node.children.some(matches);
  };

  return (
    <div className="space-y-6" dir="rtl">
      <PageHeader
        title="شجرة الحسابات الموحدة (4 مستويات)"
        description="الهيكل الهرمي IFRS-compliant — L1 الفئة → L2 الفئة الفرعية → L3 الحساب الوسيط → L4 الحساب التفصيلي"
        actions={
          <Link
            href="/dashboard"
            className="inline-flex items-center gap-2 text-sm text-primary-600 hover:text-primary-700"
          >
            <ArrowLeft className="w-4 h-4 rotate-180" />
            العودة للوحة التحكم
          </Link>
        }
      />

      {/* Level legend */}
      <Card className="p-4">
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
          {[1, 2, 3, 4].map((lvl) => {
            const m = LEVEL_META[lvl];
            return (
              <div key={lvl} className={cn('p-3 rounded-lg border', m.chip)}>
                <div className="flex items-center gap-2">
                  <span className="font-bold text-lg">{m.label}</span>
                  <span className="text-xs opacity-80">{m.desc}</span>
                </div>
                <div className="mt-1 text-2xl font-bold">{counts[lvl] ?? 0} <span className="text-xs font-normal opacity-70">حساب</span></div>
              </div>
            );
          })}
        </div>
        {counts[99] > 0 && (
          <div className="mt-3 p-2 rounded bg-red-50 border border-red-200 text-red-800 text-sm">
            ⚠️ {counts[99]} حساب يتيم (parent chain مكسور) — يحتاج إصلاح يدوي
          </div>
        )}
      </Card>

      {/* Search + actions */}
      <Card className="p-3">
        <div className="flex items-center gap-3 flex-wrap">
          <div className="relative flex-1 min-w-[200px]">
            <Search className="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
            <Input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="بحث بالكود أو الاسم…"
              className="pr-10"
            />
          </div>
          <button
            onClick={expandAll}
            className="text-xs px-3 py-1.5 rounded border border-gray-300 hover:bg-gray-50"
          >
            فتح الكل
          </button>
          <button
            onClick={collapseAll}
            className="text-xs px-3 py-1.5 rounded border border-gray-300 hover:bg-gray-50"
          >
            غلق الكل
          </button>
          <div className="text-xs text-gray-500">
            <Layers className="w-3 h-3 inline ml-1" />
            {totalAccounts} حساب
          </div>
        </div>
      </Card>

      {/* Tree */}
      <Card className="p-2">
        {loading ? (
          <div className="p-8 text-center text-gray-500">جاري التحميل…</div>
        ) : tree.length === 0 ? (
          <div className="p-8 text-center text-gray-500">لا توجد حسابات</div>
        ) : (
          <div className="divide-y divide-gray-100">
            {tree.filter(matches).map((node) => (
              <TreeRow
                key={node.id}
                node={node}
                depth={0}
                expanded={expanded}
                onToggle={toggle}
                search={search}
                matches={matches}
              />
            ))}
          </div>
        )}
      </Card>

      {/* Help */}
      <Card className="p-4 bg-gray-50">
        <h3 className="text-sm font-semibold text-gray-700 mb-2">كيف تقرأ الشجرة؟</h3>
        <ul className="text-xs text-gray-600 space-y-1">
          <li><b>L1 (Class):</b> الفئة الرئيسية — 5 حسابات فقط (الأصول، الالتزامات، حقوق الملكية، الإيرادات، المصروفات). لا تُرحّل مباشرة.</li>
          <li><b>L2 (Sub-class):</b> الفئة الفرعية — تجمع حسابات متجانسة (مثل: أصول متداولة، أصول ثابتة). لا تُرحّل مباشرة.</li>
          <li><b>L3 (Control):</b> الحساب الوسيط — لتجميع حسابات تفصيلية تحت مظلة واحدة. قد يُرحّل أو لا حسب الـ ERP.</li>
          <li><b>L4 (Detail):</b> الحساب التفصيلي — الحساب الذي ترحّل عليه القيود اليومية مباشرة. كل حساب L4 له رصيد.</li>
        </ul>
      </Card>
    </div>
  );
}

interface TreeRowProps {
  node: AccountTreeNode;
  depth: number;
  expanded: Set<string>;
  onToggle: (id: string) => void;
  search: string;
  matches: (node: AccountTreeNode) => boolean;
}

function TreeRow({ node, depth, expanded, onToggle, search, matches }: TreeRowProps) {
  const hasChildren = node.children.length > 0;
  const isOpen = expanded.has(node.id);
  const meta = LEVEL_META[node.level] ?? LEVEL_META[99];

  // If searching and this node doesn't match but has matching children, still show it
  const visibleChildren = search.trim() ? node.children.filter(matches) : node.children;

  return (
    <>
      <div
        className={cn(
          'flex items-center gap-2 py-2 px-3 hover:bg-gray-50 cursor-pointer transition-colors',
          meta.bar,
        )}
        style={{ paddingRight: `${depth * 24 + 12}px` }}
        onClick={() => hasChildren && onToggle(node.id)}
      >
        {/* Expand/collapse icon */}
        {hasChildren ? (
          isOpen ? (
            <ChevronDown className="w-4 h-4 text-gray-500 shrink-0" />
          ) : (
            <ChevronRight className="w-4 h-4 text-gray-500 shrink-0 rotate-180" />
          )
        ) : (
          <span className="w-4 shrink-0" />
        )}

        {/* Folder/file icon */}
        {hasChildren ? (
          isOpen ? <FolderOpen className={cn('w-4 h-4 shrink-0', meta.color)} /> : <Folder className={cn('w-4 h-4 shrink-0', meta.color)} />
        ) : (
          <FileText className={cn('w-4 h-4 shrink-0', meta.color)} />
        )}

        {/* Code */}
        <span className="font-mono text-sm font-semibold text-gray-800 shrink-0 min-w-[60px]">
          {node.code}
        </span>

        {/* Name */}
        <span className="text-sm text-gray-700 flex-1 truncate">
          {node.name}
        </span>

        {/* Type chip */}
        <span className={cn('text-[10px] px-1.5 py-0.5 rounded', TYPE_COLOR[node.type] ?? 'bg-gray-100 text-gray-600')}>
          {TYPE_LABEL[node.type] ?? node.type}
        </span>

        {/* Level chip */}
        <span className={cn('text-[10px] px-1.5 py-0.5 rounded border font-mono font-bold', meta.chip)}>
          {meta.label}
        </span>

        {/* Postable badge */}
        {node.isPostable && (
          <span className="text-[10px] px-1.5 py-0.5 rounded bg-green-50 text-green-700 border border-green-200">
            قابل للترحيل
          </span>
        )}
        {!node.isPostable && (
          <span className="text-[10px] px-1.5 py-0.5 rounded bg-gray-50 text-gray-500 border border-gray-200">
            مُجمِّع
          </span>
        )}

        {/* Children count */}
        {hasChildren && (
          <span className="text-[10px] text-gray-400 shrink-0">
            ({visibleChildren.length})
          </span>
        )}
      </div>

      {/* Recursive children */}
      {hasChildren && isOpen && visibleChildren.map((child) => (
        <TreeRow
          key={child.id}
          node={child}
          depth={depth + 1}
          expanded={expanded}
          onToggle={onToggle}
          search={search}
          matches={matches}
        />
      ))}
    </>
  );
}
