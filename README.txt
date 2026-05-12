NoMoreRolling v1.0.0
====================
A BepInEx mod for Gamble With Your Friends.

WHAT IT DOES
------------
Fixes the spawn box wakeup sequence. Vanilla behavior launches the player into
a ragdoll spin inside the coffin, often leaving them tumbling. This mod
replaces that with a clean, controlled exit:

  - Player snaps upright and launches forward out of the box immediately.
  - All box colliders are converted to triggers so the player passes through
    any face of the box without getting stuck on the lid.
  - The camera stand-up animation is shortened so there is no visible roll.

INSTALLATION
------------
1. Install BepInEx 5.x into your Gamble With Your Friends directory.
2. Copy NoMoreRolling.dll into:
   BepInEx/plugins/NoMoreRolling/

REQUIREMENTS
------------
  - Gamble With Your Friends (Steam)
  - BepInEx 5.x

GIT
------------
https://github.com/iRecycled/NoMoreRolling
