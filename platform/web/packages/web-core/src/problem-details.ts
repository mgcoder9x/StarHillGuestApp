export interface ProblemDetails {
  readonly type?: string;
  readonly title?: string;
  readonly status?: number;
  readonly detail?: string;
  readonly instance?: string;
  readonly code?: string;
  readonly errors?: Readonly<Record<string, readonly string[]>>;
  readonly [extension: string]: unknown;
}

export type ApiErrorKind = 'problem' | 'http' | 'network' | 'timeout' | 'aborted' | 'invalid-response';

export class ApiError extends Error {
  public readonly kind: ApiErrorKind;
  public readonly status: number | undefined;
  public readonly problem: ProblemDetails | undefined;
  public readonly correlationId: string | undefined;

  public constructor(
    message: string,
    options: {
      readonly kind: ApiErrorKind;
      readonly status?: number;
      readonly problem?: ProblemDetails;
      readonly correlationId?: string;
      readonly cause?: unknown;
    },
  ) {
    super(message, { cause: options.cause });
    this.name = 'ApiError';
    this.kind = options.kind;
    this.status = options.status;
    this.problem = options.problem;
    this.correlationId = options.correlationId;
  }
}

export function isProblemDetails(value: unknown): value is ProblemDetails {
  if (!isRecord(value)) {
    return false;
  }

  return (value.status === undefined || typeof value.status === 'number')
    && (value.title === undefined || typeof value.title === 'string')
    && (value.detail === undefined || typeof value.detail === 'string')
    && (value.code === undefined || typeof value.code === 'string');
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}
