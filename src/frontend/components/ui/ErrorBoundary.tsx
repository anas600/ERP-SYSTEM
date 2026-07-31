'use client';

// React class-based ErrorBoundary — catches render-time errors in client
// component trees that wrap it. Complementary to the Next.js route-level
// `error.tsx` files (which catch errors at the framework boundary); this
// component is useful for isolating a specific feature area (e.g. a chart
// widget or a third-party embed) so its failure doesn't break the page.
//
// Default fallback is a bilingual (AR primary) alert with the error
// message. Pass a custom `fallback` to override.

import React from 'react';
import { AlertTriangle, RefreshCw } from 'lucide-react';
import { useTranslation } from '@/lib/i18n';

interface Props {
  children: React.ReactNode;
  /** Custom fallback UI. Receives `error` + a `reset` callback. */
  fallback?: React.ReactNode | ((args: { error?: Error; reset: () => void }) => React.ReactNode);
}

interface State {
  hasError: boolean;
  error?: Error;
}

export class ErrorBoundary extends React.Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, info: React.ErrorInfo): void {
    // Centralised log point — wire to Sentry / monitoring in the future.
    // Keep it silent in production to avoid leaking internals to the console.
    if (process.env.NODE_ENV !== 'production') {
      console.error('ErrorBoundary caught:', error, info.componentStack);
    }
  }

  private handleReset = (): void => {
    this.setState({ hasError: false, error: undefined });
  };

  render(): React.ReactNode {
    if (!this.state.hasError) return this.props.children;

    const { fallback } = this.props;
    if (typeof fallback === 'function') {
      return fallback({ error: this.state.error, reset: this.handleReset });
    }
    if (fallback !== undefined) return fallback;

    return <DefaultErrorFallback error={this.state.error} reset={this.handleReset} />;
  }
}

function DefaultErrorFallback({ error, reset }: { error?: Error; reset: () => void }) {
  const t = useTranslation();
  return (
    <div
      role="alert"
      dir="rtl"
      className="p-4 m-4 border border-red-300 bg-red-50 rounded-lg flex items-start gap-3"
    >
      <AlertTriangle className="h-5 w-5 text-red-600 flex-shrink-0 mt-0.5" />
      <div className="flex-1 min-w-0">
        <h2 className="text-base font-semibold text-red-700">{t('error.unexpected')}</h2>
        <p className="text-sm text-red-600 mt-1">
          عذراً، حدث خطأ أثناء عرض هذا المكوّن. حاول إعادة المحاولة.
        </p>
        {process.env.NODE_ENV !== 'production' && error?.message && (
          <pre className="mt-2 text-xs text-red-500 whitespace-pre-wrap break-words" dir="ltr">
            {error.message}
          </pre>
        )}
        <button
          type="button"
          onClick={reset}
          className="mt-3 inline-flex items-center gap-1.5 h-8 px-3 text-xs font-semibold rounded-md bg-red-600 text-white hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-offset-1 focus:ring-red-500"
        >
          <RefreshCw className="h-3.5 w-3.5" />
          إعادة المحاولة
        </button>
      </div>
    </div>
  );
}

export default ErrorBoundary;
