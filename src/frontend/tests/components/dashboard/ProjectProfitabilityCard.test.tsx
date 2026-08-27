// Sprint 65 / Wave 2A (DEC-236) — ProjectProfitabilityCard component tests.
//
// Covers:
//   1. Renders the top-N projects sorted by grossProfit desc
//   2. Renders the health-status pill (OK / AT_RISK / OVER_BUDGET)
//   3. Shows an empty-state when the projects array is empty

import { render, screen } from '@testing-library/react';
import { ProjectProfitabilityCard } from '@/components/dashboard/ProjectProfitabilityCard';
import type { ProjectProfitabilityResponse } from '@/lib/api-types';

const PROJECTS: ProjectProfitabilityResponse[] = [
  {
    projectId: 'p1', projectCode: 'PRJ-001', projectName: 'Project Alpha',
    totalRevenue: 100_000, totalCosts: 60_000, grossProfit: 40_000,
    profitMarginPercent: 40, healthStatus: 'OK',
  },
  {
    projectId: 'p2', projectCode: 'PRJ-002', projectName: 'Project Beta',
    totalRevenue: 200_000, totalCosts: 180_000, grossProfit: 20_000,
    profitMarginPercent: 10, healthStatus: 'AT_RISK',
  },
  {
    projectId: 'p3', projectCode: 'PRJ-003', projectName: 'Project Gamma',
    totalRevenue: 50_000, totalCosts: 80_000, grossProfit: -30_000,
    profitMarginPercent: -60, healthStatus: 'OVER_BUDGET',
  },
];

describe('ProjectProfitabilityCard', () => {
  it('renders the top-N projects by grossProfit desc', () => {
    render(<ProjectProfitabilityCard projects={PROJECTS} limit={2} />);
    // Alpha (40k) should appear, Beta (20k) should appear, Gamma (-30k) should NOT
    expect(screen.getByText('Project Alpha')).toBeInTheDocument();
    expect(screen.getByText('Project Beta')).toBeInTheDocument();
    expect(screen.queryByText('Project Gamma')).not.toBeInTheDocument();
  });

  it('renders the health-status pill for each project', () => {
    render(<ProjectProfitabilityCard projects={PROJECTS} limit={3} />);
    expect(screen.getByText('سليم')).toBeInTheDocument();
    expect(screen.getByText('في خطر')).toBeInTheDocument();
    expect(screen.getByText('تجاوز الميزانية')).toBeInTheDocument();
  });

  it('shows an empty state when no projects are passed', () => {
    render(<ProjectProfitabilityCard projects={[]} limit={5} />);
    expect(screen.getByText(/لا توجد مشاريع لعرضها/i)).toBeInTheDocument();
  });
});
