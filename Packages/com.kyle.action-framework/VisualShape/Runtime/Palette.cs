using UnityEngine;

namespace VisualShape
{
    /// <summary>
    /// 颜色集合。
    ///
    /// 使用此类最简单的方式是通过 "using" 语句导入：
    ///
    /// <code>
    /// using Palette = VisualShape.Palette.Colorbrewer.Set1;
    ///
    /// class PaletteTest : MonoBehaviour {
    ///     public void Update () {
    ///         Draw.Line(new Vector3(0, 0, 0), new Vector3(1, 1, 1), Palette.Orange);
    ///     }
    /// }
    /// </code>
    /// </summary>
    public static class Palette
    {
        /// <summary>纯色</summary>
        public static class Pure
        {
            public static readonly Color Yellow = new Color(1, 1, 0, 1);
            public static readonly Color Clear = new Color(0, 0, 0, 0);
            public static readonly Color Grey = new Color(0.5f, 0.5f, 0.5f, 1);
            public static readonly Color Magenta = new Color(1, 0, 1, 1);
            public static readonly Color Cyan = new Color(0, 1, 1, 1);
            public static readonly Color Red = new Color(1, 0, 0, 1);
            public static readonly Color Black = new Color(0, 0, 0, 1);
            public static readonly Color White = new Color(1, 1, 1, 1);
            public static readonly Color Blue = new Color(0, 0, 1, 1);
            public static readonly Color Green = new Color(0, 1, 0, 1);
        }

        /// <summary>
        /// Colorbrewer 配色方案。
        /// See: http://colorbrewer2.org/
        /// </summary>
        public static class Colorbrewer
        {
            /// <summary>Set 1 - 定性配色</summary>
            public static class Set1
            {
                public static readonly Color Red = new Color(228 / 255f, 26 / 255f, 28 / 255f, 1);
                public static readonly Color Blue = new Color(55 / 255f, 126 / 255f, 184 / 255f, 1);
                public static readonly Color Green = new Color(77 / 255f, 175 / 255f, 74 / 255f, 1);
                public static readonly Color Purple = new Color(152 / 255f, 78 / 255f, 163 / 255f, 1);
                public static readonly Color Orange = new Color(255 / 255f, 127 / 255f, 0 / 255f, 1);
                public static readonly Color Yellow = new Color(255 / 255f, 255 / 255f, 51 / 255f, 1);
                public static readonly Color Brown = new Color(166 / 255f, 86 / 255f, 40 / 255f, 1);
                public static readonly Color Pink = new Color(247 / 255f, 129 / 255f, 191 / 255f, 1);
                public static readonly Color Grey = new Color(153 / 255f, 153 / 255f, 153 / 255f, 1);
            }

            /// <summary>Blues - 顺序配色</summary>
            public static class Blues
            {
                static readonly Color[] Colors = new Color[] {
                    new Color(43/255f, 140/255f, 190/255f),

                    new Color(166/255f, 189/255f, 219/255f),
                    new Color(43/255f, 140/255f, 190/255f),

                    new Color(236/255f, 231/255f, 242/255f),
                    new Color(166/255f, 189/255f, 219/255f),
                    new Color(43/255f, 140/255f, 190/255f),

                    new Color(241/255f, 238/255f, 246/255f),
                    new Color(189/255f, 201/255f, 225/255f),
                    new Color(116/255f, 169/255f, 207/255f),
                    new Color(5/255f, 112/255f, 176/255f),

                    new Color(241/255f, 238/255f, 246/255f),
                    new Color(189/255f, 201/255f, 225/255f),
                    new Color(116/255f, 169/255f, 207/255f),
                    new Color(43/255f, 140/255f, 190/255f),
                    new Color(4/255f, 90/255f, 141/255f),

                    new Color(241/255f, 238/255f, 246/255f),
                    new Color(208/255f, 209/255f, 230/255f),
                    new Color(166/255f, 189/255f, 219/255f),
                    new Color(116/255f, 169/255f, 207/255f),
                    new Color(43/255f, 140/255f, 190/255f),
                    new Color(4/255f, 90/255f, 141/255f),

                    new Color(241/255f, 238/255f, 246/255f),
                    new Color(208/255f, 209/255f, 230/255f),
                    new Color(166/255f, 189/255f, 219/255f),
                    new Color(116/255f, 169/255f, 207/255f),
                    new Color(54/255f, 144/255f, 192/255f),
                    new Color(5/255f, 112/255f, 176/255f),
                    new Color(3/255f, 78/255f, 123/255f),

                    new Color(255/255f, 247/255f, 251/255f),
                    new Color(236/255f, 231/255f, 242/255f),
                    new Color(208/255f, 209/255f, 230/255f),
                    new Color(166/255f, 189/255f, 219/255f),
                    new Color(116/255f, 169/255f, 207/255f),
                    new Color(54/255f, 144/255f, 192/255f),
                    new Color(5/255f, 112/255f, 176/255f),
                    new Color(3/255f, 78/255f, 123/255f),

                    new Color(255/255f, 247/255f, 251/255f),
                    new Color(236/255f, 231/255f, 242/255f),
                    new Color(208/255f, 209/255f, 230/255f),
                    new Color(166/255f, 189/255f, 219/255f),
                    new Color(116/255f, 169/255f, 207/255f),
                    new Color(54/255f, 144/255f, 192/255f),
                    new Color(5/255f, 112/255f, 176/255f),
                    new Color(4/255f, 90/255f, 141/255f),
                    new Color(2/255f, 56/255f, 88/255f),
                };

                /// <summary>返回指定分类的颜色。</summary>
                /// <param name="classes">分类数量。必须在 1 到 9 之间。</param>
                /// <param name="index">颜色分类索引。必须在 0 到 classes-1 之间。</param>
                public static Color GetColor(int classes, int index)
                {
                    if (index < 0 || index >= classes) throw new System.ArgumentOutOfRangeException("index", "Index must be less than classes and at least 0");
                    if (classes <= 0 || classes > 9) throw new System.ArgumentOutOfRangeException("classes", "Only up to 9 classes are supported");

                    return Colors[(classes - 1) * classes / 2 + index];
                }
            }
        }
    }
}
