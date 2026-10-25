(function (root, factory) {
  "use strict";
  const api = factory();
  if (typeof module === "object" && module.exports) module.exports = api;
  else root.GojoPreview = api;
})(typeof globalThis !== "undefined" ? globalThis : this, function () {
  "use strict";
  const phases = Object.freeze(["hunting", "retreat", "aggro", "dragging"]);

  class PreviewState {
    constructor() { this.reset(); }

    reset() {
      this.phase = "hunting";
      this.anger = 0;
      this.active = true;
      this.voiceTriggerCount = 0;
    }

    get intensity() {
      return this.active ? 0.15 + 0.85 * this.anger : 0;
    }

    setAnger(value) {
      if (typeof value !== "number" || !Number.isFinite(value)) {
        throw new TypeError("Anger must be a finite number.");
      }
      this.anger = Math.max(0, Math.min(1, value));
    }

    setPhase(phase) {
      if (!phases.includes(phase)) throw new RangeError("Unknown preview phase.");
      if (!this.active || phase === this.phase) return false;
      this.phase = phase;
      const voiceTriggered = phase === "aggro";
      if (voiceTriggered) this.voiceTriggerCount++;
      return voiceTriggered;
    }

    end() { this.active = false; }
  }

  return Object.freeze({ PreviewState, phases });
});

