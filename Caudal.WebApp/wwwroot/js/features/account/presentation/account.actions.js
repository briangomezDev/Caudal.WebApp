import { afterClick } from '../../../shared/pointer.js';
import { clearErrors, showError, touchAllFields } from './account.validation.js';

// Lo que la vista puede disparar: data-on-blur="touch" y data-on-submit="register".
export function createAccountActions({ state, registerUser, loginUrl, focusFirstError }) {
  return {
    touch(event) {
      const touched = state.touched[event.target.dataset.model];
      // Mostrar el error mueve el formulario: si se hace durante un clic, el clic puede fallar.
      afterClick(() => touched.set(true));
    },

    async register() {
      if (state.loading.get()) return;

      touchAllFields(state);
      clearErrors(state);
      state.loading.set(true);

      try {
        await registerUser.execute({
          fullName: state.fullName.get(),
          email: state.email.get(),
          password: state.password.get(),
          terms: state.terms.get(),
        });
        window.location.assign(loginUrl);
        return; // la página se va; dejamos el botón bloqueado
      } catch (error) {
        showError(state, error);
      }

      state.loading.set(false);
      focusFirstError();
    },
  };
}
