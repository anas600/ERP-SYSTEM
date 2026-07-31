'use client';

// Sprint 11 (T1) — New Accounts hub page.
//
// This is a top-level "/accounts" landing page that surfaces the Chart of
// Accounts in the new Sprint 11 DTO shape (string-union enums for `type`
// and `normalBalance`). It complements the existing /finance/accounts page
// (which uses the legacy numeric-enum Account shape) by providing a simpler,
// flat, demo-friendly view focused on the most recent / active accounts.
//
// Contract:
//   GET /api/accounts          → AccountDto[] (new flat DTO)
//
// The page degrades gracefully if the BE endpoint isn't wired yet on the
// parallel branch.

import { useEffect, useMemo, useState } from 'react';
import {
  Wallet,
  Search,
  RefreshCw,
  AlertCircle,
  ChevronDown,
  ChevronRight,
  Filter,
} from 'lucide-react';
import {
  Card,
  PageHeader,
  Button,
  Input,
  Select,
  Badge,
  EmptyState,
} from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { getAccounts, getErrorMessage } from '@/lib/api';
import type { AccountDto, AccountType } from '@/lib/api-types';
import { formatCurrency } from '@/lib/utils';

const TYPE_LABELS: Record<AccountType, { label: string; color: string; chip: string }> = {
  Asset:     { label: 'أصول',          color: 'text-blue-700',   chip: 'bg-blue-50 text-blue-700' },
  Liability: { label: 'خصوم',          color: 'text-red-700',    chip: 'bg-red-50 text-red-700' },
  Equity:    { label: 'حقوق ملكية',    color: 'text-purple-700', chip: 'bg-purple-50 text-purple-700' },
  Revenue:   { label: 'إيرادات',       color: 'text-green-700',  chip: 'bg-green-50 text-green-700' },
  Expense:   { label: 'مصروفات',       color: 'text-orange-700', chip: 'bg-orange-50 text-orange-700' },
};

const TYPE_FILTER_OPTIONS = [
  { value: '', label: 'كل الأنواع' },
  { value: 'Asset', label: 'أصول' },
  { value: 'Liability', label: 'خصوم' },
  { value: 'Equity', label: 'حقوق ملكية' },
  { value: 'Revenue', label: 'إيرادات' },
  { value: 'Expense', label: 'مصروفات' },
];

export default function AccountsHubPage() {
  const { loading: authLoading } = useAuth();
  const [items, setItems] = useState<AccountDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Filters
  const [search, setSearch] = useState('');
  const [typeFilter, setTypeFilter] = useState<string>('');
  const [showInactive, setShowInactive] = useState(false);
  const [expandedIds, setExpandedIds] = useState<Set<string>>(new Set());

  useEffect(() => {
    if (authLoading) return;
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [authLoading, showInactive]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getAccounts();
      setItems(Array.isArray(data) ? data : []);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'تعذّر تحميل الحسابات.'));
      setItems([]);
    } finally {
      setLoading(false);
    }
  };

  // Build a tree from the flat list (client-side; matches the existing
  // /finance/accounts pattern but uses the new DTO shape).
  const tree = useMemo(() => {
    const filtered = items.filter((a) => {
      if (!showInactive && !a.isActive) return false;
      if (typeFilter && a.type !== typeFilter) return false;
      if (search.trim()) {
        const q = search.trim().toLowerCase();
        return (
          a.name.toLowerCase().includes(q) ||
          a.code.toLowerCase().includes(q)
        );
      }
      return true;
    });
    return buildTree(filtered);
  }, [items, showInactive, typeFilter, search]);

  // Auto-expand all root nodes by default. We intentionally only re-run
  // when the root count changes — re-running on every tree object would
  // collapse the user's manual expand/collapse state on each re-render.
  useEffect(() => {
    setExpandedIds(new Set(tree.map((n) => n.id)));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tree.length]);

  const toggleExpanded = (id: string) => {
    setExpandedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  // Summary stats
  const stats = useMemo(() => {
    const active = items.filter((a) => a.isActive).length;
    const postable = items.filter((a) => a.isPostable).length;
    const byType: Record<string, number> = {};
    for (const a of items) {
      byType[a.type] = (byType[a.type] ?? 0) + 1;
    }
    return { total: items.length, active, postable, byType };
  }, [items]);

  return (
    <div>
      <PageHeader
        title="💼 الحسابات"
        description="دليل الحسابات — Chart of Accounts (Sprint 11 T1)"
        actions={
          <Button
            variant="secondary"
            onClick={load}
            disabled={loading}
            iconLeft={<RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />}
          >
            تحديث
          </Button>
        }
      />

      {/* Error banner */}
      {error && (
        <div
          className="bg-amber-50 border border-amber-200 text-amber-800 px-4 py-3 rounded-lg mb-4 flex items-start gap-3"
          role="alert"
        >
          <AlertCircle className="h-5 w-5 flex-shrink-0 mt-0.5" />
          <div className="flex-1">
            <p className="font-semibold">تعذّر تحميل الحسابات</p>
            <p className="text-sm mt-0.5">{error}</p>
            <p className="text-xs mt-1 text-amber-700">
              ملاحظة: قد يكون الـ endpoint الجديد <code>/api/accounts</code> غير مُفعَّل بعد على الفرع المتوازي.
            </p>
          </div>
          <Button variant="secondary" onClick={load} disabled={loading}>
            إعادة المحاولة
          </Button>
        </div>
      )}

      {/* Stats cards */}
      {!loading && items.length > 0 && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4">
          <Card>
            <p className="text-xs text-gray-500">إجمالي الحسابات</p>
            <p className="text-2xl font-bold text-blue-600 mt-1">{stats.total}</p>
          </Card>
          <Card>
            <p className="text-xs text-gray-500">فعّالة</p>
            <p className="text-2xl font-bold text-green-600 mt-1">{stats.active}</p>
          </Card>
          <Card>
            <p className="text-xs text-gray-500">قابلة للترحيل</p>
            <p className="text-2xl font-bold text-purple-600 mt-1">{stats.postable}</p>
          </Card>
          <Card>
            <p className="text-xs text-gray-500">أنواع الحسابات</p>
            <p className="text-2xl font-bold text-gray-700 mt-1">
              {Object.keys(stats.byType).length}
            </p>
          </Card>
        </div>
      )}

      {/* Filters */}
      <Card className="mb-4">
        <div className="flex items-center gap-2 mb-3">
          <Filter className="h-4 w-4 text-gray-500" />
          <span className="text-sm font-medium text-gray-700">فلاتر</span>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-4 gap-3" dir="rtl">
          <Input
            label="بحث"
            placeholder="اسم أو رمز..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            containerClassName="md:col-span-2"
          />
          <Select
            label="النوع"
            value={typeFilter}
            onChange={(e) => setTypeFilter(e.target.value)}
            options={TYPE_FILTER_OPTIONS}
          />
          <div className="flex items-end">
            <label className="flex items-center gap-2 text-sm text-gray-700 pb-2">
              <input
                type="checkbox"
                checked={showInactive}
                onChange={(e) => setShowInactive(e.target.checked)}
                className="rounded"
              />
              <span>إظهار المعطّلة</span>
            </label>
          </div>
        </div>
      </Card>

      {/* Tree */}
      {loading ? (
        <Card>
          <div className="space-y-2">
            {[1, 2, 3, 4].map((i) => (
              <div key={i} className="h-7 bg-gray-100 rounded animate-pulse" />
            ))}
          </div>
        </Card>
      ) : tree.length === 0 ? (
        <Card>
          <EmptyState
            icon={<Wallet className="h-12 w-12" />}
            title="لا توجد حسابات"
            description={
              search || typeFilter
                ? 'لا توجد حسابات تطابق الفلاتر الحالية.'
                : 'لم يتم إنشاء أي حساب بعد.'
            }
          />
        </Card>
      ) : (
        <Card>
          <ul className="space-y-0.5 text-sm" dir="rtl">
            {tree.map((node) => (
              <AccountTreeRow
                key={node.id}
                node={node}
                depth={0}
                expandedIds={expandedIds}
                onToggle={toggleExpanded}
              />
            ))}
          </ul>
        </Card>
      )}
    </div>
  );
}

// ============ Tree types + builder ============

interface AccountTreeNode extends AccountDto {
  children: AccountTreeNode[];
  depth: number;
}

function buildTree(flat: AccountDto[]): AccountTreeNode[] {
  const byId = new Map<string, AccountTreeNode>();
  for (const a of flat) {
    byId.set(a.id, { ...a, children: [], depth: 0 });
  }
  const roots: AccountTreeNode[] = [];
  for (const node of byId.values()) {
    const parentId = node.parentAccountId;
    if (parentId && byId.has(parentId)) {
      const parent = byId.get(parentId)!;
      node.depth = parent.depth + 1;
      parent.children.push(node);
    } else {
      roots.push(node);
    }
  }
  const sortByCode = (a: AccountTreeNode, b: AccountTreeNode) =>
    a.code.localeCompare(b.code, undefined, { numeric: true });
  const recurse = (nodes: AccountTreeNode[]) => {
    nodes.sort(sortByCode);
    for (const n of nodes) recurse(n.children);
  };
  recurse(roots);
  return roots;
}

interface AccountTreeRowProps {
  node: AccountTreeNode;
  depth: number;
  expandedIds: Set<string>;
  onToggle: (id: string) => void;
}

function AccountTreeRow({ node, depth, expandedIds, onToggle }: AccountTreeRowProps) {
  const hasChildren = node.children.length > 0;
  const expanded = expandedIds.has(node.id);
  const typeMeta = TYPE_LABELS[node.type];
  return (
    <li>
      <div
        className="flex items-center gap-2 py-1.5 hover:bg-gray-50 rounded px-1"
        style={{ paddingRight: `${depth * 20}px` }}
      >
        {hasChildren ? (
          <button
            type="button"
            onClick={() => onToggle(node.id)}
            className="text-gray-400 hover:text-gray-700"
            aria-label={expanded ? 'طي' : 'فتح'}
          >
            {expanded ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
          </button>
        ) : (
          <span className="w-4 inline-block" />
        )}
        <span className={`text-xs px-1.5 py-0.5 rounded ${typeMeta.chip}`}>
          {typeMeta.label}
        </span>
        <span className="font-mono text-xs text-gray-500">{node.code}</span>
        <span className="text-gray-800 flex-1 truncate">{node.name}</span>
        {node.isPostable ? (
          <Badge variant="info">قابل للترحيل</Badge>
        ) : (
          <Badge variant="neutral">مجموعة</Badge>
        )}
        {node.isActive ? (
          <Badge variant="success">فعّال</Badge>
        ) : (
          <Badge variant="neutral">معطّل</Badge>
        )}
      </div>
      {hasChildren && expanded && (
        <ul className="space-y-0.5">
          {node.children.map((child) => (
            <AccountTreeRow
              key={child.id}
              node={child}
              depth={depth + 1}
              expandedIds={expandedIds}
              onToggle={onToggle}
            />
          ))}
        </ul>
      )}
    </li>
  );
}
