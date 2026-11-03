// Reactividad mínima: signal() guarda un valor, effect() se re-ejecuta cuando cambia.
let current = null;

export function signal(initial) {
  let value = initial;
  const subs = new Set();
  return {
    get() {
      if (current) subs.add(current);
      return value;
    },
    set(next) {
      if (Object.is(value, next)) return;
      value = next;
      [...subs].forEach((run) => run());
    },
  };
}

export function effect(fn) {
  const run = () => {
    const previous = current;
    current = run;
    try {
      fn();
    } finally {
      current = previous;
    }
  };
  run();
}
