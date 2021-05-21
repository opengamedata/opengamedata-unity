using System.Collections.Generic;
using BeauData;

namespace FieldDay
{
    public class SurveyQuestion : ISerializedObject
    {
        private string m_Id;
        private string m_Text;
        private List<string> m_Answers;

        public string Id { get { return m_Id; } }
        public string Text { get { return m_Text; } }
        public List<string> Answers { get { return m_Answers; } }

        public void Serialize(Serializer ioSerializer)
        {
            ioSerializer.Serialize("id", ref m_Id);
            ioSerializer.Serialize("text", ref m_Text);
            ioSerializer.Array("answers", ref m_Answers);
        }
    }
}
