# Modular Hot-Swappable Battery System

## Overview

The Familiar device uses a modular hot-swappable battery system that allows battery replacement without powering down the device. This ensures continuous operation during conventions and events.

## Design Goals

- **Zero-downtime battery swap**: Device continues operating during battery replacement
- **Modular battery packs**: Standardized, user-replaceable battery modules
- **Safe operation**: Protection against overcurrent, overvoltage, undervoltage, and reverse polarity
- **Status monitoring**: Real-time battery level and health reporting

## System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        MAINBOARD                                 │
│  ┌─────────────┐    ┌─────────────┐    ┌──────────────────────┐ │
│  │   Buffer    │    │   Power     │    │    Raspberry Pi      │ │
│  │  Capacitor  │───▶│   Manager   │───▶│    5V @ 3A           │ │
│  │  (Backup)   │    │   Circuit   │    │                      │ │
│  └─────────────┘    └──────┬──────┘    └──────────────────────┘ │
│                            │                                     │
│         ┌──────────────────┼──────────────────┐                 │
│         │                  │                  │                 │
│         ▼                  ▼                  ▼                 │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐         │
│  │  Battery    │    │  Battery    │    │  Battery    │         │
│  │  Slot A     │    │  Slot B     │    │  Slot C     │         │
│  │  (Primary)  │    │ (Secondary) │    │ (Optional)  │         │
│  └──────┬──────┘    └──────┬──────┘    └──────┬──────┘         │
└─────────┼──────────────────┼──────────────────┼─────────────────┘
          │                  │                  │
          ▼                  ▼                  ▼
   ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
   │   Battery   │    │   Battery   │    │   Battery   │
   │   Module    │    │   Module    │    │   Module    │
   │   18650x2   │    │   18650x2   │    │   18650x2   │
   └─────────────┘    └─────────────┘    └─────────────┘
```

## Components

### 1. Buffer Capacitor Bank

Provides power during battery hot-swap (holds power for ~5-10 seconds).

| Component | Value | Purpose |
|-----------|-------|---------|
| Supercapacitors | 2x 10F 2.7V in series | Backup power during swap |
| Balancing resistors | 2x 100Ω | Voltage equalization |
| Charge limiting resistor | 10Ω 2W | Inrush current limiting |

**Hold-up time calculation:**
- Pi 4 consumption: ~5W (1A @ 5V)
- Capacitor energy: ½ × C × V² = ½ × 5F × 5.4² = 72.9J
- Usable energy (5.4V → 4.5V): ~40J
- Hold-up time: 40J / 5W = **8 seconds**

### 2. Battery Module Specification

Each battery module is a self-contained unit with:

| Specification | Value |
|--------------|-------|
| Cell configuration | 2S1P (2x 18650 in series) |
| Nominal voltage | 7.4V |
| Capacity | 2500-3500mAh (cell dependent) |
| Max discharge | 10A continuous |
| Connector | XT30 (rated 30A) |
| Protection | Integrated BMS per module |

#### Module BMS Features
- Overcurrent protection: 15A cutoff
- Overvoltage protection: 4.25V/cell
- Undervoltage protection: 2.8V/cell
- Short circuit protection
- Temperature monitoring (NTC)

### 3. Power Manager Circuit

Central power management IC handles:

| Function | Implementation |
|----------|----------------|
| Battery selection | Priority-based OR-ing with ideal diodes |
| Voltage regulation | Synchronous buck converter 7.4V → 5.1V |
| Current monitoring | INA219 or similar (I²C) |
| Fuel gauge | Per-slot coulomb counting |
| Charging | Optional pass-through charging |

#### Recommended ICs

| Function | Part Number | Notes |
|----------|-------------|-------|
| Ideal diode controller | LTC4357 | Per battery slot |
| Buck converter | TPS62913 | 5V/3A, 95% efficiency |
| Fuel gauge | MAX17048 | I²C, per-slot |
| Charge controller | BQ25895 | Optional, for charging |

### 4. Battery Slot Interface

Each slot provides:

```
       Battery Module Connector
    ┌─────────────────────────────┐
    │  ┌───┐ ┌───┐ ┌───┐ ┌───┐   │
    │  │B+ │ │NTC│ │ID │ │B- │   │
    │  └───┘ └───┘ └───┘ └───┘   │
    └─────────────────────────────┘
         │     │     │     │
         │     │     │     └── Battery negative
         │     │     └──────── Module ID (1-wire or resistor)
         │     └────────────── Temperature sense (10K NTC)
         └──────────────────── Battery positive (7.4V nominal)
```

| Pin | Function | Description |
|-----|----------|-------------|
| B+  | Power    | Battery positive, fused at 15A |
| B-  | Ground   | Battery negative / common ground |
| NTC | Temp     | 10K NTC thermistor for temperature |
| ID  | Identify | Module identification / presence detect |

## Hot-Swap Procedure

### Automatic Failover

1. **Normal operation**: Power drawn from highest-charge battery
2. **Battery removal detected**:
   - ID pin goes open
   - Ideal diode isolates slot within 1µs
   - Load transfers to next battery / buffer
3. **New battery inserted**:
   - ID pin detected
   - Voltage verified (within range)
   - Soft-start connects battery to bus
4. **Priority recalculation**: System rebalances load

### User Procedure

1. Check battery indicator (LED or app) - ensure backup battery present
2. Press release latch on battery module
3. Remove depleted battery
4. Insert fresh battery (keyed connector prevents reverse insertion)
5. Verify indicator shows battery detected

## Safety Features

### Hardware Protection

| Protection | Method | Response |
|------------|--------|----------|
| Reverse polarity | Keyed connector + ideal diode | Prevents connection |
| Overcurrent | Fuse + electronic limit | 15A cutoff |
| Overvoltage | Zener clamp + disconnect | Isolate slot |
| Undervoltage | BMS cutoff | Disconnect at 2.8V/cell |
| Short circuit | Electronic fuse | <10µs response |
| Overtemperature | NTC monitoring | Disconnect at 60°C |

### Software Monitoring

The firmware monitors via I²C:
- Individual cell voltages
- Pack current (charge/discharge)
- Temperature per slot
- State of charge (%)
- Cycle count
- Health estimation

## Connector Specifications

### Main Battery Connector: XT30

| Parameter | Value |
|-----------|-------|
| Current rating | 30A continuous |
| Voltage rating | 500V |
| Contact resistance | <1mΩ |
| Mating cycles | 1000+ |
| Keyed | Yes (prevents reverse) |

### Alternative: Custom Pogo Pin

For tool-less insertion:
- Spring-loaded pogo pins
- Magnetic alignment
- 4-pin configuration (B+, B-, NTC, ID)

## Battery Module Design

### Enclosure

- Material: ABS or nylon (flame retardant)
- Dimensions: 75mm × 45mm × 22mm
- Weight: ~100g with cells
- Features:
  - Thumb grip ridges
  - LED status indicator (optional)
  - Vent holes for thermal management

### Internal Layout

```
┌─────────────────────────────────┐
│  ┌─────────┐    ┌─────────┐    │
│  │         │    │         │    │
│  │ 18650   │────│ 18650   │    │
│  │ Cell 1  │    │ Cell 2  │    │
│  │         │    │         │    │
│  └────┬────┘    └────┬────┘    │
│       │              │         │
│  ┌────┴──────────────┴────┐    │
│  │         BMS            │    │
│  │   Protection Circuit   │    │
│  └───────────┬────────────┘    │
│              │                 │
│         ┌────┴────┐            │
│         │ XT30    │            │
│         │Connector│            │
└─────────┴─────────┴────────────┘
```

## Charging System (Optional)

If pass-through charging is desired:

### Charging Specifications

| Parameter | Value |
|-----------|-------|
| Input voltage | 12V DC or USB-C PD |
| Charge current | 1A per slot (configurable) |
| Charge method | CC-CV |
| Balance charging | Yes, per-cell |
| Charge time | ~3 hours (empty to full) |

### Charging IC Selection

| Option | Part | Features |
|--------|------|----------|
| USB-C PD | BQ25895 | PD negotiation, 5A max |
| 12V barrel | TP4056 (x2) | Simple, cheap |
| Smart | BQ40Z50 | Full fuel gauge + charging |

## Bill of Materials (Per Mainboard)

| Qty | Component | Part Number | Notes |
|-----|-----------|-------------|-------|
| 2 | Supercapacitor 10F 2.7V | EECS0HD106 | Buffer bank |
| 3 | Ideal diode controller | LTC4357 | One per slot |
| 1 | Buck converter module | TPS62913 | 5V/3A output |
| 3 | Fuel gauge IC | MAX17048 | One per slot |
| 3 | XT30 female connector | XT30U-F | Battery slots |
| 3 | 15A fuse | Littelfuse 0451015 | Per slot protection |
| 3 | NTC thermistor | 10K 3950 | Temperature sense |

## Firmware Integration

### I²C Address Map

| Device | Address | Function |
|--------|---------|----------|
| Fuel Gauge Slot A | 0x36 | State of charge |
| Fuel Gauge Slot B | 0x37 | State of charge |
| Fuel Gauge Slot C | 0x38 | State of charge |
| Power Monitor | 0x40 | Current/voltage |

### API Endpoints

The battery status is exposed via the Familiar API:

```
GET /api/battery/status
{
  "slots": [
    {
      "slot": "A",
      "present": true,
      "voltage": 7.82,
      "current": 1.2,
      "stateOfCharge": 85,
      "temperature": 28.5,
      "health": 98,
      "charging": false
    },
    ...
  ],
  "buffer": {
    "voltage": 5.2,
    "chargePercent": 95
  },
  "systemPower": {
    "inputVoltage": 7.82,
    "outputVoltage": 5.1,
    "outputCurrent": 2.1,
    "powerWatts": 10.7
  }
}
```

## Future Considerations

- **USB-C PD integration**: Power Delivery for fast charging
- **Wireless charging**: Qi-compatible charging pad
- **Smart battery modules**: Bluetooth-enabled for individual monitoring
- **Solar input**: MPPT charging from portable panels
