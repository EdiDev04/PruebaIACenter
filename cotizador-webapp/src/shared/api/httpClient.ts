const getAuthHeader = (): string => {
  const user = import.meta.env.VITE_API_USER ?? '';
  const pass = import.meta.env.VITE_API_PASSWORD ?? '';
  const credentials = `${user}:${pass}`;
  return `Basic ${btoa(credentials)}`;
};

const getBaseUrl = (): string => import.meta.env.VITE_API_URL ?? '';

export const httpClient = {
  get: async <T>(url: string): Promise<T> => {
    const res = await fetch(`${getBaseUrl()}${url}`, {
      headers: {
        Authorization: getAuthHeader(),
        'Content-Type': 'application/json',
        'Correlation-Id': crypto.randomUUID(),
      },
    });
    if (!res.ok) throw await res.json();
    return res.json() as Promise<T>;
  },

  post: async <T>(
    url: string,
    body: unknown,
    extraHeaders?: Record<string, string>
  ): Promise<T> => {
    const res = await fetch(`${getBaseUrl()}${url}`, {
      method: 'POST',
      headers: {
        Authorization: getAuthHeader(),
        'Content-Type': 'application/json',
        'Correlation-Id': crypto.randomUUID(),
        ...extraHeaders,
      },
      body: JSON.stringify(body),
    });
    if (!res.ok) throw await res.json();
    return res.json() as Promise<T>;
  },

  put: async <T>(url: string, body: unknown): Promise<T> => {
    const res = await fetch(`${getBaseUrl()}${url}`, {
      method: 'PUT',
      headers: {
        Authorization: getAuthHeader(),
        'Content-Type': 'application/json',
        'Correlation-Id': crypto.randomUUID(),
      },
      body: JSON.stringify(body),
    });
    if (!res.ok) throw await res.json();
    return res.json() as Promise<T>;
  },

  patch: async <T>(url: string, body: unknown): Promise<T> => {
    const res = await fetch(`${getBaseUrl()}${url}`, {
      method: 'PATCH',
      headers: {
        Authorization: getAuthHeader(),
        'Content-Type': 'application/json',
        'Correlation-Id': crypto.randomUUID(),
      },
      body: JSON.stringify(body),
    });
    if (!res.ok) throw await res.json();
    return res.json() as Promise<T>;
  },
};
