using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using StardewValley.SpecialOrders;
using StardewValley.TokenizableStrings;

namespace ADSpecialOrderBoard;

public sealed class OrderBoardUI : IClickableMenu
{
    private const int SCALE = 4;
    private readonly Texture2D bgTexture;
    public SpecialOrder? currentOrder;
    public readonly string orderBoardId;
    public readonly string orderType;
    public readonly OrderBoardData orderBoardData;

    private Rectangle textArea = Rectangle.Empty;
    private readonly ClickableComponent? acceptButtonCC = null;
    private readonly string acceptButtonText = Game1.content.LoadString("Strings\\UI:AcceptQuest");
    private readonly Texture2D acceptButtonTexture = Game1.mouseCursors;
    private readonly Rectangle acceptButtonSourceRect = new(403, 373, 9, 9);
    private readonly string daysLeftText = Game1.content.LoadString("Strings\\StringsFromCSFiles:QuestLog.cs.11374");
    private Vector2 daysLeftTextPos = Vector2.Zero;

    private Rectangle imageArea = Rectangle.Empty;
    private readonly Texture2D? orderImage = null;
    private readonly Rectangle orderImageSourceRect = Rectangle.Empty;

    private Rectangle requesterMugShotSourceRect = Rectangle.Empty;
    private readonly Texture2D? requesterTexture = null;

    public static OrderBoardUI? MakeOrderBoard(string orderBoardId)
    {
        if (ModEntry.LoADSpecialOrderBoardData(orderBoardId) is OrderBoardData orderBoardData)
        {
            if (
                !string.IsNullOrEmpty(orderBoardData.Background)
                && Game1.content.Load<Texture2D>(orderBoardData.Background) is Texture2D bgTexture
            )
            {
                return new OrderBoardUI(orderBoardId, orderBoardData, bgTexture);
            }
        }
        else
        {
            ModEntry.Log($"Could not open order board '{orderBoardId}'");
        }
        return null;
    }

    public OrderBoardUI(string orderBoardId, OrderBoardData orderBoardData, Texture2D bgTexture)
        : base(0, 0, 0, 0, showUpperRightCloseButton: false)
    {
        this.orderBoardId = orderBoardId;
        this.orderBoardData = orderBoardData;
        this.bgTexture = bgTexture;
        orderType = $"{ModEntry.ModId}/{orderBoardId}";

        Vector2 topLeftPositionForCenteringOnScreen = Utility.getTopLeftPositionForCenteringOnScreen(width, height);
        xPositionOnScreen = (int)topLeftPositionForCenteringOnScreen.X - bgTexture.Bounds.Width * SCALE / 2;
        yPositionOnScreen = (int)topLeftPositionForCenteringOnScreen.Y - bgTexture.Bounds.Height * SCALE / 2;

        bool hasAcceptedOrder = Game1.player.team.acceptedSpecialOrderTypes.Contains(orderType);
        if (hasAcceptedOrder)
        {
            foreach (SpecialOrder specialOrder in Game1.player.team.specialOrders)
            {
                if (
                    specialOrder.orderType.Value == orderType
                    && specialOrder.questState.Value == SpecialOrderStatus.InProgress
                )
                {
                    currentOrder = specialOrder;
                    break;
                }
            }
            hasAcceptedOrder = currentOrder != null;
        }
        if (!hasAcceptedOrder)
        {
            ModEntry.RefreshSpecialOrders(orderType);
            currentOrder = Game1.player.team.GetAvailableSpecialOrder(0, orderType);
            currentOrder.SetDuration(currentOrder.questDuration.Value);
        }

        if (currentOrder == null)
        {
            return;
        }

        textArea = new(
            xPositionOnScreen + orderBoardData.TextArea.X * SCALE,
            yPositionOnScreen + orderBoardData.TextArea.Y * SCALE,
            orderBoardData.TextArea.Width * SCALE,
            orderBoardData.TextArea.Height * SCALE
        );

        Vector2 acceptQuestTextLen = Game1.dialogueFont.MeasureString(acceptButtonText);
        acceptButtonCC = new ClickableComponent(
            new Rectangle(
                (int)(textArea.Right - acceptQuestTextLen.X - 24),
                (int)(textArea.Bottom - acceptQuestTextLen.Y / 2 - 36),
                (int)acceptQuestTextLen.X + 24,
                (int)acceptQuestTextLen.Y + 24
            ),
            ""
        )
        {
            myID = 0,
            leftNeighborID = -99998,
            rightNeighborID = -99998,
            upNeighborID = -99998,
            downNeighborID = -99998,
            visible = !hasAcceptedOrder,
        };
        if (!string.IsNullOrEmpty(orderBoardData.ButtonTexture))
        {
            acceptButtonTexture = Game1.content.Load<Texture2D>(orderBoardData.ButtonTexture);
            acceptButtonSourceRect = orderBoardData.ButtonSourceRect;
        }

        int daysLeft = currentOrder.GetDaysLeft();
        daysLeftText = Game1.parseText(
            (daysLeft > 1)
                ? Game1.content.LoadString("Strings\\StringsFromCSFiles:QuestLog.cs.11374", daysLeft)
                : Game1.content.LoadString("Strings\\StringsFromCSFiles:QuestLog.cs.11375", daysLeft),
            Game1.dialogueFont,
            textArea.Width
        );
        daysLeftTextPos = new Vector2(textArea.X + 48 + 8, textArea.Bottom - acceptQuestTextLen.Y / 2 - 24);

        if (
            !string.IsNullOrEmpty(currentOrder.requester.Value)
            && Game1.getCharacterFromName(currentOrder.requester.Value) is NPC requesterNPC
        )
        {
            requesterTexture = requesterNPC.Sprite.Texture;
            requesterMugShotSourceRect = requesterNPC.getMugShotSourceRect();
            requesterMugShotSourceRect.Height -= 5;
        }
        else if (!string.IsNullOrEmpty(orderBoardData.DefaultRequesterTexture))
        {
            requesterTexture = Game1.content.Load<Texture2D>(orderBoardData.DefaultRequesterTexture);
            requesterMugShotSourceRect = orderBoardData.DefaultRequesterSourceRect;
        }

        if (
            currentOrder
                .GetData()
                ?.CustomFields?.TryGetValue(ModEntry.SpecialOrder_CustomField_Image, out string? questImage) ?? false
        )
        {
            string[] imageArgs = ArgUtility.SplitBySpaceQuoteAware(questImage);
            if (
                ArgUtility.TryGet(imageArgs, 0, out string imagePath, out _, name: "string image")
                && Game1.content.DoesAssetExist<Texture2D>(imagePath)
            )
            {
                orderImage = Game1.content.Load<Texture2D>(imagePath);
                if (
                    !ArgUtility.TryGetRectangle(
                        imageArgs,
                        1,
                        out orderImageSourceRect,
                        out _,
                        name: "Rectange orderImageSourceRect"
                    )
                )
                {
                    orderImageSourceRect = orderImage.Bounds;
                }
                imageArea = new(
                    xPositionOnScreen + orderBoardData.ImageArea.X * SCALE,
                    yPositionOnScreen + orderBoardData.ImageArea.Y * SCALE,
                    orderBoardData.ImageArea.Width * SCALE,
                    orderBoardData.ImageArea.Height * SCALE
                );
            }
        }
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);

        Vector2 topLeftPositionForCenteringOnScreen = Utility.getTopLeftPositionForCenteringOnScreen(width, height);
        xPositionOnScreen = (int)topLeftPositionForCenteringOnScreen.X - bgTexture.Bounds.Width * SCALE / 2;
        yPositionOnScreen = (int)topLeftPositionForCenteringOnScreen.Y - bgTexture.Bounds.Height * SCALE / 2;

        if (!textArea.IsEmpty)
            textArea = new(
                xPositionOnScreen + orderBoardData.TextArea.X * SCALE,
                yPositionOnScreen + orderBoardData.TextArea.Y * SCALE,
                orderBoardData.TextArea.Width * SCALE,
                orderBoardData.TextArea.Height * SCALE
            );

        Vector2 acceptQuestTextLen = Game1.dialogueFont.MeasureString(acceptButtonText);
        if (acceptButtonCC != null)
        {
            acceptButtonCC.bounds = new Rectangle(
                (int)(textArea.Right - acceptQuestTextLen.X - 12),
                (int)(textArea.Bottom - acceptQuestTextLen.Y / 2 - 24),
                (int)acceptQuestTextLen.X + 24,
                (int)acceptQuestTextLen.Y + 24
            );
        }
        daysLeftTextPos = new Vector2(textArea.X + 48 + 8, textArea.Bottom - acceptQuestTextLen.Y / 2 - 24);

        if (!imageArea.IsEmpty)
            imageArea = new(
                xPositionOnScreen + orderBoardData.ImageArea.X * SCALE,
                yPositionOnScreen + orderBoardData.ImageArea.Y * SCALE,
                orderBoardData.ImageArea.Width * SCALE,
                orderBoardData.ImageArea.Height * SCALE
            );
    }

    public override void performHoverAction(int x, int y)
    {
        base.performHoverAction(x, y);
        if (acceptButtonCC?.visible ?? false)
        {
            float scale = acceptButtonCC.scale;
            acceptButtonCC.scale = acceptButtonCC.bounds.Contains(x, y) ? 1.5f : 1f;
            if (acceptButtonCC.scale > scale)
            {
                Game1.playSound("Cowboy_gunshot");
            }
        }
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (currentOrder != null && (acceptButtonCC?.visible ?? false) && acceptButtonCC.containsPoint(x, y))
        {
            Game1.playSound("newArtifact");
            Game1.player.team.acceptedSpecialOrderTypes.Add(orderType);
            Game1.player.team.AddSpecialOrder(currentOrder.questKey.Value, currentOrder.generationSeed.Value);
            Game1.Multiplayer.globalChatInfoMessage(
                "AcceptedSpecialOrder",
                Game1.player.Name,
                TokenStringBuilder.SpecialOrderName(currentOrder.questKey.Value)
            );
            acceptButtonCC.visible = false;
            acceptButtonCC.scale = 1f;
            return;
        }
        base.receiveLeftClick(x, y, playSound);
    }

    public override void draw(SpriteBatch b)
    {
        if (!Game1.options.showClearBackgrounds)
        {
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);
        }

        b.Draw(
            bgTexture,
            new Vector2(xPositionOnScreen, yPositionOnScreen),
            bgTexture.Bounds,
            Color.White,
            0f,
            Vector2.Zero,
            4f,
            SpriteEffects.None,
            1f
        );

        base.draw(b);

        if (currentOrder != null)
        {
            SpriteFont spriteFont =
                (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ko)
                    ? Game1.smallFont
                    : Game1.dialogueFont;
            Color textColor = Game1.textColor;
            float intensity = 0.5f;
            float alpha = 1f;
            if (!(acceptButtonCC?.visible ?? false))
            {
                alpha = 0.4f;
                textColor *= alpha;
                intensity = 0.1f;
            }

            float textY = textArea.Y;
            string name = currentOrder.GetName();
            Vector2 nameSize = spriteFont.MeasureString(name);
            Utility.drawTextWithShadow(
                b,
                name,
                spriteFont,
                new Vector2(textArea.X + (textArea.Width - nameSize.X) / 2f, textY),
                textColor,
                1f,
                -1f,
                -1,
                -1,
                intensity
            );
            textY += 18 + nameSize.Y;

            string description = currentOrder.GetDescription();
            string text = Game1.parseText(description, spriteFont, textArea.Width);
            Utility.drawTextWithShadow(
                b,
                text,
                spriteFont,
                new Vector2(textArea.X, textY),
                textColor,
                shadowIntensity: intensity
            );

            Utility.drawWithShadow(
                b,
                Game1.mouseCursors,
                new Vector2(daysLeftTextPos.X - 48, daysLeftTextPos.Y),
                new Rectangle(410, 501, 9, 9),
                Color.White * alpha,
                0f,
                Vector2.Zero,
                4f,
                flipped: false,
                0.99f,
                -1,
                -1,
                intensity * 0.6f
            );
            Utility.drawTextWithShadow(
                b,
                daysLeftText,
                Game1.dialogueFont,
                daysLeftTextPos,
                textColor,
                shadowIntensity: intensity
            );

            if (orderImage != null)
            {
                b.Draw(orderImage, imageArea, orderImageSourceRect, Color.White);
            }

            if (acceptButtonCC != null)
            {
                if (requesterTexture != null)
                {
                    b.Draw(
                        requesterTexture,
                        new Vector2(
                            acceptButtonCC.bounds.Right - requesterMugShotSourceRect.Width * 4 - 16,
                            acceptButtonCC.bounds.Y - requesterMugShotSourceRect.Height * 4
                        ),
                        requesterMugShotSourceRect,
                        Color.White * alpha,
                        0f,
                        Vector2.Zero,
                        4f,
                        SpriteEffects.None,
                        1f
                    );
                }

                drawTextureBox(
                    b,
                    acceptButtonTexture,
                    acceptButtonSourceRect,
                    acceptButtonCC.bounds.X,
                    acceptButtonCC.bounds.Y,
                    acceptButtonCC.bounds.Width,
                    acceptButtonCC.bounds.Height,
                    (acceptButtonCC.scale > 1f) ? Color.LightPink : Color.White * alpha,
                    4f * acceptButtonCC.scale,
                    drawShadow: acceptButtonCC.visible
                );
                Utility.drawTextWithShadow(
                    b,
                    acceptButtonText,
                    Game1.dialogueFont,
                    new Vector2(
                        acceptButtonCC.bounds.X + 12,
                        acceptButtonCC.bounds.Y + (LocalizedContentManager.CurrentLanguageLatin ? 16 : 12)
                    ),
                    textColor,
                    shadowIntensity: intensity
                );
            }
        }

        Game1.mouseCursorTransparency = 1f;
        if (!Game1.options.SnappyMenus || currentOrder != null)
        {
            drawMouse(b);
        }
    }
}
