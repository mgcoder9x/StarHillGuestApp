export interface DialogController {
  open(trigger?: HTMLElement): void;
  close(returnValue?: string): void;
  destroy(): void;
}

export function createDialogController(
  dialog: HTMLDialogElement,
  initialFocus?: () => HTMLElement | null,
): DialogController {
  let restoreFocus: HTMLElement | null = null;
  const restore = (): void => {
    const target = restoreFocus;
    restoreFocus = null;
    if (target?.isConnected) target.focus();
  };
  dialog.addEventListener('close', restore);

  return {
    open(trigger) {
      if (dialog.open) return;
      restoreFocus = trigger ?? (document.activeElement instanceof HTMLElement ? document.activeElement : null);
      dialog.showModal();
      queueMicrotask(() => (initialFocus?.() ?? firstFocusable(dialog) ?? dialog).focus());
    },
    close(returnValue = '') {
      if (dialog.open) dialog.close(returnValue);
    },
    destroy() {
      dialog.removeEventListener('close', restore);
      restoreFocus = null;
    },
  };
}

export function mergeAriaIds(...groups: ReadonlyArray<string | null | undefined>): string | undefined {
  const ids = [...new Set(groups.flatMap((group) => group?.trim().split(/\s+/) ?? []).filter(Boolean))];
  return ids.length === 0 ? undefined : ids.join(' ');
}

export function fieldAccessibility(
  descriptionIds: ReadonlyArray<string | null | undefined>,
  errorId?: string,
): Readonly<{ 'aria-describedby'?: string; 'aria-invalid': 'true' | 'false'; 'aria-errormessage'?: string }> {
  const describedBy = mergeAriaIds(...descriptionIds, errorId);
  return Object.freeze({
    ...(describedBy ? { 'aria-describedby': describedBy } : {}),
    'aria-invalid': errorId ? 'true' : 'false',
    ...(errorId ? { 'aria-errormessage': errorId } : {}),
  });
}

function firstFocusable(root: HTMLElement): HTMLElement | null {
  return root.querySelector<HTMLElement>(
    'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])',
  );
}
