using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Ancestries.Kitsune;

public static class KitsuneAncestryLoader
{
    static readonly Trait KitsuneTrait = ModManager.RegisterTrait("Kitsune", new TraitProperties("Kitsune", true) { IsAncestryTrait = true });

    static FeatName FrozenWindKitsuneFeatName = ModManager.RegisterFeatName("Frozen Wind Kitsune");

    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        //Debugger.Launch();

        Feat KitsuneAncestry = new AncestrySelectionFeat(
            ModManager.RegisterFeatName("Kitsune"),
            description: "Kitsune are a charismatic and witty people with a connection to the spiritual that grants them many magical abilities, chiefly the power to shapechange into other forms. Whether they pass unseen among other peoples or hold their tails high, kitsune are clever observers of the societies around them.",
            traits: [Trait.Humanoid, KitsuneTrait],
            hp: 8,
            speed: 5,
            abilityBoosts: [
                new EnforcedAbilityBoost(Ability.Charisma),
                new FreeAbilityBoost()
            ],
            heritages: GetHeritages().ToList());

        ModManager.AddFeat(KitsuneAncestry);

        AddFeats(GetAncestryFeats());
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
        // this needs to be cleaned up so badly, its disgusting right now
        yield return new TrueFeat(
            ModManager.RegisterFeatName("Foxfire"),
            level: 1,
            "A crack of your tail sparks wisps of blue energy.",
            "Choose either electricity or fire when you gain this feat. You gain a foxfire ranged unarmed attack with a maximum range of 20 feet. The attack deals 1d4 damage of the chosen type. Your foxfire is in the sling weapon group and has the magical trait. Like other unarmed attacks, you can improve this attack with handwraps of mighty blows.\n\n{b}Special{/b} If you are a frozen wind kitsune, your foxfire deals cold damage instead of electricity or fire.",
            [KitsuneTrait],
            [
                new Feat(
                    ModManager.RegisterFeatName("FoxfireFire", "Fire"),
                    "Your tail produces sparks of flame.",
                    "Your foxfire deals fire damage.",
                    [KitsuneTrait], null).WithPermanentQEffect(null, (QEffect self) => {
                        self.AdditionalUnarmedStrike = new Item(IllustrationName.ElementFire, "foxfire", [Trait.Unarmed, Trait.Ranged, Trait.Weapon, Trait.Magical])
                            .WithWeaponProperties(
                                new WeaponProperties("1d4", DamageKind.Fire)
                                {
                                    Sfx = Audio.SfxName.FireRay,
                                    VfxStyle = new VfxStyle(1, Core.Animations.ProjectileKind.Arrow, IllustrationName.FireRay)
                                }.WithMaximumRange(4).WithRangeIncrement(4));
                    }).WithPrerequisite((charSheet) => charSheet.Sheet.Heritage?.FeatName != FrozenWindKitsuneFeatName, "Frozen wind kitsune must have a cold foxfire."),
                    new Feat(
                    ModManager.RegisterFeatName("FoxfireElectric", "Electric"),
                    "Your tail produces sparks of electricity.",
                    "Your foxfire deals electricity damage.",
                    [KitsuneTrait], null).WithPermanentQEffect(null, (QEffect self) => {
                        self.AdditionalUnarmedStrike = new Item(IllustrationName.ElementFire, "foxfire", [Trait.Unarmed, Trait.Ranged, Trait.Weapon, Trait.Magical])
                            .WithWeaponProperties(
                                new WeaponProperties("1d4", DamageKind.Electricity)
                                {
                                    Sfx = Audio.SfxName.ShockingGrasp,
                                    VfxStyle = new VfxStyle(1, Core.Animations.ProjectileKind.Arrow, IllustrationName.FireRay)
                                }.WithMaximumRange(4).WithRangeIncrement(4));
                    }).WithPrerequisite((charSheet) => charSheet.Sheet.Heritage?.FeatName != FrozenWindKitsuneFeatName, "Frozen wind kitsune must have a cold foxfire."),
                    new Feat(
                    ModManager.RegisterFeatName("FoxfireCold", "Cold"),
                    "Your tail produces sparks of cold.",
                    "Your foxfire deals cold damage.",
                    [KitsuneTrait], null).WithPermanentQEffect(null, (QEffect self) => {
                        self.AdditionalUnarmedStrike = new Item(IllustrationName.ElementFire, "foxfire", [Trait.Unarmed, Trait.Ranged, Trait.Weapon, Trait.Magical])
                            .WithWeaponProperties(
                                new WeaponProperties("1d4", DamageKind.Cold)
                                {
                                    Sfx = Audio.SfxName.RayOfFrost,
                                    VfxStyle = new VfxStyle(1, Core.Animations.ProjectileKind.Arrow, IllustrationName.FireRay)
                                }.WithMaximumRange(4).WithRangeIncrement(4));
                    }).WithPrerequisite((charSheet) => charSheet.Sheet.Heritage?.FeatName == FrozenWindKitsuneFeatName, "Only frozen wind kitsune can have a cold foxfire.")
                ]);
    }

    static IEnumerable<Feat> GetHeritages()
    {
        yield return new HeritageSelectionFeat(
            ModManager.RegisterFeatName("Earthly Wilds Kitsune"),
            "You are a creature of the material world, with an affinity closer to the wilds than urban society.",
            "You gain a jaws unarmed attack that deals 1d6 piercing damage. Your jaws are in the brawling group and have the finesse and unarmed traits."
            ).WithOnCreature(delegate (Creature cr)
            {
                cr.AddQEffect(new QEffect()
                {
                    AdditionalUnarmedStrike = new Item(IllustrationName.Jaws, "bite", [Trait.Finesse, Trait.Weapon, Trait.Melee, Trait.Unarmed, Trait.Brawling])
                    .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Piercing))
                });
            });
        yield return new HeritageSelectionFeat(
            FrozenWindKitsuneFeatName,
            "Your ancestors resided on snowy peaks.",
            "You gain cold resistance equal to half your level (minimum 1)."
            ).WithOnCreature(delegate (Creature cr)
            {
                cr.AddQEffect(QEffect.DamageResistance(DamageKind.Cold, cr.MaximumSpellRank));
            });
    }
}
