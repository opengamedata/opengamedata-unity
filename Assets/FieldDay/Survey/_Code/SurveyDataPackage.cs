using System;
using System.Collections.Generic;
using BeauUtil.Blocks;
using BeauUtil.Tags;
using UnityEngine.Scripting;

namespace FieldDay
{
    /*
    public class SurveyDataPackage : ScriptableDataBlockPackage<SurveyQuestion>
    {
        [NonSerialized] private readonly Dictionary<string, SurveyQuestion> m_Questions = new Dictionary<string, SurveyQuestion>();

        [NonSerialized] private List<string> m_DefaultAnswers = new List<string>();

        [BlockMeta("defaultAnswers"), Preserve]
        private void AddAnswers(string line)
        {
            string[] defaultAnswers = line.Split(',');
            
            foreach (string answer in defaultAnswers)
            {
                if (!m_DefaultAnswers.Contains(answer))
                {
                    m_DefaultAnswers.Add(answer.Trim());
                }
            }
        }

        public Dictionary<string, SurveyQuestion> Questions { get { return m_Questions; } }

        public List<string> DefaultAnswers { get { return m_DefaultAnswers; } }

        public override int Count { get { return m_Questions.Count; } }

        public override IEnumerator<SurveyQuestion> GetEnumerator()
        {
            return m_Questions.Values.GetEnumerator();
        }

        #if UNITY_EDITOR

        [ScriptedExtension(1, "survey")]
        private class Importer : ImporterBase<SurveyDataPackage> { }

        #endif // UNITY_EDITOR

        public class Generator : GeneratorBase<SurveyDataPackage>
        {
            public override bool TryCreateBlock(IBlockParserUtil inUtil, SurveyDataPackage inPackage, TagData inId, out SurveyQuestion outBlock)
            {
                string id = inId.Id.ToString();
                outBlock = new SurveyQuestion(id);
                inPackage.m_Questions.Add(id, outBlock);
                return true;
            }
        }
    }
    */
}
