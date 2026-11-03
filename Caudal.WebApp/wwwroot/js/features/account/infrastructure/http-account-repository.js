import { HttpError, postJson } from '../../../shared/http.js';
import { FormError } from '../application/errors/form-error.js';

const CREATE_ACCOUNT_URL = '/account/create-account';

// Códigos de UserRequestValidator / UserCreateAccountUseCase -> campo del formulario.
const FIELD_BY_CODE = {
  'SECURITY-0001': 'fullName',
  'SECURITY-0002': 'fullName',
  'SECURITY-0003': 'email',
  'SECURITY-0004': 'email',
  'SECURITY-0005': 'email',
  'SECURITY-0012': 'email',
  'SECURITY-0006': 'password',
  'SECURITY-0007': 'password',
  'SECURITY-0009': 'password',
  'SECURITY-0013': 'password',
  'SECURITY-0014': 'password',
};

// El backend responde en inglés; la interfaz está en español.
const MESSAGE_BY_CODE = {
  'SECURITY-0001': 'El nombre es requerido',
  'SECURITY-0002': 'El nombre no puede superar 150 caracteres',
  'SECURITY-0003': 'El correo es requerido',
  'SECURITY-0004': 'El correo no puede superar 100 caracteres',
  'SECURITY-0005': 'El correo no es válido',
  'SECURITY-0006': 'La contraseña es requerida',
  'SECURITY-0007': 'La contraseña debe tener al menos 8 caracteres',
  'SECURITY-0009': 'La contraseña no puede superar 128 caracteres',
  'SECURITY-0011': 'Ocurrió un error inesperado. Intenta de nuevo.',
  'SECURITY-0012': 'Ya existe una cuenta con este correo',
  'SECURITY-0013': 'La contraseña debe incluir un número',
  'SECURITY-0014': 'La contraseña debe incluir una mayúscula',
};

// Traduce la respuesta HTTP del controller (ToErrorResult en BaseController) a un FormError.
//  - 400: ValidationProblem -> body.errors = { "SECURITY-0001": ["mensaje"], ... }
//  - 409/500/...: Problem   -> body.title = código, body.detail = mensaje
function toFormError(error) {
  if (!(error instanceof HttpError)) {
    return new FormError('No pudimos conectar con el servidor. Intenta de nuevo.');
  }

  const body = error.body ?? {};
  const entries = body.errors
    ? Object.entries(body.errors).map(([code, messages]) => [code, messages?.[0]])
    : body.title
      ? [[body.title, body.detail]]
      : [];

  const fieldErrors = {};
  let general = null;

  for (const [code, serverMessage] of entries) {
    const message = MESSAGE_BY_CODE[code] ?? serverMessage;
    const field = FIELD_BY_CODE[code];
    if (field) fieldErrors[field] ??= message;
    else general ??= message;
  }

  const fallback = Object.keys(fieldErrors).length > 0
    ? 'Revisa los campos marcados'
    : 'Ocurrió un error inesperado. Intenta de nuevo.';

  return new FormError(general ?? fallback, fieldErrors);
}

/** @implements {import('../application/ports/account-repository.js').AccountRepository} */
export class HttpAccountRepository {
  async createAccount(request) {
    try {
      await postJson(CREATE_ACCOUNT_URL, request);
    } catch (error) {
      throw toFormError(error);
    }
  }
}
