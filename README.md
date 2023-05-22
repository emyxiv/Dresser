# Dresser

A [Dalamud](https://discord.gg/3NMcUV5) plugin aiming to ease glamour dresser experience.


This plugin is still under development, there are a lot of things to improve.

⚠️ This plugin may be resources hungry as it attempts to recreate original item's slots style visuals.

### Planned features

1. current gear window
    - vanilla buttons for current appearance show/hide weapons/helmet/visor
    - show what gearsets are linked to this glam plate
    - displays greyed out when selecting an items that isn't in dresser/armoire
    - context menu
    - colorize portable plates different from actual plates
    - show tasks to do
      - get dyes
      - buy from vendor
      - take item from x inventory and put it in glamour chest

2. Gear Browser window
    - make a "lite" version without icons so it can run smoother on low grade hardware
    - fuzzy search (split sentances with && contains words ignorecase)
      - also add search in dye names and show items with dye name matching with the text
    - advanced filters
      - regex search
      - category, level, ilvl, race, gender, class
      - current compatible (of all the above)
      - rarity
      - dyeable
      - favourites (possible IPC with Item Search?)
      - craftable
      - tradable
      - sold by npc
      - store item (mogstation)
    - unobtained
      - store item (mogstation)
      - sold by npc for < xxx gil
      - sold at mb for < xxx gil


3. On actual dresser
  - tell what to get from other inventories/vendor before applying

4. Item Icons
    - more keyboard shortcuts
       - similar to simple tweaks:
          - copy item name
          - open in inventory tools (recipe)
          - open gamer escape
          - open garland tools
          - teamcraft
          - universalis
        - link in chat (echo)
        - link in chat (for other, with < item > )

5. Camera placement
    - place camera on character on enter
    - set lower FOV on enter
    - unset custom camera settings on leave

6. keep track of everything
    - add/save InventoryItem those data:
      - number of tryon
      - number of saved
      - last tryon
      - last saved
    - analysis or "rarely used items" for suggestions what to store in retainers



Thanks to Critical impact for making CriticalImpactLib and their great AllaganTools. Thanks to Anna for their great Glamaholic. Thanks to Chirp for their amazing discoveries in Ktisis, and Fayti1703 for their patience when teaching me all kind of dev tips ❤️.
