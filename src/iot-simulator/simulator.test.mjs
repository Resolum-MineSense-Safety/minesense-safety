// node --test: the cabin classifier must match FatigueRiskPolicy (Fatigue Detection Service).
import assert from 'node:assert/strict'
import { test } from 'node:test'
import { classify } from './simulator.mjs'

test('normal reading', () => assert.equal(classify({ perclos: 0.08, blinkRatePerMinute: 14, heartRateVariabilityMs: 52 }), 'Normal'))
test('warning by PERCLOS', () => assert.equal(classify({ perclos: 0.15, blinkRatePerMinute: 14, heartRateVariabilityMs: 52 }), 'Warning'))
test('warning by blink rate', () => assert.equal(classify({ perclos: 0.05, blinkRatePerMinute: 25, heartRateVariabilityMs: 52 }), 'Warning'))
test('warning by low HRV', () => assert.equal(classify({ perclos: 0.05, blinkRatePerMinute: 14, heartRateVariabilityMs: 29 }), 'Warning'))
test('critical by PERCLOS', () => assert.equal(classify({ perclos: 0.4, blinkRatePerMinute: 14, heartRateVariabilityMs: 52 }), 'Critical'))
test('critical by PERCLOS and HRV combined', () => assert.equal(classify({ perclos: 0.25, blinkRatePerMinute: 14, heartRateVariabilityMs: 19 }), 'Critical'))
