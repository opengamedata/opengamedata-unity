using System.Collections.Generic;
using BeauUtil.Blocks;
using UnityEngine.Scripting;

namespace FieldDay
{
    public class SurveyQuestion : IDataBlock
    {
        protected string m_Id = null;

        private List<string> m_Answers = new List<string>();

        [BlockMeta("answers"), Preserve]
        private void AddAnswers(string line)
        {
            string[] answers = line.Split(',');

            foreach(string answer in answers)
            {
                m_Answers.Add(answer.Trim());
            }
        }
        
        [BlockContent] private string m_Question = null;

        public SurveyQuestion(string inId)
        {
            m_Id = inId;
        }

        public string Id { get { return m_Id; } }
        public List<string> Answers { get { return m_Answers; } }
        public string Question { get { return m_Question; } }
    }
}
