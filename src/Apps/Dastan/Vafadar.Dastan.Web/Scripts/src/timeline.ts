// DastanTimeline in the browser (ADR 0021): the statements of a page with a time, drawn as views that are moved
// and made larger and smaller without asking the server – one view for each axis something is counted on. The
// calendar is a line of years; a depth of time is a line of powers of ten (a million years and ten thousand
// years both have room on it); the order of a telling is a line of steps, which has no distances to stretch.
// What is known is what is drawn: a window in which something happened once is a dashed frame, something that
// lasted is a bar, a beginning or an end that is only known to lie between two times is hatched, and an end
// that is open, or that nobody states, ends in an arrow or fades. Nothing here reads a clock or a date of the
// browser: every place is a number the server wrote, and every word is the server's.
//
// The script is asked for by a page that holds a <dastan-timeline>, after the page is shown. Until it has
// drawn, and where it cannot, the still chart of the server stays.
import { inStep, keepStill, stillChosen } from './explore';

type Axis = 'calendar' | 'depth' | 'order';
type Edge = 'known' | 'between' | 'ongoing' | 'endedAtUnknownTime' | 'unknown';

interface Mark {
  key: string; label: string; shape: 'moment' | 'window' | 'lasting'; from: number; to: number; when: string; address: string;
  start?: Edge; end?: Edge; startedBy?: number; endsFrom?: number; loose?: boolean; disputed?: boolean;
  // At a step of a telling: what the step says about the name being there, and whether it is only spoken of.
  presence?: string; spokenOf?: boolean;
}
interface Lane { key: string; label: string; marks: Mark[]; }
interface View { axis: Axis; title: string; from: number; to: number; lanes: Lane[]; }
interface Words {
  commonEra: string; beforeCommonEra: string; yearsAgo: string; thousandYearsAgo: string; millionYearsAgo: string;
  billionYearsAgo: string; step: string; persianDigits: boolean;
}
interface Data { views: View[]; words: Words; }

const svgSpace = 'http://www.w3.org/2000/svg';
const rowHeight = 30, laneGap = 26, axisHeight = 34, sidePad = 16, barHeight = 16;

function shape<K extends keyof SVGElementTagNameMap>(name: K, attributes: Record<string, string | number>, parent?: Element): SVGElementTagNameMap[K] {
  const made = document.createElementNS(svgSpace, name);
  for (const [key, value] of Object.entries(attributes)) {
    made.setAttribute(key, String(value));
  }

  parent?.append(made);
  return made;
}

// The place of the axis as a number the view is linear in. A depth of time is counted in years before the
// present and drawn by its power of ten, the older to the left: time runs from left to right in every view.
const along = (axis: Axis, place: number): number => axis === 'depth' ? -Math.log10(Math.max(place, 1)) : place;

class TimelineView {
  private from: number;
  private to: number;
  private readonly whole: [number, number];
  private readonly svg: SVGSVGElement;
  private width = 0;

  constructor(private readonly view: View, private readonly words: Words, private readonly frame: HTMLElement, private readonly index: number) {
    const [first, last] = [along(view.axis, view.from), along(view.axis, view.to)];
    // A little room at both sides, so that a mark at the very beginning is not cut by the frame.
    const room = view.axis === 'order' ? 0.6 : Math.max((last - first) * 0.04, view.axis === 'depth' ? 0.08 : 1);
    this.whole = [first - room, last + room];
    [this.from, this.to] = this.asked() ?? this.whole;
    // The frame of the view carries its name; the drawing in it is a group of links, each with its own.
    this.svg = shape('svg', { class: 'tlv' });
    frame.append(this.svg);
    frame.setAttribute('aria-label', view.title);
    new ResizeObserver(() => this.draw()).observe(frame);
    this.listen();
    this.draw();
  }

  /** What a test of the browser reads: the stretch that is shown, in the numbers of the axis. */
  get seen(): { from: number; to: number; whole: [number, number]; marks: number } {
    return { from: this.from, to: this.to, whole: this.whole, marks: this.svg.querySelectorAll('a.tlv-mark').length };
  }

  // The stretch the address asks for: "from" and "to" of the calendar as the still chart reads them, and a
  // stretch of every other view under the name of its place on the page.
  private get name(): string {
    return this.view.axis === 'calendar' ? '' : `v${this.index}`;
  }

  private asked(): [number, number] | null {
    const query = new URLSearchParams(location.search);
    const read = (name: string): number | null => {
      const value = query.get(name);
      return value !== null && /^-?\d+(\.\d+)?$/.test(value) ? Number(value) : null;
    };
    if (this.view.axis === 'calendar') {
      // The address counts years without a year 0: 1 is the first year of the era, -1 the one before it.
      const [first, last] = [read('from'), read('to')];
      if (first === null || last === null || first === 0 || last === 0 || last <= first) {
        return null;
      }

      // On the axis of years the first year of the era begins at 1, and the year before it at 0.
      const place = (year: number): number => year > 0 ? year : year + 1;
      return [place(first), place(last) + 1];
    }

    const [first, last] = [read(this.name + 'from'), read(this.name + 'to')];
    return first !== null && last !== null && last > first ? [first, last] : null;
  }

  // The view is kept in the address, so that it can be passed on and so that the still chart of the server,
  // asked for with it, shows the same stretch of the calendar.
  private keep(): void {
    const address = new URL(location.href);
    const isWhole = this.from === this.whole[0] && this.to === this.whole[1];
    if (this.view.axis === 'calendar') {
      const year = (place: number): number => { const whole = Math.floor(place); return whole >= 1 ? whole : whole - 1; };
      const [first, last] = [year(this.from), year(this.to - 0.0001)];
      if (isWhole || last <= first || Math.abs(first) > 1_000_000 || Math.abs(last) > 1_000_000) {
        address.searchParams.delete('from');
        address.searchParams.delete('to');
      } else {
        address.searchParams.set('from', String(first));
        address.searchParams.set('to', String(last));
      }
    } else if (isWhole) {
      address.searchParams.delete(this.name + 'from');
      address.searchParams.delete(this.name + 'to');
    } else {
      address.searchParams.set(this.name + 'from', this.from.toFixed(4));
      address.searchParams.set(this.name + 'to', this.to.toFixed(4));
    }

    history.replaceState(history.state, '', address);

    // The stretch of years is part of the state of exploring (ADR 0025): the links of the views carry it.
    if (this.view.axis === 'calendar') {
      inStep(['from', 'to']);
    }
  }

  private x(value: number): number {
    return sidePad + ((value - this.from) / (this.to - this.from)) * (this.width - 2 * sidePad);
  }

  private number(value: number): string {
    const digits = String(value);
    return this.words.persianDigits ? digits.replace(/\d/g, digit => '۰۱۲۳۴۵۶۷۸۹'[Number(digit)]!).replace('.', '٫') : digits;
  }

  // The marks of the axis: round years of each era and the beginning of the common era; round depths of time,
  // one, two and five of each power of ten; the steps of a telling.
  private ticks(): { at: number; label: string }[] {
    const span = this.to - this.from;
    const room = Math.max(2, Math.floor((this.width - 2 * sidePad) / 96));
    const ticks: { at: number; label: string }[] = [];
    if (this.view.axis === 'order') {
      const step = Math.max(1, Math.ceil(span / room));
      for (let ordinal = Math.max(1, Math.ceil(this.from)); ordinal <= this.to; ordinal += step) {
        ticks.push({ at: ordinal, label: this.words.step.replace('{0}', this.number(ordinal)) });
      }

      return ticks;
    }

    if (this.view.axis === 'depth') {
      for (let power = Math.floor(-this.to) - 1; power <= Math.ceil(-this.from) + 1; power++) {
        for (const lead of span * 3 > room ? [1] : span * 1.2 > room ? [1, 3] : [1, 2, 5]) {
          const years = lead * 10 ** power;
          const at = -Math.log10(years);
          if (years >= 1 && at >= this.from && at <= this.to) {
            const [unit, size] = years >= 1e9 ? [this.words.billionYearsAgo, 1e9] : years >= 1e6 ? [this.words.millionYearsAgo, 1e6]
              : years >= 1e3 ? [this.words.thousandYearsAgo, 1e3] : [this.words.yearsAgo, 1];
            ticks.push({ at, label: unit.replace('{0}', this.number(Number((years / size).toPrecision(3)))) });
          }
        }
      }

      return ticks.sort((one, other) => one.at - other.at);
    }

    // The calendar: a step of 1, 2 or 5 times a power of ten, in years of each era, counted from its beginning.
    const rough = span / room;
    const power = 10 ** Math.floor(Math.log10(Math.max(rough, 1)));
    const step = Math.max(1, [1, 2, 5, 10].map(lead => lead * power).find(candidate => candidate >= rough) ?? 10 * power);
    const eraInSight = this.from <= 1 && this.to >= 1;
    if (eraInSight) {
      ticks.push({ at: 1, label: this.words.commonEra.replace('{0}', this.number(1)) });
    }

    // A round year next to the beginning of the era would stand on its mark.
    const clear = (at: number): boolean => !eraInSight || Math.abs(at - 1) * ((this.width - 2 * sidePad) / span) > 48;
    for (let year = step; year <= this.to; year += step) {
      if (year >= this.from && year > 1 && clear(year)) {
        ticks.push({ at: year, label: this.words.commonEra.replace('{0}', this.number(year)) });
      }
    }

    for (let year = step; 1 - year >= this.from; year += step) {
      if (1 - year <= this.to && clear(1 - year)) {
        ticks.push({ at: 1 - year, label: this.words.beforeCommonEra.replace('{0}', this.number(year)) });
      }
    }

    return ticks.sort((one, other) => one.at - other.at);
  }

  // How long a name is when it is drawn: measured, not guessed from the number of its letters – the letters of
  // Persian and of English are not equally wide, and a name that stands before its mark must not reach the one
  // before it. A name is measured once.
  private readonly lengths = new Map<string, number>();

  private long(label: string): number {
    let length = this.lengths.get(label);
    if (length === undefined) {
      const probe = shape('text', { class: 'tlv-name', x: 0, y: -100, 'unicode-bidi': 'plaintext' }, this.svg);
      probe.textContent = label;
      const measured = probe.getComputedTextLength();
      probe.remove();
      length = measured > 0 ? measured : label.length * 7.2;
      this.lengths.set(label, length);
    }

    return length;
  }

  private draw(): void {
    const width = Math.floor(this.frame.clientWidth);
    if (width < 40) {
      return;
    }

    this.width = width;
    this.svg.replaceChildren();
    const defs = shape('defs', {}, this.svg);
    const hatch = shape('pattern', { id: `tlv-hatch-${this.index}`, width: 6, height: 6, patternUnits: 'userSpaceOnUse', patternTransform: 'rotate(45)' }, defs);
    shape('line', { class: 'tlv-hatch', x1: 0, y1: 0, x2: 0, y2: 6 }, hatch);

    let top = axisHeight;
    const rows: (() => void)[] = [];
    for (const [laneIndex, lane] of this.view.lanes.entries()) {
      // Each lane in its own colour, as in the still chart; the colour is never the only sign of the lane.
      const colour = ` lane-${(laneIndex % 6) + 1}`;
      const laneTop = top;
      const name = shape('text', { class: 'tlv-lane', x: sidePad, y: laneTop + 14, 'unicode-bidi': 'plaintext' }, this.svg);
      name.textContent = lane.label;
      top += 20;

      // Each mark in the first row in which it and its name have room.
      const ends: number[] = [];
      for (const mark of lane.marks) {
        const [first, last] = [along(this.view.axis, mark.from), along(this.view.axis, mark.to)];
        if (last < this.from || first > this.to) {
          continue;
        }

        const start = Math.max(this.x(first), -40), end = Math.min(Math.max(this.x(last), this.x(first) + 4), this.width + 40);
        // A name stands after its mark. Where the view ends before the name would (seen on a phone, step 20.10:
        // the name then lay over its own mark), it stands before the mark – and the row has to be free that far.
        const long = this.long(mark.label);
        const before = mark.shape !== 'lasting' && end + 12 + long > this.width - 4 && start - 10 - long >= 4;
        const [needs, extent] = before ? [start - 10 - long, end + 14] : [start, end + 12 + long + 14];
        let row = ends.findIndex(taken => needs >= taken + 8);
        if (row < 0) {
          row = ends.length;
          ends.push(extent);
        } else {
          ends[row] = extent;
        }

        const y = top + row * rowHeight;
        rows.push(() => this.mark(mark, start, end, y, before, colour));
      }

      top += Math.max(1, ends.length) * rowHeight + laneGap;
      shape('line', { class: 'tlv-rule', x1: 0, x2: this.width, y1: top - laneGap / 2, y2: top - laneGap / 2 }, this.svg);
    }

    const height = top;
    this.svg.setAttribute('width', String(this.width));
    this.svg.setAttribute('height', String(height));
    this.svg.setAttribute('viewBox', `0 0 ${this.width} ${height}`);

    // The axis and its lines first, the marks over them.
    const axis = shape('g', { class: 'tlv-axis' });
    this.svg.prepend(axis);
    for (const tick of this.ticks()) {
      const x = this.x(tick.at);
      shape('line', { class: 'tlv-grid', x1: x, x2: x, y1: axisHeight - 8, y2: height }, axis);
      // Seen in a browser: the words of a mark at the very side were cut by the frame.
      const label = shape('text', { class: 'tlv-tick', x: Math.min(Math.max(x, 34), this.width - 34), y: 16, 'text-anchor': 'middle', 'unicode-bidi': 'plaintext' }, axis);
      label.textContent = tick.label;
    }

    for (const row of rows) {
      row();
    }
  }

  private mark(mark: Mark, start: number, end: number, y: number, before: boolean, colour: string): void {
    // Who is only spoken of at a step is said in the name of the mark and drawn hollow – as the still chart and
    // its table say it: taking part and being mentioned are not the same.
    const said = mark.presence ? ` – ${mark.presence}` : '';
    const link = shape('a', { class: `tlv-mark tlv-${mark.shape}${mark.disputed ? ' tlv-disputed' : ''}${mark.loose ? ' tlv-loose' : ''}${mark.spokenOf ? ' tlv-spoken-of' : ''}${colour}`, href: new URL(mark.address, document.baseURI).href, 'aria-label': `${mark.label}: ${mark.when}${said}` }, this.svg);
    link.dataset['key'] = mark.key;
    shape('title', {}, link).textContent = `${mark.label} – ${mark.when}${said}`;
    const middle = y + barHeight / 2;
    let textAt = end + 8;
    if (mark.shape === 'moment') {
      // A time that is stated: a point, or the stretch of its year where a year is wide enough to see.
      if (end - start >= 10) {
        shape('rect', { class: 'tlv-bar', x: start, y: y + 3, width: end - start, height: barHeight - 6, rx: 3 }, link);
      } else {
        const at = (start + end) / 2;
        shape('circle', { class: 'tlv-point', cx: at, cy: middle, r: 6 }, link);
        textAt = at + 12;
      }
    } else if (mark.shape === 'window') {
      // The window in which it happened once: a frame, not a bar – nothing says that it took so long.
      shape('rect', { class: 'tlv-frame-of-window', x: start, y: y + 1, width: Math.max(end - start, 8), height: barHeight - 2, rx: 3 }, link);
      shape('circle', { class: 'tlv-point tlv-point-open', cx: (start + end) / 2, cy: middle, r: 3.5 }, link);
    } else {
      shape('rect', { class: 'tlv-bar', x: start, y, width: end - start, height: barHeight, rx: 3 }, link);
      // A beginning or an end between two times: the part in which it may lie is hatched.
      if (mark.start === 'between' && mark.startedBy !== undefined) {
        const until = Math.min(this.x(along(this.view.axis, mark.startedBy)), end);
        shape('rect', { class: 'tlv-unsure', x: start, y, width: Math.max(until - start, 3), height: barHeight, fill: `url(#tlv-hatch-${this.index})` }, link);
      }

      if (mark.end === 'between' && mark.endsFrom !== undefined) {
        const since = Math.max(this.x(along(this.view.axis, mark.endsFrom)), start);
        shape('rect', { class: 'tlv-unsure', x: since, y, width: Math.max(end - since, 3), height: barHeight, fill: `url(#tlv-hatch-${this.index})` }, link);
      }

      // A beginning nobody states: the bar comes out of the unknown at its left.
      if (mark.start === 'unknown') {
        shape('path', { class: 'tlv-open', d: `M${start - 22} ${middle}H${start}`, 'data-open': 'start' }, link);
      }

      // Still going on: an arrow onward. Ended at a time nobody states, or an end nobody states: a line that
      // fades, with the mark of a question – never the arrow, which would say that it goes on.
      if (mark.end === 'ongoing') {
        shape('path', { class: 'tlv-arrow', d: `M${end} ${middle}H${end + 20}M${end + 14} ${middle - 5}L${end + 21} ${middle}L${end + 14} ${middle + 5}`, 'data-open': 'ongoing' }, link);
        textAt = end + 28;
      } else if (mark.end === 'unknown' || mark.end === 'endedAtUnknownTime') {
        shape('path', { class: 'tlv-open', d: `M${end} ${middle}H${end + 18}`, 'data-open': mark.end }, link);
        shape('text', { class: 'tlv-question', x: end + 22, y: middle + 4 }, link).textContent = '?';
        textAt = end + 34;
      }
    }

    // The name stands after its mark. Seen in a browser: where the mark runs out of the view at the right, the
    // name was out of sight with it. Then it stands on the part of the bar that is in sight, where that has
    // room for it, and otherwise at the right side of the view.
    const long = this.long(mark.label);
    const name = shape('text', { class: 'tlv-name', x: textAt, y: middle + 4, 'unicode-bidi': 'plaintext' }, link);
    name.textContent = mark.label;
    if (before) {
      // The row was kept free for it: the name ends where the mark begins.
      name.setAttribute('x', String(start - 10));
      name.setAttribute('text-anchor', 'end');
    } else if (textAt + long > this.width - 4) {
      const [left, right] = [Math.max(start, 0), Math.min(end, this.width)];
      if (mark.shape === 'lasting' && right - left >= long + 12) {
        name.setAttribute('x', String(left + 6));
        name.setAttribute('class', 'tlv-name tlv-name-inside');
      } else if (mark.shape === 'lasting') {
        name.setAttribute('x', String(this.width - 6));
        name.setAttribute('text-anchor', 'end');
      } else {
        // A point or a window with room for its whole name on neither side (a phone, a long name): the name is
        // shortened on the side that has more room, so that it never lies over its mark. The whole name is what
        // the mark is called for a screen reader, what its title shows, and what the table of the page says.
        const [after, ahead] = [this.width - 6 - textAt, start - 14];
        const room = Math.max(after, ahead);
        const letters = Math.floor(room / (long / Math.max(mark.label.length, 1)));
        // What is shown is the beginning of the name as it is, so that somebody who speaks to the device can say
        // what they see (WCAG 2.5.3); that it goes on is drawn – three dots where the reading would go on –, not
        // written into the words.
        // Whole words only: half a word is nothing a reader or a device can match with the name.
        const cut = mark.label.slice(0, Math.max(letters - 1, 0));
        name.textContent = cut.includes(' ') ? cut.slice(0, cut.lastIndexOf(' ')).trimEnd() : '';
        if (ahead > after) {
          name.setAttribute('x', String(start - 10));
          name.setAttribute('text-anchor', 'end');
        }

        if (name.textContent !== '') {
          const box = name.getBBox();
          const leftward = /[֐-ࣿיִ-ﻼ]/.test(mark.label);
          for (const step of [1, 2, 3]) {
            shape('circle', { class: 'tlv-more', cx: leftward ? box.x - step * 4 : box.x + box.width + step * 4, cy: middle + 3, r: 1.1 }, link);
          }
        }
      }
    }
  }

  private show(from: number, to: number): void {
    // Not nearer than a part of a step, a year or a power of ten, and not further than three times the whole.
    const least = this.view.axis === 'calendar' ? 0.25 : this.view.axis === 'depth' ? 0.02 : 1;
    const most = (this.whole[1] - this.whole[0]) * 3;
    let span = Math.min(Math.max(to - from, least), most);
    const middle = (from + to) / 2;
    [from, to] = [middle - span / 2, middle + span / 2];
    // The view does not leave what there is to see by more than its own width.
    const shift = Math.max(this.whole[0] - span - from, 0) + Math.min(this.whole[1] + span - to, 0);
    span = to - from;
    this.from = from + shift;
    this.to = this.from + span;
    this.draw();
    this.keep();
  }

  zoom(factor: number, around = 0.5): void {
    const at = this.from + (this.to - this.from) * around;
    this.show(at - (at - this.from) / factor, at + (this.to - at) / factor);
  }

  pan(part: number): void {
    const move = (this.to - this.from) * part;
    this.show(this.from + move, this.to + move);
  }

  fit(): void {
    // Exactly the whole, so that the address says nothing of a view that is none.
    [this.from, this.to] = this.whole;
    this.draw();
    this.keep();
  }

  private listen(): void {
    // The keys of the view, when it has the focus; a link of a mark keeps its own keys.
    this.frame.addEventListener('keydown', event => {
      if (event.ctrlKey || event.altKey || event.metaKey) {
        return;
      }

      const taken = ((): boolean => {
        switch (event.key) {
          case 'ArrowLeft': this.pan(-0.2); return true;
          case 'ArrowRight': this.pan(0.2); return true;
          case '+': case '=': this.zoom(1.5); return true;
          case '-': this.zoom(1 / 1.5); return true;
          case '0': this.fit(); return true;
          default: return false;
        }
      })();
      if (taken) {
        event.preventDefault();
      }
    });

    // The wheel moves the page, as everywhere; with the key for it (and with two fingers on a pad, which a
    // browser reports the same way) it makes the view larger and smaller around the pointer.
    this.frame.addEventListener('wheel', event => {
      if (event.ctrlKey || event.metaKey) {
        event.preventDefault();
        const box = this.frame.getBoundingClientRect();
        this.zoom(event.deltaY < 0 ? 1.25 : 0.8, Math.min(Math.max((event.clientX - box.left) / box.width, 0), 1));
      }
    }, { passive: false });

    // One finger or the mouse moves the view sideways; two fingers make it larger and smaller. A press that
    // does not move is the press of a link.
    const fingers = new Map<number, number>();
    let moved = false, apart = 0;
    this.frame.addEventListener('pointerdown', event => {
      fingers.set(event.pointerId, event.clientX);
      moved = false;
      apart = 0;
    });
    this.frame.addEventListener('pointermove', event => {
      const before = fingers.get(event.pointerId);
      if (before === undefined) {
        return;
      }

      fingers.set(event.pointerId, event.clientX);
      const inner = this.width - 2 * sidePad;
      if (fingers.size === 1) {
        if (Math.abs(event.clientX - before) > 0) {
          if (moved || Math.abs(event.clientX - before) > 3) {
            moved = true;
            this.frame.setPointerCapture(event.pointerId);
            this.pan(-(event.clientX - before) / inner);
          } else {
            fingers.set(event.pointerId, before);
          }
        }
      } else if (fingers.size === 2) {
        const [one, other] = [...fingers.values()] as [number, number];
        const now = Math.abs(one - other);
        if (apart > 0 && now > 0) {
          moved = true;
          const box = this.frame.getBoundingClientRect();
          this.zoom(now / apart, Math.min(Math.max(((one + other) / 2 - box.left) / box.width, 0), 1));
        }

        apart = now;
      }
    });
    const lift = (event: PointerEvent): void => {
      fingers.delete(event.pointerId);
      apart = 0;
    };
    this.frame.addEventListener('pointerup', lift);
    this.frame.addEventListener('pointercancel', lift);
    this.frame.addEventListener('click', event => {
      if (moved) {
        event.preventDefault();
        moved = false;
      }
    }, true);
  }
}

function start(host: HTMLElement): void {
  const data = host.dataset['timeline'];
  const still = host.querySelector<HTMLElement>('[data-timeline-still]');
  const live = host.querySelector<HTMLElement>('[data-timeline-live]');
  const views = host.querySelector<HTMLElement>('[data-timeline-views]');
  const template = host.querySelector<HTMLTemplateElement>('template[data-timeline-view]');
  const back = host.querySelector<HTMLElement>('[data-timeline-to-live-row]');
  if (!data || !still || !live || !views || !template || !back || host.dataset['state']) {
    return;
  }

  try {
    const timeline = JSON.parse(data) as Data;
    if (timeline.views.length === 0) {
      // Nothing has a place on an axis: the page says so itself.
      host.dataset['state'] = 'empty';
      return;
    }

    // The views are measured where they are shown: the place is opened first.
    live.hidden = false;
    still.hidden = true;
    const drawn = timeline.views.map((view, index) => {
      const part = template.content.firstElementChild!.cloneNode(true) as HTMLElement;
      part.querySelector<HTMLElement>('[data-view-title]')!.textContent = view.title;
      part.dataset['axis'] = view.axis;
      views.append(part);
      const made = new TimelineView(view, timeline.words, part.querySelector<HTMLElement>('[data-view-frame]')!, index);
      const on = (name: string, act: () => void): void => part.querySelector<HTMLElement>(`[data-view-${name}]`)?.addEventListener('click', act);
      on('earlier', () => made.pan(-0.4));
      on('later', () => made.pan(0.4));
      on('in', () => made.zoom(1.5));
      on('out', () => made.zoom(1 / 1.5));
      on('whole', () => made.fit());
      return made;
    });

    const show = (moving: boolean): void => {
      live.hidden = !moving;
      still.hidden = moving;
      back.hidden = moving;
      host.dataset['state'] = moving ? 'live' : 'still';
    };
    const choose = (moving: boolean): void => {
      show(moving);
      keepStill(still, !moving);
    };
    host.querySelector<HTMLElement>('[data-timeline-to-still]')?.addEventListener('click', () => choose(false));
    host.querySelector<HTMLElement>('[data-timeline-to-live]')?.addEventListener('click', () => choose(true));
    host.dataset['state'] = 'live';
    // The visitor chose the still chart and then one of its tools, which asked for the page again: it stays.
    if (stillChosen()) {
      choose(false);
    }
    // What a test of the browser asks the views.
    (host as unknown as { views: TimelineView[] }).views = drawn;
  } catch {
    // Whatever went wrong: the still chart is the chart.
    live.hidden = true;
    still.hidden = false;
    host.dataset['state'] = 'failed';
  }
}

for (const host of document.querySelectorAll<HTMLElement>('dastan-timeline')) {
  start(host);
}
