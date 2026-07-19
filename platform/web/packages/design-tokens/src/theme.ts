export interface ThemeTokens {
  readonly color: {
    readonly canvas: string;
    readonly surface: string;
    readonly surfaceRaised: string;
    readonly text: string;
    readonly textMuted: string;
    readonly border: string;
    readonly accent: string;
    readonly onAccent: string;
    readonly danger: string;
    readonly onDanger: string;
    readonly focus: string;
  };
  readonly typography: {
    readonly display: string;
    readonly body: string;
    readonly mono: string;
  };
  readonly radius: {
    readonly small: string;
    readonly medium: string;
    readonly large: string;
    readonly pill: string;
  };
  readonly space: {
    readonly x1: string;
    readonly x2: string;
    readonly x3: string;
    readonly x4: string;
    readonly x6: string;
    readonly x8: string;
    readonly x12: string;
  };
  readonly shadow: {
    readonly raised: string;
    readonly overlay: string;
  };
}

export const defaultTheme = defineTheme({
  color: {
    canvas: '#f4f1e8',
    surface: '#fffdf7',
    surfaceRaised: '#ffffff',
    text: '#17231b',
    textMuted: '#536158',
    border: '#c9d0c8',
    accent: '#146b4f',
    onAccent: '#ffffff',
    danger: '#a72b20',
    onDanger: '#ffffff',
    focus: '#b55d00',
  },
  typography: {
    display: '"Fraunces", "Iowan Old Style", serif',
    body: '"Source Sans 3", "Segoe UI", sans-serif',
    mono: '"IBM Plex Mono", "Cascadia Code", monospace',
  },
  radius: { small: '0.375rem', medium: '0.75rem', large: '1.25rem', pill: '999px' },
  space: {
    x1: '0.25rem',
    x2: '0.5rem',
    x3: '0.75rem',
    x4: '1rem',
    x6: '1.5rem',
    x8: '2rem',
    x12: '3rem',
  },
  shadow: {
    raised: '0 10px 30px rgb(23 35 27 / 0.10)',
    overlay: '0 24px 70px rgb(23 35 27 / 0.22)',
  },
});

export function defineTheme(tokens: ThemeTokens): Readonly<ThemeTokens> {
  assertThemeContrast(tokens);
  return deepFreeze(tokens);
}

export function themeToCssVariables(theme: ThemeTokens): Readonly<Record<string, string>> {
  const variables: Record<string, string> = {};
  for (const [group, values] of Object.entries(theme) as [string, Readonly<Record<string, string>>][]) {
    for (const [name, value] of Object.entries(values)) {
      variables[`--bedrock-${toKebabCase(group)}-${toKebabCase(name)}`] = value;
    }
  }

  return Object.freeze(variables);
}

export function contrastRatio(foreground: string, background: string): number {
  const lighter = Math.max(relativeLuminance(foreground), relativeLuminance(background));
  const darker = Math.min(relativeLuminance(foreground), relativeLuminance(background));
  return (lighter + 0.05) / (darker + 0.05);
}

function assertThemeContrast(tokens: ThemeTokens): void {
  const pairs: readonly [string, string, string][] = [
    ['text/canvas', tokens.color.text, tokens.color.canvas],
    ['text/surface', tokens.color.text, tokens.color.surface],
    ['onAccent/accent', tokens.color.onAccent, tokens.color.accent],
    ['onDanger/danger', tokens.color.onDanger, tokens.color.danger],
  ];
  for (const [name, foreground, background] of pairs) {
    if (contrastRatio(foreground, background) < 4.5) {
      throw new RangeError(`Theme color pair ${name} must meet WCAG AA contrast (4.5:1).`);
    }
  }
}

function relativeLuminance(color: string): number {
  if (!/^#[0-9a-f]{6}$/i.test(color)) {
    throw new TypeError(`Color '${color}' must use six-digit hex notation.`);
  }

  const channels = [1, 3, 5].map((index) => Number.parseInt(color.slice(index, index + 2), 16) / 255);
  const linear = channels.map((channel) => channel <= 0.04045
    ? channel / 12.92
    : ((channel + 0.055) / 1.055) ** 2.4);
  return (linear[0] ?? 0) * 0.2126 + (linear[1] ?? 0) * 0.7152 + (linear[2] ?? 0) * 0.0722;
}

function toKebabCase(value: string): string {
  return value.replace(/([a-z0-9])([A-Z])/g, '$1-$2').toLowerCase();
}

function deepFreeze<T>(value: T): Readonly<T> {
  if (typeof value === 'object' && value !== null && !Object.isFrozen(value)) {
    for (const child of Object.values(value)) {
      deepFreeze(child);
    }
    Object.freeze(value);
  }

  return value;
}
