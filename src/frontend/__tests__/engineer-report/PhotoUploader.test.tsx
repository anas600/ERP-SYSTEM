/**
 * Sprint 61 (DEC-193): PhotoUploader component unit tests.
 *
 * Two tests:
 *  1. Renders the "Add Photos" button and respects maxFiles.
 *  2. Removes a photo when the X button is clicked.
 */
import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { PhotoUploader } from '@/components/engineer-report/PhotoUploader';

function makeFile(name: string, type = 'image/png'): File {
  return new File([new Uint8Array([1, 2, 3])], name, { type });
}

describe('PhotoUploader (Sprint 61 DEC-193)', () => {
  it('renders the "Add Photos" button when under the max', () => {
    const onChange = jest.fn();
    render(<PhotoUploader files={[]} onChange={onChange} maxFiles={5} />);
    const add = screen.getByTestId('photo-add');
    expect(add).toBeInTheDocument();
    expect(add).toHaveTextContent(/Add Photos/i);
  });

  it('removes a photo when the X button is clicked', () => {
    const onChange = jest.fn();
    const files = [makeFile('a.png'), makeFile('b.png')];
    render(<PhotoUploader files={files} onChange={onChange} maxFiles={5} />);

    const thumbs = screen.getAllByTestId('photo-thumb');
    expect(thumbs).toHaveLength(2);

    const removeButtons = screen.getAllByTestId('photo-remove');
    fireEvent.click(removeButtons[0]);

    expect(onChange).toHaveBeenCalledTimes(1);
    const remaining = onChange.mock.calls[0][0] as File[];
    expect(remaining).toHaveLength(1);
    expect(remaining[0].name).toBe('b.png');
  });
});
