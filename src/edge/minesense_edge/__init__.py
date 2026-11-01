"""MineSense Safety Edge agent (cabin gateway).

Generates synthetic biometric signals, classifies fatigue risk locally so the
in-cabin warnings keep working without connectivity (US13), stores every event
in a durable SQLite buffer and synchronizes it in idempotent batches with the
Edge Synchronization Service when the link returns (US14, TS-13).
"""

__version__ = "0.2.0"
