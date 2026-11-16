# 03 — Domain Model and Ontology

**Version:** 0.6  
**Date:** 2026-11-15  
**Status:** Conceptual baseline for design; detailed rules remain proposals unless identified as accepted requirements  
**Scope:** Entities, relationships, roles, statements, sources, identity, media, and contextual views; no database or framework selection

## DOM-001 — Purpose and decision status

**Accepted direction:** The primary launch audience is the general curious public. People should quickly understand an interesting subject, explore connected entities and stories, and move among useful visual views. Researchers remain a possible secondary audience. Most knowledge content will be public; optional personal, family, and social uses remain part of the same product.

The domain includes people, countries, companies, football teams, works, places, and other meaningful subjects. It also includes photos and videos reached through people, family, recent authorized shares, historical periods, and other entity contexts. The common core must serve these different purposes without making every subject a person or every experience a research form.

**Proposal:** Use a shared conceptual core with typed capabilities and distinct public and personal views. Public knowledge does not automatically make a connected person's private information, friendship, invitation, education record, or media public. Model detail supports a concise first impression; it need not appear as a large form or terminology lesson in the interface.

## DOM-002 — Entities, types, collections, capabilities, and views

An entity has an identity that can be referenced independently of its current name or on-screen card. Its type determines which facts and relationships make sense. A collection selects material for a purpose. A view presents authorized information and must not create a contradictory copy of the underlying subject.

| Concept | Illustrative example | Proposed rule |
|---|---|---|
| Entity | A person, company, club, city, work, or event | Stable identity independent of display name |
| Entity type | Person, organization, place, creative work, natural subject | Defined meaning and room for specialization |
| Specialized type | Company, university, sports club | Shared capabilities plus appropriate domain rules |
| Capability | Membership, genealogy, media association, narrative participation | Applicable where meaningful; not compulsory for all types |
| Collection | Companies active in a period; selected family memories | Inclusion does not create a new real-world relationship |
| Comparison view | Three club histories shown together | Selects and arranges information without duplicating identities |
| Gallery | Photos and videos related to an entity or period | A selection of media associations, not a new media database |
| Guided story | A short route through the development of an industry | An editorial presentation linked to checkable statements |

Support for additional scientific, natural, cultural, and technical subjects is an extensibility proposal. A type system should make common tasks simple while preserving domain-specific meanings. It does not prescribe a class hierarchy, database schema, or separate service for each type.

## DOM-003 — Identity and names

An entity may have a preferred name, alternatives, historical names, names in several languages, titles, abbreviations, and external identifiers. A historical name may have a validity period and a source. Search should find these names; similarity does not establish identity.

**Proposed rule:** Merging records requires review of identity, evidence, relationships, and disclosure consequences. A mistaken merge must be correctable. “Possibly the same person,” “commonly confused with,” and “portrayal of” are different relationships from “the same entity.” External identifiers retain their issuing reference and match status; importing an identifier does not endorse all statements in that reference.

Renaming an entity must not break an existing entity link. Merging or separating identities should preserve an intelligible correction trail without exposing previously restricted information.

## DOM-004 — A person, an account, and a narrative character

**Accepted requirement:** A person may exist in a family tree or historical content without an account. Invitation, account-to-record association, and acceptance of a social relationship are separate actions.

**Proposed distinction:** An account enables authentication and participation. A person record describes a subject. Account association, editing authority, and publication permission are different grants. An editor can contribute information about many people without becoming those people. An authorized company representative does not obtain exclusive control over the company's documented history.

A character in one work, a version in an adaptation, an actor portraying that character, and a potentially related historical person must not merge automatically. The work, edition, narrative context, and asserted relationship remain explicit. Several accounts of a subject can coexist without inventing a single identity or chronology to reconcile them.

## DOM-005 — Statements as checkable units of knowledge

**Proposal:** Important information should be expressible as an identifiable statement: what is being asserted, about which subject, with which value or related entity, in which context and time, and on what evidence? An illustrative statement is “Person A managed Company B during a specified period.”

| Statement component | Product purpose |
|---|---|
| Subject and property or relationship | Identifies the exact assertion |
| Value or related entity | Text, a quantity with unit and precision, a time expression, a place, or another entity |
| Time and context | Qualifies applicability by period, season, edition, narrative, or another relevant context; may be unknown or inapplicable |
| Source and evidence locator | Lets a viewer move from a visual connection to its support |
| Review status | Proposed, reviewed, disputed, or withdrawn; publication status remains separately meaningful |
| Creation and change provenance | Records who or what process entered or changed the statement |

Wikibase's conceptual model also describes statements, qualifiers, and references. It is a useful design reference, not an implementation decision for this product. [Official Wikibase data model](https://www.mediawiki.org/wiki/Wikibase/DataModel).

The everyday interface can show a short fact or labeled connection first. Its source and contextual detail should be available progressively, without erasing relevant uncertainty from the first view.

## DOM-006 — Sources, disagreement, and provenance

A public statement should support confirming evidence, opposing evidence, editorial explanation, and a specific locator such as a page or section. Selecting a default presentation must not silently remove competing statements. “Confirmed by a user,” “has a source,” and “editorially reviewed” are different statuses; none is a guarantee of truth.

Extracted, translated, and imported information should remain traceable to its origin. W3C PROV describes provenance in terms of entities, activities, and people involved in producing data. This is a useful conceptual reference for the history of knowledge production. [Official PROV overview](https://www.w3.org/TR/prov-overview/).

Missing information is different from an explicit assertion that something does not exist. An unknown relationship end does not prove either continuation or absence. Popularity, likes, official account status, and display rank must not automatically become evidence quality. A concise public view should preserve an understandable “approximate,” “disputed,” or similar qualification when it changes the meaning.

## DOM-007 — Relationships and roles through time

Each relationship type needs a clear direction, permitted participants, inverse meaning, repeatability, and temporal behavior. “Parent of” is directional. An accepted close-friend connection is a reciprocal social agreement. Membership of a particular team in a particular season is a contextual role.

A person may hold several roles simultaneously or return to an organization in separate periods. Separate tenures must not become one continuous bar. Relationship type, audience circle, and view filter are different concepts. University membership, club support, company ownership, and employment have different meanings.

**Accepted social requirement:** Display of an account-based connection such as close friendship requires a request, notification, and acceptance by the other account. Acceptance does not authorize public disclosure of all related information. Historical claims and documented public roles instead follow sourcing and publication governance. Neither social consent nor a source citation substitutes for the other workflow where that workflow applies.

## DOM-008 — Events, periods, and participation

An event can connect several participants in different roles, several places, and several sourced accounts. A subject mentioned in a scene, a setting, an object, and an actor are different kinds of association. A collection of associated entities is not a list of people who were physically together. An editorial story beat may condense several scenes; each association must say what it asserts. A match, company founding, or family trip should not become unrelated duplicate events on each participant's timeline. Each view references the shared event and presents only the information authorized for its viewer.

A period of education, government, or membership is distinct from its start and end events. A person's lifespan, tenure in office, and an organization's existence are separate intervals. A post describing an event is not that event. Media creation, what the media depicts, publication, sharing, and import can have different times; [document 04](04-time-and-uncertainty.md) defines these distinctions.

In the bounded story sample, `entityAssociations` records an entity, its role and its presence meaning. A character lane can distinguish action, mention and a report about that character; it must not infer attendance, shared location, continuous activity or friendship. Relationship claims can refer to the selected beat that supports their scoped meaning without converting the beat ordinal into a date.

An event can provide an understandable next step in an everyday exploration: a person leads to a shared event, then to another participant or a relevant gallery. Such navigation explains the recorded connection and does not imply that co-participation proves friendship or causation.

## DOM-009 — Companies, brands, and organizational change

The following are **proposed product distinctions**. Classifying a real case requires evidence about that case.

| Change | Meaning to record |
|---|---|
| Rename | A new name with a validity context; identity continuity requires support |
| Acquisition | A change in ownership or control; the acquired company need not cease to exist |
| Merger | An event identifying predecessor organizations and the resulting or succeeding organization |
| Purchase of assets or a brand | Transfer of a specified subject, not necessarily the entire company's identity |
| Spin-off or split | An origin relationship and resulting organizations, with time and sources |
| Dissolution | A documented end in context; continuing brands or activities are considered separately |

A brand, legal company, corporate group, and product should be distinguishable. An origin-and-successor graph is different from an ownership view at a selected date. A shareholder change must not transform a company's whole history into the shareholder's history. The simple public presentation can summarize these changes while retaining their distinct meanings underneath.

## DOM-010 — Clubs, teams, seasons, and honors

For sport, distinguish a club or organization from a specific team, section or age category, season participation, and squad. A senior, youth, or women's team of the same club need not be the same competitive entity. A season is meaningful in its competition or organizer context.

Player membership, transfer, loan, match appearance, captaincy, and coaching are separate relationships. Inclusion in a season's squad does not prove appearance in every match. An honor should identify the competition, edition, and team; it must not automatically be assigned to every club member in every period. This permits both an engaging season overview and more detailed inspection of who did what and when.

## DOM-011 — Places, governments, and contextual identity

A physical place, territory, government, country, dynasty, and human community must not become one entity merely because they share a name. Names, boundaries, and affiliations can depend on time and source. A university may also have several campuses or branches; the place of attendance and the parent organization are different information.

For university discovery, attending the same institution, overlapping attendance, and belonging to the same entering cohort are separate results. Discovery of personal accounts and profiles is opt-in. Exploration of documented public roles follows public-content governance. Search results create no social connection and expose no information outside the applicable permission.

## DOM-012 — Extending the vocabulary without losing meaning

**Proposal:** Each entity or relationship type has a short definition, examples, translated labels, participant constraints, and temporal rules. A personal label does not automatically create a new official public relationship type. Synonyms can point to an existing definition; real differences in meaning must remain distinct.

Validation should distinguish structural errors, suspicious information, and legitimate disagreement between sources. A questionable claim needs an explanation or review state; it should not be silently altered merely to make a chart tidy. Fictional material is evaluated in its own context, rather than against a real human lifespan rule.

Typed capabilities may provide specialized forms and default views for family, company, sport, narrative, and media use. They should reuse the core identity, statement, time, provenance, and access rules instead of creating incompatible versions of those concepts.

## DOM-013 — Conceptual exploration and query contracts

These are expectations of product responses, not technical API signatures or storage choices.

| User action or question | What the response must make clear |
|---|---|
| Open a subject for the first time | A concise essential overview, meaningful visual choices, and a few explainable next connections |
| Follow a connection or story thread | The relationship or editorial reason, relevant time and context, and a route back |
| Who was on this team during this period? | Team and season, role, time, source, and uncertainty |
| How did this company develop? | Renames, acquisitions, mergers, and splits with distinct meanings |
| How are these characters related? | Relationship, work or narrative context, and supporting account |
| Who attended this university at the same time? | Discovery authorization, institution identity, and overlap certainty |
| Why is this image or video in this gallery? | The media's contextual association and whether it depicts, documents, or merely illustrates the selected subject |
| Switch graph, timeline, or gallery | Retained subject, applicable time, world or version, filters, and navigation history |

Responses distinguish explicit information, inference, and suggestions for further exploration. Permissions apply to results, counts, and connection paths. Hiding a card after sending its private data is not sufficient. A saved view selects information; it is not an independent copy of all the selected facts.

## DOM-014 — Open decisions for later design

- Initial official entity and relationship types, and the process for extending them.
- Boundaries between a shared public record, personal notes, and restricted records about the same subject.
- Identity continuity rules for companies, teams, and narrative characters when sources disagree.
- Authority and correction workflows for merge and split decisions.
- Minimum evidence for different statement types and the concise presentation of disagreement.
- Organization representation without giving a representative exclusive control over documented public history.
- The initial meaningful visual presets and capability modules for each launch subject type.
- Media association types, identity and rendition rules, and gallery defaults.

Use family, history, company, football, and media examples to examine these decisions. Technology selection follows the intended behavior. The selected product name is **Dastan**; the [brand brief](11-brand-and-positioning.md) records the naming decision and remaining identity work.

## DOM-015 — Media as first-class contextual content

**Accepted direction:** Photos and videos are first-class content, accessible through public subjects and periods as well as personal and family contexts. A media item is not merely a decorative thumbnail attached to a person.

**Proposed distinctions:** Preserve the media identity, the underlying work or capture when relevant, its stored file or external reference, renditions such as thumbnails or video posters, and contextual associations. A crop, restoration, excerpt, or edited video may require a derivative relationship and its own provenance. Neither the same filename nor similar appearance proves that two items have the same identity.

An association should explain whether media depicts an entity, records an event, was created by someone, was shared by an account, illustrates a period, or is an editorially related item. These meanings must not collapse into one ambiguous tag. A modern illustration of an ancient period is not an ancient photograph. A newly shared old family photo can belong to an old memory and a recent-shares view for different reasons.

Personal tagging requires the established consent workflow before linking the item into another person's personal profile or timeline. Public descriptions of historical subjects follow publication and evidence rules. File access, association disclosure, caption visibility, attribution, and reuse rights remain separately meaningful. An authorized association does not widen access to the media itself.

## DOM-016 — Galleries and reusable public and personal views

A curated album selects items and may define an order or description. A dynamic gallery evaluates a contextual query, such as an entity, period, event, story, place, or authorized recent sharing activity. A saved gallery preserves its selection rules or explicit membership, display choices, and context; it does not duplicate every asset or its facts.

The same permitted media item may appear in several galleries. Its appearance in a public period view must not expose a private family annotation, unpublished association, or personal account link. Combining galleries does not combine their audiences. A saved view, public share link, or thumbnail must reapply the applicable publication and access policy.

The shared core supplies identity, associations, statements, time, sources, media, and permissions. Typed capabilities determine which actions and views make sense. Public and personal views select different authorized presentations from that core. This separation supports reuse without turning optional family sharing into a prerequisite for public exploration.

## DOM-017 — Bounded implementation adapter

**Proposed implementation boundary:** The companion JSON files are editorial and review inputs. They are not the production entity schema or an authorization API. The first executable slice should map the selected record types into a small typed read model, retain source IDs and context, and expose only the records eligible for that response. It does not need a user-editable ontology engine, a graph database, or every future domain module to demonstrate the common core.

The Shahnameh input inherits world and edition context for its narrative claims; artwork creation metadata retains its separate real-world meaning. The synthetic company/team/family inputs remain clearly marked review fixtures. Publication approval, local demonstration eligibility, reader progress and future private access are independent decisions. Successful fixture validation must never be described as production authorization or public editorial approval.
