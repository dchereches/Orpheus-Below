/*
 * ORPHEUS BELOW — ESP32 Piezo Tap/Punch + Optional BMP280 Breath Controller
 * Streams 115200-baud intensity pulses directly into Unity's SensorPulseInput.cs (COM5):
 *
 * WIRING:
 *   1) PIEZO DISC (Primary / Instant Analog Impact):
 *      - Red wire (center brass/ceramic) -> GPIO 34
 *      - Black wire (outer rim)          -> GND
 *      - 1 MOhm resistor in parallel between GPIO 34 and GND (bleeds DC charge & protects ADC!)
 *
 *   2) OPTIONAL REPLACEMENT BMP280 (Breath Alternative Input):
 *      - VCC -> 3V3, GND -> GND, SDA -> GPIO 22, SCL -> GPIO 21
 *      - Auto-detected at boot; if absent, runs pure Piezo mode with zero lag!
 *
 * SERIAL PROTOCOL (parsed automatically by Assets/OrpheusBelow/Scripts/Input/SensorPulseInput.cs):
 *   - "P,<0.000..1.000>" : Triggered attack pulse! (< 0.60 = Soft Lyre Note, >= 0.60 = HARD BOOM!)
 *   - "0.0,<0.000..1.000>" : 30 Hz live meter telemetry for the HUD bar
 */

#include <Arduino.h>
#include <Wire.h>

static const int PIN_PIEZO_ADC = 34;

// Tunable Piezo thresholds (12-bit ESP32 ADC: 0..4095)
static const int PIEZO_NOISE_FLOOR = 140;   // Ignore tiny desk vibration
static const int PIEZO_HARD_BOOM   = 1650;  // >= 0.60 normalized -> HARD ORPHIC BOOM!
static const int PIEZO_MAX_CLIP    = 3100;  // 1.00 normalized max slam

static unsigned long lastTelemetryMs = 0;
static unsigned long cooldownUntilMs = 0;

static bool capturingPeak = false;
static unsigned long peakWindowStartMs = 0;
static int peakAdc = 0;

// Optional BMP280 I2C auto-detected state (zero extra libraries needed!)
static uint8_t bmpAddr = 0;
static float bmpBaselineRaw = 0.0f;
static float bmpBreathNorm = 0.0f;
static bool bmpExhaleArmed = true;

static bool probeBmp280(uint8_t addr) {
  Wire.beginTransmission(addr);
  Wire.write(0xD0);
  if (Wire.endTransmission(false) != 0) return false;
  if (Wire.requestFrom(addr, (uint8_t)1) != 1) return false;
  uint8_t id = Wire.read();
  if (id != 0x58 && id != 0x60) return false;

  // Configure normal mode, 16x pressure oversampling, fast 0.5ms standby
  Wire.beginTransmission(addr);
  Wire.write(0xF4);
  Wire.write(0x57);
  Wire.endTransmission();
  return true;
}

static int32_t readBmpRawPressure(uint8_t addr) {
  Wire.beginTransmission(addr);
  Wire.write(0xF7);
  if (Wire.endTransmission(false) != 0) return 0;
  if (Wire.requestFrom(addr, (uint8_t)3) != 3) return 0;
  int32_t p = ((int32_t)Wire.read() << 12) | ((int32_t)Wire.read() << 4) | (Wire.read() >> 4);
  return p;
}

float adcToNormalized(int raw) {
  if (raw <= PIEZO_NOISE_FLOOR) return 0.0f;
  if (raw < PIEZO_HARD_BOOM) {
    float t = (float)(raw - PIEZO_NOISE_FLOOR) / (float)(PIEZO_HARD_BOOM - PIEZO_NOISE_FLOOR);
    return 0.16f + t * 0.43f; // Maps soft taps smoothly to [0.16 .. 0.59]
  } else {
    float t = (float)(raw - PIEZO_HARD_BOOM) / (float)(PIEZO_MAX_CLIP - PIEZO_HARD_BOOM);
    if (t > 1.0f) t = 1.0f;
    return 0.60f + t * 0.40f; // Maps hard punches/slams to [0.60 .. 1.00] BOOM!
  }
}

void setup() {
  Serial.begin(115200);
  analogReadResolution(12);
  analogSetPinAttenuation(PIN_PIEZO_ADC, ADC_11db); // Full ~0..3.3V range

  Wire.begin(22, 21);
  if (probeBmp280(0x76)) bmpAddr = 0x76;
  else if (probeBmp280(0x77)) bmpAddr = 0x77;

  delay(200);
  if (bmpAddr != 0) {
    bmpBaselineRaw = (float)readBmpRawPressure(bmpAddr);
    Serial.println("# ORPHEUS_BELOW_PIEZO_PLUS_BMP280_READY");
  } else {
    Serial.println("# ORPHEUS_BELOW_PIEZO_READY");
  }
}

void loop() {
  unsigned long now = millis();
  int raw = analogRead(PIN_PIEZO_ADC);

  // 14 ms peak-hold window captures the true crest of a physical knock/punch!
  if (now >= cooldownUntilMs) {
    if (!capturingPeak && raw > PIEZO_NOISE_FLOOR) {
      capturingPeak = true;
      peakWindowStartMs = now;
      peakAdc = raw;
    } else if (capturingPeak) {
      if (raw > peakAdc) peakAdc = raw;
      if (now - peakWindowStartMs >= 14) {
        float norm = adcToNormalized(peakAdc);
        Serial.print("P,");
        Serial.println(norm, 3);

        capturingPeak = false;
        peakAdc = 0;
        cooldownUntilMs = now + 115; // Debounce piezoelectric ringing
      }
    }
  }

  // 30 Hz live meter feed + optional BMP280 breath check
  if (now - lastTelemetryMs >= 33) {
    lastTelemetryMs = now;

    if (bmpAddr != 0) {
      int32_t pRaw = readBmpRawPressure(bmpAddr);
      if (pRaw > 0) {
        float delta = (float)pRaw - bmpBaselineRaw;
        if (fabs(delta) < 120.0f) {
          bmpBaselineRaw = bmpBaselineRaw * 0.98f + (float)pRaw * 0.02f;
        }
        bmpBreathNorm = constrain((delta - 90.0f) / 650.0f, 0.0f, 1.0f);
        if (bmpBreathNorm < 0.10f) bmpExhaleArmed = true;
        else if (bmpExhaleArmed && bmpBreathNorm >= 0.22f && now >= cooldownUntilMs) {
          bmpExhaleArmed = false;
          Serial.print("S,");
          Serial.println(bmpBreathNorm, 3);
          cooldownUntilMs = now + 150;
        }
      }
    }

    float piezoNorm = adcToNormalized(capturingPeak ? peakAdc : raw);
    float liveNorm = piezoNorm > bmpBreathNorm ? piezoNorm : bmpBreathNorm;
    Serial.print("0.0,");
    Serial.println(liveNorm, 3);
  }

  delayMicroseconds(500); // ~2 kHz sampling so fast piezo spikes are never missed
}
