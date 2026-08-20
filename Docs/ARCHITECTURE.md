# Architecture baseline

The runtime is split into deterministic simulation-oriented systems and presentation.

- `Core`: 60 Hz clock and application bootstrap.
- `Input`: per-player device pairing and reconnect policy.
- `Combat`: move frame data, hit/hurt regions, hit pause, and attack resolution.
- `Characters`: explicit fighter state machine and lane movement.
- `AI`: encounter attack-token arbitration.
- `Content`: character, companion, and customer-pack definitions.

World movement uses an `x/depth` ground plane plus a separate visual height value for jumps and thrown objects. Combat does not depend on unconstrained rigidbody contacts.

Customer-specific presentation is referenced by `CustomerPackDefinition`; game systems must never branch on a customer name.

