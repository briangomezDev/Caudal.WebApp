import { signal } from '../../../shared/signals.js';

export const createAccountState = () => ({
  // campos del formulario
  fullName: signal(''),
  email: signal(''),
  password: signal(''),
  terms: signal(false),

  // estado de la operación
  loading: signal(false),
  error: signal(null),

  // errores por campo
  fullNameError: signal(null),
  emailError: signal(null),
  passwordError: signal(null),
  termsError: signal(null),

  // campos que ya deben mostrar su error: los que el usuario abandonó o intentó enviar
  touched: {
    fullName: signal(false),
    email: signal(false),
    password: signal(false),
    terms: signal(false),
  },
});
