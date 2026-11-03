// Reglas del registro: cada una recibe el valor del campo y devuelve el mensaje de error o null.
// Reflejan UserRequestValidator, que sigue siendo la autoridad en el backend.
// "terms" solo existe en la interfaz: no viaja en la petición.

const isEmail = (value) => {
  const at = value.indexOf('@');
  return at > 0 && at === value.lastIndexOf('@') && at < value.length - 1;
};

export const registerRules = {
  fullName(value) {
    const name = value.trim();
    if (!name) return 'El nombre es requerido';
    if (name.length > 150) return 'El nombre no puede superar 150 caracteres';
    return null;
  },

  email(value) {
    const email = value.trim();
    if (!email) return 'El correo es requerido';
    if (email.length > 100) return 'El correo no puede superar 100 caracteres';
    if (!isEmail(email)) return 'El correo no es válido';
    return null;
  },

  password(value) {
    if (!value) return 'La contraseña es requerida';
    if (value.length < 8) return 'La contraseña debe tener al menos 8 caracteres';
    if (value.length > 128) return 'La contraseña no puede superar 128 caracteres';
    if (!/[0-9]/.test(value)) return 'La contraseña debe incluir un número';
    if (!/\p{Lu}/u.test(value)) return 'La contraseña debe incluir una mayúscula';
    return null;
  },

  terms(value) {
    return value ? null : 'Debes aceptar los términos y la política de privacidad';
  },
};
