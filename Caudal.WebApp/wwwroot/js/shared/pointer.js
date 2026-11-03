let pressed = false;

window.addEventListener('pointerdown', () => { pressed = true; });
window.addEventListener('pointerup', () => { pressed = false; });
window.addEventListener('pointercancel', () => { pressed = false; });

// Ejecuta fn de inmediato o, si hay un clic en curso, justo después de que termine.
export function afterClick(fn) {
  if (!pressed) {
    fn();
    return;
  }
  window.addEventListener('pointerup', () => setTimeout(fn), { once: true });
}
