import { describe, expect, it } from 'vitest';
import { contrastRatio, defaultTheme, defineTheme, themeToCssVariables } from '../src/theme.js';

describe('design tokens', () => {
  it('ships WCAG AA contrast for primary semantic pairs', () => {
    expect(contrastRatio(defaultTheme.color.text, defaultTheme.color.canvas)).toBeGreaterThanOrEqual(4.5);
    expect(contrastRatio(defaultTheme.color.onAccent, defaultTheme.color.accent)).toBeGreaterThanOrEqual(4.5);
    expect(contrastRatio(defaultTheme.color.onDanger, defaultTheme.color.danger)).toBeGreaterThanOrEqual(4.5);
  });

  it('rejects inaccessible product themes', () => {
    expect(() => defineTheme({
      ...defaultTheme,
      color: { ...defaultTheme.color, text: '#eeeeee', canvas: '#ffffff' },
    })).toThrow(RangeError);
  });

  it('renders stable semantic CSS variable names', () => {
    const variables = themeToCssVariables(defaultTheme);

    expect(variables['--bedrock-color-surface-raised']).toBe('#ffffff');
    expect(variables['--bedrock-typography-display']).toContain('Fraunces');
    expect(Object.keys(variables)).toHaveLength(27);
  });
});
