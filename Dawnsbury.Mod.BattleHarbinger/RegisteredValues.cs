using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Modding;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;

namespace Dawnsbury.Mods.BattleHarbinger.RegisteredValues
{
    // Class to contain any registered enum members which are used to refer to things, e.g. QEffectId, ItemName, FeatName

    internal class ModTrait
    {
        public static readonly Trait BattleHarbinger = ModManager.RegisterTrait("Battle Harbinger");
        public static readonly Trait BattleAura = ModManager.RegisterTrait("Battle Aura", new TraitProperties("Battle Aura", false));
    }

    internal class ModFeatName
    {
        public static readonly FeatName BattleHarbingerDoctrine = ModManager.RegisterFeatName("Battle Harbinger");
        public static readonly FeatName BattleHarbingerDedication = ModManager.RegisterFeatName("Battle Harbinger Dedication");
        public static readonly FeatName AuraEnhancement = ModManager.RegisterFeatName("Aura Enhancement");
    }

    internal class ModQEffectId
    {
        public static readonly QEffectId TandemOnslaughtOncePerTurn = ModManager.RegisterEnumMember<QEffectId>("tandemOnslaughtUsedOnceThisTurnAlready");
        public static readonly QEffectId RolledAgainstMalediction = ModManager.RegisterEnumMember<QEffectId>("RolledAgainstMalediction");
        public static readonly QEffectId FailedAgainstMalediction = ModManager.RegisterEnumMember<QEffectId>("FailedAgainstMalediction");
    }

    // Spells registered in ModSpells
    internal class ModSpellId
    {
        static public SpellId Benediction;
        static public SpellId Malediction;
    }
}
