using System;
using Server.Engines.ConPVP;
using Server.Items;
using Server.Misc;
using Server.Targeting;

namespace Server.SkillHandlers
{
    public static class Poisoning
    {
        public static void Initialize()
        {
            SkillInfo.Table[(int)SkillName.Poisoning].Callback = OnUse;
        }

        public static TimeSpan OnUse(Mobile m)
        {
            m.Target = new InternalTargetPoison();

            m.SendLocalizedMessage(502137); // Select the poison you wish to use

            return TimeSpan.Zero; // Mahaon: skill-reuse delay removed
        }

        private class InternalTargetPoison : Target
        {
            public InternalTargetPoison() : base(2, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is BasePoisonPotion potion)
                {
                    from.SendLocalizedMessage(502142); // To what do you wish to apply the poison?
                    from.Target = new InternalTarget(potion);
                }
                else // Not a Poison Potion
                {
                    from.SendLocalizedMessage(502139); // That is not a poison potion.
                }
            }

            private class InternalTarget : Target
            {
                private readonly BasePoisonPotion m_Potion;

                public InternalTarget(BasePoisonPotion potion) : base(2, false, TargetFlags.None) => m_Potion = potion;

                protected override void OnTarget(Mobile from, object targeted)
                {
                    if (m_Potion.Deleted)
                    {
                        return;
                    }

                    var startTimer = false;

                    if (targeted is Food or FukiyaDarts or Shuriken)
                    {
                        startTimer = true;
                    }
                    else if (targeted is BaseWeapon)
                    {
                        // Mahaon: any weapon can be poisoned. Vanilla AOS only allowed the
                        // handful of types carrying InfectiousStrike, which locked out
                        // Swordsmanship and most of Fencing; delivery on a normal hit is
                        // handled by Systems.MahaonCombat.WeaponPoisonSystem.
                        startTimer = true;
                    }

                    if (startTimer)
                    {
                        new InternalTimer(from, (Item)targeted, m_Potion).Start();

                        from.PlaySound(0x4F);

                        if (!DuelContext.IsFreeConsume(from))
                        {
                            m_Potion.Consume();
                            from.AddToBackpack(new Bottle());
                        }
                    }
                    else // Target can't be poisoned
                    {
                        // You cannot poison that! You can only poison weapons, food or drink.
                        from.SendLocalizedMessage(502145);
                    }
                }

                private class InternalTimer : Timer
                {
                    private readonly Mobile m_From;
                    private readonly double m_MaxSkill;
                    private readonly double m_MinSkill;
                    private readonly Poison m_Poison;
                    private readonly Item m_Target;

                    public InternalTimer(Mobile from, Item target, BasePoisonPotion potion) : base(TimeSpan.FromSeconds(2.0))
                    {
                        m_From = from;
                        m_Target = target;
                        m_Poison = potion.Poison;
                        m_MinSkill = potion.MinPoisoningSkill;
                        m_MaxSkill = potion.MaxPoisoningSkill;
                    }

                    protected override void OnTick()
                    {
                        if (m_From.CheckTargetSkill(SkillName.Poisoning, m_Target, m_MinSkill, m_MaxSkill))
                        {
                            if (m_Target is Food food)
                            {
                                food.Poison = m_Poison;
                            }
                            else if (m_Target is BaseWeapon weapon)
                            {
                                weapon.Poison = m_Poison;
                                weapon.PoisonCharges = 18 - m_Poison.Level * 2;
                            }
                            else if (m_Target is FukiyaDarts darts)
                            {
                                darts.Poison = m_Poison;
                                darts.PoisonCharges = Math.Min(
                                    18 - m_Poison.Level * 2,
                                    darts.UsesRemaining
                                );
                            }
                            else if (m_Target is Shuriken shuriken)
                            {
                                shuriken.Poison = m_Poison;
                                shuriken.PoisonCharges = Math.Min(
                                    18 - m_Poison.Level * 2,
                                    shuriken.UsesRemaining
                                );
                            }

                            m_From.SendLocalizedMessage(1010517); // You apply the poison

                            Titles.AwardKarma(m_From, -20, true);

                            Server.Systems.MahaonCombat.ThievingSpecializationSystem.Train(
                                m_From, Server.Systems.MahaonCombat.ThievingSpecialization.Poisoner
                            );
                        }
                        else // Failed
                        {
                            // 5% of chance of getting poisoned if failed, reduced by Отравитель specialization
                            var reduction = Server.Systems.MahaonCombat.ThievingSpecializationSystem.GetSelfPoisonReduction(m_From);

                            if (m_From.Skills.Poisoning.Base < 80.0 && Utility.RandomDouble() < 0.05 * (1.0 - reduction))
                            {
                                m_From.SendLocalizedMessage(502148); // You make a grave mistake while applying the poison.
                                m_From.ApplyPoison(m_From, m_Poison);
                            }
                            else
                            {
                                if (m_Target is BaseWeapon weapon)
                                {
                                    if (weapon.Type == WeaponType.Slashing)
                                    {
                                        m_From.SendLocalizedMessage(
                                            1010516
                                        ); // You fail to apply a sufficient dose of poison on the blade
                                    }
                                    else
                                    {
                                        m_From.SendLocalizedMessage(
                                            1010518
                                        ); // You fail to apply a sufficient dose of poison
                                    }
                                }
                                else
                                {
                                    m_From.SendLocalizedMessage(1010518); // You fail to apply a sufficient dose of poison
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
