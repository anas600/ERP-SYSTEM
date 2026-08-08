// مُصدّر مركزي لمكونات الـ UI
// استخدم: import { Button, Card, Table } from '@/components/ui';

export { Button } from './Button';
export type { ButtonProps, ButtonVariant, ButtonSize } from './Button';

export { Input } from './Input';
export type { InputProps } from './Input';

export { Select } from './Select';
export type { SelectProps, SelectOption } from './Select';

export { Badge } from './Badge';
export type { BadgeProps, BadgeVariant, BadgeSize } from './Badge';

export { Card } from './Card';
export type { CardProps } from './Card';

export { Table } from './Table';
export type { TableProps, TableColumn } from './Table';

export { Modal } from './Modal';
export type { ModalProps, ModalSize } from './Modal';

export { PageHeader } from './PageHeader';
export type { PageHeaderProps } from './PageHeader';

// مكونات Polish (EmptyState, Loading skeletons, Toast viewport, Confirm dialog)
export { EmptyState } from './EmptyState';
export type { EmptyStateProps } from './EmptyState';

export {
  Skeleton,
  SkeletonCard,
  SkeletonTable,
  SkeletonPage,
} from './LoadingSkeleton';
export type {
  SkeletonProps,
  SkeletonCardProps,
  SkeletonTableProps,
  SkeletonPageProps,
} from './LoadingSkeleton';

// ملاحظة: مكوّن Toast نفسه (الـ viewport) والـ useToast hook
// الـ Provider مُصدَّر من '@/components/ui/Toast'
// الـ hook useToast() + الأنواع مُصدَّرة من '@/lib/useToast'
export { ToastProvider } from './Toast';
export { useToast } from '@/lib/useToast';
export type {
  Toast,
  ToastType,
  ShowOptions,
  ToastContextValue,
  ToastProviderProps,
} from '@/lib/useToast';

export { ConfirmDialog } from './ConfirmDialog';
export type { ConfirmDialogProps, ConfirmDialogVariant } from './ConfirmDialog';

// Sprint 59 — Modern design system for /inventory/* pages.
export { StatCard } from './StatCard';
export type { StatCardProps, StatCardTone } from './StatCard';

export { PageHero } from './PageHero';
export type { PageHeroProps } from './PageHero';

export { StatusPill, ProgressBar, SectionCard } from './StatusPill';
export type {
  StatusPillProps,
  StatusPillTone,
  ProgressBarProps,
  SectionCardProps,
} from './StatusPill';
