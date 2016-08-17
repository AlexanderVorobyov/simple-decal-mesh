using UnityEngine;

namespace SimpleDecalMesh
{
    [AddComponentMenu("Effects/Decal Volume")]
    public class DecalVolume : TransformVolume
    {
        [Header("Surface Settings")]
        [SerializeField] [Range(0, 180)] private float m_normalsSmoothAngle = 60f;
        [SerializeField] [Range(0, 180)] private float m_maxAngle = 90;
        [SerializeField] [Range(0, 1)] private float m_distance = 0.01f;
        [SerializeField] private LayerMask m_layer = 1;

        [Header("UV")]
        [SerializeField] private Sprite m_sprite;
        
        public float NormalsSmoothAngle { get { return m_normalsSmoothAngle; } set { m_normalsSmoothAngle = value; } }
        public float MaxAngle { get { return m_maxAngle; } set { m_maxAngle = value; } }
        public float Distance { get { return m_distance; } set { m_distance = value; } }
        public LayerMask Layer { get { return m_layer; } set { m_layer = value; } }
        public Sprite Sprite { get { return m_sprite; } set { m_sprite = value; } }

        public MeshFilter MeshFilter
        {
            get
            {
                if (m_meshFilter == null)
                {
                    m_meshFilter = GetComponent<MeshFilter>();
                }
                return m_meshFilter;
            }
        }

        public static Color EditorColor { get { return new Color(0.5f, 0.75f, 1f); } }

        private MeshFilter m_meshFilter;
        
        public void Create()
        {
            MeshFilter.sharedMesh = DecalMesh.Create(this, m_maxAngle, m_distance, m_normalsSmoothAngle);
        }

        public void Clear()
        {
            MeshFilter.sharedMesh = null;
        }

        [ContextMenu("Reset Settings")]
        public void ResetSettings()
        {
            Volume = new Volume(Vector3.zero, Vector3.one);
            m_maxAngle = 90f;
            m_distance = 0.01f;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.color = EditorColor;
            Gizmos.DrawWireCube(Origin, Size);
        }
    }
}