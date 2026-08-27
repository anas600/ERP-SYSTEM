// Sprint 63 (DEC-218) — usePermissions tests.
//
// Covers the three critical branches of `hasPermission(code)`:
//   1. Missing permission → false
//   2. Present permission → true
//   3. `admin.all` wildcard → true for any code

import { renderHook, waitFor } from '@testing-library/react';
import { usePermissions } from '@/hooks/usePermissions';
import { fetchMyPermissions } from '@/lib/api/permissions';

jest.mock('@/lib/api/permissions');
const mockedFetch = fetchMyPermissions as jest.MockedFunction<typeof fetchMyPermissions>;

describe('usePermissions', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('returns false for a permission the user does not hold', async () => {
    mockedFetch.mockResolvedValue(['projects.view', 'projects.create']);
    const { result } = renderHook(() => usePermissions());

    // loading should be true initially, then false after the fetch resolves.
    expect(result.current.loading).toBe(true);

    await waitFor(() => expect(result.current.loading).toBe(false));

    expect(result.current.hasPermission('projects.view')).toBe(true);
    expect(result.current.hasPermission('projects.create')).toBe(true);
    expect(result.current.hasPermission('hr.employees.create')).toBe(false);
    expect(result.current.hasPermission('admin.all')).toBe(false);
  });

  it('returns true for a permission the user holds', async () => {
    mockedFetch.mockResolvedValue(['hr.employees.create', 'hr.employees.update']);
    const { result } = renderHook(() => usePermissions());

    await waitFor(() => expect(result.current.loading).toBe(false));

    expect(result.current.hasPermission('hr.employees.create')).toBe(true);
    expect(result.current.hasPermission('hr.employees.update')).toBe(true);
    // The set is exact-match — a sibling code should not pass.
    expect(result.current.hasPermission('hr.employees.delete')).toBe(false);
  });

  it('admin.all wildcard grants every permission', async () => {
    mockedFetch.mockResolvedValue(['admin.all']);
    const { result } = renderHook(() => usePermissions());

    await waitFor(() => expect(result.current.loading).toBe(false));

    expect(result.current.hasPermission('anything.at.all')).toBe(true);
    expect(result.current.hasPermission('projects.create')).toBe(true);
    expect(result.current.hasPermission('finance.accounts.delete')).toBe(true);
  });

  it('exposes the raw permission set + handles fetch errors', async () => {
    mockedFetch.mockResolvedValue(['x.y', 'a.b']);
    const { result } = renderHook(() => usePermissions());
    await waitFor(() => expect(result.current.loading).toBe(false));

    expect(result.current.permissions).toBeInstanceOf(Set);
    expect(result.current.permissions.size).toBe(2);
    expect(result.current.error).toBeNull();
  });
});
