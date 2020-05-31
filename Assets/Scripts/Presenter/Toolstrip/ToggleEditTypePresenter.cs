using NoteEditor.Notes;
using NoteEditor.Model;
using NoteEditor.Utility;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace NoteEditor.Presenter
{
    public class ToggleEditTypePresenter : MonoBehaviour
    {
        [SerializeField]
        Button editTypeToggleButton = default;
        [SerializeField]
        Sprite iconLongNotes = default;
        [SerializeField]
        Sprite iconSingleNotes = default;
        [SerializeField]
        Sprite iconMultiNotes = default;
        [SerializeField]
        Color longTypeStateButtonColor = default;
        [SerializeField]
        Color singleTypeStateButtonColor = default;
        [SerializeField]
        Color multiTypeStateButtonColor = default;

        void Awake()
        {
            editTypeToggleButton.OnClickAsObservable()
                .Merge(this.UpdateAsObservable().Where(_ => KeyInput.AltKeyDown()))
                .Select(_ => EditState.NoteType.Value == NoteTypes.Single ? NoteTypes.Long : 
                                EditState.NoteType.Value == NoteTypes.Long ? NoteTypes.Multi : NoteTypes.Single)
                .Subscribe(editType => EditState.NoteType.Value = editType);

            var buttonImage = editTypeToggleButton.GetComponent<Image>();

            EditState.NoteType.Select(_ => EditState.NoteType.Value == NoteTypes.Long)
                .Subscribe(isLongType =>
                {
                    if (isLongType)
                    {
                        buttonImage.sprite = iconLongNotes;
                        buttonImage.color = longTypeStateButtonColor;
                    }
                });

            EditState.NoteType.Select(_ => EditState.NoteType.Value == NoteTypes.Multi)
                .Subscribe(isMultiType =>
                {
                    if (isMultiType)
                    {
                        buttonImage.sprite = iconMultiNotes;
                        buttonImage.color = multiTypeStateButtonColor;
                    }
                });

            EditState.NoteType.Select(_ => EditState.NoteType.Value == NoteTypes.Single)
                .Subscribe(isSingleType =>
                {
                    if (isSingleType)
                    {
                        buttonImage.sprite = iconSingleNotes;
                        buttonImage.color = singleTypeStateButtonColor;
                    }
                });
        }
    }
}
