export default function Home() {
  return (
    <main className="flex min-h-screen flex-col items-center justify-center p-24">
      <div className="z-10 max-w-5xl w-full items-center justify-between font-mono text-sm">
        <h1 className="text-4xl font-bold mb-4">🏢 ERP-SYSTEM</h1>
        <p className="text-xl mb-8">نظام ERP متكامل - Finance + Projects + Inventory</p>
        <div className="grid grid-cols-3 gap-4 mt-8">
          <div className="p-6 border rounded-lg">
            <h2 className="text-2xl font-semibold mb-2">💰 Finance</h2>
            <p>القيود المحاسبية، الفواتير، المدفوعات</p>
          </div>
          <div className="p-6 border rounded-lg">
            <h2 className="text-2xl font-semibold mb-2">📊 Projects</h2>
            <p>المشاريع، المهام، الميزانيات</p>
          </div>
          <div className="p-6 border rounded-lg">
            <h2 className="text-2xl font-semibold mb-2">📦 Inventory</h2>
            <p>المخازن، المنتجات، الحركات</p>
          </div>
        </div>
        <p className="mt-12 text-sm text-gray-500">
          🚧 قيد التطوير - Phase 0: Foundation
        </p>
      </div>
    </main>
  );
}
