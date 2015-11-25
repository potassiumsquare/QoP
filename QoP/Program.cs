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
        private static float targetDistance;
        private static double turnTime;
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
                {"item_mjollnir", false},
                {"item_shivas_guard", false}
            };
            Menu.AddItem(new MenuItem("enabledAbilities", "Items:").SetValue(new AbilityToggler(dict)));
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
                    "<font color='#3377ff'>[QoP#]</font>: loaded! <font face='Tahoma' size='9'></font>",
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
            turnTime = me.GetTurnTime(_target);
            OrbWalk(Orbwalking.CanCancelAnimation());
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
            Item orchid = null;
            if (Menu.Item("enabledAbilities").GetValue<AbilityToggler>().IsEnabled("item_orchid"))
            {
                orchid = me.FindItem("item_orchid"); //Orchid [Silence]
            }
            Item hex = null;
            if (Menu.Item("enabledAbilities").GetValue<AbilityToggler>().IsEnabled("item_sheepstick"))
            {
                hex = me.FindItem("item_sheepstick"); //Hex [Disable]
            }
            Item mask = null;
            if (Menu.Item("enabledAbilities").GetValue<AbilityToggler>().IsEnabled("item_mask_of_madness"))
            { 
                mask = me.FindItem("item_mask_of_madness"); //Mask of Madness [Attack Speed]
            }
            Item mjo = null;
            if (Menu.Item("enabledAbilities").GetValue<AbilityToggler>().IsEnabled("item_mjollnir"))
            { mjo = me.FindItem("item_mjollnir"); //Mjollnir Buff [AoE lightning]
            }
            Item shivas = null;
            if (Menu.Item("enabledAbilities").GetValue<AbilityToggler>().IsEnabled("item_shivas_guard"))
            {
                shivas = me.FindItem("item_shivas_guard"); //Shivas [AoE Slow]
            }
            var neededMana = me.Mana - sonic.ManaCost;
            var allitems = me.Inventory.Items.Where(x => x.CanBeCasted() && x.ManaCost <= neededMana);
            var enumerable = allitems as Item[] ?? allitems.ToArray();
            var itemOnTarget =
                enumerable.FirstOrDefault(
                    x =>
                        x.Name == "item_orchid");
            var itemWithOutTarget = enumerable.FirstOrDefault(
                    x =>
						x.Name == "item_shivas_guard");
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
            #region Items
            //Mjollnir
            if (itemOnMySelf != null && Menu.Item("enabledAbilities").GetValue<AbilityToggler>().IsEnabled("item_mjollnir"))
            {
                itemOnMySelf.UseAbility(me);
                Utils.Sleep(50 + Game.Ping, "nextAction");
                return;
            }
			//Blink to Target
            if (blink != null && blink.CanBeCasted() && distance >= attackRange && Utils.SleepCheck("blink"))
            {
                var point = new Vector3(
                    (float)(target.Position.X - 20 * Math.Cos(me.FindAngleBetween(target.Position, true))),
                    (float)(target.Position.Y - 20 * Math.Sin(me.FindAngleBetween(target.Position, true))),
                    target.Position.Z);
                blink.UseAbility(point);
                Utils.Sleep(200 + Game.Ping, "blink");
                return;
            }
			//Orchid & Hex
            if (itemOnTarget != null && Menu.Item("enabledAbilities").GetValue<AbilityToggler>().IsEnabled("item_orchid"))
            {
                orchid.UseAbility(target);
                Utils.Sleep(50 + Game.Ping, "nextAction");
                return;
            }
            if (Menu.Item("enabledAbilities").GetValue<AbilityToggler>().IsEnabled("item_sheepstick") && hex !=null && hex.CanBeCasted() && Utils.SleepCheck("nextAction"))
            {
                hex.UseAbility(target);
                Utils.Sleep(50 + Game.Ping, "nextAction");
                return;
            }
            //Shivas Guard
            if (itemWithOutTarget != null && Menu.Item("enabledAbilities").GetValue<AbilityToggler>().IsEnabled("item_shivas_guard"))
            {
                itemWithOutTarget.UseAbility();
                Utils.Sleep(100 + Game.Ping, "nextAction");
                return;
            }
			//BKB 
            if (Menu.Item("enabledAbilities").GetValue<AbilityToggler>().IsEnabled("item_black_king_bar") && bkb != null && bkb.CanBeCasted() && Utils.SleepCheck("bkb"))
            {
                bkb.UseAbility();
                Utils.Sleep(35+Game.Ping, "bkb");
                return;
            }
            #endregion
            #region Spells
            if (ss != null && ss.CanBeCasted() && distance <= ss.CastRange && Utils.SleepCheck("nextAction"))
			{
                ss.UseAbility(target);
                Utils.Sleep(150 + Game.Ping, "nextAction");
                return;
            }
            if (scream != null && scream.CanBeCasted() && Utils.SleepCheck("nextAction"))
			{
                scream.UseAbility();
                Utils.Sleep(150 + Game.Ping, "nextAction");
                return;
            }
            if (sonic != null && sonic.CanBeCasted() && distance <= sonic.CastRange && Utils.SleepCheck("nextAction"))
			{
                sonic.UseAbility(target.Position);
                Utils.Sleep(150 + Game.Ping, "nextAction");
                return;
            }
            #endregion
        }
		#endregion
        #region OrbWalk
        private static void OrbWalk(bool canCancel)
        {
            var canAttack = !Orbwalking.AttackOnCooldown(_target) && !_target.IsInvul() && !_target.IsAttackImmune()
                            && me.CanAttack();
            if (canAttack && (targetDistance <= (550)))
            {
                if (!Utils.SleepCheck("attack"))
                {
                    return;
                }
                me.Attack(_target);
                Utils.Sleep(100, "attack");
                return;
            }

            var canMove = (canCancel && Orbwalking.AttackOnCooldown(_target))
                          || (!Orbwalking.AttackOnCooldown(_target) && targetDistance > 350);
            if (!Utils.SleepCheck("move") || !canMove)
            {
                return;
            }
            var mousePos = Game.MousePosition;
            if (_target.Distance2D(me) < 500)
            {
                var pos = _target.Position
                          + _target.Vector3FromPolarAngle()
                          * (float)
                            Math.Max(
                                (Game.Ping / 1000 + (targetDistance / me.MovementSpeed) + turnTime)
                                * _target.MovementSpeed,
                                500);
                if (pos.Distance(me.Position) > _target.Distance2D(pos) - 80)
                {
                    me.Move(pos);
                }
                else
                {
                    me.Follow(_target);
                }
            }
            else
            {
                me.Move(mousePos);
            }
            Utils.Sleep(100, "move");
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