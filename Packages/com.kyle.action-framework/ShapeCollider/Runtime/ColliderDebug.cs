using UnityEngine;
namespace ShapeCollider
{
    public class ColliderDebug : MonoBehaviour
    {
        public enum ShapeType
        {
            Sphere,
            Cylinder,
            Pie,
            Box,
            Ray,
        }
        public ShapeType ColliderType;
        public float Radius = 1;
        [Range(1, 360)]
        public float Angle = 30;
        public float Height = 1;
        public Vector3 Size = Vector3.one;

        public Transform Body;
        public bool BodyIsSphere;
        public float BodyRadius = 1;
        public float BodyHeigh = 1;


        private void OnDrawGizmos()
        {
            Vector3 pos = transform.position;
            var eulerAngles = transform.rotation.eulerAngles;
            ShapeBox colliderBox = new ShapeBox { Position = pos, Extern = Size * 0.5f, YDegree = eulerAngles.y };
            ShapeSphere colliderSphere = new ShapeSphere { Position = pos, Radius = Radius };
            ShapeCylinder colliderCylinder = new ShapeCylinder { Position = pos, Radius = Radius, Height = Height };
            ShapePie colliderPie = new ShapePie { Position = pos, Angle = Angle, Height = Height, Radius = Radius, YDegree = eulerAngles.y };
            ShapeRay colliderLine = new ShapeRay { Position = pos, Direction = transform.forward, Length = Radius };
            switch (ColliderType)
            {
                case ShapeType.Sphere:
                    ColliderGizmos.DrawSphere(colliderSphere);
                    break;
                case ShapeType.Cylinder:
                    ColliderGizmos.DrawCylinder(colliderCylinder);
                    break;
                case ShapeType.Pie:
                    ColliderGizmos.DrawPie(colliderPie);
                    break;
                case ShapeType.Box:
                    ColliderGizmos.DrawBox(colliderBox);
                    break;
                case ShapeType.Ray:
                    ColliderGizmos.DrawLine(colliderLine);
                    break;
            }

            if (Body)
            {
                Vector3 center = Body.position;
                ShapeSphere bodySphere = new ShapeSphere { Position = center, Radius = BodyRadius };
                ShapeCylinder bodyCylinder = new ShapeCylinder { Position = center, Radius = BodyRadius, Height = BodyHeigh };
                Color color = Gizmos.color;
                bool isOverlap = false;
                if (BodyIsSphere)
                {
                    switch (ColliderType)
                    {
                        case ShapeType.Sphere:
                            isOverlap = ColliderOverlapUtil.Overlap(bodySphere, colliderSphere);
                            break;
                        case ShapeType.Cylinder:
                            isOverlap = ColliderOverlapUtil.Overlap(bodySphere, colliderCylinder);
                            break;
                        case ShapeType.Pie:
                            isOverlap = ColliderOverlapUtil.Overlap(bodySphere, colliderPie);
                            break;
                        case ShapeType.Box:
                            isOverlap = ColliderOverlapUtil.Overlap(bodySphere, colliderBox);
                            break;
                        case ShapeType.Ray:
                            isOverlap = ColliderOverlapUtil.Overlap(bodySphere, colliderLine, out float t);
                            if (isOverlap)
                            {
                                Vector3 pt = colliderLine.Position + colliderLine.Direction * t;
                                Vector3 p = new Vector3(pt.x, pt.y, pt.z);
                                Gizmos.DrawWireSphere(p, 0.1f);
                            }
                            break;
                    }
                }
                else
                {
                    switch (ColliderType)
                    {
                        case ShapeType.Sphere:
                            isOverlap = ColliderOverlapUtil.Overlap(bodyCylinder, colliderSphere);
                            break;
                        case ShapeType.Cylinder:
                            isOverlap = ColliderOverlapUtil.Overlap(bodyCylinder, colliderCylinder);
                            break;
                        case ShapeType.Pie:
                            isOverlap = ColliderOverlapUtil.Overlap(bodyCylinder, colliderPie);
                            break;
                        case ShapeType.Box:
                            isOverlap = ColliderOverlapUtil.Overlap(bodyCylinder, colliderBox);
                            break;
                        case ShapeType.Ray:
                            isOverlap = ColliderOverlapUtil.Overlap(bodyCylinder, colliderLine, out float t);
                            if (isOverlap)
                            {
                                Vector3 pt = colliderLine.Position + colliderLine.Direction * t;
                                Vector3 p = new Vector3(pt.x, pt.y, pt.z);
                                Gizmos.DrawWireSphere(p, 0.1f);
                            }
                            break;
                    }
                }
                if (isOverlap)
                    Gizmos.color = Color.red;
                else
                    Gizmos.color = Color.green;
                if (BodyIsSphere)
                {
                    ColliderGizmos.DrawSphere(bodySphere);
                }
                else
                {
                    ColliderGizmos.DrawCylinder(bodyCylinder);
                }

                Gizmos.color = color;
            }

        }
    }
}