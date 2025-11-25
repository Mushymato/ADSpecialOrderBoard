using System.Diagnostics;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.SpecialOrders;

namespace ADSpecialOrderBoard;

public class OrderBoardData
{
    public string? Background { get; set; } = null;
    public Rectangle TextArea { get; set; } = Rectangle.Empty;
    public Rectangle ImageArea { get; set; } = Rectangle.Empty;
    public string? DefaultRequesterTexture { get; set; } = null;
    public Rectangle DefaultRequesterSourceRect { get; set; } = new Rectangle(0, 0, 16, 19);
    public string? ButtonTexture { get; set; } = null;
    public Rectangle ButtonSourceRect { get; set; } = Rectangle.Empty;
    public string? TimeLeftClockTexture { get; set; } = null;
    public Rectangle TimeLeftClockSourceRect { get; set; } = Rectangle.Empty;
}

public sealed class ModEntry : Mod
{
#if DEBUG
    private const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Debug;
#else
    private const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Trace;
#endif

    public const string ModId = "mushymato.ADSpecialOrderBoard";
    public const string SpecialOrder_CustomField_Image = $"{ModId}/Image";
    public const string DataAssetPrefix = $"{ModId}/Boards";
    public const string TileAction = $"{ModId}_Show";
    private static IMonitor? mon;

    private static readonly HashSet<string> hasRefreshedToday = [];

    internal static OrderBoardData? LoADSpecialOrderBoardData(string orderBoardId)
    {
        string assetName = $"{DataAssetPrefix}/{orderBoardId}";
        if (Game1.content.DoesAssetExist<OrderBoardData>(assetName))
        {
            return Game1.content.Load<OrderBoardData>(assetName);
        }
        return null;
    }

    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        mon = Monitor;
        helper.Events.Content.AssetRequested += OnAssetRequested;

        GameLocation.RegisterTileAction(TileAction, TileActionShowQuestBoard);
    }

    internal static void RefreshSpecialOrders(string orderType)
    {
        SpecialOrder.UpdateAvailableSpecialOrders(orderType, forceRefresh: !hasRefreshedToday.Contains(orderType));
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsDirectlyUnderPath(DataAssetPrefix))
        {
            e.LoadFrom(() => new OrderBoardData(), AssetLoadPriority.Exclusive);
        }
    }

    private bool TileActionShowQuestBoard(GameLocation location, string[] args, Farmer farmer, Point point)
    {
        if (!ArgUtility.TryGet(args, 1, out string questBoardId, out string error, name: "string boardId"))
        {
            Log(error, LogLevel.Error);
            return false;
        }
        if (OrderBoardUI.MakeOrderBoard(questBoardId) is OrderBoardUI questBoardUI)
        {
            Game1.activeClickableMenu = questBoardUI;
            return true;
        }
        return false;
    }

    /// <summary>SMAPI static monitor Log wrapper</summary>
    /// <param name="msg"></param>
    /// <param name="level"></param>
    internal static void Log(string msg, LogLevel level = DEFAULT_LOG_LEVEL)
    {
        mon!.Log(msg, level);
    }

    /// <summary>SMAPI static monitor LogOnce wrapper</summary>
    /// <param name="msg"></param>
    /// <param name="level"></param>
    internal static void LogOnce(string msg, LogLevel level = DEFAULT_LOG_LEVEL)
    {
        mon!.LogOnce(msg, level);
    }

    /// <summary>SMAPI static monitor Log wrapper, debug only</summary>
    /// <param name="msg"></param>
    /// <param name="level"></param>
    [Conditional("DEBUG")]
    internal static void LogDebug(string msg, LogLevel level = DEFAULT_LOG_LEVEL)
    {
        mon!.Log(msg, level);
    }
}
