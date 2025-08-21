using UnityEngine;
using System.Text;

namespace FFramework.Kit
{
    [AddComponentMenu("CustomPrimitiveColliders/3D/Annular Sector Collider"), RequireComponent(typeof(MeshCollider))]
    public class AnnularSectorCollider : CustomColliderBase
    {
        [Tooltip("扇形内圆半径"), Min(0)]
        public float innerCircleRadius = 0.5f;

        [Tooltip("扇形外圆半径"), Min(1)]
        public float outerCircleRadius = 1f;

        [Tooltip("扇形角度"), Range(0, 360)]
        public float sectorAngle = 90f;

        [Tooltip("扇形厚度"), Min(0.1f)]
        public float sectorThickness = 0.1f;

        [Tooltip("细分数量"), Min(4)]
        public int numSegments = 32;

        private void Awake()
        {
            ReCreate(innerCircleRadius, outerCircleRadius, sectorAngle, sectorThickness, numSegments);
        }

#if UNITY_EDITOR
        private void Reset()
        {
            ReCreate(innerCircleRadius, outerCircleRadius, sectorAngle, sectorThickness, numSegments);
        }

        private void OnValidate()
        {
            if (innerCircleRadius >= outerCircleRadius)
                innerCircleRadius = outerCircleRadius - 0.1f;

            ReCreate(innerCircleRadius, outerCircleRadius, sectorAngle, sectorThickness, numSegments);
        }
#endif

        public void ReCreate(float innerRadius, float outerRadius, float angle, float thickness, int segments)
        {
            Mesh mesh = CreateMesh(innerRadius, outerRadius, angle, thickness, segments);

            if (meshCollider.sharedMesh != null)
            {
                meshCollider.sharedMesh.Clear();
                if (Application.isPlaying)
                {
                    Destroy(meshCollider.sharedMesh);
                }
                else
                {
                    DestroyImmediate(meshCollider.sharedMesh);
                }
            }

            meshCollider.sharedMesh = mesh;
        }

        private Mesh CreateMesh(float innerRadius, float outerRadius, float angle, float thickness, int segments)
        {
            // 参数验证
            innerRadius = Mathf.Max(0, innerRadius);
            outerRadius = Mathf.Max(innerRadius + 0.01f, outerRadius);
            angle = Mathf.Clamp(angle, 0, 360);
            thickness = Mathf.Max(0.01f, thickness);
            segments = Mathf.Max(4, segments);

            // 更新公共字段
            this.innerCircleRadius = innerRadius;
            this.outerCircleRadius = outerRadius;
            this.sectorAngle = angle;
            this.sectorThickness = thickness;
            this.numSegments = segments;

            Mesh mesh = new Mesh();

#if UNITY_EDITOR
            StringBuilder sbName = new StringBuilder("AnnularSector");
            sbName.Append("_inner_");
            sbName.Append(innerRadius);
            sbName.Append("_outer_");
            sbName.Append(outerRadius);
            sbName.Append("_angle_");
            sbName.Append(angle);
            sbName.Append("_thickness_");
            sbName.Append(thickness);
            mesh.name = sbName.ToString();
#endif

            // 计算顶点数量 - 修正顶点计数
            int verticesPerRing = segments + 1;
            int vertexCount = verticesPerRing * 4; // 内外圆弧各需要verticesPerRing个点，上下表面各需要两组
            Vector3[] vertices = new Vector3[vertexCount];
            Vector3[] normals = new Vector3[vertexCount];

            // 计算三角形数量 - 修正三角形计数
            int sideTriangles = segments * 2 * 2; // 内外侧面各2个三角形/段
            int capTriangles = segments * 2 * 2;  // 上下表面各2个三角形/段
            int endTriangles = (angle < 360) ? 4 : 0; // 如果不是完整圆环，两端各有2个三角形

            int totalTriangles = sideTriangles + capTriangles + endTriangles;
            int[] triangles = new int[totalTriangles * 3];

            float halfThickness = thickness / 2f;
            float startAngle = -angle / 2f;
            float angleStep = angle / segments;

            // 创建顶点
            int vertexIndex = 0;

            // 下表面 - 外圆弧
            for (int i = 0; i <= segments; i++)
            {
                float currentAngle = startAngle + i * angleStep;
                float x = Mathf.Sin(currentAngle * Mathf.Deg2Rad) * outerRadius;
                float z = Mathf.Cos(currentAngle * Mathf.Deg2Rad) * outerRadius;
                vertices[vertexIndex] = new Vector3(x, -halfThickness, z);
                normals[vertexIndex] = Vector3.down;
                vertexIndex++;
            }

            // 下表面 - 内圆弧
            for (int i = 0; i <= segments; i++)
            {
                float currentAngle = startAngle + i * angleStep;
                float x = Mathf.Sin(currentAngle * Mathf.Deg2Rad) * innerRadius;
                float z = Mathf.Cos(currentAngle * Mathf.Deg2Rad) * innerRadius;
                vertices[vertexIndex] = new Vector3(x, -halfThickness, z);
                normals[vertexIndex] = Vector3.down;
                vertexIndex++;
            }

            // 上表面 - 外圆弧
            for (int i = 0; i <= segments; i++)
            {
                float currentAngle = startAngle + i * angleStep;
                float x = Mathf.Sin(currentAngle * Mathf.Deg2Rad) * outerRadius;
                float z = Mathf.Cos(currentAngle * Mathf.Deg2Rad) * outerRadius;
                vertices[vertexIndex] = new Vector3(x, halfThickness, z);
                normals[vertexIndex] = Vector3.up;
                vertexIndex++;
            }

            // 上表面 - 内圆弧
            for (int i = 0; i <= segments; i++)
            {
                float currentAngle = startAngle + i * angleStep;
                float x = Mathf.Sin(currentAngle * Mathf.Deg2Rad) * innerRadius;
                float z = Mathf.Cos(currentAngle * Mathf.Deg2Rad) * innerRadius;
                vertices[vertexIndex] = new Vector3(x, halfThickness, z);
                normals[vertexIndex] = Vector3.up;
                vertexIndex++;
            }

            // 创建三角形
            int triangleIndex = 0;
            int bottomOuterStart = 0;
            int bottomInnerStart = segments + 1;
            int topOuterStart = (segments + 1) * 2;
            int topInnerStart = (segments + 1) * 3;

            // 侧面（外圆弧）
            for (int i = 0; i < segments; i++)
            {
                // 外侧面 - 确保索引在范围内
                if (bottomOuterStart + i < vertexCount &&
                    bottomOuterStart + i + 1 < vertexCount &&
                    topOuterStart + i < vertexCount)
                {
                    triangles[triangleIndex++] = bottomOuterStart + i;
                    triangles[triangleIndex++] = bottomOuterStart + i + 1;
                    triangles[triangleIndex++] = topOuterStart + i;

                    triangles[triangleIndex++] = bottomOuterStart + i + 1;
                    triangles[triangleIndex++] = topOuterStart + i + 1;
                    triangles[triangleIndex++] = topOuterStart + i;
                }
            }

            // 侧面（内圆弧）
            for (int i = 0; i < segments; i++)
            {
                // 内侧面 - 确保索引在范围内
                if (bottomInnerStart + i < vertexCount &&
                    topInnerStart + i < vertexCount &&
                    bottomInnerStart + i + 1 < vertexCount)
                {
                    triangles[triangleIndex++] = bottomInnerStart + i;
                    triangles[triangleIndex++] = topInnerStart + i;
                    triangles[triangleIndex++] = bottomInnerStart + i + 1;

                    triangles[triangleIndex++] = bottomInnerStart + i + 1;
                    triangles[triangleIndex++] = topInnerStart + i;
                    triangles[triangleIndex++] = topInnerStart + i + 1;
                }
            }

            // 侧面（两端，如果不是完整圆环）
            if (angle < 360)
            {
                // 开始端 - 确保索引在范围内
                if (bottomOuterStart < vertexCount &&
                    topOuterStart < vertexCount &&
                    bottomInnerStart < vertexCount &&
                    topInnerStart < vertexCount)
                {
                    triangles[triangleIndex++] = bottomOuterStart;
                    triangles[triangleIndex++] = topOuterStart;
                    triangles[triangleIndex++] = bottomInnerStart;

                    triangles[triangleIndex++] = bottomInnerStart;
                    triangles[triangleIndex++] = topOuterStart;
                    triangles[triangleIndex++] = topInnerStart;
                }

                // 结束端 - 确保索引在范围内
                int end = segments;
                if (bottomOuterStart + end < vertexCount &&
                    bottomInnerStart + end < vertexCount &&
                    topOuterStart + end < vertexCount &&
                    topInnerStart + end < vertexCount)
                {
                    triangles[triangleIndex++] = bottomOuterStart + end;
                    triangles[triangleIndex++] = bottomInnerStart + end;
                    triangles[triangleIndex++] = topOuterStart + end;

                    triangles[triangleIndex++] = bottomInnerStart + end;
                    triangles[triangleIndex++] = topInnerStart + end;
                    triangles[triangleIndex++] = topOuterStart + end;
                }
            }

            // 上下表面
            for (int i = 0; i < segments; i++)
            {
                // 下表面 - 确保索引在范围内
                if (bottomOuterStart + i < vertexCount &&
                    bottomInnerStart + i < vertexCount &&
                    bottomOuterStart + i + 1 < vertexCount &&
                    bottomInnerStart + i + 1 < vertexCount)
                {
                    triangles[triangleIndex++] = bottomOuterStart + i;
                    triangles[triangleIndex++] = bottomInnerStart + i;
                    triangles[triangleIndex++] = bottomOuterStart + i + 1;

                    triangles[triangleIndex++] = bottomInnerStart + i;
                    triangles[triangleIndex++] = bottomInnerStart + i + 1;
                    triangles[triangleIndex++] = bottomOuterStart + i + 1;
                }

                // 上表面 - 确保索引在范围内
                if (topOuterStart + i < vertexCount &&
                    topOuterStart + i + 1 < vertexCount &&
                    topInnerStart + i < vertexCount &&
                    topInnerStart + i + 1 < vertexCount)
                {
                    triangles[triangleIndex++] = topOuterStart + i;
                    triangles[triangleIndex++] = topOuterStart + i + 1;
                    triangles[triangleIndex++] = topInnerStart + i;

                    triangles[triangleIndex++] = topInnerStart + i;
                    triangles[triangleIndex++] = topOuterStart + i + 1;
                    triangles[triangleIndex++] = topInnerStart + i + 1;
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.triangles = triangles;

            mesh.RecalculateBounds();

            return mesh;
        }
    }
}