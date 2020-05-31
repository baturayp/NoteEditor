using NoteEditor.DTO;
using NoteEditor.Notes;
using NoteEditor.Presenter;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NoteEditor.Model
{
    public class EditDataSerializer
    {
        public static string Serialize()
        {
            var dto = new MusicDTO.EditData
            {
                BPM = EditData.BPM.Value,
                maxBlock = EditData.MaxBlock.Value,
                offset = EditData.OffsetSamples.Value,
                name = Path.GetFileNameWithoutExtension(EditData.Name.Value)
            };

            var sortedNoteObjects = EditData.Notes.Values
                .Where(note => !(note.note.type == NoteTypes.Long && EditData.Notes.ContainsKey(note.note.prev)))
                .OrderBy(note => note.note.position.ToSamples(Audio.Source.clip.frequency, EditData.BPM.Value));

            dto.notes = new List<MusicDTO.Note>();

            foreach (var noteObject in sortedNoteObjects)
            {
                if (noteObject.note.type == NoteTypes.Single || noteObject.note.type == NoteTypes.Multi)
                {
                    dto.notes.Add(ToDTO(noteObject));
                }
                else if (noteObject.note.type == NoteTypes.Long)
                {
                    var current = noteObject;
                    var note = ToDTO(noteObject);

                    while (EditData.Notes.ContainsKey(current.note.next))
                    {
                        var nextObj = EditData.Notes[current.note.next];
                        //note.notes.Add(ToDTO(nextObj));
                        var duration = nextObj.note.position.num - current.note.position.num;
                        note.length += duration;
                        current = nextObj;
                    }

                    dto.notes.Add(note);
                }
            }

            return UnityEngine.JsonUtility.ToJson(dto,true);
        }

        public static void Deserialize(string json)
        {
            var editData = UnityEngine.JsonUtility.FromJson<MusicDTO.EditData>(json);
            var notePresenter = EditNotesPresenter.Instance;

            EditData.BPM.Value = editData.BPM;
            EditData.MaxBlock.Value = editData.maxBlock;
            EditData.OffsetSamples.Value = editData.offset;

            foreach (var note in editData.notes)
            {
                if (note.type == 1 || note.type == 3)
                {
                    notePresenter.AddNote(ToNoteObject(note));
                    continue;
                }

                if (note.type == 2)
                {
                    notePresenter.AddNote(ToNoteObject(note));
                    var nextNote = new Note(new NotePosition(note.LPB, note.num + note.length, note.block), NoteTypes.Long);
                    notePresenter.AddNote(nextNote);
                    var eNote = EditData.Notes[ToNoteObject(note).position];
                    var eNext = EditData.Notes[nextNote.position];
                    eNote.note.next = eNext.note.position;
                    eNext.note.prev = eNote.note.position;
                    continue;
                }

                //var longNoteObjects = new[] { note }.Concat(note.notes)
                //    .Select(note_ =>
                //    {
                //        notePresenter.AddNote(ToNoteObject(note_));
                //        return EditData.Notes[ToNoteObject(note_).position];
                //    })
                //    .ToList();

                //for (int i = 1; i < longNoteObjects.Count; i++)
                //{
                //    longNoteObjects[i].note.prev = longNoteObjects[i - 1].note.position;
                //    longNoteObjects[i - 1].note.next = longNoteObjects[i].note.position;
                //}

                EditState.LongNoteTailPosition.Value = NotePosition.None;
            }
        }

        // note types: 2 == Long, 1 == single, 3 == multi
        static MusicDTO.Note ToDTO(NoteObject noteObject)
        {
            var note = new MusicDTO.Note
            {
                num = noteObject.note.position.num,
                block = noteObject.note.position.block,
                LPB = noteObject.note.position.LPB,
                type = noteObject.note.type == NoteTypes.Long ? 2 : noteObject.note.type == NoteTypes.Single ? 1 : 3,
                length = noteObject.note.type == NoteTypes.Long ? 1 : 0,
                times = noteObject.note.type == NoteTypes.Multi ? noteObject.note.howManyTimes : 0,
            };
            return note;
        }

        public static Note ToNoteObject(MusicDTO.Note musicNote)
        {
           if (musicNote.type == 1)
            {
                return new Note(
               new NotePosition(musicNote.LPB, musicNote.num, musicNote.block), NoteTypes.Single);
            }
            
            if (musicNote.type == 2)
            {
                return new Note(
               new NotePosition(musicNote.LPB, musicNote.num, musicNote.block), NoteTypes.Long);
            }

            if (musicNote.type == 3)
            {
                return new Note(
               new NotePosition(musicNote.LPB, musicNote.num, musicNote.block), NoteTypes.Multi, musicNote.times);
            }

            return new Note();
        }
    }
}
