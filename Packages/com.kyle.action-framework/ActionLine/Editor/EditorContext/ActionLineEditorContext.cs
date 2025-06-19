using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
namespace ActionLine.EditorView
{
    public class ActionLineEditorContext : ScriptableObject
    {
        [SerializeField]
        private struct ViewPortData
        {
            public float Scale;
            public Vector2 Position;
        }
        [SerializeField]
        private ViewPortData viewPortData = new ViewPortData
        {
            Scale = 1f,
            Position = Vector2.zero
        };

        [SerializeField]
        private ActionLineAsset target;
        public List<ActionClipData> SelectedClips = new List<ActionClipData>();
        public List<ActionClipData> SelectedTracks = new List<ActionClipData>();

        private readonly List<ActionClipEditorContext> clipEditors = new List<ActionClipEditorContext>();
        private readonly List<ActionClipData> clips = new List<ActionClipData>();
        private readonly List<EditorAction> actions = new List<EditorAction>();
        private ActionLineView view;

        public IReadOnlyList<ActionClipData> Clips => clips;
        public ActionLineAsset Target => target;
        public ActionLineView View => view;

        public void SetView(ActionLineView actionLineView)
        {
            if (view != actionLineView)
            {
                view = actionLineView;
                view.AddManipulator(new ClipManipulator(this));
                view.AddManipulator(new TrackTitleManipulator(this));
                Clear();
                if (target)
                    Update();
                if(actions.Count == 0)
                {
                    InitEditorAction();
                }
            }
        }

        public int GetIndex(ActionClipData data)
        {
            return clips.IndexOf(data);
        }

        public void SetTarget(ActionLineAsset asset)
        {
            if (asset != target)
            {
                Clear();
                target = asset;
                RefreshViewPort();
                Update();
            }
        }

        public void ShowContextMenue(ActionModeType mode)
        {
            bool hasInherit = SelectedClips.Exists(x => x.IsInherit);
            GenericMenu menu = new GenericMenu();
            foreach (var item in actions)
            {
                if(!item.Visable(mode))
                    continue;
                if (item.IsValid(mode))
                {
                    menu.AddItem(new GUIContent(item.MenuPath, item.Icon), item.IsOn(mode), () => item.Execute(mode));
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent(item.MenuPath));
                }
            }
            menu.ShowAsContext();
        }

        public void RefreshViewPort()
        {
            view?.SetViewPort(viewPortData.Scale, viewPortData.Position.x, viewPortData.Position.y);
        }

        public void Update()
        {
            clips.Clear();
            target.ExportClipData(clips);
            int clipCount = clips.Count;
            view.Track.Group.SetVisableCount(clipCount);
            view.Title.Group.SetVisableCount(clipCount);
            //删除不存在的（被删除或者Undo）
            for (int i = 0; i < clipEditors.Count; i++)
            {
                var editor = clipEditors[i];
                if(!editor.Data.Clip || !target.ContainsClip(editor.Data.Clip))
                {
                    editor.Destroy();
                    clipEditors.RemoveAt(i);
                    i--;
                }
            }
            for (int i = 0; i < clips.Count; i++)
            {
                var clip = clips[i];
                int index = clipEditors.FindIndex(x => x.Data == clip);
                ActionClipEditorContext context = null;
                if (index < 0)
                {
                    context = ActionClipEditorContext.CreateEditorContext(clip);
                    clipEditors.Insert(i, context);
                }
                else
                {
                    context = clipEditors[index];
                    if(index != i)
                    {
                        clipEditors.RemoveAt(index);
                        clipEditors.Insert(i, context);
                    }
                }
                UpdateClip(context, i);
            }
            view.Track.Group.UpdateClipPosition();
        }

        /// <summary>
        /// 注册Undo操作，记录当前ActionLineAsset的状态
        /// </summary>
        /// <param name="name">修改的名字</param>
        /// <param name="clip">如果是继承的clip，则不允许被修改，调用无效</param>
        public void RegisterUndo(string name, ActionLineClip clip = null)
        {
            if (clip && clip.Owner != target)
                return;
            if (clip)
                Undo.RegisterCompleteObjectUndo(clip, name);
            viewPortData = new ViewPortData
            {
                Scale = view.Scale,
                Position = new Vector2(view.HorizontalOffset, view.VerticalOffset)
            };
            Undo.RegisterCompleteObjectUndo(target, name);
            EditorUtility.SetDirty(target);
        }

        public void Clear()
        {
            clips.Clear();
            foreach (var editor in clipEditors)
            {
                editor.Destroy();
            }
            clipEditors.Clear();
            view.Track.Group.SetVisableCount(0);
            view.Title.Group.SetVisableCount(0);
        }

        protected void RegisterAction<T>() where T : EditorAction, new()
        {
            if (actions.Exists(x => x.GetType() == typeof(T)))
                return; //已经注册过了
            var action = new T();
            action.Context = this;
            actions.Add(action);
        }

        protected virtual void InitEditorAction()
        {

        }

        private void UpdateClip(ActionClipEditorContext context, int index)
        {
            context.TitleView = view.Title.Group.GetTitleView(index);
            context.TitleView.SetCustomElement(context.CustomTitle);
            var clipTypeInfo = ActionClipTypeUtil.GetTypeInfo(context.Data.GetType());
            context.TitleView.SetStyle(clipTypeInfo.ClipColor, clipTypeInfo.Icon);
            context.TitleView.SetTitle(context.Data.Clip.name);
            context.TitleView.SetVisableButton(context.Data.IsActive);

            context.ClipView = view.Track.Group.GetClipView(index);
            context.ClipView.SetClipColor(clipTypeInfo.ClipColor);
            context.ClipView.SetClipName(context.Editor.GetClipShowName(context.Data.Clip));
            context.ClipView.SetCustomElement(context.CustomContent);
            context.ClipView.StartFrame = context.Data.Clip.StartFrame;
            context.ClipView.EndFrame = context.Data.Clip.StartFrame + context.Data.Clip.Length;
        }

    }
}
