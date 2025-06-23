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
        [SerializeField]
        private ActionClipTypeSelectWindow typeSelectWindow;
        public List<ActionClipData> SelectedClips = new List<ActionClipData>();
        public List<ActionClipData> SelectedTracks = new List<ActionClipData>();

        private readonly List<ActionClipEditorContext> clipEditors = new List<ActionClipEditorContext>();
        private readonly List<ActionClipData> clips = new List<ActionClipData>();
        private readonly List<EditorAction> actions = new List<EditorAction>();
        private ActionLineView view;

        public IReadOnlyList<ActionClipData> Clips => clips;
        public ActionLineAsset Target => target;
        public ActionLineView View => view;

        protected virtual void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
            if(view == null)
            {
                view = new ActionLineView();
                view.AddManipulator(new ClipManipulator(this));
                view.AddManipulator(new TrackTitleManipulator(this));
                view.RegisterCallback<KeyDownEvent>(OnKeyDown);
            }
        }

        protected virtual void OnDestroy()
        {
            if (typeSelectWindow)
            {
                DestroyImmediate(typeSelectWindow);
                typeSelectWindow = null;
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
                if(target && (!asset || asset.GetType() != target.GetType()))
                {
                    if(typeSelectWindow)
                    {
                        DestroyImmediate(typeSelectWindow);
                        typeSelectWindow = null;
                    }
                }
                Clear();
                target = asset;
                if(target)
                {
                    if(actions.Count == 0)
                    {
                        ActionClipTypeUtil.CollectEditorAction(asset.GetType(), actions);
                        foreach (var item in actions)
                        {
                            item.Context = this;
                        }
                    }
                    RefreshViewPort();
                    RefreshView();
                }
            }
        }

        public void ShowTypeSelectWindow()
        {
            if (!Target)
                return;
            if(!typeSelectWindow)
            {
                typeSelectWindow = TypeSelectWindow.Create<ActionClipTypeSelectWindow>();
                typeSelectWindow.Context = this;
            }
            typeSelectWindow.Show(Event.current.mousePosition, 0, 0f);
        }

        public void ShowContextMenue(ActionModeType mode)
        {
            bool hasInherit = SelectedClips.Exists(x => x.IsInherit);
            GenericMenu menu = new GenericMenu();
            int preOrder = -1;
            foreach (var item in actions)
            {
                if(item.MenuPath == null || !item.Visable(mode))
                    continue;

                int order = item.ShowOrder / 100;
                if (order != preOrder)
                {
                    if (preOrder >= 0)
                        menu.AddSeparator("");
                    preOrder = order;
                }

                string menuPath = item.MenuPath;
                if (item.ShowShortCut && item.ShortCutKey != KeyCode.None)
                {
                    if (item.ActionKey && item.ShiftKey)
                        menuPath = $"{menuPath}\tCtrl + Shift + {item.ShortCutKey}";
                    else if (item.ActionKey)
                        menuPath = $"{menuPath}\tCtrl + {item.ShortCutKey}";
                    else if (item.ShiftKey)
                        menuPath = $"{menuPath}\tShift + {item.ShortCutKey}";
                    else
                        menuPath = $"{menuPath}\t{item.ShortCutKey}";
                }
                if (item.IsValid(mode))
                {
                    menu.AddItem(new GUIContent(menuPath, item.Icon), item.IsOn(mode), () => item.Execute(mode));
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent(item.MenuPath));
                }
            }
            menu.ShowAsContext();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            bool isActionKey = evt.actionKey;
            bool isShiftKey = evt.shiftKey;
            var matched = actions.Find(it => it.ShortCutKey == evt.keyCode && it.ActionKey == isActionKey && it.ShiftKey == isShiftKey);
            if (matched != null)
            {
                if (matched.IsValid(ActionModeType.ShortCut))
                {
                    matched.Execute(ActionModeType.ShortCut);
                    evt.StopPropagation();
                }
            }
        }

        public void RefreshViewPort()
        {
            view?.SetViewPort(viewPortData.Scale, viewPortData.Position.x, viewPortData.Position.y);
        }

        public void RefreshView()
        {
            clips.Clear();
            target.ExportClipData(clips);
            view.Property.SetAsset(target);
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
            SelectedClips.RemoveAll(it => !it.Clip);
            SelectedTracks.RemoveAll(it => !it.Clip);
            RefreshSelectState();
            view.Track.Group.UpdateClipPosition();
        }

        public void RefreshSelectState()
        {
            view.Property.SetVisableCount(SelectedClips.Count);
            for (int i = 0; i < SelectedClips.Count; i++)
            {
                var data = SelectedClips[i];
                view.Property.SetClip(i, data.Clip);
            }
            for (int i = 0; i < clipEditors.Count; i++)
            {
                var editor = clipEditors[i];
                bool isClipSelected = SelectedClips.Contains(editor.Data);
                bool isTrackSelected = SelectedTracks.Contains(editor.Data);
                editor.TitleView.SetSelected(isTrackSelected);
                editor.ClipView.ShowOutLine(isClipSelected);
                view.Track.Group.SetClipBGColor(i, isTrackSelected ? ActionLineStyles.SelectTrackColor : ActionLineStyles.NormalTrackColor);
                view.Track.Group.SetClipDisable(i, !editor.Data.IsActive);
            }
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
