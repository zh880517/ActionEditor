using UnityEngine;
namespace ShapeCollider
{
    [System.Serializable]
    public struct ShapeBox
    {
        public Vector3 Position;
        public Vector3 Extern;
        public float YDegree;
    }

    //圆柱
    [System.Serializable]
    public struct ShapeCylinder
    {
        public Vector3 Position;
        public float Radius;
        public float Height;
    }

    //球形
    [System.Serializable]
    public struct ShapeSphere
    {
        public Vector3 Position;
        public float Radius;
    }

    //胶囊体，Position为起点球心，Direction为轴向，Length为两端球心距离
    [System.Serializable]
    public struct ShapeCapsule
    {
        public Vector3 Position;
        public Vector3 Direction;
        public float Length;
        public float Radius;
    }

    //线段
    [System.Serializable]
    public struct ShapeSegment
    {
        public Vector3 Start;
        public Vector3 End;
    }

    //轴对齐包围盒
    [System.Serializable]
    public struct ShapeAABB
    {
        public Vector3 Center;
        public Vector3 Extents;
    }

    //饼形，带高度的3D扇形
    [System.Serializable]
    public struct ShapePie
    {
        public Vector3 Position;
        public float YDegree;
        public float Angle;//夹角
        public float Radius;
        public float Height;
    }

    [System.Serializable]
    public struct ShapeRay
    {
        public Vector3 Position;
        public Vector3 Direction;
        public float Length;
    }

}