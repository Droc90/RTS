# RTS development checklist

## Current status

- Current branch: `feature/evaluation-worker-charts`
- Current milestone: Chart Evaluation and Scoring
- Application Foundation: 100% complete
- Configurable Trading Model and Criteria Foundation: 100% complete
- Candidate Discovery and Screening: 100% complete
- Overall RTS checklist: 59% complete (63 of 106 items)
- Cumulative active developer effort: 20–21 hours
- Architecture: [RTS application architecture](architecture.md)
- Product and AI design: [RTS product, AI, and adaptive-presentation design](product-ai-and-presentation.md)

The overall percentage is calculated from the complete current checklist. It may decrease when newly accepted product scope adds work; completed items and recorded effort do not decrease.

## Application Foundation — complete

- [x] Copy and rename Shell
- [x] Create RTS database
- [x] Configure migrations and connection
- [x] Create administrator account
- [x] Verify authentication and administration
- [x] Apply RTS branding and logo
- [x] Establish Git repository
- [x] Add Radzen
- [x] Add persistent light and dark modes
- [x] Build and test

## Configurable Trading Model and Criteria Foundation — complete

- [x] Define strategy and trading-model ownership
- [x] Create versioned screening-strategy profiles
- [x] Create versioned trading/evaluation-model definitions
- [x] Define configurable indicators and typed parameters
- [x] Define configurable timeframes and bar intervals
- [x] Define regular and extended market-session behavior
- [x] Define operators and supported data types
- [x] Separate screening, scoring, and presentation rules
- [x] Support system templates and user-owned copies
- [x] Add rule, parameter, and model validation
- [x] Create indicator and evaluator registries
- [x] Preserve criteria and model versions for historical results
- [x] Add authoritative database scripts and EF Core mappings
- [x] Add application services and automated tests

Developer effort completed: approximately 4 active developer hours.

## Candidate Discovery and Screening — complete

- [x] Translate the initial methodology into rule definitions
- [x] Define candidate universes and supported asset types
- [x] Support manual and watchlist candidate entry
- [x] Run deterministic fundamental and market screens
- [x] Add AI-assisted catalyst discovery
- [x] Record sources, citations, timestamps, and evidence quality
- [x] Track previously evaluated, rejected, deferred, and excluded symbols
- [x] Execute repeatable candidate-discovery runs
- [x] Rank candidates using visible factors
- [x] Build the Candidate Inbox
- [x] Require user selection before full evaluation
- [x] Explain why candidates passed, warned, or failed
- [x] Save discovery runs and criteria snapshots
- [x] Export candidate lists
- [x] Persist per-user candidate-identification settings
- [x] Configure universe, liquidity, fundamental, catalyst, risk, portfolio, and ETF identification gates
- [x] Snapshot candidate-identification settings into AI discovery runs

## Market Data, Chart Automation, and Evaluation Jobs — complete

- [x] Define market-data coverage, freshness, and licensing requirements
- [x] Create a provider-neutral market-price-data interface
- [x] Retrieve OHLCV bars, quotes, and corporate actions
- [x] Use regular trading hours by default for the initial model
- [x] Support optional extended-hours retrieval and display
- [x] Configure whether extended hours affect indicators and scoring
- [x] Normalize timestamps, sessions, missing bars, and adjustments
- [x] Retrieve sufficient warm-up history for indicator calculations
- [x] Cache or persist normalized evaluation inputs
- [x] Queue durable evaluation jobs for selected candidates
- [x] Display evaluation-job progress, failures, cancellation, and retry
- [x] Render model-driven interactive financial charts
- [x] Reproduce the initial 5D, 1M, and 3M chart specification

## Chart Evaluation and Scoring

- [x] Define technical and chart-evaluation criteria
- [x] Calculate Bollinger Bands, SMA, MACD, and RSI for the initial model
- [x] Produce separate findings and scores by timeframe
- [x] Apply configurable weights, thresholds, and mandatory rules
- [x] Produce category and overall scores
- [ ] Identify bullish, bearish, and conflicting evidence
- [x] Explain every score contribution and deduction
- [ ] Produce the canonical comprehensive evaluation
- [ ] Preserve data, model, criteria, and calculation snapshots
- [x] Add calculation and evaluation regression tests

## Adaptive Evaluation Presentation

- [ ] Define structured canonical evaluation content blocks
- [ ] Separate evaluation, education, scenarios, and personalized trade planning
- [ ] Implement the Narrative Report presentation
- [ ] Implement the Interactive Dashboard presentation
- [ ] Implement the Analyst Workbench presentation
- [ ] Define experience-based analytical and educational options
- [ ] Define age-range and generational presentation presets
- [ ] Allow explicit layout and information-presentation preferences
- [ ] Add opt-in behavioral preference learning
- [ ] Track confidence and evidence for inferred preferences
- [ ] Let users inspect, edit, disable, and reset learned preferences
- [ ] Guarantee identical facts and scores across presentation modes
- [ ] Support responsive and accessible presentation variants
- [ ] Test presentation modes with varied users and preferences

## AI Enablement and Agentic Workflows

- [x] Create provider-neutral generative-AI application interfaces
- [x] Require structured outputs linked to evidence identifiers
- [ ] Add a natural-language strategy-drafting assistant
- [x] Add the catalyst-discovery agent
- [ ] Generate grounded evaluation narratives
- [ ] Combine controlled chart images with calculated metrics when vision adds value
- [ ] Expose narrowly defined, typed, and authorized agent tools
- [ ] Require user approval for material state changes
- [ ] Record provider, model, prompt, schema, usage, latency, cost, and outcomes
- [ ] Add AI data-minimization and privacy controls
- [ ] Build AI evaluation datasets and regression tests
- [ ] Add safety testing, feature flags, deterministic fallbacks, and a kill switch
- [ ] Define legal and governance boundaries for personalized recommendations
- [ ] Prohibit autonomous trade execution

## Workflow and Administration

- [ ] Move candidates through discovery, review, evaluation, and decision stages
- [x] Add watchlists and exclusions
- [ ] Manage user presentation and trading preferences separately
- [ ] Support strategy and trading-model sharing or copying
- [ ] Add data-provider and AI-provider configuration
- [ ] Add product-specific audit history and error monitoring

## Quality and Release

- [ ] Maintain automated unit, integration, and regression tests
- [ ] Perform security and authorization review
- [ ] Perform performance and background-job load testing
- [ ] Verify accessibility and responsive layouts
- [ ] Review market-data licensing and display obligations
- [ ] Review AI, financial-communication, and personalized-recommendation scope
- [ ] Prepare deployment and operational configuration
- [ ] Document backup, recovery, retention, and incident procedures

## Completed effort

| Milestone | Active developer effort | Cumulative effort |
|---|---:|---:|
| Application Foundation | 3–4 hours | 3–4 hours |
| Configurable Trading Model and Criteria Foundation | 4 hours | 7–8 hours |
| Candidate Discovery and Screening | 4 hours | 11–12 hours |
| Market Data, Chart Automation, and Evaluation Jobs | 6 hours | 17–18 hours |
| Chart Evaluation and Scoring (in progress) | 3 hours | 20–21 hours |
