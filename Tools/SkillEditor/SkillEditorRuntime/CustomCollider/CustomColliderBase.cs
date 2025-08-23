using UnityEngine;

namespace FFramework.Kit
{
    public abstract class CustomColliderBase : MonoBehaviour
    {
        private MeshCollider m_meshCollider;

        protected MeshCollider meshCollider
        {
            get
            {
                if (m_meshCollider == null)
                {
                    m_meshCollider = GetComponent<MeshCollider>();

                    if (m_meshCollider == null)
                    {
                        m_meshCollider = gameObject.AddComponent<MeshCollider>();
                        meshCollider.convex = true;
                    }
                }
                return m_meshCollider;
            }
        }

        // private PolygonCollider2D m_polygonCollider2d;

        // protected PolygonCollider2D polygonCollider2d
        // {
        //     get
        //     {
        //         if (m_polygonCollider2d == null)
        //         {
        //             m_polygonCollider2d = GetComponent<PolygonCollider2D>();

        //             if (m_polygonCollider2d == null)
        //             {
        //                 m_polygonCollider2d = gameObject.AddComponent<PolygonCollider2D>();
        //                 meshCollider.convex = true;
        //             }
        //         }
        //         return m_polygonCollider2d;
        //     }
        // }
    }
}