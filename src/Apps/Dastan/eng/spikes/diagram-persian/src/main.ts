// Step 19.2 of Dastan's delivery plan: what Syncfusion EJ2 Diagram does with Persian. One page, one case per
// address (?case=…), so that each can be looked at and photographed by itself. Not part of the product.
//
// Switches of the address, all for narrowing a finding down (see README.md):
//   case=family|relations   which graph is shown
//   plain=1                 no card template – the node as the control draws it by itself
//   nolabels=1              connectors without labels
//   wrap=char               the control's 'Wrap' (breaks inside words) in the place of 'WrapWithOverflow'
//   same=1                  (relations) the two opposite connectors as the control routes them by itself
//   curve=1                 (relations) the two told apart by bent Bezier curves instead of ports
//   layout=tree             (family) HierarchicalTree in the place of ComplexHierarchicalTree
//   fix=none|ltr|css        without the correction of the text direction, or with another one (see index.html)
import {
    ComplexHierarchicalTree, ConnectorModel, Container, DataBinding, Diagram, DiagramTools, HierarchicalTree,
    ImageElement, NodeConstraints, NodeModel, PortVisibility, PrintAndExport, Size, SnapConstraints, StackPanel, TextElement,
} from '@syncfusion/ej2-diagrams';

Diagram.Inject(DataBinding, ComplexHierarchicalTree, HierarchicalTree, PrintAndExport);

interface Person { id: string; name: string; facts: string; kind: 'person' | 'place' | 'team'; }

const font = 'Vazirmatn, sans-serif';
const query = new URLSearchParams(location.search);

// A drawing in the place of a profile picture: a circle with the first letter, as a picture file would be.
function picture(letter: string, colour: string): string {
    const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="96" height="96"><circle cx="48" cy="48" r="46" fill="${colour}"/>` +
        `<text x="48" y="62" font-size="44" text-anchor="middle" fill="#fff" font-family="sans-serif">${letter}</text></svg>`;
    return 'data:image/svg+xml;base64,' + btoa(unescape(encodeURIComponent(svg)));
}

// The texts are chosen for what can go wrong: a long name that must wrap, Persian with Latin in parentheses,
// Persian digits, a full stop, "!", "؟" and "–" at the end or in the middle of a line, a Latin line with digits.
const people: Person[] = [
    { id: 'sam', name: 'سام نریمان، پهلوان نامدار سیستان و نیای بزرگ رستم دستان', facts: 'پهلوان سیستان · زادهٔ حدود ۱۲۰۰ پ.م.', kind: 'person' },
    { id: 'zal', name: 'زال (Zāl)', facts: 'زادهٔ ۱۱۵۰ پ.م. – درگذشتهٔ ؟', kind: 'person' },
    { id: 'rudabeh', name: 'رودابه، دختر مهراب کابلی', facts: 'کابل · Kabul', kind: 'person' },
    { id: 'rostam', name: 'رستم دستان، پسر زال و رودابه (تهمتن)', facts: 'زادهٔ ۱۱۰۰ پ.م. · ۶۰۰ سال زیست!', kind: 'person' },
    { id: 'tahmineh', name: 'تهمینه', facts: 'سمنگان (Samangan)', kind: 'person' },
    { id: 'shaghad', name: 'شغاد', facts: 'نابرادری رستم', kind: 'person' },
    { id: 'sohrab', name: 'سهراب', facts: 'زادهٔ ۱۰۷۰ پ.م. – ۱۰۵۵ پ.م.', kind: 'person' },
    { id: 'faramarz', name: 'فرامرز', facts: 'FC Sistan 1925 ✓', kind: 'team' },
];
const parents: [string, string][] = [
    ['sam', 'zal'], ['zal', 'rostam'], ['rudabeh', 'rostam'], ['zal', 'shaghad'],
    ['rostam', 'sohrab'], ['tahmineh', 'sohrab'], ['rostam', 'faramarz'],
];

// How the direction of the texts is put right; see index.html for the four ways and what each one shows.
const fix = query.get('fix') ?? 'marks';

// Workaround, the second half of the one in index.html. The control draws every line of a text as its own
// <tspan> in a text whose base direction is left to right, and has no setting for the direction. So each line
// that holds Persian is wrapped in the Unicode marks "right-to-left embedding" … "pop" (U+202B … U+202C): they
// take no room, and give that one line the Persian base direction – in the diagram and in what it exports.
// A line without any right-to-left letter ("FC Sistan 1925 ✓") is left as it is.
// Found here, in WebKit only: two lines that both end and begin with right-to-left text are taken as ONE run
// across the line break, and their words are drawn in each other's line. A "left-to-right mark" (U+200E) after
// each line parts the runs; it has no width either.
const persian = (line: string): string => fix === 'marks' && /[\u0590-\u08ff\ufb1d-\ufefc]/.test(line) ? '\u202b' + line + '\u202c\u200e' : line;

// The measures of a card. The width is the same for all, so that the layout has equal columns; the height
// follows the text.
const cardWidth = 230, pictureSize = 48, gap = 8, inset = 8;
const wordsWidth = cardWidth - 2 * inset - pictureSize - gap;

const text = (content: string, size: number, bold: boolean, width: number): TextElement => {
    const element = new TextElement();
    element.content = content;
    // The width is what a line is wrapped at. A text shorter than it is measured as narrow as its words, so the
    // element is also told to stand at the right of its column; the default ('Auto') centres it.
    element.width = width;
    element.horizontalAlignment = 'Right';
    // Found here: textWrapping 'Wrap' breaks a line at ANY character ("رستم دستان، پسر زال و ر" / "ودابه"), which
    // tears a Persian word apart and breaks the joining of its letters (see it with &wrap=char).
    // 'WrapWithOverflow' breaks at spaces only; a single word wider than the column would stick out of the card,
    // so the product must keep the column wider than the longest word it shows.
    const textWrapping = query.get('wrap') === 'char' ? 'Wrap' : 'WrapWithOverflow';
    // The style an element comes with is changed, never replaced.
    Object.assign(element.style, { color: '#1c1b19', fontSize: size, fontFamily: font, bold, textWrapping, textAlign: 'Right', strokeColor: 'none', fill: 'none' });
    // The marks must stand around each LINE, and the lines are made by the control. So the control wraps the
    // text once, and the lines it found are given back to it with their marks, parted by line feeds – which it
    // keeps as line breaks. The marks have no width, so the second wrapping finds the same lines.
    if (fix === 'marks') {
        element.measure(new Size(width, Number.MAX_VALUE));
        const lines = (element as unknown as { childNodes: { text: string }[] }).childNodes;
        element.content = lines.map(line => persian(line.text.trim())).join('\n');
    }
    return element;
};

// A card of ordinary elements – a picture and two texts – so that it is drawn, laid out and exported by the
// control itself, with nothing of HTML in it.
function card(node: NodeModel): Container {
    const person = people.find(candidate => candidate.id === node.id)!;
    const row = new StackPanel();
    row.orientation = 'Horizontal';
    Object.assign(row.style, { fill: '#fffdf8', strokeColor: '#b8a98a', strokeWidth: 1 });
    row.cornerRadius = 8;
    row.padding = { left: inset, right: inset, top: inset, bottom: inset };
    row.id = node.id + '-card';

    const words = new StackPanel();
    words.orientation = 'Vertical';
    Object.assign(words.style, { fill: 'none', strokeColor: 'none' });
    words.id = node.id + '-words';
    // Found here: a `width` alone is not honoured by a container (the control takes it only together with a
    // `height`); `minWidth` is. With it every card is as wide as the node, whatever the length of its words.
    words.minWidth = wordsWidth;
    const name = text(person.name, 15, true, wordsWidth); name.id = node.id + '-name';
    const facts = text(person.facts, 12, false, wordsWidth); facts.id = node.id + '-facts';
    words.children = [name, facts];

    const image = new ImageElement();
    image.id = node.id + '-picture';
    image.source = picture(person.name[0], person.kind === 'team' ? '#2f6f5e' : '#8a4b2a');
    image.width = pictureSize; image.height = pictureSize;
    // Found here: a margin (or padding) must name all four sides. One that names only `left` leaves the others
    // undefined, every size computed from it becomes NaN, and the control fails far away: cards are drawn tiny
    // at the top-left corner, a connector label throws "Cannot read properties of undefined (reading 'y')" in
    // getAnnotationPosition, and ComplexHierarchicalTree ends in "Maximum call stack size exceeded".
    image.margin = { left: gap, right: 0, top: 0, bottom: 0 };
    Object.assign(image.style, { fill: 'none', strokeColor: 'none' });

    // Right to left: the picture stands at the right, the words to its left. A horizontal stack has no
    // right-to-left order of its own; the children are simply given in the order left to right.
    row.children = [words, image];

    // Seen here: the node takes the size of its card – a card with a long name is taller than the height the
    // model names (72), and the layout keeps the rows apart accordingly. Nothing has to be measured by hand.
    return row;
}

const template = query.has('plain') ? undefined : card;
const node = (id: string, offsetX?: number, offsetY?: number): NodeModel => ({ id, width: cardWidth, height: 72, offsetX, offsetY, constraints: NodeConstraints.Default & ~NodeConstraints.Rotate });
const link = (id: string, from: string, to: string, label?: string, more: ConnectorModel = {}): ConnectorModel => ({
    id, sourceID: from, targetID: to, type: 'Orthogonal', cornerRadius: 6,
    style: { strokeColor: '#6b5d45', strokeWidth: 1.5 },
    targetDecorator: { shape: 'Arrow', style: { fill: '#6b5d45', strokeColor: '#6b5d45' } },
    annotations: label && !query.has('nolabels') ? [{ content: persian(label), style: { fontFamily: font, fontSize: 12, color: '#1c1b19', fill: '#fffdf8' } }] : [],
    ...more,
});

const which = query.get('case') ?? 'family';
let diagram: Diagram;

if (which === 'relations') {
    // Two people with several relationships at once, in both directions, each with its own label.
    // Found here: two connectors between the same two nodes, whatever their directions, are drawn on the very
    // same line – one line with an arrow at each end and the two labels on top of each other (&same=1 shows it).
    // The control does not spread them. Two ways to tell them apart were tried:
    // – &curve=1: Bezier curves bent to opposite sides by the two vectors of their segment. They stand right
    //   until a node is moved (a drag, or an arrow key on a selected node): then the control routes both anew
    //   and they fall back onto one straight line.
    // – (kept) each connector ends at a port of its own, at a different height of the card. Ports are points of
    //   the node, so the two lines stay apart wherever the nodes go.
    const side = (name: string, x: number, y: number) => ({ id: name, offset: { x, y }, visibility: PortVisibility.Hidden });
    const ports = [side('right-upper', 1, 0.3), side('right-lower', 1, 0.7), side('left-upper', 0, 0.3), side('left-lower', 0, 0.7)];
    const person = (id: string, x: number, y: number): NodeModel => ({ ...node(id, x, y), ports });
    const bent = (up: boolean, leftToRight: boolean): ConnectorModel => ({
        type: 'Bezier',
        segments: [{
            type: 'Bezier',
            vector1: { angle: leftToRight ? (up ? 320 : 40) : (up ? 220 : 140), distance: 90 },
            vector2: { angle: leftToRight ? (up ? 220 : 140) : (up ? 320 : 40), distance: 90 },
        }],
    });
    const apart = (up: boolean, leftToRight: boolean): ConnectorModel =>
        query.has('same') ? { type: 'Straight' }
            : query.has('curve') ? bent(up, leftToRight)
                : { type: 'Straight', sourcePortID: (leftToRight ? 'right-' : 'left-') + (up ? 'upper' : 'lower'), targetPortID: (leftToRight ? 'left-' : 'right-') + (up ? 'upper' : 'lower') };
    diagram = new Diagram({
        width: '100%', height: '520px',
        nodes: [person('rostam', 180, 140), person('sohrab', 620, 140), person('tahmineh', 400, 420)],
        connectors: [
            link('r1', 'rostam', 'sohrab', 'پدرِ', apart(true, true)),
            link('r2', 'sohrab', 'rostam', 'هماوردِ (۱۰۵۵ پ.م.)', apart(false, false)),
            link('r3', 'rostam', 'tahmineh', 'همسرِ', { type: 'Straight' }),
            link('r4', 'tahmineh', 'sohrab', 'مادرِ – mother of', { type: 'Straight' }),
        ],
        setNodeTemplate: template,
        tool: DiagramTools.ZoomPan | DiagramTools.SingleSelect,
        snapSettings: { constraints: SnapConstraints.None },
    });
} else {
    // A family with a child of two parents, a parent with children of two partners, and a half-brother.
    diagram = new Diagram({
        width: '100%', height: '640px',
        nodes: people.map(person => node(person.id)),
        connectors: parents.map(([from, to], index) => link('p' + index, from, to, index === 0 ? 'پدر' : index === 5 ? 'مادر (mother)' : undefined)),
        layout: { type: query.get('layout') === 'tree' ? 'HierarchicalTree' : 'ComplexHierarchicalTree', orientation: 'TopToBottom', horizontalSpacing: 36, verticalSpacing: 56 },
        setNodeTemplate: template,
        tool: DiagramTools.ZoomPan | DiagramTools.SingleSelect,
        snapSettings: { constraints: SnapConstraints.None },
    });
}

diagram.appendTo('#diagram');

// What the look asks the page: the control itself, and its export as the control makes it.
const page = window as unknown as Record<string, unknown>;
page.dastanDiagram = diagram;
page.dastanExport = (format: 'SVG' | 'PNG'): string => {
    const made = diagram.exportDiagram({ format, mode: 'Data', region: 'Content', margin: { left: 10, right: 10, top: 10, bottom: 10 } });
    return typeof made === 'string' ? made : new XMLSerializer().serializeToString(made);
};

// What the product would have to do to the control's export, tried here to see that it is enough. The exported
// SVG stands by itself and does not carry the page's font, so it is drawn in a fallback font; the control's PNG
// is that same SVG drawn at the size of the screen (1x), in the same fallback font. (The direction needs no
// repair: the marks around each line travel inside the text.)
// The repair: the font is put into the SVG file, and the PNG is drawn from that SVG at twice the size.
page.dastanExportRepaired = async (format: 'SVG' | 'PNG'): Promise<string> => {
    const made = diagram.exportDiagram({ format: 'SVG', mode: 'Data', region: 'Content', margin: { left: 10, right: 10, top: 10, bottom: 10 } });
    const svg = (typeof made === 'string' ? new DOMParser().parseFromString(made, 'image/svg+xml').documentElement : made) as unknown as SVGSVGElement;
    const bytes = new Uint8Array(await (await fetch('vazirmatn-nl-wght.woff2')).arrayBuffer());
    let binary = '';
    for (const byte of bytes) binary += String.fromCharCode(byte);
    const style = document.createElementNS('http://www.w3.org/2000/svg', 'style');
    style.textContent = `@font-face { font-family: Vazirmatn; font-weight: 100 900; src: url(data:font/woff2;base64,${btoa(binary)}) format("woff2"); }`;
    svg.insertBefore(style, svg.firstChild);
    const text = new XMLSerializer().serializeToString(svg);
    if (format === 'SVG') return text;

    const image = new Image();
    image.src = 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(text);
    await image.decode();
    const canvas = document.createElement('canvas');
    canvas.width = image.width * 2; canvas.height = image.height * 2;
    const context = canvas.getContext('2d')!;
    context.fillStyle = '#fff';
    context.fillRect(0, 0, canvas.width, canvas.height);
    context.drawImage(image, 0, 0, canvas.width, canvas.height);
    return canvas.toDataURL('image/png');
};
page.dastanReady = true;
