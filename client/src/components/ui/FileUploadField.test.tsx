import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useState } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { FileUploadField } from '@/components/ui/FileUploadField';
import i18n from '@/i18n';

interface FileUploadHarnessProps {
  disabled?: boolean;
  error?: string;
  onFileChange?: (file: File | null) => void;
}

function FileUploadHarness({ disabled = false, error, onFileChange }: FileUploadHarnessProps) {
  const [file, setFile] = useState<File | null>(null);
  const errorProps = error ? { error } : {};

  return (
    <>
      <FileUploadField
        accept="image/*"
        disabled={disabled}
        file={file}
        label="Profile image"
        onFileChange={(nextFile) => {
          setFile(nextFile);
          onFileChange?.(nextFile);
        }}
        {...errorProps}
      />
      <p data-testid="selected-file-name">{file?.name ?? 'none'}</p>
    </>
  );
}

describe('FileUploadField', () => {
  beforeEach(async () => {
    await i18n.changeLanguage('en');
  });

  afterEach(async () => {
    await i18n.changeLanguage('en');
  });

  it('renders empty state when no file is selected', () => {
    render(<FileUploadHarness />);

    expect(screen.getByText('No file selected')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Choose file' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Remove selected file' })).not.toBeInTheDocument();
  });

  it('shows selected file name after upload', async () => {
    const user = userEvent.setup();
    const file = new File(['avatar-content'], 'avatar.png', { type: 'image/png' });

    render(<FileUploadHarness />);

    await user.upload(screen.getByLabelText('Profile image'), file);

    expect(screen.getByTestId('selected-file-name')).toHaveTextContent('avatar.png');
    expect(screen.getByTitle('avatar.png')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Remove selected file' })).toBeInTheDocument();
  });

  it('clears selected file and allows selecting the same file again', async () => {
    const user = userEvent.setup();
    const file = new File(['avatar-content'], 'avatar.png', { type: 'image/png' });
    const onFileChange = vi.fn<(file: File | null) => void>();

    render(<FileUploadHarness onFileChange={onFileChange} />);

    const input = screen.getByLabelText('Profile image');
    await user.upload(input, file);
    await user.click(screen.getByRole('button', { name: 'Remove selected file' }));
    await user.upload(input, file);

    expect(onFileChange).toHaveBeenNthCalledWith(1, file);
    expect(onFileChange).toHaveBeenNthCalledWith(2, null);
    expect(onFileChange).toHaveBeenNthCalledWith(3, file);
    expect(screen.getByTestId('selected-file-name')).toHaveTextContent('avatar.png');
  });

  it('renders disabled and error states', () => {
    render(<FileUploadHarness disabled error="Image is required." />);

    expect(screen.getByRole('button', { name: 'Choose file' })).toBeDisabled();
    expect(screen.getByText('Image is required.')).toBeInTheDocument();
  });
});
