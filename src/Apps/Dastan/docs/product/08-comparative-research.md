# 08 — Comparative Research and Product Implications

**Version:** 0.6  
**Original source review:** 2026-11-14  
**Targeted recheck:** 2026-11-15; Histropedia availability, TimelineNG announcement, Aeon date guide and the Shahnameh public home page  
**Status:** Evidence for product decisions; not an exhaustive market or usability audit

## 1. Main conclusion

The primary audience is curious people who want to explore. They may arrive through a photograph, a person, a club, a company, a fictional character, or an intriguing event, then follow a connection to another story. Researchers and educators remain valuable secondary audiences. Evidence and detail support that experience without dominating the first screen.

The competitive field therefore includes visual discovery, structured knowledge, narrative tools, network visualization, and family services. Combining time, connections, and media has precedents. The product must earn differentiation through a coherent, inviting experience, not a claim to be the first or only platform of its kind.

Our working hypothesis is a shared core that offers a short visual first look, useful next connections, several coordinated views, and optional depth. Public historical galleries and authorized family memories use that core under different publication rules.

## 2. Evidence and scope

- **Officially documented:** described by the provider or project; not necessarily exercised in this review.
- **Directly observed earlier:** a specific behavior recorded during the earlier Shahnameh walkthrough in this conversation. It is not a new browser audit conducted on 2026-11-14.
- **Product inference:** our interpretation or proposal, not a provider claim.

The v0.6 recheck confirmed the time-sensitive Histropedia availability statement and the cited TimelineNG announcement, re-read the Aeon date guide, and read the Shahnameh public home page. Other provider descriptions retain their original review date. No subscription purchase, authenticated workflow test, performance benchmark, comprehensive mobile test, or security audit was performed. A feature not found in the reviewed pages must not be described as absent. A page's review date is not its feature launch date.

## 3. Comparison map

| Reference | Why it matters | Decision it informs |
|---|---|---|
| Wikidata | Public knowledge with statements and provenance | Reliable information beneath accessible discovery |
| Histropedia | Connected, layered historical timelines | Direct overlap and public exploration |
| Aeon Timeline | Flexible dates and connected timelines | Coordinated views and honest time representation |
| World Anvil | Fictional worlds, characters, maps, and chronology | World and interpretation boundaries |
| Kumu | Exploring connected networks | Readable neighborhoods and progressive expansion |
| TimelineJS / TimelineNG | Shareable multimedia storytelling | Guided stories alongside free exploration |
| MyHeritage, FamilySearch, WikiTree | Family views and shared stewardship | Optional family spaces and collaborative editing |
| Shahnameh example | A focused literary exploration experience | Story order, character navigation, and source context |

## 4. Wikidata: a knowledge model and possible data source

**Documented:** an item has statements built from a property and value, with optional qualifiers, references, and ranks. Items can represent many kinds of subjects and link to one another. This is useful for companies, teams, works, places, and people. [Statements](https://www.wikidata.org/wiki/Help:Statements)

Qualifiers add context such as a role, method, or period of validity. Our proposed examples include a player belonging to a team during a season, or a person holding a company role during a stated interval. A relationship label alone is insufficient. [Qualifiers](https://www.wikidata.org/wiki/Help:Qualifiers)

References identify support for a statement. Wikidata's guidance distinguishes an actual supporting reference from a note that information was imported from another Wikimedia project. **Inference:** import origin and claim evidence must remain separate. [Sources](https://www.wikidata.org/wiki/Help:Sources/en)

Normal, preferred, and deprecated ranks support statement selection; they are not numerical truth probabilities. Historically correct information does not become deprecated merely because its period has ended. **Inference:** former team membership and an erroneous membership claim need different treatment. [Ranking](https://www.wikidata.org/wiki/Help:Ranking)

**Reuse:** structured data in Wikidata's specified namespaces is released under CC0. This does not make every linked image or text CC0. Media needs its own source and reuse conditions. [Wikidata copyright](https://www.wikidata.org/wiki/Wikidata:Copyright), [Commons reuse guidance](https://commons.wikimedia.org/wiki/Commons:Reusing_content_outside_Wikimedia)

Specific entity revisions can be retrieved. Simplified output can omit qualifiers and references. **Proposal:** preserve external identifiers, revision information, retrieval time, and supporting information when importing. Data ingestion must not automatically publish every imported assertion. [Data access](https://www.wikidata.org/wiki/Wikidata:Data_access)

**Tradeoff for our product:** this model preserves context, but exposing its full editing complexity would overwhelm a casual visitor. Show a clear relationship and short explanation first; offer evidence, alternative claims, and history when requested. The underlying rigor and the first-look interface have different jobs.

## 5. Histropedia: direct overlap with public discovery

**Documented status, rechecked 2026-11-15:** the original Timeline of Everything has been retired, while its successor is not yet publicly available. The announced successor combines searchable timelines with personal life stories, family trees, and dated photo collections. Wikipedia and Wikidata remain part of its approach. These are announced capabilities, not our observations of a completed release. [Official FAQ](https://histropedia.com/faq/)

HistropediaJS is separately presented as a timeline library with institutional examples. Its published performance claims were not independently measured here. [HistropediaJS](https://js.histropedia.com/)

**Inference:** layered timelines alone are insufficient differentiation. Evaluate whether our first encounter, relevant next connections, Persian-language experience, and movement between a chart and its images are easier to understand. A general platform should invite a visitor to follow an interesting thread without requiring a research question.

**Tradeoff:** a visualization library and a complete public discovery service are different offerings. Engine adoption would require a separate capability, licensing, and cost evaluation.

## 6. Aeon Timeline: flexible time and coordinated comparisons

**Documented:** real or custom calendars, relative dates, different precision levels, uncertain start/end windows, and ongoing items are supported. Changing a custom calendar after dated content exists is constrained; the guide describes moving content to a new file. [Date management](https://www.aeontimeline.com/guides/add-and-customize-dates)

Grouping, split views with synchronized or independent movement, and a Subway view connecting tracks through shared events are documented. [Timeline comparison](https://www.aeontimeline.com/guides/compare-timelines)

**Inference:** an engaging comparison must say what is aligned. “What happened at the same historical time?” differs from “How did two organizations develop after their founding?” The same content may support both views, but the alignment must be visible.

The common core should preserve uncertain dates as structured information, while offering understandable labels. A visitor should not have to learn calendar configuration to compare two lives. The calendar constraint also illustrates why changing a time system requires a controlled migration rather than a cosmetic setting.

## 7. World Anvil: worlds and character relationships

**Documented:** character family trees and nonfamilial relationships such as friends, rivals, and teacher–student relationships are available. [Family trees and relationships](https://www.worldanvil.com/learn/family-trees/family-trees-guide)

Chronicles combines maps and timelines. Its multiple-timeline guide describes four event lanes within a timeline and two initially visible timelines, with navigation for additional ones. This describes that documented presentation, not a product-wide limit. [Multiple timelines](https://www.worldanvil.com/learn/chronicles/chronicles-multiple-timelines)

**Inference:** a fictional world, edition, and interpretation must provide context for statements. A character in an adaptation may have a different history from the character in the original work.

For casual discovery, default to a manageable selection of relevant tracks and connections. Let visitors expand the view as curiosity develops. A broad toolkit is valuable only if the entry experience remains approachable; that is a hypothesis to test for our interface, not a measured criticism of World Anvil.

## 8. Kumu: keeping networks readable

**Documented:** Focus limits a map to selected elements, then expands by connection degree. Search-based entry can construct a focused map. [Focus](https://docs.kumu.io/guides/focus)

Share links can provide access to a private project's map to anyone who has the link and any required password. [Sharing and embedding](https://docs.kumu.io/guides/share-and-embed)

**Inference:** begin a company view with a few meaningful relationships, then let the visitor reveal people, products, or earlier periods. A complete graph is not automatically a useful first view. Offer a concise reason for each suggested connection.

For family spaces, visual filtering must remain separate from authorization. Hiding a node in a chart cannot protect content already delivered to the viewer. The same distinction applies to search results, image galleries, shared views, and cached data.

## 9. TimelineJS / TimelineNG: guided multimedia stories

**Documented:** the 2026-08-13 TimelineNG announcement describes a new editor while retaining the slideshow model and compatibility with previous configurations. Media uploading through the announced path requires an Atmosphere account. [Official announcement](https://knightlab.northwestern.edu/2026/08/13/timelinejs-reimagined/)

TimelineJS's changelog documents cosmological timelines and BigDate formatting. Old answers about limited BCE ranges should not be applied to every current version. Our specific 50–60-million-year scenario was not tested. [Official changelog](https://github.com/NUKnightLab/TimelineJS3/blob/master/CHANGELOG.md)

**Inference:** add guided stories as a complement to free exploration. A curated sequence can help a visitor enter an unfamiliar subject, then open the same events in a chart, gallery, or relationship view.

Images and videos should connect to relevant subjects, places, and periods. A selected story must remain distinguishable from the complete record: omitting an event from a presentation does not mean that the event never happened.

## 10. Family and shared-stewardship references

| Reference | Documented capability or rule | Product implication |
|---|---|---|
| MyHeritage | Family Tree Timeline aligns a person and direct ancestors. Its BC guide treats such dates as free text, unavailable for age calculations. | A temporal display does not guarantee a computational model suitable for ancient dates. |
| FamilySearch | The guide updated on 2026-11-09 describes shared family group trees containing living people. | Optional family spaces need explicit membership and sharing context. |
| WikiTree | A profile manager is a steward or collaboration leader, not the owner of truth. | Public-page stewardship should support evidence and accountable collaboration. |

Official sources: [MyHeritage Timeline](https://www.myheritage.com/help/en/articles/12889322-what-is-the-family-tree-timeline-and-how-do-i-use-it), [MyHeritage BC dates](https://www.myheritage.com/help/en/articles/12851191-can-i-add-bc-dates-to-the-family-tree), [FamilySearch](https://articles.familysearch.org/hc/en-us/articles/41819974887963-About-family-group-trees), [WikiTree Profile Manager](https://www.wikitree.com/wiki/Help:Profile_Manager).

WikiTree's guidance was read through an indexed result of the official page; its editing workflow was not exercised. These references inform a complementary use case. They do not justify making genealogy the product's main identity.

## 11. Shahnameh: earlier direct observations

In the earlier walkthrough, the [lanes view](https://shahname.mosaic-hub.de/#/lanes) used story sections rather than calendar years. A character bar covered first-to-last appearance; it did not establish appearance in every intervening section or a meeting between overlapping characters.

The [Rostam profile](https://shahname.mosaic-hub.de/#/c/rostam) and [family view](https://shahname.mosaic-hub.de/#/tree/p/rostam) connected character, family, events, and text. The profile could reveal a character's fate. The [sources page](https://shahname.mosaic-hub.de/#/page/sources) distinguished the Ganjoor text based on the Moscow edition from the Ferdowsikhani narrative based on Khaleghi-Motlagh, described possible automated-extraction errors, and labeled thematic parallels as interpretive.

**Inference:** chronological time, narrative order, and appearance spans require distinct labels. Character discovery is compelling when a visitor can move naturally to another character or event. Optional spoiler controls deserve testing. Edition, extraction provenance, and interpretation should remain accessible without crowding the first look.

The 2026-11-15 [public home page](https://shahname.mosaic-hub.de/) also offers Persian/English controls, links to a readable non-JavaScript version, and visible notices that thematic parallels are interpretive and automated speaker/transcript extraction may contain errors. This is a current public-page observation, not a fresh execution of the interactive lanes or family workflow.

The comparison site's Moscow attribution is its own source description. It does not independently establish the critical edition of Dastan's Ganjoor sample. [Document 17](17-flagship-content-and-source-plan.md) owns the sample's observed text-witness attribution and its unresolved edition status. The earlier observations establish nothing about the site's complete accuracy, access controls, performance, or mobile behavior.

## 12. Competitive implications and validation

The following are proposals, not claims that competitors lack them:

1. **Discovery first.** A visitor should understand the initial chart and find a meaningful next connection without reading a long article.
2. **Several views, one context.** Timeline, relationship graph, gallery, and short story views should preserve the selected subject, period, and access scope where meaningful.
3. **Media with context.** Public historical-period galleries and recent authorized family shares should reuse media handling while retaining their publication boundaries.
4. **Optional depth.** Sources, competing claims, uncertain dates, and revisions are available when needed; concise introductions remain the default.
5. **An initial collection worth exploring.** Select a small, connected public collection with interesting transitions between subjects. Dastan now uses the Rostam and Sohrab sample provisionally for the first implementation; the final public catalogue remains a release decision.
6. **Evidence through use.** Test whether ordinary visitors understand a relationship, distinguish a date from narrative order, follow a useful connection, and recover their previous view.

Differentiation remains a testable proposition. The brand should convey curiosity and connected stories rather than requiring visitors to identify as researchers. Document 11 translates that direction into a naming and visual-identity brief.
