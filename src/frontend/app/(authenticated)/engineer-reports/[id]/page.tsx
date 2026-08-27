'use client';

// صفحة تفاصيل تقرير المهندس (Sprint 61, DEC-192/193/194)
//
// Read-only view of a report + its photos + signoff workflow.
//
// - "Submit" button visible only if status=Draft AND current user is the engineer
// - "SignoffPanel" visible only if status=Submitted AND user is PM or Client
// - Photos gallery always visible (when any photos exist)

import { useEffect, useMemo, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import {
  ArrowRight, ClipboardList, Camera, Send, Calendar, Cloud, ListChecks, MessageCircle, Trash2,
} from 'lucide-react';
import {
  PageHero, SectionCard, EmptyState, SkeletonTable, Button, StatusPill, Card,
} from '@/components/ui';
import {
  projectsApi, getErrorMessage,
  ENGINEER_REPORT_STATUS_LABELS,
  type EngineerReportDto, type EngineerReportPhotoDto, type EngineerReportStatus,
} from '@/lib/api';
import { SignoffPanel } from '@/components/engineer-report/SignoffPanel';
import { PhotoUploader } from '@/components/engineer-report/PhotoUploader';
import { formatDate, formatTime } from '@/lib/utils';

export default function EngineerReportDetailPage() {
  const params = useParams<{ id: string }>();
  const reportId = params.id;
  const router = useRouter();

  const [report, setReport] = useState<EngineerReportDto | null>(null);
  const [photos, setPhotos] = useState<EngineerReportPhotoDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  // Local state for the "add photo" mini-form (only on Draft)
  const [addFiles, setAddFiles] = useState<File[]>([]);
  const [uploading, setUploading] = useState(false);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const r = await projectsApi.getEngineerReport(reportId);
      setReport(r);
      try {
        const ph = await projectsApi.listEngineerReportPhotos(reportId);
        setPhotos(Array.isArray(ph) ? ph : []);
      } catch {
        setPhotos([]);
      }
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل التقرير.'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); /* eslint-disable-next-line react-hooks/exhaustive-deps */ }, [reportId]);

  const statusMeta = useMemo(() => {
    if (!report) return null;
    return ENGINEER_REPORT_STATUS_LABELS[report.status] ?? ENGINEER_REPORT_STATUS_LABELS.Draft;
  }, [report]);

  // Best-effort role detection. The BE enforces the real authorization; here
  // we just gate the UI. Since we don't have a user-store in the demo, we
  // default to allowing PM/Client panels when the report is Submitted.
  const currentUserRoles = useMemo<string[]>(() => {
    if (typeof window === 'undefined') return [];
    try {
      const raw = localStorage.getItem('user');
      if (!raw) return [];
      const u = JSON.parse(raw) as { roles?: string[] };
      return Array.isArray(u.roles) ? u.roles : [];
    } catch {
      return [];
    }
  }, []);

  const isDraft = report?.status === 'Draft';
  const isSubmitted = report?.status === 'Submitted';
  const isApprovedOrRejected =
    report?.status === 'Approved' || report?.status === 'Rejected';

  // Engineer view: assume the current user is the engineer if there's no role
  // OR if they have the Engineer role. In the real app we'd compare ids.
  const isEngineer =
    currentUserRoles.length === 0 || currentUserRoles.includes('Engineer');
  const isPmOrClient = currentUserRoles.some(
    (r) => r === 'PM' || r === 'ProjectManager' || r === 'Client'
  );

  const handleSubmit = async () => {
    if (!report) return;
    if (!confirm('إرسال التقرير للاعتماد؟ لن تتمكن من تعديله بعد ذلك.')) return;
    setBusy(true);
    setError(null);
    try {
      await projectsApi.submitEngineerReport(report.id);
      await load();
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل الإرسال.'));
    } finally {
      setBusy(false);
    }
  };

  const handleUploadPhotos = async () => {
    if (!report || addFiles.length === 0) return;
    setUploading(true);
    setError(null);
    try {
      for (const f of addFiles) {
        await projectsApi.uploadEngineerReportPhoto(report.id, f);
      }
      setAddFiles([]);
      const ph = await projectsApi.listEngineerReportPhotos(report.id);
      setPhotos(Array.isArray(ph) ? ph : []);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل رفع الصور.'));
    } finally {
      setUploading(false);
    }
  };

  const handleSignoff = async (
    req: Parameters<typeof projectsApi.signoffEngineerReport>[1]
  ) => {
    if (!report) return;
    setBusy(true);
    setError(null);
    try {
      await projectsApi.signoffEngineerReport(report.id, req);
      await load();
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل الاعتماد.'));
    } finally {
      setBusy(false);
    }
  };

  if (loading) {
    return (
      <div className="space-y-6">
        <PageHero eyebrow="تقارير المهندس / Engineer Reports" title="جاري التحميل…" tone="violet" />
        <SkeletonTable rows={4} cols={3} />
      </div>
    );
  }

  if (!report) {
    return (
      <div className="space-y-6">
        <PageHero eyebrow="تقارير المهندس / Engineer Reports" title="تقرير غير موجود" tone="violet" />
        <SectionCard>
          <EmptyState
            icon={<ClipboardList className="h-12 w-12" />}
            title="لم يتم العثور على التقرير"
            description="قد يكون التقرير محذوفاً أو ليس لديك صلاحية لعرضه."
            action={
              <Button variant="primary" onClick={() => router.push('/projects')}>
                العودة إلى المشاريع
              </Button>
            }
          />
        </SectionCard>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHero
        eyebrow="تقارير المهندس / Engineer Reports"
        title={`تقرير ${formatDate(report.reportDate)}`}
        subtitle={
          report.engineerName
            ? `بقلم: ${report.engineerName}`
            : `المهندس: ${report.engineerId.slice(0, 8)}`
        }
        tone="violet"
        actions={
          <>
            {isDraft && isEngineer && (
              <Button
                variant="primary"
                onClick={handleSubmit}
                iconLeft={<Send className="h-4 w-4" />}
                disabled={busy}
              >
                {busy ? 'جاري الإرسال…' : 'إرسال للاعتماد / Submit'}
              </Button>
            )}
            <Link href={`/projects/${report.projectId}/engineer-reports`}>
              <Button variant="ghost" iconLeft={<ArrowRight className="h-4 w-4" />}>
                العودة / Back
              </Button>
            </Link>
          </>
        }
        highlight={
          statusMeta
            ? { label: 'الحالة', value: `${statusMeta.ar} / ${statusMeta.en}` }
            : undefined
        }
      />

      {error && (
        <div className="rounded-2xl border border-rose-200 bg-rose-50 p-4 text-rose-700" role="alert">
          <p className="font-semibold">تعذّر تنفيذ العملية</p>
          <p className="mt-1 text-sm">{error}</p>
        </div>
      )}

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
        {/* Status + meta */}
        <div className="lg:col-span-1">
          <SectionCard title="معلومات التقرير / Report Meta">
            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <span className="text-xs text-gray-500">الحالة / Status</span>
                {statusMeta && (
                  <StatusPill tone={statusMeta.tone} label={`${statusMeta.ar} / ${statusMeta.en}`} showDot />
                )}
              </div>
              <MetaRow icon={<Calendar className="h-3.5 w-3.5" />} label="التاريخ" value={formatDate(report.reportDate)} />
              {report.weather && (
                <MetaRow icon={<Cloud className="h-3.5 w-3.5" />} label="الطقس" value={report.weather} />
              )}
              <MetaRow icon={<ListChecks className="h-3.5 w-3.5" />} label="عدد الصور" value={`${photos.length}`} />
              <MetaRow icon={<MessageCircle className="h-3.5 w-3.5" />} label="أنشئ في" value={`${formatDate(report.createdAt)} ${formatTime(report.createdAt)}`} />
              {report.updatedAt && report.updatedAt !== report.createdAt && (
                <MetaRow icon={<MessageCircle className="h-3.5 w-3.5" />} label="آخر تحديث" value={`${formatDate(report.updatedAt)} ${formatTime(report.updatedAt)}`} />
              )}
            </div>
          </SectionCard>
        </div>

        {/* Work done + issues */}
        <div className="lg:col-span-2">
          <SectionCard title="ما تم إنجازه / Work Done">
            <p className="whitespace-pre-wrap text-sm leading-relaxed text-gray-900">
              {report.workDone}
            </p>
          </SectionCard>

          {report.issues && (
            <SectionCard
              title="المشاكل والمعوقات / Issues"
              className="mt-4"
            >
              <p className="whitespace-pre-wrap text-sm leading-relaxed text-gray-900">
                {report.issues}
              </p>
            </SectionCard>
          )}

          {/* Photos gallery */}
          <SectionCard
            title="الصور / Photos"
            description={`${photos.length} صورة مرفقة`}
            className="mt-4"
          >
            {photos.length === 0 ? (
              <p className="text-sm text-gray-500">لا توجد صور مرفقة بهذا التقرير.</p>
            ) : (
              <ul className="grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4">
                {photos.map((p) => (
                  <li
                    key={p.id}
                    className="overflow-hidden rounded-lg border border-gray-200 bg-gray-50"
                  >
                    <a href={p.publicUrl} target="_blank" rel="noopener noreferrer">
                      {/* eslint-disable-next-line @next/next/no-img-element */}
                      <img
                        src={p.publicUrl}
                        alt={p.caption ?? 'صورة التقرير'}
                        className="h-32 w-full object-cover transition hover:scale-105"
                      />
                    </a>
                    {p.caption && (
                      <p className="px-2 py-1 text-[11px] text-gray-600">{p.caption}</p>
                    )}
                  </li>
                ))}
              </ul>
            )}

            {/* Add photos inline only when Draft */}
            {isDraft && (
              <div className="mt-4 border-t border-gray-100 pt-4">
                <p className="mb-2 text-xs font-semibold text-gray-700">إضافة صور إضافية</p>
                <PhotoUploader
                  files={addFiles}
                  onChange={setAddFiles}
                  maxFiles={10}
                  disabled={uploading}
                />
                <div className="mt-2 flex justify-end">
                  <Button
                    variant="primary"
                    onClick={handleUploadPhotos}
                    disabled={addFiles.length === 0 || uploading}
                    iconLeft={<Camera className="h-4 w-4" />}
                  >
                    {uploading ? 'جاري الرفع…' : 'رفع الصور / Upload'}
                  </Button>
                </div>
              </div>
            )}
          </SectionCard>

          {/* Sign-off section */}
          {isSubmitted && (
            <div className="mt-4">
              <SignoffPanel
                canSign={isPmOrClient}
                disabledReason={
                  isPmOrClient
                    ? null
                    : 'الاعتماد متاح فقط للمدير (PM) أو العميل.'
                }
                submitting={busy}
                onSign={handleSignoff}
              />
            </div>
          )}

          {isApprovedOrRejected && (
            <Card
              className={
                'mt-4 p-4 text-sm ' +
                (report.status === 'Approved'
                  ? 'border-emerald-200 bg-emerald-50/40 text-emerald-900'
                  : 'border-rose-200 bg-rose-50/40 text-rose-900')
              }
            >
              <p className="font-semibold">
                {report.status === 'Approved'
                  ? '✓ تم اعتماد هذا التقرير / Approved'
                  : '✗ تم رفض هذا التقرير / Rejected'}
              </p>
              <p className="mt-1 text-xs">
                {report.status === 'Approved'
                  ? 'تم اعتماد التقرير. لن تتمكن من تعديله بعد ذلك.'
                  : 'تم رفض التقرير. تواصل مع المهندس لإعادة التقديم.'}
              </p>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
}

function MetaRow({ icon, label, value }: { icon: React.ReactNode; label: string; value: string }) {
  return (
    <div className="flex items-center justify-between gap-2 border-b border-gray-100 pb-2 last:border-0 last:pb-0">
      <span className="flex items-center gap-1 text-xs text-gray-500">
        {icon} {label}
      </span>
      <span className="text-end text-xs font-medium text-gray-900">{value}</span>
    </div>
  );
}
