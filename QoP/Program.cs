using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using Ensage.Common.Menu;
using SharpDX;

namespace QoP
{
    class Program
    {
        #region Members || Menu
        //============================================================
        private static bool _loaded;
        private static readonly string Ver = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        //============================================================
        private static readonly Menu Menu = new Menu("[QoP#]", "[QoP#]", true);//Common.Menu
        private static Hero _target;
        private static Hero me;
        #endregion

        private static void Main()
        {
            Game.OnUpdate += Game_OnUpdate;
            _loaded = false;
            Menu.AddItem(new MenuItem("comboKey", "Combo Key").SetValue(new KeyBind('D',KeyBindType.Press)).SetTooltip("Hold this key for combo"));
            var dict = new Dictionary<string, bool>
            {
                {"item_black_king_bar", false},
                {"item_sheepstick", false},
                {"item_orchid", true},
                {"item_mask_of_madness", false},
                {"item_mjollnir", false},
                {"item_shivas_guard", false}
            };
            Menu.AddItem(new MenuItem("enabledAbilities", "Items:").SetValue(new AbilityToggler(dict)));
			Menu.AddItem(new MenuItem("shadow", "Invisible:").SetValue(true).SetTooltip("use Shadow Blade & Silver Edge"));
            Menu.AddToMainMenu();
        }


        private static void Game_OnUpdate(EventArgs args)
        {
            #region Init
            if (!_loaded)
            {
				me = ObjectMgr.LocalHero;
                if (!Game.IsInGame || me == null || me.ClassID != ClassID.CDOTA_Unit_Hero_QueenOfPain)
                {
                    return;
                }
                _loaded = true;
				#region Game Message Print
                Game.PrintMessage(
                    "<font color='#3377ff'>[QoP#]</font>: loaded! <font face='Tahoma' size='9'>(</font>",
                    MessageType.ChatMessage);
				#endregion
            }
            if (!Game.IsInGame || me == null)
            {
                _loaded = false;
                PrintInfo("===>[QoP#] Unloaded<===");
                return;
            }
            if (Game.IsPaused)
            {
                return;
            }
            #endregion

            #region Lets combo
            if (!Menu.Item("comboKey").GetValue<KeyBind>().Active)
            {
                _target = null;
                return;
            }
            if (_target == null || !_target.IsValid)
            {
                _target = ClosestToMouse(me);
            }
            if (_target == null || !_target.IsValid || !_target.IsAlive || !me.CanCast()) return;
            
            ComboInAction(me, _target);
            #endregion
        }
		#region ComboInAction
        private static void ComboInAction(Hero me, Hero target)
        {
            if (!Utils.SleepCheck("nextAction")) return;
            var ss = me.Spellbook.Spell1; //Shadow Strike
			var blink = me.Spellbook.Spell2; //Blink
			var scream = me.Spellbook.Spell3; //Scream of Pain
			var sonic = me.Spellbook.Spell4; // Sonic Wave
			//orchid = _me.FindItem("item_orchid"); //Orchid [Silence]
			//hex = _me.FindItem("item_sheepstick"); //Hex [Disable]
			//bkb = _me.FindItem("item_black_king_bar"); //BKB [Magic Immune]
			//mask = _me.FindItem("item_mask_of_madness"); //Mask of Madness [Attack Speed]
			//mjo = _me.FindItem("item_mjollnir"); //Mjollnir Buff [AoE lightning]
			//shivas = _me.FindItem("item_shivas_guard"); //Shivas [AoE Slow]
            var neededMana = me.Mana - sonic.ManaCost;
            var allitems = me.Inventory.Items.Where(x => x.CanBeCasted() && x.ManaCost <= neededMana);
            var enumerable = allitems as Item[] ?? allitems.ToArray();
            var isInvis = me.IsInvisible();
            var itemOnTarget =
                enumerable.FirstOrDefault(
                    x =>
                        x.Name == "item_orchid" ||
                        x.Name == "item_sheepstick");
            var itemWithOutTarget = enumerable.FirstOrDefault(
                    x =>
						x.Name == "item_silver_edge" || x.Name == "item_invis_sword");
            var itemOnMySelf = enumerable.FirstOrDefault(
                x =>
                    x.Name == "item_mjollnir");
            Item bkb = null;
            if (Menu.Item("enabledAbilities").GetValue<AbilityToggler>().IsEnabled("item_black_king_bar"))
            {
                bkb = me.FindItem("item_black_king_bar");
            }
            var distance = me.Distance2D(target); //Target Distance
            var attackRange = 550;			  //Attack Range
            if (distance >= 1350)             //Move to Target if Distance too far to blink
            {
                me.Move(target.Position);
                Utils.Sleep(200 + Game.Ping, "nextAction");
                return;
            }
			//Mjollnir
            if (itemOnMySelf != null && Menu.Item("enabledAbilities").GetValue<AbilityToggler>().IsEnabled("item_mjollnir"))
            {
                itemOnMySelf.UseAbility(me);
                Utils.Sleep(50 + Game.Ping, "nextAction");
                return;
            }
			//Shadow Blade & Silver Edge
            if (itemWithOutTarget != null && distance <= attackRange && Menu.Item("shadow").GetValue<bool>())
            {
                itemWithOutTarget.UseAbility();
                Utils.Sleep(100 + Game.Ping, "nextAction");
                return;
            }
			//Blink to Target
            if (blink != null && blink.CanBeCasted() && !isInvis && distance >= attackRange && Utils.SleepCheck("blink"))
            {
                var point = new Vector3(
                    (float)(target.Position.X - 20 * Math.Cos(me.FindAngleBetween(target.Position, true))),
                    (float)(target.Position.Y - 20 * Math.Sin(me.FindAngleBetween(target.Position, true))),
                    target.Position.Z);
                blink.UseAbility(point);
                Utils.Sleep(200 + Game.Ping, "blink");
                return;
            }
			//Invisible Attack
			if (isInvis && distance <= attackRange)
            {
                me.Attack(target);
                Utils.Sleep(200 + Game.Ping, "nextAction");
            }
            Utils.Sleep(10, "nextAction");
			//Orchid & Hex
            if (itemOnTarget != null && !isInvis)
            {
                itemOnTarget.UseAbility(target);
                Utils.Sleep(50 + Game.Ping, "nextAction");
                return;
            }
			//BKB 
            if (Menu.Item("enabledAbilities").GetValue<AbilityToggler>().IsEnabled("item_black_king_bar") && bkb != null && bkb.CanBeCasted() && Utils.SleepCheck("bkb") && !isInvis)
            {
                bkb.UseAbility();
                Utils.Sleep(35+Game.Ping, "bkb");
                return;
            }
			//Spells
			if (ss != null && ss.CanBeCasted() && distance <= ss.CastRange)
			{
                ss.UseAbility(target);
                Utils.Sleep(150 + Game.Ping, "nextAction");
                return;
            }
			if (scream != null && scream.CanBeCasted() && distance <= scream.CastRange)
			{
                scream.UseAbility();
                Utils.Sleep(150 + Game.Ping, "nextAction");
                return;
            }
			if (sonic != null && sonic.CanBeCasted() && distance <= sonic.CastRange)
			{
                sonic.UseAbility(target.Position);
                Utils.Sleep(150 + Game.Ping, "nextAction");
                return;
            }
        }
		#endregion
		#region ClosestToMouse
        public static Hero ClosestToMouse(Hero source, float range = 600)
        {
            var mousePosition = Game.MousePosition;
            var enemyHeroes =
                ObjectMgr.GetEntities<Hero>()
                    .Where(
                        x =>
                            x.Team == source.GetEnemyTeam() && !x.IsIllusion && x.IsAlive && x.IsVisible
                            && x.Distance2D(mousePosition) <= range);
            Hero[] closestHero = { null };
            foreach (var enemyHero in enemyHeroes.Where(enemyHero => closestHero[0] == null || closestHero[0].Distance2D(mousePosition) > enemyHero.Distance2D(mousePosition)))
            {
                closestHero[0] = enemyHero;
            }
            return closestHero[0];
        }
		#endregion
        #region Helpers
        public static void PrintInfo(string text, params object[] arguments)
        {
            PrintEncolored(text, ConsoleColor.White, arguments);
        }

        public static void PrintSuccess(string text, params object[] arguments)
        {
            PrintEncolored(text, ConsoleColor.Green, arguments);
        }

        public static void PrintError(string text, params object[] arguments)
        {
            PrintEncolored(text, ConsoleColor.Red, arguments);
        }

        public static void PrintEncolored(string text, ConsoleColor color, params object[] arguments)
        {
            var clr = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(text, arguments);
            Console.ForegroundColor = clr;
        }
        #endregion

    }
}