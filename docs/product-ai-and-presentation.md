# RTS product, AI, and adaptive-presentation design

## Product objective

RTS provides a comprehensive, evidence-backed evaluation of trading candidates. It is a platform for multiple configurable trading and evaluation models rather than an application built around one fixed methodology.

The first methodology is an initial system template. Future models may use different universes, indicators, timeframes, scoring rules, weights, and evaluation methods without changing the identity or shared foundation of RTS.

## End-to-end workflow

```text
Manual candidates ─────────┐
Deterministic screening ───┼── Candidate Inbox
AI catalyst discovery ─────┘          |
                                      | User selects candidates
                                      v
                              Evaluation job queue
                                      |
                                      v
                         Price data and chart generation
                                      |
                                      v
                         Deterministic model evaluation
                                      |
                                      v
                           Canonical RTS evaluation
                                      |
                    ┌─────────────────┼─────────────────┐
                    v                 v                 v
             Narrative report    Visual dashboard  Analyst workbench
```

Candidate discovery proposes. The user decides which candidates advance to full chart evaluation. Closing the browser must not cancel an accepted evaluation job.

## Candidate discovery

Candidate generation has three distinct sources:

1. Deterministic screens apply configured fundamental, market, liquidity, price, and eligibility rules to a defined universe.
2. AI-assisted discovery searches approved current sources for earnings, index changes, sector developments, unusual activity, and other catalysts.
3. Manual discovery allows a user to add a symbol, ETF, or watchlist entry directly.

All sources feed a shared Candidate Inbox. Every candidate records its discovery source, asset type, data timestamp, strategy version, evidence, risks, eligibility results, and current workflow status.

AI-supplied facts about current prices, fundamentals, events, or dates must originate from approved tools or sources rather than model memory. Substantive external claims retain citations and retrieval timestamps. Primary sources are preferred, and secondary-source claims are labeled accordingly.

## Candidate approval gate

Candidates remain proposals until the user selects them. The inbox supports selection, rejection, deferral, exclusion, and advancement to full evaluation.

The approval gate prevents the discovery agent from spending data, computing, or AI resources on every proposed symbol and preserves human control over the evaluation workflow.

## Trading-model platform

RTS distinguishes a trading or evaluation model from a generative AI model.

A versioned trading-model definition includes:

- Identity, description, ownership, and version
- Eligible security and asset types
- Required timeframes and bar intervals
- Market-session behavior
- Indicator definitions and parameters
- Evaluation rules and typed operators
- Mandatory and optional criteria
- Rule and timeframe weights
- Category and overall score aggregation
- Pass, warning, and rejection behavior
- Optional AI-interpretation instructions

System templates can be copied into user-owned models. Editing a model creates a new version when historical meaning would otherwise change. Existing evaluations retain the exact model and strategy versions used.

## Market sessions

The initial chart model uses regular trading hours. Extended hours are an optional capability.

The model must distinguish three settings:

- Whether extended-hours bars are retrieved
- Whether extended-hours bars are displayed
- Whether extended-hours bars participate in indicator calculations and scoring

An evaluation records all three settings. Extended-hours activity must never be included silently.

## Initial chart specification

The first model requires:

| View | Bar interval | Indicators |
|---|---|---|
| 5-day | 5-minute | Volume, Bollinger Bands, SMA50, MACD, RSI |
| 1-month | Daily | Volume, Bollinger Bands, SMA50, MACD, RSI |
| 3-month | Daily | Volume, Bollinger Bands, SMA50, MACD, RSI |

Initial indicator parameters are:

- Bollinger Bands: 20 periods and 2 standard deviations
- Simple moving average: 50 periods
- MACD: 12, 26, and 9
- RSI: 14

RTS retrieves sufficient warm-up bars to calculate every indicator correctly before limiting the displayed range. It normalizes timestamps, market sessions, missing bars, and corporate-action adjustments before evaluation.

Users do not upload screenshots during the normal workflow. RTS retrieves OHLCV data, calculates indicators, and renders interactive charts. It may generate controlled chart images internally when a vision model adds value, but numerical facts come from normalized data and deterministic calculations rather than pixel interpretation.

## Canonical evaluation

Every completed evaluation produces one canonical, versioned result containing:

- Evaluation and data timestamps
- Security and asset identity
- Fundamental and catalyst evidence
- Findings for every timeframe
- Indicator values and rule results
- Category scores and overall score
- Weights and reasons for deductions
- Bullish, bearish, and conflicting evidence
- Risks, uncertainty, confidence, and missing-data warnings
- Scenario levels and their calculated basis
- Candidate comparisons when requested
- Data-provider, strategy, trading-model, and calculation versions
- AI provider, model, prompt, and schema versions when AI participates
- Citations and evidence identifiers

Presentation modes cannot change the canonical facts, calculations, scores, or conclusions.

## Evaluation, education, scenarios, and trade planning

RTS treats the following as separate content areas:

1. Evaluation contains evidence, calculations, scores, risks, and uncertainty.
2. Educational interpretation explains concepts and why findings matter.
3. Scenario planning describes explicit conditional outcomes and invalidation conditions.
4. Personalized trade planning may include entry levels, position sizes, or calls to action only if RTS deliberately adopts that product scope and its legal, compliance, suitability, and governance requirements.

Specific entry zones, share quantities, and personalized allocations are not merely presentation details. The product must not enable them for general users without an explicit scope decision and qualified review.

## Adaptive presentation

RTS presents the same canonical evaluation through approved layouts rather than allowing generative AI to invent arbitrary interfaces.

Initial presentation modes are:

- Narrative Report: a linear analyst-style report with findings, grades, scenarios, and comparisons
- Interactive Dashboard: charts, scorecards, evidence panels, visual risk/reward, and progressive drill-down
- Analyst Workbench: synchronized charts, dense indicator grids, rule-level scoring, model controls, and minimal narrative

The narrative output used during early RTS development is the reference for the Narrative Report mode and must remain available.

## Learned presentation preferences

Experience, age range, generational preference, explicit choices, accessibility settings, and observed interaction patterns are separate inputs to presentation selection.

- Trading experience controls analytical controls and optional educational assistance.
- Age range and generational preference may initialize layout and information-architecture defaults.
- Explicit user choices override inferred defaults.
- Observed behavior may refine presentation only when behavioral learning is enabled.

Useful preference signals may include selected presentation mode, expanded sections, preferred chart or table views, comparison usage, evidence drill-down, and recurring overrides of the default layout.

RTS must allow users to inspect, edit, disable, and reset learned preferences. Preference learning must be transparent, data-minimized, and stored separately from trading-model settings, investor profile, and risk settings.

Presentation preferences never alter candidate rankings, market facts, evaluation rules, scores, entry levels, position sizes, or assumed risk tolerance.

## Generative AI responsibilities

Generative AI may:

- Translate natural-language strategy intent into a structured draft
- Discover and summarize current catalysts using approved research tools
- Produce grounded narratives from canonical evaluation facts
- Interpret controlled chart images alongside calculated indicators
- Adapt content arrangement within approved presentation templates
- Explain evidence at a requested level of detail
- Compare completed candidate evaluations

Generative AI may not:

- Invent current prices, fundamentals, catalysts, dates, or citations
- Change deterministic scores or model results
- Save a proposed strategy without validation and user approval
- Advance candidates through approval gates without authorization
- Infer risk tolerance from age or presentation behavior
- Execute trades

## Agentic workflows

An RTS agent operates through narrowly defined application tools such as:

```text
LoadActiveStrategy
LoadPresentationProfile
LoadPriorEvaluationHistory
RunDeterministicScreen
FindCurrentCatalysts
RetrieveMarketData
ValidateCandidateEligibility
RankCandidates
CreateCandidateShortlist
QueueSelectedEvaluations
CompareCompletedEvaluations
GenerateGroundedPresentation
```

Early tools are read-only or create drafts. Material state changes require explicit user approval. Tool inputs and outputs are typed, validated, authorized, and audited.

## AI governance

AI integrations use provider-neutral Application interfaces with provider implementations in Infrastructure. Each use case defines structured inputs and outputs, evidence requirements, failure behavior, and a deterministic fallback where practical.

RTS records model and prompt versions, structured outputs, evidence references, latency, usage, cost, approval actions, and failures. It supports feature flags, provider replacement, evaluation datasets, regression testing, safety tests, and an operational kill switch.

The system sends only the data required for the approved use case. Secrets, private diagnostics, and unrelated user information must not enter prompts.

## Product principle

> RTS learns how each user prefers to receive and explore information while preserving one evidence-based evaluation and keeping presentation preferences separate from trading decisions.

