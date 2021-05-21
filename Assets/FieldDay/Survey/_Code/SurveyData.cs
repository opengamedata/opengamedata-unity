﻿using System.Collections;
using System.Collections.Generic;
using BeauData;
using UnityEngine;

namespace FieldDay
{
    public class SurveyData : ISerializedObject
    {
        private List<SurveyQuestion> m_Questions;

        public List<SurveyQuestion> Questions { get { return m_Questions; } }

        public void Serialize(Serializer ioSerializer)
        {
            ioSerializer.ObjectArray<SurveyQuestion>("questions", ref m_Questions);
        }
    }
}
