using NoteEditor.GLDrawing;
using NoteEditor.Model;
using NoteEditor.Presenter;
using NoteEditor.Utility;
using System;
using System.Linq;
using UniRx;
using UnityEngine;

namespace NoteEditor.Notes
{
    public class NoteObject : IDisposable
    {
        public Note note = new Note();
        public ReactiveProperty<bool> isSelected = new ReactiveProperty<bool>();
        public Subject<Unit> LateUpdateObservable = new Subject<Unit>();
        public Subject<Unit> OnClickObservable = new Subject<Unit>();
        public Color NoteColor { get { return noteColor_.Value; } }
        ReactiveProperty<Color> noteColor_ = new ReactiveProperty<Color>();

        Color selectedStateColor = new Color(255 / 255f, 0 / 255f, 255 / 255f);
        Color singleNoteColor = new Color(175 / 255f, 255 / 255f, 78 / 255f);
        Color longNoteColor = new Color(0 / 255f, 255 / 255f, 255 / 255f);
        Color multiNoteColor = new Color(124 / 255f, 0 / 255f, 255 / 255f);
        Color invalidStateColor = new Color(255 / 255f, 0 / 255f, 0 / 255f);

        public ReadOnlyReactiveProperty<NoteTypes> noteType { get; private set; }
        CompositeDisposable disposable = new CompositeDisposable();

        public void Init()
        {
            disposable = new CompositeDisposable(
                isSelected,
                LateUpdateObservable,
                OnClickObservable,
                noteColor_,
                noteType);

            var editPresenter = EditNotesPresenter.Instance;
            noteType = this.ObserveEveryValueChanged(_ => note.type).ToReadOnlyReactiveProperty();

            //long note
            disposable.Add(noteType.Where(_ => !isSelected.Value)
                .Merge(isSelected.Select(_ => noteType.Value))
                .Select(type => type == NoteTypes.Long)
                .Subscribe(isLongNote => { if (isLongNote) noteColor_.Value = longNoteColor; }));

            //single note
            disposable.Add(noteType.Where(_ => !isSelected.Value)
                .Merge(isSelected.Select(_ => noteType.Value))
                .Select(type => type == NoteTypes.Single)
                .Subscribe(isSingleNote => { if (isSingleNote) noteColor_.Value = singleNoteColor; }));

            //multi note
            disposable.Add(noteType.Where(_ => !isSelected.Value)
                .Merge(isSelected.Select(_ => noteType.Value))
                .Select(type => type == NoteTypes.Multi)
                .Subscribe(isMultiNote => { if (isMultiNote) noteColor_.Value = multiNoteColor; }));

            disposable.Add(isSelected.Where(selected => selected)
                .Subscribe(_ => noteColor_.Value = selectedStateColor));

            var mouseDownObservable = OnClickObservable
                .Select(_ => EditState.NoteType.Value)
                .Where(_ => NoteCanvas.ClosestNotePosition.Value.Equals(note.position));

            disposable.Add(mouseDownObservable.Where(editType => editType == NoteTypes.Single)
                .Where(editType => editType == noteType.Value)
                .Subscribe(_ => editPresenter.RequestForRemoveNote.OnNext(note)));

            //multi note
            disposable.Add(mouseDownObservable.Where(editType => editType == NoteTypes.Multi)
                .Where(editType => editType == noteType.Value)
                .Where(_ => !KeyInput.ShiftKey())
                .Subscribe(_ => { note.howManyTimes += 1; }));

            disposable.Add(mouseDownObservable.Where(editType => editType == NoteTypes.Multi)
                .Where(editType => editType == noteType.Value)
                .Where(_ => KeyInput.ShiftKey())
                .Where(_ => note.howManyTimes > 1)
                .Subscribe(_ => { note.howManyTimes -= 1; }));

            disposable.Add(mouseDownObservable.Where(editType => editType == NoteTypes.Long)
                .Where(editType => editType == noteType.Value)
                .Subscribe(_ =>
                {
                    if (EditData.Notes.ContainsKey(EditState.LongNoteTailPosition.Value))
                    {
                        return;
                    }
                    else
                    {
                        if (EditData.Notes.ContainsKey(note.prev) && !EditData.Notes.ContainsKey(note.next))
                            EditState.LongNoteTailPosition.Value = note.prev;

                        if (!EditData.Notes.ContainsKey(note.prev) && EditData.Notes.ContainsKey(note.next)) 
                        { 
                        var orphanedNote = EditData.Notes[note.next].note;
                        editPresenter.RequestForRemoveNote.OnNext(orphanedNote);
                        }

                        editPresenter.RequestForRemoveNote.OnNext(new Note(note.position, EditState.NoteType.Value, note.next, note.prev));
                        RemoveLink();
                    }
                }));

            var longNoteUpdateObservable = LateUpdateObservable
                .Where(_ => noteType.Value == NoteTypes.Long);

            disposable.Add(longNoteUpdateObservable
                .Where(_ => EditData.Notes.ContainsKey(note.next))
                .Select(_ => ConvertUtils.NoteToCanvasPosition(note.next))
                .Merge(longNoteUpdateObservable
                    .Where(_ => EditState.NoteType.Value == NoteTypes.Long)
                    .Where(_ => EditState.LongNoteTailPosition.Value.Equals(note.position))
                    .Select(_ => ConvertUtils.ScreenToCanvasPosition(Input.mousePosition)))
                .Select(nextPosition => new Line(
                    ConvertUtils.CanvasToScreenPosition(ConvertUtils.NoteToCanvasPosition(note.position)),
                    ConvertUtils.CanvasToScreenPosition(nextPosition),
                    isSelected.Value || EditData.Notes.ContainsKey(note.next) && EditData.Notes[note.next].isSelected.Value ? selectedStateColor
                        : ((0 < nextPosition.x - ConvertUtils.NoteToCanvasPosition(note.position).x) && (nextPosition.y < ConvertUtils.NoteToCanvasPosition(note.position).y + 40) && (nextPosition.y > ConvertUtils.NoteToCanvasPosition(note.position).y - 40)) ? longNoteColor : invalidStateColor))
                .Subscribe(line => GLLineDrawer.Draw(line)));
        }

        void RemoveLink()
        {
            if (EditData.Notes.ContainsKey(note.prev))
                EditData.Notes[note.prev].note.next = note.next;

            if (EditData.Notes.ContainsKey(note.next))
                EditData.Notes[note.next].note.prev = note.prev;
        }

        void InsertLink(NotePosition position)
        {
            if (EditData.Notes.ContainsKey(note.prev))
                EditData.Notes[note.prev].note.next = position;

            if (EditData.Notes.ContainsKey(note.next))
                EditData.Notes[note.next].note.prev = position;
        }

        public void SetState(Note note)
        {
            if (note.type == NoteTypes.Single)
            {
                RemoveLink();
            }

            this.note = note;

            if (note.type == NoteTypes.Long)
            {
                InsertLink(note.position);
                EditState.LongNoteTailPosition.Value = EditState.LongNoteTailPosition.Value.Equals(NotePosition.None)
                    ? note.position
                    : NotePosition.None;
                if (EditData.Notes.ContainsKey(note.prev))
                    EditState.NoteType.Value = NoteTypes.Single;
            }
        }

        public void Dispose()
        {
            disposable.Dispose();
        }
    }
}
