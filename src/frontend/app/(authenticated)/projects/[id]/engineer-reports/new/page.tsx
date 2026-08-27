'use client';

// صفحة إنشاء تقرير المهندس (Sprint 61, DEC-192)
//
// - On save-as-draft: create report then upload each photo sequentially
// - On save-and-submit: create report, upload photos, then call /submit
// - On any error, surface a friendly message and stay on the form
//
// Bilingual (AR + EN).

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import { ArrowRight } from 'lucide-react';
import {
  PageHero, SectionCard, Button, Card,
} from '@/components/ui';
import {
  projectsApi, getErrorMessage, type Project,
} from '@/lib/api';
import {
  ReportForm, toCreateRequest, type ReportFormValues,
} from '@/components/engineer-report/ReportForm';

export default function NewEngineerReportPage() {
  const params = useParams<{ id: string }>();
  const projectId = params.id;
  const router = useRouter();

  const [project, setProject] = useState<Project | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    projectsApi.getProject(projectId)
      .then(setProject)
      .catch(() => setProject(null));
  }, [projectId]);

  const handleSubmit = async (values: ReportFormValues, submitAfter: boolean) => {
    setSubmitting(true);
    setError(null);
    try {
      // 1. Create the report (Draft)
      const created = await projectsApi.createEngineerReport(
        projectId,
        toCreateRequest(values)
      );
      const reportId = created.id;

      // 2. Upload photos (best-effort — fail the whole flow if any upload errors)
      for (const file of values.files) {
        await projectsApi.uploadEngineerReportPhoto(reportId, file);
      }

      // 3. Optionally submit
      if (submitAfter) {
        await projectsApi.submitEngineerReport(reportId);
      }

      router.push(`/engineer-reports/${reportId}`);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل إنشاء التقرير.'));
      setSubmitting(false);
    }
  };

  return (
    <div className="space-y-6">
      <PageHero
        eyebrow="تقارير المهندس / Engineer Reports"
        title="تقرير جديد / New Report"
        subtitle={
          project
            ? `${project.code} — ${project.name}`
            : 'أنشئ تقرير المهندس اليومي'
        }
        tone="violet"
        actions={
          <Link href={`/projects/${projectId}/engineer-reports`}>
            <Button variant="ghost" iconLeft={<ArrowRight className="h-4 w-4" />}>
              العودة / Back
            </Button>
          </Link>
        }
      />

      <SectionCard
        title="بيانات التقرير / Report Details"
        description="سيتم حفظ التقرير كمسودة. يمكنك إرساله للاعتماد لاحقاً."
      >
        <ReportForm
          onSubmit={handleSubmit}
          onCancel={() => router.push(`/projects/${projectId}/engineer-reports`)}
          submitting={submitting}
          error={error}
        />
      </SectionCard>

      <Card className="border-blue-200 bg-blue-50/40 p-4 text-sm text-blue-900">
        <p className="font-semibold">ملاحظة / Note</p>
        <p className="mt-1 text-xs">
          بعد الحفظ، ستنتقل إلى صفحة التفاصيل حيث يمكنك إرسال التقرير للاعتماد أو إضافة صور إضافية.
        </p>
      </Card>
    </div>
  );
}
