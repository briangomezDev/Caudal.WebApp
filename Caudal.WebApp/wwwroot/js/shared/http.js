// Cliente HTTP compartido. Solo transporte: no conoce features ni reglas de negocio.
export class HttpError extends Error {
  constructor(status, body) {
    super(body?.title ?? `HTTP ${status}`);
    this.status = status;
    this.body = body;
  }
}

export async function postJson(url, payload) {
  const response = await fetch(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    body: JSON.stringify(payload),
  });

  const text = await response.text();
  let body = null;
  try {
    body = text ? JSON.parse(text) : null;
  } catch {
    // respuesta que no es JSON (p. ej. una página de error): body queda en null
  }

  if (!response.ok) throw new HttpError(response.status, body);
  return body;
}
