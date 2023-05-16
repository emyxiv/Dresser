# Dresser

A [Dalamud](https://discord.gg/3NMcUV5) plugin aiming to ease glamour dresser experience.


This plugin is still under development, there are a lot of things to improve.
WARNING: this plugin may be resources hungry as it attempts to recreate original item's slots style visuals.

Sorry for the dirty coding. I'm just a mediocre dev with ideas but no RE capacities.

TODO:
Optimize image memory or make image-less option.

Planned features:

1. glam anywhere
    - offer to apply pending changes when opening dresser with pending changes
       - tell what to get from other inventories/vendor before applying
       - display that ^ as a nice list of items and dyes
       - after it's applied and it differs from pending plates, ask if they want to reload actual plates into pending plates

2. current appearance
    - load current plate appearance on enter
    - vanilla buttons for current appearance show/hide weapons/helmet/visor also affect current appearance
    - show what gearsets are linked to this glam plate

3. current gear window
    - add controls:
       - visibility
    - displays greyed out when selecting an items that isn't in dresser/armoire
    - context menu 
    - show tasks to do
      - get dyes
      - buy from vendor
      - take item from x inventory and put it in glamour chest
      - select plate x and apply changes

4. Gear Browser window
    - fuzzy search (split sentances with && contains words ignorecase)
    - advanced filters
      - regex search
      - category, level, ilvl, race, gender, class
      - current compatible (of all the above)
      - rarity
      - dyeable
      - favourites (IPC with Item Search?)
      - craftable
      - tradable
      - sold by npc
      - store item (mogstation)
    - set a dye to try with

5. Item Icons
    - more keyboard shortcuts
       - like simple tweaks:
          - copy item name
          - open in inventory tools (recipe)
          - open gamer escape
          - open garland tools
          - teamcraft
          - universalis
        - link in chat (echo)
        - link in chat (for other, with < item > )

6. Camera placement
    - place camera on character on enter
    - set lower FOV on enter
    - unset custom camera settings on leave


7. keep track of everything
    - add/save InventoryItem those data:
      - number of tryon
      - number of saved
      - last tryon
      - last saved
    - analysis or "rarely used items" for suggestions what to store in retainers



Thanks to Critical impact for making CriticalImpactLib and their great AllaganTools. Thanks to Anna for their great Glamaholic. Thanks to Chirp for their amazing discoveries in Ktisis, and Fayti1703 for their patience when teaching me all kind of dev tips ♥.
