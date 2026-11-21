// The frame of every page (step 20.5, ADR 0024). The control of language and appearance is a "details" and
// works without this: it opens and closes when it is pressed. With the script it also closes when the visitor
// presses elsewhere or the Escape key – as a menu does –, and the Escape key gives the focus back to it.
export function closeSettingsWhenLeft(): void {
  const settings = document.querySelector<HTMLDetailsElement>('details.site-settings');
  if (!settings) {
    return;
  }

  document.addEventListener('pointerdown', (event) => {
    if (settings.open && event.target instanceof Node && !settings.contains(event.target)) {
      settings.open = false;
    }
  });

  settings.addEventListener('keydown', (event) => {
    if (event.key === 'Escape' && settings.open) {
      settings.open = false;
      settings.querySelector<HTMLElement>('summary')?.focus();
    }
  });
}
