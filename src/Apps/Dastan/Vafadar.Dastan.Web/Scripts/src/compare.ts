// The list of names to compare (D-093) can be long: every name of every collection that has a time. Where the
// page offers it, a word typed in the field narrows the list to the names that contain it, in every collection
// at once. Nothing is asked of the server and nothing is chosen or unchosen by narrowing: a ticked name that is
// out of sight stays ticked. Without this script the whole list is there.

// Letters are compared without case and without the marks that change no letter: the Persian ی and ك, the Arabic
// ي and ك, and the zero-width joiners between the parts of a word.
function plain(text: string): string {
  return text
    .toLocaleLowerCase()
    .normalize('NFKD')
    .replace(/[̀-ͯ‌‍]/g, '')
    .replace(/ي/g, 'ی')
    .replace(/ك/g, 'ک');
}

/** Lets the visitor narrow the list of names of the page by a word. */
export function narrowNames(): void {
  for (const form of document.querySelectorAll<HTMLFormElement>('form[data-compare-pick]')) {
    const row = form.querySelector<HTMLElement>('[data-compare-filter-row]');
    const field = form.querySelector<HTMLInputElement>('[data-compare-filter]');
    if (!row || !field) {
      continue;
    }

    row.hidden = false;
    const folds = [...form.querySelectorAll<HTMLDetailsElement>('details')];
    // Which collections were open before anything was typed: they are open again when the field is emptied.
    const wasOpen = new Map(folds.map(fold => [fold, fold.open]));
    field.addEventListener('input', () => {
      const word = plain(field.value.trim());
      for (const fold of folds) {
        let shown = 0;
        for (const group of fold.querySelectorAll<HTMLElement>('fieldset')) {
          let inGroup = 0;
          for (const option of group.querySelectorAll<HTMLElement>('[data-compare-option]')) {
            const fits = word.length === 0 || plain(option.textContent ?? '').includes(word);
            option.hidden = !fits;
            inGroup += fits ? 1 : 0;
          }

          group.hidden = inGroup === 0;
          shown += inGroup;
        }

        fold.hidden = shown === 0;
        fold.open = word.length === 0 ? wasOpen.get(fold) ?? false : shown > 0;
      }
    });
  }
}
