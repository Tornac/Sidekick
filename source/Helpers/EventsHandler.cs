using Gma.System.MouseKeyHook;
using Sidekick.Helpers;
using Sidekick.Helpers.NativeMethods;
using Sidekick.Helpers.POETradeAPI;
using Sidekick.Windows.Overlay;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sidekick.Helpers
{
    public static class EventsHandler
    {
        private static IKeyboardMouseEvents _globalHook;

        public static void Initialize()
        {
            _globalHook = Hook.GlobalEvents();
            _globalHook.KeyDown += GlobalHookKeyPressHandler;
        }

        private static void GlobalHookKeyPressHandler(object sender, KeyEventArgs e)
        {
            if (!TradeClient.IsReady)
            {
                return;
            }

            if (OverlayController.IsDisplayed && e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                OverlayController.Hide();
            }
            else if (!OverlayController.IsDisplayed && e.Modifiers == Keys.Control && e.KeyCode == Keys.D)
            {
                if (!ProcessHelper.IsPathOfExileInFocus())
                    return;

                e.Handled = true;
                Task.Run(TriggerItemFetch);
            }
            else if (!OverlayController.IsDisplayed && e.Modifiers == (Keys.Control | Keys.Shift) && e.KeyCode == Keys.D)
            {
                if (!ProcessHelper.IsPathOfExileInFocus())
                    return;

                e.Handled = true;
                Task.Run(TriggerBulkQuery);
            }
        }

        private static Item ParseItemUnderCursor()
        {
            SendKeys.SendWait("^{c}");
            Thread.Sleep(10); // without this, it seems to sometimes get outdated clipboard data
            return ItemParser.ParseItem(ClipboardHelper.GetText());
        }

        private static async void TriggerItemFetch()
        {
            Logger.Log("Hotkey pressed.");
            var item = ParseItemUnderCursor();
            if (item != null)
            {
                OverlayController.SetPosition(Cursor.Position.X, Cursor.Position.Y);
                OverlayController.Show();

                var queryResult = await TradeClient.GetListings(item);
                if (queryResult != null)
                {
                    OverlayController.SetQueryResult(queryResult);
                    return;
                }
            }

            OverlayController.Hide();
        }

        private static async void TriggerBulkQuery()
        {
            var item = ParseItemUnderCursor();
            if (item == null) return;
            var result = await TradeClient.BulkQuery(item);
            var text = string.Join("\n", from l in result
                                         select $"{l.Ratio.BuyerReceives}c for {l.Ratio.SellerGives}, {l.Ratio.UnitPrice} each, stock: {l.Stock} seller: {l.SellerAccountName}");
            // TODO: replace with a proper WPF window
            MessageBox.Show($"Bulk Listings for {item.Name}\n\n{text}");
        }

        public static void Dispose()
        {
            _globalHook?.Dispose();
        }
    }
}
