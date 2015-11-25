using System;
using System.Linq;
using Ensage;
using SharpDX;
using Ensage.Common.Extensions;
using Ensage.Common;
using Ensage.Common.Menu;
using System.Windows.Input;
using SharpDX.Direct3D9;


namespace QoP
{
    class Program
    {
        private static bool _activated;
        private static Ability ss, blink, scream, sonic;
        private static Item orchid, hex, bkb;
        private static Font _text;
        private const Key KeyCombo = Key.Space;
        private const Key BkbToggleKey = Key.F;
        private static bool _bkbToggle;
        private static Hero _me;
        private static Hero _target;
        static void Main(string[] args)
        {
            Game.OnUpdate += Game_OnUpdate;
            Game.OnWndProc += Game_OnWndProc;
            Drawing.OnPreReset += Drawing_OnPreReset;
            Drawing.OnPostReset += Drawing_OnPostReset;
            Drawing.OnEndScene += Drawing_OnEndScene;
            AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;
            Console.WriteLine("==========>[QoP#] loaded<=========");
            #region _text:Description
            _text = new Font(
               Drawing.Direct3DDevice9,
               new FontDescription
               {
                   FaceName = "Segoe UI",
                   Height = 17,
                   OutputPrecision = FontPrecision.Default,
                   Quality = FontQuality.ClearType
               });
            #endregion
        }
        public static void Game_OnUpdate(EventArgs args)
        {
            if (!Game.IsInGame) return;
            _me = ObjectMgr.LocalHero;
            if (_me == null || _me.ClassID != ClassID.CDOTA_Unit_Hero_QueenOfPain)
            {
                return;
            }
            if (!_activated) return;
            if ((_target == null || !_target.IsVisible))
                _me.Move(Game.MousePosition);
            if (_target == null || _me.Distance2D(_target) > 1300)
                _target = _me.ClosestToMouseTarget(1000);
            //Skills & Items
            ss = _me.Spellbook.Spell1; //Shadow Strike
            blink = _me.Spellbook.Spell2; //Blink
            scream = _me.Spellbook.Spell3; //Scream of Pain
            sonic = _me.Spellbook.Spell4; // Sonic Wave
            orchid = _me.FindItem("item_orchid"); //Orchid [Silence]
            hex = _me.FindItem("item_sheepstick"); //Hex [Disable]
            bkb = _me.FindItem("item_black_king_bar"); //BKB [Magic Immune]
            if (_target != null && _target.IsAlive && !_target.IsIllusion && !_target.IsMagicImmune())
            {
                if (!Utils.SleepCheck("QoP")) return;
                //Determine Distance from Target
                var targetDistance = _me.Distance2D(_target);
                var attackRange = 550;
                var screamRange = 475;
                //Blink to Target
                if (targetDistance >= attackRange && blink != null && blink.CanBeCasted() && Utils.SleepCheck("blink"))
                {
                    blink.UseAbility(_target.Position);
                    Utils.Sleep(150 + Game.Ping, "blink");
                }
                //Check to Use BKB
                else if (bkb !=null && _bkbToggle && Utils.SleepCheck("BKB"))
                {
                    bkb.UseAbility();
                    Utils.Sleep(150 + Game.Ping, "BKB");
                }
                //Use Orchid
                else if (targetDistance <= attackRange && orchid != null && orchid.CanBeCasted() && Utils.SleepCheck("orchid"))
                {
                    orchid.UseAbility(_target);
                    Utils.Sleep(150 + Game.Ping, "orchid");
                }
                //Use Hex
                else if (targetDistance <= attackRange && hex != null && hex.CanBeCasted() && Utils.SleepCheck("hex"))
                {
                    hex.UseAbility(_target);
                    Utils.Sleep(150 + Game.Ping, "hex");
                }
                //Use Shadow Strike
                else if (ss != null && ss.CanBeCasted() && Utils.SleepCheck("ss"))
                {
                    ss.UseAbility(_target);
                    Utils.Sleep(150 + Game.Ping, "ss");
                }
                //Use Scream of Pain
                else if (targetDistance <= screamRange && scream != null && scream.CanBeCasted() && Utils.SleepCheck("scream"))
                {
                    scream.UseAbility();
                    Utils.Sleep(150 + Game.Ping, "scream");
                }
                //Use Sonic Wave
                else if (sonic != null && sonic.CanBeCasted() && Utils.SleepCheck("sonic"))
                {
                    sonic.UseAbility(_target.Position);
                    Utils.Sleep(150 + Game.Ping, "sonic");
                }
                else if (nothingCanCast())
                {
                    _me.Attack(_target);
                }
            }
        }
        private static bool nothingCanCast()
        {
            if (!ss.CanBeCasted() &&
                !blink.CanBeCasted() &&
                !scream.CanBeCasted() &&
                !sonic.CanBeCasted() &&
                !orchid.CanBeCasted() &&
                !hex.CanBeCasted())
                return true;
            else
            {
                return false;
            }
        }
        //Toggle BKB Key
        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (Game.IsChatOpen) return;
            _activated = Game.IsKeyDown(KeyCombo);
            if (!Game.IsKeyDown(BkbToggleKey) || !Utils.SleepCheck("toggleBKB")) return;
            _bkbToggle = !_bkbToggle;
            Utils.Sleep(250, "toggleBKB");
        }
        static void Drawing_OnEndScene(EventArgs args)
        {
            if (Drawing.Direct3DDevice9 == null || Drawing.Direct3DDevice9.IsDisposed || !Game.IsInGame) return;
            if (_me == null || _me.Player.Team == Team.Observer || _me.ClassID != ClassID.CDOTA_Unit_Hero_QueenOfPain) return;
            if (_activated)
            {
                _text.DrawText(null, "[QoP#] is COMBOING!\n", 5, 50, Color.Green);
            }
            if (!_activated && !_bkbToggle)
            {
                _text.DrawText(null, "[QoP#]: Use  [" + KeyCombo + "] for combo. BKB Disabled. Use " + BkbToggleKey + " to turn it on!", 5, 50, Color.White);
            }
            if (!_activated && _bkbToggle)
            {
                _text.DrawText(null, "[QoP#]: Use  [" + KeyCombo + "] for combo. BKB Enabled. Use " + BkbToggleKey + " to turn it off!", 5, 50, Color.Yellow);
            }
        }
        static void CurrentDomain_DomainUnload(object sender, EventArgs e)
        {
            _text.Dispose();
        }
        static void Drawing_OnPostReset(EventArgs args)
        {
            _text.OnResetDevice();
        }
        static void Drawing_OnPreReset(EventArgs args)
        {

            _text.OnLostDevice();
        }

    }
}
