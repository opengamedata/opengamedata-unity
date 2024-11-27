using UnityEngine;
using UnityEngine.UI;

namespace OGD {
    [AddComponentMenu("OGD/Axis Layout Group")]
    public sealed class AxisLayoutGroup : HorizontalOrVerticalLayoutGroup {
        [SerializeField] private bool m_IsVertical;

        public bool IsVertical {
            get { return m_IsVertical; }
            set {
                if (m_IsVertical != value) {
                    m_IsVertical = value;
                    SetDirty();
                }
            }
        }

        public override void CalculateLayoutInputHorizontal() {
            base.CalculateLayoutInputHorizontal();
            CalcAlongAxis(0, m_IsVertical);
        }

        public override void CalculateLayoutInputVertical() {
            CalcAlongAxis(1, m_IsVertical);
        }

        public override void SetLayoutHorizontal() {
            SetChildrenAlongAxis(0, m_IsVertical);
        }

        public override void SetLayoutVertical() {
            SetChildrenAlongAxis(1, m_IsVertical);
        }
    }
}