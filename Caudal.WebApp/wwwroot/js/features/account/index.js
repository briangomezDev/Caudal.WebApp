import { HttpAccountRepository } from './infrastructure/http-account-repository.js';
import { RegisterUser } from './application/use-cases/register-user.js';
import { createAccountState } from './presentation/account.state.js';
import { createAccountActions } from './presentation/account.actions.js';
import { watchValidation } from './presentation/account.validation.js';
import { bind } from '../../shared/bind.js';

// Punto de entrada de la feature: arma dependencias y conecta la vista que Razor ya renderizó.
export function render(app) {
  const state = createAccountState();

  const actions = createAccountActions({
    state,
    registerUser: new RegisterUser(new HttpAccountRepository()),
    loginUrl: app.dataset.loginUrl ?? '/account/login',
    focusFirstError: () => app.querySelector('[aria-invalid="true"]')?.focus(),
  });

  watchValidation(state);
  bind(app, state, actions);
}
