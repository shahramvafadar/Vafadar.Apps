/**
 * A chart of connections or of a family is drawn around one name. Its stage begins at the top of the drawing;
 * when much is opened above the name or before it, the name lies outside what the stage shows first. The stage
 * is moved once, when the page has arrived, and only as far as the name needs – a visitor who scrolls afterwards
 * is not followed. Without this script the name is reached by scrolling the stage, as before (ADR 0008).
 */
export function bringNamesIntoSight(): void {
  for (const frame of document.querySelectorAll<HTMLElement>('.stage-frame')) {
    // The box is asked for by its place in the drawing: an engine does not say where an element inside it is.
    const name = frame.querySelector('.graph-focus')?.closest('foreignObject');
    if (frame.clientHeight === 0) {
      continue;
    }

    if (!name) {
      bringPlacesIntoSight(frame);
      continue;
    }

    const shown = frame.getBoundingClientRect();
    const place = name.getBoundingClientRect();
    if (place.top < shown.top || place.bottom > shown.bottom) {
      frame.scrollTop += place.top + (place.height / 2) - (shown.top + (shown.height / 2));
    }

    if (place.left < shown.left || place.right > shown.right) {
      frame.scrollLeft += place.left + (place.width / 2) - (shown.left + (shown.width / 2));
    }
  }
}

/**
 * A map that is wider than its stage – on a phone – begins at its western edge, whatever it shows: the map of
 * ancient Iran began with Greece. The stage is moved sideways once, so that the place in the middle of the
 * places – as many to its west as to its east – stands in the middle of what is shown.
 */
function bringPlacesIntoSight(frame: HTMLElement): void {
  const marks = [...frame.querySelectorAll('.places-map .map-mark')];
  if (marks.length === 0 || frame.scrollWidth <= frame.clientWidth) {
    return;
  }

  const middles = marks.map(mark => { const box = mark.getBoundingClientRect(); return box.left + (box.width / 2); }).sort((a, b) => a - b);
  const shown = frame.getBoundingClientRect();
  frame.scrollLeft += (middles[Math.floor(middles.length / 2)] ?? shown.left) - (shown.left + (shown.width / 2));
}
