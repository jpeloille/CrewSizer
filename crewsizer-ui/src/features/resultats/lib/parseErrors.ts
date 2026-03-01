import { isAxiosError } from 'axios';

export interface CalculError {
  severity: 'fatal' | 'error' | 'warning';
  message: string;
  hint?: string;
}

/**
 * Parse les alertes string du backend en objets structurés.
 * Convention backend :
 *  - "ERREUR: message — détail" → fatal
 *  - "ALERTE: ..." ou contient DEPASSEMENT → error
 *  - Tout le reste → warning
 */
export function parseAlertes(alertes: string[]): CalculError[] {
  return alertes.map((raw) => {
    if (raw.startsWith('ERREUR:')) {
      const body = raw.slice(7).trim();
      const dashIdx = body.indexOf('—');
      if (dashIdx >= 0) {
        return {
          severity: 'fatal' as const,
          message: body.slice(0, dashIdx).trim(),
          hint: body.slice(dashIdx + 1).trim(),
        };
      }
      return { severity: 'fatal' as const, message: body };
    }

    if (raw.startsWith('ALERTE:') || raw.toUpperCase().includes('DEPASSEMENT')) {
      const body = raw.startsWith('ALERTE:') ? raw.slice(7).trim() : raw;
      const dashIdx = body.indexOf('—');
      if (dashIdx >= 0) {
        return {
          severity: 'error' as const,
          message: body.slice(0, dashIdx).trim(),
          hint: body.slice(dashIdx + 1).trim(),
        };
      }
      return { severity: 'error' as const, message: body };
    }

    return { severity: 'warning' as const, message: raw };
  });
}

/**
 * Parse une erreur HTTP (Axios) en CalculError[].
 */
export function parseHttpError(error: unknown): CalculError[] {
  if (!isAxiosError(error)) {
    return [
      {
        severity: 'fatal',
        message: error instanceof Error ? error.message : 'Erreur inconnue',
      },
    ];
  }

  const status = error.response?.status;
  const data = error.response?.data as Record<string, unknown> | undefined;

  // 422 — validation errors: { errors: { prop: ["msg1", "msg2"] } }
  if (status === 422 && data?.errors) {
    const errMap = data.errors as Record<string, string[]>;
    return Object.entries(errMap).flatMap(([field, messages]) =>
      messages.map((msg) => ({
        severity: 'error' as const,
        message: msg,
        hint: `Champ : ${field}`,
      }))
    );
  }

  // 404 — not found: { message: "..." }
  if (status === 404) {
    return [
      {
        severity: 'fatal',
        message: (data?.message as string) ?? 'Ressource introuvable',
        hint: 'Verifiez que le scenario existe toujours.',
      },
    ];
  }

  // 409 — conflict: { message: "..." }
  if (status === 409) {
    return [
      {
        severity: 'error',
        message: (data?.message as string) ?? 'Conflit de donnees',
      },
    ];
  }

  // 500 — server error: { message: "...", detail: "..." }
  return [
    {
      severity: 'fatal',
      message: (data?.message as string) ?? 'Erreur serveur interne',
      hint: (data?.detail as string) ?? undefined,
    },
  ];
}
