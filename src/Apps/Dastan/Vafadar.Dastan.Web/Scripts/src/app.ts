// The script of the pages of Dastan (ADR 0019). A page is complete without it: the server answers with the
// whole page, and this layer takes over where a page says so – it moves, opens and shows in place what a link
// would open as another page. Nothing is fetched from another site, and nothing is sent that the page of the
// same visitor would not show.
import { keepOrForget, sayWhatIsKept } from './offline';
import { offerProfiles } from './profile-panel';
import { bringNamesIntoSight } from './stage';

// It waits until the page has arrived with its styles and fonts: before that a box is not where it will be.
window.addEventListener('load', () => {
  void keepOrForget();
  void sayWhatIsKept();
  bringNamesIntoSight();
});

// What a page offers beyond itself is offered as soon as the page can be read.
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', offerProfiles);
} else {
  offerProfiles();
}
