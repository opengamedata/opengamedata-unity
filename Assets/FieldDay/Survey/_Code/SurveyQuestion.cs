using System.Collections.Generic;
using BeauData;

namespace FieldDay
{
    public class SurveyQuestion : ISerializedObject
    {
        private string m_Id;
        private string m_Type;
        private string m_Text;
        private List<string> m_Answers;

        #region Accessors

        public string Id { get { return m_Id; } }
        public string Type { get { return m_Type; } }
        public string Text { get { return m_Text; } }
        public List<string> Answers { get { return m_Answers; } }

        #endregion // Accessors

        #region ISerializedObject

        public void Serialize(Serializer ioSerializer)
        {
            ioSerializer.Serialize("id", ref m_Id);
            ioSerializer.Serialize("type", ref m_Type);
            ioSerializer.Serialize("text", ref m_Text);
            ioSerializer.Array("answers", ref m_Answers);
        }

        #endregion // ISerializedObject
    }
}
