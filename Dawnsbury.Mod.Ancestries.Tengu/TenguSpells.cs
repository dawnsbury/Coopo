using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Dawnsbury.Mods.Ancestries.Tengu
{
    class TenguSpells
    {

        public static readonly SpellId IgnitionSpellId = ModManager.RegisterNewSpell("Tengu:Ignition", 0, (spellId, caster, level, inCombat, spellInfo) =>
        {
            return Spells.CreateModern(IllustrationName.ProduceFlame, "Ignition",
                [Trait.Attack, Trait.Cantrip, Trait.Fire, Trait.Primal, Trait.Arcane, Trait.VersatileMelee, Trait.SpellCannotBeChosenInCharacterBuilder],
                "You snap your fingers and point at a target, which begins to smolder.",
                "Make a spell attack roll. The flame deals " + S.HeightenedVariable(level + 1, 2) + "d4 fire damage." + S.FourDegreesOfSuccessReverse(null, null, "Full damage.", "Double damage, and " + S.HeightenedVariable(level, 1) + "d4 persistent fire damage.") + "\n\n{b}Special: Versatile Melee.{/b} If you're adjacent to the target, increase all of the spell's damage dice to d6s; The spell becomes a melee spell attack and benefits from flanking." + S.HeightenText(level, 1, inCombat, "{b}Heightened (+1){/b} Increase the damage by 1d4 and the persistent damage on a critical hit by 1d4."),
                Target.Ranged(6), level, null).WithSpellAttackRoll().WithSoundEffect(SfxName.FireRay)
                .WithEffectOnEachTarget(async (spell, caster, target, result) =>
                {
                    string dieSize = caster.IsAdjacentTo(target) ? "d6" : "d4";
                    await CommonSpellEffects.DealAttackRollDamage(spell, caster, target, result, (spell.SpellLevel + 1) + dieSize, DamageKind.Fire);
                    if (result == CheckResult.CriticalSuccess)
                        target.AddQEffect(QEffect.PersistentDamage(spell.SpellLevel + dieSize, DamageKind.Fire));
                });
        });

        public static readonly SpellId VitalityLash = ModManager.RegisterNewSpell("Tengu:VitalityLash", 0, (spellId, caster, level, inCombat, spellInfo) =>
        {
            return Spells.CreateModern(IllustrationName.DisruptUndead, "Vitality Lash",
                [Trait.Cantrip, Trait.Concentrate, Trait.Manipulate, Trait.Positive, Trait.Divine, Trait.Primal, Trait.SpellCannotBeChosenInCharacterBuilder],
                "You demolish the target's corrupted essence with vital energy.",
                "Deal " + S.HeightenedVariable(level + 1, 2) + "d6 vitality damage. If the target critically fails its basic Fortitude save, it's also enfeebled 1 for 1 round.",
                Target.Ranged(6).WithAdditionalConditionOnTargetCreature((caster, target) => !target.HasTrait(Trait.Undead) ? Usability.CommonReasons.TargetIsNotUndead : Usability.Usable),
                level,
                SpellSavingThrow.Basic(Defense.Fortitude)).WithSoundEffect(SfxName.DivineLance).WithHeighteningOfDamageEveryLevel(level, 1, inCombat, "1d6")
                .WithEffectOnEachTarget(async (spell, caster, target, checkResult) =>
                {
                    await CommonSpellEffects.DealBasicDamage(spell, caster, target, checkResult, (level + 1).ToString() + "d6", DamageKind.Positive);
                    if (checkResult != CheckResult.CriticalFailure)
                        return;
                    target.AddQEffect(QEffect.Enfeebled(1).WithExpirationAtStartOfSourcesTurn(caster, 1));
                });
        });

        public static readonly SpellId GustOfWind = ModManager.RegisterNewSpell("Tengu:GustOfWind", 1, (spellId, caster, level, inCombat, spellInfo) =>
        {
            return Spells.CreateModern(IllustrationName.PushingGust, "Gust of Wind",
                [Trait.Spell, Trait.Air, Trait.Concentrate, Trait.Manipulate, Trait.Arcane, Trait.Primal, Trait.SpellCannotBeChosenInCharacterBuilder],
                "flavour text",
                "rules text",
                Target.Line(60 / 5),
                level,
                SpellSavingThrow.Standard(Defense.Fortitude)).WithSoundEffect(SfxName.GaleBlast)
                //.WithEffectOnEachTarget(async (action, caster, target, result) =>
                //{
                //    switch (result)
                //    {
                //        case CheckResult.CriticalSuccess:
                //            return;
                //        case CheckResult.Success:
                //            return;
                //        case CheckResult.Failure:
                //            return;
                //        case CheckResult.CriticalFailure:
                //            return;
                //    };
                //})
                .WithEffectOnEachTile(async (action, caster, tiles) =>
                {
                    foreach (Tile t in tiles)
                    {
                        t.AddQEffect(new TileQEffect()
                        {
                            Illustration = IllustrationName.FourWinds,
                            ExpiresAt = ExpirationCondition.ExpiresAtStartOfSourcesTurn
                        });
                    }
                    //foreach (Creature c in caster.Battle.AllCreatures)
                    //{
                    //    c.AddQEffect(new QEffect() // tracker qeffect
                    //    {
                            
                    //        ExpiresAt = ExpirationCondition.ExpiresAtStartOfSourcesTurn
                    //    });
                    //}
                    
                    // QEffect.YouBeginAction can be used to disrupt an action - see gnome's Empathetic Plea for an example
                });
        });

    }

}
