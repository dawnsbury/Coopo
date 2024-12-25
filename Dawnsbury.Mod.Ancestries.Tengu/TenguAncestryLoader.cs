using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core;
using System.Diagnostics;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Display.Text;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Auxiliary;
using Dawnsbury.Audio;

namespace Dawnsbury.Mods.Ancestries.Tengu;

// TODO: try making a mod that allows custom music? should be fairly straight forward, mess with Dawnsbury.Audio.Truesong using reflection

public static class TenguAncestryLoader
{
    public static readonly Trait TenguTrait = ModManager.RegisterTrait("Tengu", new TraitProperties("Tengu", true) { IsAncestryTrait = true });

    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        Debugger.Launch();

        Feat TenguAncestry = new AncestrySelectionFeat(
            ModManager.RegisterFeatName("Tengu"),
            description: "Tengus are gregarious and resourceful avian humanoids who collect knowledge and treasures alike. They are natural survivalists and conversationalists, equally at home living off the wilderness and finding a niche in dense cities. Tengu are known to accumulate knowledge, tools, and companions, adding them to their collection as they travel.\n\n{b}Sharp Beak{/b} With your sharp beak, you are never without a weapon. You have a beak unarmed attack that deals 1d6 piercing damage. Your beak is in the brawling weapon group and has the finesse and unarmed traits.",
            traits: [Trait.Humanoid, TenguTrait],
            hp: 6,
            speed: 5,
            abilityBoosts: [
                new EnforcedAbilityBoost(Ability.Dexterity),
                new FreeAbilityBoost()
            ],
            heritages: GetHeritages().ToList())
            .WithOnCreature((Creature cr) =>
            {
                cr.AddQEffect(new QEffect()
                {
                    AdditionalUnarmedStrike = new Item(IllustrationName.Beak, "beak", [Trait.Finesse, Trait.Weapon, Trait.Melee, Trait.Unarmed, Trait.Brawling])
                    .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Piercing))
                });
            });

        ModManager.AddFeat(TenguAncestry);

        AddFeats(GetAncestryFeats());

        Items.RegisterItems();
    }

    static void AddFeats(IEnumerable<Feat> feats)
    {
        foreach (Feat f in feats)
        {
            ModManager.AddFeat(f);
        }
    }

    static IEnumerable<Feat> GetAncestryFeats()
    {
        Spell produceFlame = AllSpells.CreateModernSpellTemplate(SpellId.ProduceFlame, TenguTrait);
        yield return new TrueFeat(
            ModManager.RegisterFeatName("Mariner's Fire"),
            1,
            "You conjure uncanny orbs of spiritual flame that float above or below the water's surface.",
            $"You can cast the {produceFlame.ToSpellLink()} cantrip as a primal innate spell at will. Your spellcasting ability for this spell is Charisma. You can cast this cantrip underwater.",
            [TenguTrait]
            ).WithIllustration(produceFlame.Illustration).WithRulesBlockForSpell(produceFlame.SpellId).WithOnCreature(delegate (Creature cr)
            {
                cr.GetOrCreateSpellcastingSource(SpellcastingKind.Innate, TenguTrait, Ability.Charisma, Trait.Primal).WithSpells([produceFlame.SpellId], 0);
            });
        yield return new TrueFeat(
            ModManager.RegisterFeatName("One-Toed Hop", "One-Toed Hop {icon:Action}"),
            1,
            "Assuming a peculiar stance, you make a short hop on each toe.",
            "You make a short 5ft Leap which does not trigger reactions that are triggered by movement, such as Attack of Opportunity.\n\n{b}Special{/b} If you also have the Powerful Leap feat, ",
            [TenguTrait]
            ).WithOnCreature((Creature cr) =>
            {
                cr.AddQEffect(new QEffect("One-Toed Hop {icon:Action}", "Make a short Leap which doesn't trigger reactions to movement.")
                {
                    ProvideActionIntoPossibilitySection = (QEffect self, PossibilitySection section) =>
                    {
                        if (section.PossibilitySectionId == PossibilitySectionId.Movement)
                        {
                            return new ActionPossibility(OneToedHop(cr));
                        }
                        else return null;
                    }
                });
            });
        yield return new TrueFeat(
            ModManager.RegisterFeatName("Scavenger's Search"),
            1,
            "You're always on the lookout for supplies and valuables.",
            "You gain a +2 circumstance bonus to locate objects (such as secret doors and hazards) you search for within 30 feet with a Seek action.",
            [TenguTrait]
            ).WithOnCreature((Creature cr) =>
            {
                cr.AddQEffect(new QEffect("Scavenger's Search", "You have a +2 circumstance bonus when Seeking objects.")
                {
                    BonusToAttackRolls = (QEffect self, CombatAction action, Creature? target) =>
                    {
                        if (target == null) return null;
                        // triggers on Seek actions, and Pseudocreature is the secret sauce that indicates it's a tile being seeked
                        if (action.ActionId == ActionId.Seek && target.HasTrait(Trait.Pseudocreature)) return new Bonus(2, BonusType.Circumstance, "Scavenger's Search");
                        else return null;
                    }
                });
            });
        // TODO: Implement feat and test if the homebrew feels reasonably powerful. Also, reformat to be like "You gain the Squawk! reaction:" and then show a reaction block
        yield return new TrueFeat(
            ModManager.RegisterFeatName("Squawk!", "Squawk! {icon:Reaction}"),
            1,
            "You let out an awkward squawk, ruffle your feathers, or fake some other birdlike tic to cover up a poor attempt to intimidate.",
            "{b}Frequency{/b} Once per day\n{b}Trigger{/b} You fail or critically fail an Intimidation check to Demoralize a creature without the tengu trait\n\nReroll the failed Intimidation check and keep the new result. ",
            [TenguTrait]
            ).WithOnCreature((Creature cr) =>
            {
                cr.AddQEffect(new QEffect("Squawk! {icon:Reaction}", "Reroll a failed check to Demoralize.")
                {
                    // TODO: cannot be implemented (easily) until this event is added next update
                    //RerollActiveRoll = 
                });
            });
        Spell electricArc = AllSpells.CreateModernSpellTemplate(SpellId.ElectricArc, TenguTrait);
        yield return new TrueFeat(
            ModManager.RegisterFeatName("Storm's Lash"),
            1,
            "Wind and lightning have always been friends to you.",
            $"You can cast the {electricArc.ToSpellLink()} cantrip as a primal innate spell at will. Your spellcasting ability for this spell is Charisma.",
            [TenguTrait]
            ).WithIllustration(electricArc.Illustration).WithRulesBlockForSpell(electricArc.SpellId).WithOnCreature(delegate (Creature cr)
            {
                cr.GetOrCreateSpellcastingSource(SpellcastingKind.Innate, TenguTrait, Ability.Charisma, Trait.Primal).WithSpells([electricArc.SpellId], 0);
            });
        // Tengu Weapon Familiarity and Subfeats
        List<Trait> familiarWeapons = [Items.Katana, Items.Khakkara, Items.TempleSword, Items.Wakizashi, Items.TenguGaleBlade];
        Feat TenguWeaponFamiliarity = new TrueFeat(
            ModManager.RegisterFeatName("Tengu Weapon Familiarity"),
            1,
            "You have eclectic experience with all sorts of weapons.",
            "You have familiarity with all weapons with the tengu trait, plus the katana, khakkara, temple sword, and wakizashi. For the purpose of proficiency, you treat any of these that are martial weapons as simple weapons and any that are advanced weapons as martial weapons. At 5th level, whenever you get a critical hit with one of these weapons, you get its critical specialization effect.\n\nIn addition, choose another weapon of your choice from the sword group: You are also familiar with this weapon, and gain the same benefits.",
            [TenguTrait]
            ).WithOnSheet((calculatedSheet) =>
            {
                foreach (Trait t in familiarWeapons)
                {
                    calculatedSheet.Proficiencies.AddProficiencyAdjustment(traits => traits.Contains(t) && traits.Contains(Trait.Martial), Trait.Simple);
                    calculatedSheet.Proficiencies.AddProficiencyAdjustment(traits => traits.Contains(t) && traits.Contains(Trait.Advanced), Trait.Martial);
                }
            });
        TenguWeaponFamiliarity.Subfeats = [];
        foreach (Item item in Core.Mechanics.Treasure.Items.ShopItems)
        {
            if (!item.HasTrait(Trait.Sword)) continue;
            if (item.MainTrait == Trait.None) continue;
            if (item.Runes.Count() != 0) continue;
            if (familiarWeapons.Where(weaponTrait => item.HasTrait(weaponTrait)).Any()) continue; // if this weapon is anything already covered by the base feat, don't list it
            if (item.HasTrait(Trait.Simple)) continue; // no point taking extra proficiency in a weapon that everyone is already proficient in
            TenguWeaponFamiliarity.Subfeats.Add(new Feat(
                ModManager.RegisterFeatName($"TenguWeaponFamiliarity:{item.Name}", item.Name.Capitalize()),
                $"You have experience with {item.Name}s.",
                $"For the purpose of proficiency, you treat {item.Name}s as {(item.HasTrait(Trait.Advanced)? "martial" : "simple")} weapons.",
                traits: [],
                subfeats: []).WithOnSheet((calculatedSheet) =>
                {
                    calculatedSheet.Proficiencies.AddProficiencyAdjustment(traits => traits.Contains(item.MainTrait) && traits.Contains(Trait.Martial), Trait.Simple);
                    calculatedSheet.Proficiencies.AddProficiencyAdjustment(traits => traits.Contains(item.MainTrait) && traits.Contains(Trait.Advanced), Trait.Martial);
                }));
        }
        yield return TenguWeaponFamiliarity;
        // end of Tengu Weapon Familiarity
        yield return new TrueFeat(
            ModManager.RegisterFeatName("Uncanny Agility"),
            1,
            "You have near-supernatural poise that lets you move swiftly across the most unsteady surfaces.",
            "You gain the Feather Step skill feat, which allows you to Step into difficult terrain.",
            [TenguTrait]
            ).WithOnSheet((calculatedSheet) =>
            {
                calculatedSheet.GrantFeat(FeatName.FeatherStep);
            });
        yield return new TrueFeat(
            ModManager.RegisterFeatName("Waxed Feathers"),
            1,
            "Your feathers are coated in a waxy substance that repels water.",
            "You gain a +1 circumstance bonus to saving throws against effects that have the water trait.",
            [TenguTrait]
            ).WithPermanentQEffect((QEffect self) =>
            {
                self.Name = "Waxed Feathers";
                self.Description = "You have a +1 circumstance bonus to saving throws against water effects.";
                self.BonusToDefenses = (QEffect self, CombatAction? action, Defense defense) =>
                {
                    if (!defense.IsSavingThrow()) return null;
                    else if (action != null && action.HasTrait(Trait.Water)) return new Bonus(1, BonusType.Circumstance, "Waxed Feathers");
                    else return null;
                };
            });
    }

    static IEnumerable<Feat> GetHeritages()
    {
        yield return new HeritageSelectionFeat(
            ModManager.RegisterFeatName("Dogtooth Tengu"),
            "In addition to a beak, your mouth also features a number of vicious, pointed teeth. Some legends claim your powerful jaws can even bite through steel. While you aren't that strong yet, your fangs can still leave terrible wounds.",
            "Your beak unarmed attack gains the deadly d8 trait."
            ).WithOnCreature(delegate (Creature cr)
            {
                cr.GetAttackItem("beak")?.Traits.Add(Trait.DeadlyD8);
            });
        // TODO: implement heritage
        yield return new HeritageSelectionFeat(
            ModManager.RegisterFeatName("Jinxed Tengu"),
            "Your lineage has been exposed to curse after curse, and now they slide off your feathers like rain.",
            "If you succeed at a saving throw against a curse or misfortune effect, you get a critical success instead. When you would gain the doomed condition, attempt a DC 17 flat check. On a success, reduce the value of the doomed condition you would gain by 1."
            ).WithOnCreature(delegate (Creature cr)
            {
                cr.AddQEffect(new QEffect("Jinxed Tengu", "TO BE IMPLEMENTED"));
            });
        // TODO: implement heritage, replace the Great Beyond with whatever lore equivalent/see what pathbuilder does
        yield return new HeritageSelectionFeat(
            ModManager.RegisterFeatName("Mountainkeeper Tengu"),
            "You come from a line of tengu ascetics, leaving you with a link to the spirits of the world and the Great Beyond.",
            $"You can cast the {AllSpells.CreateSpellLink(SpellId.DisruptUndead, TenguTrait)} cantrip as an innate spell at will. Your spellcasting ability for this spell is Charisma. When you choose this feat, you can decide if the spell is primal or divine."
            ).WithOnCreature(delegate (Creature cr)
            {
                cr.AddQEffect(new QEffect("Mountainkeeper Tengu", "TO BE IMPLEMENTED"));
            });
        // TODO: implement heritage, requires homebrew cause the original effect doesnt work in dawnsbury days
        yield return new HeritageSelectionFeat(
            ModManager.RegisterFeatName("Skyborn Tengu"),
            "Your bones may be especially light, you may be a rare tengu with wings, or your connection to the spirits of wind and sky might be stronger than most, slowing your descent through the air.",
            "You take no damage from falling, regardless of the distance you fall."
            ).WithOnCreature(delegate (Creature cr)
            {
                cr.AddQEffect(new QEffect("Skyborn Tengu", "TO BE IMPLEMENTED"));
            });
        // TODO: implement heritage, replace Hei Feng with the equivalent lore or just remove it
        yield return new HeritageSelectionFeat(
            ModManager.RegisterFeatName("Stormtossed Tengu"),
            "Whether due to a blessing from Hei Feng or hatching from your egg during a squall, you are resistant to storms.",
            "You gain electricity resistance equal to half your level (minimum 1). You automatically succeed at the flat check to target a concealed creature if that creature is concealed only by rain or fog."
            ).WithOnCreature(delegate (Creature cr)
            {
                cr.AddQEffect(new QEffect("Stormtossed Tengu", "TO BE IMPLEMENTED"));
            });
        yield return new HeritageSelectionFeat(
            ModManager.RegisterFeatName("Taloned Tengu"),
            "Your talons are every bit as sharp and strong as your beak.",
            "You gain a talons unarmed attack that deals 1d4 slashing damage. Your talons are in the brawling group and have the agile, finesse, unarmed, and versatile P traits."
            ).WithOnCreature(delegate (Creature cr)
            {
                cr.AddQEffect(new QEffect()
                {
                    AdditionalUnarmedStrike = new Item(new ModdedIllustration("TenguAssets/talons.png"), "talons", [Trait.Brawling, Trait.Agile, Trait.Finesse, Trait.Weapon, Trait.Melee, Trait.Unarmed, Trait.VersatileP])
                    .WithWeaponProperties(new WeaponProperties("1d4", DamageKind.Slashing)).WithSoundEffect(SfxName.Fist2)
                });
            });
        // TODO: implement heritage, maybe change the wording of the effect, since the effect will probably just be "you can move through water"
        yield return new HeritageSelectionFeat(
            ModManager.RegisterFeatName("Wavediver Tengu"),
            "You're one of the rare tengu who can cut through water like a bird through air, and you often lurk in rivers or oceans where few expect you.",
            "You gain a swim Speed of 15 feet."
            ).WithOnCreature(delegate (Creature cr)
            {

                cr.AddQEffect(new QEffect("Wavediver Tengu", "TO BE IMPLEMENTED"));
            });
    }

    static CombatAction OneToedHop(Creature self)
    {
        
        int leapDistance = self.HasEffect(QEffectId.PowerfulLeap) ? 2 : 1;
        return new CombatAction(self, IllustrationName.Jump, "One-Toed Hop",
            [Trait.Move, Trait.Basic],
            "{i}Assuming a peculiar stance, you make a short hop on each toe.{/i}\n\nMake a short " + S.HeightenedVariable(leapDistance * 5, 5) + "ft Leap which does not trigger reactions which are triggered by movement.",
            new TileTarget((Creature jumper, Tile tile) => jumper.Occupies != null && tile.IsTrulyGenuinelyFreeTo(jumper) && jumper.DistanceTo(tile) <= leapDistance && jumper.Occupies.HasLineOfEffectToIgnoreLesser(tile) != CoverKind.Blocked, null)
            ).WithEffectOnChosenTargets(async delegate (CombatAction action, Creature jumper, ChosenTargets target)
            {
                if (target.ChosenTile == null) return;
                await jumper.SingleTileMove(target.ChosenTile, action);
            }).WithActionId(ActionId.Leap);
    }
}
