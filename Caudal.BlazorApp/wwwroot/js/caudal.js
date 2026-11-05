// Interface behavior only: no business rules, sample data or local storage.
// Listeners live on document because Blazor's enhanced navigation replaces the page content,
// and this keeps pure interface behavior from opening a server circuit on static pages.
(() => {
  const $ = (selector, root = document) => root.querySelector(selector);
  const $$ = (selector, root = document) => [...root.querySelectorAll(selector)];
  const on = (type, selector, handler) => document.addEventListener(type, event => {
    const match = event.target.closest?.(selector);
    if (match) handler(match, event);
  });

  /* Show or hide password */
  on('click', '[data-toggle-pass]', button => {
    const input = document.getElementById(button.dataset.togglePass);
    input.type = input.type === 'password' ? 'text' : 'password';
    button.setAttribute('aria-label', input.type === 'password' ? 'Mostrar contraseña' : 'Ocultar contraseña');
  });

  /* Password strength meter */
  const STRENGTH_TEXT = [
    'Usa 8 o más caracteres, con un número y una mayúscula.',
    'Débil: agrega más caracteres.',
    'Regular: suma un número o una mayúscula.',
    'Buena.',
    'Excelente.'
  ];
  function strength(value) {
    const hasUpper = /\p{Lu}/u.test(value);
    const hasDigit = /[0-9]/.test(value);
    let score = 0;
    if (value.length >= 8) score++;
    if (hasUpper) score++;
    if (hasDigit) score++;
    if (/[^A-Za-z0-9]/.test(value) || value.length >= 12) score++;
    // Sin número o mayúscula no es válida: nunca debe verse como "Buena".
    if (!hasUpper || !hasDigit) score = Math.min(score, 2);
    return value ? Math.max(1, score) : 0;
  }
  on('input', '[data-strength]', input => {
    const level = strength(input.value);
    document.getElementById(input.dataset.strength).dataset.lvl = level;
    const text = document.getElementById(input.dataset.strengthText);
    if (text) text.textContent = STRENGTH_TEXT[level];
  });

  /* 6-digit code: the visible inputs feed the single hidden field that gets posted */
  const codeInputs = () => $$('[data-code-inputs] input');
  const syncCode = () => { $('[data-code-value]').value = codeInputs().map(input => input.value).join(''); };
  on('focusin', '[data-code-inputs] input', input => input.select());
  on('input', '[data-code-inputs] input', input => {
    const inputs = codeInputs();
    input.value = input.value.replace(/\D/g, '').slice(0, 1);
    syncCode();
    const next = inputs[inputs.indexOf(input) + 1];
    if (input.value && next) next.focus();
  });
  on('keydown', '[data-code-inputs] input', (input, event) => {
    const inputs = codeInputs();
    const previous = inputs[inputs.indexOf(input) - 1];
    if (event.key === 'Backspace' && !input.value && previous) previous.focus();
  });
  on('paste', '[data-code-inputs] input', (input, event) => {
    const digits = (event.clipboardData.getData('text') || '').replace(/\D/g, '').slice(0, 6);
    if (!digits) return;
    event.preventDefault();
    const inputs = codeInputs();
    inputs.forEach((codeInput, position) => { codeInput.value = digits[position] || ''; });
    syncCode();
    inputs[Math.min(digits.length, 5)].focus();
  });

  /* Mobile sidebar */
  function setSidebar(open) {
    const burger = $('#burger');
    $('#sidebar').classList.toggle('is-open', open);
    $('#scrim').classList.toggle('is-open', open);
    burger.setAttribute('aria-expanded', open);
    burger.setAttribute('aria-label', open ? 'Cerrar menú' : 'Abrir menú');
    (open ? $('a', $('#sidebar')) : burger).focus();
  }
  on('click', '#burger', () => setSidebar(!$('#sidebar').classList.contains('is-open')));
  on('click', '#scrim', () => setSidebar(false));
  document.addEventListener('keydown', event => {
    if (event.key === 'Escape' && $('#sidebar')?.classList.contains('is-open')) setSidebar(false);
  });

  /* Copy the reference code on the error page */
  on('click', '[data-copy]', button => {
    const source = document.getElementById(button.dataset.copy);
    const label = $('[data-copy-label]', button);
    const selectCode = () => {
      const range = document.createRange();
      range.selectNodeContents(source);
      const selection = getSelection();
      selection.removeAllRanges();
      selection.addRange(range);
      label.textContent = 'Usa Ctrl+C';
    };
    // navigator.clipboard only exists on secure contexts.
    if (!navigator.clipboard) { selectCode(); return; }
    navigator.clipboard.writeText(source.textContent).then(() => { label.textContent = 'Copiado'; }, selectCode);
  });

  /* "Volver atrás" on the 404 page */
  on('click', '[data-back]', button => {
    if (history.length > 1) history.back();
    else location.href = button.dataset.back;
  });

  /* Pointer tracking: a new validation error moves the form, which can make an ongoing click miss its target. */
  let pressed = false;
  window.addEventListener('pointerdown', () => { pressed = true; });
  window.addEventListener('pointerup', () => { pressed = false; });
  window.addEventListener('pointercancel', () => { pressed = false; });

  window.caudal = {
    // Resolves right away or, when a click is in progress, right after it ends.
    afterClick: () => new Promise(resolve => {
      if (!pressed) { resolve(); return; }
      window.addEventListener('pointerup', () => setTimeout(resolve), { once: true });
    })
  };
})();
