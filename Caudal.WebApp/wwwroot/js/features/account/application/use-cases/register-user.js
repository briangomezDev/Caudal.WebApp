import { FormError } from '../errors/form-error.js';
import { registerRules } from '../../domain/register-rules.js';

/** @typedef {import('../ports/account-repository.js').AccountRepository} AccountRepository */

export class RegisterUser {
  /** @param {AccountRepository} accountRepository */
  constructor(accountRepository) {
    this.accountRepository = accountRepository;
  }

  /** @param {{ fullName: string, email: string, password: string, terms: boolean }} input */
  async execute(input) {
    const { fullName, email, password } = input;
    const request = { fullName: fullName.trim(), email: email.trim(), password };

    // Chequeos baratos para evitar el viaje al servidor.
    const fieldErrors = {};
    for (const [field, rule] of Object.entries(registerRules)) {
      const message = rule(input[field]);
      if (message) fieldErrors[field] = message;
    }

    if (Object.keys(fieldErrors).length > 0) {
      throw new FormError('Revisa los campos marcados', fieldErrors);
    }

    await this.accountRepository.createAccount(request);
  }
}
