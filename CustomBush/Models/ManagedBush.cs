using LeFauxMods.Common.Integrations.CustomBush;
using LeFauxMods.Common.Models;
using LeFauxMods.Common.Utilities;
using LeFauxMods.CustomBush.Services;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Internal;
using StardewValley.TerrainFeatures;

namespace LeFauxMods.CustomBush.Models;

/// <inheritdoc cref="ICustomBush" />
internal sealed class ManagedBush(Bush bush) : DictionaryDataModel(bush), ICustomBush
{
    private Item? shakeItem;

    public BushData Data => ModState.Data[this.Id];

    /// <inheritdoc />
    public bool IsInSeason => !string.IsNullOrWhiteSpace(this.Condition);

    public ICustomBushStage Stage => this.Data.Stages[this.StageId];

    /// <inheritdoc />
    public Texture2D Texture => bush.IsSheltered() && !string.IsNullOrWhiteSpace(this.Stage.IndoorTexture)
        ? ModState.GetTexture(this.Stage.IndoorTexture)
        : ModState.GetTexture(this.Stage.Texture);

    /// <inheritdoc />
    public string? Condition
    {
        get => this.Get(nameof(this.Condition));
        set => this.Set(nameof(this.Condition), value ?? string.Empty);
    }

    /// <inheritdoc />
    public string Id
    {
        get => this.Get(nameof(this.Id));
        set => this.Set(nameof(this.Id), value);
    }

    /// <inheritdoc />
    public Item? Item
    {
        get
        {
            if (!bush.readyForHarvest() || string.IsNullOrWhiteSpace(this.ShakeOff))
            {
                return null;
            }

            return this.shakeItem ??= ItemRegistry.Create(this.ShakeOff, this.Stack, this.Quality);
        }
        set
        {
            this.shakeItem = value;
            this.ShakeOff = value?.ItemId;
            this.Quality = value?.Quality ?? 0;
            this.Stack = value?.Stack ?? 0;
            bush.tileSheetOffset.Value = value is null ? 0 : 1;
            bush.setUpSourceRect();
        }
    }

    public int Quality
    {
        get => this.Get(nameof(this.Quality), StringToInt);
        set => this.Set(nameof(this.Quality), value, IntToString);
    }

    public string? ShakeOff
    {
        get => this.Get(nameof(this.ShakeOff));
        set => this.Set(nameof(this.ShakeOff), value ?? string.Empty);
    }

    /// <inheritdoc />
    public int SpriteOffset
    {
        get => this.Get(nameof(this.SpriteOffset), StringToInt);
        set => this.Set(nameof(this.SpriteOffset), value, IntToString);
    }

    public int Stack
    {
        get => this.Get(nameof(this.Stack), StringToInt, 1);
        set => this.Set(nameof(this.Stack), value, IntToString);
    }

    /// <inheritdoc />
    public int StageCounter
    {
        get => this.Get(nameof(this.StageCounter), StringToInt);
        set => this.Set(nameof(this.StageCounter), value, IntToString);
    }

    /// <inheritdoc />
    public string StageId
    {
        get => this.Get(nameof(this.StageId));
        set => this.Set(nameof(this.StageId), value);
    }

    /// <inheritdoc />
    protected override string Prefix => ModConstants.ModDataPrefix;

    /// <inheritdoc />
    public bool TestCondition(string? condition)
    {
        if (string.IsNullOrWhiteSpace(condition))
        {
            return true;
        }

        return GameStateQuery.CheckConditions(
            condition,
            bush.Location,
            null,
            null,
            null,
            null,
            bush.Location.SeedsIgnoreSeasonsHere()
                ? GameStateQuery.SeasonQueryKeys
                : null);
    }

    /// <inheritdoc />
    public bool TryProduceDrop(ICustomBushDrop drop, [NotNullWhen(true)] out Item? item)
    {
        if (!this.TestCondition(drop.Condition))
        {
            item = null;
            return false;
        }

        item = ItemQueryResolver.TryResolveRandomItem(
            drop,
            new ItemQueryContext(
                bush.Location,
                null,
                null,
                $"custom bush '{this.Id}' > drop '{drop.Id}'"),
            false,
            null,
            null,
            null,
            (query, error) => Log.Error(
                "{0} failed parsing item query {1} for item {2}: {3}",
                this.Id,
                query,
                drop.Id,
                error));

        return item is not null;
    }
}