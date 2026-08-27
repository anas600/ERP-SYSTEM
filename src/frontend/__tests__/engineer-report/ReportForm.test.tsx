/**
 * Sprint 61 (DEC-192): ReportForm component unit tests.
 *
 * Two tests:
 *  1. Renders empty form with all bilingual labels and disables Save until valid.
 *  2. Calls onSubmit with the collected values + submitAfter flag.
 */
import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ReportForm } from '@/components/engineer-report/ReportForm';

describe('ReportForm (Sprint 61 DEC-192)', () => {
  it('renders the bilingual form and disables Save until work_done is long enough', async () => {
    const onSubmit = jest.fn();
    const onCancel = jest.fn();
    render(<ReportForm onSubmit={onSubmit} onCancel={onCancel} />);

    // Field labels (bilingual)
    expect(screen.getByLabelText(/التاريخ/)).toBeInTheDocument();
    expect(screen.getByLabelText(/الطقس/)).toBeInTheDocument();
    expect(screen.getByLabelText(/ما تم إنجازه/)).toBeInTheDocument();
    expect(screen.getByLabelText(/المشاكل/)).toBeInTheDocument();

    // Buttons
    const saveDraft = screen.getByTestId('save-draft-btn');
    const saveSubmit = screen.getByTestId('save-submit-btn');
    expect(saveDraft).toBeInTheDocument();
    expect(saveSubmit).toBeInTheDocument();

    // Initially disabled (no work_done yet)
    expect(saveDraft).toBeDisabled();
    expect(saveSubmit).toBeDisabled();

    // Type a too-short work_done
    const workDone = screen.getByTestId('report-work-done') as HTMLTextAreaElement;
    fireEvent.change(workDone, { target: { value: 'قصير' } });
    expect(saveDraft).toBeDisabled();

    // Type a valid work_done
    fireEvent.change(workDone, {
      target: { value: 'تم صب الخرسانة في الجناح الشرقي بكمية 25 متر مكعب' },
    });
    await waitFor(() => expect(saveDraft).not.toBeDisabled());

    // No submit yet
    expect(onSubmit).not.toHaveBeenCalled();
  });

  it('calls onSubmit with form values + submitAfter=false on "Save as Draft"', async () => {
    const onSubmit = jest.fn();
    const onCancel = jest.fn();
    const user = userEvent.setup();
    render(<ReportForm onSubmit={onSubmit} onCancel={onCancel} />);

    // Fill the form
    await user.type(
      screen.getByTestId('report-work-done'),
      'تم تركيب 12 نافذة في الواجهة الغربية'
    );
    await user.type(
      screen.getByTestId('report-weather'),
      'مشمس 30م'
    );
    await user.type(
      screen.getByTestId('report-issues'),
      'لا توجد مشاكل'
    );

    // Click "Save as Draft"
    const btn = screen.getByTestId('save-draft-btn');
    await waitFor(() => expect(btn).not.toBeDisabled());
    await user.click(btn);

    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalledTimes(1);
    });
    const [values, submitAfter] = onSubmit.mock.calls[0];
    expect(submitAfter).toBe(false);
    expect(values.workDone).toContain('12 نافذة');
    expect(values.weather).toBe('مشمس 30م');
    expect(values.issues).toBe('لا توجد مشاكل');
    expect(values.reportDate).toMatch(/^\d{4}-\d{2}-\d{2}$/);
    expect(Array.isArray(values.files)).toBe(true);
  });
});
