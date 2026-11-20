// DastanGraph in the browser (ADR 0020): a family, the connections of a node or a tree of a reader as a
// drawing that can be moved, made larger and smaller, walked with the keyboard and saved as a picture. What it
// draws is what the address of the graph answers – which is what the page of the same visitor lists – and
// where a card of a family stands is decided by the layout of Dastan, not here. The control that draws
// (Syncfusion EJ2 Diagram) appears in this file only; the page speaks of nodes, edges and their words.
//
// The script is asked for by a page that holds a <dastan-graph>, after the page is shown. Until it has drawn,
// and where it cannot, the still drawing of the server stays.
import {
  ConnectorConstraints, type ConnectorModel, Diagram, DiagramConstraints, DiagramTools, ImageElement,
  NodeConstraints, type NodeModel, type PointPortModel, PortVisibility, PrintAndExport, Size, SnapConstraints, StackPanel, TextElement,
} from '@syncfusion/ej2-diagrams';
import { registerLicense, Telemetry } from '@syncfusion/ej2-base';

// Found in a browser (step 19.8): the control comes with a count of its use that it sends to a host of its
// maker (Application Insights) when a page runs at an address of development. Dastan sends nothing about a
// reader to anybody: the count is switched off before the control is made, and the rules of the site (its
// content security policy, which allows connections to the site alone) would refuse the sending in any case.
Telemetry.disable();
Diagram.Inject(PrintAndExport);

interface Picture { source: string; sizes: string | null; words: string; }
interface GraphNode {
  id: string; name: string; address: string; profile: string; nameLanguage?: string; kind?: string;
  picture?: Picture; lines: string[]; row?: number; place?: number; expandable: boolean;
}
interface GraphEdge {
  id: string; from: string; to: string; role: string; label: string; directed: boolean;
  labelLanguage?: string; when?: string; source?: string; disputed: boolean;
}
interface Graph { kind: string; focus: string | null; nodes: GraphNode[]; edges: GraphEdge[]; }

// The measures of a card, in pixels of the drawing. Every card has the same width, so that the places of the
// layout – counted in cards – are columns of the drawing.
const cardWidth = 220, pictureSize = 44, gap = 8, inset = 8;
const columnStep = cardWidth + 40, rowStep = 150;
// Above this number of cards only what is in sight is drawn.
const manyNodes = 150;

const rightToLeftLetter = /[֐-ࣿיִ-ﻼ]/;

// Workaround 1 of ADR 0020: the control has no direction for a text. The diagram is kept left to right, and a
// line that holds right-to-left letters is given its direction by the marks of Unicode: "right-to-left
// embedding" … "pop", and a "left-to-right mark" after them, without which WebKit takes two such lines as one
// run and draws their words in each other's line. The marks have no width.
const directed = (line: string): string => rightToLeftLetter.test(line) ? '‫' + line + '‬‎' : line;

function here(address: string): string {
  return new URL(address, document.baseURI).href;
}

interface Look { surface: string; raised: string; text: string; muted: string; border: string; accent: string; mark: string; font: string; }

// The colours and the font of the page, so that the drawing is of the page in its light and its dark form.
function lookOf(host: HTMLElement): Look {
  const style = getComputedStyle(host);
  const colour = (name: string, otherwise: string): string => style.getPropertyValue(name).trim() || otherwise;
  return {
    surface: colour('--surface', '#fbfaf7'), raised: colour('--surface-raised', '#ffffff'), text: colour('--text', '#1c1b2e'),
    muted: colour('--text-muted', '#56556b'), border: colour('--border', '#d9d6e3'), accent: colour('--accent', '#3b3a8f'),
    mark: colour('--mark', '#92400e'), font: style.fontFamily || 'sans-serif',
  };
}

class DastanGraph {
  private readonly rightToLeft: boolean;
  private readonly look: Look;
  private readonly byKey = new Map<string, GraphNode>();
  private readonly keyOf = new Map<string, string>();
  private readonly spot = new Map<string, { x: number; y: number }>();
  private diagram!: Diagram;
  private selected: string | null = null;

  constructor(private readonly host: HTMLElement, private readonly canvas: HTMLElement, private readonly graph: Graph) {
    this.rightToLeft = getComputedStyle(host).direction === 'rtl';
    this.look = lookOf(host);
    // The control does not take every identifier; the nodes and the edges are numbered for it.
    graph.nodes.forEach((node, index) => {
      this.byKey.set('n' + index, node);
      this.keyOf.set(node.id, 'n' + index);
    });
  }

  // Where the cards stand. A family: the row and the place the layout of Dastan gave, mirrored on a page that
  // is read from right to left, as the still drawing is. Anything else: the focus in the middle and the
  // others around it, on one round.
  private place(): void {
    const nodes = this.graph.nodes;
    if (nodes.length > 0 && nodes.every(node => node.row !== undefined && node.place !== undefined)) {
      const last = Math.max(...nodes.map(node => node.place!));
      for (const node of nodes) {
        const along = this.rightToLeft ? last - node.place! : node.place!;
        this.spot.set(node.id, { x: along * columnStep, y: node.row! * rowStep });
      }

      return;
    }

    const around = nodes.filter(node => node.id !== this.graph.focus);
    if (around.length < nodes.length) {
      this.spot.set(this.graph.focus!, { x: 0, y: 0 });
    }

    // One round, which grows with the number of cards on it. Seen in a browser with two rounds: the line to
    // a card of the outer round went through a card of the inner one, with its label on that card.
    const across = Math.max(380, around.length * 46), down = Math.max(240, around.length * 30);
    around.forEach((node, index) => {
      const angle = (2 * Math.PI * index) / around.length - Math.PI / 2;
      this.spot.set(node.id, { x: Math.cos(angle) * across, y: Math.sin(angle) * down });
    });
  }

  private text(content: string, size: number, bold: boolean, colour: string, width: number): TextElement {
    const element = new TextElement();
    const side = this.rightToLeft ? 'Right' : 'Left';
    element.content = content;
    element.width = width;
    element.horizontalAlignment = side;
    // Workaround 2: the control's 'Wrap' breaks a line inside a word and tears joined letters apart;
    // 'WrapWithOverflow' breaks at spaces only.
    Object.assign(element.style, { color: colour, fontSize: size, fontFamily: this.look.font, bold, textWrapping: 'WrapWithOverflow', textAlign: side, strokeColor: 'none', fill: 'none' });
    // The marks of the direction stand around each line, and the lines are made by the control: it wraps the
    // text once, and gets the lines it found back with their marks, parted by line feeds, which it keeps.
    if (rightToLeftLetter.test(content)) {
      element.measure(new Size(width, Number.MAX_VALUE));
      const lines = (element as unknown as { childNodes: { text: string }[] }).childNodes;
      element.content = lines.map(line => directed(line.text.trim())).join('\n');
    }

    return element;
  }

  // The card of a node: its picture, its name and the lines of its card – ordinary elements of the control,
  // which it lays out, leaves out when out of sight and writes into a picture.
  private card(model: NodeModel): StackPanel {
    const key = model.id!;
    const node = this.byKey.get(key)!;
    const isFocus = node.id === this.graph.focus;
    const row = new StackPanel();
    row.id = key + '-card';
    row.orientation = 'Horizontal';
    Object.assign(row.style, { fill: this.look.raised, strokeColor: isFocus ? this.look.accent : this.look.border, strokeWidth: isFocus ? 2.5 : 1 });
    row.cornerRadius = 8;
    // Workaround 5: a padding or a margin always names all four sides; one that names a side only makes sizes
    // "not a number" and the control fail far from here.
    row.padding = { left: inset, right: inset, top: inset, bottom: inset };

    const wordsWidth = cardWidth - 2 * inset - (node.picture ? pictureSize + gap : 0);
    const words = new StackPanel();
    words.id = key + '-words';
    words.orientation = 'Vertical';
    Object.assign(words.style, { fill: 'none', strokeColor: 'none' });
    // A width alone is not honoured by a container; a least width is.
    words.minWidth = wordsWidth;
    const name = this.text(node.name, 15, true, this.look.text, wordsWidth);
    name.id = key + '-name';
    const lines = node.lines.map((line, index) => {
      const element = this.text(line, 12, false, this.look.muted, wordsWidth);
      element.id = key + '-line' + index;
      return element;
    });
    words.children = [name, ...lines];
    if (!node.picture) {
      row.children = [words];
      return row;
    }

    const image = new ImageElement();
    image.id = key + '-picture';
    image.source = here(node.picture.source);
    image.width = pictureSize;
    image.height = pictureSize;
    image.margin = this.rightToLeft ? { left: gap, right: 0, top: 0, bottom: 0 } : { left: 0, right: gap, top: 0, bottom: 0 };
    Object.assign(image.style, { fill: 'none', strokeColor: 'none' });
    // A row of the control has no order of its own for a page read from right to left; the picture is simply
    // given last there, and stands at the right.
    row.children = this.rightToLeft ? [words, image] : [image, words];
    return row;
  }

  // Workaround 3: two lines between the same two cards lie on one another, and one label hides the other. So
  // every end of a line gets a point of its own on the side of the card that looks at the other card.
  private lines(): { connectors: ConnectorModel[]; ports: Map<string, PointPortModel[]> } {
    const ports = new Map<string, PointPortModel[]>();
    const connectors: ConnectorModel[] = [];
    const together = new Map<string, GraphEdge[]>();
    for (const edge of this.graph.edges) {
      const pair = [edge.from, edge.to].sort().join('\u0000');
      together.set(pair, [...(together.get(pair) ?? []), edge]);
    }

    const family = this.graph.kind !== 'connections';
    let number = 0;
    for (const edges of together.values()) {
      edges.forEach((edge, index) => {
        const [first, second] = [edges[0]!.from, edges[0]!.to].sort() as [string, string];
        const a = this.spot.get(first), b = this.spot.get(second);
        const from = this.keyOf.get(edge.from), to = this.keyOf.get(edge.to);
        if (!a || !b || !from || !to || edge.from === edge.to) {
          return;
        }

        const share = (index + 0.5) / edges.length;
        const upright = Math.abs(b.y - a.y) >= Math.abs(b.x - a.x) * 0.6;
        const atFirst = upright ? { x: share, y: b.y > a.y ? 1 : 0 } : { x: b.x > a.x ? 1 : 0, y: share };
        const atSecond = upright ? { x: share, y: b.y > a.y ? 0 : 1 } : { x: b.x > a.x ? 0 : 1, y: share };
        const id = 'e' + number++;
        const port = (node: string, offset: { x: number; y: number }, end: string): string => {
          const key = this.keyOf.get(node)!;
          ports.set(key, [...(ports.get(key) ?? []), { id: id + end, offset, visibility: PortVisibility.Hidden }]);
          return id + end;
        };
        const firstPort = port(first, atFirst, 'a'), secondPort = port(second, atSecond, 'b');
        const colour = edge.disputed ? this.look.mark : this.look.muted;
        // In a family the rows say who is a parent of whom; a line is named where it is something else. Among
        // connections every line is named, with its time where it has one.
        const named = !family || (edge.role !== 'parent' && edge.role !== 'partner');
        const label = edge.when ? edge.label + ' · ' + edge.when : edge.label;
        connectors.push({
          id,
          sourceID: from, targetID: to,
          sourcePortID: edge.from === first ? firstPort : secondPort,
          targetPortID: edge.to === second ? secondPort : firstPort,
          type: family && upright ? 'Orthogonal' : 'Straight',
          cornerRadius: 6,
          constraints: ConnectorConstraints.None,
          style: { strokeColor: colour, strokeWidth: 1.5, strokeDashArray: edge.disputed ? '3 3' : edge.role === 'parent' || edge.role === 'partner' || !family ? '' : '6 3' },
          targetDecorator: edge.directed && !(family && edge.role === 'parent')
            ? { shape: 'Arrow', style: { fill: colour, strokeColor: colour } }
            : { shape: 'None' },
          // The words of a line stay on one line of text: the control would break them at every space, and
          // the marks of the direction must stand around a whole line.
          // Seen in Firefox, where the letters are a little larger: the labels of two lines between the same
          // two cards touched. So each also stands at its own part of the way along its line.
          annotations: named ? [{ content: directed(label), offset: share, style: { fontFamily: this.look.font, fontSize: 12, color: this.look.text, fill: this.look.surface, textWrapping: 'NoWrap' } }] : [],
        });
      });
    }

    return { connectors, ports };
  }

  draw(): void {
    this.place();
    const { connectors, ports } = this.lines();
    const fixed = NodeConstraints.Default & ~NodeConstraints.Drag & ~NodeConstraints.Rotate & ~NodeConstraints.Resize & ~NodeConstraints.Delete;
    const nodes: NodeModel[] = this.graph.nodes.map(node => {
      const key = this.keyOf.get(node.id)!;
      const at = this.spot.get(node.id) ?? { x: 0, y: 0 };
      return { id: key, width: cardWidth, height: 72, offsetX: at.x, offsetY: at.y, constraints: fixed, ports: ports.get(key) ?? [] };
    });
    this.diagram = new Diagram({
      width: '100%', height: '100%',
      nodes, connectors,
      setNodeTemplate: (model: NodeModel) => this.card(model),
      tool: DiagramTools.ZoomPan | DiagramTools.SingleSelect,
      snapSettings: { constraints: SnapConstraints.None },
      constraints: nodes.length > manyNodes ? DiagramConstraints.Default | DiagramConstraints.Virtualization : DiagramConstraints.Default,
      // The keys are Dastan's (see the keys below): the control gets none of its own.
      commandManager: { commands: [] },
      selectionChange: (change: { state?: string; newValue?: { id?: string }[] }) => {
        if (change.state === 'Changed') {
          this.say(change.newValue?.[0]?.id ?? null);
        }
      },
      doubleClick: () => this.openProfile(),
    });
    this.diagram.appendTo(this.canvas);
    this.diagram.fitToPage({ mode: 'Page', region: 'Content', canZoomIn: false, margin: { left: 24, right: 24, top: 24, bottom: 24 } });
    this.oneStop();
    this.keys();
  }

  // The keyboard of ADR 0020: the drawing is one stop of Tab. Nothing inside it takes the focus by itself.
  private oneStop(): void {
    for (const inside of this.canvas.querySelectorAll<HTMLElement>('[tabindex]')) {
      inside.tabIndex = -1;
    }
  }

  private node(key: string | null): GraphNode | null {
    return key ? this.byKey.get(key) ?? null : null;
  }

  // What is chosen is said: to a reader who hears the page, and to the buttons beside the drawing.
  private say(key: string | null): void {
    const node = this.node(key);
    this.selected = node ? key : null;
    const said = this.host.querySelector<HTMLElement>('[data-graph-said]');
    const open = this.host.querySelector<HTMLButtonElement>('[data-graph-profile]');
    if (open) {
      open.disabled = !node;
    }

    if (!said) {
      return;
    }

    if (!node) {
      said.textContent = '';
      return;
    }

    const name = (id: string): string => this.graph.nodes.find(other => other.id === id)?.name ?? '';
    const relations = this.graph.edges
      .filter(edge => edge.from === node.id || edge.to === node.id)
      .map(edge => `${name(edge.from)} – ${edge.label}${edge.when ? ' (' + edge.when + ')' : ''} – ${name(edge.to)}`);
    said.textContent = [node.name, ...node.lines, ...relations].join('. ');
  }

  private choose(key: string): void {
    const object = this.diagram.getObject(key) as NodeModel & { wrapper?: { bounds: { x: number; y: number; width: number; height: number } } };
    this.diagram.select([object]);
    if (object.wrapper) {
      this.diagram.bringIntoView(object.wrapper.bounds as never);
    }
  }

  // From a card to the one that stands next to it in the direction of the arrow: the nearest one on that
  // side, where a card straight ahead counts as nearer than one far off to the side.
  private step(dx: number, dy: number): void {
    const from = this.node(this.selected);
    if (!from) {
      const first = this.keyOf.get(this.graph.focus ?? '') ?? (this.graph.nodes.length > 0 ? 'n0' : null);
      if (first) {
        this.choose(first);
      }

      return;
    }

    const at = this.spot.get(from.id)!;
    let best: string | null = null, least = Number.MAX_VALUE;
    for (const other of this.graph.nodes) {
      const there = this.spot.get(other.id);
      if (!there || other.id === from.id) {
        continue;
      }

      const ahead = (there.x - at.x) * dx + (there.y - at.y) * dy;
      const aside = Math.abs((there.x - at.x) * dy) + Math.abs((there.y - at.y) * dx);
      if (ahead > 0 && ahead + 2 * aside < least) {
        least = ahead + 2 * aside;
        best = other.id;
      }
    }

    if (best) {
      this.choose(this.keyOf.get(best)!);
    }
  }

  private keys(): void {
    // Before the control hears of a key: it would move the chosen card with an arrow, walk nodes and lines
    // with Tab in the order they were made, and never let Tab leave.
    this.canvas.addEventListener('keydown', event => {
      event.stopPropagation();
      if (event.ctrlKey || event.altKey || event.metaKey) {
        return;
      }

      const taken = ((): boolean => {
        switch (event.key) {
          case 'ArrowLeft': this.step(-1, 0); return true;
          case 'ArrowRight': this.step(1, 0); return true;
          case 'ArrowUp': this.step(0, -1); return true;
          case 'ArrowDown': this.step(0, 1); return true;
          case 'Enter': case ' ': this.openProfile(); return true;
          case 'Escape': this.diagram.clearSelection(); this.say(null); return true;
          case '+': case '=': this.zoom(1.25); return true;
          case '-': this.zoom(0.8); return true;
          case '0': this.fit(); return true;
          default: return false;
        }
      })();
      if (taken) {
        event.preventDefault();
      }
    }, true);
    for (const kind of ['keyup', 'keypress']) {
      this.canvas.addEventListener(kind, event => event.stopPropagation(), true);
    }
  }

  /** What a test of the browser reads: how large the drawing is shown, what is chosen, and where the cards stand. */
  get seen(): { zoom: number; chosen: string | null; cards: { id: string; x: number; y: number; width: number; height: number }[] } {
    return {
      zoom: this.diagram.scrollSettings.currentZoom ?? 1,
      chosen: this.node(this.selected)?.id ?? null,
      cards: this.graph.nodes.map(node => {
        const bounds = (this.diagram.getObject(this.keyOf.get(node.id)!) as { wrapper?: { bounds: { x: number; y: number; width: number; height: number } } }).wrapper!.bounds;
        return { id: node.id, x: bounds.x, y: bounds.y, width: bounds.width, height: bounds.height };
      }),
    };
  }

  zoom(factor: number): void {
    this.diagram.zoomTo({ type: factor > 1 ? 'ZoomIn' : 'ZoomOut', zoomFactor: Math.abs(factor - 1) });
  }

  fit(): void {
    // The control makes a drawing smaller to fit and never larger; so it is first shown at its own size.
    this.diagram.reset();
    this.diagram.fitToPage({ mode: 'Page', region: 'Content', canZoomIn: false, margin: { left: 24, right: 24, top: 24, bottom: 24 } });
  }

  // The profile of what is chosen opens in the panel of the page (ADR 0017), outside the drawing.
  openProfile(): void {
    const node = this.node(this.selected);
    if (node) {
      document.dispatchEvent(new CustomEvent('dastan:profile', { detail: { address: node.profile, name: node.name } }));
    }
  }

  // Workaround 4: what the control writes as a picture holds the texts in the right order but names the font
  // and the pictures of the page, which a file by itself does not have; and its PNG is soft. So the font and
  // the pictures are put into the SVG, and the PNG is drawn from that SVG at twice the size.
  async picture(format: 'svg' | 'png'): Promise<Blob> {
    const margin = { left: 16, right: 16, top: 16, bottom: 16 };
    const made = this.diagram.exportDiagram({ format: 'SVG', mode: 'Data', region: 'Content', margin });
    const svg = (typeof made === 'string' ? new DOMParser().parseFromString(made, 'image/svg+xml').documentElement : made) as unknown as SVGSVGElement;
    const inFile = async (address: string): Promise<string> => {
      const blob = await (await fetch(address)).blob();
      return await new Promise<string>((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(String(reader.result));
        reader.onerror = () => reject(reader.error ?? new Error('read'));
        reader.readAsDataURL(blob);
      });
    };
    for (const image of svg.querySelectorAll('image')) {
      const address = image.getAttribute('href') ?? image.getAttributeNS('http://www.w3.org/1999/xlink', 'href');
      if (address && !address.startsWith('data:')) {
        image.removeAttributeNS('http://www.w3.org/1999/xlink', 'href');
        image.setAttribute('href', await inFile(address));
      }
    }

    const font = this.host.dataset['font'];
    if (font && svg.textContent && rightToLeftLetter.test(svg.textContent)) {
      const style = document.createElementNS('http://www.w3.org/2000/svg', 'style');
      style.textContent = `@font-face { font-family: Vazirmatn; font-weight: 100 900; src: url(${await inFile(here(font))}) format("woff2"); }`;
      svg.insertBefore(style, svg.firstChild);
    }

    const text = new XMLSerializer().serializeToString(svg);
    const file = new Blob([text], { type: 'image/svg+xml' });
    if (format === 'svg') {
      return file;
    }

    const address = URL.createObjectURL(file);
    try {
      const image = new Image();
      image.src = address;
      await image.decode();
      const canvas = document.createElement('canvas');
      canvas.width = image.width * 2;
      canvas.height = image.height * 2;
      const context = canvas.getContext('2d')!;
      context.fillStyle = this.look.surface;
      context.fillRect(0, 0, canvas.width, canvas.height);
      context.drawImage(image, 0, 0, canvas.width, canvas.height);
      return await new Promise<Blob>((resolve, reject) => canvas.toBlob(blob => blob ? resolve(blob) : reject(new Error('png')), 'image/png'));
    } finally {
      URL.revokeObjectURL(address);
    }
  }
}

function save(blob: Blob, name: string): void {
  const link = document.createElement('a');
  link.href = URL.createObjectURL(blob);
  link.download = name;
  link.click();
  setTimeout(() => URL.revokeObjectURL(link.href), 10_000);
}

async function start(host: HTMLElement): Promise<void> {
  const source = host.dataset['source'];
  const still = host.querySelector<HTMLElement>('[data-graph-still]');
  const live = host.querySelector<HTMLElement>('[data-graph-live]');
  const canvas = host.querySelector<HTMLElement>('[data-graph-canvas]');
  const tools = host.querySelector<HTMLElement>('[data-graph-tools]');
  const liveTools = host.querySelector<HTMLElement>('[data-graph-live-tools]');
  const toLive = host.querySelector<HTMLElement>('[data-graph-to-live]');
  if (!source || !still || !live || !canvas || !tools || !liveTools || !toLive || host.dataset['state']) {
    return;
  }

  host.dataset['state'] = 'loading';
  try {
    const answer = await fetch(here(source), { headers: { Accept: 'application/json' }, cache: 'no-store' });
    if (!answer.ok) {
      throw new Error(String(answer.status));
    }

    const graph = await answer.json() as Graph;
    const began = performance.now();
    if (graph.nodes.length === 0) {
      throw new Error('empty');
    }

    if (host.dataset['key']) {
      registerLicense(host.dataset['key']);
    }

    // The drawing is measured where it is shown: the place is opened first.
    live.hidden = false;
    still.hidden = true;
    const drawn = new DastanGraph(host, canvas, graph);
    drawn.draw();
    // How long the drawing took, for the measures of step 19.8.
    host.dataset['drawnIn'] = String(Math.round(performance.now() - began));
    host.dataset['state'] = 'live';
    tools.hidden = false;
    const show = (moving: boolean): void => {
      live.hidden = !moving;
      still.hidden = moving;
      liveTools.hidden = !moving;
      toLive.hidden = moving;
      host.dataset['state'] = moving ? 'live' : 'still';
    };

    const on = (name: string, act: () => void): void => host.querySelector<HTMLElement>(`[data-graph-${name}]`)?.addEventListener('click', act);
    on('in', () => drawn.zoom(1.25));
    on('out', () => drawn.zoom(0.8));
    on('fit', () => drawn.fit());
    on('profile', () => drawn.openProfile());
    on('svg', () => void drawn.picture('svg').then(blob => save(blob, 'dastan.svg')));
    on('png', () => void drawn.picture('png').then(blob => save(blob, 'dastan.png')));
    on('to-still', () => show(false));
    on('to-live', () => { show(true); drawn.fit(); });
    // What a test of the browser asks the drawing.
    (host as unknown as { graph: DastanGraph }).graph = drawn;
  } catch {
    // Whatever went wrong: the still drawing is the drawing.
    live.hidden = true;
    still.hidden = false;
    tools.hidden = true;
    host.dataset['state'] = 'failed';
  }
}

for (const host of document.querySelectorAll<HTMLElement>('dastan-graph')) {
  void start(host);
}
