import { describe, expect, it } from 'vitest';
import { fieldAccessibility, mergeAriaIds } from '../src/index.js';

describe('accessible primitive contracts', () => {
  it('deduplicates and normalizes aria id references', () => {
    expect(mergeAriaIds('hint help', 'help error', undefined)).toBe('hint help error');
  });

  it('links invalid fields to both description and error', () => {
    expect(fieldAccessibility(['email-hint'], 'email-error')).toEqual({
      'aria-describedby': 'email-hint email-error',
      'aria-invalid': 'true',
      'aria-errormessage': 'email-error',
    });
  });

  it('does not emit empty optional aria attributes', () => {
    expect(fieldAccessibility([])).toEqual({ 'aria-invalid': 'false' });
  });
});
