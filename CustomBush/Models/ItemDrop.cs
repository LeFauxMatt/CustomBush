using LeFauxMods.Common.Integrations.CustomBush;
using StardewValley.GameData;

namespace LeFauxMods.CustomBush.Models;

/// <inheritdoc cref="ICustomBushDrop" />
internal sealed class ItemDrop : GenericSpawnItemDataWithCondition, ICustomBushDrop
{
    private float chance = -1f;
    private int day = -1;

    /// <inheritdoc />
    public bool ReplaceItem { get; set; }

    /// <inheritdoc />
    public int SpriteOffset { get; set; } = 1;

    public float GetChance()
    {
        if (this.chance >= 0)
        {
            return this.chance;
        }

        if (string.IsNullOrWhiteSpace(this.Condition))
        {
            this.chance = 1;
            return 1;
        }

        foreach (var parsed in GameStateQuery.Parse(this.Condition))
        {
            if (parsed.Error is not null ||
                parsed.Query.Length < 2 ||
                string.IsNullOrWhiteSpace(parsed.Query[0]) ||
                !parsed.Query[0].Trim().Equals(nameof(GameStateQuery.DefaultResolvers.RANDOM),
                    StringComparison.OrdinalIgnoreCase) ||
                !float.TryParse(parsed.Query[1], out var value))
            {
                continue;
            }

            this.chance = value;
            return value;
        }

        this.chance = 1;
        return 1;
    }

    public int GetDay()
    {
        if (this.day >= 0)
        {
            return this.day;
        }

        if (string.IsNullOrWhiteSpace(this.Condition))
        {
            this.day = 0;
            return 0;
        }

        var days = Enumerable.Range(1, 28);
        foreach (var parsed in GameStateQuery.Parse(this.Condition))
        {
            if (parsed.Error is not null ||
                parsed.Query.Length < 2 ||
                string.IsNullOrWhiteSpace(parsed.Query[0]))
            {
                continue;
            }

            var trimmedQuery = parsed.Query[0].Trim();
            if (trimmedQuery.Equals(
                    nameof(GameStateQuery.DefaultResolvers.DAY_OF_MONTH),
                    StringComparison.OrdinalIgnoreCase))
            {
                if (parsed.Query[1].Equals("odd", StringComparison.OrdinalIgnoreCase))
                {
                    days = days.Where(static value => value % 2 == 1);
                    continue;
                }

                if (parsed.Query[1].Equals("even", StringComparison.OrdinalIgnoreCase))
                {
                    days = days.Where(static value => value % 2 == 0);
                    continue;
                }

                var validDays = parsed.Query[1..]
                    .Select(static query => int.TryParse(query, out var value) ? value : 29).ToList();
                days = days.Where(validDays.Contains);
                continue;
            }

            if (trimmedQuery.Equals(ModConstants.QueryKeyDayIsMultipleOf, StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(parsed.Query[1], out var multipleOf))
            {
                days = days.Where(value => value % multipleOf == 0);
                continue;
            }

            if (trimmedQuery.Equals(ModConstants.QueryKeyDayRange, StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(parsed.Query[1], out var minDay))
            {
                days = days.Where(value => value >= minDay);
            }
        }

        this.day = days.Min();
        return this.day;
    }
}