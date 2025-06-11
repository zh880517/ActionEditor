using UnityEngine;
using UnityEngine.UIElements;

public class IconButton : Button
{
    private static readonly Color color = new Color(60 / 255f, 60 / 255f, 60 / 255f, 1);
    private static readonly Color color_Hover = new Color(80 / 255f, 80 / 255f, 80 / 255f, 1);
    private static readonly Color color_Focus = new Color(40 / 255f, 40 / 255f, 40 / 255f, 1);

    private bool isMouseDown = false;
    private Image imgIcon;
    public IconButton()
    {
        style.borderTopWidth = 0;
        style.borderBottomWidth = 0;
        style.borderLeftWidth = 0;
        style.borderRightWidth = 0;

        style.borderBottomLeftRadius = 0;
        style.borderBottomRightRadius = 0;
        style.borderTopLeftRadius = 0;
        style.borderTopRightRadius = 0;

        style.marginTop = 0;
        style.marginBottom = 0;
        style.marginLeft = 1;
        style.marginRight = 0;

        style.paddingTop = 0;
        style.paddingBottom = 0;
        style.paddingLeft = 0;
        style.paddingRight = 0;

        style.justifyContent = Justify.Center;
        style.alignItems = Align.Center;

        style.backgroundColor = color;

        RegisterCallback<MouseEnterEvent>(OnMouseEnter);
        RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
    }

    public void SetBuildinIcon(string iconName)
    {
        if (imgIcon == null)
        {
            imgIcon = new Image();
            Add(imgIcon);
        }
        var playContent = UnityEditor.EditorGUIUtility.IconContent(iconName);
        imgIcon.image = playContent.image;
    }

    public void SetIcon(Texture icon)
    {
        if (imgIcon == null)
        {
            imgIcon = new Image();
            Add(imgIcon);
        }
        imgIcon.image = icon;
    }

    private void OnMouseLeave(MouseLeaveEvent evt)
    {
        style.backgroundColor = color;
        SetDown(isMouseDown);
    }

    private void OnMouseEnter(MouseEnterEvent evt)
    {
        style.backgroundColor = color_Hover;
    }

    private void SetDown(bool isMouseDown)
    {
        this.isMouseDown = isMouseDown;
        if (isMouseDown)
        {
            style.backgroundColor = color_Focus;
        }
        else
        {
            style.backgroundColor = color;
        }
    }
}
