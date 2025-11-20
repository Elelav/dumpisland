# dumpisland
Game for GameOff GameJam (Game GamE gamE) 


#  DumpIsland

A 2D roguelike survivor-like game built in Unity where you clean up beach trash while fighting off waves of ocean enemies.

##  Game Overview

Battle through waves of increasingly difficult enemies, collect trash to upgrade your character, and survive the ultimate boss fight. After defeating the boss, test your build in endless mode!

##  Key Features

### Core Gameplay
- **Auto-attack system** - Equipped weapons automatically target enemies
- **Trash collection** - Collect enemy remains and exchange them for money
- **Shop system** - Purchase upgrades, weapons, perks, and healing
- **Perk system** - 21 unique perks with multiple levels
- **14 weapons** - Mix of melee (AoE) and ranged (projectile) attacks
- **Wave-based progression** - 11 minutes of enemy waves → boss fight → endless mode

### Advanced Mechanics
- **Armor & Regeneration** - Damage mitigation and passive healing
- **Loot auto-merging** - Nearby loot automatically combines into larger piles
- **Magnetic collection** - Perk-based automatic loot attraction
- **Interactive objects** - Food, energy drinks, chests, power-ups
- **Tsunami waves** - Clear the field between waves with destructive water
- **Elite enemies & bosses** - Special abilities and challenging encounters

### Performance Optimizations (Somehow it went not that good as it sounds but i`m trying)
- **Object pooling** - For loot, projectiles, and particles 
- **Batched animations** - Single-update system for all loot bobbing
- **Memory management** - Cached sprites, material cleanup, aggressive GC
- **Particle limits** - Controlled hit effects and visual effects

## How to import
Just drag n drop files into new Unity 6.2 project -> Open "Main Menu" or any other scene and press Play. Thats should do it.
