using System.Collections;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace FieldDay
{
    public class AnswerButton : MonoBehaviour
    {
        private Transform m_Transform;

        public Transform Transform { get { return this.CacheComponent(ref m_Transform); } }

        public void Initialize()
        {
            
        }
    }
}
