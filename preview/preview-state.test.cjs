"use strict";
const test = require("node:test");
const assert = require("node:assert/strict");
const { PreviewState, phases } = require("./preview-state.js");

test("all four phases retain anger-scaled aura, including retreat and dragging", () => {
  for (const phase of phases) {
    const state = new PreviewState();
    state.setPhase(phase);
    state.setAnger(0);
    assert.equal(state.intensity, 0.15, phase + " has a faint baseline");
    state.setAnger(0.5);
    assert.ok(Math.abs(state.intensity - 0.575) < 1e-12);
    state.setAnger(1);
    assert.equal(state.intensity, 1, phase + " reaches the bounded maximum");
  }
});

test("one voice event per aggro entry, never from repeated updates or anger changes", () => {
  const state = new PreviewState();
  assert.equal(state.setPhase("aggro"), true);
  for (let i = 0; i < 100; i++) {
    state.setAnger(i / 100);
    assert.equal(state.setPhase("aggro"), false);
  }
  assert.equal(state.voiceTriggerCount, 1);
  assert.equal(state.setPhase("retreat"), false);
  assert.equal(state.setPhase("aggro"), true);
  assert.equal(state.voiceTriggerCount, 2);
  assert.equal(state.setPhase("dragging"), false);
  assert.equal(state.voiceTriggerCount, 2);
});

test("anger is bounded and invalid input cannot corrupt state", () => {
  const state = new PreviewState();
  state.setAnger(-5);
  assert.equal(state.anger, 0);
  state.setAnger(5);
  assert.equal(state.anger, 1);
  for (const value of [NaN, Infinity, -Infinity, "0.5", null]) {
    assert.throws(() => state.setAnger(value), TypeError);
    assert.equal(state.anger, 1);
  }
  assert.throws(() => state.setPhase("unknown"), RangeError);
  assert.equal(state.phase, "hunting");
});

test("ending clears aura and blocks new phase/audio events; reset starts cleanly", () => {
  const state = new PreviewState();
  state.setAnger(1);
  state.setPhase("aggro");
  state.end();
  assert.equal(state.intensity, 0);
  assert.equal(state.setPhase("hunting"), false);
  assert.equal(state.setPhase("aggro"), false);
  assert.equal(state.voiceTriggerCount, 1);
  state.reset();
  assert.equal(state.phase, "hunting");
  assert.equal(state.anger, 0);
  assert.equal(state.active, true);
  assert.equal(state.voiceTriggerCount, 0);
  assert.equal(state.intensity, 0.15);
});

test("multiple preview instances do not share phase, anger, or audio counters", () => {
  const first = new PreviewState();
  const second = new PreviewState();
  first.setAnger(1);
  first.setPhase("aggro");
  assert.equal(second.phase, "hunting");
  assert.equal(second.anger, 0);
  assert.equal(second.voiceTriggerCount, 0);
});

