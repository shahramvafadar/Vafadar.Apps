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
import { carry, explores, said } from './explore';
import { drawn as profileDrawn, type Profile } from './profile-panel';

// Found in a browser (step 19.8): the control comes with a count of its use that it sends to a host of its
// maker (Application Insights) when a page runs at an address of development. Dastan sends nothing about a
// reader to anybody: the count is switched off before the control is made, and the rules of the site (its
// content security policy, which allows connections to the site alone) would refuse the sending in any case.
Telemetry.disable();
Diagram.Inject(PrintAndExport);

interface Picture { source: string; sizes: string | null; words: string; }
interface GraphNode {
  id: string; name: string; address: string; profile: string; nameLanguage?: string; kind?: string;
  picture?: Picture; lines: string[]; row?: number; place?: number; expandable: boolean; timeline?: string; gallery?: string;
  centre?: string; compare?: string; size?: 'main' | 'compact';
}
interface GraphEdge {
  id: string; from: string; to: string; role: string; label: string; directed: boolean;
  labelLanguage?: string; when?: string; source?: string; disputed: boolean;
}
interface Graph { kind: string; focus: string | null; nodes: GraphNode[]; edges: GraphEdge[]; flow?: string; }

// The measures of a card, in pixels of the drawing. A card has one of three sizes (step 25.6), which the
// server names with the graph so that this drawing and the still one agree: the name in the middle is larger,
// a card of a drawing with many is compact – its name and one line, no picture –, every other is the usual.
// In a family every card has one size, so that the places of the layout – counted in cards – are columns.
interface CardSize { width: number; height: number; name: number; line: number; picture: number; lines: number; }
const gap = 8, inset = 8;
const cardSizes: Record<'main' | 'usual' | 'compact', CardSize> = {
  main: { width: 264, height: 88, name: 17, line: 13, picture: 56, lines: 3 },
  usual: { width: 220, height: 72, name: 15, line: 12, picture: 44, lines: 3 },
  compact: { width: 184, height: 52, name: 13, line: 11, picture: 0, lines: 1 },
};
const sizeOf = (node: GraphNode): CardSize => cardSizes[node.size ?? 'usual'];
// The steps of a family: a column and a row for the widest and the highest card it has. Across the page
// (step 20.8) a column is a generation, with the room its lines need between two columns, and the cards of
// one generation stand under each other.
interface Steps { column: number; row: number; generation: number; beside: number; }
function stepsOf(nodes: GraphNode[]): Steps {
  const width = Math.max(...nodes.map(node => sizeOf(node).width)), height = Math.max(...nodes.map(node => sizeOf(node).height));
  return { column: width + 40, row: height + 78, generation: width + 90, beside: height + 52 };
}
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
  // The inspector (step 20.8): what is chosen, shown closer beside the drawing – a sheet at the foot of a phone.
  private readonly inspector: HTMLElement | null;
  private asked: AbortController | null = null;
  private byKeyboard = false;
  private fitNext = false;
  private inHistory = false;
  // Where the drawing stood (ADR 0025): the place of this drawing among those of the page, whether the visitor
  // moved or resized it, and whether it was just put back where it had been left.
  private readonly slot: string;
  private moved = false;
  private asLeft = false;
  private left: { zoom: number; x: number; y: number } | null = null;
  private keeping = 0;

  constructor(private readonly host: HTMLElement, private readonly canvas: HTMLElement, private readonly graph: Graph) {
    this.rightToLeft = getComputedStyle(host).direction === 'rtl';
    this.look = lookOf(host);
    this.inspector = host.querySelector<HTMLElement>('[data-graph-inspector]');
    this.slot = 'g' + [...document.querySelectorAll('dastan-graph')].indexOf(host);
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
      // Across the page the generations are columns: the oldest where the page begins – at the right of a
      // page that is read from right to left –, and the places of a generation run down the drawing.
      const across = this.graph.flow === 'across';
      const youngest = Math.max(...nodes.map(node => node.row!));
      const steps = stepsOf(nodes);
      for (const node of nodes) {
        if (across) {
          this.spot.set(node.id, { x: (this.rightToLeft ? youngest - node.row! : node.row!) * steps.generation, y: node.place! * steps.beside });
          continue;
        }

        const along = this.rightToLeft ? last - node.place! : node.place!;
        this.spot.set(node.id, { x: along * steps.column, y: node.row! * steps.row });
      }

      return;
    }

    const around = nodes.filter(node => node.id !== this.graph.focus);
    if (around.length < nodes.length) {
      this.spot.set(this.graph.focus!, { x: 0, y: 0 });
    }

    // One round, which grows with the number of cards on it. Seen in a browser with two rounds: the line to
    // a card of the outer round went through a card of the inner one, with its label on that card.
    // A larger card in the middle pushes the round out by what it is larger, so that the lines to it stay as
    // long as they were and their labels keep their room (seen in Firefox: two labels of one pair touched).
    const middle = nodes.find(node => node.id === this.graph.focus);
    const wider = middle ? (sizeOf(middle).width - cardSizes.usual.width) / 2 : 0, higher = middle ? (sizeOf(middle).height - cardSizes.usual.height) / 2 : 0;
    const across = Math.max(380, around.length * 46) + wider, down = Math.max(240, around.length * 30) + higher;
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

    const size = sizeOf(node);
    // A compact card has no picture: a drawing with many cards is read by its names.
    const picture = size.picture > 0 ? node.picture : undefined;
    const wordsWidth = size.width - 2 * inset - (picture ? size.picture + gap : 0);
    const words = new StackPanel();
    words.id = key + '-words';
    words.orientation = 'Vertical';
    Object.assign(words.style, { fill: 'none', strokeColor: 'none' });
    // A width alone is not honoured by a container; a least width is.
    words.minWidth = wordsWidth;
    const name = this.text(node.name, size.name, true, this.look.text, wordsWidth);
    name.id = key + '-name';
    // A compact card says what the node is where the server says it, and else the first of its lines.
    const said = size.lines === 1 && node.kind ? [node.kind] : node.lines.slice(0, size.lines);
    const lines = said.map((line, index) => {
      const element = this.text(line, size.line, false, this.look.muted, wordsWidth);
      element.id = key + '-line' + index;
      return element;
    });
    words.children = [name, ...lines];
    if (!picture) {
      row.children = [words];
      return row;
    }

    const image = new ImageElement();
    image.id = key + '-picture';
    image.source = here(picture.source);
    image.width = size.picture;
    image.height = size.picture;
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
          // The lines of a family run at right angles, down the drawing or – where it grows across – along it.
          type: family && (upright || this.graph.flow === 'across') ? 'Orthogonal' : 'Straight',
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
      const size = sizeOf(node);
      return { id: key, width: size.width, height: size.height, offsetX: at.x, offsetY: at.y, constraints: fixed, ports: ports.get(key) ?? [] };
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
      doubleClick: () => this.toInspector(),
      scrollChange: () => this.keepStanding(),
    });
    this.diagram.appendTo(this.canvas);
    this.diagram.fitToPage({ mode: 'Page', region: 'Content', canZoomIn: false, margin: { left: 24, right: 24, top: 24, bottom: 24 } });

    // A visitor who comes back to this place of the history finds the drawing as large and as far moved as it
    // was left; somebody who comes for the first time finds the whole of it.
    this.left = this.stood();
    if (this.left) {
      [this.moved, this.asLeft] = [true, true];
      this.putBack();
      // The control settles its own bars of scrolling a moment later and may then stand a pixel or two beside
      // (seen in WebKit on a narrow Persian page): once it has, the rest of the way is gone.
      requestAnimationFrame(() => requestAnimationFrame(() => this.putBack()));
    }

    const stood = this.left;

    // Somebody who leaves the page within that moment has moved the drawing all the same. What waits to be
    // written is written when a finger lets go – a movement ends there –, and once more when the page is left;
    // the second alone would not do: Chromium and Firefox do not keep what is written while a page is left.
    const atOnce = (): void => {
      if (this.keeping !== 0) {
        this.write();
      }
    };
    for (const kind of ['pointerup', 'pointercancel', 'pagehide']) {
      window.addEventListener(kind, atOnce, { capture: true });
    }

    // Only what the visitor does is kept – a finger, a wheel, a key, a tool –, not what the page does by itself.
    for (const kind of ['pointerdown', 'wheel', 'keydown']) {
      this.host.addEventListener(kind, () => { [this.moved, this.asLeft] = [true, false]; }, { capture: true, passive: true });
    }

    this.oneStop();
    this.keys();
    this.inspectorKeys();

    // What the address says is chosen (ADR 0025) is chosen in the drawing as it is on the page of the server –
    // without taking the focus: the visitor has not asked for it in this drawing. The inspector of the drawing
    // then says what the page said about it, so the block of the page goes.
    const chosen = explores() ? this.keyOf.get(said('sel') ?? '') : undefined;
    if (chosen) {
      this.byKeyboard = true;
      this.choose(chosen);
      this.byKeyboard = false;
    }

    if (this.inspector && !this.isSheet) {
      document.querySelector('[data-explore-chosen]')?.remove();

      // Drawn with something chosen, the drawing is shown whole in the room the inspector leaves it: once
      // the page has given the inspector its room.
      this.fitNext = chosen !== undefined && !stood;
    }

    this.watchRoom();
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

    // What is chosen is part of the state of exploring (ADR 0025): it goes into the address and into the links
    // of the views, and what the page of the server said was chosen is taken away when it is no longer so.
    // What stands in the middle is not chosen in itself.
    const chosenNow = node && node.id !== this.graph.focus ? node.id : null;
    carry('sel', chosenNow);
    // Where the page still says what was chosen (a phone, whose sheet opens only when asked for), it says so
    // only while it is true.
    const written = document.querySelector<HTMLElement>('[data-explore-chosen]');
    if (written && written.dataset['exploreChosen'] !== chosenNow) {
      written.remove();
    }

    const said = this.host.querySelector<HTMLElement>('[data-graph-said]');
    this.inspect(node);

    // What is chosen is carried on: to its timeline and to its pictures, where it has any (R-EXP-05).
    for (const [name, address] of [['timeline', node?.timeline], ['gallery', node?.gallery]] as const) {
      const on = this.host.querySelector<HTMLAnchorElement>(`[data-graph-${name}]`);
      if (on) {
        on.hidden = !address;
        if (address) {
          on.href = here(address);
        } else {
          on.removeAttribute('href');
        }
      }
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

  // On a phone the inspector is a sheet over the foot of the drawing; beside the drawing where there is room.
  private get isSheet(): boolean {
    return matchMedia('(max-width: 60rem)').matches;
  }

  // The inspector shows what is chosen: at once the name the drawing has, then the profile the site answers
  // with – which is what the page of the same visitor shows. Nothing chosen, no inspector.
  private inspect(node: GraphNode | null): void {
    const inspector = this.inspector;
    if (!inspector) {
      return;
    }

    this.asked?.abort();
    const part = <T extends HTMLElement>(name: string): T | null => inspector.querySelector<T>(`[data-inspector-${name}]`);
    const [name, kind, body, wait, failed] = [part('name'), part('kind'), part('body'), part('wait'), part('failed')];
    if (!node || !name || !kind || !body || !wait || !failed) {
      const held = inspector.contains(document.activeElement);
      inspector.hidden = true;
      if (held) {
        this.canvas.focus();
      }


      this.leaveHistory();
      return;
    }

    name.textContent = node.name;
    if (node.nameLanguage) {
      name.lang = node.nameLanguage;
    } else {
      name.removeAttribute('lang');
    }

    kind.textContent = node.kind ?? '';
    body.replaceChildren();
    wait.hidden = false;
    failed.hidden = true;
    for (const [link, address] of [['centre', node.centre], ['open', node.address], ['compare', node.compare]] as const) {
      const to = part<HTMLAnchorElement>(link);
      if (to) {
        to.hidden = !address;
        if (address) {
          to.href = here(address);
        } else {
          to.removeAttribute('href');
        }
      }
    }

    // Beside the drawing the inspector follows what is chosen. A sheet lies over the drawing: it opens when the
    // visitor asks for it – a finger on a card, or Enter – and not because the keys walk past a card or an
    // address names one. Open already, it follows what is chosen.
    if (!this.isSheet || !this.byKeyboard) {
      inspector.hidden = false;
    }

    if (this.isSheet && !this.byKeyboard) {
      this.toInspector();
    }

    const asked = this.asked = new AbortController();
    void (async (): Promise<void> => {
      try {
        const answer = await fetch(here(node.profile), { headers: { Accept: 'application/json' }, cache: 'no-store', signal: asked.signal });
        if (!answer.ok) {
          throw new Error(String(answer.status));
        }

        const profile = await answer.json() as Profile;
        kind.textContent = profile.kind ?? '';
        body.replaceChildren(...profileDrawn(profile));
        wait.hidden = true;
      } catch (problem) {
        if (!(problem instanceof DOMException && problem.name === 'AbortError')) {
          wait.hidden = true;
          failed.hidden = false;
        }
      }
    })();
  }

  // Beside the drawing the inspector takes room from it, and gives it back; a window is made wider or
  // narrower. The control is not told by itself: whenever the room of the drawing changes it is told its new
  // room, and what is chosen is brought into it – or, once, the whole drawing is shown in it.
  private watchRoom(): void {
    if (typeof ResizeObserver !== 'function') {
      return;
    }

    let room = [this.canvas.clientWidth, this.canvas.clientHeight];
    new ResizeObserver(() => {
      const now = [this.canvas.clientWidth, this.canvas.clientHeight];
      if (now[0] === room[0] && now[1] === room[1]) {
        return;
      }

      room = now;
      this.diagram.updateViewPort();
      if (this.asLeft) {
        // Put back where it was left, it stays there until the visitor does something – however often the page
        // gives the drawing another room while it arrives (a note above it, a font, a bar of scrolling). The
        // control holds a drawing inside a room it is told of, so it is put back again (seen by eye: it came
        // back as large as it was left and at another place).
        this.putBack();
        return;
      }

      if (this.fitNext) {
        this.fitNext = false;
        this.fit();
        return;
      }

      // With nothing chosen the whole drawing is shown in its new room – otherwise a part of it would lie
      // outside, behind a bar that scrolls and that no key reaches.
      const chosen = this.selected ? this.diagram.getObject(this.selected) as { wrapper?: { bounds: unknown } } : null;
      if (chosen?.wrapper) {
        // In the middle of the new room: "into view" of the control keeps a card where it was when a part of
        // it was in the old room, also where the inspector now stands.
        this.diagram.bringToCenter(chosen.wrapper.bounds as never);
      } else if (!this.moved) {
        // What the visitor moved stays where the visitor put it; only a drawing nobody touched is shown whole
        // again – showing it whole would also forget where it stood.
        this.fit();
      }
    }).observe(this.canvas);
  }

  // The focus goes to the inspector: Enter in the drawing, or a finger on a card of a phone. A sheet is a
  // place of its own in the history, so that the Back button closes it and not the page.
  toInspector(): void {
    if (!this.inspector || !this.node(this.selected)) {
      return;
    }

    this.inspector.hidden = false;

    this.inspector.focus();
    if (this.isSheet && !this.inHistory) {
      history.pushState({ ...(history.state as object | null), inspector: true }, '');
      this.inHistory = true;
    }
  }

  // Back from the inspector to the drawing; what is chosen stays chosen.
  private toDrawing(): void {
    this.canvas.focus();
  }

  // Nothing is chosen any more: the inspector goes, and the focus is in the drawing again.
  private letGo(): void {
    this.diagram.clearSelection();
    this.say(null);
    this.canvas.focus();
  }

  private leaveHistory(): void {
    if (this.inHistory) {
      this.inHistory = false;
      history.back();
    }
  }

  private inspectorKeys(): void {
    const inspector = this.inspector;
    if (!inspector) {
      return;
    }

    inspector.addEventListener('keydown', event => {
      if (event.key === 'Escape') {
        event.preventDefault();
        // Beside the drawing Escape goes back to it; a sheet is closed by it, as a dialog is.
        if (this.isSheet) {
          this.letGo();
        } else {
          this.toDrawing();
        }
      }
    });
    inspector.querySelector<HTMLElement>('[data-inspector-close]')?.addEventListener('click', () => this.letGo());
    // The Back button of the browser closes a sheet that was opened.
    window.addEventListener('popstate', () => {
      if (this.inHistory) {
        this.inHistory = false;
        this.letGo();
      }
    });
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
        this.byKeyboard = true;
        this.choose(first);
        this.byKeyboard = false;
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
      this.byKeyboard = true;
      this.choose(this.keyOf.get(best)!);
      this.byKeyboard = false;
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
          case 'Enter': case ' ': this.toInspector(); return true;
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
  get seen(): { zoom: number; x: number; y: number; chosen: string | null; cards: { id: string; x: number; y: number; width: number; height: number }[] } {
    return {
      zoom: this.diagram.scroller.currentZoom,
      x: this.diagram.scroller.horizontalOffset,
      y: this.diagram.scroller.verticalOffset,
      chosen: this.node(this.selected)?.id ?? null,
      cards: this.graph.nodes.map(node => {
        const bounds = (this.diagram.getObject(this.keyOf.get(node.id)!) as { wrapper?: { bounds: { x: number; y: number; width: number; height: number } } }).wrapper!.bounds;
        return { id: node.id, x: bounds.x, y: bounds.y, width: bounds.width, height: bounds.height };
      }),
    };
  }

  zoom(factor: number): void {
    [this.moved, this.asLeft] = [true, false];
    this.diagram.zoomTo({ type: factor > 1 ? 'ZoomIn' : 'ZoomOut', zoomFactor: Math.abs(factor - 1) });
    // A tool or a key is one movement, and it has ended: written at once.
    this.write();
  }

  // Puts the drawing where it was left, while the visitor has done nothing since. The control takes a size and a
  // movement as steps from where it stands, not as a place to stand at: so it is first shown at its own size
  // and unmoved, then made as large, then moved as far as it was.
  private putBack(): void {
    const stood = this.left;
    const moved = this.diagram.scroller;
    if (!stood || !this.asLeft) {
      return;
    }

    const sameSize = Math.abs(moved.currentZoom - stood.zoom) < 0.0001;
    if (sameSize && Math.abs(moved.horizontalOffset - stood.x) < 0.5 && Math.abs(moved.verticalOffset - stood.y) < 0.5) {
      return;
    }

    // As large as it was already – the control only settled a little beside, or was held inside a new room:
    // the rest of the way is gone and nothing is begun anew (begun anew, WebKit settles beside again).
    if (!sameSize) {
      this.diagram.reset();
      this.diagram.zoom(stood.zoom, { x: 0, y: 0 });
    }

    // Workaround 6: a movement that no finger makes is not added to where the drawing stands – the control
    // (35.2.12, scroller.applyScrollLimit) adds the step and then turns the whole place around, so the
    // drawing ends at minus (where it stood plus the step). The step is chosen for that. A movement "of the
    // visitor" is added as it is, but it is held inside the drawing, and a visitor may have moved further.
    // The test of the browser fails in all three engines when a later version does it differently.
    this.diagram.pan(-stood.x - moved.horizontalOffset, -stood.y - moved.verticalOffset);
  }

  // What this place of the history says about where the drawing stood: believed only for a drawing of as many
  // cards, and only numbers a drawing can stand at.
  private stood(): { zoom: number; x: number; y: number } | null {
    const kept = (history.state as { drawings?: Record<string, { cards?: number; zoom?: number; x?: number; y?: number }> } | null)?.drawings?.[this.slot];
    return kept && kept.cards === this.graph.nodes.length && typeof kept.zoom === 'number' && typeof kept.x === 'number' && typeof kept.y === 'number'
      && Number.isFinite(kept.zoom) && Number.isFinite(kept.x) && Number.isFinite(kept.y) && kept.zoom > 0.01 && kept.zoom < 100
      ? { zoom: kept.zoom, x: kept.x, y: kept.y }
      : null;
  }

  // Where the drawing stands is written into the place of the history the visitor is at – not into the address,
  // which is passed on and says what is explored, not where somebody's eye was. Written a moment after the
  // movement ends, so that a finger that drags does not write a hundred times.
  private keepStanding(): void {
    if (!this.moved) {
      return;
    }

    window.clearTimeout(this.keeping);
    this.keeping = window.setTimeout(() => this.write(), 120);
  }

  private write(): void {
    window.clearTimeout(this.keeping);
    this.keeping = 0;
    const state = (history.state ?? {}) as { drawings?: Record<string, unknown> };
    // The movement is read where the control keeps it and where a movement adds to it. What it writes into
    // its settings has the other sign after some movements and not after others (seen in WebKit: a drawing
    // came back moved to the other side).
    const moved = this.diagram.scroller;
    const here = { cards: this.graph.nodes.length, zoom: moved.currentZoom, x: moved.horizontalOffset, y: moved.verticalOffset };
    history.replaceState({ ...state, drawings: { ...state.drawings, [this.slot]: here } }, '');
  }

  // The whole drawing again: nothing of where it stood is kept.
  private forget(): void {
    window.clearTimeout(this.keeping);
    this.keeping = 0;
    this.moved = false;
    const state = history.state as { drawings?: Record<string, unknown> } | null;
    if (state?.drawings && this.slot in state.drawings) {
      const { [this.slot]: _, ...others } = state.drawings;
      history.replaceState({ ...state, drawings: others }, '');
    }
  }

  fit(): void {
    this.asLeft = false;
    this.forget();
    // The control makes a drawing smaller to fit and never larger; so it is first shown at its own size.
    this.diagram.reset();
    this.diagram.fitToPage({ mode: 'Page', region: 'Content', canZoomIn: false, margin: { left: 24, right: 24, top: 24, bottom: 24 } });
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
    // The tools that are not used all the time stand behind "More".
    const more = host.querySelector<HTMLElement>('[data-graph-more]');
    const rest = host.querySelector<HTMLElement>('[data-graph-more-tools]');
    if (more && rest) {
      more.addEventListener('click', () => {
        rest.hidden = !rest.hidden;
        more.setAttribute('aria-expanded', String(!rest.hidden));
      });
    }

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
