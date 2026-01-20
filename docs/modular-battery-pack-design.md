# Familiar Modular Battery Pack System
## PCB Design Specification

**Target Device:** Familiar Firmware (Raspberry Pi 5V @ 3A)
**Configuration:** 2S Li-ion (7.4V nominal, 6.0V-8.4V range)
**Architecture:** Distributed BMS (BMS on each pack for hot-swap capability)
**Buck Converter:** Allegro A6211GLJTR-T

---

## 1. System Overview

```
┌─────────────┐   ┌─────────────┐   ┌─────────────┐
│   PACK 1    │   │   PACK 2    │   │   PACK 3    │
│  2S 21700   │   │  2S 21700   │   │  2S 21700   │
│  + BMS      │   │  + BMS      │   │  + BMS      │
└──────┬──────┘   └──────┬──────┘   └──────┬──────┘
       │                 │                 │
       └────────┬────────┴────────┬────────┘
                │   OR-ing Diodes │
                │   (hot-swap)    │
                ▼                 ▼
        ┌───────────────────────────────┐
        │      MAIN BOARD               │
        │  ┌─────────────────────────┐  │
        │  │  Custom Buck Converter  │  │
        │  │  A6211 - 5V @ 2A        │  │
        │  └─────────────────────────┘  │
        │              │                │
        │              ▼                │
        │         5V Rail → Pi         │
        └───────────────────────────────┘
```

---

## 2. Battery Pack Module (SMD Version)

**Board Size:** 55mm x 35mm
**Layers:** 2-layer, 1oz copper
**Cell Format:** 21700 Li-ion (2S series)

### 2.1 PCB Layout

```
┌─────────────────────────────────────────────────────────────────┐
│                   BATTERY PACK PCB (SMD)                        │
│                                                                 │
│  ┌────────────┐      ┌────────────┐                            │
│  │  21700     │      │  21700     │                            │
│  │  CELL 1    │      │  CELL 2    │    2S / 7.4V nom           │
│  │            │      │            │                            │
│  │  nickel    │      │  nickel    │                            │
│  │  strips    │      │  strips    │                            │
│  └─────┬──────┘      └─────┬──────┘                            │
│        │                   │                                    │
│  ══════╪═══════════════════╪════════════════════════════       │
│        │    SERIES CONN    │                                    │
│   B- ──┴───────────────────┴── B+ (to BMS)                     │
│                   │BM (balance mid)                             │
│                   │                                             │
│   ┌───────────────┴───────────────────────────────────┐        │
│   │              2S BMS (SMD ICs)                     │        │
│   │                                                   │        │
│   │  ┌────────┐  ┌────────┐  ┌────────┐             │        │
│   │  │S8254AA │  │FS8205A │  │FS8205A │             │        │
│   │  │ TSSOP8 │  │ TSSOP8 │  │ TSSOP8 │             │        │
│   │  │        │  │DUAL FET│  │DUAL FET│             │        │
│   │  │PROTECT │  │ CHG    │  │ DSG    │             │        │
│   │  └───┬────┘  └───┬────┘  └───┬────┘             │        │
│   │      │           │           │                   │        │
│   │  ┌───┴───┐   ┌───┴───┐   ┌───┴───┐              │        │
│   │  │0402   │   │0402   │   │0402   │  resistors   │        │
│   │  │100Ω   │   │1kΩ    │   │1kΩ    │              │        │
│   │  └───────┘   └───────┘   └───────┘              │        │
│   │                                                   │        │
│   │  NTC ●──[0402 10k]──┐  thermistor input         │        │
│   │                      │                           │        │
│   └──────────────────────┴───────────────────────────┘        │
│                          │                                     │
│              P- ─────────┴─────── P+                           │
│               │                   │                            │
│   ┌───────────┴───────────────────┴───────────────┐           │
│   │         6-PIN POGO/SPRING CONNECTOR           │           │
│   │  ┌───┬───┬───┬───┬───┬───┐                   │           │
│   │  │P- │P- │NTC│ID │P+ │P+ │ (dual pins        │           │
│   │  └───┴───┴───┴───┴───┴───┘   for current)    │           │
│   └───────────────────────────────────────────────┘           │
│                                                                │
│   LED: 0603 RED/GRN common cathode (charge status)            │
│                                                                │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 BMS Protection Circuit

```
        B- (Cell 1 negative)
         │
         │      ┌─────────────────────────────────────┐
         │      │           S8254AA                   │
         │      │         (Protection IC)             │
         │      │                                     │
         │      │  OVP: 4.25V/cell (overcharge)      │
         │      │  ODP: 2.5V/cell  (over-discharge)  │
         │      │  OCP: 8A         (overcurrent)     │
         │      │  SCP: 25A        (short circuit)   │
         │      │                                     │
         │      │  VDD ────── B+                     │
         │      │  VSS ────── B-                     │
         │      │  VM  ────── Balance Mid            │
         │      │  CO  ────── Charge FET Gate        │
         │      │  DO  ────── Discharge FET Gate     │
         │      └─────────────────────────────────────┘
         │
         ├──────┤FS8205A├──────┤FS8205A├────── P-
         │      (CHG FET)      (DSG FET)
         │
        GND
```

---

## 3. Main Board Power Section (SMD)

**Layers:** 4-layer recommended (dedicated power/ground planes)
**Input Voltage:** 6.0V - 8.4V (from battery packs)
**Output:** 5V @ 2A continuous (3A peak)

### 3.1 PCB Layout

```
┌──────────────────────────────────────────────────────────────────────────┐
│                    MAIN BOARD - POWER SECTION (SMD)                      │
│                                                                          │
│  PACK CONNECTORS (spring-loaded pogo receptacles)                        │
│  ┌──────────┐    ┌──────────┐    ┌──────────┐                           │
│  │  PACK 1  │    │  PACK 2  │    │  PACK 3  │                           │
│  │  6-pin   │    │  6-pin   │    │  6-pin   │                           │
│  └────┬─────┘    └────┬─────┘    └────┬─────┘                           │
│       │               │               │                                  │
│       │    ┌──────────┴───────────────┤                                 │
│       │    │                          │                                  │
│  ┌────┴────┴────┐              ┌──────┴─────┐                           │
│  │ SS34 x3      │              │ ID/NTC     │                           │
│  │ DO-214AB     │              │ to MCU     │  pack detection           │
│  │ (OR-ing)     │              │ GPIO/ADC   │                           │
│  └──────┬───────┘              └────────────┘                           │
│         │                                                                │
│         │ VBAT (6V - 8.4V)                                              │
│         │                                                                │
│  ┌──────┴──────┐    ┌─────────────────┐                                 │
│  │ 100µF x2    │    │  SUPERCAP       │                                 │
│  │ 1206 MLCC   │    │  1F 10V         │   hot-swap ride-through        │
│  │ 25V X5R     │    │  (through-hole) │                                 │
│  └──────┬──────┘    └────────┬────────┘                                 │
│         │                    │                                           │
│         └─────────┬──────────┘                                          │
│                   │                                                      │
│ ══════════════════╪══════════════════════════════════════════════       │
│                   │                                                      │
│         ┌─────────┴─────────────────────────────────────────┐           │
│         │           CUSTOM BUCK CONVERTER                    │           │
│         │           A6211 - 5V @ 2A                          │           │
│         │                                                    │           │
│         │   VIN ──┬──[L1]──┬── VOUT (5V)                    │           │
│         │         │        │                                 │           │
│         │    ┌────┴────┐   │                                │           │
│         │    │ A6211   │   ├──[C_OUT]──┐                    │           │
│         │    │ SOT23-8L│   │           │                    │           │
│         │    └─────────┘   │      ┌────┴────┐               │           │
│         │                  │      │ 22µF x2 │               │           │
│         │   L1: 10µH       │      │ 0805    │               │           │
│         │   IHLP2525       │      └─────────┘               │           │
│         │                  │                                 │           │
│         └──────────────────┴─────────────────────────────────┘           │
│                   │                                                      │
│                   │ 5V @ 2A                                             │
│ ══════════════════╪══════════════════════════════════════════════       │
│                   │                                                      │
│         ┌─────────┴─────────┐                                           │
│         │  POWER MONITOR    │                                           │
│         │  INA219 (MSOP10)  │──→ I2C to Pi (SDA/SCL)                   │
│         │  R_SHUNT: 0.01Ω   │                                           │
│         └─────────┬─────────┘                                           │
│                   │                                                      │
│              5V RAIL ──────────────────→ To Raspberry Pi                │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Buck Converter Design (A6211GLJTR-T)

### 4.1 Specifications

| Parameter | Value |
|-----------|-------|
| Input Voltage | 6.0V - 8.4V (2S Li-ion) |
| Output Voltage | 5.0V |
| Output Current | 2A continuous, 3A peak |
| Switching Frequency | ~300kHz (set by RTON) |
| Efficiency | ~90% typical |
| Package | SOT23-8L + Thermal Pad |

### 4.2 A6211 Pinout

| Pin | Name | Connection |
|-----|------|------------|
| 1 | VIN | Battery input + 10µF cap |
| 2 | TON | 150kΩ to GND (sets ~300kHz) |
| 3 | EN | 100kΩ to VIN (always on) |
| 4 | CS | Feedback divider (49.9k/10k) |
| 5 | VCC | Tie to VIN + 0.1µF bypass |
| 6 | GND | Ground plane |
| 7 | BOOT | 0.1µF to SW |
| 8 | SW | To inductor |
| 9 | PAD | Solder to GND (thermal) |

### 4.3 Schematic

```
                              VIN (6V - 8.4V)
                                    │
                   ┌────────────────┴────────────────┐
                   │                                 │
              ┌────┴────┐                      ┌─────┴─────┐
              │ 10µF    │                      │ 0.1µF     │
              │ 1206    │                      │ 0402      │
              │ 25V     │                      │           │
              └────┬────┘                      └─────┬─────┘
                   │                                 │
                   └────────────────┬────────────────┘
                                    │
                              VIN ──┤1
                                    │
              ┌──────────┐    TON ──┤2          5├── VCC ──┬── VIN
              │ RTON     │          │              │        │
              │ 150kΩ    ├──────────┤     A6211   │   ┌────┴────┐
              │ 0402     │          │              │   │ 0.1µF   │
              └────┬─────┘     EN ──┤3          8├─SW │ 0402    │
                   │           │    │              │   └────┬────┘
                   │      ┌────┴────┤            7├─BOOT    │
                   │      │100kΩ    │              │  │     │
                   │      │0402     │     PAD    CS├4 │  ┌──┴──┐
                   │      └────┬────┤9          6├─┴──┼──┤0.1µF│
                   │           │    └──────┬───────┘  │  │0402 │
                   │           │           │          │  └──┬──┘
                   └───────────┴───────────┴──GND     │     │
                                                      │     │
                                          ┌───────────┴─────┘
                                          │
                                    ┌─────┴─────┐
                                    │ L1 10µH   │  (A6211 prefers
                                    │ IHLP2525  │   higher L than
                                    │ 4A SAT    │   MP2315)
                                    └─────┬─────┘
                                          │
             ┌────────────────────────────┴───────────────┐
             │                            │               │
        ┌────┴────┐                  ┌────┴────┐    ┌─────┴─────┐
        │ 22µF x2 │                  │ 49.9kΩ  │    │  VOUT     │
        │ 0805    │                  │ 0402 1% │    │  5V       │
        │ 10V X5R │                  │ (R_TOP) │    │           │
        └────┬────┘                  └────┬────┘    └───────────┘
             │                            │
             │                       CS ──┤ (Pin 4, feedback)
             │                            │
             │                       ┌────┴────┐
             │                       │ 10kΩ    │
             │                       │ 0402 1% │
             │                       │ (R_BOT) │
             │                       └────┬────┘
             │                            │
             └────────────────────────────┴──────────── GND
```

### 4.4 Output Voltage Calculation

```
VOUT = VREF × (1 + R_TOP / R_BOT)
VOUT = 1.0V × (1 + 49.9kΩ / 10kΩ)
VOUT = 1.0V × 5.99
VOUT ≈ 5.0V
```

### 4.5 Frequency Setting

```
f_SW = 5000 / RTON(kΩ)
f_SW = 5000 / 150
f_SW ≈ 333 kHz
```

---

## 5. Hot-Swap Circuit

### 5.1 OR-ing Diode Configuration

```
Pack 1 ──┬──|>|──┐
         │ SS34  │
Pack 2 ──┼──|>|──┼───┬────[SUPERCAP]────┬────[BUCK]──→ 5V
         │ SS34  │   │     1F 10V       │
Pack 3 ──┴──|>|──┘   └────[100µF x2]────┘
            SS34
```

### 5.2 Ride-Through Calculation

```
Supercap Energy:   E = ½CV² = ½ × 1F × (8.4V)² = 35.3J
Power Draw:        P = 5V × 2A = 10W
Hold-up Time:      t = E/P = 35.3J / 10W ≈ 3.5 seconds

(Actual time lower due to dropout voltage, but >500ms guaranteed)
```

---

## 6. Bill of Materials

### 6.1 Battery Pack BOM

| Ref | Part Number | Package | Value | Qty | Description |
|-----|-------------|---------|-------|-----|-------------|
| U1 | S8254AA | TSSOP-8 | - | 1 | 2S Protection IC |
| U2,U3 | FS8205A | TSSOP-8 | - | 2 | Dual N-FET |
| R1 | - | 0402 | 100Ω | 1 | Current sense |
| R2,R3 | - | 0402 | 1kΩ | 2 | Gate resistors |
| R4 | - | 0402 | 10kΩ | 1 | NTC pullup |
| NTC1 | NCP18XH103F03RB | 0402 | 10kΩ | 1 | Thermistor |
| LED1 | - | 0603 | R/G | 1 | Status LED |
| J1 | - | - | 6-pin | 1 | Pogo connector |

### 6.2 Main Board Power Section BOM

| Ref | Part Number | Package | Value | Qty | Description |
|-----|-------------|---------|-------|-----|-------------|
| U1 | A6211GLJTR-T | SOT23-8L | - | 1 | Sync buck converter |
| U2 | INA219 | MSOP-10 | - | 1 | Power monitor |
| L1 | IHLP2525CZER100M01 | 2525 | 10µH | 1 | Power inductor |
| D1-D3 | SS34 | DO-214AB | 40V 3A | 3 | Schottky diodes |
| C1 | GRM31CR61E106K | 1206 | 10µF 25V | 2 | Input capacitor |
| C2 | GRM21BR61A226M | 0805 | 22µF 10V | 2 | Output capacitor |
| C3-C5 | - | 0402 | 0.1µF 25V | 3 | Bypass capacitors |
| C6 | - | Radial | 1F 10V | 1 | Supercapacitor |
| R1 | - | 0402 | 150kΩ 1% | 1 | TON resistor |
| R2 | - | 0402 | 100kΩ 1% | 1 | EN pullup |
| R3 | - | 0402 | 49.9kΩ 1% | 1 | FB divider top |
| R4 | - | 0402 | 10kΩ 1% | 1 | FB divider bottom |
| R5 | WSL2512R0100F | 2512 | 0.01Ω 1W | 1 | Current shunt |
| J1-J3 | - | - | 6-pin | 3 | Pogo receptacles |

---

## 7. Layout Guidelines

### 7.1 Buck Converter Layout (Critical)

```
┌─────────────────────────────────────────────────┐
│                                                 │
│  INPUT CAPS (C_IN)                             │
│  ┌───────┐                                      │
│  │ 10µF  │ ← Place within 3mm of VIN pin       │
│  └───┬───┘                                      │
│      │                                          │
│      │   ┌────────────┐                        │
│      └───┤   A6211    ├───┐                    │
│          └────────────┘   │                    │
│               SW pin ─────┤                    │
│                           │                    │
│               ┌───────────┴───────────┐        │
│               │      INDUCTOR         │        │
│               │       10µH            │        │
│               │  (short, wide trace)  │        │
│               └───────────┬───────────┘        │
│                           │                    │
│               ┌───────────┴───────────┐        │
│               │   OUTPUT CAPS (x2)    │        │
│               │  Place close to L1    │        │
│               └───────────────────────┘        │
│                                                 │
│  CRITICAL NOTES:                               │
│  • Keep SW node trace short and small (EMI)    │
│  • Wide ground plane under entire converter    │
│  • Multiple vias for thermal dissipation       │
│  • Bootstrap cap close to BOOT pin             │
│  • CS trace away from SW node                  │
│  • Thermal pad (pin 9) needs vias to GND       │
│                                                 │
└─────────────────────────────────────────────────┘
```

### 7.2 Power Plane Recommendations

| Layer | Purpose |
|-------|---------|
| Layer 1 (Top) | Signal + component placement |
| Layer 2 | Ground plane (unbroken under buck converter) |
| Layer 3 | Power plane (VBAT, 5V rails) |
| Layer 4 (Bottom) | Signal + additional components |

---

## 8. Connector System (Pogo Pins)

### 8.1 Overview

The battery pack uses a 6-pin pogo/spring pin connector system for hot-swap capability. This allows packs to be inserted and removed without tools while maintaining reliable electrical contact.

### 8.2 Connector Layout

```
BATTERY PACK SIDE (Male Pogo Pads)
┌─────────────────────────────────────┐
│                                     │
│   ●───●───●───●───●───●            │
│   1   2   3   4   5   6            │
│   P-  P-  NTC ID  P+  P+           │
│                                     │
│   Pad size: 2.0mm diameter         │
│   Pitch: 2.54mm (0.1")             │
│   Gold plated: ENIG recommended    │
│                                     │
└─────────────────────────────────────┘

MAIN BOARD SIDE (Female Pogo Receptacles)
┌─────────────────────────────────────┐
│                                     │
│   ◎───◎───◎───◎───◎───◎            │
│   1   2   3   4   5   6            │
│   P-  P-  NTC ID  P+  P+           │
│                                     │
│   Spring-loaded pogo pins          │
│   Travel: 1.0-1.5mm                │
│   Current rating: 2A per pin       │
│                                     │
└─────────────────────────────────────┘
```

### 8.3 Pin Assignment

| Pin | Signal | Description | Trace Width | Notes |
|-----|--------|-------------|-------------|-------|
| 1 | P- | Pack negative | 1.0mm min | Power return |
| 2 | P- | Pack negative | 1.0mm min | Paralleled for 4A total |
| 3 | NTC | Temperature | 0.2mm | 10kΩ NTC to GND |
| 4 | ID | Pack ID | 0.2mm | Resistor divider for pack type |
| 5 | P+ | Pack positive | 1.0mm min | Main power |
| 6 | P+ | Pack positive | 1.0mm min | Paralleled for 4A total |

### 8.4 Recommended Pogo Pin Parts

| Component | Part Number | Description |
|-----------|-------------|-------------|
| Pogo Pin (receptacle) | Mill-Max 0906-0-15-20-76-14-11-0 | Spring probe, 2A rated |
| Target Pad | Mill-Max 2199-0-00-80-00-00-03-0 | Gold plated target |
| Alternative | Coda Systems P50-series | Lower cost option |

### 8.5 Mating Mechanism

```
SIDE VIEW - PACK INSERTION

         Pack PCB
         ┌───────────────────┐
         │   ●   ●   ●   ●   │  ← Target pads (gold plated)
         └───────────────────┘
              ↓   ↓   ↓   ↓      Insertion direction
         ┌───────────────────┐
         │   ◎   ◎   ◎   ◎   │  ← Pogo pins (spring loaded)
         │   ║   ║   ║   ║   │     compress 1mm on contact
         └───────────────────┘
         Main Board

ALIGNMENT FEATURES:
- Add alignment posts/slots to enclosure
- 0.5mm tolerance on pad placement
- Chamfered entry on enclosure for guided insertion
```

### 8.6 ID Pin Resistor Values

The ID pin uses a resistor divider to identify pack type/capacity:

| Pack Type | R_ID to GND | Voltage at ID pin | Detected As |
|-----------|-------------|-------------------|-------------|
| Standard 2S 5Ah | 10kΩ | 1.65V | Type 0 |
| Extended 2S 10Ah | 22kΩ | 2.27V | Type 1 |
| High-drain 2S 5Ah | 33kΩ | 2.56V | Type 2 |
| Reserved | 47kΩ | 2.77V | Type 3 |

*Main board has 10kΩ pullup to 3.3V on ID pin*

### 8.7 NTC Connection

```
BATTERY PACK:
                    ┌─────────────┐
    NTC Pin (3) ────┤ 10kΩ NTC    ├──── GND (via P-)
                    │ 0402        │
                    └─────────────┘

MAIN BOARD:
                    ┌─────────────┐
    3.3V ───────────┤ 10kΩ        ├──── NTC Pin (3) ──→ ADC
                    │ pullup      │
                    └─────────────┘

Temperature Calculation:
- At 25°C: NTC = 10kΩ, Vout = 1.65V
- At 45°C: NTC ≈ 4.5kΩ, Vout = 1.02V
- At 0°C: NTC ≈ 27kΩ, Vout = 2.41V
```

---

## 9. Thermal Considerations

| Component | Power Dissipation | Notes |
|-----------|------------------|-------|
| A6211 | ~0.5W | Add thermal vias to ground plane |
| SS34 diodes | ~0.3W each | Adequate copper pour |
| FS8205A FETs | ~0.2W each | Spread across PCB |
| Shunt resistor | ~0.1W | 2512 package handles this |

**Important:** The A6211 has an exposed thermal pad (Pin 9) that MUST be soldered to the ground plane with multiple thermal vias for proper heat dissipation.

---

---

## 10. Centralized Charging System

### 10.1 Overview

Charging is handled by the main module rather than individual packs. This provides:
- Single USB-C input for all packs
- Intelligent charge management via Pi
- Temperature monitoring per pack
- Sequential charging (one pack at a time)

### 10.2 System Architecture

```
                                  MAIN MODULE
┌─────────────────────────────────────────────────────────────────────────┐
│                                                                         │
│   USB-C PD          CHARGE                    PACK SELECT              │
│   INPUT             CONTROLLER                (one at a time)           │
│                                                                         │
│  ┌───────┐      ┌─────────────┐         ┌──────────────────┐           │
│  │USB-C  │      │  BQ25895    │         │   CHARGE MUX     │           │
│  │ PD    │──────│  or         │─────────│   (P-FETs)       │           │
│  │       │      │  BQ24195    │         │                  │           │
│  └───────┘      │             │         │  ┌──┐ ┌──┐ ┌──┐ │           │
│     │           │  CC/CV      │         │  │P1│ │P2│ │P3│ │           │
│     │           │  8.4V 2A    │         │  └┬─┘ └┬─┘ └┬─┘ │           │
│  ┌──┴──┐        └─────────────┘         └───┼────┼────┼───┘           │
│  │STUSB│                                    │    │    │               │
│  │4500 │        DISCHARGE PATH              │    │    │               │
│  │(PD) │        (OR-ing diodes)             │    │    │               │
│  └─────┘        ───────────────             │    │    │               │
│                      │                      │    │    │               │
│                      ▼                      ▼    ▼    ▼               │
│                 ┌─────────┐            ┌─────────────────┐            │
│                 │  BUCK   │            │  PACK CONNECTORS │            │
│                 │  A6211  │            │  (6-pin pogo)    │            │
│                 │  5V 2A  │            └─────────────────┘            │
│                 └────┬────┘                  │    │    │               │
│                      │                       │    │    │               │
│                      ▼                       ▼    ▼    ▼               │
│                   5V OUT              PACK 1  PACK 2  PACK 3          │
│                   TO PI                                               │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

### 10.3 Charge Controller Circuit (BQ25895)

```
                         VBUS (5V-20V from USB-C PD)
                              │
                         ┌────┴────┐
                         │ 10µF    │
                         │ 25V     │
                         └────┬────┘
                              │
         ┌────────────────────┴────────────────────────────────────┐
         │                                                         │
         │  VBUS ─┤1                                          24├─ PMID
         │        │                                              │
         │   D+ ──┤2        BQ25895                           23├─ SYS ──→ System
         │        │         (QFN-24)                             │
         │   D- ──┤3                                          22├─ BTST
         │        │                                              │    │
         │  ILIM ─┤4                                          21├────┼─[0.1µF]
         │    │   │                                              │    │
         │  [Rilim]                                           20├─ SW ┘
         │    │   │                                              │    │
         │   GND  │                                              │   [L]
         │        │                                              │  2.2µH
         │   TS ──┤5   (temp sense from pack NTC)            19├────┴────┐
         │        │                                              │         │
         │  QON ──┤6                                          18├─ BAT ───┴──→ CHG MUX
         │        │                                              │
         │  CE ───┤7   (charge enable, GPIO)                 17├─ REGN
         │        │                                              │    │
         │  SDA ──┤8                                          16├────┴─[1µF]
         │        │                                              │
         │  SCL ──┤9                                          15├─ STAT ──→ LED
         │        │                                              │
         │  INT ──┤10                                         14├─ OTG
         │        │                                              │
         │  PSEL ─┤11                                         13├─ NC
         │        │                                              │
         │  GND ──┤12                                         PAD├─ GND
         │        │                                              │
         └────────┴──────────────────────────────────────────────┘
```

**Key BQ25895 Connections:**

| Pin | Name | Connection | Notes |
|-----|------|------------|-------|
| 1 | VBUS | USB-C VBUS | 5-14V input |
| 2,3 | D+/D- | USB data lines | For BC1.2 detection |
| 4 | ILIM | 1kΩ to GND | Sets 2A input limit |
| 5 | TS | Pack NTC (via mux) | Temperature sensing |
| 7 | CE | Pi GPIO | Charge enable |
| 8,9 | SDA/SCL | Pi I2C | Configuration |
| 15 | STAT | LED/GPIO | Charge status |
| 18 | BAT | Charge MUX input | 8.4V output |
| 20 | SW | Inductor | Switching node |

### 10.4 Charge MUX Circuit

The charge MUX uses P-channel MOSFETs to route charge current to one pack at a time:

```
                    CHARGE OUTPUT (from BQ25895 BAT pin)
                              │
              ┌───────────────┼───────────────┐
              │               │               │
         ┌────┴────┐     ┌────┴────┐     ┌────┴────┐
         │ Si2301  │     │ Si2301  │     │ Si2301  │
         │ P-FET   │     │ P-FET   │     │ P-FET   │
         │         │     │         │     │         │
         │ S ── D  │     │ S ── D  │     │ S ── D  │
         │    │    │     │    │    │     │    │    │
         └────┼────┘     └────┼────┘     └────┼────┘
              │G              │G              │G
              │               │               │
         ┌────┴────┐     ┌────┴────┐     ┌────┴────┐
         │ 10kΩ    │     │ 10kΩ    │     │ 10kΩ    │
         │ pullup  │     │ pullup  │     │ pullup  │
         └────┬────┘     └────┬────┘     └────┬────┘
              │               │               │
              │               │               │
         GPIO_CHG1       GPIO_CHG2       GPIO_CHG3
         (from Pi)       (from Pi)       (from Pi)
         LOW=charge      LOW=charge      LOW=charge
              │               │               │
              ▼               ▼               ▼
         PACK 1 P+       PACK 2 P+       PACK 3 P+
```

**MUX Operation:**
- All GPIOs HIGH (default): No charging, FETs OFF
- GPIO_CHG1 LOW: Pack 1 charges, others isolated
- Only ONE GPIO should be LOW at a time

### 10.5 USB-C Power Delivery

```
┌─────────────────────────────────────────────────────────────────┐
│                     USB-C PD CIRCUIT                            │
│                                                                 │
│   USB-C Connector          STUSB4500              To BQ25895   │
│   ┌─────────────┐         ┌─────────────┐                      │
│   │             │         │             │                      │
│   │  VBUS ──────┼─────────┤ VBUS    VIN ├────────→ VBUS       │
│   │             │         │             │                      │
│   │  CC1 ───────┼─────────┤ CC1         │                      │
│   │             │         │             │                      │
│   │  CC2 ───────┼─────────┤ CC2     SDA ├────────→ Pi I2C     │
│   │             │         │             │                      │
│   │  D+ ────────┼─────────┤         SCL ├────────→ Pi I2C     │
│   │             │         │             │                      │
│   │  D- ────────┼─────────┤        GPIO ├────────→ Pi GPIO    │
│   │             │         │             │                      │
│   │  GND ───────┼─────────┤ GND         │                      │
│   │             │         │             │                      │
│   └─────────────┘         └─────────────┘                      │
│                                                                 │
│   Negotiated Power Profiles:                                   │
│   - 5V @ 3A  (15W) - Default                                   │
│   - 9V @ 2A  (18W) - Fast charge                               │
│   - 12V @ 1.5A (18W) - Alternative                             │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 10.6 Complete Main Board Power Schematic

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                     MAIN BOARD - POWER + CHARGING                            │
│                                                                              │
│   ┌─────────────┐                                                           │
│   │   USB-C     │                                                           │
│   │  CONNECTOR  │                                                           │
│   └──────┬──────┘                                                           │
│          │                                                                   │
│     ┌────┴────┐      ┌─────────────────────────────────────┐                │
│     │ STUSB   │      │         CHARGE CONTROLLER           │                │
│     │ 4500    │      │            BQ25895                  │                │
│     │ (USB PD)│      │                                     │                │
│     │         │      │  VIN ──────── VBUS (from USB-C)    │                │
│     │  VBUS───┼──────│  SYS ──────── System power out     │                │
│     │         │      │  BAT ──────── To charge mux        │                │
│     │  I2C────┼──────│  I2C ──────── To Pi (config)       │                │
│     │         │      │  STAT ─────── LED/GPIO             │                │
│     └─────────┘      │  CE ────────── Charge enable       │                │
│                      │                                     │                │
│                      └──────────────┬──────────────────────┘                │
│                                     │ CHARGE OUT (8.4V CC/CV)               │
│                                     │                                        │
│     ┌───────────────────────────────┴───────────────────────────────┐       │
│     │                     CHARGE MUX (P-FET switches)               │       │
│     │                                                               │       │
│     │    Pack 1 Select      Pack 2 Select      Pack 3 Select       │       │
│     │    GPIO from Pi       GPIO from Pi       GPIO from Pi        │       │
│     │         │                  │                  │               │       │
│     │    ┌────┴────┐        ┌────┴────┐        ┌────┴────┐         │       │
│     │    │ Si2301  │        │ Si2301  │        │ Si2301  │         │       │
│     │    │ P-FET   │        │ P-FET   │        │ P-FET   │         │       │
│     │    └────┬────┘        └────┬────┘        └────┬────┘         │       │
│     │         │                  │                  │               │       │
│     └─────────┼──────────────────┼──────────────────┼───────────────┘       │
│               │                  │                  │                        │
│  ═════════════╪══════════════════╪══════════════════╪════════════════       │
│               │                  │                  │                        │
│          PACK 1 CHG         PACK 2 CHG         PACK 3 CHG                   │
│               │                  │                  │                        │
│  ┌────────────┴──────────────────┴──────────────────┴────────────┐          │
│  │                      PACK CONNECTORS                          │          │
│  │                                                               │          │
│  │   ┌──────────┐       ┌──────────┐       ┌──────────┐        │          │
│  │   │  PACK 1  │       │  PACK 2  │       │  PACK 3  │        │          │
│  │   │  6-pin   │       │  6-pin   │       │  6-pin   │        │          │
│  │   │          │       │          │       │          │        │          │
│  │   │ P+ P- NTC│       │ P+ P- NTC│       │ P+ P- NTC│        │          │
│  │   └────┬─────┘       └────┬─────┘       └────┬─────┘        │          │
│  │        │                  │                  │               │          │
│  └────────┼──────────────────┼──────────────────┼───────────────┘          │
│           │                  │                  │                           │
│      ┌────┴────┐        ┌────┴────┐        ┌────┴────┐                     │
│      │  SS34   │        │  SS34   │        │  SS34   │  DISCHARGE          │
│      │ (diode) │        │ (diode) │        │ (diode) │  OR-ing             │
│      └────┬────┘        └────┬────┘        └────┬────┘                     │
│           │                  │                  │                           │
│           └──────────────────┴──────────────────┴───────┐                  │
│                                                         │                   │
│                              VBAT BUS ──────────────────┴                   │
│                                                         │                   │
│                                                    ┌────┴────┐              │
│                                                    │  BUCK   │              │
│                                                    │  A6211  │              │
│                                                    └────┬────┘              │
│                                                         │                   │
│                                                    5V OUTPUT                │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 10.7 NTC Multiplexing for Temperature Sensing

To monitor the temperature of the pack being charged:

```
                    Pack 1 NTC    Pack 2 NTC    Pack 3 NTC
                         │             │             │
                    ┌────┴────┐   ┌────┴────┐   ┌────┴────┐
                    │ 10kΩ    │   │ 10kΩ    │   │ 10kΩ    │
                    │ NTC     │   │ NTC     │   │ NTC     │
                    └────┬────┘   └────┬────┘   └────┬────┘
                         │             │             │
    Pack detect ─────────┴─────────────┴─────────────┴─────────→ Pi ADC
    (presence)                                                   (channels 1-3)
                         │             │             │
                    ┌────┴────┐   ┌────┴────┐   ┌────┴────┐
                    │ 4053    │   │         │   │         │
                    │ Analog  │   │  (MUX)  │   │         │
                    │ MUX     │   │         │   │         │
                    └────┬────┘   └─────────┘   └─────────┘
                         │
                         └──────────────────────────────────────→ BQ25895 TS pin

    Alternative: Read all NTCs via Pi ADC, disable charging if selected
                 pack is out of temperature range
```

### 10.8 Charge Sequence (Software)

```python
# Pseudo-code for charge management

def charge_manager():
    while True:
        # 1. Detect connected packs
        packs = detect_packs()  # Read ID pins via ADC

        # 2. Read all pack voltages and temperatures
        for pack in packs:
            pack.voltage = read_voltage(pack)
            pack.temp = read_ntc(pack)
            pack.needs_charge = pack.voltage < 8.2  # Not full

        # 3. Select pack to charge (lowest voltage first)
        pack_to_charge = min(packs, key=lambda p: p.voltage)

        # 4. Safety checks
        if pack_to_charge.temp < 5 or pack_to_charge.temp > 45:
            disable_charging()
            continue

        # 5. Enable charging for selected pack
        select_charge_mux(pack_to_charge.id)  # GPIO LOW

        # 6. Configure BQ25895 via I2C
        bq25895.set_charge_voltage(8400)  # 8.4V
        bq25895.set_charge_current(2000)  # 2A
        bq25895.enable_charging()

        # 7. Monitor charging
        while charging:
            if bq25895.charge_complete():
                break
            if read_ntc(pack_to_charge) > 45:
                disable_charging()
                break
            sleep(1000)

        # 8. Move to next pack
        disable_charge_mux()
```

### 10.9 Charging BOM

| Ref | Part Number | Package | Value | Qty | Description |
|-----|-------------|---------|-------|-----|-------------|
| U3 | BQ25895RTWR | QFN-24 | - | 1 | USB battery charger IC |
| U4 | STUSB4500QTR | QFN-24 | - | 1 | USB-C PD controller |
| Q4 | Si2301CDS | SOT-23 | - | 3 | P-FET charge switch |
| L2 | SRP4020TA-2R2M | 4x4mm | 2.2µH | 1 | Charger inductor |
| J2 | USB4110-GF-A | - | - | 1 | USB-C receptacle |
| C10 | - | 0805 | 10µF 25V | 2 | VBUS capacitor |
| C11 | - | 0402 | 0.1µF | 1 | Bootstrap cap |
| C12 | - | 0402 | 1µF | 1 | REGN cap |
| R10 | - | 0402 | 1kΩ | 1 | ILIM resistor |
| R11-R13 | - | 0402 | 10kΩ | 3 | Gate pullups |
| LED1 | - | 0603 | Red | 1 | Charge status |

### 10.10 Pi GPIO Assignments (Charging)

| GPIO | Function | Direction | Notes |
|------|----------|-----------|-------|
| GPIO2 | I2C SDA | Bidir | BQ25895 + STUSB4500 |
| GPIO3 | I2C SCL | Output | BQ25895 + STUSB4500 |
| GPIO17 | CHG_EN | Output | BQ25895 CE pin |
| GPIO27 | CHG_STAT | Input | BQ25895 status |
| GPIO22 | CHG_MUX1 | Output | Pack 1 charge select |
| GPIO23 | CHG_MUX2 | Output | Pack 2 charge select |
| GPIO24 | CHG_MUX3 | Output | Pack 3 charge select |
| ADC0 | NTC1 | Analog | Pack 1 temperature |
| ADC1 | NTC2 | Analog | Pack 2 temperature |
| ADC2 | NTC3 | Analog | Pack 3 temperature |
| ADC3 | ID1 | Analog | Pack 1 identification |
| ADC4 | ID2 | Analog | Pack 2 identification |
| ADC5 | ID3 | Analog | Pack 3 identification |

*Note: Pi doesn't have native ADC - use MCP3008 or ADS1115 for analog inputs*

---

## 11. BMS Wiring Detail

### 11.1 Complete 2S BMS Schematic (S8254AA + FS8205A)

```
                            CELL 2 (+)
                               │
    B+ ────────────────────────┴────────────────────────────── P+
     │                                                     (Pack Output+)
     │
    [R1 100Ω 0402]
     │
     ├─────────────────────────────────────────┐
     │                                         │
     │              S8254AA                    │
     │         ┌──────────────┐               │
     │         │ 1  VDD       │───────────────┘
     │         │              │
     │         │ 2  VM ───────┼─────────────────── BALANCE MID
     │         │              │                    (between cells)
     │         │ 3  VSS ──────┼─────────────────── B-
     │         │              │
     │         │ 4  OC ───────┼──────────┐
     │         │              │          │
     │         │ 5  DO ───────┼───┐      │
     │         │              │   │      │
     │         │ 6  CO ───────┼───┼──┐   │
     │         └──────────────┘   │  │   │
     │                            │  │   │
     │                           [R2]│   │
     │                          1kΩ │[R3]│
     │                            │  │1kΩ│
     │                            │  │   │
     │         FS8205A #1         │  │   │       FS8205A #2
     │        (CHARGE FET)        │  │   │      (DISCHARGE FET)
     │       ┌────────────┐       │  │   │     ┌────────────┐
     │       │            │       │  │   │     │            │
     │       │  1,2,6,7,8 │ DRAIN │  │   │     │  1,2,6,7,8 │ DRAIN
     │       │      │     │───────┼──┼───┼─────│      │     │
     │       │      │     │       │  │   │     │      │     │
     │       │    ──┴──   │       │  │   │     │    ──┴──   │
     │       │   │     │  │       │  │   │     │   │     │  │
     │       │   │FET1 │  │       │  │   │     │   │FET1 │  │
     │       │   │     │  │       │  │   │     │   │     │  │
     │       │    ──┬──   │       │  │   │     │    ──┬──   │
     │       │      │     │       │  │   │     │      │     │
     │       │   3  S1    │───────┘  │   │     │   3  S1    │──────┐
     │       │            │          │   │     │            │      │
     │       │   4  GATE  │──────────┘   │     │   4  GATE  │──────┼──┐
     │       │            │              │     │            │      │  │
     │       │   5  S2    │──────────────┼─────│   5  S2    │──┐   │  │
     │       │            │              │     │            │  │   │  │
     │       └────────────┘              │     └────────────┘  │   │  │
     │                                   │                     │   │  │
     │                                   │                     │   │  │
     │                                   └─────────────────────┼───┘  │
     │                                                         │      │
     │                                                    [R_CS]      │
     │                                                    0.01Ω      │
     │                                                    2512       │
     │                                                         │      │
    B- ────────────────────────────────────────────────────────┴──────┘
     │                                                            │
     │                                                            │
  CELL 1 (-)                                                     P-
                                                           (Pack Output-)
```

### 11.2 S8254AA Pin Functions

| Pin | Name | Function | Connection |
|-----|------|----------|------------|
| 1 | VDD | Power supply | B+ via 100Ω |
| 2 | VM | Cell mid-point | Between Cell 1 and Cell 2 |
| 3 | VSS | Ground reference | B- (Cell 1 negative) |
| 4 | OC | Overcurrent sense | Top of sense resistor |
| 5 | DO | Discharge control | FS8205A #2 gate via 1kΩ |
| 6 | CO | Charge control | FS8205A #1 gate via 1kΩ |

### 11.3 Protection Thresholds

| Protection | Threshold | Action |
|------------|-----------|--------|
| Overcharge | 4.25V/cell | CO goes LOW, blocks charge |
| Over-discharge | 2.5V/cell | DO goes LOW, blocks discharge |
| Overcurrent | ~8A | DO goes LOW (based on R_CS) |
| Short circuit | >25A | Immediate DO shutdown |

### 11.4 FET Configuration (Back-to-Back)

```
    B- ────┬──[R_sense 0.01Ω]──┬────────────────────── P-
           │                   │
           │    FS8205A #1     │    FS8205A #2
           │   (Charge FET)    │   (Discharge FET)
           │   ┌─────────┐     │   ┌─────────┐
           │   │  D──D   │     │   │  D──D   │
           └───┤         ├─────┴───┤         ├────┘
               │  S──S   │         │  S──S   │
               └────┬────┘         └────┬────┘
                    │                   │
                  GATE                GATE
                    │                   │
                 CO pin              DO pin
              (from S8254)        (from S8254)
              via 1kΩ             via 1kΩ
```

### 11.5 BMS Component Values

| Ref | Value | Package | Purpose |
|-----|-------|---------|---------|
| R1 | 100Ω | 0402 | VDD current limit |
| R2 | 1kΩ | 0402 | CO gate resistor |
| R3 | 1kΩ | 0402 | DO gate resistor |
| R_CS | 0.01Ω (10mΩ) | 2512 | Current sense |
| C1 | 0.1µF | 0402 | VDD bypass (optional) |

---

## 12. Complete System BOM

### 12.1 Battery Pack PCB

| Ref | Part Number | Package | Value | Qty | Description |
|-----|-------------|---------|-------|-----|-------------|
| U1 | S8254AA | TSSOP-8 | - | 1 | 2S Protection IC |
| U2 | FS8205A | TSSOP-8 | - | 1 | Dual N-FET (charge) |
| U3 | FS8205A | TSSOP-8 | - | 1 | Dual N-FET (discharge) |
| R1 | - | 0402 | 100Ω | 1 | VDD resistor |
| R2,R3 | - | 0402 | 1kΩ | 2 | Gate resistors |
| R4 | - | 0402 | 10kΩ | 1 | NTC pullup |
| R_CS | WSL2512R0100F | 2512 | 0.01Ω 1W | 1 | Current sense |
| R_ID | - | 0402 | varies | 1 | Pack ID resistor |
| NTC1 | NCP18XH103F03RB | 0402 | 10kΩ | 1 | Thermistor |
| C1 | - | 0402 | 0.1µF | 1 | Bypass cap |
| LED1 | - | 0603 | R/G | 1 | Status LED |
| J1 | Mill-Max target | - | 6-pad | 1 | Pogo pads |

### 12.2 Main Board (Power Section)

| Ref | Part Number | Package | Value | Qty | Description |
|-----|-------------|---------|-------|-----|-------------|
| U1 | A6211GLJTR-T | SOT23-8L | - | 1 | Buck converter |
| U2 | INA219BIDCNR | MSOP-10 | - | 1 | Power monitor |
| U3 | BQ25895RTWR | QFN-24 | - | 1 | USB charger IC |
| U4 | STUSB4500QTR | QFN-24 | - | 1 | USB-C PD controller |
| U5 | MCP3008 | DIP-16/SOIC | - | 1 | ADC for NTC/ID |
| L1 | SRP7028A-4R7M | 7x7mm | 4.7µH | 1 | Buck inductor |
| L2 | SRP4020TA-2R2M | 4x4mm | 2.2µH | 1 | Charger inductor |
| D1-D3 | SS34 | DO-214AB | 40V 3A | 3 | OR-ing diodes |
| Q1-Q3 | Si2301CDS | SOT-23 | - | 3 | P-FET charge mux |
| J1-J3 | Mill-Max 0906 | - | 6-pin | 3 | Pogo receptacles |
| J4 | USB4110-GF-A | - | - | 1 | USB-C connector |
| C1-C2 | GRM31CR61E106K | 1206 | 10µF 25V | 4 | Input caps |
| C3-C5 | GRM21BR61A226M | 0805 | 22µF 10V | 3 | Output caps |
| C6-C10 | - | 0402 | 0.1µF | 5 | Bypass caps |
| C11 | - | Radial | 1F 10V | 1 | Supercap |
| R1 | - | 0402 | 150kΩ 1% | 1 | TON resistor |
| R2 | - | 0402 | 100kΩ 1% | 1 | EN pullup |
| R3 | - | 0402 | 49.9kΩ 1% | 1 | FB top |
| R4 | - | 0402 | 10kΩ 1% | 1 | FB bottom |
| R5 | WSL2512R0100F | 2512 | 0.01Ω 1W | 1 | Current shunt |
| R6 | - | 0402 | 1kΩ | 1 | ILIM resistor |
| R7-R9 | - | 0402 | 10kΩ | 3 | Gate pullups |

---

*Document Version: 1.2 (Charging System Added)*
*Project: Familiar Firmware Modular Battery System*
