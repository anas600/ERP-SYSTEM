'use client';

// تقرير المهندس — نموذج إنشاء/تعديل (Sprint 61, DEC-192 + DEC-193)
//
// Bilingual (AR + EN). Used on:
//   - /projects/[id]/engineer-reports/new
//   - /engineer-reports/[id]/edit (future)
//
// Props:
//   - initial: optional seed values (edit case)
//   - submitting: busy state from parent
//   - onSubmit: receives the form values + the list of files to upload
//   - onCancel: cancel callback
//
// Photos are kept in component state (files + previews). The parent decides
// whether to upload them inline (create flow) or after the report exists
// (edit flow). For Sprint 61 we upload on save (simpler & idempotent).

import { useState, useMemo, type ChangeEvent, type FormEvent } from 'react';
import { Button, Input } from '@/components/ui';
import { PhotoUploader } from './PhotoUploader';
import { Calendar, FileText, AlertCircle, Save, Send, X } from 'lucide-react';
import { cn } from '@/lib/utils';
import type {
  CreateEngineerReportRequest,
  EngineerReportDto,
} from '@/lib/api';

const MIN_WORK_DONE = 10;
const MAX_PHOTOS = 10;

export interface ReportFormValues {
  reportDate: string;
  weather: string;
  workDone: string;
  issues: string;
  files: File[];
}

export interface ReportFormProps {
  initial?: Partial<EngineerReportDto>;
  submitting?: boolean;
  error?: string | null;
  /** When true, shows the secondary "Save & Submit" button. */
  allowSubmit?: boolean;
  onSubmit: (values: ReportFormValues, submitAfter: boolean) => void | Promise<void>;
  onCancel: () => void;
}

function todayIso(): string {
  // YYYY-MM-DD in local time (avoid toISOString which uses UTC)
  const d = new Date();
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

export function ReportForm({
  initial,
  submitting = false,
  error = null,
  allowSubmit = true,
  onSubmit,
  onCancel,
}: ReportFormProps) {
  const [reportDate, setReportDate] = useState<string>(
    initial?.reportDate ?? todayIso()
  );
  const [weather, setWeather] = useState<string>(initial?.weather ?? '');
  const [workDone, setWorkDone] = useState<string>(initial?.workDone ?? '');
  const [issues, setIssues] = useState<string>(initial?.issues ?? '');
  const [files, setFiles] = useState<File[]>([]);
  const [touched, setTouched] = useState(false);

  const workDoneTrimmed = workDone.trim();
  const workDoneOk = workDoneTrimmed.length >= MIN_WORK_DONE;
  const workDoneCounter = useMemo(
    () => `${workDoneTrimmed.length} / ${MIN_WORK_DONE}+ حرف`,
    [workDoneTrimmed.length]
  );

  const dateOk = !!reportDate && !Number.isNaN(new Date(reportDate).getTime());

  const canSave = dateOk && workDoneOk && files.length <= MAX_PHOTOS && !submitting;

  const handleFilesChange = (next: File[]) => {
    setFiles(next.slice(0, MAX_PHOTOS));
  };

  const handleSubmit = async (e: FormEvent, submitAfter: boolean) => {
    e.preventDefault();
    setTouched(true);
    if (!dateOk || !workDoneOk) return;
    const payload: ReportFormValues = {
      reportDate,
      weather: weather.trim(),
      workDone: workDoneTrimmed,
      issues: issues.trim(),
      files,
    };
    await onSubmit(payload, submitAfter);
  };

  return (
    <form
      className="space-y-4"
      onSubmit={(e) => handleSubmit(e, false)}
      data-testid="report-form"
    >
      {error && (
        <div
          className="flex items-start gap-2 rounded-lg border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-700"
          role="alert"
        >
          <AlertCircle className="mt-0.5 h-4 w-4 flex-shrink-0" />
          <div>
            <p className="font-semibold">تعذّر الحفظ</p>
            <p className="mt-0.5 text-xs">{error}</p>
          </div>
        </div>
      )}

      <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
        <div>
          <label
            htmlFor="report-date"
            className="mb-1 flex items-center gap-1 text-xs font-medium text-gray-700"
          >
            <Calendar className="h-3.5 w-3.5" /> التاريخ / Date
          </label>
          <Input
            id="report-date"
            type="date"
            value={reportDate}
            onChange={(e: ChangeEvent<HTMLInputElement>) =>
              setReportDate(e.target.value)
            }
            required
            data-testid="report-date"
          />
          {touched && !dateOk && (
            <p className="mt-1 text-xs text-rose-600">التاريخ مطلوب.</p>
          )}
        </div>

        <div>
          <label
            htmlFor="report-weather"
            className="mb-1 block text-xs font-medium text-gray-700"
          >
            الطقس / Weather <span className="text-gray-400">(اختياري)</span>
          </label>
          <Input
            id="report-weather"
            type="text"
            value={weather}
            onChange={(e: ChangeEvent<HTMLInputElement>) =>
              setWeather(e.target.value)
            }
            placeholder="مثال: مشمس 28°م"
            maxLength={120}
            data-testid="report-weather"
          />
        </div>
      </div>

      <div>
        <label
          htmlFor="report-work-done"
          className="mb-1 flex items-center justify-between text-xs font-medium text-gray-700"
        >
          <span className="flex items-center gap-1">
            <FileText className="h-3.5 w-3.5" /> ما تم إنجازه / Work Done *
          </span>
          <span
            className={cn(
              'text-[11px] tabular-nums',
              workDoneOk ? 'text-emerald-600' : 'text-gray-500'
            )}
          >
            {workDoneCounter}
          </span>
        </label>
        <textarea
          id="report-work-done"
          rows={5}
          value={workDone}
          onChange={(e: ChangeEvent<HTMLTextAreaElement>) =>
            setWorkDone(e.target.value)
          }
          required
          minLength={MIN_WORK_DONE}
          placeholder="اكتب وصفاً مفصلاً للأعمال التي أنجزت اليوم…"
          data-testid="report-work-done"
          className="block w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm text-gray-900 placeholder:text-gray-400 focus:border-violet-500 focus:outline-none focus:ring-2 focus:ring-violet-500/20"
        />
        {touched && !workDoneOk && (
          <p className="mt-1 text-xs text-rose-600">
            الرجاء إدخال {MIN_WORK_DONE} أحرف على الأقل.
          </p>
        )}
      </div>

      <div>
        <label
          htmlFor="report-issues"
          className="mb-1 block text-xs font-medium text-gray-700"
        >
          المشاكل والمعوقات / Issues <span className="text-gray-400">(اختياري)</span>
        </label>
        <textarea
          id="report-issues"
          rows={3}
          value={issues}
          onChange={(e: ChangeEvent<HTMLTextAreaElement>) =>
            setIssues(e.target.value)
          }
          placeholder="أي مشاكل أو عوائق واجهت العمل اليوم…"
          data-testid="report-issues"
          className="block w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm text-gray-900 placeholder:text-gray-400 focus:border-violet-500 focus:outline-none focus:ring-2 focus:ring-violet-500/20"
        />
      </div>

      <div>
        <p className="mb-1 text-xs font-medium text-gray-700">
          الصور / Photos <span className="text-gray-400">(اختياري، حتى {MAX_PHOTOS} صور)</span>
        </p>
        <PhotoUploader files={files} onChange={handleFilesChange} maxFiles={MAX_PHOTOS} />
      </div>

      <div className="flex flex-col-reverse justify-end gap-2 border-t border-gray-100 pt-3 sm:flex-row">
        <Button
          type="button"
          variant="ghost"
          onClick={onCancel}
          iconLeft={<X className="h-4 w-4" />}
          disabled={submitting}
        >
          إلغاء / Cancel
        </Button>
        <Button
          type="submit"
          variant="secondary"
          disabled={!canSave}
          iconLeft={<Save className="h-4 w-4" />}
          data-testid="save-draft-btn"
        >
          {submitting ? 'جاري الحفظ…' : 'حفظ كمسودة / Save as Draft'}
        </Button>
        {allowSubmit && (
          <Button
            type="button"
            variant="primary"
            disabled={!canSave}
            iconLeft={<Send className="h-4 w-4" />}
            onClick={(e) => handleSubmit(e as unknown as FormEvent, true)}
            data-testid="save-submit-btn"
          >
            {submitting ? 'جاري الإرسال…' : 'حفظ وإرسال / Save & Submit'}
          </Button>
        )}
      </div>
    </form>
  );
}

export function toCreateRequest(
  values: ReportFormValues
): CreateEngineerReportRequest {
  return {
    reportDate: values.reportDate,
    weather: values.weather || null,
    workDone: values.workDone,
    issues: values.issues || null,
  };
}
