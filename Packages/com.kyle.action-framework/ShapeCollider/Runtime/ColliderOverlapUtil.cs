using System;
using UnityEngine;
namespace ShapeCollider
{
    public static class ColliderOverlapUtil
    {
        private const float Epsilon = 0.000001f;

        private static Vector2 ToV2(this Vector3 v)
        {
            return new Vector2(v.x, v.z);
        }

        private static float ProjectRadius(float radius, float offsetToCenter)
        {
            return Mathf.Sqrt(Mathf.Max(0, (radius * radius) - (offsetToCenter * offsetToCenter)));
        }

        private static bool TryNormalizeRay(in ShapeRay ray, out Vector3 direction, out float length)
        {
            length = Mathf.Max(0, ray.Length);
            float sqrMagnitude = ray.Direction.sqrMagnitude;
            if (sqrMagnitude < Epsilon)
            {
                direction = Vector3.zero;
                return false;
            }

            direction = ray.Direction / Mathf.Sqrt(sqrMagnitude);
            return true;
        }

        private static bool TryGetCapsuleSegment(in ShapeCapsule capsule, out Vector3 start, out Vector3 end)
        {
            start = capsule.Position;
            float length = Mathf.Max(0, capsule.Length);
            float sqrMagnitude = capsule.Direction.sqrMagnitude;
            if (sqrMagnitude < Epsilon || length <= Epsilon)
            {
                end = start;
                return false;
            }

            end = start + capsule.Direction / Mathf.Sqrt(sqrMagnitude) * length;
            return true;
        }

        private static float SqrDistancePointAABB(Vector3 point, in ShapeAABB aabb)
        {
            Vector3 extents = new Vector3(Mathf.Abs(aabb.Extents.x), Mathf.Abs(aabb.Extents.y), Mathf.Abs(aabb.Extents.z));
            Vector3 d = new Vector3(
                Mathf.Max(Mathf.Abs(point.x - aabb.Center.x) - extents.x, 0),
                Mathf.Max(Mathf.Abs(point.y - aabb.Center.y) - extents.y, 0),
                Mathf.Max(Mathf.Abs(point.z - aabb.Center.z) - extents.z, 0));
            return d.sqrMagnitude;
        }

        private static bool IsPointInAABB(Vector3 point, in ShapeAABB aabb)
        {
            Vector3 extents = new Vector3(Mathf.Abs(aabb.Extents.x), Mathf.Abs(aabb.Extents.y), Mathf.Abs(aabb.Extents.z));
            Vector3 offset = point - aabb.Center;
            return Mathf.Abs(offset.x) <= extents.x && Mathf.Abs(offset.y) <= extents.y && Mathf.Abs(offset.z) <= extents.z;
        }

        private static Vector3 Abs(Vector3 vector)
        {
            return new Vector3(Mathf.Abs(vector.x), Mathf.Abs(vector.y), Mathf.Abs(vector.z));
        }

        private static Vector3 ToBoxLocal(Vector3 point, in ShapeBox box)
        {
            Vector3 offset = point - box.Position;
            Vector2 xz = ShapeSDFUtil.Rotate(offset.ToV2(), -box.YDegree);
            return new Vector3(xz.x, offset.y, xz.y);
        }

        private static Vector3 ToBoxLocalDirection(Vector3 direction, in ShapeBox box)
        {
            Vector2 xz = ShapeSDFUtil.Rotate(direction.ToV2(), -box.YDegree);
            return new Vector3(xz.x, direction.y, xz.y);
        }

        private static ShapeAABB BoxLocalAABB(in ShapeBox box)
        {
            return new ShapeAABB { Center = Vector3.zero, Extents = Abs(box.Extern) };
        }

        private static void GetBoxCorners2D(in ShapeBox box, Span<Vector2> corners)
        {
            Vector3 extents = Abs(box.Extern);
            Vector2 right = ShapeSDFUtil.Rotate(new Vector2(1, 0), box.YDegree) * extents.x;
            Vector2 forward = ShapeSDFUtil.Rotate(new Vector2(0, 1), box.YDegree) * extents.z;
            Vector2 center = box.Position.ToV2();
            corners[0] = center - right - forward;
            corners[1] = center + right - forward;
            corners[2] = center + right + forward;
            corners[3] = center - right + forward;
        }

        private static bool OverlapOBB2D(in ShapeBox c1, in ShapeBox c2)
        {
            Span<Vector2> corners1 = stackalloc Vector2[4];
            Span<Vector2> corners2 = stackalloc Vector2[4];
            GetBoxCorners2D(c1, corners1);
            GetBoxCorners2D(c2, corners2);
            return OverlapOnBoxAxes(corners1, c1.YDegree, corners2)
                && OverlapOnBoxAxes(corners1, c2.YDegree, corners2);
        }

        private static bool OverlapOnBoxAxes(ReadOnlySpan<Vector2> corners1, float yDegree, ReadOnlySpan<Vector2> corners2)
        {
            Vector2 axisX = ShapeSDFUtil.Rotate(new Vector2(1, 0), yDegree);
            Vector2 axisZ = ShapeSDFUtil.Rotate(new Vector2(0, 1), yDegree);
            return OverlapProjection(corners1, corners2, axisX) && OverlapProjection(corners1, corners2, axisZ);
        }

        private static bool OverlapProjection(ReadOnlySpan<Vector2> corners1, ReadOnlySpan<Vector2> corners2, Vector2 axis)
        {
            Project(corners1, axis, out float min1, out float max1);
            Project(corners2, axis, out float min2, out float max2);
            return max1 >= min2 && min1 <= max2;
        }

        private static void Project(ReadOnlySpan<Vector2> corners, Vector2 axis, out float min, out float max)
        {
            min = Vector2.Dot(corners[0], axis);
            max = min;
            for (int i = 1; i < corners.Length; i++)
            {
                float value = Vector2.Dot(corners[i], axis);
                min = Mathf.Min(min, value);
                max = Mathf.Max(max, value);
            }
        }

        private static bool OverlapHeight(float min1, float max1, float min2, float max2)
        {
            return max1 >= min2 && min1 <= max2;
        }

        private static bool IsPointInSector(Vector2 point, in ShapePie pie)
        {
            return ShapeSDFUtil.SectorSDF(point, pie.Position.ToV2(), pie.YDegree, pie.Angle, Mathf.Max(0, pie.Radius)) <= Epsilon;
        }

        private static bool SegmentIntersectsSector(Vector2 start, Vector2 end, in ShapePie pie)
        {
            if (IsPointInSector(start, pie) || IsPointInSector(end, pie))
                return true;

            Vector2 center = pie.Position.ToV2();
            if (SegmentSDFSqr(center, start, end) <= Epsilon && IsPointInSector(center, pie))
                return true;

            float halfAngle = pie.Angle * 0.5f;
            Vector2 leftDir = ShapeSDFUtil.Rotate(new Vector2(0, 1), pie.YDegree - halfAngle);
            Vector2 rightDir = ShapeSDFUtil.Rotate(new Vector2(0, 1), pie.YDegree + halfAngle);
            float radius = Mathf.Max(0, pie.Radius);
            Vector2 leftEnd = center + leftDir * radius;
            Vector2 rightEnd = center + rightDir * radius;
            if (SegmentSegmentSqrDistance2D(start, end, center, leftEnd) <= Epsilon)
                return true;
            if (SegmentSegmentSqrDistance2D(start, end, center, rightEnd) <= Epsilon)
                return true;

            return SegmentIntersectsSectorArc(start, end, pie);
        }

        private static bool SegmentIntersectsSectorArc(Vector2 start, Vector2 end, in ShapePie pie)
        {
            Vector2 center = pie.Position.ToV2();
            Vector2 d = end - start;
            float a = Vector2.Dot(d, d);
            if (a <= Epsilon)
                return false;

            Vector2 f = start - center;
            float b = 2 * Vector2.Dot(f, d);
            float radius = Mathf.Max(0, pie.Radius);
            float c = Vector2.Dot(f, f) - radius * radius;
            float discriminant = b * b - 4 * a * c;
            if (discriminant < 0)
                return false;

            float sqrt = Mathf.Sqrt(discriminant);
            float inv = 1 / (2 * a);
            float t1 = (-b - sqrt) * inv;
            float t2 = (-b + sqrt) * inv;
            if (t1 >= 0 && t1 <= 1 && IsPointInSector(start + d * t1, pie))
                return true;

            return t2 >= 0 && t2 <= 1 && IsPointInSector(start + d * t2, pie);
        }

        private static float SegmentSegmentSqrDistance2D(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2)
        {
            Vector2 d1 = q1 - p1;
            Vector2 d2 = q2 - p2;
            Vector2 r = p1 - p2;
            float a = Vector2.Dot(d1, d1);
            float e = Vector2.Dot(d2, d2);
            float f = Vector2.Dot(d2, r);

            float s;
            float t;
            if (a <= Epsilon && e <= Epsilon)
                return (p1 - p2).sqrMagnitude;

            if (a <= Epsilon)
            {
                s = 0;
                t = Mathf.Clamp01(f / e);
            }
            else
            {
                float c = Vector2.Dot(d1, r);
                if (e <= Epsilon)
                {
                    t = 0;
                    s = Mathf.Clamp01(-c / a);
                }
                else
                {
                    float b = Vector2.Dot(d1, d2);
                    float denom = a * e - b * b;
                    s = denom > Epsilon ? Mathf.Clamp01((b * f - c * e) / denom) : 0;
                    t = (b * s + f) / e;

                    if (t < 0)
                    {
                        t = 0;
                        s = Mathf.Clamp01(-c / a);
                    }
                    else if (t > 1)
                    {
                        t = 1;
                        s = Mathf.Clamp01((b - c) / a);
                    }
                }
            }

            Vector2 c1 = p1 + d1 * s;
            Vector2 c2 = p2 + d2 * t;
            return (c1 - c2).sqrMagnitude;
        }

        private static bool OverlapSectorAABB2D(in ShapePie pie, Vector2 min, Vector2 max)
        {
            Vector2 p0 = new Vector2(min.x, min.y);
            Vector2 p1 = new Vector2(max.x, min.y);
            Vector2 p2 = new Vector2(max.x, max.y);
            Vector2 p3 = new Vector2(min.x, max.y);
            if (IsPointInSector(p0, pie) || IsPointInSector(p1, pie) || IsPointInSector(p2, pie) || IsPointInSector(p3, pie))
                return true;

            Vector2 center = pie.Position.ToV2();
            if (center.x >= min.x && center.x <= max.x && center.y >= min.y && center.y <= max.y)
                return true;

            return SegmentIntersectsSector(p0, p1, pie)
                || SegmentIntersectsSector(p1, p2, pie)
                || SegmentIntersectsSector(p2, p3, pie)
                || SegmentIntersectsSector(p3, p0, pie);
        }

        private static bool OverlapSectorOBB2D(in ShapePie pie, in ShapeBox box)
        {
            Span<Vector2> corners = stackalloc Vector2[4];
            GetBoxCorners2D(box, corners);
            if (IsPointInSector(corners[0], pie) || IsPointInSector(corners[1], pie) || IsPointInSector(corners[2], pie) || IsPointInSector(corners[3], pie))
                return true;

            Vector3 extents = Abs(box.Extern);
            Vector3 localPieCenter = ToBoxLocal(pie.Position, box);
            if (Mathf.Abs(localPieCenter.x) <= extents.x && Mathf.Abs(localPieCenter.z) <= extents.z)
                return true;

            return SegmentIntersectsSector(corners[0], corners[1], pie)
                || SegmentIntersectsSector(corners[1], corners[2], pie)
                || SegmentIntersectsSector(corners[2], corners[3], pie)
                || SegmentIntersectsSector(corners[3], corners[0], pie);
        }

        private static bool SectorArcIntersectsSectorArc(in ShapePie c1, in ShapePie c2)
        {
            Vector2 center1 = c1.Position.ToV2();
            Vector2 center2 = c2.Position.ToV2();
            Vector2 delta = center2 - center1;
            float distance = delta.magnitude;
            if (distance <= Epsilon)
                return false;

            float radius1 = Mathf.Max(0, c1.Radius);
            float radius2 = Mathf.Max(0, c2.Radius);
            if (distance > radius1 + radius2 || distance < Mathf.Abs(radius1 - radius2))
                return false;

            float a = (radius1 * radius1 - radius2 * radius2 + distance * distance) / (2 * distance);
            float hSqr = radius1 * radius1 - a * a;
            if (hSqr < 0)
                return false;

            Vector2 dir = delta / distance;
            Vector2 basePoint = center1 + dir * a;
            Vector2 perp = new Vector2(-dir.y, dir.x) * Mathf.Sqrt(hSqr);
            Vector2 p1 = basePoint + perp;
            Vector2 p2 = basePoint - perp;
            return (IsPointInSector(p1, c1) && IsPointInSector(p1, c2))
                || (IsPointInSector(p2, c1) && IsPointInSector(p2, c2));
        }

        private static bool OverlapPieCapsuleByDistance(in ShapePie pie, Vector3 start, Vector3 end, float radius)
        {
            return SqrDistanceSegmentPie(start, end, pie) <= radius * radius;
        }

        private static bool OverlapPieCapsuleYBoundarySample(in ShapePie pie, Vector3 start, Vector3 segment, float y, float radius)
        {
            if (Mathf.Abs(segment.y) <= Epsilon)
                return false;

            float t = (y - start.y) / segment.y;
            if (t < 0 || t > 1)
                return false;

            return OverlapPieCapsuleSample(pie, start + segment * t, radius);
        }

        private static bool OverlapPieCapsuleSample(in ShapePie pie, Vector3 point, float radius)
        {
            float bottom = pie.Position.y;
            float top = pie.Position.y + Mathf.Max(0, pie.Height);
            float offsetY = 0;
            if (point.y < bottom)
                offsetY = bottom - point.y;
            else if (point.y > top)
                offsetY = point.y - top;

            if (offsetY > radius)
                return false;

            float horizontalRadius = Mathf.Sqrt(Mathf.Max(0, radius * radius - offsetY * offsetY));
            float sdf = ShapeSDFUtil.SectorSDF(point.ToV2(), pie.Position.ToV2(), pie.YDegree, pie.Angle, Mathf.Max(0, pie.Radius));
            return sdf <= horizontalRadius;
        }

        private static float SegmentSegmentSqrDistance(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2)
        {
            Vector3 d1 = q1 - p1;
            Vector3 d2 = q2 - p2;
            Vector3 r = p1 - p2;
            float a = Vector3.Dot(d1, d1);
            float e = Vector3.Dot(d2, d2);
            float f = Vector3.Dot(d2, r);

            float s;
            float t;
            if (a <= Epsilon && e <= Epsilon)
                return Vector3.SqrMagnitude(p1 - p2);

            if (a <= Epsilon)
            {
                s = 0;
                t = Mathf.Clamp01(f / e);
            }
            else
            {
                float c = Vector3.Dot(d1, r);
                if (e <= Epsilon)
                {
                    t = 0;
                    s = Mathf.Clamp01(-c / a);
                }
                else
                {
                    float b = Vector3.Dot(d1, d2);
                    float denom = a * e - b * b;
                    s = denom > Epsilon ? Mathf.Clamp01((b * f - c * e) / denom) : 0;
                    t = (b * s + f) / e;

                    if (t < 0)
                    {
                        t = 0;
                        s = Mathf.Clamp01(-c / a);
                    }
                    else if (t > 1)
                    {
                        t = 1;
                        s = Mathf.Clamp01((b - c) / a);
                    }
                }
            }

            Vector3 c1 = p1 + d1 * s;
            Vector3 c2 = p2 + d2 * t;
            return Vector3.SqrMagnitude(c1 - c2);
        }

        private static float SqrDistanceSegmentAABB(Vector3 start, Vector3 end, in ShapeAABB aabb)
        {
            ShapeSegment segment = new ShapeSegment { Start = start, End = end };
            if (Overlap(segment, aabb))
                return 0;

            Vector3 extents = Abs(aabb.Extents);
            Vector3 min = aabb.Center - extents;
            Vector3 max = aabb.Center + extents;
            Vector3 direction = end - start;
            Span<float> candidates = stackalloc float[8];
            int count = 0;
            AddCandidate(candidates, ref count, 0);
            AddCandidate(candidates, ref count, 1);
            AddAxisBoundaries(candidates, ref count, start.x, direction.x, min.x, max.x);
            AddAxisBoundaries(candidates, ref count, start.y, direction.y, min.y, max.y);
            AddAxisBoundaries(candidates, ref count, start.z, direction.z, min.z, max.z);
            SortCandidates(candidates.Slice(0, count));

            float sqrDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
                sqrDistance = Mathf.Min(sqrDistance, SqrDistancePointAABB(start + direction * candidates[i], aabb));

            for (int i = 0; i < count - 1; i++)
            {
                float from = candidates[i];
                float to = candidates[i + 1];
                if (to - from <= Epsilon)
                    continue;

                float mid = (from + to) * 0.5f;
                if (TryGetSegmentAABBIntervalMinimum(start, direction, min, max, mid, from, to, out float t))
                    sqrDistance = Mathf.Min(sqrDistance, SqrDistancePointAABB(start + direction * t, aabb));
            }

            return sqrDistance;
        }

        private static void AddAxisBoundaries(Span<float> candidates, ref int count, float origin, float direction, float min, float max)
        {
            if (Mathf.Abs(direction) <= Epsilon)
                return;

            AddCandidate(candidates, ref count, (min - origin) / direction);
            AddCandidate(candidates, ref count, (max - origin) / direction);
        }

        private static void AddCandidate(Span<float> candidates, ref int count, float t)
        {
            if (t < 0 || t > 1)
                return;

            t = Mathf.Clamp01(t);
            for (int i = 0; i < count; i++)
            {
                if (Mathf.Abs(candidates[i] - t) <= Epsilon)
                    return;
            }

            candidates[count++] = t;
        }

        private static void SortCandidates(Span<float> candidates)
        {
            for (int i = 1; i < candidates.Length; i++)
            {
                float value = candidates[i];
                int j = i - 1;
                while (j >= 0 && candidates[j] > value)
                {
                    candidates[j + 1] = candidates[j];
                    j--;
                }

                candidates[j + 1] = value;
            }
        }

        private static bool TryGetSegmentAABBIntervalMinimum(Vector3 start, Vector3 direction, Vector3 min, Vector3 max, float sampleT, float from, float to, out float t)
        {
            Vector3 point = start + direction * sampleT;
            float numerator = 0;
            float denominator = 0;
            AddIntervalAxisMinimum(point.x, start.x, direction.x, min.x, max.x, ref numerator, ref denominator);
            AddIntervalAxisMinimum(point.y, start.y, direction.y, min.y, max.y, ref numerator, ref denominator);
            AddIntervalAxisMinimum(point.z, start.z, direction.z, min.z, max.z, ref numerator, ref denominator);
            if (denominator <= Epsilon)
            {
                t = 0;
                return false;
            }

            t = Mathf.Clamp(-numerator / denominator, from, to);
            return true;
        }

        private static void AddIntervalAxisMinimum(float sample, float origin, float direction, float min, float max, ref float numerator, ref float denominator)
        {
            float target;
            if (sample < min)
                target = min;
            else if (sample > max)
                target = max;
            else
                return;

            numerator += direction * (origin - target);
            denominator += direction * direction;
        }

        private static bool TryGetClosestSegmentSegment2DParameters(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2, out float s, out float t)
        {
            Vector2 d1 = q1 - p1;
            Vector2 d2 = q2 - p2;
            Vector2 r = p1 - p2;
            float a = Vector2.Dot(d1, d1);
            float e = Vector2.Dot(d2, d2);
            float f = Vector2.Dot(d2, r);

            if (a <= Epsilon && e <= Epsilon)
            {
                s = 0;
                t = 0;
                return false;
            }

            if (a <= Epsilon)
            {
                s = 0;
                t = Mathf.Clamp01(f / e);
                return false;
            }

            float c = Vector2.Dot(d1, r);
            if (e <= Epsilon)
            {
                t = 0;
                s = Mathf.Clamp01(-c / a);
                return true;
            }

            float b = Vector2.Dot(d1, d2);
            float denom = a * e - b * b;
            s = denom > Epsilon ? Mathf.Clamp01((b * f - c * e) / denom) : 0;
            t = (b * s + f) / e;

            if (t < 0)
            {
                t = 0;
                s = Mathf.Clamp01(-c / a);
            }
            else if (t > 1)
            {
                t = 1;
                s = Mathf.Clamp01((b - c) / a);
            }

            return true;
        }

        private static float SqrDistancePointPie(Vector3 point, in ShapePie pie)
        {
            float bottom = pie.Position.y;
            float top = pie.Position.y + Mathf.Max(0, pie.Height);
            float offsetY = 0;
            if (point.y < bottom)
                offsetY = bottom - point.y;
            else if (point.y > top)
                offsetY = point.y - top;

            float sdf = ShapeSDFUtil.SectorSDF(point.ToV2(), pie.Position.ToV2(), pie.YDegree, pie.Angle, Mathf.Max(0, pie.Radius));
            float offsetXZ = Mathf.Max(0, sdf);
            return offsetXZ * offsetXZ + offsetY * offsetY;
        }

        private static void AddSegmentCircleCandidates(Span<float> candidates, ref int count, Vector2 start, Vector2 direction, Vector2 center, float radius)
        {
            float a = Vector2.Dot(direction, direction);
            if (a <= Epsilon)
                return;

            Vector2 f = start - center;
            float b = 2 * Vector2.Dot(f, direction);
            float c = Vector2.Dot(f, f) - radius * radius;
            float discriminant = b * b - 4 * a * c;
            if (discriminant < 0)
                return;

            float sqrt = Mathf.Sqrt(discriminant);
            float inv = 1 / (2 * a);
            AddCandidate(candidates, ref count, (-b - sqrt) * inv);
            AddCandidate(candidates, ref count, (-b + sqrt) * inv);
        }

        private static float SqrDistanceSegmentPie(Vector3 start, Vector3 end, in ShapePie pie)
        {
            Vector3 segment = end - start;
            Vector2 start2D = start.ToV2();
            Vector2 segment2D = segment.ToV2();
            Vector2 center = pie.Position.ToV2();
            float halfAngle = pie.Angle * 0.5f;
            float radius = Mathf.Max(0, pie.Radius);
            Vector2 leftEnd = center + ShapeSDFUtil.Rotate(new Vector2(0, 1), pie.YDegree - halfAngle) * radius;
            Vector2 rightEnd = center + ShapeSDFUtil.Rotate(new Vector2(0, 1), pie.YDegree + halfAngle) * radius;
            Span<float> candidates = stackalloc float[16];
            int count = 0;
            AddCandidate(candidates, ref count, 0);
            AddCandidate(candidates, ref count, 1);
            AddAxisBoundaries(candidates, ref count, start.y, segment.y, pie.Position.y, pie.Position.y + Mathf.Max(0, pie.Height));

            float segment2DSqr = Vector2.Dot(segment2D, segment2D);
            if (segment2DSqr > Epsilon)
                AddCandidate(candidates, ref count, Vector2.Dot(center - start2D, segment2D) / segment2DSqr);

            if (TryGetClosestSegmentSegment2DParameters(start2D, start2D + segment2D, center, leftEnd, out float leftT, out _))
                AddCandidate(candidates, ref count, leftT);
            if (TryGetClosestSegmentSegment2DParameters(start2D, start2D + segment2D, center, rightEnd, out float rightT, out _))
                AddCandidate(candidates, ref count, rightT);

            AddSegmentCircleCandidates(candidates, ref count, start2D, segment2D, center, radius);
            SortCandidates(candidates.Slice(0, count));

            float sqrDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
                sqrDistance = Mathf.Min(sqrDistance, SqrDistancePointPie(start + segment * candidates[i], pie));

            return sqrDistance;
        }

        private static bool RaySphere(Vector3 center, float radius, in ShapeRay ray, out float t)
        {
            t = 0;
            radius = Mathf.Max(0, radius);
            float radiusSquare = radius * radius;
            if (!TryNormalizeRay(ray, out Vector3 direction, out float length))
                return Vector3.SqrMagnitude(center - ray.Position) <= radiusSquare;

            Vector3 oc = center - ray.Position;
            if (Vector3.Dot(oc, oc) <= radiusSquare)
                return true;

            if (length <= Epsilon)
                return false;

            float projection = Vector3.Dot(oc, direction);
            if (projection < 0)
                return false;

            float distance2 = Vector3.Dot(oc, oc) - projection * projection;
            if (distance2 > radiusSquare)
                return false;

            t = projection - Mathf.Sqrt(Mathf.Max(0, radiusSquare - distance2));
            return t <= length;
        }

        private static bool RayCapsule(in ShapeRay ray, in ShapeCapsule capsule, out float t)
        {
            t = float.MaxValue;
            TryGetCapsuleSegment(capsule, out Vector3 capsuleStart, out Vector3 capsuleEnd);
            float radius = Mathf.Max(0, capsule.Radius);

            if (!TryNormalizeRay(ray, out Vector3 rayDirection, out float rayLength))
            {
                float sqrDistance = SegmentSegmentSqrDistance(ray.Position, ray.Position, capsuleStart, capsuleEnd);
                t = 0;
                return sqrDistance <= radius * radius;
            }

            if (SegmentSegmentSqrDistance(ray.Position, ray.Position, capsuleStart, capsuleEnd) <= radius * radius)
            {
                t = 0;
                return true;
            }

            if (rayLength <= Epsilon)
                return false;

            ShapeRay normalizedRay = new ShapeRay { Position = ray.Position, Direction = rayDirection, Length = rayLength };
            if (RaySphere(capsuleStart, radius, normalizedRay, out float enterStart))
                t = Mathf.Min(t, enterStart);
            if (RaySphere(capsuleEnd, radius, normalizedRay, out float enterEnd))
                t = Mathf.Min(t, enterEnd);

            Vector3 axis = capsuleEnd - capsuleStart;
            float axisLength = axis.magnitude;
            if (axisLength > Epsilon)
            {
                Vector3 axisDirection = axis / axisLength;
                Vector3 m = ray.Position - capsuleStart;
                Vector3 d = rayDirection - axisDirection * Vector3.Dot(rayDirection, axisDirection);
                Vector3 q = m - axisDirection * Vector3.Dot(m, axisDirection);
                float a = Vector3.Dot(d, d);
                float b = 2 * Vector3.Dot(q, d);
                float c = Vector3.Dot(q, q) - radius * radius;
                float discriminant = b * b - 4 * a * c;
                if (a > Epsilon && discriminant >= 0)
                {
                    float sqrt = Mathf.Sqrt(discriminant);
                    float inv = 1 / (2 * a);
                    float enter = (-b - sqrt) * inv;
                    float exit = (-b + sqrt) * inv;
                    if (enter >= 0 && enter <= rayLength)
                    {
                        float axisT = Vector3.Dot(m + rayDirection * enter, axisDirection);
                        if (axisT >= 0 && axisT <= axisLength)
                            t = Mathf.Min(t, enter);
                    }

                    if (exit >= 0 && exit <= rayLength)
                    {
                        float axisT = Vector3.Dot(m + rayDirection * exit, axisDirection);
                        if (axisT >= 0 && axisT <= axisLength)
                            t = Mathf.Min(t, exit);
                    }
                }
            }

            return t <= rayLength;
        }

        public static Vector3 ClosestPoint(Vector3 start, Vector3 end, Vector3 point)
        {
            Vector3 direction = end - start;

            float t = direction.sqrMagnitude;
            if (t < Epsilon) return start;

            t = Mathf.Clamp01(Vector3.Dot(point - start, direction) / t);
            return start + direction * t;
        }
        public static float SegmentSDFSqr(Vector2 point, Vector2 from, Vector2 to)
        {
            Vector2 ap = point - from;
            Vector2 ab = to - from;
            float abSqrMagnitude = Vector2.Dot(ab, ab);
            if (abSqrMagnitude < Epsilon)
                return ap.sqrMagnitude;

            float h = Mathf.Clamp01(Vector2.Dot(ap, ab) / abSqrMagnitude);
            return (ap - h * ab).sqrMagnitude;
        }
        #region 球形
        public static bool Overlap(in ShapeSphere c1, in ShapeSphere c2)
        {
            float sqrRadius = Mathf.Max(0, c1.Radius) + Mathf.Max(0, c2.Radius);
            sqrRadius *= sqrRadius;
            return Vector3.SqrMagnitude(c1.Position - c2.Position) <= sqrRadius;
        }
        public static bool Overlap(in ShapeSphere c1, in ShapeRay c2, out float t)
        {
            return RaySphere(c1.Position, c1.Radius, c2, out t);
        }
        //判断圆与
        public static bool Overlap(in ShapeSphere c1, in ShapeCylinder c2)
        {
            float radius = Mathf.Max(0, c1.Radius);
            float cylinderRadius = Mathf.Max(0, c2.Radius);
            float cylinderHeight = Mathf.Max(0, c2.Height);
            if (c1.Position.y < c2.Position.y)
            {//球在下方
                //不相接
                if (c1.Position.y + radius < c2.Position.y)
                    return false;
                //计算球与底部相交的圆半径
                radius = ProjectRadius(radius, c2.Position.y - c1.Position.y);
            }
            else if (c1.Position.y > (c2.Position.y + cylinderHeight))
            {//在上方
                //不相接
                if (c1.Position.y - radius > (c2.Position.y + cylinderHeight))
                    return false;
                //计算球与顶部相交的圆半径
                radius = ProjectRadius(radius, c1.Position.y - (c2.Position.y + cylinderHeight));
            }
            float sqrRadius = radius + cylinderRadius;
            sqrRadius *= sqrRadius;
            return sqrRadius >= Vector2.SqrMagnitude(c1.Position.ToV2() - c2.Position.ToV2());
        }
        
        public static bool Overlap(in ShapeSphere c1, in ShapeBox c2)
        {
            float radius = Mathf.Max(0, c1.Radius);
            Vector3 extents = Abs(c2.Extern);
            if (c1.Position.y < c2.Position.y - extents.y)
            {//球在下方
                //不相接
                if (c1.Position.y + radius < (c2.Position.y - extents.y))
                    return false;
                //计算球与底部相交的圆半径
                radius = ProjectRadius(radius, (c2.Position.y - extents.y) - c1.Position.y);
            }
            else if (c1.Position.y > (c2.Position.y + extents.y))
            {//在上方
                //不相接
                if ((c1.Position.y - radius) > (c2.Position.y + extents.y))
                    return false;
                //计算球与顶部相交的圆半径
                radius = ProjectRadius(radius, c1.Position.y - (c2.Position.y + extents.y));
            }
            float sdf = ShapeSDFUtil.OrientedBoxSDF(c1.Position.ToV2(), c2.Position.ToV2(), c2.YDegree, extents.ToV2());
            return sdf <= radius;
        }
        public static bool Overlap(in ShapeSphere c1, in ShapePie c2)
        {
            float radius = Mathf.Max(0, c1.Radius);
            float pieHeight = Mathf.Max(0, c2.Height);
            if (c1.Position.y < c2.Position.y)
            {//球在下方
                //不相接
                if (c1.Position.y + radius < c2.Position.y)
                    return false;
                //计算球与底部相交的圆半径
                radius = ProjectRadius(radius, c2.Position.y - c1.Position.y);
            }
            else if (c1.Position.y > (c2.Position.y + pieHeight))
            {//在上方
                //不相接
                if (c1.Position.y - radius > (c2.Position.y + pieHeight))
                    return false;
                //计算球与顶部相交的圆半径
                radius = ProjectRadius(radius, c1.Position.y - (c2.Position.y + pieHeight));
            }
            float sdf = ShapeSDFUtil.SectorSDF(c1.Position.ToV2(), c2.Position.ToV2(), c2.YDegree, c2.Angle, Mathf.Max(0, c2.Radius));
            return sdf <= radius;
        }
        public static bool Overlap(in ShapeSphere c1, in ShapeCapsule c2)
        {
            TryGetCapsuleSegment(c2, out Vector3 start, out Vector3 end);
            float radius = Mathf.Max(0, c1.Radius) + Mathf.Max(0, c2.Radius);
            return Vector3.SqrMagnitude(ClosestPoint(start, end, c1.Position) - c1.Position) <= radius * radius;
        }
        public static bool Overlap(in ShapeSphere c1, in ShapeSegment c2)
        {
            float radius = Mathf.Max(0, c1.Radius);
            return Vector3.SqrMagnitude(ClosestPoint(c2.Start, c2.End, c1.Position) - c1.Position) <= radius * radius;
        }
        public static bool Overlap(in ShapeSphere c1, in ShapeAABB c2)
        {
            float radius = Mathf.Max(0, c1.Radius);
            return SqrDistancePointAABB(c1.Position, c2) <= radius * radius;
        }

        public static bool Overlap(in ShapeBox c1, in ShapeSphere c2) { return Overlap(c2, c1); }
        public static bool Overlap(in ShapePie c1, in ShapeSphere c2) { return Overlap(c2, c1); }
        public static bool Overlap(in ShapeRay c1, in ShapeSphere c2, out float t) { return Overlap(c2, c1, out t); }
        public static bool Overlap(in ShapeCapsule c1, in ShapeSphere c2) { return Overlap(c2, c1); }
        public static bool Overlap(in ShapeSegment c1, in ShapeSphere c2) { return Overlap(c2, c1); }
        public static bool Overlap(in ShapeAABB c1, in ShapeSphere c2) { return Overlap(c2, c1); }
        #endregion

        #region 盒体
        public static bool Overlap(in ShapeBox c1, in ShapeBox c2)
        {
            if (!OverlapHeight(c1.Position.y - Mathf.Abs(c1.Extern.y), c1.Position.y + Mathf.Abs(c1.Extern.y),
                c2.Position.y - Mathf.Abs(c2.Extern.y), c2.Position.y + Mathf.Abs(c2.Extern.y)))
                return false;

            return OverlapOBB2D(c1, c2);
        }
        public static bool Overlap(in ShapeBox c1, in ShapeCapsule c2)
        {
            TryGetCapsuleSegment(c2, out Vector3 start, out Vector3 end);
            ShapeAABB localBox = BoxLocalAABB(c1);
            Vector3 localStart = ToBoxLocal(start, c1);
            Vector3 localEnd = ToBoxLocal(end, c1);
            float radius = Mathf.Max(0, c2.Radius);
            return SqrDistanceSegmentAABB(localStart, localEnd, localBox) <= radius * radius;
        }
        public static bool Overlap(in ShapeBox c1, in ShapeSegment c2)
        {
            ShapeAABB localBox = BoxLocalAABB(c1);
            ShapeSegment localSegment = new ShapeSegment
            {
                Start = ToBoxLocal(c2.Start, c1),
                End = ToBoxLocal(c2.End, c1),
            };
            return Overlap(localSegment, localBox);
        }
        public static bool Overlap(in ShapeBox c1, in ShapeAABB c2)
        {
            ShapeBox box = new ShapeBox { Position = c2.Center, Extern = Abs(c2.Extents), YDegree = 0 };
            return Overlap(c1, box);
        }
        public static bool Overlap(in ShapeBox c1, in ShapePie c2)
        {
            if (!OverlapHeight(c1.Position.y - Mathf.Abs(c1.Extern.y), c1.Position.y + Mathf.Abs(c1.Extern.y),
                c2.Position.y, c2.Position.y + Mathf.Max(0, c2.Height)))
                return false;

            return OverlapSectorOBB2D(c2, c1);
        }
        public static bool Overlap(in ShapeBox c1, in ShapeRay c2, out float t)
        {
            ShapeAABB localBox = BoxLocalAABB(c1);
            ShapeRay localRay = new ShapeRay
            {
                Position = ToBoxLocal(c2.Position, c1),
                Direction = ToBoxLocalDirection(c2.Direction, c1),
                Length = c2.Length,
            };
            return Overlap(localBox, localRay, out t);
        }

        public static bool Overlap(in ShapeCapsule c1, in ShapeBox c2) { return Overlap(c2, c1); }
        public static bool Overlap(in ShapeSegment c1, in ShapeBox c2) { return Overlap(c2, c1); }
        public static bool Overlap(in ShapeAABB c1, in ShapeBox c2) { return Overlap(c2, c1); }
        public static bool Overlap(in ShapePie c1, in ShapeBox c2) { return Overlap(c2, c1); }
        public static bool Overlap(in ShapeRay c1, in ShapeBox c2, out float t) { return Overlap(c2, c1, out t); }
        #endregion

        #region 圆柱
        public static bool Overlap(in ShapeCylinder c1, in ShapeSphere c2) { return Overlap(c2, c1); }
        public static bool Overlap(in ShapeCylinder c1, in ShapeCylinder c2)
        {
            float height1 = Mathf.Max(0, c1.Height);
            float height2 = Mathf.Max(0, c2.Height);
            if (c1.Position.y + height1 < c2.Position.y)
                return false;
            if (c1.Position.y > (c2.Position.y + height2))
                return false;

            float sqrRadius = Mathf.Max(0, c1.Radius) + Mathf.Max(0, c2.Radius);
            sqrRadius *= sqrRadius;
            return sqrRadius >= Vector2.SqrMagnitude(c1.Position.ToV2() - c2.Position.ToV2());
        }
        //圆柱跟矩形，计算出点到一个矩形的距离，如果这个距离小于圆柱的半径，就证明是会发生碰撞的
        public static bool Overlap(in ShapeCylinder c1, in ShapeBox c2)
        {
            Vector3 extents = Abs(c2.Extern);
            float height = Mathf.Max(0, c1.Height);
            if (c1.Position.y + height < c2.Position.y - extents.y)
                return false;
            if (c1.Position.y > c2.Position.y + extents.y)
                return false;

            float sdf = ShapeSDFUtil.OrientedBoxSDF(c1.Position.ToV2(), c2.Position.ToV2(), c2.YDegree, extents.ToV2());
            return sdf <= Mathf.Max(0, c1.Radius);
        }
        public static bool Overlap(in ShapeCylinder c1, in ShapePie c2)
        {
            float height = Mathf.Max(0, c1.Height);
            float pieHeight = Mathf.Max(0, c2.Height);
            if (c1.Position.y + height < c2.Position.y)
                return false;
            if (c1.Position.y > c2.Position.y + pieHeight)
                return false;
            float sdf = ShapeSDFUtil.SectorSDF(c1.Position.ToV2(), c2.Position.ToV2(), c2.YDegree, c2.Angle, Mathf.Max(0, c2.Radius));
            return sdf <= Mathf.Max(0, c1.Radius);
        }
        public static bool Overlap(in ShapeCylinder c1, in ShapeRay c2, out float t)
        {
            t = 0;
            if (!TryNormalizeRay(c2, out Vector3 direction, out float length))
                return IsPointInCylinder(c2.Position, c1);

            if (IsPointInCylinder(c2.Position, c1))
                return true;

            float top = c1.Position.y + Mathf.Max(0, c1.Height);
            float bottom = c1.Position.y;
            if (length <= Epsilon)
                return false;

            Vector3 end = c2.Position + direction * length;
            if (c2.Position.y < bottom && end.y < bottom)
                return false;
            if (c2.Position.y > top && end.y > top)
                return false;
            float radius = Mathf.Max(0, c1.Radius);
            float radiusSquare = radius * radius;
            Vector2 center = c1.Position.ToV2();
            Vector2 lineStart = c2.Position.ToV2();
            Vector2 lineEnd = end.ToV2();
            //如果在平面上圆和线段不相交则返回
            if (SegmentSDFSqr(center, lineStart, lineEnd) > radiusSquare)
                return false;
            Vector2 startToCenter = lineStart - center;
            Vector2 endToCenter = lineEnd - center;
            bool isStartInCircle = startToCenter.sqrMagnitude < radiusSquare;
            bool isEndInCircle = endToCenter.sqrMagnitude < radiusSquare;
            t = float.MaxValue;
            {//处理和顶部或者底部相交的情况
                if (new Plane(Vector3.up, -top).Raycast(new Ray(c2.Position, direction), out float enterTop)
                    && enterTop <= length && enterTop >= 0)
                {
                    Vector2 pt = (c2.Position + direction * enterTop).ToV2();
                    if ((pt - center).sqrMagnitude <= radiusSquare)
                    {//交点在圆柱内
                        t = Mathf.Min(t, enterTop);
                    }
                }
                if (new Plane(Vector3.up, -bottom).Raycast(new Ray(c2.Position, direction), out float enterBottom)
                    && enterBottom <= length && enterBottom >= 0)
                {
                    Vector2 pt = (c2.Position + direction * enterBottom).ToV2();
                    if ((pt - center).sqrMagnitude <= radiusSquare)
                    {//交点在圆柱内
                        t = Mathf.Min(t, enterBottom);
                    }
                }
            }

            do
            {
                if (isStartInCircle && isEndInCircle)
                {//两个点都在圆内，要么和顶部或者底部相交，要么在圆柱内部，内部这里也算相交
                    t = Mathf.Min(t, length);
                    break;
                }

                Vector2 lineDir = lineEnd - lineStart;
                float sqrMagnitude = lineDir.sqrMagnitude;
                if (sqrMagnitude < 1E-05f)
                    break;//射线垂直方向，只与顶部或者底部相交
                lineDir /= Mathf.Sqrt(sqrMagnitude);

                float b = Vector2.Dot(startToCenter, lineDir);// b大于0，说明射线方向背向圆心
                float c = Vector2.Dot(startToCenter, startToCenter) - radiusSquare;
                // 如果射线起点在圆外，并且方向与到圆方向相反，则不相交,前面处理过，正常这里不会判断失败
                if (c > 0 && b > 0)
                    return false;
                float discr = b * b - c;
                // 判别式小于0。前面处理过，正常这里不会判断失败
                if (discr < 0f)
                    return false;
                float enter = -b - Mathf.Sqrt(discr); // -b-sqrt(b*b-c)表示从射线起点比较近的相交点
                if (enter < 0f)                     // 射线的长度t值应该为正方向，最小为0
                    enter = 0f;
                Vector3 hDir = direction;//射线水平方向朝向
                hDir.y = 0;
                float hDirSqrMagnitude = hDir.sqrMagnitude;
                if (hDirSqrMagnitude < Epsilon)
                    break;

                hDir /= Mathf.Sqrt(hDirSqrMagnitude);
                float dot = Vector3.Dot(direction, hDir);
                enter /= dot;
                if (enter <= length)
                {
                    Vector3 p = c2.Position + direction * enter;
                    if (p.y >= bottom && p.y <= top)
                    {
                        t = Mathf.Min(t, enter);
                    }
                }

            } while (false);

            return t <= length;
        }
        public static bool Overlap(in ShapeCylinder c1, in ShapeCapsule c2)
        {
            TryGetCapsuleSegment(c2, out Vector3 start, out Vector3 end);
            float radius = Mathf.Max(0, c2.Radius);
            ShapeSphere startSphere = new ShapeSphere { Position = start, Radius = radius };
            ShapeSphere endSphere = new ShapeSphere { Position = end, Radius = radius };
            if (Overlap(startSphere, c1) || Overlap(endSphere, c1))
                return true;

            ShapeCylinder expanded = new ShapeCylinder
            {
                Position = c1.Position - Vector3.up * radius,
                Radius = Mathf.Max(0, c1.Radius) + radius,
                Height = Mathf.Max(0, c1.Height) + radius * 2,
            };
            ShapeRay segmentRay = new ShapeRay { Position = start, Direction = end - start, Length = (end - start).magnitude };
            return Overlap(expanded, segmentRay, out _);
        }
        public static bool Overlap(in ShapeCylinder c1, in ShapeSegment c2)
        {
            Vector3 direction = c2.End - c2.Start;
            ShapeRay ray = new ShapeRay { Position = c2.Start, Direction = direction, Length = direction.magnitude };
            return Overlap(c1, ray, out _);
        }
        public static bool Overlap(in ShapeCylinder c1, in ShapeAABB c2)
        {
            Vector3 extents = Abs(c2.Extents);
            if (!OverlapHeight(c1.Position.y, c1.Position.y + Mathf.Max(0, c1.Height), c2.Center.y - extents.y, c2.Center.y + extents.y))
                return false;

            Vector2 d = new Vector2(
                Mathf.Max(Mathf.Abs(c1.Position.x - c2.Center.x) - extents.x, 0),
                Mathf.Max(Mathf.Abs(c1.Position.z - c2.Center.z) - extents.z, 0));
            float radius = Mathf.Max(0, c1.Radius);
            return d.sqrMagnitude <= radius * radius;
        }

        private static bool IsPointInCylinder(Vector3 point, in ShapeCylinder cylinder)
        {
            float top = cylinder.Position.y + Mathf.Max(0, cylinder.Height);
            if (point.y < cylinder.Position.y || point.y > top)
                return false;

            float radius = Mathf.Max(0, cylinder.Radius);
            return Vector2.SqrMagnitude(point.ToV2() - cylinder.Position.ToV2()) <= radius * radius;
        }

        public static bool Overlap(in ShapeBox c1, in ShapeCylinder c2) { return Overlap(c2, c1); }
        public static bool Overlap(in ShapePie c1, in ShapeCylinder c2) { return Overlap(c2, c1); }
        public static bool Overlap(in ShapeRay c1, in ShapeCylinder c2, out float t) { return Overlap(c2, c1, out t); }
        public static bool Overlap(in ShapeCapsule c1, in ShapeCylinder c2) { return Overlap(c2, c1); }
        public static bool Overlap(in ShapeSegment c1, in ShapeCylinder c2) { return Overlap(c2, c1); }
        public static bool Overlap(in ShapeAABB c1, in ShapeCylinder c2) { return Overlap(c2, c1); }
        #endregion

        #region 扇形柱
        public static bool Overlap(in ShapePie c1, in ShapePie c2)
        {
            float height1 = Mathf.Max(0, c1.Height);
            float height2 = Mathf.Max(0, c2.Height);
            if (!OverlapHeight(c1.Position.y, c1.Position.y + height1, c2.Position.y, c2.Position.y + height2))
                return false;

            Vector2 center1 = c1.Position.ToV2();
            Vector2 center2 = c2.Position.ToV2();
            if (IsPointInSector(center1, c2) || IsPointInSector(center2, c1))
                return true;

            float half1 = c1.Angle * 0.5f;
            float half2 = c2.Angle * 0.5f;
            float radius1 = Mathf.Max(0, c1.Radius);
            float radius2 = Mathf.Max(0, c2.Radius);
            Vector2 c1Left = center1 + ShapeSDFUtil.Rotate(new Vector2(0, 1), c1.YDegree - half1) * radius1;
            Vector2 c1Right = center1 + ShapeSDFUtil.Rotate(new Vector2(0, 1), c1.YDegree + half1) * radius1;
            Vector2 c2Left = center2 + ShapeSDFUtil.Rotate(new Vector2(0, 1), c2.YDegree - half2) * radius2;
            Vector2 c2Right = center2 + ShapeSDFUtil.Rotate(new Vector2(0, 1), c2.YDegree + half2) * radius2;

            return SegmentIntersectsSector(center1, c1Left, c2)
                || SegmentIntersectsSector(center1, c1Right, c2)
                || SegmentIntersectsSector(center2, c2Left, c1)
                || SegmentIntersectsSector(center2, c2Right, c1)
                || SectorArcIntersectsSectorArc(c1, c2);
        }
        public static bool Overlap(in ShapePie c1, in ShapeAABB c2)
        {
            Vector3 extents = new Vector3(Mathf.Abs(c2.Extents.x), Mathf.Abs(c2.Extents.y), Mathf.Abs(c2.Extents.z));
            float pieBottom = c1.Position.y;
            float pieTop = c1.Position.y + Mathf.Max(0, c1.Height);
            if (!OverlapHeight(pieBottom, pieTop, c2.Center.y - extents.y, c2.Center.y + extents.y))
                return false;

            Vector2 min = new Vector2(c2.Center.x - extents.x, c2.Center.z - extents.z);
            Vector2 max = new Vector2(c2.Center.x + extents.x, c2.Center.z + extents.z);
            return OverlapSectorAABB2D(c1, min, max);
        }

        public static bool Overlap(in ShapePie c1, in ShapeCapsule c2)
        {
            TryGetCapsuleSegment(c2, out Vector3 start, out Vector3 end);
            float radius = Mathf.Max(0, c2.Radius);
            ShapeSphere startSphere = new ShapeSphere { Position = start, Radius = radius };
            ShapeSphere endSphere = new ShapeSphere { Position = end, Radius = radius };
            if (Overlap(startSphere, c1) || Overlap(endSphere, c1))
                return true;

            return OverlapPieCapsuleByDistance(c1, start, end, radius);
        }
        public static bool Overlap(in ShapePie c1, in ShapeSegment c2)
        {
            float minY = Mathf.Min(c2.Start.y, c2.End.y);
            float maxY = Mathf.Max(c2.Start.y, c2.End.y);
            float height = Mathf.Max(0, c1.Height);
            if (!OverlapHeight(c1.Position.y, c1.Position.y + height, minY, maxY))
                return false;

            if (OverlapPieCapsuleSample(c1, c2.Start, 0) || OverlapPieCapsuleSample(c1, c2.End, 0))
                return true;

            Vector3 segment = c2.End - c2.Start;
            if (OverlapPieCapsuleYBoundarySample(c1, c2.Start, segment, c1.Position.y, 0)
                || OverlapPieCapsuleYBoundarySample(c1, c2.Start, segment, c1.Position.y + height, 0))
                return true;

            return SegmentIntersectsSector(c2.Start.ToV2(), c2.End.ToV2(), c1);
        }

        public static bool Overlap(in ShapeAABB c1, in ShapePie c2) { return Overlap(c2, c1); }
        public static bool Overlap(in ShapeCapsule c1, in ShapePie c2) { return Overlap(c2, c1); }
        public static bool Overlap(in ShapeSegment c1, in ShapePie c2) { return Overlap(c2, c1); }
        #endregion

        #region 胶囊体
        public static bool Overlap(in ShapeCapsule c1, in ShapeCapsule c2)
        {
            TryGetCapsuleSegment(c1, out Vector3 start1, out Vector3 end1);
            TryGetCapsuleSegment(c2, out Vector3 start2, out Vector3 end2);
            float radius = Mathf.Max(0, c1.Radius) + Mathf.Max(0, c2.Radius);
            return SegmentSegmentSqrDistance(start1, end1, start2, end2) <= radius * radius;
        }
        public static bool Overlap(in ShapeCapsule c1, in ShapeSegment c2)
        {
            TryGetCapsuleSegment(c1, out Vector3 start, out Vector3 end);
            float radius = Mathf.Max(0, c1.Radius);
            return SegmentSegmentSqrDistance(start, end, c2.Start, c2.End) <= radius * radius;
        }
        public static bool Overlap(in ShapeCapsule c1, in ShapeRay c2, out float t)
        {
            return RayCapsule(c2, c1, out t);
        }
        public static bool Overlap(in ShapeCapsule c1, in ShapeAABB c2)
        {
            TryGetCapsuleSegment(c1, out Vector3 start, out Vector3 end);
            float radius = Mathf.Max(0, c1.Radius);
            return SqrDistanceSegmentAABB(start, end, c2) <= radius * radius;
        }
        public static bool Overlap(in ShapeSegment c1, in ShapeCapsule c2) { return Overlap(c2, c1); }
        public static bool Overlap(in ShapeRay c1, in ShapeCapsule c2, out float t) { return Overlap(c2, c1, out t); }
        public static bool Overlap(in ShapeAABB c1, in ShapeCapsule c2) { return Overlap(c2, c1); }
        #endregion

        #region 线段
        public static bool Overlap(in ShapeSegment c1, in ShapeSegment c2)
        {
            return SegmentSegmentSqrDistance(c1.Start, c1.End, c2.Start, c2.End) <= Epsilon;
        }
        public static bool Overlap(in ShapeSegment c1, in ShapeAABB c2)
        {
            Vector3 direction = c1.End - c1.Start;
            ShapeRay ray = new ShapeRay { Position = c1.Start, Direction = direction, Length = direction.magnitude };
            return Overlap(c2, ray, out _);
        }
        public static bool Overlap(in ShapeSegment c1, in ShapeRay c2)
        {
            if (!TryNormalizeRay(c2, out Vector3 direction, out float length))
                return Vector3.SqrMagnitude(ClosestPoint(c1.Start, c1.End, c2.Position) - c2.Position) <= Epsilon;

            Vector3 rayEnd = c2.Position + direction * length;
            return SegmentSegmentSqrDistance(c1.Start, c1.End, c2.Position, rayEnd) <= Epsilon;
        }
        public static bool Overlap(in ShapeAABB c1, in ShapeSegment c2) { return Overlap(c2, c1); }
        public static bool Overlap(in ShapeRay c1, in ShapeSegment c2) { return Overlap(c2, c1); }
        #endregion

        #region AABB
        public static bool Overlap(in ShapeAABB c1, in ShapeAABB c2)
        {
            Vector3 extents1 = new Vector3(Mathf.Abs(c1.Extents.x), Mathf.Abs(c1.Extents.y), Mathf.Abs(c1.Extents.z));
            Vector3 extents2 = new Vector3(Mathf.Abs(c2.Extents.x), Mathf.Abs(c2.Extents.y), Mathf.Abs(c2.Extents.z));
            Vector3 offset = c1.Center - c2.Center;
            return Mathf.Abs(offset.x) <= extents1.x + extents2.x
                && Mathf.Abs(offset.y) <= extents1.y + extents2.y
                && Mathf.Abs(offset.z) <= extents1.z + extents2.z;
        }
        public static bool Overlap(in ShapeAABB c1, in ShapeRay c2, out float t)
        {
            t = 0;
            if (IsPointInAABB(c2.Position, c1))
                return true;

            if (!TryNormalizeRay(c2, out Vector3 direction, out float length) || length <= Epsilon)
                return false;

            Vector3 extents = new Vector3(Mathf.Abs(c1.Extents.x), Mathf.Abs(c1.Extents.y), Mathf.Abs(c1.Extents.z));
            Vector3 min = c1.Center - extents;
            Vector3 max = c1.Center + extents;
            float tMin = 0;
            float tMax = length;

            if (!ClipRaySlab(c2.Position.x, direction.x, min.x, max.x, ref tMin, ref tMax))
                return false;
            if (!ClipRaySlab(c2.Position.y, direction.y, min.y, max.y, ref tMin, ref tMax))
                return false;
            if (!ClipRaySlab(c2.Position.z, direction.z, min.z, max.z, ref tMin, ref tMax))
                return false;

            t = tMin;
            return t <= length;
        }

        private static bool ClipRaySlab(float origin, float direction, float min, float max, ref float tMin, ref float tMax)
        {
            if (Mathf.Abs(direction) < Epsilon)
                return origin >= min && origin <= max;

            float inv = 1 / direction;
            float enter = (min - origin) * inv;
            float exit = (max - origin) * inv;
            if (enter > exit)
            {
                float temp = enter;
                enter = exit;
                exit = temp;
            }

            tMin = Mathf.Max(tMin, enter);
            tMax = Mathf.Min(tMax, exit);
            return tMin <= tMax;
        }

        public static bool Overlap(in ShapeRay c1, in ShapeAABB c2, out float t) { return Overlap(c2, c1, out t); }
        #endregion

    }
}
