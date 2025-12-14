# Spire Game Protocol

## Protocol Header Format

* Total 4 bytes.
* Length does not include header size.
* Protocol IDs are defined manually. See [schema/game/auth.json](schema/game/auth.json) for reference.

| Bytes | Content     |
|------:|-------------|
|   0~1 | Length      |
|   2~3 | Protocol ID |
