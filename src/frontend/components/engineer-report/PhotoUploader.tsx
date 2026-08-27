'use client';

// رفع صور تقرير المهندس (Sprint 61, DEC-193)
//
// Uses the native <input type="file" multiple> + URL.createObjectURL previews.
// The actual upload happens in the parent (PhotoUploader is dumb state).
//
// Bilingual (AR + EN).

import { useRef, useEffect, type ChangeEvent } from 'react';
import { Camera, X, Upload } from 'lucide-react';
import { cn } from '@/lib/utils';

export interface PhotoUploaderProps {
  files: File[];
  onChange: (files: File[]) => void;
  maxFiles?: number;
  accept?: string;
  disabled?: boolean;
  className?: string;
}

const DEFAULT_MAX = 10;
const DEFAULT_ACCEPT = 'image/*';

export function PhotoUploader({
  files,
  onChange,
  maxFiles = DEFAULT_MAX,
  accept = DEFAULT_ACCEPT,
  disabled = false,
  className,
}: PhotoUploaderProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const previewsRef = useRef<string[]>([]);

  // Build/rebuild object URLs whenever the file list changes. Revoke old
  // ones first to avoid leaking memory.
  useEffect(() => {
    previewsRef.current.forEach((u) => URL.revokeObjectURL(u));
    previewsRef.current = files.map((f) => URL.createObjectURL(f));
    return () => {
      previewsRef.current.forEach((u) => URL.revokeObjectURL(u));
      previewsRef.current = [];
    };
  }, [files]);

  const remaining = Math.max(0, maxFiles - files.length);
  const canAdd = remaining > 0 && !disabled;

  const handlePick = (e: ChangeEvent<HTMLInputElement>) => {
    const picked = Array.from(e.target.files ?? []);
    if (picked.length === 0) return;
    const merged = [...files, ...picked].slice(0, maxFiles);
    onChange(merged);
    // reset so the same file can be re-picked later
    if (inputRef.current) inputRef.current.value = '';
  };

  const removeAt = (idx: number) => {
    onChange(files.filter((_, i) => i !== idx));
  };

  return (
    <div className={cn('space-y-2', className)} data-testid="photo-uploader">
      {files.length > 0 && (
        <ul
          className="grid grid-cols-2 gap-2 sm:grid-cols-3 md:grid-cols-4"
          aria-label="الصور المختارة"
        >
          {files.map((f, i) => {
            const url = previewsRef.current[i];
            return (
              <li
                key={`${f.name}-${i}`}
                className="group relative overflow-hidden rounded-lg border border-gray-200 bg-gray-50"
                data-testid="photo-thumb"
              >
                {url ? (
                  // eslint-disable-next-line @next/next/no-img-element
                  <img
                    src={url}
                    alt={f.name}
                    className="h-24 w-full object-cover"
                  />
                ) : (
                  <div className="flex h-24 w-full items-center justify-center bg-gray-100 text-xs text-gray-400">
                    {f.name}
                  </div>
                )}
                <button
                  type="button"
                  onClick={() => removeAt(i)}
                  disabled={disabled}
                  className="absolute right-1 top-1 rounded-full bg-white/90 p-1 text-rose-600 shadow hover:bg-white disabled:opacity-50"
                  title="حذف / Remove"
                  data-testid="photo-remove"
                >
                  <X className="h-3.5 w-3.5" />
                </button>
                <div className="truncate px-1.5 py-1 text-[10px] text-gray-600">
                  {f.name} ({(f.size / 1024).toFixed(0)} KB)
                </div>
              </li>
            );
          })}
        </ul>
      )}

      {canAdd && (
        <div>
          <input
            ref={inputRef}
            type="file"
            accept={accept}
            multiple
            onChange={handlePick}
            className="hidden"
            data-testid="photo-input"
            disabled={disabled}
          />
          <button
            type="button"
            onClick={() => inputRef.current?.click()}
            disabled={disabled}
            className="flex w-full items-center justify-center gap-2 rounded-lg border-2 border-dashed border-gray-300 bg-white px-3 py-4 text-sm font-medium text-gray-600 transition hover:border-violet-400 hover:bg-violet-50 hover:text-violet-700 disabled:opacity-50"
            data-testid="photo-add"
          >
            <Camera className="h-4 w-4" />
            <span>
              إضافة صور / Add Photos
              <span className="ms-1 text-xs font-normal text-gray-500">
                ({files.length} / {maxFiles})
              </span>
            </span>
          </button>
        </div>
      )}

      {!canAdd && files.length >= maxFiles && (
        <p className="flex items-center gap-1 text-xs text-amber-700">
          <Upload className="h-3 w-3" /> وصلت للحد الأقصى ({maxFiles} صور).
        </p>
      )}
    </div>
  );
}
