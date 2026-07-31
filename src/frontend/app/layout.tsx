import './globals.css';
import { ToastProvider } from '@/components/ui/Toast';

export const metadata = {
  title: 'ERP-SYSTEM',
  description: 'نظام ERP متكامل - Finance + Projects + Inventory',
  icons: {
    icon: '/favicon.svg',
  },
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="ar" dir="rtl">
      <body className="bg-gray-50 text-gray-900 min-h-screen">
        <ToastProvider>{children}</ToastProvider>
      </body>
    </html>
  );
}
