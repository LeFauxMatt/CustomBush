# Custom Bush

Custom Bush is a framework mod which allows custom crops to be added to the game
that inherit all of the functionality of Tea Saplings.

- Tea saplings can be planted on untilled ground.
- They continue to produce after reaching an age of maturity.

Unlike Tea Saplings, Custom Bush offers additional options:

- Any number of any items can be dropped.
- The bush sprite can vary based on the item that will be dropped.
- Bushes are able to use game state queries to determine what can be dropped.
- `PlantableRules` can be used to restrict where custom bushes are allowed to be
  planted at.

In general any of the fields that applies to [Fruit
Trees](https://stardewvalleywiki.com/Modding:Fruit_trees) can also apply to
Custom Bushes since they share many of the same attributes.

## Data Format

Refer to [Fruit Trees](https://stardewvalleywiki.com/Modding:Fruit_trees) for
most of the applicable attributes. Additional attributes are explained below:

| Field               | Description                                                                                      |
| :------------------ | :----------------------------------------------------------------------------------------------- |
| AgeToProduce        | The minimum number of days from planting before a bush will start to produce items.              |
| ConditionsToProduce | A list of game state queries that determines whether the bush can produce any items.<sup>1</sup> |
| DisplayName         | Mainly used for mod integration such as UI Info Suite 2 and Lookup Anything.                     |
| Description         | Mainly used for mod integration such as UI Info Suite 2 and Lookup Anything.                     |
| IndoorTexture       | The texture to use when the bush is planted indoors in a pot.                                    |
| Texture             | The texture to use when the bush is planted under any other conditions.                          |
| TextureSpriteRow    | The row that will be used to draw the bush. Each bush has a height of 32 pixels.                 |
| ItemsProduced       | A list of items that can be dropped and their conditions.<sup>2</sup>                            |

1. See [ConditionsToProduce](#conditions-to-produce)
2. See [Drops](#drops).

### Example

```jsonc
{
  "Format": "2.4.0",
  "Changes": [
    {
      "LogName": "Load the custom bush sapling",
      "Action": "EditData",
      "Target": "Data/Objects",
      "Entries": {
        "{{ModId}}_MyBush_Sapling": {
          "Name": "MyBush_Sapling",
          "DisplayName": "{{i18n: MyBush_Sapling.name}}",
          "DisplayName": "{{i18n: MyBush_Sapling.description}}",
          "Type": "Basic",
          "Category": -74,
          "Texture": "{{InternalAssetKey: assets/MyBush_Sapling.png}}",
          "SpriteIndex": 0
        }
      }
    },
    {
      "LogName": "Load the custom bush data",
      "Action": "EditData",
      "Target": "furyx639.CustomBush/Data",
      "Entries": {
        "(O){{ModId}}_MyBush_Sapling": {
          "AgeToProduce": 22,
          "ConditionsToProduce": [
            "SEASON Spring,DAY_OF_MONTH 15 16 17 18 19 20 21", // Early spring harvest
            "SEASON Spring,DAY_OF_MONTH 22 23 24 25 26 27 28"  // Late spring harvest
          ],
          "ItemsProduced": [
            {
              "ItemId": "(O)442",
              "Condition": "SEASON Spring,DAY_OF_MONTH 15 16 17 18 19 20 21",
              "Chance": 1.0,
              "MinStack": 1,
              "MaxStack": 1
            },
            {
              "ItemId": "(O)307",
              "Condition": "SEASON Spring,DAY_OF_MONTH 22 23 24 25 26 27 28",
              "Chance": 1.0,
              "MinStack": 1,
              "MaxStack": 1,
              "SpriteOffset": 1 // Vary the texture if this item is being produced
            }
          ],
          "Texture": "{{InternalAssetKey: assets/MyBush.png}}",
          "IndoorTexture": "{{InternalAssetKey: assets/MyBush_indoors.png}}"
        }
      }
    }
  ]
}
```

## Drops

Drops use [item spawn
fields](https://stardewvalleywiki.com/Modding:Item_queries#Item_spawn_fields).
Additional attributes are explained below:

| Field        | Description                                                                                                                                                          |
| :----------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Change       | (Optional) (Optional) The probability that this entry is selected, as a value between 0 (never drops) and 1 (always drops). Default 1 (100% chance).                 |
| Season       | (Optional) If set, the group only applies if the bush's location is in this season. This is ignored in non-seasonal locations like the greenhouse and Ginger Island. |
| SpriteOffset | (Optional) An offset to the right of the bloomed sprite to vary the texture based on the item drop that was selected. Each offset is 16 pixels in width.             |

## Conditions To Produce

Conditions are [Game State
Queries](https://stardewvalleywiki.com/Modding:Game_state_queries). When the
bush is not carrying an item, it will check each condition, one at a time, from
top to bottom, and stop at the first condition that passes.

Once a condition passes, the bush then produces an item, and saves which
condition passed.

Then every day, it will continue to check that same condition to make sure it
still passes. As long as the condition passes, the item can be collected by the
player.

If the condition ever fails, the item will be cleared out, and the process
repeats.

What this means is that you can group harvests based on conditions. A simple
example is that you can have one condition for spring, and another one for
summer. When an item is produced in the spring, as soon as it changes to summer,
if the item isn't collected, that item will be lost and the bush can reroll for
a different summer item.