﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace OneKeyToWin_AIO_Sebby.Champions
{
    class Draven
    {
        private Menu Config = Program.Config;
        public static Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;
        private Spell E, Q, R, W;
        private float QMANA, WMANA, EMANA, RMANA;
        private int axeCatchRange;
        public Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        private static GameObject RMissile = null;
        public List<GameObject> axeList = new List<GameObject>();

        public void LoadOKTW()
        {
            
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 1100);
            R = new Spell(SpellSlot.E, 3000);

            E.SetSkillshot(0.25f, 130, 1400, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(0.4f, 160, 2000, false, SkillshotType.SkillshotLine);

            Config.SubMenu(Player.ChampionName).SubMenu("AXE option").AddItem(new MenuItem("axeCatchRange", "Axe catch range").SetValue(new Slider(500, 200, 2000)));

            Config.SubMenu(Player.ChampionName).SubMenu("R config").AddItem(new MenuItem("autoR", "Auto R").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").AddItem(new MenuItem("Rcc", "R cc").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").AddItem(new MenuItem("Raoe", "R aoe").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rjungle", "R Jungle stealer").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rdragon", "Dragon").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rbaron", "Baron").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rred", "Red").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rblue", "Blue").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rally", "Ally stealer").SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").AddItem(new MenuItem("hitchanceR", "VeryHighHitChanceR").SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").AddItem(new MenuItem("useR", "Semi-manual cast R key").SetValue(new KeyBind('t', KeyBindType.Press))); //32 == space

            Obj_SpellMissile.OnCreate += SpellMissile_OnCreateOld;
            Obj_SpellMissile.OnDelete += Obj_SpellMissile_OnDelete;
            Orbwalking.BeforeAttack += BeforeAttack;
            GameObject.OnCreate += GameObjectOnOnCreate;
            GameObject.OnDelete += GameObjectOnOnDelete;
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += GameOnOnUpdate;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;

        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            
        }

        private void Obj_SpellMissile_OnDelete(GameObject sender, EventArgs args)
        {
            if (!sender.IsValid<MissileClient>())
                return;
            MissileClient missile = (MissileClient)sender;

            if (missile.IsValid && missile.IsAlly && missile.SData.Name != null && missile.SData.Name == "DravenR")
            {
                RMissile = null;
            }
        }

        private void SpellMissile_OnCreateOld(GameObject sender, EventArgs args)
        {
            if (!sender.IsValid<MissileClient>())
                return;

            MissileClient missile = (MissileClient)sender;

            if (missile.IsValid && missile.IsAlly && missile.SData.Name != null && missile.SData.Name == "DravenR")
            {
                RMissile = sender;
            }
        }

        private void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            //Program.debug("" + OktwCommon.GetBuffCount(Player, "dravenspinningattack"));
            if (Q.IsReady())
            {
                if (Program.Combo && Player.Mana > RMANA + QMANA && OktwCommon.GetBuffCount(Player,"dravenspinningattack") + axeList.Count < 2)
                {
                    Q.Cast();
                }
                if (Program.Farm && Player.Mana > RMANA + QMANA + EMANA + WMANA && OktwCommon.GetBuffCount(Player, "dravenspinningattack") + axeList.Count == 0)
                    Q.Cast();
            }
        }

        private void GameObjectOnOnCreate(GameObject sender, EventArgs args)
        {
            if (sender.Name.Contains("Q_reticle_self"))
            {
                axeList.Add(sender);
                
            }
        }

        private void GameObjectOnOnDelete(GameObject sender, EventArgs args)
        {
            if (sender.Name.Contains("Q_reticle_self"))
            {
                axeList.Remove(sender);
            }
        }

        private void GameOnOnUpdate(EventArgs args)
        {
            SetMana();
            //Program.debug("" + OktwCommon.GetBuffCount(Player, "dravenspinningattack"));
            axeCatchRange = Config.Item("axeCatchRange").GetValue<Slider>().Value;
            axeList.RemoveAll(x => !x.IsValid);
            AxeLogic();

            if (Program.LagFree(1) && E.IsReady() && !Player.IsWindingUp)
                LogicE();

            if (Program.LagFree(3) && W.IsReady() && !Player.HasBuff("dravenfurybuff"))
                LogicW();

            if (Program.LagFree(4) && R.IsReady() && !Player.IsWindingUp)
                LogicR();
        }

        private void LogicW()
        {
            if (Program.Combo && Player.Mana > RMANA + EMANA + WMANA && Player.CountEnemiesInRange(1000) > 0)
            {
                W.Cast();
            }
        }

        private void LogicE()
        {
            foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(E.Range) && !Orbwalking.InAutoAttackRange(enemy) && E.GetDamage(enemy) > enemy.Health))
            {
                Program.CastSpell(E, enemy);
                return;
            }

            var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
            if (t.IsValidTarget())
            {
                if (Program.Combo && Player.Mana > RMANA + EMANA)
                {
                    if (!Orbwalking.InAutoAttackRange(t))
                    {
                        Program.CastSpell(E, t);
                    }
                    R.CastIfWillHit(t, 2, true);
                    if(Player.Health < Player.MaxHealth * 0.5)
                        Program.CastSpell(E, t);

                }
            }
            foreach (var target in Program.Enemies.Where(target => target.IsValidTarget(E.Range)))
            {
                if (target.IsValidTarget(300) && target.IsMelee)
                {
                    Program.CastSpell(E, t);
                }
            }
        }

        private void LogicR()
        {
            if (Config.Item("useR").GetValue<KeyBind>().Active)
            {
                var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);

                if (t.IsValidTarget())
                {
                    R.CastIfWillHit(t, 2, true);
                    R.Cast(t, true, true);
                }
            }
            if (Config.Item("autoR").GetValue<bool>())
            {
                foreach (var target in Program.Enemies.Where(target => target.IsValidTarget(R.Range) && Program.ValidUlt(target)))
                {

                    float predictedHealth = target.Health + target.HPRegenRate * 2;
                    double Rdmg = R.GetDamage(target);
                    if (Rdmg > predictedHealth)
                        Rdmg = getRdmg(target);
                    var qDmg = Q.GetDamage(target);
                    var wDmg = W.GetDamage(target);
                    if (Rdmg > predictedHealth && target.CountAlliesInRange(400) == 0)
                    {
                        castR(target);
                        Program.debug("R normal");
                    }
                    else if (Rdmg > predictedHealth && target.HasBuff("Recall"))
                    {
                        R.Cast(target, true, true);
                        Program.debug("R recall");
                    }
                    else if (!OktwCommon.CanMove(target) && Config.Item("Rcc").GetValue<bool>() &&
                        target.IsValidTarget(Q.Range + E.Range) && Rdmg + qDmg * 4 > predictedHealth)
                    {
                        R.CastIfWillHit(target, 2, true);
                        R.Cast(target, true);
                    }
                    else if (Program.Combo && Config.Item("Raoe").GetValue<bool>())
                    {
                        R.CastIfWillHit(target, 3, true);
                    }
                    else if (target.IsValidTarget(Q.Range + E.Range) && Rdmg + qDmg + wDmg > predictedHealth && Program.Combo && Config.Item("Raoe").GetValue<bool>())
                    {
                        R.CastIfWillHit(target, 2, true);
                    }
                }
            }
        }

        private void castR(Obj_AI_Hero target)
        {
            if (Config.Item("hitchanceR").GetValue<bool>())
            {
                List<Vector2> waypoints = target.GetWaypoints();
                if (target.Path.Count() < 2 && (Player.Distance(waypoints.Last<Vector2>().To3D()) - Player.Distance(target.Position)) > 400)
                {
                    R.CastIfHitchanceEquals(target, HitChance.High, true);
                }
            }
            else
                Program.CastSpell(R, target);
        }

        private double getRdmg(Obj_AI_Base target)
        {
            var rDmg = R.GetDamage(target) ;
            var dmg = 0;
            PredictionOutput output = R.GetPrediction(target);
            Vector2 direction = output.CastPosition.To2D() - Player.Position.To2D();
            direction.Normalize();
            List<Obj_AI_Hero> enemies = ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsEnemy && x.IsValidTarget()).ToList();
            foreach (var enemy in enemies)
            {
                PredictionOutput prediction = R.GetPrediction(enemy);
                Vector3 predictedPosition = prediction.CastPosition;
                Vector3 v = output.CastPosition - Player.ServerPosition;
                Vector3 w = predictedPosition - Player.ServerPosition;
                double c1 = Vector3.Dot(w, v);
                double c2 = Vector3.Dot(v, v);
                double b = c1 / c2;
                Vector3 pb = Player.ServerPosition + ((float)b * v);
                float length = Vector3.Distance(predictedPosition, pb);
                if (length < (R.Width + 100 + enemy.BoundingRadius / 2) && Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
                    dmg++;
            }
            var allMinionsR = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, R.Range, MinionTypes.All);
            foreach (var minion in allMinionsR)
            {
                PredictionOutput prediction = R.GetPrediction(minion);
                Vector3 predictedPosition = prediction.CastPosition;
                Vector3 v = output.CastPosition - Player.ServerPosition;
                Vector3 w = predictedPosition - Player.ServerPosition;
                double c1 = Vector3.Dot(w, v);
                double c2 = Vector3.Dot(v, v);
                double b = c1 / c2;
                Vector3 pb = Player.ServerPosition + ((float)b * v);
                float length = Vector3.Distance(predictedPosition, pb);
                if (length < (R.Width + 100 + minion.BoundingRadius / 2) && Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
                    dmg++;
            }
            //if (Config.Item("debug").GetValue<bool>())
            //    Game.PrintChat("R collision" + dmg);

            if (dmg > 7)
                return rDmg * 0.7 + R.GetDamage(target);
            else
                return rDmg - (rDmg * 0.1 * dmg) + R.GetDamage(target);
        }

        private void AxeLogic()
        {
            if (axeList.Count == 0)
            {
                Orbwalker.SetOrbwalkingPoint(Game.CursorPos);
                return;
            }
            var bestAxe = axeList.First();

            if (axeList.Count == 1)
            {
                CatchAxe(bestAxe);
                return;
            }
            else
            {
                foreach (var obj in axeList)
                {
                    if (Game.CursorPos.Distance(bestAxe.Position) > Game.CursorPos.Distance(obj.Position))
                        bestAxe = obj;
                }
                CatchAxe(bestAxe);
            }
        }

        private void CatchAxe(GameObject Axe)
        {
            if (Player.Distance(Axe.Position) > 120 && Game.CursorPos.Distance(Axe.Position) < axeCatchRange && !Axe.Position.UnderTurret(true))
            {
                Orbwalker.SetOrbwalkingPoint(Axe.Position);
            }
            else
            {
                Orbwalker.SetOrbwalkingPoint(Game.CursorPos);
            }
        }

        private void SetMana()
        {
            QMANA = Q.Instance.ManaCost;
            WMANA = W.Instance.ManaCost;
            EMANA = E.Instance.ManaCost;
            if (!R.IsReady())
                RMANA = EMANA - Player.PARRegenRate * E.Instance.Cooldown;
            else
                RMANA = R.Instance.ManaCost;

            if (Player.Health < Player.MaxHealth * 0.2)
            {
                QMANA = 0;
                WMANA = 0;
                EMANA = 0;
                RMANA = 0;
            }
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            foreach (var obj in axeList)
            {
                Utility.DrawCircle(obj.Position, 200, System.Drawing.Color.Red, 1, 1);
            }

            Utility.DrawCircle(Game.CursorPos, axeCatchRange, System.Drawing.Color.LemonChiffon, 1, 1);
            if (RMissile != null )
                OktwCommon.DrawLineRectangle(RMissile.Position, Player.Position, (int)R.Width, 1, System.Drawing.Color.White);
        }
    }
}