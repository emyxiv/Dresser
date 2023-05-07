# Dresser

A [Dalamud](https://discord.gg/3NMcUV5) plugin aiming to ease glamour dresser experience.

Planned features:

0. glam anywhere
    - save pending changes from anywhere
    - offer to apply pending changes when opening dresser with pending changes
       - tell what to get from other inventories/vendor before applying
       - display that ^ as a nice list of items and dyes

1. current appearance
    - save current appearance on enter
    - restore current appearance on exit
    - load current plate appearance on enter
    - vanilla buttons for current appearance show/hide weapons/helmet/visor also affect current appearance
    - show what gearsets are linked to this glam plate

2. current gear window
    - add controls:
       - change plate number
       - visibility
    - select a slot
    - displays greyed out when selecting an items that isn't in dresser/armoire
    - context menu 
    - tasks to do
      - get dyes
      - buy from vendor
      - take item from x inventory and put it in glamour chest
      - select plate x and apply changes

3. Gear Browser window
    - save inventories across disconnections
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

4. Item Icons
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

5. Lighting
    - either control inn room lights, or spawn gpose lights
    - set 3 point light around character

6. Camera placement
    - place camera on character on enter
    - set lower FOV on enter
    - unset custom camera settings on leave

7. gamepad support
    - item navigation
    - ...

8. keep track of everything
    - add/save InventoryItem those data:
      - number of tryon
      - number of saved
      - last tryon
      - last saved
    - analysis or "rarely used items" for suggestions what to store in retainers

