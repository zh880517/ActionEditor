using UnityEngine;

namespace ShapeCollider
{
    public static class ColliderGizmos
    {

        public static void DrawSphere(ShapeSphere sphere)
        {
            DrawSphere(sphere.Position, sphere.Radius);
        }

        public static void DrawSphere(Vector3 pos, float radius)
        {
            Gizmos.DrawWireSphere(pos, radius);
        }

        public static void DrawBox(ShapeBox box)
        {
            var matrix = Gizmos.matrix;
            Quaternion r = Quaternion.Euler(0, box.YDegree, 0);
            Gizmos.matrix = Matrix4x4.TRS(box.Position, r, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, box.Extern * 2);
            Gizmos.matrix = matrix;
        }

        public static void DrawAABB(ShapeAABB box)
        {
            Gizmos.DrawWireCube(box.Center, box.Extents * 2);
        }

        public static void DrawLine(ShapeRay ray)
        {
            if (ray.Direction.sqrMagnitude <= 0.000001f || ray.Length <= 0)
                return;

            Gizmos.DrawLine(ray.Position, ray.Position + ray.Direction.normalized * ray.Length);
        }

        public static void DrawSegment(ShapeSegment segment)
        {
            Gizmos.DrawLine(segment.Start, segment.End);
        }

        public static void DrawCylinder(ShapeCylinder cylinder)
        {
            DrawCylinder(cylinder.Position, cylinder.Radius, cylinder.Height);
        }

        public static void DrawCapsule(ShapeCapsule capsule)
        {
            if (capsule.Radius <= 0.001f)
                return;

            Vector3 start = capsule.Position;
            Vector3 end = start;
            if (capsule.Direction.sqrMagnitude > 0.000001f && capsule.Length > 0)
                end = start + capsule.Direction.normalized * capsule.Length;

            Gizmos.DrawWireSphere(start, capsule.Radius);
            Gizmos.DrawWireSphere(end, capsule.Radius);
            if ((end - start).sqrMagnitude <= 0.000001f)
                return;

            Vector3 axis = (end - start).normalized;
            Vector3 right = Vector3.Cross(axis, Vector3.up);
            if (right.sqrMagnitude <= 0.000001f)
                right = Vector3.Cross(axis, Vector3.right);

            right.Normalize();
            Vector3 forward = Vector3.Cross(axis, right).normalized;
            Gizmos.DrawLine(start + right * capsule.Radius, end + right * capsule.Radius);
            Gizmos.DrawLine(start - right * capsule.Radius, end - right * capsule.Radius);
            Gizmos.DrawLine(start + forward * capsule.Radius, end + forward * capsule.Radius);
            Gizmos.DrawLine(start - forward * capsule.Radius, end - forward * capsule.Radius);
        }

        public static void DrawPie(ShapePie pie)
        {
            DrawPie(pie.Position, pie.YDegree, pie.Angle, pie.Radius, pie.Height);
        }

        public static void DrawCylinder(Vector3 pos, float radius, float height)
        {
            if (radius <= 0.001 || height <= 0)
                return;
            if (radius <= 0.001)
                return;
            float perimeter = 2 * radius * Mathf.PI;
            int count = Mathf.CeilToInt(perimeter * 2);
            //分成偶数段
            if (count % 2 == 1)
                count++;
            if (count < 8)
                count = 8;
            float stepDegress = 360f / count;
            Vector3 heightOffset = new Vector3(0, height, 0);
            Vector3 center = pos;
            Vector3 beginPoint = center + new Vector3(radius, 0, 0);
            Vector3 start = beginPoint;
            for (int i = 1; i < count; ++i)
            {
                float angle = i * stepDegress * Mathf.Deg2Rad;
                float x = radius * Mathf.Cos(angle);
                float z = radius * Mathf.Sin(angle);
                Vector3 end = center + new Vector3(x, 0, z);
                Gizmos.DrawLine(start, end);
                Gizmos.DrawLine(start + heightOffset, end + heightOffset);
                start = end;
            }
            Gizmos.DrawLine(start, beginPoint);
            Gizmos.DrawLine(start + heightOffset, beginPoint + heightOffset);

            //画十字线
            Vector3 top = center + new Vector3(0, 0, radius);
            Vector3 bottom = center + new Vector3(0, 0, -radius);
            Vector3 left = center + new Vector3(-radius, 0, 0);
            Vector3 right = center + new Vector3(radius, 0, 0);

            Gizmos.DrawLine(left, right);
            Gizmos.DrawLine(left + heightOffset, right + heightOffset);
            Gizmos.DrawLine(top, bottom);
            Gizmos.DrawLine(top + heightOffset, bottom + heightOffset);

            Gizmos.DrawLine(top, top + heightOffset);
            Gizmos.DrawLine(bottom, bottom + heightOffset);
            Gizmos.DrawLine(left, left + heightOffset);
            Gizmos.DrawLine(right, right + heightOffset);
        }

        public static void DrawCircle(Vector3 pos, float radius)
        {
            if (radius <= 0.001)
                return;
            float perimeter = 2 * radius * Mathf.PI;
            int count = Mathf.CeilToInt(perimeter * 2);
            //分成偶数段
            if (count % 2 == 1)
                count++;
            if (count < 8)
                count = 8;
            float stepDegree = 360f / count;
            Vector3 center = pos;
            Vector3 beginPoint = center + new Vector3(radius, 0, 0);
            Vector3 start = beginPoint;
            for (int i = 1; i < count; ++i)
            {
                float angle = i * stepDegree * Mathf.Deg2Rad;
                float x = radius * Mathf.Cos(angle);
                float z = radius * Mathf.Sin(angle);
                Vector3 end = center + new Vector3(x, 0, z);
                Gizmos.DrawLine(start, end);
                start = end;
            }
            Gizmos.DrawLine(start, beginPoint);
        }

        public static void DrawPie(Vector3 pos, float yDegree, float angle, float radius, float height)
        {
            if (height <= 0.001 || radius <= 0.001 || angle <= 0)
                return;

            float perimeter = 2 * radius * Mathf.PI * (angle / 360);
            int count = Mathf.CeilToInt(perimeter * 2);
            Vector3 heightOffset = new Vector3(0, height, 0);
            Vector3 center = pos;
            //右侧开始
            Vector2 startDir = ShapeSDFUtil.Rotate(new Vector2(0, 1), -(angle * 0.5f + yDegree));
            Vector3 beginPoint = center + new Vector3(startDir.x, 0, startDir.y) * radius;
            Gizmos.DrawLine(center, center + heightOffset);//中心
            //起始边
            Gizmos.DrawLine(center, beginPoint);
            Gizmos.DrawLine(center + heightOffset, beginPoint + heightOffset);
            Gizmos.DrawLine(beginPoint, beginPoint + heightOffset);
            //画圆弧
            float stepDegree = angle / count;
            Vector3 start = beginPoint;
            for (int i = 1; i <= count; ++i)
            {
                float offsetDegree = Mathf.Min(i * stepDegree, angle);
                Vector2 dir = ShapeSDFUtil.Rotate(startDir, offsetDegree);
                Vector3 end = center + new Vector3(dir.x, 0, dir.y) * radius;
                Gizmos.DrawLine(start, end);
                Gizmos.DrawLine(start + heightOffset, end + heightOffset);
                start = end;
            }
            Gizmos.DrawLine(center, start);
            Gizmos.DrawLine(center + heightOffset, start + heightOffset);
            Gizmos.DrawLine(start, start + heightOffset);
            //画中线
            Vector2 forward = ShapeSDFUtil.Rotate(startDir, angle * 0.5f);
            Vector3 dirEnd = center + new Vector3(forward.x, 0, forward.y) * radius;
            Gizmos.DrawLine(dirEnd, dirEnd + heightOffset);
            Gizmos.DrawLine(center + heightOffset, dirEnd + heightOffset);
            Gizmos.DrawLine(center, dirEnd);
        }
    }

}