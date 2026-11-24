// The script of the pages of Dastan (ADR 0019). A page is complete without it: the server answers with the
// whole page, and this layer takes over where a page says so – it moves, opens and shows in place what a link
// would open as another page. Nothing is fetched from another site, and nothing is sent that the page of the
// same visitor would not show.
import { narrowNames } from './compare';
import { closeSettingsWhenLeft } from './frame';
import { keepOrForget, sayWhatIsKept } from './offline';
import { offerProfiles } from './profile-panel';
import { bringNamesIntoSight } from './stage';

// It waits until the page has arrived with its styles and fonts: before that a box is not where it will be.
window.addEventListener('load', () => {
  void keepOrForget();
  void sayWhatIsKept();
  bringNamesIntoSight();
  drawGraphs();
});

// A page with a graph (ADR 0020) or a timeline (ADR 0021) names the script that draws it; it is asked for once
// the page is shown, and by no other page.
function drawGraphs(): void {
  const asked = new Set<string>();
  for (const drawing of document.querySelectorAll<HTMLElement>('dastan-graph[data-script], dastan-timeline[data-script]')) {
    const address = drawing.dataset['script'];
    if (address && !asked.has(address)) {
      asked.add(address);
      const script = document.createElement('script');
      script.src = new URL(address, document.baseURI).href;
      script.async = true;
      document.head.append(script);
    }
  }
}

// What a page offers beyond itself is offered as soon as the page can be read.
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', offerProfiles);
  document.addEventListener('DOMContentLoaded', closeSettingsWhenLeft);
  document.addEventListener('DOMContentLoaded', narrowNames);
} else {
  offerProfiles();
  closeSettingsWhenLeft();
  narrowNames();
}
