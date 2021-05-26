using System.Collections.Generic;
using BeauData;

namespace FieldDay
{
    public class SurveyData : ISerializedObject
    {
        private List<SurveyQuestion> m_Questions;

        public List<SurveyQuestion> Questions { get { return m_Questions; } }

        #region ISerializedObject

        public void Serialize(Serializer ioSerializer)
        {
            ioSerializer.ObjectArray<SurveyQuestion>("questions", ref m_Questions);
        }

        #endregion // ISerializedObject
    }
}
