using UnityEngine;

namespace HeroManagement
{
    public enum SkillType { Physical, Magical, Utility, Passive }

    [CreateAssetMenu(fileName = "New Skill", menuName = "Hero Management/Skill Data")]
    public class SkillData : ScriptableObject
    {
        [Header("Skill Identity")]
        public string skillName;
        [TextArea(2, 4)]
        public string description;
        public SkillType type;

        [Header("Combat Stats")]
        public int baseDamageOrHeal;
        public int manaCost;

        [Header("Animation")]
        [Tooltip("The exact Trigger name in the Unity Animator (e.g., 'CastFireball')")]
        public string animTrigger;

        [Header("Economy (Scroll Form)")]
        [Tooltip("If converted to a scroll, how much can it be sold for?")]
        public int scrollMarketValue;
    }
}