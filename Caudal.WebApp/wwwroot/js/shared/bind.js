import { effect } from './signals.js';

// Conecta signals con el HTML que ya renderizó Razor. No crea DOM.
//
// Atributos soportados:
//   data-model="campo"                 input <-> signal (el valor inicial sale del HTML; una casilla usa checked)
//   data-bind-text="signal"            textContent
//   data-bind-disabled="signal"        propiedad disabled
//   data-bind-invalid="signal"         atributo aria-invalid
//   data-bind-class="clase:signal ..." alterna la clase; "!signal" invierte la condición
//   data-on-submit="accion"            submit del form (hace preventDefault)
//   data-on-click="accion"             click
//   data-on-blur="accion"              blur (la acción recibe el evento)

// Incluye al propio root, porque querySelectorAll solo busca descendientes.
const select = (root, selector) => [
  ...(root.matches(selector) ? [root] : []),
  ...root.querySelectorAll(selector),
];

const read = (state, name) => {
  const s = state[name];
  if (!s) throw new Error(`bind: no existe el signal "${name}" en el estado`);
  return s;
};

const run = (actions, name) => {
  const action = actions[name];
  if (!action) throw new Error(`bind: no existe la acción "${name}"`);
  return action;
};

export function bind(root, state, actions = {}) {
  select(root, '[data-model]').forEach((el) => {
    const s = read(state, el.dataset.model);
    const value = () => (el.type === 'checkbox' ? el.checked : el.value);
    s.set(value());
    el.addEventListener('input', () => s.set(value()));
  });

  select(root, '[data-bind-text]').forEach((el) => {
    const s = read(state, el.dataset.bindText);
    effect(() => { el.textContent = s.get() ?? ''; });
  });

  select(root, '[data-bind-disabled]').forEach((el) => {
    const s = read(state, el.dataset.bindDisabled);
    effect(() => { el.disabled = !!s.get(); });
  });

  select(root, '[data-bind-invalid]').forEach((el) => {
    const s = read(state, el.dataset.bindInvalid);
    effect(() => { el.setAttribute('aria-invalid', s.get() ? 'true' : 'false'); });
  });

  select(root, '[data-bind-class]').forEach((el) => {
    el.dataset.bindClass.split(/\s+/).filter(Boolean).forEach((pair) => {
      const [className, rawName] = pair.split(':');
      const negate = rawName.startsWith('!');
      const s = read(state, negate ? rawName.slice(1) : rawName);
      effect(() => { el.classList.toggle(className, negate ? !s.get() : !!s.get()); });
    });
  });

  select(root, '[data-on-submit]').forEach((el) => {
    const action = run(actions, el.dataset.onSubmit);
    el.addEventListener('submit', (event) => {
      event.preventDefault();
      action();
    });
  });

  select(root, '[data-on-click]').forEach((el) => {
    const action = run(actions, el.dataset.onClick);
    el.addEventListener('click', () => action());
  });

  select(root, '[data-on-blur]').forEach((el) => {
    const action = run(actions, el.dataset.onBlur);
    el.addEventListener('blur', (event) => action(event));
  });
}
