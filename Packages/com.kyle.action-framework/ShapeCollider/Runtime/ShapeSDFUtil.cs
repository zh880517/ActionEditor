using UnityEngine;
namespace ShapeCollider
{
    public static class ShapeSDFUtil
    {
        public static Vector2 Rotate(Vector2 v, float degree)
        {
            float radians = degree * Mathf.Deg2Rad;
            var ca = Mathf.Cos(radians);
            var sa = Mathf.Sin(radians);
            return new Vector2(ca * v.x - sa * v.y, sa * v.x + ca * v.y);
        }

        public static Vector2 Abs(this Vector2 vector)
        {
            return new Vector2(Mathf.Abs(vector.x), Mathf.Abs(vector.y));
        }
        /// <summary>
        /// 计算BoxSDF
        /// </summary>
        /// <param name="point">点位置</param>
        /// <param name="center">box中心点</param>
        /// <param name="yDegree">相对Y轴的旋转角度</param>
        /// <param name="halfSize"></param>
        /// <returns></returns>
        public static float OrientedBoxSDF(Vector2 point, Vector2 center, float yDegree, Vector2 halfSize)
        {
            //将point转换到目标坐标系
            point -= center;
            point = Rotate(point, -yDegree);
            Vector2 d = point.Abs() - halfSize;
            return Vector2.Max(d, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0);
        }
        /// <summary>
        /// 计算扇形的SDF
        /// </summary>
        /// <param name="point">点位置</param>
        /// <param name="pos">扇形原点位置</param>
        /// <param name="yDegree">相对Y轴旋转角度</param>
        /// <param name="angle">扇形夹角</param>
        /// <param name="radius">扇形半径</param>
        /// <returns>点到扇形最近边缘的距离，小于0在扇形内部，大于0在扇形外部</returns>
        public static float SectorSDF(Vector2 point, Vector2 pos, float yDegree, float angle, float radius)
        {
            //将point转换到目标坐标系
            point -= pos;
            point = Rotate(point, yDegree);
            //计算sdf https://zhuanlan.zhihu.com/p/427587359
            //原点在圆心图形以y轴为对称轴
            float d = (angle * 0.5f) * Mathf.Deg2Rad;
            Vector2 c = new Vector2(Mathf.Sin(d), Mathf.Cos(d));
            point.x = Mathf.Abs(point.x);
            //点到外围圆弧的距离
            float qp1 = point.magnitude - radius;
            //点到直线
            float qp2 = (point - c * Mathf.Clamp(Vector2.Dot(point, c), 0, radius)).magnitude * Mathf.Sign(c.y * point.x - c.x * point.y);
            return Mathf.Max(qp1, qp2);
        }
    }

}