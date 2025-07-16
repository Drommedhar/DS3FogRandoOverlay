﻿# DS1 Fog Gate Randomizer

Changes how areas link together by randomizing where fog gates lead to, in the spirit of ALttP/OoT entrance randomizers

Fog gates are now permanent, and traversing them warps you to the other side of a different fog gate. These warps are fixed for a given seed.

This mod was designed for Dark Souls Remastered, but it should also work for PTDE. The game installation location to randomize is configurable in the randomizer UI.

**This mod must be played with the game offline in all editions. Also, before going back online in Remastered, be sure to delete any save files you used.** Steam does not have to be offline, just the in-game network settings.

## Gameplay changes
It is configurable which types of entrances are randomized and which behave like the base game. They come in these categories:

- Traversable fog gates (non-boss)
- Boss fog gates
- Preexisting warps between areas
- Major PvP fog gates (between areas)
- Minor PvP fog gates
- Lordvessel gates

No new fog gates are added. PvP fog gates are ones you normally only see when you are invaded.

By default, warps are bidirectional, meaning you can explore the world like it is a cut up and reassembled version of the base game. If you instead enable the "Disconnected fog gates" option, warps become points of no return. This is similar to ALttP Insanity Entrance Shuffle or OoT BetaQuest.

Usually more randomized entrances = longer run. Randomized boss fog gates, preexisting warps, and major PvP fog gates are usually easy to backtrack from. Traversable fog gates and minor PvP fog gates make it more difficult to backtrack, because you often have to check both sides and drop down to do so. Other randomizers and disconnected fog gates both result in more to explore.

- For an insane completionist run, you can enable all fog gates (with or without lordvessel gates, which are also interesting with their vanilla behavior), and this takes about 8 hours to find all areas. With key item randomization, it can take around 12 hours (less with Race Mode).
- For a more bite-sized or race-friendly run - if you don't want to go hollow, or are streaming it and want to fit it into one stream - enable only boss fog gates and area warps. This takes about 4 hours to find all areas. With key item randomization, it can take around 8 hours (less with Race Mode).

These estimates are from people who have played the mod. If you have more data, I can add it here!

There are a few other differences from the base game:

- Estus and starting items are available before any fog gates
- All bonfires have an option to return to the start of the game, in case you get stuck after using a bonfire. This is like save & quit in Zelda entrance randomizers.
- You can't save & quit to escape a boss fight, because positions before warps are always discarded by the game. Use Pacifist Mode for more mobility options.
- The trigger for Undead Asylum #2 is using the Big Pilgrim's Key and warping away and back, rather than traveling to Firelink.
- Seath's scripted player death has been replaced with an object you can use to warp always (to a random place, if warps are randomized)
- NPC invasions removed for now, they are messy to clean up after

Also, some things which are in the base game you may need to know about:

- The elevator above Demon Firesage automatically activates after you defeat Demon Firesage, even if you're coming from Daughter of Chaos
- You can drop down from Darkroot Forest to Darkroot Basin by starting after the Crest of Artorias door and following the wall to the left
- You can drop down into the Phalanx courtyard in Painted World from the bridge above it
- It's possible to go backwards up Crystal Caves using an invisible path

### How to win
By defeating Gwyn. If the "Require Lord Souls" option is enabled then opening the Firelink Altar door (or PCC warp) is the only way to get to Gwyn. Warps to the Firelink Altar are not randomized, mainly because they are tied to serpent loyalty. If this is disabled, Gwyn can potentially be among the first few fog gates.

Taking notes can be helpful to remember how to get to different important places. One possible strategy is to list out routes you *didn't* take when going deeper along one particular branch. If you reach a place you need to come back to later, make sure you know how to get there.

Starting with or using the master key is never required. If you have it, it can be used to access areas early, but there may be significant scaling differences on the other side of those doors.

### Scaling
This is an optional feature of the mod to scale up and down enemy health and damage based on distance from the start. This is done statically when the randomizer is run, not during the playthrough. The goal is to make it more enjoyable to actually fight enemies, rather than only run past them.

The randomizer checks what is the shortest path for you to access a blacksmith who sells titanite shards (Andre, Giant Blacksmith, or Vamos), and ensures that bosses on that path are scaled within reason for a +0 weapon. On the other hand, areas which appear earlier in the base game are never scaled down. Gwyn is never scaled down.

## Installation
For PTDE only, you must first unpack Dark Souls for modding. (https://www.nexusmods.com/darksouls/mods/1304)

To install the mod, unzip FogMod.zip anywhere. It should contain FogMod.exe along with a dist subdirectory used by the randomizer. Run FogMod.exe, select your options, and click "Randomize!" to randomize. This automatically creates backups, and also creates a "runs" folder that has logs, including the seed and spoilers. You must select the game exe manually if you have a non-standard DS1 install location. You can also enter a seed (any number between 0 and 4294967295). For all changes to take effect, restart the game, and start a new save file.

When randomizing with scaling enabled, try not to access existing save files that have been previously accessed with different scaling (different seed or options) or no scaling. If you quit out of a save file with enemies loaded into the area, and the scaling changes too much on reload, the game will crash to desktop. The save file is not corrupted, but can't be opened anymore unless you return to the previous enemy scaling.

To uninstall, click "Restore backups". This replaces the game files with whatever they were before the first randomization. To be completely sure all mods are gone, in Remastered only, select Properties → Local Files → Verify Integrity Of Game Files in Steam. In PTDE, it will work to re-run UDSFM.

### Installing with other randomizers
This mod is compatible with DS1 Item Randomizer starting from v0.2 (https://www.nexusmods.com/darksouls/mods/1305/ or https://www.nexusmods.com/darksoulsremastered/mods/86) and DS1 Enemy Randomizer (https://www.nexusmods.com/darksouls/mods/1407) starting from v0.3. You can also try it with orthogonal randomizers like Paramdomizer (misc param randomizer) and ClusterFXR (visual effects randomizer).

This order works well for running them:
1. Item Randomizer
2. Enemy Randomizer
3. Paramdomizer
4. Fog Gate Randomizer

Sometimes, Item Randomizer will produce an item configuration that Fog Gate Randomizer says it can't solve, or Enemy Randomizer will produce invalid MSB files. When this happens, just re-run those respective randomizers with different seeds. Incidentally, doing "Restore backups" in Fog Gate Randomizer seems to undo most of the effects of the above randomizers, since they all backup to the same locations.

If you want to re-run Enemy Randomizer during a playthrough, you need to re-run Fog Gate Randomizer afterwards with the last used seed as the fixed seed. The last used seed can be found under the "Randomize!" button, and also in the newest file in the "runs" directory. If you get any errors re-running Enemy Randomizer, try clicking "Revert to normal" and then trying again.

Item Randomizer compatibility is thanks to HotPocketRemix for classifying key item locations in a way fog gate randomizer can understand. If you use Race Mode+ in Item Randomizer, you should also use Glitched Logic in Fog Gate Randomizer. This will work the vast majority of the time. If it doesn't, try a different seed in Item Randomizer.

Also, if you're using "fully random" enemy randomization (rather than "difficulty curve"), it may be better to avoid Fog Gate Randomizer scaling, since it is possible for difficult enemies in early areas to be scaled up even further.

### Compatibility with other mods
Fog Gate Randomizer is *not* compatible with game-file-based mods which make event-based changes to game progression, like Daughters of Ash or Scorched Contract. Mods like these require changes to files in event\ or script\talk.

If the other mod has game files in map\MapStudio, msg\ENGLISH, or param\GameParam, then you can try installing it before Fog Gate Randomizer, but there is no guarantee they will work well together.

Otherwise, the mods should be independent.

## Appendix: Randomizable entrances
Traversable fog gates (non-boss)

- Near the area with the Channeler in the Depths
- At the bridge crossing over the boar in Undead Parish
- Near the drake encounter in Upper Burg
- Between tower with statue and courtyard with phalanx
- Between the Darkroot Garden bonfire and the area with the stone knights
- Between the bridge Patches flips over and the room with two skeletons
- Before the Halberd Black Knight in TotG
- Between Great Hollow and Ash Lake
- In between the main pit and wood planks while climbing up/down Blighttown
- Near the outside boulder stairway and Siegmeyer in Sen's Fortress
- Between final swinging axes aand Sen's Fortress roof
- Between the Painting Guardian rafters and the elevator contraption
- Between the first two Silver Knights and the main building
- Near the New Londo Ruins shortcut ladder
- On an entrance to the building with the Very Large Ember in lower ruins
- Between Duke's Archives and the field before Crystal Caves
- Between hallway with initial weapons and Oscar's cell

Boss fog gates

- To Gaping Dragon
- To Gargoyles
- After Gargoyles
- To Taurus Demon
- After Taurus Demon
- To Capra Demon
- To Priscilla
- To Sif
- To Moonlight Butterfly
- After Moonlight Butterfly
- To Sanctuary Guardian
- After Sanctuary Guardian
- To Manus
- To Artorias
- After Artorias
- To Kalameet
- To Pinwheel
- To Nito
- To Quelaag
- After Quelaag
- To Ceaseless Discharge
- To Demon Firesage
- After Demon Firesage
- To Centipede Demon
- After Centipede Demon
- To Bed of Chaos
- To Iron Golem
- To O&S
- After O&S on left side
- After O&S on right side
- To Gwyndolin
- To Four Kings
- To Seath in Crystal Caves
- To Seath in Archives
- To Gwyn
- To Asylum Demon from balcony
- In Stray Demon arena

Preexisting warps between areas

- Crow transport from Firelink to Asylum
- Leaving Painted World back to Anor Londo
- Warp to DLC
- Getting into coffin to Nito
- Getting back in coffin at Nito
- From Sen's to Anor Londo
- Using Peculiar Doll to access Painted World
- From Anor Londo back to Sen's
- Warp to Firelink crow's nest
- To Duke's Archives prison

Major PvP fog gates (between areas)

- Between Firelink Shrine and Upper Burg
- Between Undead Parish and Andre
- In front of Firelink Shrine dual elevator
- Between female Undead Merchant and Lower Burg
- Between very long ladder and Lower Burg
- Upper doorway of room with burg bonfire
- Between Darkroot and Andre
- Between Darkroot and tunnel leading to Valley of the Drakes
- Between Firelink Shrine and Catacombs
- Between TotG and Catacombs
- Between Blighttown and Valley of the Drakes
- Between Blighttown and Great Hollow
- Between Quelaag's Domain and the Demon Ruins bonfire
- Between the Firelink Shrine elevator and New Londo Ruins
- Between the Kiln door and Black Knight area

Minor PvP fog gates

- Between area after Taurus Demon and Hellkite bridge
- Lower doorway of room with burg bonfire
- At one end of the bridge at the top of the waterfall with the hydra below
- At the bottom of the elevator from before Artorias to before Manus
- Between Sanctuary Garden and Royal Forest
- At the top of the elevator from before Artorias to before Manus
- Between Gough's stairway and Oolacile Township
- Between Gough's stairway and Artorias
- Between Blighttown and the Great Hollow
- Between stairways behind Firesage and elevator to Quelaag's Domain
- Between the Demon Ruins lava area (drained by Ceaseless) and the cliffside path where Kirk invades
- Between Sen's Fortress gate and Andre
- At the doorway connecting the first Anor Londo elevator and the first gargoyle
- From the Anor Londo main cathedral out of a broken window in front
- Between the Anor Londo main cathedral and the area with bedrooms and paintings
- Between the Anor Londo main cathedral and the Giant Blacksmith
- At the first elevator in Duke's Archives
- At the hallway with the crystal warrior in Duke's Archives

Lordvessel gates

- Golden fog gate in Tomb of the Giants
- Golden fog gate in Demon Ruins
- Golden fog gate leading to Duke's Archives

## Appendix: Glitched logic

Glitched logic includes the following skips:

- Gaping Dragon fog gate skip: jump from balcony near channeler into Gaping arena
- Lower Undead Burg skip: jump from Upper Burg (before or after first fog gate) into Lower Burg
- Capra skip: jump from Upper Burg (after first fog gate) into Depths
- Annex Key skip: jump in Painted World into Annex area
- PCC item swap in Darkroot: warp from Darkroot Garden to Battle of Stoicism entrance with PCC
- PCC kiln wrong warp in DLC: warp from Oolacile (in Royal Woods, Township, Chasm, or stairs near Gough) to Kiln interior with PCC, if Firelink Altar has been rested at
- Tomb of the Giants fog gate skip: jump around fog gate, used in All Bosses
- Seal Skip: jump from the start of New Londo Ruins to the chest behind an illusory wall, and into Four Kings fight if Covenant of Artorias has been obtained
- Duke Skip: use the elevator to go from the first half of the Duke's Archives library to the second half
