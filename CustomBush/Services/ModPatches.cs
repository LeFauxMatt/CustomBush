namespace LeFauxMods.CustomBush.Services;

using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using Common.Utilities;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Models;
using Netcode;
using StardewValley.Characters;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using Utilities;

internal static class ModPatches
{
    private static readonly MethodInfo CheckItemPlantRules;
    private static readonly Harmony Harmony;

    private static Func<Dictionary<string, CustomBush>>? getData;
    private static Func<string, Texture2D>? getTexture;

    static ModPatches()
    {
        Harmony = new Harmony(Constants.ModId);
        CheckItemPlantRules =
            typeof(GameLocation).GetMethod("CheckItemPlantRules", BindingFlags.NonPublic | BindingFlags.Instance) ??
            throw new MethodAccessException("Unable to access CheckItemPlantRules");
    }

    private static Dictionary<string, CustomBush> Data => getData!();

    public static void Init(Func<Dictionary<string, CustomBush>> getDataFunc, Func<string, Texture2D> getTextureFunc)
    {
        getData = getDataFunc;
        getTexture = getTextureFunc;

        _ = Harmony.Patch(
            AccessTools.DeclaredMethod(typeof(Bush), nameof(Bush.draw), [typeof(SpriteBatch)]),
            new HarmonyMethod(typeof(ModPatches), nameof(Bush_draw_prefix)));

        _ = Harmony.Patch(
            AccessTools.DeclaredMethod(typeof(Bush), nameof(Bush.GetShakeOffItem)),
            postfix: new HarmonyMethod(typeof(ModPatches), nameof(Bush_GetShakeOffItem_postfix)));

        _ = Harmony.Patch(
            AccessTools.DeclaredMethod(typeof(Bush), nameof(Bush.inBloom)),
            postfix: new HarmonyMethod(typeof(ModPatches), nameof(Bush_inBloom_postfix)));

        _ = Harmony.Patch(
            AccessTools.DeclaredMethod(typeof(Bush), nameof(Bush.performToolAction)),
            transpiler: new HarmonyMethod(typeof(ModPatches), nameof(Bush_performToolAction_transpiler)));

        _ = Harmony.Patch(
            AccessTools.DeclaredMethod(typeof(Bush), nameof(Bush.setUpSourceRect)),
            postfix: new HarmonyMethod(typeof(ModPatches), nameof(Bush_setUpSourceRect_postfix)));

        _ = Harmony.Patch(
            AccessTools.DeclaredMethod(typeof(Bush), nameof(Bush.shake)),
            transpiler: new HarmonyMethod(typeof(ModPatches), nameof(Bush_shake_transpiler)));

        _ = Harmony.Patch(
            typeof(GameLocation).GetMethod(
                nameof(GameLocation.CheckItemPlantRules),
                BindingFlags.Public | BindingFlags.Instance)
            ?? throw new MethodAccessException("Unable to access CheckItemPlantRules"),
            postfix: new HarmonyMethod(typeof(ModPatches), nameof(GameLocation_CheckItemPlantRules_postfix)));

        _ = Harmony.Patch(
            AccessTools.DeclaredMethod(typeof(HoeDirt), nameof(HoeDirt.canPlantThisSeedHere)),
            postfix: new HarmonyMethod(typeof(ModPatches), nameof(HoeDirt_canPlantThisSeedHere_postfix)));

        _ = Harmony.Patch(
            AccessTools.DeclaredMethod(typeof(IndoorPot), nameof(IndoorPot.performObjectDropInAction)),
            postfix: new HarmonyMethod(typeof(ModPatches), nameof(IndoorPot_performObjectDropInAction_postfix)));

        _ = Harmony.Patch(
            AccessTools.DeclaredMethod(typeof(JunimoHarvester), nameof(JunimoHarvester.update)),
            transpiler: new HarmonyMethod(typeof(ModPatches), nameof(JunimoHarvester_update_transpiler)));

        _ = Harmony.Patch(
            AccessTools.DeclaredMethod(typeof(SObject), nameof(SObject.IsTeaSapling)),
            postfix: new HarmonyMethod(typeof(ModPatches), nameof(Object_IsTeaSapling_postfix)));

        _ = Harmony.Patch(
            AccessTools.DeclaredMethod(typeof(SObject), nameof(SObject.placementAction)),
            transpiler: new HarmonyMethod(typeof(ModPatches), nameof(Object_placementAction_transpiler)));
    }

    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Harmony.")]
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony.")]
    private static bool Bush_draw_prefix(
        Bush __instance,
        SpriteBatch spriteBatch,
        float ___shakeRotation,
        NetRectangle ___sourceRect,
        float ___yDrawOffset)
    {
        if (!__instance.modData.TryGetValue(Constants.ModDataId, out var id)
            || !Data.TryGetValue(id, out var bushModel))
        {
            return true;
        }

        var x = (__instance.Tile.X * 64) + 32;
        var y = (__instance.Tile.Y * 64) + 64 + ___yDrawOffset;
        if (__instance.drawShadow.Value)
        {
            spriteBatch.Draw(
                Game1.shadowTexture,
                Game1.GlobalToLocal(Game1.viewport, new Vector2(x, y - 4)),
                Game1.shadowTexture.Bounds,
                Color.White,
                0,
                new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y),
                4,
                SpriteEffects.None,
                1E-06f);
        }

        string path;
        if (!__instance.IsSheltered())
        {
            path = bushModel.Texture;
        }
        else if (!string.IsNullOrWhiteSpace(bushModel.IndoorTexture))
        {
            path = bushModel.IndoorTexture;
        }
        else
        {
            path = bushModel.Texture;
        }

        var texture = GetTexture(path);

        spriteBatch.Draw(
            texture,
            Game1.GlobalToLocal(Game1.viewport, new Vector2(x, y)),
            ___sourceRect.Value,
            Color.White,
            ___shakeRotation,
            new Vector2(8, 32),
            4,
            __instance.flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
            ((__instance.getBoundingBox().Center.Y + 48) / 10000f) - (__instance.Tile.X / 1000000f));

        return false;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony.")]
    private static void Bush_GetShakeOffItem_postfix(Bush __instance, ref string __result)
    {
        if (__instance.modData.TryGetValue(Constants.ModDataItem, out var itemId)
            && !string.IsNullOrWhiteSpace(itemId))
        {
            __result = itemId;
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony.")]
    private static void Bush_inBloom_postfix(Bush __instance, ref bool __result)
    {
        var season = __instance.Location.GetSeason();

        if (__instance.modData.TryGetValue(Constants.ModDataItem, out var itemId)
            && !string.IsNullOrWhiteSpace(itemId))
        {
            // Verify the cached item is for the current season
            if (__instance.modData.TryGetValue(Constants.ModDataItemSeason, out var itemSeason)
                && !string.IsNullOrWhiteSpace(itemSeason)
                && itemSeason == nameof(season))
            {
                __result = true;
                return;
            }

            Log.Trace(
                "Cached item's {0} season does not match current {1} season. Clearing cache and recalculating item.",
                itemSeason,
                nameof(season));

            // If the cached item is not for the current season, remove its cached info, and we'll recalculate it below.
            // This saves us having to patch `seasonUpdate()` or `dayUpdate()`.
            // In `dayUpdate()`, the correct `tileSheetOffset` will now be set since `inBloom()` will be accurate.
            __instance.ClearCachedData();
        }

        if (!__instance.modData.TryGetValue(Constants.ModDataId, out var id)
            || !Data.TryGetValue(id, out var bushModel))
        {
            return;
        }

        var dayOfMonth = Game1.dayOfMonth;
        var age = __instance.getAge();

        // Fails basic conditions
        if (age < bushModel.AgeToProduce || dayOfMonth < bushModel.DayToBeginProducing)
        {
            Log.Trace(
                "{0} will not produce. Age: {1} < {2} , Day: {3} < {4}",
                id,
                age.ToString(CultureInfo.InvariantCulture),
                bushModel.AgeToProduce.ToString(CultureInfo.InvariantCulture),
                dayOfMonth.ToString(CultureInfo.InvariantCulture),
                bushModel.DayToBeginProducing.ToString(CultureInfo.InvariantCulture));

            __result = false;
            return;
        }

        Log.Trace(
            "{0} passed basic conditions. Age: {1} >= {2} , Day: {3} >= {4}",
            id,
            age.ToString(CultureInfo.InvariantCulture),
            bushModel.AgeToProduce.ToString(CultureInfo.InvariantCulture),
            dayOfMonth.ToString(CultureInfo.InvariantCulture),
            bushModel.DayToBeginProducing.ToString(CultureInfo.InvariantCulture));

        // Fails default season conditions
        if (!bushModel.Seasons.Any() && season == Season.Winter && !__instance.IsSheltered())
        {
            Log.Trace("{0} will not produce. Season: {1} and plant is outdoors.", id, season.ToString());

            __result = false;
            return;
        }

        if (!bushModel.Seasons.Any())
        {
            Log.Trace("{0} passed default season condition. Season: {1} or plant is indoors.", id, season.ToString());
        }

        // Fails custom season conditions
        if (bushModel.Seasons.Any() && !bushModel.Seasons.Contains(season) && !__instance.IsSheltered())
        {
            Log.Trace(
                "{0} will not produce. Season: {1} not in {2} and plant is outdoors.",
                id,
                season.ToString(),
                string.Join(',', bushModel.Seasons));

            __result = false;
            return;
        }

        if (bushModel.Seasons.Any())
        {
            Log.Trace(
                "{0} passed custom season conditions. Season: {1} in {2} or plant is indoors.",
                id,
                season.ToString(),
                string.Join(',', bushModel.Seasons));
        }

        // Try to produce item
        Log.Trace("{0} attempting to produce random item.", id);
        if (!__instance.TryProduceAny(out var item, bushModel))
        {
            Log.Trace("{0} will not produce. No item was produced.", id);
            __result = false;
            return;
        }

        Log.Trace(
            "{0} selected {1} to grow with quality {2} and quantity {3}.",
            id,
            item.QualifiedItemId,
            item.Quality,
            item.Stack);

        __result = true;
        __instance.modData[Constants.ModDataItem] = item.QualifiedItemId;
        __instance.modData[Constants.ModDataItemSeason] = nameof(season);
        __instance.modData[Constants.ModDataQuality] = item.Quality.ToString(CultureInfo.InvariantCulture);
        __instance.modData[Constants.ModDataStack] = item.Stack.ToString(CultureInfo.InvariantCulture);
    }

    private static IEnumerable<CodeInstruction> Bush_performToolAction_transpiler(
        IEnumerable<CodeInstruction> instructions)
    {
        var method = AccessTools
            .GetDeclaredMethods(typeof(ItemRegistry))
            .First(method => method.Name == nameof(ItemRegistry.Create) && !method.IsGenericMethod);

        return new CodeMatcher(instructions)
            .MatchStartForward(new CodeMatch(instruction => instruction.Calls(method)))
            .RemoveInstruction()
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(ModPatches), nameof(CreateBushItem)))
            .InstructionEnumeration();
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony.")]
    private static void Bush_setUpSourceRect_postfix(Bush __instance, NetRectangle ___sourceRect)
    {
        if (!__instance.modData.TryGetValue(Constants.ModDataId, out var id)
            || !Data.TryGetValue(id, out var bushModel))
        {
            return;
        }

        if (!__instance.IsSheltered())
        {
            __instance.modData[Constants.ModDataTexture] = bushModel.Texture;
        }
        else if (!string.IsNullOrWhiteSpace(bushModel.IndoorTexture))
        {
            __instance.modData[Constants.ModDataTexture] = bushModel.IndoorTexture;
        }
        else
        {
            __instance.modData[Constants.ModDataTexture] = bushModel.Texture;
        }

        var age = __instance.getAge();
        var growthPercent = (float)age / bushModel.AgeToProduce;
        var x = (Math.Min(2, (int)(2 * growthPercent)) + __instance.tileSheetOffset.Value) * 16;
        var y = bushModel.TextureSpriteRow * 16;

        ___sourceRect.Value = new Rectangle(x, y, 16, 32);
    }

    [SuppressMessage("ReSharper", "RedundantAssignment", Justification = "Harmony.")]
    private static void Bush_shake_CreateObjectDebris(
        string id,
        int xTile,
        int yTile,
        int groundLevel,
        int itemQuality,
        float velocityMultiplier,
        GameLocation? location,
        Bush bush)
    {
        // Create cached item
        if (bush.TryGetCachedData(out var itemId, out itemQuality, out var itemStack))
        {
            for (var i = 0; i < itemStack; i++)
            {
                Game1.createObjectDebris(itemId, xTile, yTile, groundLevel, itemQuality, velocityMultiplier, location);
            }

            bush.ClearCachedData();
            return;
        }

        bush.ClearCachedData();

        // Try to create random item
        if (bush.TryProduceAny(out var item))
        {
            Game1.createObjectDebris(
                item.QualifiedItemId,
                xTile,
                yTile,
                groundLevel,
                item.Quality,
                velocityMultiplier,
                location);

            return;
        }

        // Create vanilla item
        Game1.createObjectDebris(id, xTile, yTile, groundLevel, itemQuality, velocityMultiplier, location);
    }

    private static IEnumerable<CodeInstruction> Bush_shake_transpiler(IEnumerable<CodeInstruction> instructions) =>
        new CodeMatcher(instructions)
            .MatchStartForward(
                new CodeMatch(
                    instruction => instruction.Calls(
                        AccessTools.DeclaredMethod(
                            typeof(Game1),
                            nameof(Game1.createObjectDebris),
                            [
                                typeof(string),
                                typeof(int),
                                typeof(int),
                                typeof(int),
                                typeof(int),
                                typeof(float),
                                typeof(GameLocation)
                            ]))))
            .RemoveInstruction()
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(ModPatches), nameof(Bush_shake_CreateObjectDebris)))
            .InstructionEnumeration();

    private static Item CreateBushItem(string itemId, int amount, int quality, bool allowNull, Bush bush)
    {
        if (bush.modData.TryGetValue(Constants.ModDataId, out var bushId))
        {
            itemId = bushId;
        }

        return ItemRegistry.Create(itemId, amount, quality, allowNull);
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony.")]
    private static void GameLocation_CheckItemPlantRules_postfix(
        GameLocation __instance,
        ref bool __result,
        string itemId,
        bool isGardenPot,
        bool defaultAllowed,
        ref string deniedMessage)
    {
        var metadata = ItemRegistry.GetMetadata(itemId);
        if (metadata is null
            || !Data.TryGetValue(metadata.QualifiedItemId, out var bushModel))
        {
            return;
        }

        var parameters = new object[] { bushModel.PlantableLocationRules, isGardenPot, defaultAllowed, null! };
        __result = (bool)CheckItemPlantRules.Invoke(__instance, parameters)!;
        deniedMessage = (string)parameters[3];
    }

    private static Texture2D GetTexture(string path) => getTexture!(path);

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony.")]
    private static void HoeDirt_canPlantThisSeedHere_postfix(string itemId, ref bool __result)
    {
        if (!__result || !Data.ContainsKey($"(O){itemId}"))
        {
            return;
        }

        __result = false;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony.")]
    private static void IndoorPot_performObjectDropInAction_postfix(
        IndoorPot __instance,
        Item dropInItem,
        bool probe,
        ref bool __result)
    {
        if (!Data.ContainsKey(dropInItem.QualifiedItemId)
            || __instance.hoeDirt.Value.crop != null)
        {
            return;
        }

        if (!probe)
        {
            __instance.bush.Value = new Bush(__instance.TileLocation, 3, __instance.Location)
            {
                modData = { [Constants.ModDataId] = dropInItem.QualifiedItemId }, inPot = { Value = true }
            };

            if (!__instance.Location.IsOutdoors)
            {
                __instance.bush.Value.loadSprite();
                _ = Game1.playSound("coin");
            }
        }

        __result = true;
    }

    private static Item JunimoHarvester_update_CreateItem(Item i, Bush bush)
    {
        // Return cached item
        if (bush.TryGetCachedData(out var itemId, out var itemQuality, out var itemStack))
        {
            return ItemRegistry.Create(itemId, itemStack, itemQuality);
        }

        // Try to return random item else return vanilla item
        return bush.TryProduceAny(out var item) ? item : i;
    }

    private static IEnumerable<CodeInstruction>
        JunimoHarvester_update_transpiler(IEnumerable<CodeInstruction> instructions) =>
        new CodeMatcher(instructions)
            .MatchEndForward(
                new CodeMatch(OpCodes.Ldstr, "(O)815"),
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(OpCodes.Ldc_I4_0),
                new CodeMatch(OpCodes.Ldc_I4_0))
            .Advance(2)
            .Insert(
                new CodeInstruction(OpCodes.Ldloc_S, (short)7),
                CodeInstruction.Call(typeof(ModPatches), nameof(JunimoHarvester_update_CreateItem)))
            .InstructionEnumeration();

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony.")]
    private static void Object_IsTeaSapling_postfix(SObject __instance, ref bool __result)
    {
        if (__result)
        {
            return;
        }

        if (Data.ContainsKey(__instance.QualifiedItemId))
        {
            __result = true;
        }
    }

    private static Bush Object_placementAction_AddModData(Bush bush, SObject obj)
    {
        if (!Data.ContainsKey(obj.QualifiedItemId))
        {
            return bush;
        }

        bush.modData[Constants.ModDataId] = obj.QualifiedItemId;
        bush.setUpSourceRect();
        return bush;
    }

    private static IEnumerable<CodeInstruction>
        Object_placementAction_transpiler(IEnumerable<CodeInstruction> instructions) =>
        new CodeMatcher(instructions)
            .MatchEndForward(
                new CodeMatch(
                    OpCodes.Newobj,
                    AccessTools.Constructor(
                        typeof(Bush),
                        [typeof(Vector2), typeof(int), typeof(GameLocation), typeof(int)])))
            .Advance(1)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(ModPatches), nameof(Object_placementAction_AddModData)))
            .InstructionEnumeration();
}
