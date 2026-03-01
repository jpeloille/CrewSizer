---
name: oro-ftl
description: >
  Complete EU Regulation 965/2012 Subpart ORO.FTL and CS FTL.1 — all FDP tables (acclimatised, 
  unknown, with FRM), extensions (in-flight rest, split duty, planned), augmented crew, rest 
  facility classes 1/2/3, cumulative flight time and duty limits, rest periods (home base, away, 
  reduced, extended recovery), standby (airport/other), reserve, positioning, disruptive schedules, 
  WOCL, acclimatisation, delayed reporting, commander's discretion, FRM. Operator-agnostic — no 
  simplifications. Use for FTL rule implementation, crew scheduling, roster generation, duty/rest 
  validation, FDP calculations, cumulative counters, fatigue compliance. Trigger on: FTL, FDP, TSV, 
  HDV, duty hours, rest periods, WOCL, ORO.FTL, EU-OPS Subpart Q, CS FTL, augmented crew, in-flight 
  rest, split duty, standby, reserve, acclimatisation, margin calculation, crew sizing.
---

# ORO.FTL — Flight Time Limitations

Complete regulatory framework from **EU Regulation 965/2012, Annex III, Subpart FTL** and 
**CS FTL.1** (Certification Specifications for Scheduled and Charter Operations), as amended 
by **Regulation 83/2014**.

This skill is **operator-agnostic** — it contains all provisions of the regulation without 
any simplifications tied to a specific operator, aircraft type, network, or crew configuration.

## Regulatory Hierarchy

```
EU Regulation 965/2012, Annex III
└── Subpart FTL (ORO.FTL.100 – ORO.FTL.250)     ← Implementing Rules (IR), binding
    └── CS FTL.1 (CS FTL.1.100 – CS FTL.1.235)   ← Certification Specifications, default compliance
        └── AMC / GM                               ← Acceptable Means of Compliance / Guidance Material
```

Operators must comply with ORO.FTL and apply CS FTL.1 as the default means of compliance.
Deviations from CS FTL.1 require prior competent authority approval under Article 22(2) 
of Regulation 216/2008, including FRM-based justification and data collection.

## Document Structure

- **SKILL.md** (this file): Overview, key rules, quick-reference tables, implementation guidance
- **references/regulations.md**: Complete article-by-article regulatory text with all tables, 
  all CS FTL.1 provisions, and guidance material summaries

**When to read the reference file:**
- Implementing FDP max lookup → read Section 3 (FDP tables — Table 2, 3, 4, extension tables)
- Implementing in-flight rest logic → read Section 4 (augmented crew, rest facilities, cabin crew)
- Implementing standby/reserve → read Section 8 and 9 (complex FDP reduction rules)
- Implementing rest calculations → read Section 10 (reduced rest, rotation rest, disruptive schedules)
- Implementing acclimatisation → read Section 2 (Table 1, state determination)
- Need the delayed reporting algorithm → read Section 5

---

## Key Definitions (ORO.FTL.105)

| # | Term | Definition |
|---|------|-----------|
| 1 | **Acclimatised** | Circadian clock synchronized to local time zone. Considered acclimatised within ±2h time zone band of departure point. Beyond 2h: Table 1 determines state (B/D/X) based on time zones crossed and time elapsed |
| 5 | **Augmented flight crew** | More than minimum crew required, allowing in-flight rest with replacement by qualified crew |
| 6 | **Break** | Period within FDP, shorter than rest, free of all tasks, counts as duty |
| 8 | **Disruptive schedule** | Roster disrupting sleep during optimal window. Early start (05:00–05:59 or 05:00–06:59), late finish (23:00–01:59 or 00:00–01:59), night duty (encroaching 02:00–04:59). Early/late type determined by competent authority per ARO.OPS.230 |
| 9 | **Night duty** | Duty encroaching any portion of 02:00–04:59 in acclimatised time |
| 10 | **Duty** | Any task for the operator: flight duty, admin, training/checking, positioning, some standby |
| 11 | **Duty period** | Start: required to report/commence duty. End: free of all duties including post-flight |
| 12 | **FDP** | Report for duty (including sector(s)) → engines off at end of last operated sector |
| 13 | **Flight time** | First movement from parking for takeoff → rest on parking with engines shut down |
| 14 | **Home base** | Assigned location where crew normally starts/ends duty. Operator not responsible for accommodation |
| 15 | **Local day** | 24h period from 00:00 local |
| 16 | **Local night** | 8h period between 22:00 and 08:00 local |
| 19 | **Rest facility** | Class 1: bunk ≥80° recline, separated, light/noise control. Class 2: seat ≥45°, 55" pitch, 20" width, curtain. Class 3: seat ≥40°, leg support, curtain, not adjacent to passengers |
| 20 | **Reserve** | Available for assignment, notified ≥10h in advance. Not standby |
| 21 | **Rest period** | Continuous, uninterrupted, free of all duties, standby, and reserve |
| 22 | **Rotation** | Duty/duties with ≥1 flight duty + rest out of home base, starting and ending at home base |
| 23 | **Single day free** | 1 day + 2 local nights, free of all duties and standby, notified in advance |
| 25 | **Standby** | Pre-notified period, available for assignment without intervening rest. Airport (26) or other/home (27) |
| 28 | **WOCL** | Window of Circadian Low: **02:00–05:59** acclimatised time |

---

## Cumulative Limits (ORO.FTL.210)

### Flight Time Limits

| Window | Maximum | Type |
|--------|---------|------|
| 28 consecutive days | **100 hours** | Rolling |
| Calendar year | **900 hours** | Calendar (Jan 1 – Dec 31) |
| 12 consecutive calendar months | **1,000 hours** | Rolling by calendar month |

### Duty Period Limits

| Window | Maximum | Type |
|--------|---------|------|
| 7 consecutive days | **60 hours** | Rolling |
| 14 consecutive days | **110 hours** | Rolling |
| 28 consecutive days | **190 hours** (spread evenly) | Rolling |

Post-flight duty counts as duty period. Operator specifies minimum post-flight duty time in OM.

---

## FDP — Quick Reference (ORO.FTL.205)

### Basic Maximum FDP (Table 2 — Acclimatised Crew)

| Start of FDP (ref. time) | 1–2 sec | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 |
|--------------------------|:-------:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:----:|
| 06:00–13:29 | 13:00 | 12:30 | 12:00 | 11:30 | 11:00 | 10:30 | 10:00 | 09:30 | 09:00 |
| 13:30–13:59 | 12:45 | 12:15 | 11:45 | 11:15 | 10:45 | 10:15 | 09:45 | 09:15 | 09:00 |
| 14:00–14:29 | 12:30 | 12:00 | 11:30 | 11:00 | 10:30 | 10:00 | 09:30 | 09:00 | 09:00 |
| 14:30–14:59 | 12:15 | 11:45 | 11:15 | 10:45 | 10:15 | 09:45 | 09:15 | 09:00 | 09:00 |
| 15:00–15:29 | 12:00 | 11:30 | 11:00 | 10:30 | 10:00 | 09:30 | 09:00 | 09:00 | 09:00 |
| 15:30–15:59 | 11:45 | 11:15 | 10:45 | 10:15 | 09:45 | 09:15 | 09:00 | 09:00 | 09:00 |
| 16:00–16:29 | 11:30 | 11:00 | 10:30 | 10:00 | 09:30 | 09:00 | 09:00 | 09:00 | 09:00 |
| 16:30–16:59 | 11:15 | 10:45 | 10:15 | 09:45 | 09:15 | 09:00 | 09:00 | 09:00 | 09:00 |
| 17:00–04:59 | 11:00 | 10:30 | 10:00 | 09:30 | 09:00 | 09:00 | 09:00 | 09:00 | 09:00 |
| 05:00–05:14 | 12:00 | 11:30 | 11:00 | 10:30 | 10:00 | 09:30 | 09:00 | 09:00 | 09:00 |
| 05:15–05:29 | 12:15 | 11:45 | 11:15 | 10:45 | 10:15 | 09:45 | 09:15 | 09:00 | 09:00 |
| 05:30–05:44 | 12:30 | 12:00 | 11:30 | 11:00 | 10:30 | 10:00 | 09:30 | 09:00 | 09:00 |
| 05:45–05:59 | 12:45 | 12:15 | 11:45 | 11:15 | 10:45 | 10:15 | 09:45 | 09:15 | 09:00 |

### Unknown State of Acclimatisation (Table 3 — no FRM / Table 4 — with FRM)

| Table | 1–2 sec | 3 | 4 | 5 | 6 | 7 | 8 |
|-------|:-------:|:---:|:---:|:---:|:---:|:---:|:---:|
| **Table 3** (no FRM) | 11:00 | 10:30 | 10:00 | 09:30 | 09:00 | 09:00 | 09:00 |
| **Table 4** (with FRM) | 12:00 | 11:30 | 11:00 | 10:30 | 10:00 | 09:30 | 09:00 |

### FDP Extension Summary (Mutually Exclusive)

| Method | Max Extension | Key Conditions | Ref |
|--------|:------------:|----------------|-----|
| **Without in-flight rest** | +1h | ≤2× per 7 days. Rest +2h pre+post OR +4h post. Max 5 sectors (no WOCL), 4 (WOCL≤2h), 2 (WOCL>2h) | ORO.FTL.205(d), CS FTL.1.205(b) |
| **In-flight rest (1 extra FCM)** | up to 14h/15h/16h | Class 3/2/1 facility. Max 3 sectors. Min rest 90min (2h for landing FCM). Min dest rest max(duty,14h) | CS FTL.1.205(c)(2)(i) |
| **In-flight rest (2 extra FCM)** | up to 15h/16h/17h | Class 3/2/1. Same conditions as above | CS FTL.1.205(c)(2)(ii) |
| **In-flight rest +1h bonus** | +1h above limits | Only if ≤2 sectors AND 1 sector >9h continuous flight | CS FTL.1.205(c)(4) |
| **Split duty** | +50% of break | Break ≥3h. Time >6h or encroaching WOCL excluded. Cannot follow reduced rest | ORO.FTL.220, CS FTL.1.220 |
| **Commander's discretion** | +2h (std) / +3h (augmented) | Unforeseen only. Post-FDP rest ≥10h. Report required | ORO.FTL.205(f) |

**Critical rule**: Extension methods are **mutually exclusive** — you cannot combine in-flight 
rest with split duty or with planned FDP extension without in-flight rest in the same FDP.

---

## Rest Periods — Quick Reference (ORO.FTL.235)

| Situation | Minimum Rest |
|-----------|-------------|
| **At home base** | max(preceding duty period, **12h**) |
| **Away from home base** | max(preceding duty period, **10h**), must include 8h sleep opportunity + travel + physiological needs (≥1h) |
| **After augmented FDP** | max(preceding duty period, **14h**) |
| **Reduced rest (with FRM)** | Minimum 10h at home base; must be compensated on subsequent rest |
| **Extended recovery rest** | **36h** including **2 local nights**. Max gap between extended rests: **168h** (7 days). Increased to 2 local days, twice per month |

---

## Standby & Reserve — Quick Reference

### Airport Standby (CS FTL.1.225(a))
- Counts **in full** as duty for ORO.FTL.210 and ORO.FTL.235
- If FDP assigned during ASB: FDP counts from FDP start; max FDP reduced by ASB time >4h
- Max combined ASB + FDP = **16 hours**

### Other Standby / Home Standby (CS FTL.1.225(b))
- Max duration: **16 hours**
- Combination standby + FDP ≤ **18 hours awake time**
- **25%** of time counts as duty for ORO.FTL.210
- FDP impact: ceases ≤6h → full FDP from reporting; ceases >6h → FDP reduced by excess over 6h
- If FDP extended (in-flight rest or split duty): 6h threshold becomes **8h**
- Night standby (23:00–07:00): time between 23:00–07:00 excluded from FDP reduction until contacted

### Reserve (ORO.FTL.230, CS FTL.1.230)
- Notified ≥10h in advance
- Reserve times do NOT count as duty for ORO.FTL.210 or ORO.FTL.235
- ORO.FTL.235(d) extended recovery rest applies to crew on reserve
- Max consecutive reserve days specified in flight time specification scheme

---

## Acclimatisation State (ORO.FTL.105, Table 1)

| Time zone difference | <48h elapsed | 48–71:59h | 72–95:59h | 96–119:59h | ≥120h |
|---------------------|:---:|:---:|:---:|:---:|:---:|
| <4h | B | D | D | D | D |
| ≤6h | B | X | D | D | D |
| ≤9h | B | X | X | D | D |
| ≤12h | B | X | X | X | D |

- **B** = acclimatised to departure time zone → use Table 2
- **D** = acclimatised to destination → use Table 2 with destination reference time
- **X** = unknown state → use Table 3 (or Table 4 if FRM implemented)

---

## Implementation Decision Tree

```
1. Determine acclimatisation state (Table 1)
   ├── B or D → Use Table 2 with appropriate reference time
   └── X → Use Table 3 (or Table 4 with FRM)

2. Look up basic max FDP (reference time × sector count)

3. Apply applicable extension (only ONE):
   ├── In-flight rest? → CS FTL.1.205(c) tables (requires augmented crew)
   ├── Split duty? → +50% of break (≥3h, cap at 6h, exclude WOCL)
   ├── Planned extension? → CS FTL.1.205(b) table (+1h, ≤2× per 7 days)
   └── None → basic max FDP applies

4. Check night duty provisions (CS FTL.1.205(a))
   └── Consecutive night duties: max 4 sectors per duty

5. Apply standby impact if applicable
   ├── Airport standby: reduce FDP by ASB time >4h; combined ≤16h
   └── Other standby: reduce FDP by SBY time >6h (or >8h if extended FDP)

6. Validate cumulative limits (ORO.FTL.210)
   ├── Flight time: 100h/28d, 900h/year, 1000h/12 months
   └── Duty: 60h/7d, 110h/14d, 190h/28d

7. Validate rest (ORO.FTL.235)
   ├── Minimum rest before FDP
   ├── Extended recovery rest (36h/168h)
   └── Days off (Directive 2000/79/EC)

8. Commander's discretion (unforeseen only, post-reporting)
   ├── Standard crew: +2h max FDP, rest ≥10h
   └── Augmented crew: +3h max FDP, rest ≥10h
```

---

## Other Provisions

| Article | Subject | Key Points |
|---------|---------|------------|
| ORO.FTL.110 | Operator responsibilities | Publish rosters in advance; plan FDP completable within limits; change schedule if >33% exceed max FDP |
| ORO.FTL.115 | Crew member responsibilities | Comply with CAT.GEN.MPA.100(b); optimise use of rest |
| ORO.FTL.120 | FRM | Required when using deviations. Includes hazard identification, risk mitigation, safety assurance, promotion |
| ORO.FTL.125 | Flight time specification schemes | Must be approved by competent authority. Deviations from CS FTL.1 require full description + assessment |
| ORO.FTL.215 | Positioning | Counts as FDP (not sector) if before operating. All positioning time = duty |
| ORO.FTL.240 | Nutrition | Operator provides meals/drinks during FDP. At least 1 meal for FDP >6h |
| ORO.FTL.245 | Records | Individual records maintained **24 months**: flight times, duty, rest, days off |
| ORO.FTL.250 | Fatigue management training | Initial + recurrent for crew, roster planners, management. Described in OM |

---

## Common Implementation Pitfalls

1. **Rolling vs Calendar**: The 900h limit is per *calendar year* (Jan–Dec). The 1000h/12m is 
   rolling by *calendar month*. All other limits (100h/28d, 60h/7d, etc.) are rolling by *day*.

2. **Duty ≠ FDP**: Duty includes ground training, admin, positioning. FDP specifically includes 
   flying. Both have separate cumulative limits. Don't confuse them.

3. **Rest ≠ flat 12h**: It's `max(preceding_duty, 12h)` at home base. A 14h duty day requires 
   14h rest, not 12h. After augmented FDP: `max(preceding_duty, 14h)`.

4. **Extensions are mutually exclusive**: Never combine in-flight rest + split duty + planned 
   extension in the same FDP.

5. **Extended recovery rest ≠ just 36h**: Must include **2 local nights** (22:00–08:00). 36 hours 
   starting at 10:00 spans only 1 local night → invalid.

6. **Standby FDP reduction**: The 6h/8h threshold and night exclusion rules are complex. 
   See references/regulations.md Section 8 for the complete algorithm.

7. **Acclimatisation matters**: For operators with time zone crossings, the reference time for 
   FDP table lookup depends on acclimatisation state, not just local departure time.

8. **Cabin crew FDP start**: When cabin crew reports earlier than flight crew, FDP starts at 
   cabin crew reporting time but max FDP is based on flight crew reporting time (difference ≤1h).

9. **Commander's discretion ≠ planned extension**: It applies only to unforeseen circumstances 
   starting at or after reporting. Cannot be planned. Requires crew consultation and reporting.

10. **Night duty sector limit**: CS FTL.1.205(a)(1) limits consecutive night duties to 
    **4 sectors per duty** — this is additional to the FDP table sector-based reduction.
