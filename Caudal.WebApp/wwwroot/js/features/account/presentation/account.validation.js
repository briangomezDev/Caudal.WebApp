import { effect } from '../../../shared/signals.js';
import { FormError } from '../application/errors/form-error.js';
import { registerRules } from '../domain/register-rules.js';

const FIELDS = Object.keys(registerRules);

const errorOf = (state, field) => state[`${field}Error`];

// Revalida cada campo cuando cambia. Solo muestra el error si el campo ya está "touched".
// Editar un campo también reemplaza el error que haya devuelto el servidor para él.
function watchField(state, field) {
  effect(() => {
    const value = state[field].get();
    const touched = state.touched[field].get();
    errorOf(state, field).set(touched ? registerRules[field](value) : null);
  });
}

// El error general deja de aplicar en cuanto el usuario edita cualquier campo.
function watchGeneralError(state) {
  effect(() => {
    FIELDS.forEach((field) => state[field].get());
    state.error.set(null);
  });
}

export function watchValidation(state) {
  FIELDS.forEach((field) => watchField(state, field));
  watchGeneralError(state);
}

export function touchAllFields(state) {
  FIELDS.forEach((field) => state.touched[field].set(true));
}

export function clearErrors(state) {
  state.error.set(null);
  FIELDS.forEach((field) => errorOf(state, field).set(null));
}

export function showError(state, error) {
  if (!(error instanceof FormError)) {
    console.error(error);
    state.error.set('Ocurrió un error inesperado. Intenta de nuevo.');
    return;
  }

  state.error.set(error.message);
  FIELDS.forEach((field) => errorOf(state, field).set(error.fieldErrors[field] ?? null));
}
