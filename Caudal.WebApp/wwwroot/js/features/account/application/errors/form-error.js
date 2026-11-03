// Error que la capa de aplicación entiende: un mensaje general más errores por campo.
// Lo lanzan los casos de uso y los adaptadores; la vista lo pinta.
export class FormError extends Error {
  /**
   * @param {string} message
   * @param {Partial<Record<'fullName' | 'email' | 'password' | 'terms', string>>} [fieldErrors]
   */
  constructor(message, fieldErrors = {}) {
    super(message);
    this.name = 'FormError';
    this.fieldErrors = fieldErrors;
  }
}
