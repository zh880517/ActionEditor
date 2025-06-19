namespace ActionLine.EditorView
{
    public static class ActionLineSelectUtil
    {

        public static void ClearSelected(this ActionLineEditorContext context, bool clip = true, bool track = true)
        {
            if (clip)
                context.SelectedClips.Clear();
            if (track)
                context.SelectedTracks.Clear();
            context.RefreshSelectState();
        }

        public static void SelectAll(this ActionLineEditorContext context, bool clip = true, bool track = true)
        {
            if (clip)
            {
                context.SelectedClips.Clear();
                context.SelectedClips.AddRange(context.Clips);
            }
            if (track)
            {
                context.SelectedTracks.Clear();
                context.SelectedTracks.AddRange(context.Clips);
            }
            context.RefreshSelectState();
        }

        public static void SelectClip(this ActionLineEditorContext context, int index, bool multi)
        {
            var data = context.Clips[index];
            if (multi)
            {
                int selectedIndex = context.SelectedClips.IndexOf(data);
                if (selectedIndex >= 0)
                {
                    context.SelectedClips.RemoveAt(selectedIndex);
                }
                else
                {
                    context.SelectedClips.Add(data);
                }
            }
            else
            {
                context.SelectedClips.Clear();
                context.SelectedClips.Add(data);
                context.SelectedTracks.Clear();
            }
            context.RefreshSelectState();
        }

        public static void SelectTrack(this ActionLineEditorContext context, int index, bool multi)
        {
            var data = context.Clips[index];
            if (multi)
            {
                int selectedIndex = context.SelectedTracks.IndexOf(data);
                if (selectedIndex >= 0)
                {
                    context.SelectedTracks.RemoveAt(selectedIndex);
                }
                else
                {
                    context.SelectedTracks.Add(data);
                }
            }
            else
            {
                context.SelectedTracks.Clear();
                context.SelectedTracks.Add(data);
                context.SelectedClips.Clear();
            }
            context.RefreshSelectState();
        }

        public static void ClearSelection(this ActionLineEditorContext context)
        {
            context.SelectedClips.Clear();
            context.SelectedTracks.Clear();
            context.RefreshSelectState();
        }
    }
}
