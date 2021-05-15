using System.Collections.Generic;
using BeauUtil.Blocks;
using UnityEngine;

namespace FieldDay
{
    [CreateAssetMenu(menuName = "FieldDay/Survey Data Manager")]
    public class SurveyDataManager : ScriptableObject
    {
        [SerializeField] private SurveyDataPackage[] m_DefaultAssets = null;

        private Dictionary<string, SurveyDataPackage> m_Packages = new Dictionary<string, SurveyDataPackage>();

        private SurveyDataPackage.Generator m_Generator = new SurveyDataPackage.Generator();

        public SurveyDataPackage GetPackage(string name)
        {
            if (m_Packages.TryGetValue(name, out SurveyDataPackage package))
            {
                return package;
            }

            throw new System.ArgumentNullException($"No package '{name}' was found");
        }

        public void Apply()
        {
            if (m_Packages.Count >= m_DefaultAssets.Length)
            {
                return;
            }

            foreach(var asset in m_DefaultAssets)
            {
                asset.Parse(BlockParsingRules.Default, m_Generator);
                m_Packages.Add(asset.name, asset);
            }
        }
    }
}

