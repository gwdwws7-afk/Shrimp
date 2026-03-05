using UnityEngine;

namespace ThirdPersonController
{
    [CreateAssetMenu(fileName = "SkillLoadoutConfig", menuName = "Skills/Skill Loadout Config")]
    public class SkillLoadoutConfig : ScriptableObject
    {
        public string resourcesFolder = "Skills";
        public string[] skillResourceNames = new string[6]
        {
            "SKILL_Whirlwind",
            "SKILL_Shockwave",
            "SKILL_DashAttack",
            "SKILL_Berserk",
            "SKILL_Pull",
            "SKILL_Ultimate"
        };
    }
}
