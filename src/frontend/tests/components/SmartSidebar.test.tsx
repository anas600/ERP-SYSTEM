// Sprint 63 (DEC-218) — SmartSidebar tests.
//
// Covers:
//   1. Hides modules the user cannot see
//   2. Shows all 10 modules for an Admin
//   3. Shows a loading state initially (no flicker)
//   4. Highlights the active route
//   5. Handles the empty-state (no visible modules) gracefully

import { render, screen, waitFor } from '@testing-library/react';
import { SmartSidebar } from '@/components/layout/SmartSidebar';
import { fetchVisibleModules } from '@/lib/api/module-visibility';

jest.mock('@/lib/api/module-visibility');
const mockedFetch = fetchVisibleModules as jest.MockedFunction<typeof fetchVisibleModules>;

// Mock next/navigation's usePathname so we can drive the active-route test.
const mockUsePathname = jest.fn(() => '/dashboard');
jest.mock('next/navigation', () => ({
  usePathname: () => mockUsePathname(),
}));

describe('SmartSidebar', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockUsePathname.mockReturnValue('/dashboard');
  });

  it('hides modules the user cannot see (HR user view)', async () => {
    mockedFetch.mockResolvedValue(['Dashboard', 'HR', 'Companies', 'Payroll']);
    render(<SmartSidebar open onClose={() => {}} />);

    // Wait for the BE fetch to resolve + the sidebar to render.
    await waitFor(() => {
      expect(screen.queryByText('جاري التحميل...')).not.toBeInTheDocument();
    });

    // HR should see the HR-module links (e.g. /hr/employees).
    expect(screen.getByRole('link', { name: 'الموظفين' })).toBeInTheDocument();
    // HR should NOT see the Projects link.
    expect(screen.queryByRole('link', { name: 'المشاريع' })).not.toBeInTheDocument();
    // HR should NOT see the Finance accounts link.
    expect(screen.queryByRole('link', { name: 'دليل الحسابات' })).not.toBeInTheDocument();
  });

  it('shows all 10 modules for an Admin', async () => {
    mockedFetch.mockResolvedValue([
      'Dashboard', 'Projects', 'Finance', 'AR', 'Inventory',
      'Procurement', 'HR', 'Payroll', 'Companies', 'Identity',
    ]);
    render(<SmartSidebar open onClose={() => {}} />);

    await waitFor(() => {
      expect(screen.getByRole('link', { name: 'الموظفين' })).toBeInTheDocument();
    });

    // Spot-check a few module links by their visible text (not the group header).
    expect(screen.getByRole('link', { name: 'المشاريع' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'دليل الحسابات' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'الأصناف' })).toBeInTheDocument();      // Inventory
    expect(screen.getByRole('link', { name: 'الموردين' })).toBeInTheDocument();     // Procurement
  });

  it('shows a loading state while the fetch is in flight', () => {
    // Never-resolving promise = the request is still in flight when we assert.
    // Cast through unknown so the type is wider than ModuleCode[] (this
    // never resolves anyway, so we don't need a realistic value).
    mockedFetch.mockReturnValue(new Promise<never>(() => {}));
    render(<SmartSidebar open onClose={() => {}} />);

    expect(screen.getByText('جاري التحميل...')).toBeInTheDocument();
    // No navigation links should be rendered yet.
    expect(screen.queryByRole('link', { name: 'المشاريع' })).not.toBeInTheDocument();
  });

  it('highlights the active route', async () => {
    mockedFetch.mockResolvedValue(['Dashboard', 'Projects']);
    mockUsePathname.mockReturnValue('/projects');
    render(<SmartSidebar open onClose={() => {}} />);

    // Wait for the link to render.
    await waitFor(() => {
      expect(screen.getByRole('link', { name: 'المشاريع' })).toBeInTheDocument();
    });

    // The active link should be the one whose href is /projects.
    const projectsLink = screen.getByRole('link', { name: 'المشاريع' });
    expect(projectsLink).toHaveAttribute('href', '/projects');
    expect(projectsLink).toHaveClass('bg-brand-50');
    expect(projectsLink).toHaveClass('text-brand-700');
  });

  it('renders the empty-state copy when the user sees no modules', async () => {
    mockedFetch.mockResolvedValue([]); // no modules visible
    render(<SmartSidebar open onClose={() => {}} />);

    await waitFor(() => {
      expect(
        screen.getByText(/لا توجد وحدات متاحة لك/i),
      ).toBeInTheDocument();
    });
  });
});
