// Interface behavior only: no business rules, sample data or local storage.
(() => {
  const $ = (selector, root = document) => root.querySelector(selector);
  const $$ = (selector, root = document) => [...root.querySelectorAll(selector)];

  /* Show or hide password */
  $$('[data-toggle-pass]').forEach(button => button.addEventListener('click', () => {
    const input = document.getElementById(button.dataset.togglePass);
    input.type = input.type === 'password' ? 'text' : 'password';
    button.setAttribute('aria-label', input.type === 'password' ? 'Mostrar contraseña' : 'Ocultar contraseña');
  }));

  /* Password strength meter */
  const STRENGTH_TEXT = [
    'Usa 8 o más caracteres, con un número y una mayúscula.',
    'Débil: agrega más caracteres.',
    'Regular: suma un número o una mayúscula.',
    'Buena.',
    'Excelente.'
  ];
  function strength(value) {
    let score = 0;
    if (value.length >= 8) score++;
    if (/[A-Z]/.test(value)) score++;
    if (/\d/.test(value)) score++;
    if (/[^A-Za-z0-9]/.test(value) || value.length >= 12) score++;
    return value ? Math.max(1, score) : 0;
  }
  $$('[data-strength]').forEach(input => input.addEventListener('input', () => {
    const level = strength(input.value);
    document.getElementById(input.dataset.strength).dataset.lvl = level;
    const text = document.getElementById(input.dataset.strengthText);
    if (text) text.textContent = STRENGTH_TEXT[level];
  }));

  /* 6-digit code: the visible inputs feed the single hidden field that gets posted */
  const codeInputs = $$('[data-code-inputs] input');
  const codeValue = $('[data-code-value]');
  const syncCode = () => { codeValue.value = codeInputs.map(input => input.value).join(''); };
  codeInputs.forEach((input, index) => {
    input.addEventListener('focus', () => input.select());
    input.addEventListener('input', () => {
      input.value = input.value.replace(/\D/g, '').slice(0, 1);
      syncCode();
      if (input.value && codeInputs[index + 1]) codeInputs[index + 1].focus();
    });
    input.addEventListener('keydown', event => {
      if (event.key === 'Backspace' && !input.value && codeInputs[index - 1]) codeInputs[index - 1].focus();
    });
    input.addEventListener('paste', event => {
      const digits = (event.clipboardData.getData('text') || '').replace(/\D/g, '').slice(0, 6);
      if (!digits) return;
      event.preventDefault();
      codeInputs.forEach((codeInput, position) => { codeInput.value = digits[position] || ''; });
      syncCode();
      codeInputs[Math.min(digits.length, 5)].focus();
    });
  });

  /* Mobile sidebar */
  const sidebar = $('#sidebar');
  const scrim = $('#scrim');
  const burger = $('#burger');
  function setSidebar(open) {
    sidebar.classList.toggle('is-open', open);
    scrim.classList.toggle('is-open', open);
    burger.setAttribute('aria-expanded', open);
    burger.setAttribute('aria-label', open ? 'Cerrar menú' : 'Abrir menú');
    (open ? $('a', sidebar) : burger).focus();
  }
  if (sidebar) {
    burger.addEventListener('click', () => setSidebar(!sidebar.classList.contains('is-open')));
    scrim.addEventListener('click', () => setSidebar(false));
    document.addEventListener('keydown', event => {
      if (event.key === 'Escape' && sidebar.classList.contains('is-open')) setSidebar(false);
    });
  }

  /* Copy the reference code on the error page */
  const copyButton = $('[data-copy]');
  if (copyButton) {
    const source = document.getElementById(copyButton.dataset.copy);
    const label = $('[data-copy-label]', copyButton);
    const selectCode = () => {
      const range = document.createRange();
      range.selectNodeContents(source);
      const selection = getSelection();
      selection.removeAllRanges();
      selection.addRange(range);
      label.textContent = 'Usa Ctrl+C';
    };
    copyButton.addEventListener('click', () => {
      // navigator.clipboard only exists on secure contexts.
      if (!navigator.clipboard) { selectCode(); return; }
      navigator.clipboard.writeText(source.textContent).then(() => { label.textContent = 'Copiado'; }, selectCode);
    });
  }

  /* "Volver atrás" on the 404 page */
  $$('[data-back]').forEach(button => button.addEventListener('click', () => {
    if (history.length > 1) history.back();
    else location.href = button.dataset.back;
  }));
})();
