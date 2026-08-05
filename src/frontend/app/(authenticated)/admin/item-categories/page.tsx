'use client';

// قائمة فئات الأصناف (Item Categories) — Tree view + Add/Edit/Delete

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { FolderTree, Pencil, Plus, Trash2 } from 'lucide-react';
import {
  Badge,
  Button,
  ConfirmDialog,
  EmptyState,
  PageHeader,
  SkeletonTable,
  useToast,
} from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { getErrorMessage } from '@/lib/api';

interface ItemCategory {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  parentId?: string | null;
  isActive: boolean;
}

export default function ItemCategoriesPage() {
  const { loading: authLoading } = useAuth();
  const toast = useToast();
  const [items, setItems] = useState<ItemCategory[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<ItemCategory | null>(null);
  const [deleteSubmitting, setDeleteSubmitting] = useState(false);

  useEffect(() => {
    if (authLoading) return;
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [authLoading]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await fetch('/api/inventory/categories', { cache: 'no-store' });
      if (!res.ok) throw new Error('فشل التحميل');
      const data = (await res.json()) as ItemCategory[];
      setItems(data);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل التحميل'));
    } finally {
      setLoading(false);
    }
  };

  // Group by parent
  const roots = items.filter((c) => !c.parentId);
  const childrenOf = (parentId: string) => items.filter((c) => c.parentId === parentId);

  const submitDelete = async () => {
    if (!deleteTarget) return;
    setDeleteSubmitting(true);
    try {
      const res = await fetch(`/api/inventory/categories/${deleteTarget.id}`, {
        method: 'DELETE',
      });
      if (res.status === 404 || res.status === 405) {
        throw new Error('حذف الفئات غير مدعوم في الـ backend حالياً.');
      }
      if (!res.ok) {
        const t = await res.text();
        throw new Error(t || 'فشل الحذف');
      }
      toast.success(`تم حذف الفئة "${deleteTarget.name}".`);
      setDeleteTarget(null);
      await load();
    } catch (e: unknown) {
      toast.error(getErrorMessage(e, 'فشل حذف الفئة.'));
    } finally {
      setDeleteSubmitting(false);
    }
  };

  return (
    <div>
      <PageHeader
        title="📁 فئات الأصناف"
        description="التصنيف الهرمي للأصناف في المخزون"
        actions={
          <div className="flex items-center gap-2">
            <Button onClick={load} variant="secondary" size="sm" disabled={loading}>
              تحديث
            </Button>
            <Link href="/admin/item-categories/new">
              <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />}>
                فئة جديدة
              </Button>
            </Link>
          </div>
        }
      />

      {error && (
        <div className="bg-danger-50 border border-danger-200 text-danger-700 px-4 py-3 rounded-lg mb-4 text-sm">
          {error}
        </div>
      )}

      {loading ? (
        <SkeletonTable rows={5} cols={3} />
      ) : items.length === 0 ? (
        <EmptyState
          icon={<FolderTree className="h-12 w-12" />}
          title="لا توجد فئات"
          description="ابدأ بإنشاء أول فئة لتنظيم الأصناف في المخزون."
          action={
            <Link href="/admin/item-categories/new">
              <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />}>
                فئة جديدة
              </Button>
            </Link>
          }
        />
      ) : (
        <div className="space-y-3">
          {roots.map((root) => {
            const children = childrenOf(root.id);
            return (
              <div key={root.id} className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
                <div className="p-4 border-r-4 border-blue-500">
                  <div className="flex items-start justify-between gap-3">
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 flex-wrap">
                        <p className="text-xs text-gray-500 font-mono">{root.code}</p>
                        <Badge variant={root.isActive ? 'success' : 'neutral'}>
                          {root.isActive ? 'فعّال' : 'معطّل'}
                        </Badge>
                        {children.length > 0 && (
                          <Badge variant="info">{children.length} فئة فرعية</Badge>
                        )}
                      </div>
                      <h3 className="font-bold text-gray-800 mt-1">{root.name}</h3>
                      {root.description && (
                        <p className="text-sm text-gray-500 mt-1">{root.description}</p>
                      )}
                    </div>
                    <div className="flex items-center gap-1 flex-shrink-0">
                      <Link href={`/admin/item-categories/${root.id}/edit`}>
                        <Button variant="ghost" size="sm" iconLeft={<Pencil className="h-3 w-3" />}>
                          تعديل
                        </Button>
                      </Link>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => setDeleteTarget(root)}
                        iconLeft={<Trash2 className="h-3 w-3 text-danger-500" />}
                      >
                        حذف
                      </Button>
                    </div>
                  </div>
                </div>
                {children.length > 0 && (
                  <div className="bg-gray-50/50 border-t border-gray-100 divide-y divide-gray-100">
                    {children.map((c) => (
                      <div
                        key={c.id}
                        className="p-3 ps-10 border-r-4 border-gray-200 flex items-start justify-between gap-3"
                      >
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center gap-2 flex-wrap">
                            <p className="text-xs text-gray-500 font-mono">{c.code}</p>
                            <Badge variant={c.isActive ? 'success' : 'neutral'}>
                              {c.isActive ? 'فعّال' : 'معطّل'}
                            </Badge>
                          </div>
                          <h4 className="font-bold text-gray-800 text-sm mt-0.5">{c.name}</h4>
                          {c.description && (
                            <p className="text-xs text-gray-500 mt-1">{c.description}</p>
                          )}
                        </div>
                        <div className="flex items-center gap-1 flex-shrink-0">
                          <Link href={`/admin/item-categories/${c.id}/edit`}>
                            <Button variant="ghost" size="sm" iconLeft={<Pencil className="h-3 w-3" />}>
                              تعديل
                            </Button>
                          </Link>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => setDeleteTarget(c)}
                            iconLeft={<Trash2 className="h-3 w-3 text-danger-500" />}
                          >
                            حذف
                          </Button>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}

      <ConfirmDialog
        open={!!deleteTarget}
        title="حذف فئة الأصناف"
        message={
          deleteTarget ? (
            <span>
              هل تريد حذف الفئة <b>{deleteTarget.name}</b>؟ إذا كانت تحوي فئات فرعية فقد يفشل الحذف.
            </span>
          ) : null
        }
        confirmLabel="حذف"
        cancelLabel="إلغاء"
        variant="danger"
        loading={deleteSubmitting}
        onConfirm={submitDelete}
        onCancel={() => setDeleteTarget(null)}
      />
    </div>
  );
}
